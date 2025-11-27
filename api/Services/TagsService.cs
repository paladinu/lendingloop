using Api.Models;
using MongoDB.Driver;

namespace Api.Services;

public class TagsService : ITagsService
{
    private readonly IMongoCollection<SystemTag> _tagsCollection;
    private readonly IConfiguration _configuration;

    public TagsService(IMongoDatabase database, IConfiguration configuration)
    {
        _tagsCollection = database.GetCollection<SystemTag>("systemTags");
        _configuration = configuration;
        
        // Ensure indexes are created when service is instantiated
        _ = Task.Run(EnsureIndexesAsync);
    }

    public async Task<List<SystemTag>> GetAllActiveTagsAsync()
    {
        var filter = Builders<SystemTag>.Filter.Eq(tag => tag.IsActive, true);
        var sort = Builders<SystemTag>.Sort.Ascending(tag => tag.DisplayName);
        return await _tagsCollection.Find(filter).Sort(sort).ToListAsync();
    }

    public async Task<SystemTag?> GetTagByNameAsync(string name)
    {
        var filter = Builders<SystemTag>.Filter.Eq(tag => tag.Name, name);
        return await _tagsCollection.Find(filter).FirstOrDefaultAsync();
    }

    public async Task InitializeDefaultTagsAsync()
    {
        // Check if tags already exist
        var existingCount = await _tagsCollection.CountDocumentsAsync(FilterDefinition<SystemTag>.Empty);
        if (existingCount > 0)
        {
            return; // Tags already initialized
        }

        var defaultTags = new List<SystemTag>
        {
            // Tools & Equipment
            new SystemTag { Name = "power-tools", DisplayName = "Power Tools", Category = "Tools & Equipment" },
            new SystemTag { Name = "hand-tools", DisplayName = "Hand Tools", Category = "Tools & Equipment" },
            new SystemTag { Name = "garden-tools", DisplayName = "Garden Tools", Category = "Tools & Equipment" },
            new SystemTag { Name = "automotive-tools", DisplayName = "Automotive Tools", Category = "Tools & Equipment" },
            new SystemTag { Name = "measuring-tools", DisplayName = "Measuring Tools", Category = "Tools & Equipment" },

            // Home & Garden
            new SystemTag { Name = "lawn-care", DisplayName = "Lawn Care", Category = "Home & Garden" },
            new SystemTag { Name = "cleaning-equipment", DisplayName = "Cleaning Equipment", Category = "Home & Garden" },
            new SystemTag { Name = "ladders", DisplayName = "Ladders", Category = "Home & Garden" },
            new SystemTag { Name = "pressure-washers", DisplayName = "Pressure Washers", Category = "Home & Garden" },
            new SystemTag { Name = "painting-supplies", DisplayName = "Painting Supplies", Category = "Home & Garden" },

            // Electronics
            new SystemTag { Name = "cameras", DisplayName = "Cameras", Category = "Electronics" },
            new SystemTag { Name = "audio-equipment", DisplayName = "Audio Equipment", Category = "Electronics" },
            new SystemTag { Name = "projectors", DisplayName = "Projectors", Category = "Electronics" },
            new SystemTag { Name = "gaming-consoles", DisplayName = "Gaming Consoles", Category = "Electronics" },
            new SystemTag { Name = "computers", DisplayName = "Computers", Category = "Electronics" },

            // Sports & Recreation
            new SystemTag { Name = "camping-gear", DisplayName = "Camping Gear", Category = "Sports & Recreation" },
            new SystemTag { Name = "sports-equipment", DisplayName = "Sports Equipment", Category = "Sports & Recreation" },
            new SystemTag { Name = "bikes", DisplayName = "Bikes", Category = "Sports & Recreation" },
            new SystemTag { Name = "water-sports", DisplayName = "Water Sports", Category = "Sports & Recreation" },
            new SystemTag { Name = "winter-sports", DisplayName = "Winter Sports", Category = "Sports & Recreation" },

            // Kitchen & Appliances
            new SystemTag { Name = "kitchen-appliances", DisplayName = "Kitchen Appliances", Category = "Kitchen & Appliances" },
            new SystemTag { Name = "cookware", DisplayName = "Cookware", Category = "Kitchen & Appliances" },
            new SystemTag { Name = "baking-equipment", DisplayName = "Baking Equipment", Category = "Kitchen & Appliances" },
            new SystemTag { Name = "party-supplies", DisplayName = "Party Supplies", Category = "Kitchen & Appliances" },

            // Baby & Kids
            new SystemTag { Name = "baby-gear", DisplayName = "Baby Gear", Category = "Baby & Kids" },
            new SystemTag { Name = "toys", DisplayName = "Toys", Category = "Baby & Kids" },
            new SystemTag { Name = "car-seats", DisplayName = "Car Seats", Category = "Baby & Kids" },
            new SystemTag { Name = "strollers", DisplayName = "Strollers", Category = "Baby & Kids" },

            // Books & Media
            new SystemTag { Name = "books", DisplayName = "Books", Category = "Books & Media" },
            new SystemTag { Name = "movies", DisplayName = "Movies", Category = "Books & Media" },
            new SystemTag { Name = "board-games", DisplayName = "Board Games", Category = "Books & Media" },
            new SystemTag { Name = "educational", DisplayName = "Educational", Category = "Books & Media" },

            // Other
            new SystemTag { Name = "furniture", DisplayName = "Furniture", Category = "Other" },
            new SystemTag { Name = "storage", DisplayName = "Storage", Category = "Other" },
            new SystemTag { Name = "seasonal", DisplayName = "Seasonal", Category = "Other" },
            new SystemTag { Name = "party-decorations", DisplayName = "Party Decorations", Category = "Other" },
            new SystemTag { Name = "miscellaneous", DisplayName = "Miscellaneous", Category = "Other" }
        };

        await _tagsCollection.InsertManyAsync(defaultTags);
    }

