using Api.Controllers;
using Api.DTOs;
using Api.Models;
using Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using Xunit;

namespace Api.Tests;

public class UsersControllerTests
{
    private readonly Mock<ILoopScoreService> _mockLoopScoreService;
    private readonly Mock<IUserService> _mockUserService;
    private readonly Mock<ILogger<UsersController>> _mockLogger;
    private readonly UsersController _controller;

    public UsersControllerTests()
    {
        _mockLoopScoreService = new Mock<ILoopScoreService>();
        _mockUserService = new Mock<IUserService>();
        _mockLogger = new Mock<ILogger<UsersController>>();

        _controller = new UsersController(
            _mockLoopScoreService.Object,
            _mockUserService.Object,
            _mockLogger.Object);
    }

    private void SetupAuthenticatedUser(string userId)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId)
        };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };
    }

    [Fact]
    public async Task GetUserScore_ReturnsScore_WhenUserExists()
    {
        //arrange
        var userId = "user123";
        var expectedScore = 15;

        var user = new User
        {
            Id = userId,
            Email = "test@example.com",
            LoopScore = expectedScore
        };

        _mockUserService.Setup(s => s.GetUserByIdAsync(userId)).ReturnsAsync(user);
        _mockLoopScoreService.Setup(s => s.GetUserScoreAsync(userId)).ReturnsAsync(expectedScore);

        //act
        var result = await _controller.GetUserScore(userId);

        //assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(expectedScore, okResult.Value);
    }

    [Fact]
    public async Task GetUserScore_Returns404_WhenUserNotFound()
    {
        //arrange
        var userId = "nonexistent";

        _mockUserService.Setup(s => s.GetUserByIdAsync(userId)).ReturnsAsync((User?)null);

        //act
        var result = await _controller.GetUserScore(userId);

        //assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.NotNull(notFoundResult.Value);
    }

    [Fact]
    public async Task GetScoreHistory_ReturnsHistory_WithPagination()
    {
        //arrange
        var userId = "user123";
        var limit = 10;

        var user = new User
        {
            Id = userId,
            Email = "test@example.com"
        };

        var history = new List<ScoreHistoryEntry>
        {
            new ScoreHistoryEntry
            {
                Timestamp = DateTime.UtcNow,
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
            }
        };

        _mockUserService.Setup(s => s.GetUserByIdAsync(userId)).ReturnsAsync(user);
        _mockLoopScoreService.Setup(s => s.GetScoreHistoryAsync(userId, limit)).ReturnsAsync(history);

        //act
        var result = await _controller.GetScoreHistory(userId, limit);

        //assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedHistory = Assert.IsType<List<ScoreHistoryEntry>>(okResult.Value);
        Assert.Equal(2, returnedHistory.Count);
        Assert.Equal("Item 1", returnedHistory[0].ItemName);
    }

    [Fact]
    public async Task GetScoreHistory_Returns404_WhenUserNotFound()
    {
        //arrange
        var userId = "nonexistent";

        _mockUserService.Setup(s => s.GetUserByIdAsync(userId)).ReturnsAsync((User?)null);

        //act
        var result = await _controller.GetScoreHistory(userId);

        //assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.NotNull(notFoundResult.Value);
    }

    [Fact]
    public async Task GetScoreHistory_UsesDefaultLimit_WhenNotSpecified()
    {
        //arrange
        var userId = "user123";

        var user = new User
        {
            Id = userId,
            Email = "test@example.com"
        };

        var history = new List<ScoreHistoryEntry>();

        _mockUserService.Setup(s => s.GetUserByIdAsync(userId)).ReturnsAsync(user);
        _mockLoopScoreService.Setup(s => s.GetScoreHistoryAsync(userId, 50)).ReturnsAsync(history);

        //act
        var result = await _controller.GetScoreHistory(userId);

        //assert
        _mockLoopScoreService.Verify(s => s.GetScoreHistoryAsync(userId, 50), Times.Once);
    }

    [Fact]
    public async Task GetUserBadges_ReturnsBadges_WhenUserExists()
    {
        //arrange
        var userId = "user123";

        var user = new User
        {
            Id = userId,
            Email = "test@example.com"
        };

        var badges = new List<BadgeAward>
        {
            new BadgeAward { BadgeType = BadgeType.Bronze, AwardedAt = DateTime.UtcNow.AddDays(-10) },
            new BadgeAward { BadgeType = BadgeType.Silver, AwardedAt = DateTime.UtcNow.AddDays(-5) }
        };

        _mockUserService.Setup(s => s.GetUserByIdAsync(userId)).ReturnsAsync(user);
        _mockLoopScoreService.Setup(s => s.GetUserBadgesAsync(userId)).ReturnsAsync(badges);

        //act
        var result = await _controller.GetUserBadges(userId);

        //assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedBadges = Assert.IsType<List<BadgeAward>>(okResult.Value);
        Assert.Equal(2, returnedBadges.Count);
        Assert.Equal(BadgeType.Bronze, returnedBadges[0].BadgeType);
        Assert.Equal(BadgeType.Silver, returnedBadges[1].BadgeType);
    }

    [Fact]
    public async Task GetUserBadges_Returns404_WhenUserNotFound()
    {
        //arrange
        var userId = "nonexistent";

        _mockUserService.Setup(s => s.GetUserByIdAsync(userId)).ReturnsAsync((User?)null);

        //act
        var result = await _controller.GetUserBadges(userId);

        //assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.NotNull(notFoundResult.Value);
    }

    [Fact]
    public async Task GetBadgeProgress_ReturnsProgress_WhenUserExists()
    {
        //arrange
        var userId = "user123";

        var user = new User
        {
            Id = userId,
            Email = "test@example.com"
        };

        var progress = new Dictionary<BadgeType, BadgeProgress>
        {
            { BadgeType.ReliableBorrower, new BadgeProgress { CurrentCount = 7, RequiredCount = 10, DisplayText = "7/10 on-time returns" } },
            { BadgeType.GenerousLender, new BadgeProgress { CurrentCount = 20, RequiredCount = 50, DisplayText = "20/50 lending transactions" } }
        };

        _mockUserService.Setup(s => s.GetUserByIdAsync(userId)).ReturnsAsync(user);
        _mockLoopScoreService.Setup(s => s.GetAllBadgeProgressAsync(userId)).ReturnsAsync(progress);

        //act
        var result = await _controller.GetBadgeProgress(userId);

        //assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedProgress = Assert.IsType<Dictionary<BadgeType, BadgeProgress>>(okResult.Value);
        Assert.Equal(2, returnedProgress.Count);
        Assert.Equal(7, returnedProgress[BadgeType.ReliableBorrower].CurrentCount);
        Assert.Equal(10, returnedProgress[BadgeType.ReliableBorrower].RequiredCount);
    }

    [Fact]
    public async Task GetBadgeProgress_Returns404_WhenUserNotFound()
    {
        //arrange
        var userId = "nonexistent";

        _mockUserService.Setup(s => s.GetUserByIdAsync(userId)).ReturnsAsync((User?)null);

        //act
        var result = await _controller.GetBadgeProgress(userId);

        //assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.NotNull(notFoundResult.Value);
    }

    [Fact]
    public async Task GetBadgeRarities_ReturnsRarities_Successfully()
    {
        //arrange
        var rarities = new Dictionary<BadgeType, BadgeRarity>
        {
            { BadgeType.Bronze, new BadgeRarity { BadgeType = BadgeType.Bronze, UsersWithBadge = 50, TotalActiveUsers = 100, Percentage = 50.0, RarityCategory = "Common" } },
            { BadgeType.Silver, new BadgeRarity { BadgeType = BadgeType.Silver, UsersWithBadge = 20, TotalActiveUsers = 100, Percentage = 20.0, RarityCategory = "Rare" } },
            { BadgeType.Gold, new BadgeRarity { BadgeType = BadgeType.Gold, UsersWithBadge = 5, TotalActiveUsers = 100, Percentage = 5.0, RarityCategory = "Very Rare" } }
        };

        _mockLoopScoreService.Setup(s => s.GetAllBadgeRaritiesAsync()).ReturnsAsync(rarities);

        //act
        var result = await _controller.GetBadgeRarities();

        //assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedRarities = Assert.IsType<Dictionary<BadgeType, BadgeRarity>>(okResult.Value);
        Assert.Equal(3, returnedRarities.Count);
        Assert.Equal(50, returnedRarities[BadgeType.Bronze].UsersWithBadge);
        Assert.Equal("Common", returnedRarities[BadgeType.Bronze].RarityCategory);
        Assert.Equal(20, returnedRarities[BadgeType.Silver].UsersWithBadge);
        Assert.Equal("Rare", returnedRarities[BadgeType.Silver].RarityCategory);
    }
    [Fact]
    public async Task GetPublicProfile_Returns200WithPublicProfileDto_WhenValidUserIdAndSharedLoops()
    {
        //arrange
        var requestingUserId = "user123";
        var targetUserId = "user456";
        
        SetupAuthenticatedUser(requestingUserId);

        var expectedProfile = new PublicProfileDto
        {
            UserId = targetUserId,
            FirstName = "John",
            LastName = "Doe",
            LoopScore = 25,
            Badges = new List<BadgeDto>
            {
                new BadgeDto { BadgeType = "Bronze", AwardedAt = DateTime.UtcNow.AddDays(-10) }
            },
            ScoreHistory = new List<ScoreHistoryEntryDto>
            {
                new ScoreHistoryEntryDto { Timestamp = DateTime.UtcNow.AddDays(-5), Points = 5, ActionType = "BorrowCompleted", ItemRequestId = "req1", ItemName = "Test Item" }
            }
        };

        _mockUserService.Setup(s => s.GetPublicProfileAsync(requestingUserId, targetUserId))
            .ReturnsAsync(expectedProfile);

        //act
        var result = await _controller.GetPublicProfile(targetUserId);

        //assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedProfile = Assert.IsType<PublicProfileDto>(okResult.Value);
        Assert.Equal(targetUserId, returnedProfile.UserId);
        Assert.Equal("John", returnedProfile.FirstName);
        Assert.Equal("Doe", returnedProfile.LastName);
        Assert.Equal(25, returnedProfile.LoopScore);
        Assert.Single(returnedProfile.Badges);
        Assert.Single(returnedProfile.ScoreHistory);
    }

    [Fact]
    public async Task GetPublicProfile_Returns404_WhenUserNotFound()
    {
        //arrange
        var requestingUserId = "user123";
        var targetUserId = "nonexistent";
        
        SetupAuthenticatedUser(requestingUserId);

        _mockUserService.Setup(s => s.GetPublicProfileAsync(requestingUserId, targetUserId))
            .ThrowsAsync(new KeyNotFoundException("User not found"));

        //act
        var result = await _controller.GetPublicProfile(targetUserId);

        //assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.NotNull(notFoundResult.Value);
    }

    [Fact]
    public async Task GetPublicProfile_Returns403_WhenNoSharedLoops()
    {
        //arrange
        var requestingUserId = "user123";
        var targetUserId = "user456";
        
        SetupAuthenticatedUser(requestingUserId);

        _mockUserService.Setup(s => s.GetPublicProfileAsync(requestingUserId, targetUserId))
            .ThrowsAsync(new UnauthorizedAccessException("You can only view profiles of users in your loops"));

        //act
        var result = await _controller.GetPublicProfile(targetUserId);

        //assert
        var forbiddenResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(403, forbiddenResult.StatusCode);
        Assert.NotNull(forbiddenResult.Value);
    }

    [Fact]
    public async Task GetPublicProfile_Returns401_WhenUnauthenticatedRequest()
    {
        //arrange
        var targetUserId = "user456";
        
        // Do not setup authenticated user - simulate unauthenticated request
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() }
        };

        //act
        var result = await _controller.GetPublicProfile(targetUserId);

        //assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result.Result);
        Assert.NotNull(unauthorizedResult.Value);
    }

    [Fact]
    public async Task GetPublicProfile_ResponseDoesNotIncludeEmailOrAddress()
    {
        //arrange
        var requestingUserId = "user123";
        var targetUserId = "user456";
        
        SetupAuthenticatedUser(requestingUserId);

        var expectedProfile = new PublicProfileDto
        {
            UserId = targetUserId,
            FirstName = "John",
            LastName = "Doe",
            LoopScore = 25,
            Badges = new List<BadgeDto>(),
            ScoreHistory = new List<ScoreHistoryEntryDto>()
        };

        _mockUserService.Setup(s => s.GetPublicProfileAsync(requestingUserId, targetUserId))
            .ReturnsAsync(expectedProfile);

        //act
        var result = await _controller.GetPublicProfile(targetUserId);

        //assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedProfile = Assert.IsType<PublicProfileDto>(okResult.Value);
        
        // Verify that PublicProfileDto does not have Email or StreetAddress properties
        var profileType = returnedProfile.GetType();
        Assert.Null(profileType.GetProperty("Email"));
        Assert.Null(profileType.GetProperty("StreetAddress"));
        Assert.Null(profileType.GetProperty("Address"));
        
        // Verify that only expected public properties are present
        Assert.NotNull(profileType.GetProperty("UserId"));
        Assert.NotNull(profileType.GetProperty("FirstName"));
        Assert.NotNull(profileType.GetProperty("LastName"));
        Assert.NotNull(profileType.GetProperty("LoopScore"));
        Assert.NotNull(profileType.GetProperty("Badges"));
        Assert.NotNull(profileType.GetProperty("ScoreHistory"));
    }
}
