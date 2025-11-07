using Api.Models;

namespace Api.Services;

public interface ILoopInvitationService
{
    Task<LoopInvitation> CreateEmailInvitationAsync(string loopId, string invitedByUserId, string email);
    Task<LoopInvitation> CreateUserInvitationAsync(string loopId, string invitedByUserId, string invitedUserId);
    Task<LoopInvitation?> AcceptInvitationAsync(string token, string? currentUserId = null);
    Task<LoopInvitation?> AcceptInvitationByUserAsync(string invitationId, string userId);
    Task<List<LoopInvitation>> GetPendingInvitationsForUserAsync(string userId);
    Task<List<LoopInvitation>> GetPendingInvitationsForLoopAsync(string loopId);
    Task ExpireOldInvitationsAsync();
    Task DeleteInvitationsForLoopAsync(string loopId);
}
