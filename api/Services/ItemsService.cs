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
        item.CreatedAt = DateTime.UtcNow;
        item.UpdatedAt = DateTime.UtcNow;
        await _itemsCollection.InsertOneAsync(item);
        return item;
    }

    public async Task<SharedItem?> GetItemByIdAsync(string itemId)
    {
        return await _itemsCollection.Find(item => item.Id == itemId).FirstOrDefaultAsync();
    }

    public async Task<List<SharedItem>> GetItemsByLoopIdAsync(string loopId)
    {
        var filter = Builders<SharedItem>.Filter.Or(
            Builders<SharedItem>.Filter.Eq(item => item.VisibleToAllLoops, true),
            Builders<SharedItem>.Filter.AnyEq(item => item.VisibleToLoopIds, loopId)
        );

        var sort = Builders<SharedItem>.Sort.Descending(item => item.CreatedAt);
        return await _itemsCollection.Find(filter).Sort(sort).ToListAsync();
    }

    public async Task<SharedItem?> UpdateItemVisibilityAsync(string itemId, string userId, List<string> loopIds, bool visibleToAllLoops, bool visibleToFutureLoops)
    {
        var filter = Builders<SharedItem>.Filter.And(
            Builders<SharedItem>.Filter.Eq(item => item.Id, itemId),
            Builders<SharedItem>.Filter.Eq(item => item.UserId, userId)
        );

        var update = Builders<SharedItem>.Update
            .Set(item => item.VisibleToLoopIds, loopIds)
            .Set(item => item.VisibleToAllLoops, visibleToAllLoops)
            .Set(item => item.VisibleToFutureLoops, visibleToFutureLoops)
            .Set(item => item.UpdatedAt, DateTime.UtcNow);

        var options = new FindOneAndUpdateOptions<SharedItem>
        {
            ReturnDocument = ReturnDocument.After
        };

        return await _itemsCollection.FindOneAndUpdateAsync(filter, update, options);
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

    public async Task<SharedItem?> UpdateItemAvailabilityAsync(string itemId, bool isAvailable)
    {
        var filter = Builders<SharedItem>.Filter.Eq(item => item.Id, itemId);
        var update = Builders<SharedItem>.Update
            .Set(item => item.IsAvailable, isAvailable)
            .Set(item => item.UpdatedAt, DateTime.UtcNow);
        
        var options = new FindOneAndUpdateOptions<SharedItem>
        {
            ReturnDocument = ReturnDocument.After
        };

        return await _itemsCollection.FindOneAndUpdateAsync(filter, update, options);
    }

    public async Task<SharedItem?> UpdateItemAsync(
        string itemId,
        string userId,
        string name,
        string description,
        bool isAvailable,
        List<string> visibleToLoopIds,
        bool visibleToAllLoops,
        bool visibleToFutureLoops)
    {
        var filter = Builders<SharedItem>.Filter.And(
            Builders<SharedItem>.Filter.Eq(item => item.Id, itemId),
            Builders<SharedItem>.Filter.Eq(item => item.UserId, userId)
        );

        var update = Builders<SharedItem>.Update
            .Set(item => item.Name, name)
            .Set(item => item.Description, description)
            .Set(item => item.IsAvailable, isAvailable)
            .Set(item => item.VisibleToLoopIds, visibleToLoopIds)
            .Set(item => item.VisibleToAllLoops, visibleToAllLoops)
            .Set(item => item.VisibleToFutureLoops, visibleToFutureLoops)
            .Set(item => item.UpdatedAt, DateTime.UtcNow);

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

            // Create index on visibleToLoopIds for loop-based queries
            var loopIdsIndexKeys = Builders<SharedItem>.IndexKeys.Ascending(item => item.VisibleToLoopIds);
            var loopIdsIndexModel = new CreateIndexModel<SharedItem>(loopIdsIndexKeys);

            // Create compound index on userId + visibleToLoopIds
            var compoundIndexKeys = Builders<SharedItem>.IndexKeys
                .Ascending(item => item.UserId)
                .Ascending(item => item.VisibleToLoopIds);
            var compoundIndexModel = new CreateIndexModel<SharedItem>(compoundIndexKeys);

            // Create index on visibleToAllLoops
            var allLoopsIndexKeys = Builders<SharedItem>.IndexKeys.Ascending(item => item.VisibleToAllLoops);
            var allLoopsIndexModel = new CreateIndexModel<SharedItem>(allLoopsIndexKeys);

            // Create index on createdAt for sorting
            var createdAtIndexKeys = Builders<SharedItem>.IndexKeys.Descending(item => item.CreatedAt);
            var createdAtIndexModel = new CreateIndexModel<SharedItem>(createdAtIndexKeys);

            await _itemsCollection.Indexes.CreateManyAsync(new[]
            {
                userIdIndexModel,
                loopIdsIndexModel,
                compoundIndexModel,
                allLoopsIndexModel,
                createdAtIndexModel
            });

            Console.WriteLine("Indexes created successfully for Items collection");
        }
        catch (Exception ex)
        {
            // Log the exception but don't fail the application startup
            Console.WriteLine($"Warning: Could not create indexes for Items collection: {ex.Message}");
        }
    }
}