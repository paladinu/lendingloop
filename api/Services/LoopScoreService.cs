using Api.Models;
using MongoDB.Driver;

namespace Api.Services;

public class LoopScoreService : ILoopScoreService
{
    private readonly IMongoCollection<User> _usersCollection;
    private readonly ILogger<LoopScoreService> _logger;
    private readonly IEmailService _emailService;

    public LoopScoreService(
        IMongoDatabase database,
        IConfiguration configuration,
        ILogger<LoopScoreService> logger,
        IEmailService emailService)
    {
        var collectionName = configuration["MongoDB:UsersCollectionName"] ?? "users";
        _usersCollection = database.GetCollection<User>(collectionName);
        _logger = logger;
        _emailService = emailService;
    }

    public async Task AwardBorrowPointsAsync(string userId, string itemRequestId, string itemName)
    {
        await AwardPointsAsync(userId, itemRequestId, itemName, 1, ScoreActionType.BorrowCompleted);
    }

    public async Task AwardOnTimeReturnPointsAsync(string userId, string itemRequestId, string itemName)
    {
        await AwardPointsAsync(userId, itemRequestId, itemName, 1, ScoreActionType.OnTimeReturn);
        
        // Increment consecutive on-time returns
        var filter = Builders<User>.Filter.Eq(u => u.Id, userId);
        var update = Builders<User>.Update.Inc(u => u.ConsecutiveOnTimeReturns, 1);
        
        var options = new FindOneAndUpdateOptions<User>
        {
            ReturnDocument = ReturnDocument.After
        };
        
        var userAfterUpdate = await _usersCollection.FindOneAndUpdateAsync(filter, update, options);
        
        if (userAfterUpdate != null)
        {
            _logger.LogInformation("User {UserId} now has {Count} consecutive on-time returns", userId, userAfterUpdate.ConsecutiveOnTimeReturns);
            
            // Check if user has reached 25 consecutive on-time returns for PerfectRecord badge
            if (userAfterUpdate.ConsecutiveOnTimeReturns >= 25)
            {
                await CheckAndAwardAchievementBadgeAsync(userId, BadgeType.PerfectRecord);
            }
        }
        
        // Check if user has reached 10 on-time returns for ReliableBorrower badge
        var onTimeReturnCount = await GetOnTimeReturnCountAsync(userId);
        if (onTimeReturnCount >= 10)
        {
            await CheckAndAwardAchievementBadgeAsync(userId, BadgeType.ReliableBorrower);
        }
    }

