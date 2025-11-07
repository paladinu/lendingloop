using Api.Models;

namespace Api.Services;

public interface ILoopJoinRequestService
{
    Task<LoopJoinRequest> CreateJoinRequestAsync(string loopId, string userId, string message);
    Task<LoopJoinRequest?> GetJoinRequestByIdAsync(string requestId);
    Task<List<LoopJoinRequest>> GetPendingJoinRequestsForLoopAsync(string loopId);
    Task<List<LoopJoinRequest>> GetUserJoinRequestsAsync(string userId);
    Task<LoopJoinRequest?> ApproveJoinRequestAsync(string requestId, string ownerId);
    Task<LoopJoinRequest?> RejectJoinRequestAsync(string requestId, string ownerId);
    Task<bool> HasPendingJoinRequestAsync(string loopId, string userId);
    Task DeleteJoinRequestsForLoopAsync(string loopId);
}
