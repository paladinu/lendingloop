using Api.Controllers;
using Api.DTOs;
using Api.Models;
using Api.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;
using System.Security.Claims;
using Xunit;

namespace Api.Tests;

public class ItemsControllerTests
{
    private readonly Mock<IItemsService> _mockItemsService;
    private readonly Mock<IWebHostEnvironment> _mockEnvironment;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly ItemsController _controller;
    private readonly string _testUserId = "user123";

    public ItemsControllerTests()
    {
        _mockItemsService = new Mock<IItemsService>();
        _mockEnvironment = new Mock<IWebHostEnvironment>();
        _mockConfiguration = new Mock<IConfiguration>();

        _controller = new ItemsController(
            _mockItemsService.Object,
            _mockEnvironment.Object,
            _mockConfiguration.Object
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
    public async Task UpdateItem_ReturnsOk_WhenUpdateIsSuccessful()
    {
        //arrange
        var itemId = "item123";
        var request = new UpdateItemRequest
        {
            Name = "Updated Name",
            Description = "Updated Description",
            IsAvailable = false,
            VisibleToLoopIds = new List<string> { "loop1" },
            VisibleToAllLoops = false,
            VisibleToFutureLoops = true
        };

        var existingItem = new SharedItem
        {
            Id = itemId,
            UserId = _testUserId,
            Name = "Old Name"
        };

        var updatedItem = new SharedItem
        {
            Id = itemId,
            UserId = _testUserId,
            Name = request.Name,
            Description = request.Description,
            IsAvailable = request.IsAvailable,
            VisibleToLoopIds = request.VisibleToLoopIds,
            VisibleToAllLoops = request.VisibleToAllLoops,
            VisibleToFutureLoops = request.VisibleToFutureLoops
        };

        _mockItemsService.Setup(s => s.GetItemByIdAsync(itemId))
            .ReturnsAsync(existingItem);
        _mockItemsService.Setup(s => s.UpdateItemAsync(
            itemId,
            _testUserId,
            request.Name,
            request.Description,
            request.IsAvailable,
            request.VisibleToLoopIds,
            request.VisibleToAllLoops,
            request.VisibleToFutureLoops))
            .ReturnsAsync(updatedItem);

        //act
        var result = await _controller.UpdateItem(itemId, request);

        //assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedItem = Assert.IsType<SharedItem>(okResult.Value);
        Assert.Equal(request.Name, returnedItem.Name);
        Assert.Equal(request.Description, returnedItem.Description);
        Assert.Equal(request.IsAvailable, returnedItem.IsAvailable);
    }

    [Fact]
    public async Task UpdateItem_ReturnsUnauthorized_WhenUserIdNotInToken()
    {
        //arrange
        var itemId = "item123";
        var request = new UpdateItemRequest { Name = "Test" };

        // Remove user claims
        _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal();

        //act
        var result = await _controller.UpdateItem(itemId, request);

        //assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result.Result);
        Assert.Equal("User ID not found in token", unauthorizedResult.Value);
    }

    [Fact]
    public async Task UpdateItem_ReturnsBadRequest_WhenNameIsEmpty()
    {
        //arrange
        var itemId = "item123";
        var request = new UpdateItemRequest
        {
            Name = "",
            Description = "Description"
        };

        //act
        var result = await _controller.UpdateItem(itemId, request);

        //assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("Item name is required.", badRequestResult.Value);
    }

    [Fact]
    public async Task UpdateItem_ReturnsNotFound_WhenItemDoesNotExist()
    {
        //arrange
        var itemId = "nonexistent";
        var request = new UpdateItemRequest { Name = "Test" };

        _mockItemsService.Setup(s => s.GetItemByIdAsync(itemId))
            .ReturnsAsync((SharedItem)null!);

        //act
        var result = await _controller.UpdateItem(itemId, request);

        //assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Contains("not found", notFoundResult.Value?.ToString());
    }

    [Fact]
    public async Task UpdateItem_ReturnsForbid_WhenUserDoesNotOwnItem()
    {
        //arrange
        var itemId = "item123";
        var request = new UpdateItemRequest { Name = "Test" };

        var existingItem = new SharedItem
        {
            Id = itemId,
            UserId = "differentUser",
            Name = "Old Name"
        };

        _mockItemsService.Setup(s => s.GetItemByIdAsync(itemId))
            .ReturnsAsync(existingItem);

        //act
        var result = await _controller.UpdateItem(itemId, request);

        //assert
        Assert.IsType<ForbidResult>(result.Result);
    }

    [Fact]
    public async Task UpdateItem_ReturnsNotFound_WhenServiceReturnsNull()
    {
        //arrange
        var itemId = "item123";
        var request = new UpdateItemRequest { Name = "Test" };

        var existingItem = new SharedItem
        {
            Id = itemId,
            UserId = _testUserId,
            Name = "Old Name"
        };

        _mockItemsService.Setup(s => s.GetItemByIdAsync(itemId))
            .ReturnsAsync(existingItem);
        _mockItemsService.Setup(s => s.UpdateItemAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<bool>(),
            It.IsAny<List<string>>(),
            It.IsAny<bool>(),
            It.IsAny<bool>()))
            .ReturnsAsync((SharedItem)null!);

        //act
        var result = await _controller.UpdateItem(itemId, request);

        //assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Contains("not found", notFoundResult.Value?.ToString());
    }

    [Fact]
    public async Task UpdateItem_UpdatesAllFields_WhenRequestIsValid()
    {
        //arrange
        var itemId = "item123";
        var request = new UpdateItemRequest
        {
            Name = "New Name",
            Description = "New Description",
            IsAvailable = false,
            VisibleToLoopIds = new List<string> { "loop1", "loop2" },
            VisibleToAllLoops = true,
            VisibleToFutureLoops = true
        };

        var existingItem = new SharedItem
        {
            Id = itemId,
            UserId = _testUserId
        };

        var updatedItem = new SharedItem
        {
            Id = itemId,
            UserId = _testUserId,
            Name = request.Name,
            Description = request.Description,
            IsAvailable = request.IsAvailable,
            VisibleToLoopIds = request.VisibleToLoopIds,
            VisibleToAllLoops = request.VisibleToAllLoops,
            VisibleToFutureLoops = request.VisibleToFutureLoops,
            UpdatedAt = DateTime.UtcNow
        };

        _mockItemsService.Setup(s => s.GetItemByIdAsync(itemId))
            .ReturnsAsync(existingItem);
        _mockItemsService.Setup(s => s.UpdateItemAsync(
            itemId,
            _testUserId,
            request.Name,
            request.Description,
            request.IsAvailable,
            request.VisibleToLoopIds,
            request.VisibleToAllLoops,
            request.VisibleToFutureLoops))
            .ReturnsAsync(updatedItem);

        //act
        var result = await _controller.UpdateItem(itemId, request);

        //assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedItem = Assert.IsType<SharedItem>(okResult.Value);
        Assert.Equal(request.Name, returnedItem.Name);
        Assert.Equal(request.Description, returnedItem.Description);
        Assert.Equal(request.IsAvailable, returnedItem.IsAvailable);
        Assert.Equal(request.VisibleToLoopIds.Count, returnedItem.VisibleToLoopIds.Count);
        Assert.Equal(request.VisibleToAllLoops, returnedItem.VisibleToAllLoops);
        Assert.Equal(request.VisibleToFutureLoops, returnedItem.VisibleToFutureLoops);
    }
}
