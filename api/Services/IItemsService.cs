using Api.Models;

namespace Api.Services;

public interface IItemsService
{
    Task<List<SharedItem>> GetAllItemsAsync();
    Task<List<SharedItem>> GetItemsByUserIdAsync(string userId);
    Task<SharedItem> CreateItemAsync(SharedItem item);
    Task<SharedItem?> UpdateItemImageAsync(string id, string imageUrl);
    Task<SharedItem?> UpdateItemImageAsync(string id, string imageUrl, string userId);
    Task<List<SharedItem>> GetItemsByLoopIdAsync(string loopId);
    Task<SharedItem?> UpdateItemVisibilityAsync(string itemId, string userId, List<string> loopIds, bool visibleToAllLoops, bool visibleToFutureLoops);
    Task<SharedItem?> GetItemByIdAsync(string itemId);
    Task<SharedItem?> UpdateItemAvailabilityAsync(string itemId, bool isAvailable);
    Task<SharedItem?> UpdateItemAsync(string itemId, string userId, string name, string description, bool isAvailable, List<string> visibleToLoopIds, bool visibleToAllLoops, bool visibleToFutureLoops);
    Task RemoveLoopFromAllItemsAsync(string loopId);
    Task RemoveLoopFromUserItemsAsync(string userId, string loopId);
}