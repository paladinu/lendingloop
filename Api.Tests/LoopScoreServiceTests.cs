using Api.Models;
using Api.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Moq;
using Xunit;

namespace Api.Tests;

public class LoopScoreServiceTests
{
    private readonly Mock<IMongoDatabase> _mockDatabase;
    private readonly Mock<IMongoCollection<User>> _mockUsersCollection;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<ILogger<LoopScoreService>> _mockLogger;
    private readonly Mock<IEmailService> _mockEmailService;
    private readonly LoopScoreService _service;

    public LoopScoreServiceTests()
    {
        _mockDatabase = new Mock<IMongoDatabase>();
        _mockUsersCollection = new Mock<IMongoCollection<User>>();
        _mockConfiguration = new Mock<IConfiguration>();
        _mockLogger = new Mock<ILogger<LoopScoreService>>();
        _mockEmailService = new Mock<IEmailService>();

        _mockConfiguration.Setup(c => c["MongoDB:UsersCollectionName"]).Returns("users");
        _mockDatabase.Setup(db => db.GetCollection<User>("users", null))
            .Returns(_mockUsersCollection.Object);

        _service = new LoopScoreService(_mockDatabase.Object, _mockConfiguration.Object, _mockLogger.Object, _mockEmailService.Object);
    }

    [Fact]
    public async Task AwardBorrowPointsAsync_IncreasesScoreByOne_AndCreatesHistoryEntry()
    {
        //arrange
        var userId = "user123";
        var itemRequestId = "request123";
        var itemName = "Test Item";
        
        var userAfterUpdate = new User
        {
            Id = userId,
            Email = "test@example.com",
            LoopScore = 1,
            Badges = new List<BadgeAward>()
        };
        
        _mockUsersCollection
            .Setup(c => c.FindOneAndUpdateAsync(
                It.IsAny<FilterDefinition<User>>(),
                It.IsAny<UpdateDefinition<User>>(),
                It.IsAny<FindOneAndUpdateOptions<User>>(),
                default))
            .ReturnsAsync(userAfterUpdate);

        //act
        await _service.AwardBorrowPointsAsync(userId, itemRequestId, itemName);

        //assert
        _mockUsersCollection.Verify(c => c.FindOneAndUpdateAsync(
            It.IsAny<FilterDefinition<User>>(),
            It.IsAny<UpdateDefinition<User>>(),
            It.IsAny<FindOneAndUpdateOptions<User>>(),
            default), Times.AtLeastOnce);
    }

    [Fact]
    public async Task AwardLendPointsAsync_IncreasesScoreByFour_AndCreatesHistoryEntry()
    {
        //arrange
        var userId = "user123";
        var itemRequestId = "request123";
        var itemName = "Test Item";
        
        var userAfterUpdate = new User
        {
            Id = userId,
            Email = "test@example.com",
            LoopScore = 4,
            Badges = new List<BadgeAward>()
        };
        
        _mockUsersCollection
            .Setup(c => c.FindOneAndUpdateAsync(
                It.IsAny<FilterDefinition<User>>(),
                It.IsAny<UpdateDefinition<User>>(),
                It.IsAny<FindOneAndUpdateOptions<User>>(),
                default))
            .ReturnsAsync(userAfterUpdate);

        //act
        await _service.AwardLendPointsAsync(userId, itemRequestId, itemName);

        //assert
        _mockUsersCollection.Verify(c => c.FindOneAndUpdateAsync(
            It.IsAny<FilterDefinition<User>>(),
            It.IsAny<UpdateDefinition<User>>(),
            It.IsAny<FindOneAndUpdateOptions<User>>(),
            default), Times.AtLeastOnce);
    }

    [Fact]
    public async Task ReverseLendPointsAsync_DecreasesScore_ButNotBelowZero()
    {
        //arrange
        var userId = "user123";
        var itemRequestId = "request123";
        var itemName = "Test Item";
        
        // First call returns negative score
        var userAfterFirstUpdate = new User
        {
            Id = userId,
            Email = "test@example.com",
            LoopScore = -4,
            Badges = new List<BadgeAward>()
        };
        
        // Second call returns score set to 0
        var userAfterSecondUpdate = new User
        {
            Id = userId,
            Email = "test@example.com",
            LoopScore = 0,
            Badges = new List<BadgeAward>()
        };
        
        _mockUsersCollection
            .SetupSequence(c => c.FindOneAndUpdateAsync(
                It.IsAny<FilterDefinition<User>>(),
                It.IsAny<UpdateDefinition<User>>(),
                It.IsAny<FindOneAndUpdateOptions<User>>(),
                default))
            .ReturnsAsync(userAfterFirstUpdate)
            .ReturnsAsync(userAfterSecondUpdate);

        //act
        await _service.ReverseLendPointsAsync(userId, itemRequestId, itemName);

        //assert
        // Verify that FindOneAndUpdateAsync is called twice: once for the decrement, once for ensuring minimum
        _mockUsersCollection.Verify(c => c.FindOneAndUpdateAsync(
            It.IsAny<FilterDefinition<User>>(),
            It.IsAny<UpdateDefinition<User>>(),
            It.IsAny<FindOneAndUpdateOptions<User>>(),
            default), Times.AtLeast(2));
    }

    [Fact]
    public async Task GetUserScoreAsync_ReturnsCurrentScore()
    {
        //arrange
        var userId = "user123";
        var user = new User
        {
            Id = userId,
            Email = "test@example.com",
            LoopScore = 10
        };

        var mockCursor = new Mock<IAsyncCursor<User>>();
        mockCursor.Setup(c => c.Current).Returns(new List<User> { user });
        mockCursor.SetupSequence(c => c.MoveNext(It.IsAny<CancellationToken>()))
            .Returns(true)
            .Returns(false);
        mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .ReturnsAsync(false);

        _mockUsersCollection
            .Setup(c => c.FindAsync(
                It.IsAny<FilterDefinition<User>>(),
                It.IsAny<FindOptions<User, User>>(),
                default))
            .ReturnsAsync(mockCursor.Object);

        //act
        var score = await _service.GetUserScoreAsync(userId);

        //assert
        Assert.Equal(10, score);
    }

