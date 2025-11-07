using Api.Models;
using MongoDB.Driver;

namespace Api.Services;

public class LoopService : ILoopService
{
    private readonly IMongoCollection<Loop> _loopsCollection;
    private readonly IMongoCollection<User> _usersCollection;
    private readonly ILogger<LoopService> _logger;
    private IItemsService? _itemsService;
    private ILoopInvitationService? _loopInvitationService;
    private ILoopJoinRequestService? _loopJoinRequestService;

    public LoopService(IMongoDatabase database, IConfiguration configuration, ILogger<LoopService> logger)
    {
        var loopsCollectionName = configuration["MongoDB:LoopsCollectionName"] ?? "loops";
        var usersCollectionName = configuration["MongoDB:UsersCollectionName"] ?? "users";
        
        _loopsCollection = database.GetCollection<Loop>(loopsCollectionName);
        _usersCollection = database.GetCollection<User>(usersCollectionName);
        _logger = logger;
        
        // Ensure indexes are created when service is instantiated
        _ = Task.Run(EnsureIndexesAsync);
    }
    
    // Method to set dependencies after construction to avoid circular dependency
    public void SetDependencies(IItemsService itemsService, ILoopInvitationService loopInvitationService, ILoopJoinRequestService loopJoinRequestService)
    {
        _itemsService = itemsService;
        _loopInvitationService = loopInvitationService;
        _loopJoinRequestService = loopJoinRequestService;
    }

