using Api.Controllers;
using Api.Models;
using Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
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
}
