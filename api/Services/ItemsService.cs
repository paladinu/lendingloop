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
        
        // Ensure indexes are created when service is instantiated
        _ = Task.Run(EnsureIndexesAsync);
    }

    public async Task<List<SharedItem>> GetAllItemsAsync()
    {
        return await _itemsCollection.Find(_ => true).ToListAsync();
    }

    public async Task<List<SharedItem>> GetItemsByUserIdAsync(string userId)
    {
        var filter = Builders<SharedItem>.Filter.Eq(item => item.UserId, userId);
        return await _itemsCollection.Find(filter).ToListAsync();
    }

    public async Task<SharedItem> CreateItemAsync(SharedItem item)
    {
        await _itemsCollection.InsertOneAsync(item);
        return item;
    }

    public async Task<SharedItem?> UpdateItemImageAsync(string id, string imageUrl)
    {
        var filter = Builders<SharedItem>.Filter.Eq(item => item.Id, id);
        var update = Builders<SharedItem>.Update.Set(item => item.ImageUrl, imageUrl);
        
        var options = new FindOneAndUpdateOptions<SharedItem>
        {
            ReturnDocument = ReturnDocument.After
        };

        return await _itemsCollection.FindOneAndUpdateAsync(filter, update, options);
    }

    public async Task<SharedItem?> UpdateItemImageAsync(string id, string imageUrl, string userId)
    {
        var filter = Builders<SharedItem>.Filter.And(
            Builders<SharedItem>.Filter.Eq(item => item.Id, id),
            Builders<SharedItem>.Filter.Eq(item => item.UserId, userId)
        );
        var update = Builders<SharedItem>.Update.Set(item => item.ImageUrl, imageUrl);
        
        var options = new FindOneAndUpdateOptions<SharedItem>
        {
            ReturnDocument = ReturnDocument.After
        };

        return await _itemsCollection.FindOneAndUpdateAsync(filter, update, options);
    }

    private async Task EnsureIndexesAsync()
    {
        try
        {
            // Create index on userId field for faster user-specific queries
            var userIdIndexKeys = Builders<SharedItem>.IndexKeys.Ascending(item => item.UserId);
            var userIdIndexModel = new CreateIndexModel<SharedItem>(userIdIndexKeys);

            await _itemsCollection.Indexes.CreateOneAsync(userIdIndexModel);
        }
        catch (Exception ex)
        {
            // Log the exception but don't fail the application startup
            Console.WriteLine($"Warning: Could not create indexes for Items collection: {ex.Message}");
        }
    }
}