    public async Task AwardLendPointsAsync(string userId, string itemRequestId, string itemName)
    {
        await AwardPointsAsync(userId, itemRequestId, itemName, 4, ScoreActionType.LendApproved);
        
        // Check and award FirstLend achievement badge
        await CheckAndAwardAchievementBadgeAsync(userId, BadgeType.FirstLend);
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

    public async Task<List<BadgeAward>> GetUserBadgesAsync(string userId)
    {
        var user = await _usersCollection.Find(u => u.Id == userId).FirstOrDefaultAsync();
        
        if (user == null || user.Badges == null)
        {
            return new List<BadgeAward>();
        }

        return user.Badges.OrderBy(b => b.AwardedAt).ToList();
    }

    public async Task<int> GetOnTimeReturnCountAsync(string userId)
    {
        var user = await _usersCollection.Find(u => u.Id == userId).FirstOrDefaultAsync();
        
        if (user == null || user.ScoreHistory == null)
        {
            return 0;
        }

        return user.ScoreHistory.Count(entry => entry.ActionType == ScoreActionType.OnTimeReturn);
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
            // $inc increments the score, $push adds to history array
            var update = Builders<User>.Update
                .Inc(u => u.LoopScore, points)
                .Push(u => u.ScoreHistory, historyEntry)
                .SetOnInsert(u => u.Badges, new List<BadgeAward>()); // Ensure badges field exists

            // Use FindOneAndUpdate to get the updated document atomically
            var options = new FindOneAndUpdateOptions<User>
            {
                ReturnDocument = ReturnDocument.After
            };

            var userAfterUpdate = await _usersCollection.FindOneAndUpdateAsync(filter, update, options);

            if (userAfterUpdate == null)
            {
                _logger.LogWarning("Failed to award {Points} points to user {UserId}. User may not exist.", points, userId);
                return;
            }

            // Ensure score never goes below 0 with a separate operation if needed
            if (userAfterUpdate.LoopScore < 0)
            {
                var ensureMinimumFilter = Builders<User>.Filter.Eq(u => u.Id, userId);
                var ensureMinimumUpdate = Builders<User>.Update.Set(u => u.LoopScore, 0);
                userAfterUpdate = await _usersCollection.FindOneAndUpdateAsync(
                    ensureMinimumFilter, 
                    ensureMinimumUpdate, 
                    new FindOneAndUpdateOptions<User> { ReturnDocument = ReturnDocument.After });
            }

            _logger.LogInformation("Awarded {Points} points to user {UserId} for {ActionType}. New score: {NewScore}", 
                points, userId, actionType, userAfterUpdate.LoopScore);

            // Check and award badges with the updated user object
            await CheckAndAwardBadgesAsync(userAfterUpdate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error awarding {Points} points to user {UserId} for {ActionType}", points, userId, actionType);
            throw;
        }
    }

    private async Task CheckAndAwardBadgesAsync(User user)
    {
        try
        {
            if (user == null)
            {
                _logger.LogWarning("User is null when checking for badge awards");
                return;
            }

            var currentScore = user.LoopScore;
            var existingBadges = user.Badges?.Select(b => b.BadgeType).ToHashSet() ?? new HashSet<BadgeType>();
            var newBadges = new List<BadgeAward>();

            _logger.LogInformation("Checking badges for user {UserId} with score {Score}. Existing badges: {ExistingBadges}", 
                user.Id, currentScore, string.Join(", ", existingBadges));

            // Check for Bronze badge (10 points)
            if (currentScore >= 10 && !existingBadges.Contains(BadgeType.Bronze))
            {
                _logger.LogInformation("User {UserId} qualifies for Bronze badge", user.Id);
                newBadges.Add(new BadgeAward
                {
                    BadgeType = BadgeType.Bronze,
                    AwardedAt = DateTime.UtcNow
                });
            }

            // Check for Silver badge (50 points)
            if (currentScore >= 50 && !existingBadges.Contains(BadgeType.Silver))
            {
                _logger.LogInformation("User {UserId} qualifies for Silver badge", user.Id);
                newBadges.Add(new BadgeAward
                {
                    BadgeType = BadgeType.Silver,
                    AwardedAt = DateTime.UtcNow
                });
            }

            // Check for Gold badge (100 points)
            if (currentScore >= 100 && !existingBadges.Contains(BadgeType.Gold))
            {
                _logger.LogInformation("User {UserId} qualifies for Gold badge", user.Id);
                newBadges.Add(new BadgeAward
                {
                    BadgeType = BadgeType.Gold,
                    AwardedAt = DateTime.UtcNow
                });
            }

            _logger.LogInformation("User {UserId} will receive {Count} new badges", user.Id, newBadges.Count);

            // Award new badges if any
            if (newBadges.Any())
            {
                var filter = Builders<User>.Filter.Eq(u => u.Id, user.Id);
                var update = Builders<User>.Update.PushEach(u => u.Badges, newBadges);
                
                await _usersCollection.UpdateOneAsync(filter, update);

                // Send email notification for each new badge
                foreach (var badge in newBadges)
                {
                    _logger.LogInformation("Awarded {BadgeType} badge to user {UserId}", badge.BadgeType, user.Id);
                    
                    try
                    {
                        await _emailService.SendBadgeAwardEmailAsync(
                            user.Email,
                            $"{user.FirstName} {user.LastName}".Trim(),
                            badge.BadgeType.ToString(),
                            currentScore
                        );
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send badge award email to user {UserId} for {BadgeType} badge", user.Id, badge.BadgeType);
                        // Don't throw - badge was awarded successfully, email is secondary
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking and awarding badges for user {UserId}", user?.Id);
            // Don't throw - this is a secondary operation
        }
    }

    private async Task CheckAndAwardAchievementBadgeAsync(string userId, BadgeType badgeType)
    {
        try
        {
            var user = await _usersCollection.Find(u => u.Id == userId).FirstOrDefaultAsync();
            
            if (user == null)
            {
                _logger.LogWarning("User {UserId} not found when checking for {BadgeType} badge", userId, badgeType);
                return;
            }

            var existingBadges = user.Badges?.Select(b => b.BadgeType).ToHashSet() ?? new HashSet<BadgeType>();
            
            // Check if badge already awarded
            if (existingBadges.Contains(badgeType))
            {
                _logger.LogInformation("User {UserId} already has {BadgeType} badge", userId, badgeType);
                return;
            }

            var newBadge = new BadgeAward
            {
                BadgeType = badgeType,
                AwardedAt = DateTime.UtcNow
            };

            // Use $addToSet to prevent duplicate awards atomically
            var filter = Builders<User>.Filter.Eq(u => u.Id, userId);
            var update = Builders<User>.Update.AddToSet(u => u.Badges, newBadge);
            
            await _usersCollection.UpdateOneAsync(filter, update);

            _logger.LogInformation("Awarded {BadgeType} achievement badge to user {UserId}", badgeType, userId);

            // Send email notification
            try
            {
                await _emailService.SendBadgeAwardEmailAsync(
                    user.Email,
                    $"{user.FirstName} {user.LastName}".Trim(),
                    badgeType.ToString(),
                    user.LoopScore
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send badge award email to user {UserId} for {BadgeType} badge", userId, badgeType);
                // Don't throw - badge was awarded successfully, email is secondary
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error awarding {BadgeType} achievement badge to user {UserId}", badgeType, userId);
            // Don't throw - this is a secondary operation
        }
    }

    public async Task<int> GetCompletedLendingTransactionCountAsync(string userId)
    {
        try
        {
            var user = await _usersCollection.Find(u => u.Id == userId).FirstOrDefaultAsync();
            
            if (user == null)
            {
                _logger.LogWarning("User {UserId} not found when getting completed lending transaction count", userId);
                return 0;
            }

            // Count LendApproved entries in score history (these represent completed lending transactions)
            var completedLendingCount = user.ScoreHistory?
                .Count(entry => entry.ActionType == ScoreActionType.LendApproved) ?? 0;

            return completedLendingCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting completed lending transaction count for user {UserId}", userId);
            return 0;
        }
    }

    public async Task<int> GetActiveInvitedUsersCountAsync(string userId)
    {
        try
        {
            // Find all users invited by this user who have at least one score history entry
            var filter = Builders<User>.Filter.And(
                Builders<User>.Filter.Eq(u => u.InvitedBy, userId),
                Builders<User>.Filter.SizeGt(u => u.ScoreHistory, 0)
            );

            var activeInvitedUsersCount = await _usersCollection.CountDocumentsAsync(filter);

            return (int)activeInvitedUsersCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active invited users count for user {UserId}", userId);
            return 0;
        }
    }

    public async Task RecordCompletedLendingTransactionAsync(string userId, string itemRequestId, string itemName)
    {
        try
        {
            _logger.LogInformation("Recording completed lending transaction for user {UserId}, request {RequestId}", userId, itemRequestId);

            // Get current completed lending transaction count
            var completedCount = await GetCompletedLendingTransactionCountAsync(userId);
            
            _logger.LogInformation("User {UserId} has completed {Count} lending transactions", userId, completedCount);

            // Check if user qualifies for GenerousLender badge (50 completed lending transactions)
            if (completedCount >= 50)
            {
                _logger.LogInformation("User {UserId} qualifies for GenerousLender badge with {Count} completed lending transactions", userId, completedCount);
                await CheckAndAwardAchievementBadgeAsync(userId, BadgeType.GenerousLender);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording completed lending transaction for user {UserId}", userId);
            // Don't throw - this is a secondary operation
        }
    }

    public async Task ResetConsecutiveOnTimeReturnsAsync(string userId)
    {
        try
        {
            _logger.LogInformation("Resetting consecutive on-time returns for user {UserId} due to late return", userId);

            var filter = Builders<User>.Filter.Eq(u => u.Id, userId);
            var update = Builders<User>.Update.Set(u => u.ConsecutiveOnTimeReturns, 0);
            
            await _usersCollection.UpdateOneAsync(filter, update);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting consecutive on-time returns for user {UserId}", userId);
            // Don't throw - this is a secondary operation
        }
    }

    public async Task AwardAchievementBadgeAsync(string userId, BadgeType badgeType)
    {
        await CheckAndAwardAchievementBadgeAsync(userId, badgeType);
    }
}
