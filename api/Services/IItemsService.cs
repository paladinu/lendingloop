using Api.Models;

namespace Api.Services;

public interface IItemsService
{
    Task<List<SharedItem>> GetAllItemsAsync();
    Task<List<SharedItem>> GetItemsByUserIdAsync(string userId);
    Task<SharedItem> CreateItemAsync(SharedItem item);
    Task<SharedItem?> UpdateItemImageAsync(string id, string imageUrl);
    Task<SharedItem?> UpdateItemImageAsync(string id, string imageUrl, string userId);
}