using Api.Models;
using MongoDB.Driver;

namespace Api.Services;

public class ItemRequestService : IItemRequestService
{
    private readonly IMongoCollection<ItemRequest> _requestsCollection;
    private readonly IItemsService _itemsService;
    private readonly INotificationService _notificationService;
    private readonly IEmailService _emailService;
    private readonly IUserService _userService;
    private readonly ILogger<ItemRequestService> _logger;

    public ItemRequestService(
        IMongoDatabase database, 
        IConfiguration configuration, 
        IItemsService itemsService,
        INotificationService notificationService,
        IEmailService emailService,
        IUserService userService,
        ILogger<ItemRequestService> logger)
    {
        var collectionName = configuration["MongoDB:ItemRequestsCollectionName"] ?? "itemrequests";
        _requestsCollection = database.GetCollection<ItemRequest>(collectionName);
        _itemsService = itemsService;
        _notificationService = notificationService;
        _emailService = emailService;
        _userService = userService;
        _logger = logger;
        
        // Ensure indexes are created when service is instantiated
        _ = Task.Run(EnsureIndexesAsync);
    }

    public async Task<ItemRequest> CreateRequestAsync(string itemId, string requesterId)
    {
        // Get the item to validate it exists and get owner info
        var item = await _itemsService.GetItemByIdAsync(itemId);
        if (item == null)
        {
            throw new ArgumentException("Item not found", nameof(itemId));
        }

        // Validate requester is not the owner
        if (item.UserId == requesterId)
        {
            throw new InvalidOperationException("Cannot request your own item");
        }

        // Create the request
        var request = new ItemRequest
        {
            ItemId = itemId,
            RequesterId = requesterId,
            OwnerId = item.UserId,
            Status = RequestStatus.Pending,
            RequestedAt = DateTime.UtcNow
        };

        await _requestsCollection.InsertOneAsync(request);

        // Send notifications to item owner
        await SendNotificationsAsync(
            item.UserId,           // recipient (owner)
            requesterId,           // related user (requester)
            itemId,
            request.Id!,
            NotificationType.ItemRequestCreated
        );

        return request;
    }

    public async Task<List<ItemRequest>> GetRequestsByRequesterAsync(string requesterId)
    {
        var filter = Builders<ItemRequest>.Filter.Eq(r => r.RequesterId, requesterId);
        var sort = Builders<ItemRequest>.Sort.Descending(r => r.RequestedAt);
        return await _requestsCollection.Find(filter).Sort(sort).ToListAsync();
    }

    public async Task<List<ItemRequest>> GetPendingRequestsByOwnerAsync(string ownerId)
    {
        var filter = Builders<ItemRequest>.Filter.And(
            Builders<ItemRequest>.Filter.Eq(r => r.OwnerId, ownerId),
            Builders<ItemRequest>.Filter.In(r => r.Status, new[] { RequestStatus.Pending, RequestStatus.Approved })
        );
        var sort = Builders<ItemRequest>.Sort.Descending(r => r.RequestedAt);
        return await _requestsCollection.Find(filter).Sort(sort).ToListAsync();
    }

    public async Task<List<ItemRequest>> GetRequestsByItemIdAsync(string itemId)
    {
        var filter = Builders<ItemRequest>.Filter.Eq(r => r.ItemId, itemId);
        var sort = Builders<ItemRequest>.Sort.Descending(r => r.RequestedAt);
        return await _requestsCollection.Find(filter).Sort(sort).ToListAsync();
    }

    public async Task<ItemRequest?> GetRequestByIdAsync(string requestId)
    {
        return await _requestsCollection.Find(r => r.Id == requestId).FirstOrDefaultAsync();
    }

    public async Task<ItemRequest?> GetActiveRequestForItemAsync(string itemId)
    {
        var filter = Builders<ItemRequest>.Filter.And(
            Builders<ItemRequest>.Filter.Eq(r => r.ItemId, itemId),
            Builders<ItemRequest>.Filter.Eq(r => r.Status, RequestStatus.Approved)
        );
        return await _requestsCollection.Find(filter).FirstOrDefaultAsync();
    }

    public async Task<ItemRequest?> ApproveRequestAsync(string requestId, string ownerId)
    {
        var request = await GetRequestByIdAsync(requestId);
        if (request == null)
        {
            return null;
        }

        // Verify owner
        if (request.OwnerId != ownerId)
        {
            throw new UnauthorizedAccessException("Only the item owner can approve requests");
        }

        // Verify status is pending
        if (request.Status != RequestStatus.Pending)
        {
            throw new InvalidOperationException("Only pending requests can be approved");
        }

        // Check for existing approved requests
        var activeRequest = await GetActiveRequestForItemAsync(request.ItemId);
        if (activeRequest != null)
        {
            throw new InvalidOperationException("Another request is already approved for this item");
        }

        // Update request status
        var filter = Builders<ItemRequest>.Filter.Eq(r => r.Id, requestId);
        var update = Builders<ItemRequest>.Update
            .Set(r => r.Status, RequestStatus.Approved)
            .Set(r => r.RespondedAt, DateTime.UtcNow);

        var options = new FindOneAndUpdateOptions<ItemRequest>
        {
            ReturnDocument = ReturnDocument.After
        };

        var updatedRequest = await _requestsCollection.FindOneAndUpdateAsync(filter, update, options);

        // Update item availability
        if (updatedRequest != null)
        {
            await UpdateItemAvailabilityAsync(request.ItemId, false);

            // Send notifications to requester
            await SendNotificationsAsync(
                request.RequesterId,   // recipient (requester)
                ownerId,               // related user (owner)
                request.ItemId,
                requestId,
                NotificationType.ItemRequestApproved
            );
        }

        return updatedRequest;
    }

