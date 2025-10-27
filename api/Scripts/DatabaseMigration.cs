using MongoDB.Driver;
using MongoDB.Bson;
using Api.Models;

namespace Api.Scripts;

/// <summary>
/// Database migration scripts for user authentication system
/// </summary>
public class DatabaseMigration
{
    private readonly IMongoDatabase _database;
    private readonly ILogger<DatabaseMigration> _logger;

    public DatabaseMigration(IMongoDatabase database, ILogger<DatabaseMigration> logger)
    {
        _database = database;
        _logger = logger;
    }

    /// <summary>
    /// Migrates existing Items collection from ownerId to userId field
    /// This handles the case where items might have ownerId instead of userId
    /// </summary>
    public async Task MigrateItemsOwnerIdToUserId()
    {
        try
        {
            var itemsCollection = _database.GetCollection<BsonDocument>("items");
            
            // Find all documents that have ownerId but not userId
            var filter = Builders<BsonDocument>.Filter.And(
                Builders<BsonDocument>.Filter.Exists("ownerId"),
                Builders<BsonDocument>.Filter.Not(Builders<BsonDocument>.Filter.Exists("userId"))
            );

            var itemsToMigrate = await itemsCollection.Find(filter).ToListAsync();
            
            _logger.LogInformation($"Found {itemsToMigrate.Count} items to migrate from ownerId to userId");

            foreach (var item in itemsToMigrate)
            {
                var update = Builders<BsonDocument>.Update
                    .Set("userId", item["ownerId"])
                    .Unset("ownerId");

                await itemsCollection.UpdateOneAsync(
                    Builders<BsonDocument>.Filter.Eq("_id", item["_id"]),
                    update
                );
            }

            _logger.LogInformation($"Successfully migrated {itemsToMigrate.Count} items from ownerId to userId");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during ownerId to userId migration");
            throw;
        }
    }

    /// <summary>
    /// Removes ownerId field from all items that still have it
    /// Should be run after ensuring all items have userId field
    /// </summary>
    public async Task RemoveOwnerIdField()
    {
        try
        {
            var itemsCollection = _database.GetCollection<BsonDocument>("items");
            
            // Find all documents that still have ownerId field
            var filter = Builders<BsonDocument>.Filter.Exists("ownerId");
            var itemsWithOwnerId = await itemsCollection.Find(filter).ToListAsync();
            
            _logger.LogInformation($"Found {itemsWithOwnerId.Count} items with ownerId field to clean up");

            if (itemsWithOwnerId.Count > 0)
            {
                var update = Builders<BsonDocument>.Update.Unset("ownerId");
                var result = await itemsCollection.UpdateManyAsync(filter, update);
                
                _logger.LogInformation($"Removed ownerId field from {result.ModifiedCount} items");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during ownerId field removal");
            throw;
        }
    }

    /// <summary>
    /// Creates necessary database indexes for performance
    /// </summary>
    public async Task CreateDatabaseIndexes()
    {
        try
        {
            // Create indexes for Users collection
            var usersCollection = _database.GetCollection<User>("users");
            
            // Create unique index on email field
            var emailIndexKeys = Builders<User>.IndexKeys.Ascending(u => u.Email);
            var emailIndexOptions = new CreateIndexOptions { Unique = true };
            var emailIndexModel = new CreateIndexModel<User>(emailIndexKeys, emailIndexOptions);
            
            await usersCollection.Indexes.CreateOneAsync(emailIndexModel);
            _logger.LogInformation("Created unique index on users.email");

            // Create index on emailVerificationToken for faster lookups
            var tokenIndexKeys = Builders<User>.IndexKeys.Ascending(u => u.EmailVerificationToken);
            var tokenIndexModel = new CreateIndexModel<User>(tokenIndexKeys);
            
            await usersCollection.Indexes.CreateOneAsync(tokenIndexModel);
            _logger.LogInformation("Created index on users.emailVerificationToken");

            // Create indexes for Items collection
            var itemsCollection = _database.GetCollection<SharedItem>("items");
            
            // Create index on userId field for faster user-specific queries
            var userIdIndexKeys = Builders<SharedItem>.IndexKeys.Ascending(i => i.UserId);
            var userIdIndexModel = new CreateIndexModel<SharedItem>(userIdIndexKeys);
            
            await itemsCollection.Indexes.CreateOneAsync(userIdIndexModel);
            _logger.LogInformation("Created index on items.userId");

            // Create compound index on userId and isAvailable for filtered queries
            var compoundIndexKeys = Builders<SharedItem>.IndexKeys
                .Ascending(i => i.UserId)
                .Ascending(i => i.IsAvailable);
            var compoundIndexModel = new CreateIndexModel<SharedItem>(compoundIndexKeys);
            
            await itemsCollection.Indexes.CreateOneAsync(compoundIndexModel);
            _logger.LogInformation("Created compound index on items.userId and items.isAvailable");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during database index creation");
            throw;
        }
    }

    /// <summary>
    /// Validates that all items have userId field and no items have ownerId
    /// </summary>
    public async Task<bool> ValidateMigration()
    {
        try
        {
            var itemsCollection = _database.GetCollection<BsonDocument>("items");
            
            // Check for items without userId
            var itemsWithoutUserId = await itemsCollection
                .CountDocumentsAsync(Builders<BsonDocument>.Filter.Not(
                    Builders<BsonDocument>.Filter.Exists("userId")));
            
            // Check for items with ownerId
            var itemsWithOwnerId = await itemsCollection
                .CountDocumentsAsync(Builders<BsonDocument>.Filter.Exists("ownerId"));
            
            var isValid = itemsWithoutUserId == 0 && itemsWithOwnerId == 0;
            
            _logger.LogInformation($"Migration validation: Items without userId: {itemsWithoutUserId}, Items with ownerId: {itemsWithOwnerId}, Valid: {isValid}");
            
            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during migration validation");
            return false;
        }
    }

    /// <summary>
    /// Runs the complete migration process
    /// </summary>
    public async Task RunCompleteMigration()
    {
        _logger.LogInformation("Starting complete database migration");
        
        // Step 1: Migrate ownerId to userId
        await MigrateItemsOwnerIdToUserId();
        
        // Step 2: Create database indexes
        await CreateDatabaseIndexes();
        
        // Step 3: Remove ownerId field
        await RemoveOwnerIdField();
        
        // Step 4: Validate migration
        var isValid = await ValidateMigration();
        
        if (isValid)
        {
            _logger.LogInformation("Database migration completed successfully");
        }
        else
        {
            _logger.LogError("Database migration validation failed");
            throw new InvalidOperationException("Database migration validation failed");
        }
    }
}