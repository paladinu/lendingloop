using Api.Models;
using MongoDB.Driver;

namespace Api.Services;

public class LoopJoinRequestService : ILoopJoinRequestService
{
    private readonly IMongoCollection<LoopJoinRequest> _joinRequestsCollection;
    private readonly ILoopService _loopService;
    private readonly ILogger<LoopJoinRequestService> _logger;

    public LoopJoinRequestService(
        IMongoDatabase database,
        IConfiguration configuration,
        ILoopService loopService,
        ILogger<LoopJoinRequestService> logger)
    {
        var collectionName = configuration["MongoDB:LoopJoinRequestsCollectionName"] ?? "loopJoinRequests";
        _joinRequestsCollection = database.GetCollection<LoopJoinRequest>(collectionName);
        _loopService = loopService;
        _logger = logger;
        
        // Ensure indexes are created when service is instantiated
        _ = Task.Run(EnsureIndexesAsync);
    }

    public async Task<LoopJoinRequest> CreateJoinRequestAsync(string loopId, string userId, string message)
    {
        // Check if user is already a member
        var isMember = await _loopService.IsUserLoopMemberAsync(loopId, userId);
        if (isMember)
        {
            _logger.LogWarning("User {UserId} is already a member of loop {LoopId}", userId, loopId);
            throw new InvalidOperationException("User is already a member of this loop");
        }

        // Check if there's already a pending request
        var hasPending = await HasPendingJoinRequestAsync(loopId, userId);
        if (hasPending)
        {
            _logger.LogWarning("User {UserId} already has a pending join request for loop {LoopId}", userId, loopId);
            throw new InvalidOperationException("User already has a pending join request for this loop");
        }

        var joinRequest = new LoopJoinRequest
        {
            LoopId = loopId,
            UserId = userId,
            Message = message,
            Status = JoinRequestStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        await _joinRequestsCollection.InsertOneAsync(joinRequest);
        _logger.LogInformation("Join request created for user {UserId} to loop {LoopId}", userId, loopId);
        return joinRequest;
    }

    public async Task<LoopJoinRequest?> GetJoinRequestByIdAsync(string requestId)
    {
        return await _joinRequestsCollection.Find(r => r.Id == requestId).FirstOrDefaultAsync();
    }

    public async Task<List<LoopJoinRequest>> GetPendingJoinRequestsForLoopAsync(string loopId)
    {
        var filter = Builders<LoopJoinRequest>.Filter.And(
            Builders<LoopJoinRequest>.Filter.Eq(r => r.LoopId, loopId),
            Builders<LoopJoinRequest>.Filter.Eq(r => r.Status, JoinRequestStatus.Pending)
        );
        return await _joinRequestsCollection.Find(filter).ToListAsync();
    }

    public async Task<List<LoopJoinRequest>> GetUserJoinRequestsAsync(string userId)
    {
        var filter = Builders<LoopJoinRequest>.Filter.Eq(r => r.UserId, userId);
        var sort = Builders<LoopJoinRequest>.Sort.Descending(r => r.CreatedAt);
        return await _joinRequestsCollection.Find(filter).Sort(sort).ToListAsync();
    }

    public async Task<LoopJoinRequest?> ApproveJoinRequestAsync(string requestId, string ownerId)
    {
        var joinRequest = await GetJoinRequestByIdAsync(requestId);
        if (joinRequest == null)
        {
            _logger.LogWarning("Join request {RequestId} not found", requestId);
            return null;
        }

        if (joinRequest.Status != JoinRequestStatus.Pending)
        {
            _logger.LogWarning("Join request {RequestId} is not pending", requestId);
            return null;
        }

        // Verify the owner
        var isOwner = await _loopService.IsLoopOwnerAsync(joinRequest.LoopId, ownerId);
        if (!isOwner)
        {
            _logger.LogWarning("User {UserId} is not the owner of loop {LoopId}", ownerId, joinRequest.LoopId);
            return null;
        }

        // Add user to loop
        await _loopService.AddMemberToLoopAsync(joinRequest.LoopId, joinRequest.UserId);

        // Update request status
        var filter = Builders<LoopJoinRequest>.Filter.Eq(r => r.Id, requestId);
        var update = Builders<LoopJoinRequest>.Update
            .Set(r => r.Status, JoinRequestStatus.Approved)
            .Set(r => r.RespondedAt, DateTime.UtcNow);

        var options = new FindOneAndUpdateOptions<LoopJoinRequest>
        {
            ReturnDocument = ReturnDocument.After
        };

        var updatedRequest = await _joinRequestsCollection.FindOneAndUpdateAsync(filter, update, options);
        if (updatedRequest != null)
        {
            _logger.LogInformation("Join request {RequestId} approved by owner {OwnerId}", requestId, ownerId);
        }
        return updatedRequest;
    }

    public async Task<LoopJoinRequest?> RejectJoinRequestAsync(string requestId, string ownerId)
    {
        var joinRequest = await GetJoinRequestByIdAsync(requestId);
        if (joinRequest == null)
        {
            _logger.LogWarning("Join request {RequestId} not found", requestId);
            return null;
        }

        if (joinRequest.Status != JoinRequestStatus.Pending)
        {
            _logger.LogWarning("Join request {RequestId} is not pending", requestId);
            return null;
        }

        // Verify the owner
        var isOwner = await _loopService.IsLoopOwnerAsync(joinRequest.LoopId, ownerId);
        if (!isOwner)
        {
            _logger.LogWarning("User {UserId} is not the owner of loop {LoopId}", ownerId, joinRequest.LoopId);
            return null;
        }

        // Update request status
        var filter = Builders<LoopJoinRequest>.Filter.Eq(r => r.Id, requestId);
        var update = Builders<LoopJoinRequest>.Update
            .Set(r => r.Status, JoinRequestStatus.Rejected)
            .Set(r => r.RespondedAt, DateTime.UtcNow);

        var options = new FindOneAndUpdateOptions<LoopJoinRequest>
        {
            ReturnDocument = ReturnDocument.After
        };

        var updatedRequest = await _joinRequestsCollection.FindOneAndUpdateAsync(filter, update, options);
        if (updatedRequest != null)
        {
            _logger.LogInformation("Join request {RequestId} rejected by owner {OwnerId}", requestId, ownerId);
        }
        return updatedRequest;
    }

    public async Task<bool> HasPendingJoinRequestAsync(string loopId, string userId)
    {
        var filter = Builders<LoopJoinRequest>.Filter.And(
            Builders<LoopJoinRequest>.Filter.Eq(r => r.LoopId, loopId),
            Builders<LoopJoinRequest>.Filter.Eq(r => r.UserId, userId),
            Builders<LoopJoinRequest>.Filter.Eq(r => r.Status, JoinRequestStatus.Pending)
        );
        var count = await _joinRequestsCollection.CountDocumentsAsync(filter);
        return count > 0;
    }

    public async Task DeleteJoinRequestsForLoopAsync(string loopId)
    {
        var filter = Builders<LoopJoinRequest>.Filter.Eq(r => r.LoopId, loopId);
        var result = await _joinRequestsCollection.DeleteManyAsync(filter);
        _logger.LogInformation("Deleted {Count} join requests for loop {LoopId}", result.DeletedCount, loopId);
    }

    private async Task EnsureIndexesAsync()
    {
        try
        {
            // Create index on loopId
            var loopIdIndexKeys = Builders<LoopJoinRequest>.IndexKeys.Ascending(r => r.LoopId);
            var loopIdIndexModel = new CreateIndexModel<LoopJoinRequest>(loopIdIndexKeys);

            // Create index on userId
            var userIdIndexKeys = Builders<LoopJoinRequest>.IndexKeys.Ascending(r => r.UserId);
            var userIdIndexModel = new CreateIndexModel<LoopJoinRequest>(userIdIndexKeys);

            // Create index on status
            var statusIndexKeys = Builders<LoopJoinRequest>.IndexKeys.Ascending(r => r.Status);
            var statusIndexModel = new CreateIndexModel<LoopJoinRequest>(statusIndexKeys);

            // Create compound index on loopId + status
            var loopStatusIndexKeys = Builders<LoopJoinRequest>.IndexKeys
                .Ascending(r => r.LoopId)
                .Ascending(r => r.Status);
            var loopStatusIndexModel = new CreateIndexModel<LoopJoinRequest>(loopStatusIndexKeys);

            // Create compound index on userId + status
            var userStatusIndexKeys = Builders<LoopJoinRequest>.IndexKeys
                .Ascending(r => r.UserId)
                .Ascending(r => r.Status);
            var userStatusIndexModel = new CreateIndexModel<LoopJoinRequest>(userStatusIndexKeys);

            await _joinRequestsCollection.Indexes.CreateManyAsync(new[]
            {
                loopIdIndexModel,
                userIdIndexModel,
                statusIndexModel,
                loopStatusIndexModel,
                userStatusIndexModel
            });

            _logger.LogInformation("Indexes created successfully for LoopJoinRequests collection");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not create indexes for LoopJoinRequests collection");
        }
    }
}