    public async Task<ItemRequest?> RejectRequestAsync(string requestId, string ownerId)
    {
        var request = await GetRequestByIdAsync(requestId);
        if (request == null)
        {
            return null;
        }

        // Verify owner
        if (request.OwnerId != ownerId)
        {
            throw new UnauthorizedAccessException("Only the item owner can reject requests");
        }

        // Verify status is pending
        if (request.Status != RequestStatus.Pending)
        {
            throw new InvalidOperationException("Only pending requests can be rejected");
        }

        // Update request status
        var filter = Builders<ItemRequest>.Filter.Eq(r => r.Id, requestId);
        var update = Builders<ItemRequest>.Update
            .Set(r => r.Status, RequestStatus.Rejected)
            .Set(r => r.RespondedAt, DateTime.UtcNow);

        var options = new FindOneAndUpdateOptions<ItemRequest>
        {
            ReturnDocument = ReturnDocument.After
        };

        var updatedRequest = await _requestsCollection.FindOneAndUpdateAsync(filter, update, options);

        // Send notifications to requester
        if (updatedRequest != null)
        {
            await SendNotificationsAsync(
                request.RequesterId,   // recipient (requester)
                ownerId,               // related user (owner)
                request.ItemId,
                requestId,
                NotificationType.ItemRequestRejected
            );
        }

        return updatedRequest;
    }

    public async Task<ItemRequest?> CancelRequestAsync(string requestId, string requesterId)
    {
        var request = await GetRequestByIdAsync(requestId);
        if (request == null)
        {
            return null;
        }

        // Verify requester
        if (request.RequesterId != requesterId)
        {
            throw new UnauthorizedAccessException("Only the requester can cancel their request");
        }

        // Verify status is pending
        if (request.Status != RequestStatus.Pending)
        {
            throw new InvalidOperationException("Only pending requests can be cancelled");
        }

        // Update request status
        var filter = Builders<ItemRequest>.Filter.Eq(r => r.Id, requestId);
        var update = Builders<ItemRequest>.Update
            .Set(r => r.Status, RequestStatus.Cancelled)
            .Set(r => r.RespondedAt, DateTime.UtcNow);

        var options = new FindOneAndUpdateOptions<ItemRequest>
        {
            ReturnDocument = ReturnDocument.After
        };

        var updatedRequest = await _requestsCollection.FindOneAndUpdateAsync(filter, update, options);

        // Send notifications to owner
        if (updatedRequest != null)
        {
            await SendNotificationsAsync(
                request.OwnerId,       // recipient (owner)
                requesterId,           // related user (requester)
                request.ItemId,
                requestId,
                NotificationType.ItemRequestCancelled
            );
        }

        return updatedRequest;
    }

    public async Task<ItemRequest?> CompleteRequestAsync(string requestId, string ownerId)
    {
        var request = await GetRequestByIdAsync(requestId);
        if (request == null)
        {
            return null;
        }

        // Verify owner
        if (request.OwnerId != ownerId)
        {
            throw new UnauthorizedAccessException("Only the item owner can complete requests");
        }

        // Verify status is approved
        if (request.Status != RequestStatus.Approved)
        {
            throw new InvalidOperationException("Only approved requests can be completed");
        }

        // Update request status
        var filter = Builders<ItemRequest>.Filter.Eq(r => r.Id, requestId);
        var update = Builders<ItemRequest>.Update
            .Set(r => r.Status, RequestStatus.Completed)
            .Set(r => r.CompletedAt, DateTime.UtcNow);

        var options = new FindOneAndUpdateOptions<ItemRequest>
        {
            ReturnDocument = ReturnDocument.After
        };

        var updatedRequest = await _requestsCollection.FindOneAndUpdateAsync(filter, update, options);

        // Update item availability
        if (updatedRequest != null)
        {
            await UpdateItemAvailabilityAsync(request.ItemId, true);

            // Send notifications to requester
            await SendNotificationsAsync(
                request.RequesterId,   // recipient (requester)
                ownerId,               // related user (owner)
                request.ItemId,
                requestId,
                NotificationType.ItemRequestCompleted
            );
        }

        return updatedRequest;
    }

    private async Task UpdateItemAvailabilityAsync(string itemId, bool isAvailable)
    {
        var item = await _itemsService.GetItemByIdAsync(itemId);
        if (item != null)
        {
            // We need to add a method to IItemsService to update availability
            // For now, we'll need to update the ItemsService to add this method
            await _itemsService.UpdateItemAvailabilityAsync(itemId, isAvailable);
        }
    }

