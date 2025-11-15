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
}
