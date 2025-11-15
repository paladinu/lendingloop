using Api.Models;
using MongoDB.Driver;

namespace Api.Services;

public class NotificationService : INotificationService
{
    private readonly IMongoCollection<Notification> _notificationsCollection;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(IMongoDatabase database, IConfiguration configuration, ILogger<NotificationService> logger)
    {
        var collectionName = configuration["MongoDB:NotificationsCollectionName"] ?? "notifications";
        _notificationsCollection = database.GetCollection<Notification>(collectionName);
        _logger = logger;
        
        // Ensure indexes are created when service is instantiated
        _ = Task.Run(EnsureIndexesAsync);
    }

    public async Task<Notification> CreateNotificationAsync(string userId, NotificationType type, 
        string message, string? itemId = null, string? itemRequestId = null, 
        string? relatedUserId = null)
    {
        var notification = new Notification
        {
            UserId = userId,
            Type = type,
            Message = message,
            ItemId = itemId,
            ItemRequestId = itemRequestId,
            RelatedUserId = relatedUserId,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        await _notificationsCollection.InsertOneAsync(notification);
        return notification;
    }

    public async Task<List<Notification>> GetUserNotificationsAsync(string userId, int limit = 50)
    {
        var filter = Builders<Notification>.Filter.Eq(n => n.UserId, userId);
        var sort = Builders<Notification>.Sort.Descending(n => n.CreatedAt);
        
        return await _notificationsCollection
            .Find(filter)
            .Sort(sort)
            .Limit(limit)
            .ToListAsync();
    }

    public async Task<int> GetUnreadCountAsync(string userId)
    {
        var filter = Builders<Notification>.Filter.And(
            Builders<Notification>.Filter.Eq(n => n.UserId, userId),
            Builders<Notification>.Filter.Eq(n => n.IsRead, false)
        );
        
        return (int)await _notificationsCollection.CountDocumentsAsync(filter);
    }

    public async Task<Notification?> MarkAsReadAsync(string notificationId, string userId)
    {
        var filter = Builders<Notification>.Filter.And(
            Builders<Notification>.Filter.Eq(n => n.Id, notificationId),
            Builders<Notification>.Filter.Eq(n => n.UserId, userId)
        );
        
        var update = Builders<Notification>.Update.Set(n => n.IsRead, true);
        
        var options = new FindOneAndUpdateOptions<Notification>
        {
            ReturnDocument = ReturnDocument.After
        };
        
        return await _notificationsCollection.FindOneAndUpdateAsync(filter, update, options);
    }

    public async Task<bool> MarkAllAsReadAsync(string userId)
    {
        var filter = Builders<Notification>.Filter.And(
            Builders<Notification>.Filter.Eq(n => n.UserId, userId),
            Builders<Notification>.Filter.Eq(n => n.IsRead, false)
        );
        
        var update = Builders<Notification>.Update.Set(n => n.IsRead, true);
        
        var result = await _notificationsCollection.UpdateManyAsync(filter, update);
        return result.ModifiedCount > 0;
    }

    public async Task<bool> DeleteNotificationAsync(string notificationId, string userId)
    {
        var filter = Builders<Notification>.Filter.And(
            Builders<Notification>.Filter.Eq(n => n.Id, notificationId),
            Builders<Notification>.Filter.Eq(n => n.UserId, userId)
        );
        
        var result = await _notificationsCollection.DeleteOneAsync(filter);
        return result.DeletedCount > 0;
    }

    private async Task EnsureIndexesAsync()
    {
        try
        {
            // Skip index creation if collection is not initialized (e.g., in test scenarios)
            if (_notificationsCollection == null || _notificationsCollection.Database == null)
            {
                return;
            }

            // Verify database connection before creating indexes
            try
            {
                await _notificationsCollection.Database.ListCollectionNamesAsync();
            }
            catch
            {
                // Database not accessible, skip index creation
                return;
            }

            // Compound index on userId + createdAt for user notification queries
            var userIdCreatedAtIndexKeys = Builders<Notification>.IndexKeys
                .Ascending(n => n.UserId)
                .Descending(n => n.CreatedAt);
            var userIdCreatedAtIndexModel = new CreateIndexModel<Notification>(userIdCreatedAtIndexKeys);

            // Compound index on userId + isRead for unread count queries
            var userIdIsReadIndexKeys = Builders<Notification>.IndexKeys
                .Ascending(n => n.UserId)
                .Ascending(n => n.IsRead);
            var userIdIsReadIndexModel = new CreateIndexModel<Notification>(userIdIsReadIndexKeys);

            // Index on createdAt for cleanup/archival operations
            var createdAtIndexKeys = Builders<Notification>.IndexKeys.Descending(n => n.CreatedAt);
            var createdAtIndexModel = new CreateIndexModel<Notification>(createdAtIndexKeys);

            await _notificationsCollection.Indexes.CreateManyAsync(new[]
            {
                userIdCreatedAtIndexModel,
                userIdIsReadIndexModel,
                createdAtIndexModel
            });

            _logger.LogInformation("Indexes created successfully for Notifications collection");
        }
        catch (Exception ex)
        {
            // Log the exception but don't fail the application startup
            _logger.LogWarning(ex, "Could not create indexes for Notifications collection");
        }
    }
}
