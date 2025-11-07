using Api.Models;

namespace Api.Services;

public interface ILoopService
{
    Task<Loop> CreateLoopAsync(string name, string creatorId);
    Task<Loop?> GetLoopByIdAsync(string loopId);
    Task<List<Loop>> GetUserLoopsAsync(string userId);
    Task<List<User>> GetLoopMembersAsync(string loopId);
    Task<bool> IsUserLoopMemberAsync(string loopId, string userId);
    Task<Loop?> AddMemberToLoopAsync(string loopId, string userId);
    Task<Loop?> RemoveMemberFromLoopAsync(string loopId, string userId);
    Task<List<User>> GetPotentialInviteesFromOtherLoopsAsync(string userId, string currentLoopId);
    
    // Loop settings management
    Task<Loop?> UpdateLoopSettingsAsync(string loopId, string name, string description, bool isPublic);
    Task<bool> IsLoopOwnerAsync(string loopId, string userId);
    
    // Loop archival
    Task<Loop?> ArchiveLoopAsync(string loopId);
    Task<Loop?> RestoreLoopAsync(string loopId);
    Task<List<Loop>> GetArchivedLoopsAsync(string userId);
    
    // Loop deletion
    Task<bool> DeleteLoopAsync(string loopId);
    
    // Ownership transfer
    Task<Loop?> InitiateOwnershipTransferAsync(string loopId, string fromUserId, string toUserId);
    Task<Loop?> AcceptOwnershipTransferAsync(string loopId, string userId);
    Task<Loop?> DeclineOwnershipTransferAsync(string loopId, string userId);
    Task<Loop?> CancelOwnershipTransferAsync(string loopId, string userId);
    Task<OwnershipTransfer?> GetPendingOwnershipTransferAsync(string loopId);
    
    // Public loop discovery
    Task<List<Loop>> GetPublicLoopsAsync(int skip = 0, int limit = 20);
    Task<List<Loop>> SearchPublicLoopsAsync(string searchTerm, int skip = 0, int limit = 20);
    
    // Member management
    Task<Loop?> LeaveLoopAsync(string loopId, string userId);
}