    private async Task SendNotificationsAsync(
        string recipientUserId, 
        string relatedUserId, 
        string itemId, 
        string itemRequestId, 
        NotificationType notificationType)
    {
        try
        {
            // Retrieve user details for notification context
            var recipient = await _userService.GetUserByIdAsync(recipientUserId);
            var relatedUser = await _userService.GetUserByIdAsync(relatedUserId);
            var item = await _itemsService.GetItemByIdAsync(itemId);

            if (recipient == null || relatedUser == null || item == null)
            {
                _logger.LogWarning("Unable to send notifications: Missing user or item data. RecipientId: {RecipientId}, RelatedUserId: {RelatedUserId}, ItemId: {ItemId}", 
                    recipientUserId, relatedUserId, itemId);
                return;
            }

            // Create notification message based on type
            var relatedUserName = $"{relatedUser.FirstName} {relatedUser.LastName}".Trim();
            string message = notificationType switch
            {
                NotificationType.ItemRequestCreated => $"{relatedUserName} requested to borrow your {item.Name}",
                NotificationType.ItemRequestApproved => $"{relatedUserName} approved your request for {item.Name}",
                NotificationType.ItemRequestRejected => $"{relatedUserName} declined your request for {item.Name}",
                NotificationType.ItemRequestCompleted => $"{relatedUserName} marked your borrowing of {item.Name} as complete",
                NotificationType.ItemRequestCancelled => $"{relatedUserName} cancelled their request for {item.Name}",
                _ => "Item request status updated"
            };

            // Create in-app notification
            try
            {
                await _notificationService.CreateNotificationAsync(
                    recipientUserId,
                    notificationType,
                    message,
                    itemId,
                    itemRequestId,
                    relatedUserId
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create in-app notification for user {UserId}, type {NotificationType}", 
                    recipientUserId, notificationType);
            }

            // Send email notification
            try
            {
                var recipientName = $"{recipient.FirstName} {recipient.LastName}".Trim();
                bool emailSent = notificationType switch
                {
                    NotificationType.ItemRequestCreated => await _emailService.SendItemRequestCreatedEmailAsync(
                        recipient.Email, recipientName, relatedUserName, item.Name, itemRequestId),
                    NotificationType.ItemRequestApproved => await _emailService.SendItemRequestApprovedEmailAsync(
                        recipient.Email, recipientName, relatedUserName, item.Name),
                    NotificationType.ItemRequestRejected => await _emailService.SendItemRequestRejectedEmailAsync(
                        recipient.Email, recipientName, relatedUserName, item.Name),
                    NotificationType.ItemRequestCompleted => await _emailService.SendItemRequestCompletedEmailAsync(
                        recipient.Email, recipientName, relatedUserName, item.Name),
                    NotificationType.ItemRequestCancelled => await _emailService.SendItemRequestCancelledEmailAsync(
                        recipient.Email, recipientName, relatedUserName, item.Name),
                    _ => false
                };

                if (!emailSent)
                {
                    _logger.LogWarning("Email notification failed for user {UserId}, type {NotificationType}", 
                        recipientUserId, notificationType);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email notification for user {UserId}, type {NotificationType}", 
                    recipientUserId, notificationType);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send notifications for type {NotificationType}", notificationType);
        }
    }

    private async Task EnsureIndexesAsync()
    {
        try
        {
            // Index on itemId for item-specific queries
            var itemIdIndexKeys = Builders<ItemRequest>.IndexKeys.Ascending(r => r.ItemId);
            var itemIdIndexModel = new CreateIndexModel<ItemRequest>(itemIdIndexKeys);

            // Index on requesterId for requester's request list
            var requesterIdIndexKeys = Builders<ItemRequest>.IndexKeys.Ascending(r => r.RequesterId);
            var requesterIdIndexModel = new CreateIndexModel<ItemRequest>(requesterIdIndexKeys);

            // Compound index on ownerId + status for pending requests query
            var ownerStatusIndexKeys = Builders<ItemRequest>.IndexKeys
                .Ascending(r => r.OwnerId)
                .Ascending(r => r.Status);
            var ownerStatusIndexModel = new CreateIndexModel<ItemRequest>(ownerStatusIndexKeys);

            // Index on status for filtering
            var statusIndexKeys = Builders<ItemRequest>.IndexKeys.Ascending(r => r.Status);
            var statusIndexModel = new CreateIndexModel<ItemRequest>(statusIndexKeys);

            // Index on requestedAt for sorting
            var requestedAtIndexKeys = Builders<ItemRequest>.IndexKeys.Descending(r => r.RequestedAt);
            var requestedAtIndexModel = new CreateIndexModel<ItemRequest>(requestedAtIndexKeys);

            await _requestsCollection.Indexes.CreateManyAsync(new[]
            {
                itemIdIndexModel,
                requesterIdIndexModel,
                ownerStatusIndexModel,
                statusIndexModel,
                requestedAtIndexModel
            });

            Console.WriteLine("Indexes created successfully for ItemRequests collection");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Could not create indexes for ItemRequests collection: {ex.Message}");
        }
    }
}
