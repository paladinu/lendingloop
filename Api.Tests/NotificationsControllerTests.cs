using Api.Controllers;
using Api.Models;
using Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using Xunit;

namespace Api.Tests;

public class NotificationsControllerTests
{
    private readonly Mock<INotificationService> _mockNotificationService;
    private readonly Mock<ILogger<NotificationsController>> _mockLogger;
    private readonly NotificationsController _controller;
    private readonly string _testUserId = "user123";

    public NotificationsControllerTests()
    {
        _mockNotificationService = new Mock<INotificationService>();
        _mockLogger = new Mock<ILogger<NotificationsController>>();

        _controller = new NotificationsController(
            _mockNotificationService.Object,
            _mockLogger.Object
        );

        // Set up authenticated user context
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, _testUserId)
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };
    }

    [Fact]
    public async Task GetNotifications_ReturnsOk_WithNotifications()
    {
        //arrange
        var notifications = new List<Notification>
        {
            new Notification
            {
                Id = "notif1",
                UserId = _testUserId,
                Type = NotificationType.ItemRequestCreated,
                Message = "Test notification 1",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            },
            new Notification
            {
                Id = "notif2",
                UserId = _testUserId,
                Type = NotificationType.ItemRequestApproved,
                Message = "Test notification 2",
                IsRead = true,
                CreatedAt = DateTime.UtcNow.AddHours(-1)
            }
        };

        _mockNotificationService.Setup(s => s.GetUserNotificationsAsync(_testUserId, 50))
            .ReturnsAsync(notifications);

        //act
        var result = await _controller.GetNotifications(null);

        //assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedNotifications = Assert.IsType<List<Notification>>(okResult.Value);
        Assert.Equal(2, returnedNotifications.Count);
        Assert.Equal("notif1", returnedNotifications[0].Id);
    }

    [Fact]
    public async Task GetNotifications_ReturnsOk_WithCustomLimit()
    {
        //arrange
        var notifications = new List<Notification>
        {
            new Notification
            {
                Id = "notif1",
                UserId = _testUserId,
                Type = NotificationType.ItemRequestCreated,
                Message = "Test notification",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            }
        };

        _mockNotificationService.Setup(s => s.GetUserNotificationsAsync(_testUserId, 10))
            .ReturnsAsync(notifications);

        //act
        var result = await _controller.GetNotifications(10);

        //assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedNotifications = Assert.IsType<List<Notification>>(okResult.Value);
        Assert.Single(returnedNotifications);
        _mockNotificationService.Verify(s => s.GetUserNotificationsAsync(_testUserId, 10), Times.Once);
    }

    [Fact]
    public async Task GetNotifications_ReturnsUnauthorized_WhenUserIdIsEmpty()
    {
        //arrange
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() }
        };

        //act
        var result = await _controller.GetNotifications(null);

        //assert
        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task GetUnreadCount_ReturnsOk_WithCount()
    {
        //arrange
        _mockNotificationService.Setup(s => s.GetUnreadCountAsync(_testUserId))
            .ReturnsAsync(5);

        //act
        var result = await _controller.GetUnreadCount();

        //assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = okResult.Value;
        var countProperty = response?.GetType().GetProperty("count");
        Assert.NotNull(countProperty);
        var count = (int?)countProperty.GetValue(response);
        Assert.Equal(5, count);
    }

    [Fact]
    public async Task GetUnreadCount_ReturnsUnauthorized_WhenUserIdIsEmpty()
    {
        //arrange
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() }
        };

        //act
        var result = await _controller.GetUnreadCount();

        //assert
        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task MarkAsRead_ReturnsOk_WhenNotificationExists()
    {
        //arrange
        var notificationId = "notif123";
        var notification = new Notification
        {
            Id = notificationId,
            UserId = _testUserId,
            Type = NotificationType.ItemRequestCreated,
            Message = "Test notification",
            IsRead = true,
            CreatedAt = DateTime.UtcNow
        };

        _mockNotificationService.Setup(s => s.MarkAsReadAsync(notificationId, _testUserId))
            .ReturnsAsync(notification);

        //act
        var result = await _controller.MarkAsRead(notificationId);

        //assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedNotification = Assert.IsType<Notification>(okResult.Value);
        Assert.Equal(notificationId, returnedNotification.Id);
        Assert.True(returnedNotification.IsRead);
    }

    [Fact]
    public async Task MarkAsRead_ReturnsNotFound_WhenNotificationDoesNotExist()
    {
        //arrange
        var notificationId = "nonexistent";
        _mockNotificationService.Setup(s => s.MarkAsReadAsync(notificationId, _testUserId))
            .ReturnsAsync((Notification?)null);

        //act
        var result = await _controller.MarkAsRead(notificationId);

        //assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        var response = notFoundResult.Value;
        var messageProperty = response?.GetType().GetProperty("message");
        Assert.NotNull(messageProperty);
        var message = messageProperty.GetValue(response) as string;
        Assert.Equal("Notification not found or access denied", message);
    }

    [Fact]
    public async Task MarkAsRead_ReturnsUnauthorized_WhenUserIdIsEmpty()
    {
        //arrange
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() }
        };

        //act
        var result = await _controller.MarkAsRead("notif123");

        //assert
        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task MarkAllAsRead_ReturnsOk_WithSuccess()
    {
        //arrange
        _mockNotificationService.Setup(s => s.MarkAllAsReadAsync(_testUserId))
            .ReturnsAsync(true);

        //act
        var result = await _controller.MarkAllAsRead();

        //assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = okResult.Value;
        var successProperty = response?.GetType().GetProperty("success");
        Assert.NotNull(successProperty);
        var success = (bool?)successProperty.GetValue(response);
        Assert.True(success);
    }

    [Fact]
    public async Task MarkAllAsRead_ReturnsUnauthorized_WhenUserIdIsEmpty()
    {
        //arrange
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() }
        };

        //act
        var result = await _controller.MarkAllAsRead();

        //assert
        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task DeleteNotification_ReturnsOk_WhenNotificationExists()
    {
        //arrange
        var notificationId = "notif123";
        _mockNotificationService.Setup(s => s.DeleteNotificationAsync(notificationId, _testUserId))
            .ReturnsAsync(true);

        //act
        var result = await _controller.DeleteNotification(notificationId);

        //assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = okResult.Value;
        var successProperty = response?.GetType().GetProperty("success");
        Assert.NotNull(successProperty);
        var success = (bool?)successProperty.GetValue(response);
        Assert.True(success);
    }

    [Fact]
    public async Task DeleteNotification_ReturnsNotFound_WhenNotificationDoesNotExist()
    {
        //arrange
        var notificationId = "nonexistent";
        _mockNotificationService.Setup(s => s.DeleteNotificationAsync(notificationId, _testUserId))
            .ReturnsAsync(false);

        //act
        var result = await _controller.DeleteNotification(notificationId);

        //assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        var response = notFoundResult.Value;
        var messageProperty = response?.GetType().GetProperty("message");
        Assert.NotNull(messageProperty);
        var message = messageProperty.GetValue(response) as string;
        Assert.Equal("Notification not found or access denied", message);
    }

    [Fact]
    public async Task DeleteNotification_ReturnsUnauthorized_WhenUserIdIsEmpty()
    {
        //arrange
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() }
        };

        //act
        var result = await _controller.DeleteNotification("notif123");

        //assert
        Assert.IsType<UnauthorizedResult>(result);
    }
}
