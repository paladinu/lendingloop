using Api.Models;

namespace Api.Services;

public interface IItemsService
{
    Task<List<SharedItem>> GetAllItemsAsync();
    Task<SharedItem> CreateItemAsync(SharedItem item);
}