    public async Task IncrementTagUsageAsync(string tagName)
    {
        var filter = Builders<SystemTag>.Filter.Eq(tag => tag.Name, tagName);
        var update = Builders<SystemTag>.Update.Inc(tag => tag.UsageCount, 1);
        await _tagsCollection.UpdateOneAsync(filter, update);
    }

    public async Task DecrementTagUsageAsync(string tagName)
    {
        var filter = Builders<SystemTag>.Filter.Eq(tag => tag.Name, tagName);
        var update = Builders<SystemTag>.Update.Inc(tag => tag.UsageCount, -1);
        await _tagsCollection.UpdateOneAsync(filter, update);
    }

    public async Task<Dictionary<string, int>> GetTagUsageStatisticsAsync()
    {
        var tags = await _tagsCollection.Find(FilterDefinition<SystemTag>.Empty).ToListAsync();
        return tags.ToDictionary(tag => tag.Name, tag => tag.UsageCount);
    }

    private async Task EnsureIndexesAsync()
    {
        try
        {
            // Skip index creation if collection is not initialized (e.g., in test scenarios)
            if (_tagsCollection == null || _tagsCollection.Database == null)
            {
                return;
            }

            // Verify database connection before creating indexes
            try
            {
                await _tagsCollection.Database.ListCollectionNamesAsync();
            }
            catch
            {
                // Database not accessible, skip index creation
                return;
            }

            // Create unique index on name field
            var nameIndexKeys = Builders<SystemTag>.IndexKeys.Ascending(tag => tag.Name);
            var nameIndexOptions = new CreateIndexOptions { Unique = true };
            var nameIndexModel = new CreateIndexModel<SystemTag>(nameIndexKeys, nameIndexOptions);

            // Create index on category field
            var categoryIndexKeys = Builders<SystemTag>.IndexKeys.Ascending(tag => tag.Category);
            var categoryIndexModel = new CreateIndexModel<SystemTag>(categoryIndexKeys);

            // Create index on isActive field
            var isActiveIndexKeys = Builders<SystemTag>.IndexKeys.Ascending(tag => tag.IsActive);
            var isActiveIndexModel = new CreateIndexModel<SystemTag>(isActiveIndexKeys);

            await _tagsCollection.Indexes.CreateManyAsync(new[]
            {
                nameIndexModel,
                categoryIndexModel,
                isActiveIndexModel
            });

            Console.WriteLine("Indexes created successfully for SystemTags collection");
        }
        catch (Exception ex)
        {
            // Log the exception but don't fail the application startup
            Console.WriteLine($"Warning: Could not create indexes for SystemTags collection: {ex.Message}");
        }
    }
}
