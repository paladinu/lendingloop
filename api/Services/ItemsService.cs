using Api.Models;
using MongoDB.Driver;

namespace Api.Services;

public class ItemsService : IItemsService
{
    private readonly IMongoCollection<SharedItem> _itemsCollection;
    private readonly IConfiguration _configuration;

    public ItemsService(IMongoDatabase database, IConfiguration configuration)
    {
        var collectionName = configuration["MongoDB:CollectionName"] ?? "items";
        _itemsCollection = database.GetCollection<SharedItem>(collectionName);
        _configuration = configuration;
        
        // Ensure indexes are created when service is instantiated
        _ = Task.Run(EnsureIndexesAsync);
    }

    private void NormalizeImageUrl(SharedItem item)
    {
        if (!string.IsNullOrEmpty(item.ImageUrl) && item.ImageUrl.StartsWith("/"))
        {
            // Convert relative URL to absolute URL
            var baseUrl = _configuration["FileStorage:BaseUrl"] ?? "https://local-api.lendingloop.com";
            item.ImageUrl = $"{baseUrl}{item.ImageUrl}";
        }
    }

    private void NormalizeImageUrls(List<SharedItem> items)
    {
        foreach (var item in items)
        {
            NormalizeImageUrl(item);
        }
    }

    public async Task<List<SharedItem>> GetAllItemsAsync()
    {
        var items = await _itemsCollection.Find(_ => true).ToListAsync();
        NormalizeImageUrls(items);
        return items;
    }

    public async Task<List<SharedItem>> GetItemsByUserIdAsync(string userId)
    {
        var filter = Builders<SharedItem>.Filter.Eq(item => item.UserId, userId);
        var items = await _itemsCollection.Find(filter).ToListAsync();
        NormalizeImageUrls(items);
        return items;
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
        var item = await _itemsCollection.Find(item => item.Id == itemId).FirstOrDefaultAsync();
        if (item != null)
        {
            NormalizeImageUrl(item);
        }
        return item;
    }

    public async Task<List<SharedItem>> GetItemsByLoopIdAsync(string loopId)
    {
        var filter = Builders<SharedItem>.Filter.Or(
            Builders<SharedItem>.Filter.Eq(item => item.VisibleToAllLoops, true),
            Builders<SharedItem>.Filter.AnyEq(item => item.VisibleToLoopIds, loopId)
        );

        var sort = Builders<SharedItem>.Sort.Descending(item => item.CreatedAt);
        var items = await _itemsCollection.Find(filter).Sort(sort).ToListAsync();
        NormalizeImageUrls(items);
        return items;
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

        var item = await _itemsCollection.FindOneAndUpdateAsync(filter, update, options);
        if (item != null)
        {
            NormalizeImageUrl(item);
        }
        return item;
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

        var item = await _itemsCollection.FindOneAndUpdateAsync(filter, update, options);
        if (item != null)
        {
            NormalizeImageUrl(item);
        }
        return item;
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

        var item = await _itemsCollection.FindOneAndUpdateAsync(filter, update, options);
        if (item != null)
        {
            NormalizeImageUrl(item);
        }
        return item;
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

        var item = await _itemsCollection.FindOneAndUpdateAsync(filter, update, options);
        if (item != null)
        {
            NormalizeImageUrl(item);
        }
        return item;
    }

    public async Task RemoveLoopFromAllItemsAsync(string loopId)
    {
        var filter = Builders<SharedItem>.Filter.AnyEq(item => item.VisibleToLoopIds, loopId);
        var update = Builders<SharedItem>.Update
            .Pull(item => item.VisibleToLoopIds, loopId)
            .Set(item => item.UpdatedAt, DateTime.UtcNow);

        await _itemsCollection.UpdateManyAsync(filter, update);
    }

    public async Task RemoveLoopFromUserItemsAsync(string userId, string loopId)
    {
        var filter = Builders<SharedItem>.Filter.And(
            Builders<SharedItem>.Filter.Eq(item => item.UserId, userId),
            Builders<SharedItem>.Filter.AnyEq(item => item.VisibleToLoopIds, loopId)
        );
        var update = Builders<SharedItem>.Update
            .Pull(item => item.VisibleToLoopIds, loopId)
            .Set(item => item.UpdatedAt, DateTime.UtcNow);

        await _itemsCollection.UpdateManyAsync(filter, update);
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