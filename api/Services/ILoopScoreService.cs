using Api.Models;

namespace Api.Services;

public interface ILoopScoreService
{
    Task AwardBorrowPointsAsync(string userId, string itemRequestId, string itemName);
    Task AwardOnTimeReturnPointsAsync(string userId, string itemRequestId, string itemName);
    Task AwardLendPointsAsync(string userId, string itemRequestId, string itemName);
    Task ReverseLendPointsAsync(string userId, string itemRequestId, string itemName);
    Task<int> GetUserScoreAsync(string userId);
    Task<List<ScoreHistoryEntry>> GetScoreHistoryAsync(string userId, int limit = 50);
    Task<List<BadgeAward>> GetUserBadgesAsync(string userId);
    Task<int> GetOnTimeReturnCountAsync(string userId);
    Task<int> GetCompletedLendingTransactionCountAsync(string userId);
    Task<int> GetActiveInvitedUsersCountAsync(string userId);
    Task RecordCompletedLendingTransactionAsync(string userId, string itemRequestId, string itemName);
    Task ResetConsecutiveOnTimeReturnsAsync(string userId);
    Task AwardAchievementBadgeAsync(string userId, BadgeType badgeType);
    Task<BadgeProgress> GetBadgeProgressAsync(string userId, BadgeType badgeType);
    Task<Dictionary<BadgeType, BadgeProgress>> GetAllBadgeProgressAsync(string userId);
}
