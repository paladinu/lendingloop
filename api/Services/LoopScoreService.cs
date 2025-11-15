using Api.Models;
using MongoDB.Driver;

namespace Api.Services;

public class LoopScoreService : ILoopScoreService
{
    private readonly IMongoCollection<User> _usersCollection;
    private readonly ILogger<LoopScoreService> _logger;

    public LoopScoreService(
        IMongoDatabase database,
        IConfiguration configuration,
        ILogger<LoopScoreService> logger)
    {
        var collectionName = configuration["MongoDB:UsersCollectionName"] ?? "users";
        _usersCollection = database.GetCollection<User>(collectionName);
        _logger = logger;
    }

    public async Task AwardBorrowPointsAsync(string userId, string itemRequestId, string itemName)
    {
        await AwardPointsAsync(userId, itemRequestId, itemName, 1, ScoreActionType.BorrowCompleted);
    }

    public async Task AwardOnTimeReturnPointsAsync(string userId, string itemRequestId, string itemName)
    {
        await AwardPointsAsync(userId, itemRequestId, itemName, 1, ScoreActionType.OnTimeReturn);
    }

    public async Task AwardLendPointsAsync(string userId, string itemRequestId, string itemName)
    {
        await AwardPointsAsync(userId, itemRequestId, itemName, 4, ScoreActionType.LendApproved);
    }

    public async Task ReverseLendPointsAsync(string userId, string itemRequestId, string itemName)
    {
        await AwardPointsAsync(userId, itemRequestId, itemName, -4, ScoreActionType.LendCancelled);
    }

    public async Task<int> GetUserScoreAsync(string userId)
    {
        var user = await _usersCollection.Find(u => u.Id == userId).FirstOrDefaultAsync();
        return user?.LoopScore ?? 0;
    }

    public async Task<List<ScoreHistoryEntry>> GetScoreHistoryAsync(string userId, int limit = 50)
    {
        var user = await _usersCollection.Find(u => u.Id == userId).FirstOrDefaultAsync();
        
        if (user == null || user.ScoreHistory == null)
        {
            return new List<ScoreHistoryEntry>();
        }

        // Return most recent entries first, limited by the specified count
        return user.ScoreHistory
            .OrderByDescending(entry => entry.Timestamp)
            .Take(limit)
            .ToList();
    }

    private async Task AwardPointsAsync(string userId, string itemRequestId, string itemName, int points, ScoreActionType actionType)
    {
        try
        {
            var historyEntry = new ScoreHistoryEntry
            {
                Timestamp = DateTime.UtcNow,
                Points = points,
                ActionType = actionType,
                ItemRequestId = itemRequestId,
                ItemName = itemName
            };

            var filter = Builders<User>.Filter.Eq(u => u.Id, userId);
            
            // Use atomic operations to prevent race conditions
            // $inc increments the score, $push adds to history array, $max ensures score never goes below 0
            var update = Builders<User>.Update
                .Inc(u => u.LoopScore, points)
                .Push(u => u.ScoreHistory, historyEntry);

            var result = await _usersCollection.UpdateOneAsync(filter, update);

            if (result.ModifiedCount == 0)
            {
                _logger.LogWarning("Failed to award {Points} points to user {UserId}. User may not exist.", points, userId);
                return;
            }

            // Ensure score never goes below 0 with a separate operation
            var ensureMinimumFilter = Builders<User>.Filter.And(
                Builders<User>.Filter.Eq(u => u.Id, userId),
                Builders<User>.Filter.Lt(u => u.LoopScore, 0)
            );
            
            var ensureMinimumUpdate = Builders<User>.Update.Set(u => u.LoopScore, 0);
            await _usersCollection.UpdateOneAsync(ensureMinimumFilter, ensureMinimumUpdate);

            _logger.LogInformation("Awarded {Points} points to user {UserId} for {ActionType}", points, userId, actionType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error awarding {Points} points to user {UserId} for {ActionType}", points, userId, actionType);
            throw;
        }
    }
}
