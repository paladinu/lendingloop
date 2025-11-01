using Api.Models;
using Api.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Moq;
using Xunit;

namespace Api.Tests;

public class LoopServiceTests
{
    private readonly Mock<IMongoDatabase> _mockDatabase;
    private readonly Mock<IMongoCollection<Loop>> _mockLoopsCollection;
    private readonly Mock<IMongoCollection<User>> _mockUsersCollection;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<ILogger<LoopService>> _mockLogger;
    private readonly LoopService _service;

    public LoopServiceTests()
    {
        _mockDatabase = new Mock<IMongoDatabase>();
        _mockLoopsCollection = new Mock<IMongoCollection<Loop>>();
        _mockUsersCollection = new Mock<IMongoCollection<User>>();
        _mockConfiguration = new Mock<IConfiguration>();
        _mockLogger = new Mock<ILogger<LoopService>>();

        _mockConfiguration.Setup(c => c["MongoDB:LoopsCollectionName"]).Returns("loops");
        _mockConfiguration.Setup(c => c["MongoDB:UsersCollectionName"]).Returns("users");
        
        _mockDatabase.Setup(db => db.GetCollection<Loop>("loops", null))
            .Returns(_mockLoopsCollection.Object);
        _mockDatabase.Setup(db => db.GetCollection<User>("users", null))
            .Returns(_mockUsersCollection.Object);

        _service = new LoopService(_mockDatabase.Object, _mockConfiguration.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task CreateLoopAsync_CreatesLoop_WithCreatorAsMember()
    {
        // Arrange
        var name = "Test Loop";
        var creatorId = "user123";

        _mockLoopsCollection.Setup(c => c.InsertOneAsync(It.IsAny<Loop>(), null, default))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.CreateLoopAsync(name, creatorId);

        // Assert
        Assert.Equal(name, result.Name);
        Assert.Equal(creatorId, result.CreatorId);
        Assert.Contains(creatorId, result.MemberIds);
        Assert.Single(result.MemberIds);
        Assert.NotEqual(default(DateTime), result.CreatedAt);
        Assert.NotEqual(default(DateTime), result.UpdatedAt);
    }

    [Fact]
    public async Task GetLoopByIdAsync_ReturnsLoop_WhenLoopExists()
    {
        // Arrange
        var loopId = "loop123";
        var expectedLoop = new Loop { Id = loopId, Name = "Test Loop" };

        var mockCursor = new Mock<IAsyncCursor<Loop>>();
        mockCursor.Setup(c => c.Current).Returns(new List<Loop> { expectedLoop });
        mockCursor.SetupSequence(c => c.MoveNext(It.IsAny<CancellationToken>()))
            .Returns(true)
            .Returns(false);
        mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .ReturnsAsync(false);

        _mockLoopsCollection.Setup(c => c.FindAsync(
            It.IsAny<FilterDefinition<Loop>>(),
            It.IsAny<FindOptions<Loop, Loop>>(),
            default))
            .ReturnsAsync(mockCursor.Object);

        // Act
        var result = await _service.GetLoopByIdAsync(loopId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(loopId, result.Id);
    }

    [Fact]
    public async Task GetLoopByIdAsync_ReturnsNull_WhenLoopDoesNotExist()
    {
        // Arrange
        var loopId = "nonexistent";

        var mockCursor = new Mock<IAsyncCursor<Loop>>();
        mockCursor.Setup(c => c.Current).Returns(new List<Loop>());
        mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _mockLoopsCollection.Setup(c => c.FindAsync(
            It.IsAny<FilterDefinition<Loop>>(),
            It.IsAny<FindOptions<Loop, Loop>>(),
            default))
            .ReturnsAsync(mockCursor.Object);

        // Act
        var result = await _service.GetLoopByIdAsync(loopId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetUserLoopsAsync_ReturnsLoops_WhenUserIsMember()
    {
        // Arrange
        var userId = "user123";
        var loops = new List<Loop>
        {
            new Loop { Id = "loop1", Name = "Loop 1", MemberIds = new List<string> { userId } },
            new Loop { Id = "loop2", Name = "Loop 2", MemberIds = new List<string> { userId, "user456" } }
        };

        var mockCursor = new Mock<IAsyncCursor<Loop>>();
        mockCursor.Setup(c => c.Current).Returns(loops);
        mockCursor.SetupSequence(c => c.MoveNext(It.IsAny<CancellationToken>()))
            .Returns(true)
            .Returns(false);
        mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .ReturnsAsync(false);

        _mockLoopsCollection.Setup(c => c.FindAsync(
            It.IsAny<FilterDefinition<Loop>>(),
            It.IsAny<FindOptions<Loop, Loop>>(),
            default))
            .ReturnsAsync(mockCursor.Object);

        // Act
        var result = await _service.GetUserLoopsAsync(userId);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, loop => Assert.Contains(userId, loop.MemberIds));
    }

    [Fact]
    public async Task GetLoopMembersAsync_ReturnsMembers_WhenLoopExists()
    {
        // Arrange
        var loopId = "loop123";
        var memberIds = new List<string> { "user1", "user2" };
        var loop = new Loop { Id = loopId, MemberIds = memberIds };
        var users = new List<User>
        {
            new User { Id = "user1", Email = "user1@example.com" },
            new User { Id = "user2", Email = "user2@example.com" }
        };

        var mockLoopCursor = new Mock<IAsyncCursor<Loop>>();
        mockLoopCursor.Setup(c => c.Current).Returns(new List<Loop> { loop });
        mockLoopCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .ReturnsAsync(false);

        _mockLoopsCollection.Setup(c => c.FindAsync(
            It.IsAny<FilterDefinition<Loop>>(),
            It.IsAny<FindOptions<Loop, Loop>>(),
            default))
            .ReturnsAsync(mockLoopCursor.Object);

        var mockUserCursor = new Mock<IAsyncCursor<User>>();
        mockUserCursor.Setup(c => c.Current).Returns(users);
        mockUserCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .ReturnsAsync(false);

        _mockUsersCollection.Setup(c => c.FindAsync(
            It.IsAny<FilterDefinition<User>>(),
            It.IsAny<FindOptions<User, User>>(),
            default))
            .ReturnsAsync(mockUserCursor.Object);

        // Act
        var result = await _service.GetLoopMembersAsync(loopId);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, u => u.Id == "user1");
        Assert.Contains(result, u => u.Id == "user2");
    }

    [Fact]
    public async Task GetLoopMembersAsync_ReturnsEmptyList_WhenLoopDoesNotExist()
    {
        // Arrange
        var loopId = "nonexistent";

        var mockCursor = new Mock<IAsyncCursor<Loop>>();
        mockCursor.Setup(c => c.Current).Returns(new List<Loop>());
        mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _mockLoopsCollection.Setup(c => c.FindAsync(
            It.IsAny<FilterDefinition<Loop>>(),
            It.IsAny<FindOptions<Loop, Loop>>(),
            default))
            .ReturnsAsync(mockCursor.Object);

        // Act
        var result = await _service.GetLoopMembersAsync(loopId);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task IsUserLoopMemberAsync_ReturnsTrue_WhenUserIsMember()
    {
        // Arrange
        var loopId = "loop123";
        var userId = "user123";
        var loop = new Loop { Id = loopId, MemberIds = new List<string> { userId } };

        var mockCursor = new Mock<IAsyncCursor<Loop>>();
        mockCursor.Setup(c => c.Current).Returns(new List<Loop> { loop });
        mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .ReturnsAsync(false);

        _mockLoopsCollection.Setup(c => c.FindAsync(
            It.IsAny<FilterDefinition<Loop>>(),
            It.IsAny<FindOptions<Loop, Loop>>(),
            default))
            .ReturnsAsync(mockCursor.Object);

        // Act
        var result = await _service.IsUserLoopMemberAsync(loopId, userId);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task IsUserLoopMemberAsync_ReturnsFalse_WhenUserIsNotMember()
    {
        // Arrange
        var loopId = "loop123";
        var userId = "user123";
        var loop = new Loop { Id = loopId, MemberIds = new List<string> { "otherUser" } };

        var mockCursor = new Mock<IAsyncCursor<Loop>>();
        mockCursor.Setup(c => c.Current).Returns(new List<Loop> { loop });
        mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .ReturnsAsync(false);

        _mockLoopsCollection.Setup(c => c.FindAsync(
            It.IsAny<FilterDefinition<Loop>>(),
            It.IsAny<FindOptions<Loop, Loop>>(),
            default))
            .ReturnsAsync(mockCursor.Object);

        // Act
        var result = await _service.IsUserLoopMemberAsync(loopId, userId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task AddMemberToLoopAsync_AddsUser_WhenLoopExists()
    {
        // Arrange
        var loopId = "loop123";
        var userId = "user123";
        var updatedLoop = new Loop
        {
            Id = loopId,
            MemberIds = new List<string> { "existingUser", userId }
        };

        _mockLoopsCollection.Setup(c => c.FindOneAndUpdateAsync(
            It.IsAny<FilterDefinition<Loop>>(),
            It.IsAny<UpdateDefinition<Loop>>(),
            It.IsAny<FindOneAndUpdateOptions<Loop>>(),
            default))
            .ReturnsAsync(updatedLoop);

        // Act
        var result = await _service.AddMemberToLoopAsync(loopId, userId);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(userId, result.MemberIds);
    }

    [Fact]
    public async Task AddMemberToLoopAsync_ReturnsNull_WhenLoopDoesNotExist()
    {
        // Arrange
        var loopId = "nonexistent";
        var userId = "user123";

        _mockLoopsCollection.Setup(c => c.FindOneAndUpdateAsync(
            It.IsAny<FilterDefinition<Loop>>(),
            It.IsAny<UpdateDefinition<Loop>>(),
            It.IsAny<FindOneAndUpdateOptions<Loop>>(),
            default))
            .ReturnsAsync((Loop)null!);

        // Act
        var result = await _service.AddMemberToLoopAsync(loopId, userId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task RemoveMemberFromLoopAsync_RemovesUser_WhenLoopExists()
    {
        // Arrange
        var loopId = "loop123";
        var userId = "user123";
        var updatedLoop = new Loop
        {
            Id = loopId,
            MemberIds = new List<string> { "remainingUser" }
        };

        _mockLoopsCollection.Setup(c => c.FindOneAndUpdateAsync(
            It.IsAny<FilterDefinition<Loop>>(),
            It.IsAny<UpdateDefinition<Loop>>(),
            It.IsAny<FindOneAndUpdateOptions<Loop>>(),
            default))
            .ReturnsAsync(updatedLoop);

        // Act
        var result = await _service.RemoveMemberFromLoopAsync(loopId, userId);

        // Assert
        Assert.NotNull(result);
        Assert.DoesNotContain(userId, result.MemberIds);
    }

    [Fact]
    public async Task GetPotentialInviteesFromOtherLoopsAsync_ReturnsUsers_FromOtherLoops()
    {
        // Arrange
        var userId = "user123";
        var currentLoopId = "loop1";
        var userLoops = new List<Loop>
        {
            new Loop { Id = "loop1", MemberIds = new List<string> { userId, "user2" } },
            new Loop { Id = "loop2", MemberIds = new List<string> { userId, "user3", "user4" } }
        };
        var currentLoop = userLoops[0];
        var potentialInvitees = new List<User>
        {
            new User { Id = "user3", Email = "user3@example.com" },
            new User { Id = "user4", Email = "user4@example.com" }
        };

        // Mock GetUserLoopsAsync
        var mockLoopsCursor = new Mock<IAsyncCursor<Loop>>();
        mockLoopsCursor.Setup(c => c.Current).Returns(userLoops);
        mockLoopsCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .ReturnsAsync(false);

        // Mock GetLoopByIdAsync
        var mockCurrentLoopCursor = new Mock<IAsyncCursor<Loop>>();
        mockCurrentLoopCursor.Setup(c => c.Current).Returns(new List<Loop> { currentLoop });
        mockCurrentLoopCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .ReturnsAsync(false);

        _mockLoopsCollection.SetupSequence(c => c.FindAsync(
            It.IsAny<FilterDefinition<Loop>>(),
            It.IsAny<FindOptions<Loop, Loop>>(),
            default))
            .ReturnsAsync(mockLoopsCursor.Object)
            .ReturnsAsync(mockCurrentLoopCursor.Object);

        // Mock users query
        var mockUsersCursor = new Mock<IAsyncCursor<User>>();
        mockUsersCursor.Setup(c => c.Current).Returns(potentialInvitees);
        mockUsersCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .ReturnsAsync(false);

        _mockUsersCollection.Setup(c => c.FindAsync(
            It.IsAny<FilterDefinition<User>>(),
            It.IsAny<FindOptions<User, User>>(),
            default))
            .ReturnsAsync(mockUsersCursor.Object);

        // Act
        var result = await _service.GetPotentialInviteesFromOtherLoopsAsync(userId, currentLoopId);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, u => u.Id == "user3");
        Assert.Contains(result, u => u.Id == "user4");
        Assert.DoesNotContain(result, u => u.Id == userId);
        Assert.DoesNotContain(result, u => u.Id == "user2");
    }
}
