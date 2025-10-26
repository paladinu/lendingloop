using Api.Models;
using MongoDB.Driver;

namespace Api.Services;

public class ItemsService : IItemsService
{
    private readonly IMongoCollection<SharedItem> _itemsCollection;

    public ItemsService(IMongoDatabase database, IConfiguration configuration)
    {
        var collectionName = configuration["MongoDB:CollectionName"] ?? "items";
        _itemsCollection = database.GetCollection<SharedItem>(collectionName);
    }

    public async Task<List<SharedItem>> GetAllItemsAsync()
    {
        return await _itemsCollection.Find(_ => true).ToListAsync();
    }

    public async Task<SharedItem> CreateItemAsync(SharedItem item)
    {
        await _itemsCollection.InsertOneAsync(item);
        return item;
    }
}