    [Fact]
    public async Task GetUserScoreAsync_ReturnsZero_WhenUserNotFound()
    {
        //arrange
        var userId = "nonexistent";

        var mockCursor = new Mock<IAsyncCursor<User>>();
        mockCursor.Setup(c => c.Current).Returns(new List<User>());
        mockCursor.SetupSequence(c => c.MoveNext(It.IsAny<CancellationToken>()))
            .Returns(false);
        mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _mockUsersCollection
            .Setup(c => c.FindAsync(
                It.IsAny<FilterDefinition<User>>(),
                It.IsAny<FindOptions<User, User>>(),
                default))
            .ReturnsAsync(mockCursor.Object);

        //act
        var score = await _service.GetUserScoreAsync(userId);

        //assert
        Assert.Equal(0, score);
    }

    [Fact]
    public async Task GetScoreHistoryAsync_ReturnsRecentEntries_InDescendingOrder()
    {
        //arrange
        var userId = "user123";
        var history = new List<ScoreHistoryEntry>
        {
            new ScoreHistoryEntry
            {
                Timestamp = DateTime.UtcNow.AddDays(-2),
                Points = 1,
                ActionType = ScoreActionType.BorrowCompleted,
                ItemRequestId = "req1",
                ItemName = "Item 1"
            },
            new ScoreHistoryEntry
            {
                Timestamp = DateTime.UtcNow.AddDays(-1),
                Points = 4,
                ActionType = ScoreActionType.LendApproved,
                ItemRequestId = "req2",
                ItemName = "Item 2"
            },
            new ScoreHistoryEntry
            {
                Timestamp = DateTime.UtcNow,
                Points = 1,
                ActionType = ScoreActionType.OnTimeReturn,
                ItemRequestId = "req3",
                ItemName = "Item 3"
            }
        };

        var user = new User
        {
            Id = userId,
            Email = "test@example.com",
            LoopScore = 6,
            ScoreHistory = history
        };

        var mockCursor = new Mock<IAsyncCursor<User>>();
        mockCursor.Setup(c => c.Current).Returns(new List<User> { user });
        mockCursor.SetupSequence(c => c.MoveNext(It.IsAny<CancellationToken>()))
            .Returns(true)
            .Returns(false);
        mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .ReturnsAsync(false);

        _mockUsersCollection
            .Setup(c => c.FindAsync(
                It.IsAny<FilterDefinition<User>>(),
                It.IsAny<FindOptions<User, User>>(),
                default))
            .ReturnsAsync(mockCursor.Object);

        //act
        var result = await _service.GetScoreHistoryAsync(userId, 10);

        //assert
        Assert.Equal(3, result.Count);
        Assert.Equal("Item 3", result[0].ItemName); // Most recent first
        Assert.Equal("Item 2", result[1].ItemName);
        Assert.Equal("Item 1", result[2].ItemName);
    }

    [Fact]
    public async Task GetScoreHistoryAsync_ReturnsEmptyList_WhenUserNotFound()
    {
        //arrange
        var userId = "nonexistent";

        var mockCursor = new Mock<IAsyncCursor<User>>();
        mockCursor.Setup(c => c.Current).Returns(new List<User>());
        mockCursor.SetupSequence(c => c.MoveNext(It.IsAny<CancellationToken>()))
            .Returns(false);
        mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _mockUsersCollection
            .Setup(c => c.FindAsync(
                It.IsAny<FilterDefinition<User>>(),
                It.IsAny<FindOptions<User, User>>(),
                default))
            .ReturnsAsync(mockCursor.Object);

        //act
        var result = await _service.GetScoreHistoryAsync(userId);

        //assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task AwardBorrowPointsAsync_AwardsBronzeBadge_WhenScoreReaches10()
    {
        //arrange
        var userId = "user123";
        var itemRequestId = "request123";
        var itemName = "Test Item";
        
        var userAfterUpdate = new User
        {
            Id = userId,
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            LoopScore = 10,  // Score after update
            Badges = new List<BadgeAward>()
        };

        _mockUsersCollection
            .Setup(c => c.FindOneAndUpdateAsync(
                It.IsAny<FilterDefinition<User>>(),
                It.IsAny<UpdateDefinition<User>>(),
                It.IsAny<FindOneAndUpdateOptions<User>>(),
                default))
            .ReturnsAsync(userAfterUpdate);

        var updateResult = new Mock<UpdateResult>();
        updateResult.Setup(r => r.ModifiedCount).Returns(1);
        
        _mockUsersCollection
            .Setup(c => c.UpdateOneAsync(
                It.IsAny<FilterDefinition<User>>(),
                It.IsAny<UpdateDefinition<User>>(),
                It.IsAny<UpdateOptions>(),
                default))
            .ReturnsAsync(updateResult.Object);

        _mockEmailService
            .Setup(e => e.SendBadgeAwardEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<int>()))
            .ReturnsAsync(true);

        //act
        await _service.AwardBorrowPointsAsync(userId, itemRequestId, itemName);

        //assert
        _mockUsersCollection.Verify(c => c.UpdateOneAsync(
            It.IsAny<FilterDefinition<User>>(),
            It.IsAny<UpdateDefinition<User>>(),
            It.IsAny<UpdateOptions>(),
            default), Times.AtLeastOnce);

        _mockEmailService.Verify(e => e.SendBadgeAwardEmailAsync(
            userAfterUpdate.Email,
            It.IsAny<string>(),
            "Bronze",
            10), Times.Once);
    }

    [Fact]
    public async Task AwardLendPointsAsync_AwardsSilverBadge_WhenScoreReaches50()
    {
        //arrange
        var userId = "user123";
        var itemRequestId = "request123";
        var itemName = "Test Item";
        
        var userAfterUpdate = new User
        {
            Id = userId,
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            LoopScore = 50,  // Score after update
            Badges = new List<BadgeAward>
            {
                new BadgeAward { BadgeType = BadgeType.Bronze, AwardedAt = DateTime.UtcNow }
            }
        };

        _mockUsersCollection
            .Setup(c => c.FindOneAndUpdateAsync(
                It.IsAny<FilterDefinition<User>>(),
                It.IsAny<UpdateDefinition<User>>(),
                It.IsAny<FindOneAndUpdateOptions<User>>(),
                default))
            .ReturnsAsync(userAfterUpdate);

        var updateResult = new Mock<UpdateResult>();
        updateResult.Setup(r => r.ModifiedCount).Returns(1);
        
        _mockUsersCollection
            .Setup(c => c.UpdateOneAsync(
                It.IsAny<FilterDefinition<User>>(),
                It.IsAny<UpdateDefinition<User>>(),
                It.IsAny<UpdateOptions>(),
                default))
            .ReturnsAsync(updateResult.Object);

        _mockEmailService
            .Setup(e => e.SendBadgeAwardEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<int>()))
            .ReturnsAsync(true);

