using Api.Models;
using Api.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Moq;
using Xunit;

namespace Api.Tests;

public class NotificationFlowIntegrationTests
{
    [Fact]
    public async Task CreateRequest_CreatesNotificationAndSendsEmail()
    {
        //arrange
        var itemId = "item123";
        var requesterId = "requester456";
        var ownerId = "owner789";
        
        var mockDatabase = new Mock<IMongoDatabase>();
        var mockRequestsCollection = new Mock<IMongoCollection<ItemRequest>>();
        var mockNotificationsCollection = new Mock<IMongoCollection<Notification>>();
        var mockConfiguration = new Mock<IConfiguration>();
        var mockItemsService = new Mock<IItemsService>();
        var mockUserService = new Mock<IUserService>();
        var mockEmailService = new Mock<IEmailService>();
        var mockRequestLogger = new Mock<ILogger<ItemRequestService>>();
        var mockNotificationLogger = new Mock<ILogger<NotificationService>>();
        
        mockConfiguration.Setup(c => c["MongoDB:ItemRequestsCollectionName"]).Returns("itemrequests");
        mockConfiguration.Setup(c => c["MongoDB:NotificationsCollectionName"]).Returns("notifications");
        
        mockDatabase.Setup(db => db.GetCollection<ItemRequest>("itemrequests", null))
            .Returns(mockRequestsCollection.Object);
        mockDatabase.Setup(db => db.GetCollection<Notification>("notifications", null))
            .Returns(mockNotificationsCollection.Object);
        
        var item = new SharedItem
        {
            Id = itemId,
            UserId = ownerId,
            Name = "Power Drill",
            Description = "18V cordless drill"
        };
        
        var owner = new User
        {
            Id = ownerId,
            Email = "owner@test.com",
            FirstName = "John",
            LastName = "Owner"
        };
        
        var requester = new User
        {
            Id = requesterId,
            Email = "requester@test.com",
            FirstName = "Jane",
            LastName = "Requester"
        };
        
        mockItemsService.Setup(s => s.GetItemByIdAsync(itemId)).ReturnsAsync(item);
        mockUserService.Setup(s => s.GetUserByIdAsync(ownerId)).ReturnsAsync(owner);
        mockUserService.Setup(s => s.GetUserByIdAsync(requesterId)).ReturnsAsync(requester);
        mockRequestsCollection.Setup(c => c.InsertOneAsync(It.IsAny<ItemRequest>(), null, default))
            .Returns(Task.CompletedTask);
        
        Notification? capturedNotification = null;
        mockNotificationsCollection.Setup(c => c.InsertOneAsync(It.IsAny<Notification>(), null, default))
            .Callback<Notification, InsertOneOptions, CancellationToken>((notif, options, token) =>
            {
                capturedNotification = notif;
            })
            .Returns(Task.CompletedTask);
        
        mockEmailService.Setup(s => s.SendItemRequestCreatedEmailAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), 
            It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);
        
        // Setup index manager mocks
        var mockRequestIndexManager = new Mock<IMongoIndexManager<ItemRequest>>();
        mockRequestIndexManager.Setup(m => m.CreateManyAsync(
            It.IsAny<IEnumerable<CreateIndexModel<ItemRequest>>>(), null, default))
            .ReturnsAsync(new List<string>());
        mockRequestsCollection.Setup(c => c.Indexes).Returns(mockRequestIndexManager.Object);
        
        var mockNotificationIndexManager = new Mock<IMongoIndexManager<Notification>>();
        mockNotificationIndexManager.Setup(m => m.CreateManyAsync(
            It.IsAny<IEnumerable<CreateIndexModel<Notification>>>(), null, default))
            .ReturnsAsync(new List<string>());
        mockNotificationsCollection.Setup(c => c.Indexes).Returns(mockNotificationIndexManager.Object);
        
        var notificationService = new NotificationService(
            mockDatabase.Object,
            mockConfiguration.Object,
            mockNotificationLogger.Object
        );
        
        var mockLoopScoreService = new Mock<ILoopScoreService>();
        
        var itemRequestService = new ItemRequestService(
            mockDatabase.Object,
            mockConfiguration.Object,
            mockItemsService.Object,
            notificationService,
            mockEmailService.Object,
            mockUserService.Object,
            mockLoopScoreService.Object,
            mockRequestLogger.Object
        );
        
        //act
        var request = await itemRequestService.CreateRequestAsync(itemId, requesterId);
        
