using Api.Models;
using MongoDB.Driver;

namespace Api.Services;

public class ItemsService : IItemsService
{
    private readonly IMongoCollection<SharedItem> _itemsCollection;
    private readonly IMongoCollection<User> _usersCollection;
    private readonly IConfiguration _configuration;
    private readonly ITagsService _tagsService;

    public ItemsService(IMongoDatabase database, IConfiguration configuration, ITagsService tagsService)
    {
        var collectionName = configuration["MongoDB:CollectionName"] ?? "items";
        _itemsCollection = database.GetCollection<SharedItem>(collectionName);
        
        var usersCollectionName = configuration["MongoDB:UsersCollectionName"] ?? "users";
        _usersCollection = database.GetCollection<User>(usersCollectionName);
        
        _configuration = configuration;
        _tagsService = tagsService;
        
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
    
    private async Task PopulateOwnerInformationAsync(List<SharedItem> items)
    {
        if (items == null || items.Count == 0) return;
        
        // Get unique user IDs
        var userIds = items.Select(i => i.UserId).Distinct().ToList();
        
        // Fetch all users in one query
        var userFilter = Builders<User>.Filter.In(u => u.Id, userIds);
        var users = await _usersCollection.Find(userFilter).ToListAsync();
        
        // Create a dictionary for quick lookup
        var userDict = users.ToDictionary(u => u.Id!, u => u);
        
        // Populate owner information for each item
        foreach (var item in items)
        {
            if (userDict.TryGetValue(item.UserId, out var user))
            {
                item.OwnerName = $"{user.FirstName} {user.LastName}".Trim();
                item.OwnerScore = user.LoopScore;
            }
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
        await PopulateOwnerInformationAsync(items);
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
        bool visibleToFutureLoops,
        List<string>? tags = null)
    {
        // Get the existing item to compare tags
        var existingItem = await GetItemByIdAsync(itemId);
        if (existingItem == null || existingItem.UserId != userId)
        {
            return null;
        }

        var filter = Builders<SharedItem>.Filter.And(
            Builders<SharedItem>.Filter.Eq(item => item.Id, itemId),
            Builders<SharedItem>.Filter.Eq(item => item.UserId, userId)
        );

        var updateBuilder = Builders<SharedItem>.Update
            .Set(item => item.Name, name)
            .Set(item => item.Description, description)
            .Set(item => item.IsAvailable, isAvailable)
            .Set(item => item.VisibleToLoopIds, visibleToLoopIds)
            .Set(item => item.VisibleToAllLoops, visibleToAllLoops)
            .Set(item => item.VisibleToFutureLoops, visibleToFutureLoops)
            .Set(item => item.UpdatedAt, DateTime.UtcNow);

        // Handle tags if provided
        if (tags != null)
        {
            updateBuilder = updateBuilder.Set(item => item.Tags, tags);

            // Update tag usage counts
            var oldTags = existingItem.Tags ?? new List<string>();
            var newTags = tags;

            // Decrement usage for removed tags
            var removedTags = oldTags.Except(newTags).ToList();
            foreach (var tag in removedTags)
            {
                await _tagsService.DecrementTagUsageAsync(tag);
            }

            // Increment usage for added tags
            var addedTags = newTags.Except(oldTags).ToList();
            foreach (var tag in addedTags)
            {
                await _tagsService.IncrementTagUsageAsync(tag);
            }
        }

        var options = new FindOneAndUpdateOptions<SharedItem>
        {
            ReturnDocument = ReturnDocument.After
        };

        var item = await _itemsCollection.FindOneAndUpdateAsync(filter, updateBuilder, options);
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

    public async Task<Api.DTOs.ItemSearchResult> SearchItemsAsync(string loopId, Api.DTOs.ItemSearchFilter filter)
    {
        var filterBuilder = Builders<SharedItem>.Filter;
        var filters = new List<FilterDefinition<SharedItem>>();

        // Filter by loop visibility (items must be visible in this loop)
        var loopFilter = filterBuilder.Or(
            filterBuilder.Eq(item => item.VisibleToAllLoops, true),
            filterBuilder.AnyEq(item => item.VisibleToLoopIds, loopId)
        );
        filters.Add(loopFilter);

        // Text search on name and description
        if (!string.IsNullOrWhiteSpace(filter.SearchText))
        {
            var searchFilter = filterBuilder.Or(
                filterBuilder.Regex(item => item.Name, new MongoDB.Bson.BsonRegularExpression(filter.SearchText, "i")),
                filterBuilder.Regex(item => item.Description, new MongoDB.Bson.BsonRegularExpression(filter.SearchText, "i"))
            );
            filters.Add(searchFilter);
        }

        // Tag filtering (AND logic - item must have all selected tags)
        if (filter.Tags != null && filter.Tags.Count > 0)
        {
            var tagFilter = filterBuilder.All(item => item.Tags, filter.Tags);
            filters.Add(tagFilter);
        }

        // Availability filtering
        if (filter.IsAvailable.HasValue)
        {
            filters.Add(filterBuilder.Eq(item => item.IsAvailable, filter.IsAvailable.Value));
        }

        // Owner filtering
        if (filter.OwnerIds != null && filter.OwnerIds.Count > 0)
        {
            filters.Add(filterBuilder.In(item => item.UserId, filter.OwnerIds));
        }

        // Combine all filters with AND logic
        var combinedFilter = filterBuilder.And(filters);

        // Get total count for pagination
        var totalCount = await _itemsCollection.CountDocumentsAsync(combinedFilter);

        // Calculate pagination
        var pageNumber = filter.PageNumber < 1 ? 1 : filter.PageNumber;
        var pageSize = filter.PageSize < 1 ? 50 : (filter.PageSize > 100 ? 100 : filter.PageSize);
        var skip = (pageNumber - 1) * pageSize;
        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

        // Get paginated results sorted by creation date
        var sort = Builders<SharedItem>.Sort.Descending(item => item.CreatedAt);
        var items = await _itemsCollection
            .Find(combinedFilter)
            .Sort(sort)
            .Skip(skip)
            .Limit(pageSize)
            .ToListAsync();

        NormalizeImageUrls(items);
        await PopulateOwnerInformationAsync(items);

        return new Api.DTOs.ItemSearchResult
        {
            Items = items,
            TotalCount = (int)totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalPages = totalPages
        };
    }

    public async Task<List<string>> GetDistinctOwnersInLoopAsync(string loopId)
    {
        var filter = Builders<SharedItem>.Filter.Or(
            Builders<SharedItem>.Filter.Eq(item => item.VisibleToAllLoops, true),
            Builders<SharedItem>.Filter.AnyEq(item => item.VisibleToLoopIds, loopId)
        );

        var fieldDefinition = new ExpressionFieldDefinition<SharedItem, string>(item => item.UserId);
        var ownerIds = await _itemsCollection
            .DistinctAsync(fieldDefinition, filter);

        return await ownerIds.ToListAsync();
    }

    private async Task EnsureIndexesAsync()
    {
        try
        {
            // Skip index creation if collection is not initialized (e.g., in test scenarios)
            if (_itemsCollection == null || _itemsCollection.Database == null)
            {
                return;
            }

            // Verify database connection before creating indexes
            try
            {
                await _itemsCollection.Database.ListCollectionNamesAsync();
            }
            catch
            {
                // Database not accessible, skip index creation
                return;
            }

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

            // Create index on tags for tag filtering
            var tagsIndexKeys = Builders<SharedItem>.IndexKeys.Ascending(item => item.Tags);
            var tagsIndexModel = new CreateIndexModel<SharedItem>(tagsIndexKeys);

            // Create compound index for search optimization (tags, isAvailable, userId)
            var searchCompoundIndexKeys = Builders<SharedItem>.IndexKeys
                .Ascending(item => item.Tags)
                .Ascending(item => item.IsAvailable)
                .Ascending(item => item.UserId);
            var searchCompoundIndexModel = new CreateIndexModel<SharedItem>(searchCompoundIndexKeys);

            await _itemsCollection.Indexes.CreateManyAsync(new[]
            {
                userIdIndexModel,
                loopIdsIndexModel,
                compoundIndexModel,
                allLoopsIndexModel,
                createdAtIndexModel,
                tagsIndexModel,
                searchCompoundIndexModel
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