        //act
        await _service.AwardLendPointsAsync(userId, itemRequestId, itemName);

        //assert
        _mockEmailService.Verify(e => e.SendBadgeAwardEmailAsync(
            userAfterUpdate.Email,
            It.IsAny<string>(),
            "Silver",
            50), Times.Once);
    }

    [Fact]
    public async Task AwardLendPointsAsync_AwardsGoldBadge_WhenScoreReaches100()
    {
        //arrange
        var userId = "user123";
        var itemRequestId = "request123";
        var itemName = "Test Item";
        
        var userAfterUpdate = new User
        {
            Id = userId,
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            LoopScore = 100,  // Score after update
            Badges = new List<BadgeAward>
            {
                new BadgeAward { BadgeType = BadgeType.Bronze, AwardedAt = DateTime.UtcNow },
                new BadgeAward { BadgeType = BadgeType.Silver, AwardedAt = DateTime.UtcNow }
            }
        };

        _mockUsersCollection
            .Setup(c => c.FindOneAndUpdateAsync(
                It.IsAny<FilterDefinition<User>>(),
                It.IsAny<UpdateDefinition<User>>(),
                It.IsAny<FindOneAndUpdateOptions<User>>(),
                default))
            .ReturnsAsync(userAfterUpdate);

        var updateResult = new Mock<UpdateResult>();
        updateResult.Setup(r => r.ModifiedCount).Returns(1);
        
        _mockUsersCollection
            .Setup(c => c.UpdateOneAsync(
                It.IsAny<FilterDefinition<User>>(),
                It.IsAny<UpdateDefinition<User>>(),
                It.IsAny<UpdateOptions>(),
                default))
            .ReturnsAsync(updateResult.Object);

        _mockEmailService
            .Setup(e => e.SendBadgeAwardEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<int>()))
            .ReturnsAsync(true);

        //act
        await _service.AwardLendPointsAsync(userId, itemRequestId, itemName);

        //assert
        _mockEmailService.Verify(e => e.SendBadgeAwardEmailAsync(
            userAfterUpdate.Email,
            It.IsAny<string>(),
            "Gold",
            100), Times.Once);
    }

    [Fact]
    public async Task AwardBorrowPointsAsync_DoesNotAwardDuplicateBadges_WhenScoreExceedsMilestone()
    {
        //arrange
        var userId = "user123";
        var itemRequestId = "request123";
        var itemName = "Test Item";
        
        var user = new User
        {
            Id = userId,
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            LoopScore = 15,
            Badges = new List<BadgeAward>
            {
                new BadgeAward { BadgeType = BadgeType.Bronze, AwardedAt = DateTime.UtcNow }
            }
        };

        var mockCursor = new Mock<IAsyncCursor<User>>();
        mockCursor.Setup(c => c.Current).Returns(new List<User> { user });
        mockCursor.SetupSequence(c => c.MoveNext(It.IsAny<CancellationToken>()))
            .Returns(true)
            .Returns(false);
        mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .ReturnsAsync(false);

        _mockUsersCollection
            .Setup(c => c.FindAsync(
                It.IsAny<FilterDefinition<User>>(),
                It.IsAny<FindOptions<User, User>>(),
                default))
            .ReturnsAsync(mockCursor.Object);

        var updateResult = new Mock<UpdateResult>();
        updateResult.Setup(r => r.ModifiedCount).Returns(1);
        
        _mockUsersCollection
            .Setup(c => c.UpdateOneAsync(
                It.IsAny<FilterDefinition<User>>(),
                It.IsAny<UpdateDefinition<User>>(),
                It.IsAny<UpdateOptions>(),
                default))
            .ReturnsAsync(updateResult.Object);

        //act
        await _service.AwardBorrowPointsAsync(userId, itemRequestId, itemName);

        //assert
        _mockEmailService.Verify(e => e.SendBadgeAwardEmailAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            "Bronze",
            It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task GetUserBadgesAsync_ReturnsAllEarnedBadges()
    {
        //arrange
        var userId = "user123";
        var badges = new List<BadgeAward>
        {
            new BadgeAward { BadgeType = BadgeType.Bronze, AwardedAt = DateTime.UtcNow.AddDays(-10) },
            new BadgeAward { BadgeType = BadgeType.Silver, AwardedAt = DateTime.UtcNow.AddDays(-5) },
            new BadgeAward { BadgeType = BadgeType.Gold, AwardedAt = DateTime.UtcNow }
        };

        var user = new User
        {
            Id = userId,
            Email = "test@example.com",
            LoopScore = 100,
            Badges = badges
        };

        var mockCursor = new Mock<IAsyncCursor<User>>();
        mockCursor.Setup(c => c.Current).Returns(new List<User> { user });
        mockCursor.SetupSequence(c => c.MoveNext(It.IsAny<CancellationToken>()))
            .Returns(true)
            .Returns(false);
        mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .ReturnsAsync(false);

        _mockUsersCollection
            .Setup(c => c.FindAsync(
                It.IsAny<FilterDefinition<User>>(),
                It.IsAny<FindOptions<User, User>>(),
                default))
            .ReturnsAsync(mockCursor.Object);

        //act
        var result = await _service.GetUserBadgesAsync(userId);

        //assert
        Assert.Equal(3, result.Count);
        Assert.Equal(BadgeType.Bronze, result[0].BadgeType);
        Assert.Equal(BadgeType.Silver, result[1].BadgeType);
        Assert.Equal(BadgeType.Gold, result[2].BadgeType);
    }

    [Fact]
    public async Task GetUserBadgesAsync_ReturnsEmptyList_WhenUserNotFound()
    {
        //arrange
        var userId = "nonexistent";

        var mockCursor = new Mock<IAsyncCursor<User>>();
        mockCursor.Setup(c => c.Current).Returns(new List<User>());
        mockCursor.SetupSequence(c => c.MoveNext(It.IsAny<CancellationToken>()))
            .Returns(false);
        mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _mockUsersCollection
            .Setup(c => c.FindAsync(
                It.IsAny<FilterDefinition<User>>(),
                It.IsAny<FindOptions<User, User>>(),
                default))
            .ReturnsAsync(mockCursor.Object);

        //act
        var result = await _service.GetUserBadgesAsync(userId);

        //assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task AwardLendPointsAsync_AwardsFirstLendBadge_OnFirstLendingTransaction()
    {
        //arrange
        var userId = "user123";
        var itemRequestId = "request123";
        var itemName = "Test Item";
        
        var userAfterUpdate = new User
        {
            Id = userId,
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            LoopScore = 4,
            Badges = new List<BadgeAward>()
        };

        var userForBadgeCheck = new User
        {
            Id = userId,
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            LoopScore = 4,
            Badges = new List<BadgeAward>()
        };

        var mockCursor = new Mock<IAsyncCursor<User>>();
        mockCursor.Setup(c => c.Current).Returns(new List<User> { userForBadgeCheck });
        mockCursor.SetupSequence(c => c.MoveNext(It.IsAny<CancellationToken>()))
            .Returns(true)
            .Returns(false);
        mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .ReturnsAsync(false);

        _mockUsersCollection
            .Setup(c => c.FindOneAndUpdateAsync(
                It.IsAny<FilterDefinition<User>>(),
                It.IsAny<UpdateDefinition<User>>(),
                It.IsAny<FindOneAndUpdateOptions<User>>(),
                default))
            .ReturnsAsync(userAfterUpdate);

        _mockUsersCollection
            .Setup(c => c.FindAsync(
                It.IsAny<FilterDefinition<User>>(),
                It.IsAny<FindOptions<User, User>>(),
                default))
            .ReturnsAsync(mockCursor.Object);

        var updateResult = new Mock<UpdateResult>();
        updateResult.Setup(r => r.ModifiedCount).Returns(1);
        
        _mockUsersCollection
            .Setup(c => c.UpdateOneAsync(
                It.IsAny<FilterDefinition<User>>(),
                It.IsAny<UpdateDefinition<User>>(),
                It.IsAny<UpdateOptions>(),
                default))
            .ReturnsAsync(updateResult.Object);

        _mockEmailService
            .Setup(e => e.SendBadgeAwardEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<int>()))
            .ReturnsAsync(true);

        //act
        await _service.AwardLendPointsAsync(userId, itemRequestId, itemName);

        //assert
        _mockUsersCollection.Verify(c => c.UpdateOneAsync(
            It.IsAny<FilterDefinition<User>>(),
            It.IsAny<UpdateDefinition<User>>(),
            It.IsAny<UpdateOptions>(),
            default), Times.AtLeastOnce);

        _mockEmailService.Verify(e => e.SendBadgeAwardEmailAsync(
            userAfterUpdate.Email,
            It.IsAny<string>(),
            "FirstLend",
            It.IsAny<int>()), Times.Once);
    }

    [Fact]
    public async Task AwardLendPointsAsync_DoesNotAwardFirstLendBadge_OnSubsequentLends()
    {
        //arrange
        var userId = "user123";
        var itemRequestId = "request123";
        var itemName = "Test Item";
        
        var userAfterUpdate = new User
        {
            Id = userId,
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            LoopScore = 8,
            Badges = new List<BadgeAward>()
        };

        var userForBadgeCheck = new User
        {
            Id = userId,
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            LoopScore = 8,
            Badges = new List<BadgeAward>
            {
                new BadgeAward { BadgeType = BadgeType.FirstLend, AwardedAt = DateTime.UtcNow.AddDays(-1) }
            }
        };

        var mockCursor = new Mock<IAsyncCursor<User>>();
        mockCursor.Setup(c => c.Current).Returns(new List<User> { userForBadgeCheck });
        mockCursor.SetupSequence(c => c.MoveNext(It.IsAny<CancellationToken>()))
            .Returns(true)
            .Returns(false);
        mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .ReturnsAsync(false);

        _mockUsersCollection
            .Setup(c => c.FindOneAndUpdateAsync(
                It.IsAny<FilterDefinition<User>>(),
                It.IsAny<UpdateDefinition<User>>(),
                It.IsAny<FindOneAndUpdateOptions<User>>(),
                default))
            .ReturnsAsync(userAfterUpdate);

        _mockUsersCollection
            .Setup(c => c.FindAsync(
                It.IsAny<FilterDefinition<User>>(),
                It.IsAny<FindOptions<User, User>>(),
                default))
            .ReturnsAsync(mockCursor.Object);

        _mockEmailService
            .Setup(e => e.SendBadgeAwardEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<int>()))
            .ReturnsAsync(true);

        //act
        await _service.AwardLendPointsAsync(userId, itemRequestId, itemName);

        //assert
        _mockEmailService.Verify(e => e.SendBadgeAwardEmailAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            "FirstLend",
            It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task AwardOnTimeReturnPointsAsync_AwardsReliableBorrowerBadge_After10OnTimeReturns()
    {
        //arrange
        var userId = "user123";
        var itemRequestId = "request123";
        var itemName = "Test Item";
        
        // Create 10 on-time return entries (including the one being added)
        var scoreHistory = new List<ScoreHistoryEntry>();
        for (int i = 0; i < 9; i++)
        {
            scoreHistory.Add(new ScoreHistoryEntry
            {
                Timestamp = DateTime.UtcNow.AddDays(-i - 1),
                Points = 1,
                ActionType = ScoreActionType.OnTimeReturn,
                ItemRequestId = $"req{i}",
                ItemName = $"Item {i}"
            });
        }

        // Add the current entry to reach 10 total
        var scoreHistoryAfterUpdate = new List<ScoreHistoryEntry>(scoreHistory)
        {
            new ScoreHistoryEntry
            {
                Timestamp = DateTime.UtcNow,
                Points = 1,
                ActionType = ScoreActionType.OnTimeReturn,
                ItemRequestId = itemRequestId,
                ItemName = itemName
            }
        };

        var userAfterUpdate = new User
        {
            Id = userId,
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            LoopScore = 15,
            ScoreHistory = scoreHistoryAfterUpdate,
            Badges = new List<BadgeAward>
            {
                new BadgeAward { BadgeType = BadgeType.Bronze, AwardedAt = DateTime.UtcNow.AddDays(-5) }
            }
        };

        var userForBadgeCheck = new User
        {
            Id = userId,
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            LoopScore = 15,
            ScoreHistory = scoreHistoryAfterUpdate,
            Badges = new List<BadgeAward>
            {
                new BadgeAward { BadgeType = BadgeType.Bronze, AwardedAt = DateTime.UtcNow.AddDays(-5) }
            }
        };

        _mockUsersCollection
            .Setup(c => c.FindOneAndUpdateAsync(
                It.IsAny<FilterDefinition<User>>(),
                It.IsAny<UpdateDefinition<User>>(),
                It.IsAny<FindOneAndUpdateOptions<User>>(),
                default))
            .ReturnsAsync(userAfterUpdate);

        // Setup for GetOnTimeReturnCountAsync call
        var mockCursor1 = new Mock<IAsyncCursor<User>>();
        mockCursor1.Setup(c => c.Current).Returns(new List<User> { userForBadgeCheck });
        mockCursor1.SetupSequence(c => c.MoveNext(It.IsAny<CancellationToken>()))
            .Returns(true)
            .Returns(false);
        mockCursor1.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .ReturnsAsync(false);

        // Setup for CheckAndAwardAchievementBadgeAsync call
        var mockCursor2 = new Mock<IAsyncCursor<User>>();
        mockCursor2.Setup(c => c.Current).Returns(new List<User> { userForBadgeCheck });
        mockCursor2.SetupSequence(c => c.MoveNext(It.IsAny<CancellationToken>()))
            .Returns(true)
            .Returns(false);
        mockCursor2.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .ReturnsAsync(false);

        _mockUsersCollection
            .SetupSequence(c => c.FindAsync(
                It.IsAny<FilterDefinition<User>>(),
                It.IsAny<FindOptions<User, User>>(),
                default))
            .ReturnsAsync(mockCursor1.Object)
            .ReturnsAsync(mockCursor2.Object);

        var updateResult = new Mock<UpdateResult>();
        updateResult.Setup(r => r.ModifiedCount).Returns(1);
        
        _mockUsersCollection
            .Setup(c => c.UpdateOneAsync(
                It.IsAny<FilterDefinition<User>>(),
                It.IsAny<UpdateDefinition<User>>(),
                It.IsAny<UpdateOptions>(),
                default))
            .ReturnsAsync(updateResult.Object);

        _mockEmailService
            .Setup(e => e.SendBadgeAwardEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<int>()))
            .ReturnsAsync(true);

        //act
        await _service.AwardOnTimeReturnPointsAsync(userId, itemRequestId, itemName);

        //assert
        _mockEmailService.Verify(e => e.SendBadgeAwardEmailAsync(
            userAfterUpdate.Email,
            It.IsAny<string>(),
            "ReliableBorrower",
            It.IsAny<int>()), Times.Once);
    }

    [Fact]
    public async Task AwardOnTimeReturnPointsAsync_DoesNotAwardReliableBorrowerBadge_BeforeThreshold()
    {
        //arrange
        var userId = "user123";
        var itemRequestId = "request123";
        var itemName = "Test Item";
        
        var scoreHistory = new List<ScoreHistoryEntry>();
        for (int i = 0; i < 5; i++)
        {
            scoreHistory.Add(new ScoreHistoryEntry
            {
                Timestamp = DateTime.UtcNow.AddDays(-i),
                Points = 1,
                ActionType = ScoreActionType.OnTimeReturn,
                ItemRequestId = $"req{i}",
                ItemName = $"Item {i}"
            });
        }

        var userAfterUpdate = new User
        {
            Id = userId,
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            LoopScore = 6,
            ScoreHistory = scoreHistory,
            Badges = new List<BadgeAward>()
        };

        var userForBadgeCheck = new User
        {
            Id = userId,
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            LoopScore = 6,
            ScoreHistory = new List<ScoreHistoryEntry>(scoreHistory)
            {
                new ScoreHistoryEntry
                {
                    Timestamp = DateTime.UtcNow,
                    Points = 1,
                    ActionType = ScoreActionType.OnTimeReturn,
                    ItemRequestId = itemRequestId,
                    ItemName = itemName
                }
            },
            Badges = new List<BadgeAward>()
        };

        var mockCursor = new Mock<IAsyncCursor<User>>();
        mockCursor.Setup(c => c.Current).Returns(new List<User> { userForBadgeCheck });
        mockCursor.SetupSequence(c => c.MoveNext(It.IsAny<CancellationToken>()))
            .Returns(true)
            .Returns(false);
        mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .ReturnsAsync(false);

        _mockUsersCollection
            .Setup(c => c.FindOneAndUpdateAsync(
                It.IsAny<FilterDefinition<User>>(),
                It.IsAny<UpdateDefinition<User>>(),
                It.IsAny<FindOneAndUpdateOptions<User>>(),
                default))
            .ReturnsAsync(userAfterUpdate);

        _mockUsersCollection
            .Setup(c => c.FindAsync(
                It.IsAny<FilterDefinition<User>>(),
                It.IsAny<FindOptions<User, User>>(),
                default))
            .ReturnsAsync(mockCursor.Object);

        _mockEmailService
            .Setup(e => e.SendBadgeAwardEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<int>()))
            .ReturnsAsync(true);

        //act
        await _service.AwardOnTimeReturnPointsAsync(userId, itemRequestId, itemName);

        //assert
        _mockEmailService.Verify(e => e.SendBadgeAwardEmailAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            "ReliableBorrower",
            It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task GetOnTimeReturnCountAsync_ReturnsCorrectCount()
    {
        //arrange
        var userId = "user123";
        var scoreHistory = new List<ScoreHistoryEntry>
        {
            new ScoreHistoryEntry { ActionType = ScoreActionType.OnTimeReturn, Points = 1 },
            new ScoreHistoryEntry { ActionType = ScoreActionType.BorrowCompleted, Points = 1 },
            new ScoreHistoryEntry { ActionType = ScoreActionType.OnTimeReturn, Points = 1 },
            new ScoreHistoryEntry { ActionType = ScoreActionType.LendApproved, Points = 4 },
            new ScoreHistoryEntry { ActionType = ScoreActionType.OnTimeReturn, Points = 1 }
        };

        var user = new User
        {
            Id = userId,
            Email = "test@example.com",
            LoopScore = 8,
            ScoreHistory = scoreHistory
        };

        var mockCursor = new Mock<IAsyncCursor<User>>();
        mockCursor.Setup(c => c.Current).Returns(new List<User> { user });
        mockCursor.SetupSequence(c => c.MoveNext(It.IsAny<CancellationToken>()))
            .Returns(true)
            .Returns(false);
        mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .ReturnsAsync(false);

        _mockUsersCollection
            .Setup(c => c.FindAsync(
                It.IsAny<FilterDefinition<User>>(),
                It.IsAny<FindOptions<User, User>>(),
                default))
            .ReturnsAsync(mockCursor.Object);

        //act
        var count = await _service.GetOnTimeReturnCountAsync(userId);

        //assert
        Assert.Equal(3, count);
    }

    [Fact]
    public async Task GetOnTimeReturnCountAsync_ReturnsZero_WhenUserNotFound()
    {
        //arrange
        var userId = "nonexistent";

        var mockCursor = new Mock<IAsyncCursor<User>>();
        mockCursor.Setup(c => c.Current).Returns(new List<User>());
        mockCursor.SetupSequence(c => c.MoveNext(It.IsAny<CancellationToken>()))
            .Returns(false);
        mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _mockUsersCollection
            .Setup(c => c.FindAsync(
                It.IsAny<FilterDefinition<User>>(),
                It.IsAny<FindOptions<User, User>>(),
                default))
            .ReturnsAsync(mockCursor.Object);

        //act
        var count = await _service.GetOnTimeReturnCountAsync(userId);

        //assert
        Assert.Equal(0, count);
    }

    [Fact]
    public async Task RecordCompletedLendingTransactionAsync_AwardsGenerousLenderBadge_After50Transactions()
    {
        //arrange
        var userId = "user123";
        var itemRequestId = "request123";
        var itemName = "Test Item";
        
        // Create 50 lending transactions in score history
        var scoreHistory = new List<ScoreHistoryEntry>();
        for (int i = 0; i < 50; i++)
        {
            scoreHistory.Add(new ScoreHistoryEntry
            {
                Timestamp = DateTime.UtcNow.AddDays(-i),
                Points = 4,
                ActionType = ScoreActionType.LendApproved,
                ItemRequestId = $"req{i}",
                ItemName = $"Item {i}"
            });
        }

        var user = new User
        {
            Id = userId,
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            LoopScore = 200,
            ScoreHistory = scoreHistory,
            Badges = new List<BadgeAward>()
        };

        // Setup for GetCompletedLendingTransactionCountAsync
        var mockCursor1 = new Mock<IAsyncCursor<User>>();
        mockCursor1.Setup(c => c.Current).Returns(new List<User> { user });
        mockCursor1.SetupSequence(c => c.MoveNext(It.IsAny<CancellationToken>()))
            .Returns(true)
            .Returns(false);
        mockCursor1.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .ReturnsAsync(false);

        // Setup for CheckAndAwardAchievementBadgeAsync
        var mockCursor2 = new Mock<IAsyncCursor<User>>();
        mockCursor2.Setup(c => c.Current).Returns(new List<User> { user });
        mockCursor2.SetupSequence(c => c.MoveNext(It.IsAny<CancellationToken>()))
            .Returns(true)
            .Returns(false);
        mockCursor2.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .ReturnsAsync(false);

        _mockUsersCollection
            .SetupSequence(c => c.FindAsync(
                It.IsAny<FilterDefinition<User>>(),
                It.IsAny<FindOptions<User, User>>(),
                default))
            .ReturnsAsync(mockCursor1.Object)
            .ReturnsAsync(mockCursor2.Object);

        var updateResult = new Mock<UpdateResult>();
        updateResult.Setup(r => r.ModifiedCount).Returns(1);
        
        _mockUsersCollection
            .Setup(c => c.UpdateOneAsync(
                It.IsAny<FilterDefinition<User>>(),
                It.IsAny<UpdateDefinition<User>>(),
                It.IsAny<UpdateOptions>(),
                default))
            .ReturnsAsync(updateResult.Object);

        _mockEmailService
            .Setup(e => e.SendBadgeAwardEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<int>()))
            .ReturnsAsync(true);

        //act
        await _service.RecordCompletedLendingTransactionAsync(userId, itemRequestId, itemName);

        //assert
        _mockEmailService.Verify(e => e.SendBadgeAwardEmailAsync(
            user.Email,
            It.IsAny<string>(),
            "GenerousLender",
            It.IsAny<int>()), Times.Once);
    }

    [Fact]
    public async Task RecordCompletedLendingTransactionAsync_DoesNotAwardBadge_BeforeThreshold()
    {
        //arrange
        var userId = "user123";
        var itemRequestId = "request123";
        var itemName = "Test Item";
        
        // Create only 30 lending transactions
        var scoreHistory = new List<ScoreHistoryEntry>();
        for (int i = 0; i < 30; i++)
        {
            scoreHistory.Add(new ScoreHistoryEntry
            {
                Timestamp = DateTime.UtcNow.AddDays(-i),
                Points = 4,
                ActionType = ScoreActionType.LendApproved,
                ItemRequestId = $"req{i}",
                ItemName = $"Item {i}"
            });
        }

        var user = new User
        {
            Id = userId,
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            LoopScore = 120,
            ScoreHistory = scoreHistory,
            Badges = new List<BadgeAward>()
        };

        var mockCursor = new Mock<IAsyncCursor<User>>();
        mockCursor.Setup(c => c.Current).Returns(new List<User> { user });
        mockCursor.SetupSequence(c => c.MoveNext(It.IsAny<CancellationToken>()))
            .Returns(true)
            .Returns(false);
        mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .ReturnsAsync(false);

        _mockUsersCollection
            .Setup(c => c.FindAsync(
                It.IsAny<FilterDefinition<User>>(),
                It.IsAny<FindOptions<User, User>>(),
                default))
            .ReturnsAsync(mockCursor.Object);

        _mockEmailService
            .Setup(e => e.SendBadgeAwardEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<int>()))
            .ReturnsAsync(true);

        //act
        await _service.RecordCompletedLendingTransactionAsync(userId, itemRequestId, itemName);

        //assert
        _mockEmailService.Verify(e => e.SendBadgeAwardEmailAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            "GenerousLender",
            It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task GetCompletedLendingTransactionCountAsync_ReturnsCorrectCount()
    {
        //arrange
        var userId = "user123";
        var scoreHistory = new List<ScoreHistoryEntry>
        {
            new ScoreHistoryEntry { ActionType = ScoreActionType.LendApproved, Points = 4 },
            new ScoreHistoryEntry { ActionType = ScoreActionType.BorrowCompleted, Points = 1 },
            new ScoreHistoryEntry { ActionType = ScoreActionType.LendApproved, Points = 4 },
            new ScoreHistoryEntry { ActionType = ScoreActionType.OnTimeReturn, Points = 1 },
            new ScoreHistoryEntry { ActionType = ScoreActionType.LendApproved, Points = 4 }
        };

        var user = new User
        {
            Id = userId,
            Email = "test@example.com",
            LoopScore = 14,
            ScoreHistory = scoreHistory
        };

        var mockCursor = new Mock<IAsyncCursor<User>>();
        mockCursor.Setup(c => c.Current).Returns(new List<User> { user });
        mockCursor.SetupSequence(c => c.MoveNext(It.IsAny<CancellationToken>()))
            .Returns(true)
            .Returns(false);
        mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .ReturnsAsync(false);

        _mockUsersCollection
            .Setup(c => c.FindAsync(
                It.IsAny<FilterDefinition<User>>(),
                It.IsAny<FindOptions<User, User>>(),
                default))
            .ReturnsAsync(mockCursor.Object);

        //act
        var count = await _service.GetCompletedLendingTransactionCountAsync(userId);

        //assert
        Assert.Equal(3, count);
    }

    [Fact]
    public async Task AwardOnTimeReturnPointsAsync_IncreasesConsecutiveCount()
    {
        //arrange
        var userId = "user123";
        var itemRequestId = "request123";
        var itemName = "Test Item";
        
        var userAfterUpdate = new User
        {
            Id = userId,
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            LoopScore = 5,
            ConsecutiveOnTimeReturns = 5,
            ScoreHistory = new List<ScoreHistoryEntry>(),
            Badges = new List<BadgeAward>()
        };

        var mockCursor = new Mock<IAsyncCursor<User>>();
        mockCursor.Setup(c => c.Current).Returns(new List<User> { userAfterUpdate });
        mockCursor.SetupSequence(c => c.MoveNext(It.IsAny<CancellationToken>()))
            .Returns(true)
            .Returns(false);
        mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .ReturnsAsync(false);

        _mockUsersCollection
            .Setup(c => c.FindOneAndUpdateAsync(
                It.IsAny<FilterDefinition<User>>(),
                It.IsAny<UpdateDefinition<User>>(),
                It.IsAny<FindOneAndUpdateOptions<User>>(),
                default))
            .ReturnsAsync(userAfterUpdate);

        _mockUsersCollection
            .Setup(c => c.FindAsync(
                It.IsAny<FilterDefinition<User>>(),
                It.IsAny<FindOptions<User, User>>(),
                default))
            .ReturnsAsync(mockCursor.Object);

        //act
        await _service.AwardOnTimeReturnPointsAsync(userId, itemRequestId, itemName);

        //assert
        _mockUsersCollection.Verify(c => c.FindOneAndUpdateAsync(
            It.IsAny<FilterDefinition<User>>(),
            It.IsAny<UpdateDefinition<User>>(),
            It.IsAny<FindOneAndUpdateOptions<User>>(),
            default), Times.AtLeast(2)); // Called for score update and consecutive count update
    }

    [Fact]
    public async Task AwardOnTimeReturnPointsAsync_AwardsPerfectRecordBadge_After25ConsecutiveReturns()
    {
        //arrange
        var userId = "user123";
        var itemRequestId = "request123";
        var itemName = "Test Item";
        
        var userAfterUpdate = new User
        {
            Id = userId,
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            LoopScore = 30,
            ConsecutiveOnTimeReturns = 25,
            ScoreHistory = new List<ScoreHistoryEntry>(),
            Badges = new List<BadgeAward>()
        };

        var mockCursor = new Mock<IAsyncCursor<User>>();
        mockCursor.Setup(c => c.Current).Returns(new List<User> { userAfterUpdate });
        mockCursor.SetupSequence(c => c.MoveNext(It.IsAny<CancellationToken>()))
            .Returns(true)
            .Returns(false)
            .Returns(true)
            .Returns(false);
        mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .ReturnsAsync(false)
            .ReturnsAsync(true)
            .ReturnsAsync(false);

        _mockUsersCollection
            .Setup(c => c.FindOneAndUpdateAsync(
                It.IsAny<FilterDefinition<User>>(),
                It.IsAny<UpdateDefinition<User>>(),
                It.IsAny<FindOneAndUpdateOptions<User>>(),
                default))
            .ReturnsAsync(userAfterUpdate);

        _mockUsersCollection
            .Setup(c => c.FindAsync(
                It.IsAny<FilterDefinition<User>>(),
                It.IsAny<FindOptions<User, User>>(),
                default))
            .ReturnsAsync(mockCursor.Object);

        var updateResult = new Mock<UpdateResult>();
        updateResult.Setup(r => r.ModifiedCount).Returns(1);
        
        _mockUsersCollection
            .Setup(c => c.UpdateOneAsync(
                It.IsAny<FilterDefinition<User>>(),
                It.IsAny<UpdateDefinition<User>>(),
                It.IsAny<UpdateOptions>(),
                default))
            .ReturnsAsync(updateResult.Object);

        _mockEmailService
            .Setup(e => e.SendBadgeAwardEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<int>()))
            .ReturnsAsync(true);

        //act
        await _service.AwardOnTimeReturnPointsAsync(userId, itemRequestId, itemName);

        //assert
        _mockEmailService.Verify(e => e.SendBadgeAwardEmailAsync(
            userAfterUpdate.Email,
            It.IsAny<string>(),
            "PerfectRecord",
            It.IsAny<int>()), Times.Once);
    }

    [Fact]
    public async Task ResetConsecutiveOnTimeReturnsAsync_ResetsCount()
    {
        //arrange
        var userId = "user123";

        var updateResult = new Mock<UpdateResult>();
        updateResult.Setup(r => r.ModifiedCount).Returns(1);
        
        _mockUsersCollection
            .Setup(c => c.UpdateOneAsync(
                It.IsAny<FilterDefinition<User>>(),
                It.IsAny<UpdateDefinition<User>>(),
                It.IsAny<UpdateOptions>(),
                default))
            .ReturnsAsync(updateResult.Object);

        //act
        await _service.ResetConsecutiveOnTimeReturnsAsync(userId);

        //assert
        _mockUsersCollection.Verify(c => c.UpdateOneAsync(
            It.IsAny<FilterDefinition<User>>(),
            It.IsAny<UpdateDefinition<User>>(),
            It.IsAny<UpdateOptions>(),
            default), Times.Once);
    }

    [Fact]
    public async Task GetActiveInvitedUsersCountAsync_ReturnsCorrectCount()
    {
        //arrange
        var inviterId = "inviter123";

        _mockUsersCollection
            .Setup(c => c.CountDocumentsAsync(
                It.IsAny<FilterDefinition<User>>(),
                It.IsAny<CountOptions>(),
                default))
            .ReturnsAsync(10);

        //act
        var count = await _service.GetActiveInvitedUsersCountAsync(inviterId);

        //assert
        Assert.Equal(10, count);
    }

    [Fact]
    public async Task GetActiveInvitedUsersCountAsync_OnlyCountsUsersWithScoreHistory()
    {
        //arrange
        var inviterId = "inviter123";

        _mockUsersCollection
            .Setup(c => c.CountDocumentsAsync(
                It.IsAny<FilterDefinition<User>>(),
                It.IsAny<CountOptions>(),
                default))
            .ReturnsAsync(5);

        //act
        var count = await _service.GetActiveInvitedUsersCountAsync(inviterId);

        //assert
        Assert.Equal(5, count);
        _mockUsersCollection.Verify(c => c.CountDocumentsAsync(
            It.IsAny<FilterDefinition<User>>(),
            It.IsAny<CountOptions>(),
            default), Times.Once);
    }

    [Fact]
    public async Task AwardOnTimeReturnPointsAsync_DoesNotAwardPerfectRecordBadge_BeforeThreshold()
    {
        //arrange
        var userId = "user123";
        var itemRequestId = "request123";
        var itemName = "Test Item";
        
        var userAfterUpdate = new User
        {
            Id = userId,
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            LoopScore = 15,
            ConsecutiveOnTimeReturns = 15, // Less than 25
            ScoreHistory = new List<ScoreHistoryEntry>(),
            Badges = new List<BadgeAward>()
        };

        var mockCursor = new Mock<IAsyncCursor<User>>();
        mockCursor.Setup(c => c.Current).Returns(new List<User> { userAfterUpdate });
        mockCursor.SetupSequence(c => c.MoveNext(It.IsAny<CancellationToken>()))
            .Returns(true)
            .Returns(false);
        mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .ReturnsAsync(false);

        _mockUsersCollection
            .Setup(c => c.FindOneAndUpdateAsync(
                It.IsAny<FilterDefinition<User>>(),
                It.IsAny<UpdateDefinition<User>>(),
                It.IsAny<FindOneAndUpdateOptions<User>>(),
                default))
            .ReturnsAsync(userAfterUpdate);

        _mockUsersCollection
            .Setup(c => c.FindAsync(
                It.IsAny<FilterDefinition<User>>(),
                It.IsAny<FindOptions<User, User>>(),
                default))
            .ReturnsAsync(mockCursor.Object);

        //act
        await _service.AwardOnTimeReturnPointsAsync(userId, itemRequestId, itemName);

        //assert
        _mockEmailService.Verify(e => e.SendBadgeAwardEmailAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            "PerfectRecord",
            It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task AwardOnTimeReturnPointsAsync_PreventsDuplicatePerfectRecordBadge()
    {
        //arrange
        var userId = "user123";
        var itemRequestId = "request123";
        var itemName = "Test Item";
        
        var userAfterUpdate = new User
        {
            Id = userId,
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            LoopScore = 30,
            ConsecutiveOnTimeReturns = 30, // More than 25
            ScoreHistory = new List<ScoreHistoryEntry>(),
            Badges = new List<BadgeAward>
            {
                new BadgeAward { BadgeType = BadgeType.PerfectRecord, AwardedAt = DateTime.UtcNow.AddDays(-1) }
            }
        };

        var mockCursor = new Mock<IAsyncCursor<User>>();
        mockCursor.Setup(c => c.Current).Returns(new List<User> { userAfterUpdate });
        mockCursor.SetupSequence(c => c.MoveNext(It.IsAny<CancellationToken>()))
            .Returns(true)
            .Returns(false)
            .Returns(true)
            .Returns(false);
        mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .ReturnsAsync(false)
            .ReturnsAsync(true)
            .ReturnsAsync(false);

        _mockUsersCollection
            .Setup(c => c.FindOneAndUpdateAsync(
                It.IsAny<FilterDefinition<User>>(),
                It.IsAny<UpdateDefinition<User>>(),
                It.IsAny<FindOneAndUpdateOptions<User>>(),
                default))
            .ReturnsAsync(userAfterUpdate);

        _mockUsersCollection
            .Setup(c => c.FindAsync(
                It.IsAny<FilterDefinition<User>>(),
                It.IsAny<FindOptions<User, User>>(),
                default))
            .ReturnsAsync(mockCursor.Object);

        //act
        await _service.AwardOnTimeReturnPointsAsync(userId, itemRequestId, itemName);

        //assert
        _mockEmailService.Verify(e => e.SendBadgeAwardEmailAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            "PerfectRecord",
            It.IsAny<int>()), Times.Never);
    }
}