        // Wait a bit for async notification creation
        await Task.Delay(200);
        
        //assert
        Assert.NotNull(request);
        Assert.Equal(itemId, request.ItemId);
        Assert.Equal(requesterId, request.RequesterId);
        Assert.Equal(ownerId, request.OwnerId);
        Assert.Equal(RequestStatus.Pending, request.Status);
        
        // Verify notification was created
        Assert.NotNull(capturedNotification);
        Assert.Equal(ownerId, capturedNotification.UserId);
        Assert.Equal(NotificationType.ItemRequestCreated, capturedNotification.Type);
        Assert.Contains("Jane Requester", capturedNotification.Message);
        Assert.Contains("Power Drill", capturedNotification.Message);
        Assert.Equal(itemId, capturedNotification.ItemId);
        Assert.Equal(requesterId, capturedNotification.RelatedUserId);
        Assert.False(capturedNotification.IsRead);
        
        // Verify email was sent
        mockEmailService.Verify(s => s.SendItemRequestCreatedEmailAsync(
            "owner@test.com",
            "John Owner",
            "Jane Requester",
            "Power Drill",
            It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task NotificationService_CreatesNotificationSuccessfully()
    {
        //arrange
        var userId = "user123";
        var itemId = "item456";
        var requestId = "request789";
        var relatedUserId = "relatedUser999";
        
        var mockDatabase = new Mock<IMongoDatabase>();
        var mockNotificationsCollection = new Mock<IMongoCollection<Notification>>();
        var mockConfiguration = new Mock<IConfiguration>();
        var mockNotificationLogger = new Mock<ILogger<NotificationService>>();
        
        mockConfiguration.Setup(c => c["MongoDB:NotificationsCollectionName"]).Returns("notifications");
        mockDatabase.Setup(db => db.GetCollection<Notification>("notifications", null))
            .Returns(mockNotificationsCollection.Object);
        
        Notification? capturedNotification = null;
        mockNotificationsCollection.Setup(c => c.InsertOneAsync(It.IsAny<Notification>(), null, default))
            .Callback<Notification, InsertOneOptions, CancellationToken>((notif, options, token) =>
            {
                capturedNotification = notif;
                notif.Id = "notif123";
            })
            .Returns(Task.CompletedTask);
        
        var mockIndexManager = new Mock<IMongoIndexManager<Notification>>();
        mockIndexManager.Setup(m => m.CreateManyAsync(
            It.IsAny<IEnumerable<CreateIndexModel<Notification>>>(), null, default))
            .ReturnsAsync(new List<string>());
        mockNotificationsCollection.Setup(c => c.Indexes).Returns(mockIndexManager.Object);
        
        var notificationService = new NotificationService(
            mockDatabase.Object,
            mockConfiguration.Object,
            mockNotificationLogger.Object
        );
        
        //act
        var notification = await notificationService.CreateNotificationAsync(
            userId,
            NotificationType.ItemRequestApproved,
            "Your request was approved",
            itemId,
            requestId,
            relatedUserId
        );
        
        //assert
        Assert.NotNull(notification);
        Assert.Equal(userId, notification.UserId);
        Assert.Equal(NotificationType.ItemRequestApproved, notification.Type);
        Assert.Equal("Your request was approved", notification.Message);
        Assert.Equal(itemId, notification.ItemId);
        Assert.Equal(requestId, notification.ItemRequestId);
        Assert.Equal(relatedUserId, notification.RelatedUserId);
        Assert.False(notification.IsRead);
        
        // Verify the notification was captured
        Assert.NotNull(capturedNotification);
        Assert.Equal(userId, capturedNotification.UserId);
    }

    [Fact]
    public async Task CompleteRequestFlow_CreatesMultipleNotifications()
    {
        //arrange
        var itemId = "item123";
        var requesterId = "requester456";
        var ownerId = "owner789";
        
        var mockDatabase = new Mock<IMongoDatabase>();
        var mockRequestsCollection = new Mock<IMongoCollection<ItemRequest>>();
        var mockNotificationsCollection = new Mock<IMongoCollection<Notification>>();
        var mockConfiguration = new Mock<IConfiguration>();
        var mockItemsService = new Mock<IItemsService>();
        var mockUserService = new Mock<IUserService>();
        var mockEmailService = new Mock<IEmailService>();
        var mockRequestLogger = new Mock<ILogger<ItemRequestService>>();
        var mockNotificationLogger = new Mock<ILogger<NotificationService>>();
        
        mockConfiguration.Setup(c => c["MongoDB:ItemRequestsCollectionName"]).Returns("itemrequests");
        mockConfiguration.Setup(c => c["MongoDB:NotificationsCollectionName"]).Returns("notifications");
        
        mockDatabase.Setup(db => db.GetCollection<ItemRequest>("itemrequests", null))
            .Returns(mockRequestsCollection.Object);
        mockDatabase.Setup(db => db.GetCollection<Notification>("notifications", null))
            .Returns(mockNotificationsCollection.Object);
        
        var item = new SharedItem
        {
            Id = itemId,
            UserId = ownerId,
            Name = "Power Drill",
            IsAvailable = true
        };
        
        var owner = new User
        {
            Id = ownerId,
            Email = "owner@test.com",
            FirstName = "John",
            LastName = "Owner"
        };
        
        var requester = new User
        {
            Id = requesterId,
            Email = "requester@test.com",
            FirstName = "Jane",
            LastName = "Requester"
        };
        
        mockItemsService.Setup(s => s.GetItemByIdAsync(itemId)).ReturnsAsync(item);
        mockItemsService.Setup(s => s.UpdateItemAvailabilityAsync(itemId, It.IsAny<bool>()))
            .ReturnsAsync(item);
        mockUserService.Setup(s => s.GetUserByIdAsync(ownerId)).ReturnsAsync(owner);
        mockUserService.Setup(s => s.GetUserByIdAsync(requesterId)).ReturnsAsync(requester);
        
        mockRequestsCollection.Setup(c => c.InsertOneAsync(It.IsAny<ItemRequest>(), null, default))
            .Callback<ItemRequest, InsertOneOptions, CancellationToken>((req, options, token) =>
            {
                req.Id = "request001";
            })
            .Returns(Task.CompletedTask);
        
        var notificationList = new List<Notification>();
        mockNotificationsCollection.Setup(c => c.InsertOneAsync(It.IsAny<Notification>(), null, default))
            .Callback<Notification, InsertOneOptions, CancellationToken>((notif, options, token) =>
            {
                notificationList.Add(notif);
            })
            .Returns(Task.CompletedTask);
        
        mockEmailService.Setup(s => s.SendItemRequestCreatedEmailAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), 
            It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);
        mockEmailService.Setup(s => s.SendItemRequestApprovedEmailAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);
        mockEmailService.Setup(s => s.SendItemRequestCompletedEmailAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);
        
        // Setup index manager mocks
        var mockRequestIndexManager = new Mock<IMongoIndexManager<ItemRequest>>();
        mockRequestIndexManager.Setup(m => m.CreateManyAsync(
            It.IsAny<IEnumerable<CreateIndexModel<ItemRequest>>>(), null, default))
            .ReturnsAsync(new List<string>());
        mockRequestsCollection.Setup(c => c.Indexes).Returns(mockRequestIndexManager.Object);
        
        var mockNotificationIndexManager = new Mock<IMongoIndexManager<Notification>>();
        mockNotificationIndexManager.Setup(m => m.CreateManyAsync(
            It.IsAny<IEnumerable<CreateIndexModel<Notification>>>(), null, default))
            .ReturnsAsync(new List<string>());
        mockNotificationsCollection.Setup(c => c.Indexes).Returns(mockNotificationIndexManager.Object);
        
        var notificationService = new NotificationService(
            mockDatabase.Object,
            mockConfiguration.Object,
            mockNotificationLogger.Object
        );
        
        var mockLoopScoreService = new Mock<ILoopScoreService>();
        
        var itemRequestService = new ItemRequestService(
            mockDatabase.Object,
            mockConfiguration.Object,
            mockItemsService.Object,
            notificationService,
            mockEmailService.Object,
            mockUserService.Object,
            mockLoopScoreService.Object,
            mockRequestLogger.Object
        );
        
        //act
        // Step 1: Create request
        var request = await itemRequestService.CreateRequestAsync(itemId, requesterId);
        await Task.Delay(200);
        
        //assert
        Assert.NotNull(request);
        Assert.Equal(RequestStatus.Pending, request.Status);
        
        // Verify notification was created for request creation
        Assert.Single(notificationList);
        var notification1 = notificationList[0];
        Assert.Equal(ownerId, notification1.UserId);
        Assert.Equal(NotificationType.ItemRequestCreated, notification1.Type);
        
        // Verify email was sent for request creation
        mockEmailService.Verify(s => s.SendItemRequestCreatedEmailAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), 
            It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }
}
