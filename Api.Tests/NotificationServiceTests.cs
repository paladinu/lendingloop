using Api.Models;
using Api.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Moq;
using Xunit;

namespace Api.Tests;

public class NotificationServiceTests
{
    private readonly Mock<IMongoDatabase> _mockDatabase;
    private readonly Mock<IMongoCollection<Notification>> _mockCollection;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<ILogger<NotificationService>> _mockLogger;
    private readonly NotificationService _service;

    public NotificationServiceTests()
    {
        _mockDatabase = new Mock<IMongoDatabase>();
        _mockCollection = new Mock<IMongoCollection<Notification>>();
        _mockConfiguration = new Mock<IConfiguration>();
        _mockLogger = new Mock<ILogger<NotificationService>>();

        _mockConfiguration.Setup(c => c["MongoDB:NotificationsCollectionName"]).Returns("notifications");
        _mockDatabase.Setup(db => db.GetCollection<Notification>("notifications", null))
            .Returns(_mockCollection.Object);

        _service = new NotificationService(_mockDatabase.Object, _mockConfiguration.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task CreateNotificationAsync_CreatesNotificationWithAllFields_WhenAllParametersProvided()
    {
        //arrange
        var userId = "user123";
        var type = NotificationType.ItemRequestCreated;
        var message = "Test notification message";
        var itemId = "item123";
        var itemRequestId = "request123";
        var relatedUserId = "relatedUser123";

        _mockCollection.Setup(c => c.InsertOneAsync(It.IsAny<Notification>(), null, default))
            .Returns(Task.CompletedTask);

        //act
        var result = await _service.CreateNotificationAsync(userId, type, message, itemId, itemRequestId, relatedUserId);

        //assert
        Assert.Equal(userId, result.UserId);
        Assert.Equal(type, result.Type);
        Assert.Equal(message, result.Message);
        Assert.Equal(itemId, result.ItemId);
        Assert.Equal(itemRequestId, result.ItemRequestId);
        Assert.Equal(relatedUserId, result.RelatedUserId);
        Assert.False(result.IsRead);
        Assert.NotEqual(default(DateTime), result.CreatedAt);
    }

    [Fact]
    public async Task CreateNotificationAsync_CreatesNotificationWithOptionalFieldsNull_WhenOptionalParametersNotProvided()
    {
        //arrange
        var userId = "user123";
        var type = NotificationType.ItemRequestApproved;
        var message = "Test notification";

        _mockCollection.Setup(c => c.InsertOneAsync(It.IsAny<Notification>(), null, default))
            .Returns(Task.CompletedTask);

        //act
        var result = await _service.CreateNotificationAsync(userId, type, message);

        //assert
        Assert.Equal(userId, result.UserId);
        Assert.Equal(type, result.Type);
        Assert.Equal(message, result.Message);
        Assert.Null(result.ItemId);
        Assert.Null(result.ItemRequestId);
        Assert.Null(result.RelatedUserId);
        Assert.False(result.IsRead);
    }

    [Fact]
    public async Task CreateNotificationAsync_SetsIsReadToFalse_WhenCreatingNotification()
    {
        //arrange
        var userId = "user123";
        var type = NotificationType.ItemRequestRejected;
        var message = "Test notification";

        _mockCollection.Setup(c => c.InsertOneAsync(It.IsAny<Notification>(), null, default))
            .Returns(Task.CompletedTask);

        //act
        var result = await _service.CreateNotificationAsync(userId, type, message);

        //assert
        Assert.False(result.IsRead);
    }



    [Fact]
    public async Task GetUnreadCountAsync_ReturnsCorrectCount_WhenUserHasUnreadNotifications()
    {
        //arrange
        var userId = "user123";
        var unreadCount = 5;

        _mockCollection.Setup(c => c.CountDocumentsAsync(
            It.IsAny<FilterDefinition<Notification>>(),
            null,
            default))
            .ReturnsAsync(unreadCount);

        //act
        var result = await _service.GetUnreadCountAsync(userId);

        //assert
        Assert.Equal(unreadCount, result);
    }

    [Fact]
    public async Task GetUnreadCountAsync_ReturnsZero_WhenUserHasNoUnreadNotifications()
    {
        //arrange
        var userId = "user123";

        _mockCollection.Setup(c => c.CountDocumentsAsync(
            It.IsAny<FilterDefinition<Notification>>(),
            null,
            default))
            .ReturnsAsync(0);

        //act
        var result = await _service.GetUnreadCountAsync(userId);

        //assert
        Assert.Equal(0, result);
    }

    [Fact]
    public async Task MarkAsReadAsync_MarksNotificationAsRead_WhenNotificationExistsAndUserOwnsIt()
    {
        //arrange
        var notificationId = "notification123";
        var userId = "user123";
        var notification = new Notification
        {
            Id = notificationId,
            UserId = userId,
            Message = "Test",
            IsRead = true
        };

        _mockCollection.Setup(c => c.FindOneAndUpdateAsync(
            It.IsAny<FilterDefinition<Notification>>(),
            It.IsAny<UpdateDefinition<Notification>>(),
            It.IsAny<FindOneAndUpdateOptions<Notification>>(),
            default))
            .ReturnsAsync(notification);

        //act
        var result = await _service.MarkAsReadAsync(notificationId, userId);

        //assert
        Assert.NotNull(result);
        Assert.True(result.IsRead);
    }

    [Fact]
    public async Task MarkAsReadAsync_ReturnsNull_WhenNotificationDoesNotExist()
    {
        //arrange
        var notificationId = "nonexistent";
        var userId = "user123";

        _mockCollection.Setup(c => c.FindOneAndUpdateAsync(
            It.IsAny<FilterDefinition<Notification>>(),
            It.IsAny<UpdateDefinition<Notification>>(),
            It.IsAny<FindOneAndUpdateOptions<Notification>>(),
            default))
            .ReturnsAsync((Notification)null!);

        //act
        var result = await _service.MarkAsReadAsync(notificationId, userId);

        //assert
        Assert.Null(result);
    }

    [Fact]
    public async Task MarkAsReadAsync_ReturnsNull_WhenUserDoesNotOwnNotification()
    {
        //arrange
        var notificationId = "notification123";
        var wrongUserId = "wrongUser";

        _mockCollection.Setup(c => c.FindOneAndUpdateAsync(
            It.IsAny<FilterDefinition<Notification>>(),
            It.IsAny<UpdateDefinition<Notification>>(),
            It.IsAny<FindOneAndUpdateOptions<Notification>>(),
            default))
            .ReturnsAsync((Notification)null!);

        //act
        var result = await _service.MarkAsReadAsync(notificationId, wrongUserId);

        //assert
        Assert.Null(result);
    }

    [Fact]
    public async Task MarkAllAsReadAsync_ReturnsTrue_WhenNotificationsAreMarkedAsRead()
    {
        //arrange
        var userId = "user123";
        var updateResult = new Mock<UpdateResult>();
        updateResult.Setup(r => r.ModifiedCount).Returns(3);

        _mockCollection.Setup(c => c.UpdateManyAsync(
            It.IsAny<FilterDefinition<Notification>>(),
            It.IsAny<UpdateDefinition<Notification>>(),
            null,
            default))
            .ReturnsAsync(updateResult.Object);

        //act
        var result = await _service.MarkAllAsReadAsync(userId);

        //assert
        Assert.True(result);
    }

    [Fact]
    public async Task MarkAllAsReadAsync_ReturnsFalse_WhenNoNotificationsAreModified()
    {
        //arrange
        var userId = "user123";
        var updateResult = new Mock<UpdateResult>();
        updateResult.Setup(r => r.ModifiedCount).Returns(0);

        _mockCollection.Setup(c => c.UpdateManyAsync(
            It.IsAny<FilterDefinition<Notification>>(),
            It.IsAny<UpdateDefinition<Notification>>(),
            null,
            default))
            .ReturnsAsync(updateResult.Object);

        //act
        var result = await _service.MarkAllAsReadAsync(userId);

        //assert
        Assert.False(result);
    }

    [Fact]
    public async Task DeleteNotificationAsync_ReturnsTrue_WhenNotificationIsDeleted()
    {
        //arrange
        var notificationId = "notification123";
        var userId = "user123";
        var deleteResult = new Mock<DeleteResult>();
        deleteResult.Setup(r => r.DeletedCount).Returns(1);

        _mockCollection.Setup(c => c.DeleteOneAsync(
            It.IsAny<FilterDefinition<Notification>>(),
            default))
            .ReturnsAsync(deleteResult.Object);

        //act
        var result = await _service.DeleteNotificationAsync(notificationId, userId);

        //assert
        Assert.True(result);
    }

    [Fact]
    public async Task DeleteNotificationAsync_ReturnsFalse_WhenNotificationDoesNotExist()
    {
        //arrange
        var notificationId = "nonexistent";
        var userId = "user123";
        var deleteResult = new Mock<DeleteResult>();
        deleteResult.Setup(r => r.DeletedCount).Returns(0);

        _mockCollection.Setup(c => c.DeleteOneAsync(
            It.IsAny<FilterDefinition<Notification>>(),
            default))
            .ReturnsAsync(deleteResult.Object);

        //act
        var result = await _service.DeleteNotificationAsync(notificationId, userId);

        //assert
        Assert.False(result);
    }

    [Fact]
    public async Task DeleteNotificationAsync_ReturnsFalse_WhenUserDoesNotOwnNotification()
    {
        //arrange
        var notificationId = "notification123";
        var wrongUserId = "wrongUser";
        var deleteResult = new Mock<DeleteResult>();
        deleteResult.Setup(r => r.DeletedCount).Returns(0);

        _mockCollection.Setup(c => c.DeleteOneAsync(
            It.IsAny<FilterDefinition<Notification>>(),
            default))
            .ReturnsAsync(deleteResult.Object);

        //act
        var result = await _service.DeleteNotificationAsync(notificationId, wrongUserId);

        //assert
        Assert.False(result);
    }
}