    public async Task<Loop> CreateLoopAsync(string name, string creatorId)
    {
        var loop = new Loop
        {
            Name = name,
            CreatorId = creatorId,
            MemberIds = new List<string> { creatorId },
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _loopsCollection.InsertOneAsync(loop);
        _logger.LogInformation("Loop created: {LoopId} by user {UserId}", loop.Id, creatorId);
        return loop;
    }

    public async Task<Loop?> GetLoopByIdAsync(string loopId)
    {
        return await _loopsCollection.Find(l => l.Id == loopId).FirstOrDefaultAsync();
    }

    public async Task<List<Loop>> GetUserLoopsAsync(string userId)
    {
        var filter = Builders<Loop>.Filter.And(
            Builders<Loop>.Filter.AnyEq(l => l.MemberIds, userId),
            Builders<Loop>.Filter.Eq(l => l.IsArchived, false)
        );
        return await _loopsCollection.Find(filter).ToListAsync();
    }

    public async Task<List<User>> GetLoopMembersAsync(string loopId)
    {
        var loop = await GetLoopByIdAsync(loopId);
        if (loop == null)
        {
            return new List<User>();
        }

        var filter = Builders<User>.Filter.In(u => u.Id, loop.MemberIds);
        return await _usersCollection.Find(filter).ToListAsync();
    }

    public async Task<bool> IsUserLoopMemberAsync(string loopId, string userId)
    {
        var loop = await GetLoopByIdAsync(loopId);
        return loop?.MemberIds.Contains(userId) ?? false;
    }

    public async Task<Loop?> AddMemberToLoopAsync(string loopId, string userId)
    {
        var filter = Builders<Loop>.Filter.Eq(l => l.Id, loopId);
        var update = Builders<Loop>.Update
            .AddToSet(l => l.MemberIds, userId)
            .Set(l => l.UpdatedAt, DateTime.UtcNow);

        var options = new FindOneAndUpdateOptions<Loop>
        {
            ReturnDocument = ReturnDocument.After
        };

        var loop = await _loopsCollection.FindOneAndUpdateAsync(filter, update, options);
        if (loop != null)
        {
            _logger.LogInformation("User {UserId} added to loop {LoopId}", userId, loopId);
        }
        return loop;
    }

    public async Task<Loop?> RemoveMemberFromLoopAsync(string loopId, string userId)
    {
        var filter = Builders<Loop>.Filter.Eq(l => l.Id, loopId);
        var update = Builders<Loop>.Update
            .Pull(l => l.MemberIds, userId)
            .Set(l => l.UpdatedAt, DateTime.UtcNow);

        var options = new FindOneAndUpdateOptions<Loop>
        {
            ReturnDocument = ReturnDocument.After
        };

        var loop = await _loopsCollection.FindOneAndUpdateAsync(filter, update, options);
        if (loop != null)
        {
            // Remove loop from user's items
            if (_itemsService != null)
            {
                await _itemsService.RemoveLoopFromUserItemsAsync(userId, loopId);
            }
            
            _logger.LogInformation("User {UserId} removed from loop {LoopId}", userId, loopId);
        }
        return loop;
    }

    public async Task<List<User>> GetPotentialInviteesFromOtherLoopsAsync(string userId, string currentLoopId)
    {
        // Get all loops the user is a member of
        var userLoops = await GetUserLoopsAsync(userId);
        
        // Get the current loop to exclude its members
        var currentLoop = await GetLoopByIdAsync(currentLoopId);
        var currentLoopMemberIds = currentLoop?.MemberIds ?? new List<string>();
        
        // Collect all unique user IDs from other loops
        var potentialInviteeIds = userLoops
            .Where(l => l.Id != currentLoopId)
            .SelectMany(l => l.MemberIds)
            .Distinct()
            .Where(id => id != userId && !currentLoopMemberIds.Contains(id))
            .ToList();

        if (!potentialInviteeIds.Any())
        {
            return new List<User>();
        }

        // Fetch user details
        var filter = Builders<User>.Filter.In(u => u.Id, potentialInviteeIds);
        return await _usersCollection.Find(filter).ToListAsync();
    }

    public async Task<Loop?> UpdateLoopSettingsAsync(string loopId, string name, string description, bool isPublic)
    {
        var filter = Builders<Loop>.Filter.Eq(l => l.Id, loopId);
        var update = Builders<Loop>.Update
            .Set(l => l.Name, name)
            .Set(l => l.Description, description)
            .Set(l => l.IsPublic, isPublic)
            .Set(l => l.UpdatedAt, DateTime.UtcNow);

        var options = new FindOneAndUpdateOptions<Loop>
        {
            ReturnDocument = ReturnDocument.After
        };

        var loop = await _loopsCollection.FindOneAndUpdateAsync(filter, update, options);
        if (loop != null)
        {
            _logger.LogInformation("Loop settings updated for loop {LoopId}", loopId);
        }
        return loop;
    }

    public async Task<bool> IsLoopOwnerAsync(string loopId, string userId)
    {
        var loop = await GetLoopByIdAsync(loopId);
        return loop?.CreatorId == userId;
    }

    public async Task<Loop?> ArchiveLoopAsync(string loopId)
    {
        var filter = Builders<Loop>.Filter.Eq(l => l.Id, loopId);
        var update = Builders<Loop>.Update
            .Set(l => l.IsArchived, true)
            .Set(l => l.ArchivedAt, DateTime.UtcNow)
            .Set(l => l.UpdatedAt, DateTime.UtcNow);

        var options = new FindOneAndUpdateOptions<Loop>
        {
            ReturnDocument = ReturnDocument.After
        };

        var loop = await _loopsCollection.FindOneAndUpdateAsync(filter, update, options);
        if (loop != null)
        {
            _logger.LogInformation("Loop {LoopId} archived", loopId);
        }
        return loop;
    }

    public async Task<Loop?> RestoreLoopAsync(string loopId)
    {
        var filter = Builders<Loop>.Filter.Eq(l => l.Id, loopId);
        var update = Builders<Loop>.Update
            .Set(l => l.IsArchived, false)
            .Set(l => l.ArchivedAt, null)
            .Set(l => l.UpdatedAt, DateTime.UtcNow);

        var options = new FindOneAndUpdateOptions<Loop>
        {
            ReturnDocument = ReturnDocument.After
        };

        var loop = await _loopsCollection.FindOneAndUpdateAsync(filter, update, options);
        if (loop != null)
        {
            _logger.LogInformation("Loop {LoopId} restored", loopId);
        }
        return loop;
    }

    public async Task<List<Loop>> GetArchivedLoopsAsync(string userId)
    {
        var filter = Builders<Loop>.Filter.And(
            Builders<Loop>.Filter.AnyEq(l => l.MemberIds, userId),
            Builders<Loop>.Filter.Eq(l => l.IsArchived, true)
        );
        return await _loopsCollection.Find(filter).ToListAsync();
    }

    public async Task<bool> DeleteLoopAsync(string loopId)
    {
        // Remove loop from all items' visibleToLoopIds
        if (_itemsService != null)
        {
            await _itemsService.RemoveLoopFromAllItemsAsync(loopId);
        }

        // Delete all invitations for this loop
        if (_loopInvitationService != null)
        {
            await _loopInvitationService.DeleteInvitationsForLoopAsync(loopId);
        }

        // Delete all join requests for this loop
        if (_loopJoinRequestService != null)
        {
            await _loopJoinRequestService.DeleteJoinRequestsForLoopAsync(loopId);
        }

        // Delete the loop itself
        var filter = Builders<Loop>.Filter.Eq(l => l.Id, loopId);
        var result = await _loopsCollection.DeleteOneAsync(filter);

        if (result.DeletedCount > 0)
        {
            _logger.LogInformation("Loop {LoopId} permanently deleted", loopId);
            return true;
        }

        _logger.LogWarning("Loop {LoopId} not found for deletion", loopId);
        return false;
    }

    public async Task<Loop?> InitiateOwnershipTransferAsync(string loopId, string fromUserId, string toUserId)
    {
        var loop = await GetLoopByIdAsync(loopId);
        if (loop == null)
        {
            return null;
        }

        // Check if there's already a pending transfer
        var pendingTransfer = loop.OwnershipHistory.FirstOrDefault(t => t.Status == TransferStatus.Pending);
        if (pendingTransfer != null)
        {
            _logger.LogWarning("Loop {LoopId} already has a pending ownership transfer", loopId);
            return null;
        }

        // Create new transfer record
        var transfer = new OwnershipTransfer
        {
            FromUserId = fromUserId,
            ToUserId = toUserId,
            TransferredAt = DateTime.UtcNow,
            Status = TransferStatus.Pending
        };

        var filter = Builders<Loop>.Filter.Eq(l => l.Id, loopId);
        var update = Builders<Loop>.Update
            .Push(l => l.OwnershipHistory, transfer)
            .Set(l => l.UpdatedAt, DateTime.UtcNow);

        var options = new FindOneAndUpdateOptions<Loop>
        {
            ReturnDocument = ReturnDocument.After
        };

        var updatedLoop = await _loopsCollection.FindOneAndUpdateAsync(filter, update, options);
        if (updatedLoop != null)
        {
            _logger.LogInformation("Ownership transfer initiated for loop {LoopId} from {FromUserId} to {ToUserId}", 
                loopId, fromUserId, toUserId);
        }
        return updatedLoop;
    }

    public async Task<Loop?> AcceptOwnershipTransferAsync(string loopId, string userId)
    {
        var loop = await GetLoopByIdAsync(loopId);
        if (loop == null)
        {
            return null;
        }

        var pendingTransfer = loop.OwnershipHistory.FirstOrDefault(t => 
            t.Status == TransferStatus.Pending && t.ToUserId == userId);
        
        if (pendingTransfer == null)
        {
            _logger.LogWarning("No pending transfer found for user {UserId} in loop {LoopId}", userId, loopId);
            return null;
        }

        // Update the transfer status
        pendingTransfer.Status = TransferStatus.Accepted;

        var filter = Builders<Loop>.Filter.Eq(l => l.Id, loopId);
        var update = Builders<Loop>.Update
            .Set(l => l.CreatorId, userId)
            .Set(l => l.OwnershipHistory, loop.OwnershipHistory)
            .Set(l => l.UpdatedAt, DateTime.UtcNow);

        var options = new FindOneAndUpdateOptions<Loop>
        {
            ReturnDocument = ReturnDocument.After
        };

        var updatedLoop = await _loopsCollection.FindOneAndUpdateAsync(filter, update, options);
        if (updatedLoop != null)
        {
            _logger.LogInformation("Ownership transfer accepted for loop {LoopId}, new owner: {UserId}", loopId, userId);
        }
        return updatedLoop;
    }

    public async Task<Loop?> DeclineOwnershipTransferAsync(string loopId, string userId)
    {
        var loop = await GetLoopByIdAsync(loopId);
        if (loop == null)
        {
            return null;
        }

        var pendingTransfer = loop.OwnershipHistory.FirstOrDefault(t => 
            t.Status == TransferStatus.Pending && t.ToUserId == userId);
        
        if (pendingTransfer == null)
        {
            _logger.LogWarning("No pending transfer found for user {UserId} in loop {LoopId}", userId, loopId);
            return null;
        }

        // Update the transfer status
        pendingTransfer.Status = TransferStatus.Declined;

        var filter = Builders<Loop>.Filter.Eq(l => l.Id, loopId);
        var update = Builders<Loop>.Update
            .Set(l => l.OwnershipHistory, loop.OwnershipHistory)
            .Set(l => l.UpdatedAt, DateTime.UtcNow);

        var options = new FindOneAndUpdateOptions<Loop>
        {
            ReturnDocument = ReturnDocument.After
        };

        var updatedLoop = await _loopsCollection.FindOneAndUpdateAsync(filter, update, options);
        if (updatedLoop != null)
        {
            _logger.LogInformation("Ownership transfer declined for loop {LoopId} by user {UserId}", loopId, userId);
        }
        return updatedLoop;
    }

    public async Task<Loop?> CancelOwnershipTransferAsync(string loopId, string userId)
    {
        var loop = await GetLoopByIdAsync(loopId);
        if (loop == null)
        {
            return null;
        }

        var pendingTransfer = loop.OwnershipHistory.FirstOrDefault(t => 
            t.Status == TransferStatus.Pending && t.FromUserId == userId);
        
        if (pendingTransfer == null)
        {
            _logger.LogWarning("No pending transfer found initiated by user {UserId} in loop {LoopId}", userId, loopId);
            return null;
        }

        // Update the transfer status
        pendingTransfer.Status = TransferStatus.Cancelled;

        var filter = Builders<Loop>.Filter.Eq(l => l.Id, loopId);
        var update = Builders<Loop>.Update
            .Set(l => l.OwnershipHistory, loop.OwnershipHistory)
            .Set(l => l.UpdatedAt, DateTime.UtcNow);

        var options = new FindOneAndUpdateOptions<Loop>
        {
            ReturnDocument = ReturnDocument.After
        };

        var updatedLoop = await _loopsCollection.FindOneAndUpdateAsync(filter, update, options);
        if (updatedLoop != null)
        {
            _logger.LogInformation("Ownership transfer cancelled for loop {LoopId} by user {UserId}", loopId, userId);
        }
        return updatedLoop;
    }

    public async Task<OwnershipTransfer?> GetPendingOwnershipTransferAsync(string loopId)
    {
        var loop = await GetLoopByIdAsync(loopId);
        if (loop == null)
        {
            return null;
        }

        return loop.OwnershipHistory.FirstOrDefault(t => t.Status == TransferStatus.Pending);
    }

    public async Task<List<Loop>> GetPublicLoopsAsync(int skip = 0, int limit = 20)
    {
        var filter = Builders<Loop>.Filter.And(
            Builders<Loop>.Filter.Eq(l => l.IsPublic, true),
            Builders<Loop>.Filter.Eq(l => l.IsArchived, false)
        );
        var sort = Builders<Loop>.Sort.Descending(l => l.CreatedAt);
        
        return await _loopsCollection.Find(filter)
            .Sort(sort)
            .Skip(skip)
            .Limit(limit)
            .ToListAsync();
    }

    public async Task<List<Loop>> SearchPublicLoopsAsync(string searchTerm, int skip = 0, int limit = 20)
    {
        var filter = Builders<Loop>.Filter.And(
            Builders<Loop>.Filter.Eq(l => l.IsPublic, true),
            Builders<Loop>.Filter.Eq(l => l.IsArchived, false),
            Builders<Loop>.Filter.Or(
                Builders<Loop>.Filter.Regex(l => l.Name, new MongoDB.Bson.BsonRegularExpression(searchTerm, "i")),
                Builders<Loop>.Filter.Regex(l => l.Description, new MongoDB.Bson.BsonRegularExpression(searchTerm, "i"))
            )
        );
        var sort = Builders<Loop>.Sort.Descending(l => l.CreatedAt);
        
        return await _loopsCollection.Find(filter)
            .Sort(sort)
            .Skip(skip)
            .Limit(limit)
            .ToListAsync();
    }

    public async Task<Loop?> LeaveLoopAsync(string loopId, string userId)
    {
        var loop = await GetLoopByIdAsync(loopId);
        if (loop == null)
        {
            return null;
        }

        // Prevent owner from leaving
        if (loop.CreatorId == userId)
        {
            _logger.LogWarning("Owner {UserId} cannot leave loop {LoopId} without transferring ownership", userId, loopId);
            return null;
        }

        // Remove user from loop
        var updatedLoop = await RemoveMemberFromLoopAsync(loopId, userId);

        // Remove loop from user's items
        if (_itemsService != null)
        {
            await _itemsService.RemoveLoopFromUserItemsAsync(userId, loopId);
        }

        _logger.LogInformation("User {UserId} left loop {LoopId}", userId, loopId);
        return updatedLoop;
    }

    private async Task EnsureIndexesAsync()
    {
        try
        {
            // Create index on creatorId field
            var creatorIdIndexKeys = Builders<Loop>.IndexKeys.Ascending(l => l.CreatorId);
            var creatorIdIndexModel = new CreateIndexModel<Loop>(creatorIdIndexKeys);

            // Create index on memberIds field for efficient member queries
            var memberIdsIndexKeys = Builders<Loop>.IndexKeys.Ascending(l => l.MemberIds);
            var memberIdsIndexModel = new CreateIndexModel<Loop>(memberIdsIndexKeys);

            // Create index on isArchived field for filtering archived loops
            var isArchivedIndexKeys = Builders<Loop>.IndexKeys.Ascending(l => l.IsArchived);
            var isArchivedIndexModel = new CreateIndexModel<Loop>(isArchivedIndexKeys);

            // Create index on isPublic field for public loop queries
            var isPublicIndexKeys = Builders<Loop>.IndexKeys.Ascending(l => l.IsPublic);
            var isPublicIndexModel = new CreateIndexModel<Loop>(isPublicIndexKeys);

            // Create compound index on isPublic + isArchived for public loop discovery
            var publicArchivedIndexKeys = Builders<Loop>.IndexKeys
                .Ascending(l => l.IsPublic)
                .Ascending(l => l.IsArchived);
            var publicArchivedIndexModel = new CreateIndexModel<Loop>(publicArchivedIndexKeys);

            // Create compound text index on name and description for search functionality
            // MongoDB only allows one text index per collection, so we combine both fields
            var textIndexKeys = Builders<Loop>.IndexKeys
                .Text(l => l.Name)
                .Text(l => l.Description);
            var textIndexModel = new CreateIndexModel<Loop>(textIndexKeys);

            await _loopsCollection.Indexes.CreateManyAsync(new[]
            {
                creatorIdIndexModel,
                memberIdsIndexModel,
                isArchivedIndexModel,
                isPublicIndexModel,
                publicArchivedIndexModel,
                textIndexModel
            });

            _logger.LogInformation("Indexes created successfully for Loops collection");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not create indexes for Loops collection");
        }
    }
}
