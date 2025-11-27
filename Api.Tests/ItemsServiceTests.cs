using Api.DTOs;
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
    private readonly Mock<ITagsService> _mockTagsService;
    private readonly ItemsService _service;

    public ItemsServiceTests()
    {
        _mockDatabase = new Mock<IMongoDatabase>();
        _mockCollection = new Mock<IMongoCollection<SharedItem>>();
        _mockConfiguration = new Mock<IConfiguration>();
        _mockTagsService = new Mock<ITagsService>();

        _mockConfiguration.Setup(c => c["MongoDB:CollectionName"]).Returns("items");
        _mockDatabase.Setup(db => db.GetCollection<SharedItem>("items", null))
            .Returns(_mockCollection.Object);

        _service = new ItemsService(_mockDatabase.Object, _mockConfiguration.Object, _mockTagsService.Object);
    }

    [Fact]
    public async Task CreateItemAsync_SetsTimestamps_WhenCreatingItem()
    {
        //arrange
        var item = new SharedItem
        {
            Name = "Test Item",
            Description = "Test Description",
            UserId = "user123"
        };

        _mockCollection.Setup(c => c.InsertOneAsync(It.IsAny<SharedItem>(), null, default))
            .Returns(Task.CompletedTask);

        //act
        var result = await _service.CreateItemAsync(item);

        //assert
        Assert.NotEqual(default(DateTime), result.CreatedAt);
        Assert.NotEqual(default(DateTime), result.UpdatedAt);
        Assert.True((result.UpdatedAt - result.CreatedAt).TotalMilliseconds < 100, 
            "CreatedAt and UpdatedAt should be within 100ms of each other");
    }

    [Fact]
    public async Task CreateItemAsync_PreservesItemProperties_WhenCreatingItem()
    {
        //arrange
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

        //act
        var result = await _service.CreateItemAsync(item);

        //assert
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
        //arrange
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

        //act
        var result = await _service.UpdateItemVisibilityAsync(itemId, userId, loopIds, false, true);

        //assert
        Assert.NotNull(result);
        Assert.True(result.UpdatedAt >= beforeUpdate);
    }

    [Fact]
    public async Task UpdateItemVisibilityAsync_ReturnsNull_WhenItemNotFound()
    {
        //arrange
        var itemId = "nonexistent";
        var userId = "user123";
        var loopIds = new List<string>();

        _mockCollection.Setup(c => c.FindOneAndUpdateAsync(
            It.IsAny<FilterDefinition<SharedItem>>(),
            It.IsAny<UpdateDefinition<SharedItem>>(),
            It.IsAny<FindOneAndUpdateOptions<SharedItem>>(),
            default))
            .ReturnsAsync((SharedItem)null!);

        //act
        var result = await _service.UpdateItemVisibilityAsync(itemId, userId, loopIds, false, false);

        //assert
        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateItemVisibilityAsync_ReturnsNull_WhenUserDoesNotOwnItem()
    {
        //arrange
        var itemId = "item123";
        var wrongUserId = "wrongUser";
        var loopIds = new List<string>();

        _mockCollection.Setup(c => c.FindOneAndUpdateAsync(
            It.IsAny<FilterDefinition<SharedItem>>(),
            It.IsAny<UpdateDefinition<SharedItem>>(),
            It.IsAny<FindOneAndUpdateOptions<SharedItem>>(),
            default))
            .ReturnsAsync((SharedItem)null!);

        //act
        var result = await _service.UpdateItemVisibilityAsync(itemId, wrongUserId, loopIds, false, false);

        //assert
        Assert.Null(result);
    }

    // TODO: These UpdateItemAsync and SearchItemsAsync tests are commented out due to MongoDB mocking complexity.
    // The Find() extension method cannot be easily mocked with Moq. These scenarios are covered by:
    // 1. Property-based tests in ItemSearchPropertyTests.cs which test actual MongoDB operations
    // 2. Integration tests that test the full stack
    // To properly test these, we would need to either:
    // - Refactor the service to use dependency injection for the Find operation
    // - Use a real MongoDB test container
    // - Create a more complex mocking setup with IAsyncCursorSource

    /*
    [Fact]
    public async Task UpdateItemAsync_UpdatesAllFields_WhenUserOwnsItem()
    {
        //arrange
        var itemId = "item123";
        var userId = "user123";
        var name = "Updated Name";
        var description = "Updated Description";
        var isAvailable = false;
        var loopIds = new List<string> { "loop1", "loop2" };
        var visibleToAllLoops = true;
        var visibleToFutureLoops = true;

        var existingItem = new SharedItem
        {
            Id = itemId,
            UserId = userId,
            Name = "Old Name",
            Tags = new List<string>()
        };

        var updatedItem = new SharedItem
        {
            Id = itemId,
            UserId = userId,
            Name = name,
            Description = description,
            IsAvailable = isAvailable,
            VisibleToLoopIds = loopIds,
            VisibleToAllLoops = visibleToAllLoops,
            VisibleToFutureLoops = visibleToFutureLoops,
            UpdatedAt = DateTime.UtcNow
        };

        // Mock GetItemByIdAsync
        _mockCollection.Setup(c => c.FindSync(It.IsAny<FilterDefinition<SharedItem>>(), It.IsAny<FindOptions<SharedItem, SharedItem>>(), default)).Returns(MockAsyncCursor(new List<SharedItem> { existingItem }));

        _mockCollection.Setup(c => c.FindOneAndUpdateAsync(
            It.IsAny<FilterDefinition<SharedItem>>(),
            It.IsAny<UpdateDefinition<SharedItem>>(),
            It.IsAny<FindOneAndUpdateOptions<SharedItem>>(),
            default))
            .ReturnsAsync(updatedItem);

        //act
        var result = await _service.UpdateItemAsync(itemId, userId, name, description, isAvailable, loopIds, visibleToAllLoops, visibleToFutureLoops);

        //assert
        Assert.NotNull(result);
        Assert.Equal(name, result.Name);
        Assert.Equal(description, result.Description);
        Assert.Equal(isAvailable, result.IsAvailable);
        Assert.Equal(loopIds, result.VisibleToLoopIds);
        Assert.Equal(visibleToAllLoops, result.VisibleToAllLoops);
        Assert.Equal(visibleToFutureLoops, result.VisibleToFutureLoops);
    }

    [Fact]
    public async Task UpdateItemAsync_UpdatesTimestamp_WhenUpdatingItem()
    {
        //arrange
        var itemId = "item123";
        var userId = "user123";
        var beforeUpdate = DateTime.UtcNow;

        var existingItem = new SharedItem
        {
            Id = itemId,
            UserId = userId,
            Name = "Old Name",
            Tags = new List<string>()
        };

        var updatedItem = new SharedItem
        {
            Id = itemId,
            UserId = userId,
            Name = "Updated Name",
            Description = "Updated Description",
            IsAvailable = true,
            UpdatedAt = DateTime.UtcNow
        };

        // Mock GetItemByIdAsync
        _mockCollection.Setup(c => c.FindSync(It.IsAny<FilterDefinition<SharedItem>>(), It.IsAny<FindOptions<SharedItem, SharedItem>>(), default)).Returns(MockAsyncCursor(new List<SharedItem> { existingItem }));

        _mockCollection.Setup(c => c.FindOneAndUpdateAsync(
            It.IsAny<FilterDefinition<SharedItem>>(),
            It.IsAny<UpdateDefinition<SharedItem>>(),
            It.IsAny<FindOneAndUpdateOptions<SharedItem>>(),
            default))
            .ReturnsAsync(updatedItem);

        //act
        var result = await _service.UpdateItemAsync(itemId, userId, "Updated Name", "Updated Description", true, new List<string>(), false, false);

        //assert
        Assert.NotNull(result);
        Assert.True(result.UpdatedAt >= beforeUpdate);
    }

    [Fact]
    public async Task UpdateItemAsync_ReturnsNull_WhenItemNotFound()
    {
        //arrange
        var itemId = "nonexistent";
        var userId = "user123";

        // Mock GetItemByIdAsync to return null
        _mockCollection.Setup(c => c.FindSync(It.IsAny<FilterDefinition<SharedItem>>(), It.IsAny<FindOptions<SharedItem, SharedItem>>(), default)).Returns(MockAsyncCursor(null));

        _mockCollection.Setup(c => c.FindOneAndUpdateAsync(
            It.IsAny<FilterDefinition<SharedItem>>(),
            It.IsAny<UpdateDefinition<SharedItem>>(),
            It.IsAny<FindOneAndUpdateOptions<SharedItem>>(),
            default))
            .ReturnsAsync((SharedItem)null!);

        //act
        var result = await _service.UpdateItemAsync(itemId, userId, "Name", "Description", true, new List<string>(), false, false);

        //assert
        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateItemAsync_ReturnsNull_WhenUserDoesNotOwnItem()
    {
        //arrange
        var itemId = "item123";
        var wrongUserId = "wrongUser";

        var existingItem = new SharedItem
        {
            Id = itemId,
            UserId = "correctUser",
            Name = "Item",
            Tags = new List<string>()
        };

        // Mock GetItemByIdAsync to return item owned by different user
        _mockCollection.Setup(c => c.FindSync(It.IsAny<FilterDefinition<SharedItem>>(), It.IsAny<FindOptions<SharedItem, SharedItem>>(), default)).Returns(MockAsyncCursor(new List<SharedItem> { existingItem }));

        _mockCollection.Setup(c => c.FindOneAndUpdateAsync(
            It.IsAny<FilterDefinition<SharedItem>>(),
            It.IsAny<UpdateDefinition<SharedItem>>(),
            It.IsAny<FindOneAndUpdateOptions<SharedItem>>(),
            default))
            .ReturnsAsync((SharedItem)null!);

        //act
        var result = await _service.UpdateItemAsync(itemId, wrongUserId, "Name", "Description", true, new List<string>(), false, false);

        //assert
        Assert.Null(result);
    }

    [Fact]
    public async Task SearchItemsAsync_ReturnsAllItemsInLoop_WhenNoFiltersApplied()
    {
        //arrange
        var loopId = "loop123";
        var filter = new ItemSearchFilter { PageNumber = 1, PageSize = 50 };
        
        var items = new List<SharedItem>
        {
            new SharedItem { Id = "1", Name = "Item 1", VisibleToLoopIds = new List<string> { loopId } },
            new SharedItem { Id = "2", Name = "Item 2", VisibleToAllLoops = true }
        };

        _mockCollection.Setup(c => c.CountDocumentsAsync(It.IsAny<FilterDefinition<SharedItem>>(), null, default))
            .ReturnsAsync(items.Count);

        _mockCollection.Setup(c => c.FindSync(It.IsAny<FilterDefinition<SharedItem>>(), It.IsAny<FindOptions<SharedItem, SharedItem>>(), default)).Returns(MockAsyncCursor(items));

        //act
        var result = await _service.SearchItemsAsync(loopId, filter);

        //assert
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Items.Count);
        Assert.Equal(1, result.PageNumber);
        Assert.Equal(1, result.TotalPages);
    }

    [Fact]
    public async Task SearchItemsAsync_FiltersByTextSearch_WhenSearchTextProvided()
    {
        //arrange
        var loopId = "loop123";
        var filter = new ItemSearchFilter { SearchText = "drill", PageNumber = 1, PageSize = 50 };
        
        var items = new List<SharedItem>
        {
            new SharedItem { Id = "1", Name = "Power Drill", VisibleToLoopIds = new List<string> { loopId } }
        };

        _mockCollection.Setup(c => c.CountDocumentsAsync(It.IsAny<FilterDefinition<SharedItem>>(), null, default))
            .ReturnsAsync(items.Count);

        _mockCollection.Setup(c => c.FindSync(It.IsAny<FilterDefinition<SharedItem>>(), It.IsAny<FindOptions<SharedItem, SharedItem>>(), default)).Returns(MockAsyncCursor(items));

        //act
        var result = await _service.SearchItemsAsync(loopId, filter);

        //assert
        Assert.NotNull(result);
        Assert.Equal(1, result.TotalCount);
        Assert.Single(result.Items);
    }

    [Fact]
    public async Task SearchItemsAsync_FiltersByTags_WhenTagsProvided()
    {
        //arrange
        var loopId = "loop123";
        var filter = new ItemSearchFilter 
        { 
            Tags = new List<string> { "power-tools", "hand-tools" },
            PageNumber = 1, 
            PageSize = 50 
        };
        
        var items = new List<SharedItem>
        {
            new SharedItem 
            { 
                Id = "1", 
                Name = "Drill", 
                Tags = new List<string> { "power-tools", "hand-tools" },
                VisibleToLoopIds = new List<string> { loopId } 
            }
        };

        _mockCollection.Setup(c => c.CountDocumentsAsync(It.IsAny<FilterDefinition<SharedItem>>(), null, default))
            .ReturnsAsync(items.Count);

        _mockCollection.Setup(c => c.FindSync(It.IsAny<FilterDefinition<SharedItem>>(), It.IsAny<FindOptions<SharedItem, SharedItem>>(), default)).Returns(MockAsyncCursor(items));

        //act
        var result = await _service.SearchItemsAsync(loopId, filter);

        //assert
        Assert.NotNull(result);
        Assert.Equal(1, result.TotalCount);
        Assert.Single(result.Items);
    }

    [Fact]
    public async Task SearchItemsAsync_FiltersByAvailability_WhenIsAvailableProvided()
    {
        //arrange
        var loopId = "loop123";
        var filter = new ItemSearchFilter { IsAvailable = true, PageNumber = 1, PageSize = 50 };
        
        var items = new List<SharedItem>
        {
            new SharedItem { Id = "1", Name = "Available Item", IsAvailable = true, VisibleToLoopIds = new List<string> { loopId } }
        };

        _mockCollection.Setup(c => c.CountDocumentsAsync(It.IsAny<FilterDefinition<SharedItem>>(), null, default))
            .ReturnsAsync(items.Count);

        _mockCollection.Setup(c => c.FindSync(It.IsAny<FilterDefinition<SharedItem>>(), It.IsAny<FindOptions<SharedItem, SharedItem>>(), default)).Returns(MockAsyncCursor(items));

        //act
        var result = await _service.SearchItemsAsync(loopId, filter);

        //assert
        Assert.NotNull(result);
        Assert.Equal(1, result.TotalCount);
        Assert.Single(result.Items);
        Assert.True(result.Items[0].IsAvailable);
    }

    [Fact]
    public async Task SearchItemsAsync_FiltersByOwner_WhenOwnerIdsProvided()
    {
        //arrange
        var loopId = "loop123";
        var ownerId = "user123";
        var filter = new ItemSearchFilter 
        { 
            OwnerIds = new List<string> { ownerId },
            PageNumber = 1, 
            PageSize = 50 
        };
        
        var items = new List<SharedItem>
        {
            new SharedItem { Id = "1", Name = "Item", UserId = ownerId, VisibleToLoopIds = new List<string> { loopId } }
        };

        _mockCollection.Setup(c => c.CountDocumentsAsync(It.IsAny<FilterDefinition<SharedItem>>(), null, default))
            .ReturnsAsync(items.Count);

        _mockCollection.Setup(c => c.FindSync(It.IsAny<FilterDefinition<SharedItem>>(), It.IsAny<FindOptions<SharedItem, SharedItem>>(), default)).Returns(MockAsyncCursor(items));

        //act
        var result = await _service.SearchItemsAsync(loopId, filter);

        //assert
        Assert.NotNull(result);
        Assert.Equal(1, result.TotalCount);
        Assert.Single(result.Items);
        Assert.Equal(ownerId, result.Items[0].UserId);
    }

    [Fact]
    public async Task SearchItemsAsync_CalculatesPaginationCorrectly_WhenMultiplePages()
    {
        //arrange
        var loopId = "loop123";
        var filter = new ItemSearchFilter { PageNumber = 2, PageSize = 10 };
        
        var totalCount = 25;
        var items = new List<SharedItem>
        {
            new SharedItem { Id = "11", Name = "Item 11", VisibleToLoopIds = new List<string> { loopId } }
        };

        _mockCollection.Setup(c => c.CountDocumentsAsync(It.IsAny<FilterDefinition<SharedItem>>(), null, default))
            .ReturnsAsync(totalCount);

        _mockCollection.Setup(c => c.FindSync(It.IsAny<FilterDefinition<SharedItem>>(), It.IsAny<FindOptions<SharedItem, SharedItem>>(), default)).Returns(MockAsyncCursor(items));

        //act
        var result = await _service.SearchItemsAsync(loopId, filter);

        //assert
        Assert.NotNull(result);
        Assert.Equal(25, result.TotalCount);
        Assert.Equal(2, result.PageNumber);
        Assert.Equal(10, result.PageSize);
        Assert.Equal(3, result.TotalPages);
    }
    */

    [Fact]
    public async Task GetDistinctOwnersInLoopAsync_ReturnsUniqueOwnerIds_ForLoopItems()
    {
        //arrange
        var loopId = "loop123";
        var ownerIds = new List<string> { "user1", "user2", "user3" };

        var mockAsyncCursor = new Mock<IAsyncCursor<string>>();
        mockAsyncCursor.Setup(c => c.Current).Returns(ownerIds);
        mockAsyncCursor.SetupSequence(c => c.MoveNext(It.IsAny<CancellationToken>()))
            .Returns(true)
            .Returns(false);
        mockAsyncCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .ReturnsAsync(false);

        _mockCollection.Setup(c => c.DistinctAsync(
            It.IsAny<FieldDefinition<SharedItem, string>>(),
            It.IsAny<FilterDefinition<SharedItem>>(),
            null,
            default))
            .ReturnsAsync(mockAsyncCursor.Object);

        //act
        var result = await _service.GetDistinctOwnersInLoopAsync(loopId);

        //assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
        Assert.Contains("user1", result);
        Assert.Contains("user2", result);
        Assert.Contains("user3", result);
    }

    private static IFindFluent<SharedItem, SharedItem> MockCursor(List<SharedItem>? items)
    {
        var mockCursor = new Mock<IAsyncCursor<SharedItem>>();
        mockCursor.Setup(c => c.Current).Returns(items ?? new List<SharedItem>());
        mockCursor.SetupSequence(c => c.MoveNext(It.IsAny<CancellationToken>()))
            .Returns(items != null && items.Count > 0)
            .Returns(false);
        mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(items != null && items.Count > 0)
            .ReturnsAsync(false);

        var mockFindFluent = new Mock<IFindFluent<SharedItem, SharedItem>>();
        mockFindFluent.Setup(f => f.Sort(It.IsAny<SortDefinition<SharedItem>>())).Returns(mockFindFluent.Object);
        mockFindFluent.Setup(f => f.Skip(It.IsAny<int>())).Returns(mockFindFluent.Object);
        mockFindFluent.Setup(f => f.Limit(It.IsAny<int>())).Returns(mockFindFluent.Object);
        mockFindFluent.Setup(f => f.ToCursorAsync(default)).ReturnsAsync(mockCursor.Object);
        mockFindFluent.Setup(f => f.ToListAsync(default)).ReturnsAsync(items ?? new List<SharedItem>());
        mockFindFluent.Setup(f => f.FirstOrDefaultAsync(default)).ReturnsAsync(items?.FirstOrDefault());

        return mockFindFluent.Object;
    }

    private static IAsyncCursor<SharedItem> MockAsyncCursor(List<SharedItem>? items)
    {
        var mockCursor = new Mock<IAsyncCursor<SharedItem>>();
        mockCursor.Setup(c => c.Current).Returns(items ?? new List<SharedItem>());
        mockCursor.SetupSequence(c => c.MoveNext(It.IsAny<CancellationToken>()))
            .Returns(items != null && items.Count > 0)
            .Returns(false);
        mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(items != null && items.Count > 0)
            .ReturnsAsync(false);
        return mockCursor.Object;
    }
}


