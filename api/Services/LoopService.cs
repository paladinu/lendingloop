using Api.Models;
using MongoDB.Driver;

namespace Api.Services;

public class LoopService : ILoopService
{
    private readonly IMongoCollection<Loop> _loopsCollection;
    private readonly IMongoCollection<User> _usersCollection;
    private readonly ILogger<LoopService> _logger;

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
        var filter = Builders<Loop>.Filter.AnyEq(l => l.MemberIds, userId);
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

            // Create text index on name field for search functionality
            var nameIndexKeys = Builders<Loop>.IndexKeys.Text(l => l.Name);
            var nameIndexModel = new CreateIndexModel<Loop>(nameIndexKeys);

            await _loopsCollection.Indexes.CreateManyAsync(new[]
            {
                creatorIdIndexModel,
                memberIdsIndexModel,
                nameIndexModel
            });

            _logger.LogInformation("Indexes created successfully for Loops collection");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not create indexes for Loops collection");
        }
    }
}
