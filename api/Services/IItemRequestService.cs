using Api.Models;

namespace Api.Services;

public interface IItemRequestService
{
    Task<ItemRequest> CreateRequestAsync(string itemId, string requesterId, string? message = null, DateTime? expectedReturnDate = null);
    Task<List<ItemRequest>> GetRequestsByRequesterAsync(string requesterId);
    Task<List<ItemRequest>> GetPendingRequestsByOwnerAsync(string ownerId);
    Task<List<ItemRequest>> GetRequestsByItemIdAsync(string itemId);
    Task<ItemRequest?> GetRequestByIdAsync(string requestId);
    Task<ItemRequest?> ApproveRequestAsync(string requestId, string ownerId);
    Task<ItemRequest?> RejectRequestAsync(string requestId, string ownerId);
    Task<ItemRequest?> CancelRequestAsync(string requestId, string requesterId);
    Task<ItemRequest?> CompleteRequestAsync(string requestId, string ownerId);
    Task<ItemRequest?> GetActiveRequestForItemAsync(string itemId);
}
