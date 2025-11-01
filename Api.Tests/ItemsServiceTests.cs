using Api.Models;
using Api.Services;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using Moq;
using Xunit;

namespace Api.Tests;

public class ItemsServiceTests
{
    private readonly Mock<IMongoDatabase> _mockDatabase;
    private readonly Mock<IMongoCollection<SharedItem>> _mockCollection;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly ItemsService _service;

    public ItemsServiceTests()
    {
        _mockDatabase = new Mock<IMongoDatabase>();
        _mockCollection = new Mock<IMongoCollection<SharedItem>>();
        _mockConfiguration = new Mock<IConfiguration>();

        _mockConfiguration.Setup(c => c["MongoDB:CollectionName"]).Returns("items");
        _mockDatabase.Setup(db => db.GetCollection<SharedItem>("items", null))
            .Returns(_mockCollection.Object);

        _service = new ItemsService(_mockDatabase.Object, _mockConfiguration.Object);
    }

    [Fact]
    public async Task CreateItemAsync_SetsTimestamps_WhenCreatingItem()
    {
        // Arrange
        var item = new SharedItem
        {
            Name = "Test Item",
            Description = "Test Description",
            UserId = "user123"
        };

        _mockCollection.Setup(c => c.InsertOneAsync(It.IsAny<SharedItem>(), null, default))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.CreateItemAsync(item);

        // Assert
        Assert.NotEqual(default(DateTime), result.CreatedAt);
        Assert.NotEqual(default(DateTime), result.UpdatedAt);
        Assert.True((result.UpdatedAt - result.CreatedAt).TotalMilliseconds < 100, 
            "CreatedAt and UpdatedAt should be within 100ms of each other");
    }

    [Fact]
    public async Task CreateItemAsync_PreservesItemProperties_WhenCreatingItem()
    {
        // Arrange
        var item = new SharedItem
        {
            Name = "Test Item",
            Description = "Test Description",
            UserId = "user123",
            VisibleToLoopIds = new List<string> { "loop1", "loop2" },
            VisibleToAllLoops = true,
            VisibleToFutureLoops = false
        };

        _mockCollection.Setup(c => c.InsertOneAsync(It.IsAny<SharedItem>(), null, default))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.CreateItemAsync(item);

        // Assert
        Assert.Equal("Test Item", result.Name);
        Assert.Equal("Test Description", result.Description);
        Assert.Equal("user123", result.UserId);
        Assert.Equal(2, result.VisibleToLoopIds.Count);
        Assert.True(result.VisibleToAllLoops);
        Assert.False(result.VisibleToFutureLoops);
    }

    [Fact]
    public async Task UpdateItemVisibilityAsync_UpdatesTimestamp_WhenUpdatingVisibility()
    {
        // Arrange
        var itemId = "item123";
        var userId = "user123";
        var loopIds = new List<string> { "loop1" };
        var beforeUpdate = DateTime.UtcNow;

        var updatedItem = new SharedItem
        {
            Id = itemId,
            UserId = userId,
            VisibleToLoopIds = loopIds,
            VisibleToAllLoops = false,
            VisibleToFutureLoops = true,
            UpdatedAt = DateTime.UtcNow
        };

        _mockCollection.Setup(c => c.FindOneAndUpdateAsync(
            It.IsAny<FilterDefinition<SharedItem>>(),
            It.IsAny<UpdateDefinition<SharedItem>>(),
            It.IsAny<FindOneAndUpdateOptions<SharedItem>>(),
            default))
            .ReturnsAsync(updatedItem);

        // Act
        var result = await _service.UpdateItemVisibilityAsync(itemId, userId, loopIds, false, true);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.UpdatedAt >= beforeUpdate);
    }

    [Fact]
    public async Task UpdateItemVisibilityAsync_ReturnsNull_WhenItemNotFound()
    {
        // Arrange
        var itemId = "nonexistent";
        var userId = "user123";
        var loopIds = new List<string>();

        _mockCollection.Setup(c => c.FindOneAndUpdateAsync(
            It.IsAny<FilterDefinition<SharedItem>>(),
            It.IsAny<UpdateDefinition<SharedItem>>(),
            It.IsAny<FindOneAndUpdateOptions<SharedItem>>(),
            default))
            .ReturnsAsync((SharedItem)null!);

        // Act
        var result = await _service.UpdateItemVisibilityAsync(itemId, userId, loopIds, false, false);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateItemVisibilityAsync_ReturnsNull_WhenUserDoesNotOwnItem()
    {
        // Arrange
        var itemId = "item123";
        var wrongUserId = "wrongUser";
        var loopIds = new List<string>();

        _mockCollection.Setup(c => c.FindOneAndUpdateAsync(
            It.IsAny<FilterDefinition<SharedItem>>(),
            It.IsAny<UpdateDefinition<SharedItem>>(),
            It.IsAny<FindOneAndUpdateOptions<SharedItem>>(),
            default))
            .ReturnsAsync((SharedItem)null!);

        // Act
        var result = await _service.UpdateItemVisibilityAsync(itemId, wrongUserId, loopIds, false, false);

        // Assert
        Assert.Null(result);
    }
}
