using Api.Models;
using Api.Services;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using Moq;
using Xunit;

namespace Api.Tests;

public class TagsServiceTests
{
    private readonly Mock<IMongoDatabase> _mockDatabase;
    private readonly Mock<IMongoCollection<SystemTag>> _mockCollection;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly TagsService _service;

    public TagsServiceTests()
    {
        _mockDatabase = new Mock<IMongoDatabase>();
        _mockCollection = new Mock<IMongoCollection<SystemTag>>();
        _mockConfiguration = new Mock<IConfiguration>();

        _mockDatabase.Setup(db => db.GetCollection<SystemTag>("systemTags", null))
            .Returns(_mockCollection.Object);

        _service = new TagsService(_mockDatabase.Object, _mockConfiguration.Object);
    }

    [Fact]
    public async Task GetAllActiveTagsAsync_ReturnsOnlyActiveTags_WhenTagsExist()
    {
        //arrange
        var tags = new List<SystemTag>
        {
            new SystemTag { Name = "tag1", DisplayName = "Tag 1", IsActive = true },
            new SystemTag { Name = "tag2", DisplayName = "Tag 2", IsActive = true },
            new SystemTag { Name = "tag3", DisplayName = "Tag 3", IsActive = false }
        };

        var mockCursor = new Mock<IAsyncCursor<SystemTag>>();
        mockCursor.Setup(c => c.Current).Returns(tags.Where(t => t.IsActive));
        mockCursor.SetupSequence(c => c.MoveNext(It.IsAny<CancellationToken>()))
            .Returns(true)
            .Returns(false);
        mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .ReturnsAsync(false);

        _mockCollection.Setup(c => c.FindAsync(
            It.IsAny<FilterDefinition<SystemTag>>(),
            It.IsAny<FindOptions<SystemTag, SystemTag>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockCursor.Object);

        //act
        var result = await _service.GetAllActiveTagsAsync();

        //assert
        Assert.Equal(2, result.Count);
        Assert.All(result, tag => Assert.True(tag.IsActive));
    }

    [Fact]
    public async Task GetAllActiveTagsAsync_ReturnsSortedTags_WhenTagsExist()
    {
        //arrange
        var tags = new List<SystemTag>
        {
            new SystemTag { Name = "tag-c", DisplayName = "C Tag", IsActive = true },
            new SystemTag { Name = "tag-a", DisplayName = "A Tag", IsActive = true },
            new SystemTag { Name = "tag-b", DisplayName = "B Tag", IsActive = true }
        };

        var sortedTags = tags.OrderBy(t => t.DisplayName).ToList();

        var mockCursor = new Mock<IAsyncCursor<SystemTag>>();
        mockCursor.Setup(c => c.Current).Returns(sortedTags);
        mockCursor.SetupSequence(c => c.MoveNext(It.IsAny<CancellationToken>()))
            .Returns(true)
            .Returns(false);
        mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .ReturnsAsync(false);

        _mockCollection.Setup(c => c.FindAsync(
            It.IsAny<FilterDefinition<SystemTag>>(),
            It.IsAny<FindOptions<SystemTag, SystemTag>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockCursor.Object);

        //act
        var result = await _service.GetAllActiveTagsAsync();

        //assert
        Assert.Equal("A Tag", result[0].DisplayName);
        Assert.Equal("B Tag", result[1].DisplayName);
        Assert.Equal("C Tag", result[2].DisplayName);
    }

    [Fact]
    public async Task GetTagByNameAsync_ReturnsTag_WhenTagExists()
    {
        //arrange
        var tagName = "test-tag";
        var expectedTag = new SystemTag { Name = tagName, DisplayName = "Test Tag" };

        var mockCursor = new Mock<IAsyncCursor<SystemTag>>();
        mockCursor.Setup(c => c.Current).Returns(new List<SystemTag> { expectedTag });
        mockCursor.SetupSequence(c => c.MoveNext(It.IsAny<CancellationToken>()))
            .Returns(true)
            .Returns(false);
        mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .ReturnsAsync(false);

        _mockCollection.Setup(c => c.FindAsync(
            It.IsAny<FilterDefinition<SystemTag>>(),
            It.IsAny<FindOptions<SystemTag, SystemTag>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockCursor.Object);

        //act
        var result = await _service.GetTagByNameAsync(tagName);

        //assert
        Assert.NotNull(result);
        Assert.Equal(tagName, result.Name);
        Assert.Equal("Test Tag", result.DisplayName);
    }

    [Fact]
    public async Task GetTagByNameAsync_ReturnsNull_WhenTagDoesNotExist()
    {
        //arrange
        var tagName = "nonexistent-tag";

        var mockCursor = new Mock<IAsyncCursor<SystemTag>>();
        mockCursor.Setup(c => c.Current).Returns(new List<SystemTag>());
        mockCursor.SetupSequence(c => c.MoveNext(It.IsAny<CancellationToken>()))
            .Returns(true)
            .Returns(false);
        mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .ReturnsAsync(false);

        _mockCollection.Setup(c => c.FindAsync(
            It.IsAny<FilterDefinition<SystemTag>>(),
            It.IsAny<FindOptions<SystemTag, SystemTag>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockCursor.Object);

        //act
        var result = await _service.GetTagByNameAsync(tagName);

        //assert
        Assert.Null(result);
    }

    [Fact]
    public async Task InitializeDefaultTagsAsync_InsertsDefaultTags_WhenNoTagsExist()
    {
        //arrange
        _mockCollection.Setup(c => c.CountDocumentsAsync(
            It.IsAny<FilterDefinition<SystemTag>>(),
            null,
            default))
            .ReturnsAsync(0);

        _mockCollection.Setup(c => c.InsertManyAsync(
            It.IsAny<IEnumerable<SystemTag>>(),
            null,
            default))
            .Returns(Task.CompletedTask);

        //act
        await _service.InitializeDefaultTagsAsync();

        //assert
        _mockCollection.Verify(c => c.InsertManyAsync(
            It.Is<IEnumerable<SystemTag>>(tags => tags.Count() > 0),
            null,
            default), Times.Once);
    }

    [Fact]
    public async Task InitializeDefaultTagsAsync_DoesNotInsertTags_WhenTagsAlreadyExist()
    {
        //arrange
        _mockCollection.Setup(c => c.CountDocumentsAsync(
            It.IsAny<FilterDefinition<SystemTag>>(),
            null,
            default))
            .ReturnsAsync(10);

        //act
        await _service.InitializeDefaultTagsAsync();

        //assert
        _mockCollection.Verify(c => c.InsertManyAsync(
            It.IsAny<IEnumerable<SystemTag>>(),
            null,
            default), Times.Never);
    }

    [Fact]
    public async Task IncrementTagUsageAsync_IncrementsUsageCount_WhenTagExists()
    {
        //arrange
        var tagName = "test-tag";

        _mockCollection.Setup(c => c.UpdateOneAsync(
            It.IsAny<FilterDefinition<SystemTag>>(),
            It.IsAny<UpdateDefinition<SystemTag>>(),
            null,
            default))
            .ReturnsAsync(new UpdateResult.Acknowledged(1, 1, null));

        //act
        await _service.IncrementTagUsageAsync(tagName);

        //assert
        _mockCollection.Verify(c => c.UpdateOneAsync(
            It.IsAny<FilterDefinition<SystemTag>>(),
            It.IsAny<UpdateDefinition<SystemTag>>(),
            null,
            default), Times.Once);
    }

    [Fact]
    public async Task DecrementTagUsageAsync_DecrementsUsageCount_WhenTagExists()
    {
        //arrange
        var tagName = "test-tag";

        _mockCollection.Setup(c => c.UpdateOneAsync(
            It.IsAny<FilterDefinition<SystemTag>>(),
            It.IsAny<UpdateDefinition<SystemTag>>(),
            null,
            default))
            .ReturnsAsync(new UpdateResult.Acknowledged(1, 1, null));

        //act
        await _service.DecrementTagUsageAsync(tagName);

        //assert
        _mockCollection.Verify(c => c.UpdateOneAsync(
            It.IsAny<FilterDefinition<SystemTag>>(),
            It.IsAny<UpdateDefinition<SystemTag>>(),
            null,
            default), Times.Once);
    }

    [Fact]
    public async Task GetTagUsageStatisticsAsync_ReturnsDictionary_WhenTagsExist()
    {
        //arrange
        var tags = new List<SystemTag>
        {
            new SystemTag { Name = "tag1", DisplayName = "Tag 1", UsageCount = 5 },
            new SystemTag { Name = "tag2", DisplayName = "Tag 2", UsageCount = 10 },
            new SystemTag { Name = "tag3", DisplayName = "Tag 3", UsageCount = 0 }
        };

        var mockCursor = new Mock<IAsyncCursor<SystemTag>>();
        mockCursor.Setup(c => c.Current).Returns(tags);
        mockCursor.SetupSequence(c => c.MoveNext(It.IsAny<CancellationToken>()))
            .Returns(true)
            .Returns(false);
        mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .ReturnsAsync(false);

        _mockCollection.Setup(c => c.FindAsync(
            It.IsAny<FilterDefinition<SystemTag>>(),
            It.IsAny<FindOptions<SystemTag, SystemTag>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockCursor.Object);

        //act
        var result = await _service.GetTagUsageStatisticsAsync();

        //assert
        Assert.Equal(3, result.Count);
        Assert.Equal(5, result["tag1"]);
        Assert.Equal(10, result["tag2"]);
        Assert.Equal(0, result["tag3"]);
    }

    [Fact]
    public async Task GetTagUsageStatisticsAsync_ReturnsEmptyDictionary_WhenNoTagsExist()
    {
        //arrange
        var mockCursor = new Mock<IAsyncCursor<SystemTag>>();
        mockCursor.Setup(c => c.Current).Returns(new List<SystemTag>());
        mockCursor.SetupSequence(c => c.MoveNext(It.IsAny<CancellationToken>()))
            .Returns(true)
            .Returns(false);
        mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .ReturnsAsync(false);

        _mockCollection.Setup(c => c.FindAsync(
            It.IsAny<FilterDefinition<SystemTag>>(),
            It.IsAny<FindOptions<SystemTag, SystemTag>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockCursor.Object);

        //act
        var result = await _service.GetTagUsageStatisticsAsync();

        //assert
        Assert.Empty(result);
    }
}
