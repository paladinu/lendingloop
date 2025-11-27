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
            request.VisibleToFutureLoops,
            null))
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
            It.IsAny<bool>(),
            It.IsAny<List<string>?>()))
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
            request.VisibleToFutureLoops,
            null))
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

    // Search endpoint tests
    [Fact]
    public async Task SearchItems_ReturnsOk_WhenSearchIsSuccessful()
    {
        //arrange
        var loopId = "loop123";
        var filter = new ItemSearchFilter
        {
            SearchText = "drill",
            Tags = new List<string> { "tools" },
            IsAvailable = true,
            PageNumber = 1,
            PageSize = 20
        };

        var searchResult = new ItemSearchResult
        {
            Items = new List<SharedItem>
            {
                new SharedItem { Id = "item1", Name = "Power Drill", Tags = new List<string> { "tools" }, IsAvailable = true },
                new SharedItem { Id = "item2", Name = "Drill Bits", Tags = new List<string> { "tools" }, IsAvailable = true }
            },
            TotalCount = 2,
            PageNumber = 1,
            PageSize = 20,
            TotalPages = 1
        };

        _mockItemsService.Setup(s => s.SearchItemsAsync(loopId, filter))
            .ReturnsAsync(searchResult);

        //act
        var result = await _controller.SearchItems(loopId, filter);

        //assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedResult = Assert.IsType<ItemSearchResult>(okResult.Value);
        Assert.Equal(2, returnedResult.Items.Count);
        Assert.Equal(2, returnedResult.TotalCount);
        Assert.Equal(1, returnedResult.PageNumber);
        _mockItemsService.Verify(s => s.SearchItemsAsync(loopId, filter), Times.Once);
    }

    [Fact]
    public async Task SearchItems_ReturnsOk_WithEmptyResults_WhenNoItemsMatch()
    {
        //arrange
        var loopId = "loop123";
        var filter = new ItemSearchFilter
        {
            SearchText = "nonexistent",
            PageNumber = 1,
            PageSize = 20
        };

        var searchResult = new ItemSearchResult
        {
            Items = new List<SharedItem>(),
            TotalCount = 0,
            PageNumber = 1,
            PageSize = 20,
            TotalPages = 0
        };

        _mockItemsService.Setup(s => s.SearchItemsAsync(loopId, filter))
            .ReturnsAsync(searchResult);

        //act
        var result = await _controller.SearchItems(loopId, filter);

        //assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedResult = Assert.IsType<ItemSearchResult>(okResult.Value);
        Assert.Empty(returnedResult.Items);
        Assert.Equal(0, returnedResult.TotalCount);
        _mockItemsService.Verify(s => s.SearchItemsAsync(loopId, filter), Times.Once);
    }

    [Fact]
    public async Task SearchItems_ReturnsOk_WhenLoopIdIsEmpty()
    {
        //arrange
        var loopId = "";
        var filter = new ItemSearchFilter { PageNumber = 1, PageSize = 20 };

        var searchResult = new ItemSearchResult
        {
            Items = new List<SharedItem>(),
            TotalCount = 0,
            PageNumber = 1,
            PageSize = 20,
            TotalPages = 0
        };

        _mockItemsService.Setup(s => s.SearchItemsAsync(loopId, filter))
            .ReturnsAsync(searchResult);

        //act
        var result = await _controller.SearchItems(loopId, filter);

        //assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.IsType<ItemSearchResult>(okResult.Value);
    }

    [Fact]
    public async Task SearchItems_ReturnsBadRequest_WhenPageNumberIsInvalid()
    {
        //arrange
        var loopId = "loop123";
        var filter = new ItemSearchFilter { PageNumber = 0, PageSize = 20 };

        //act
        var result = await _controller.SearchItems(loopId, filter);

        //assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("Invalid pagination parameters: PageNumber must be at least 1", badRequestResult.Value);
    }

    [Fact]
    public async Task SearchItems_ReturnsBadRequest_WhenPageSizeIsInvalid()
    {
        //arrange
        var loopId = "loop123";
        var filter = new ItemSearchFilter { PageNumber = 1, PageSize = 0 };

        //act
        var result = await _controller.SearchItems(loopId, filter);

        //assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("Invalid pagination parameters: PageSize must be between 1 and 100", badRequestResult.Value);
    }

    [Fact]
    public async Task SearchItems_ReturnsBadRequest_WhenPageSizeExceedsMaximum()
    {
        //arrange
        var loopId = "loop123";
        var filter = new ItemSearchFilter { PageNumber = 1, PageSize = 150 };

        //act
        var result = await _controller.SearchItems(loopId, filter);

        //assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("Invalid pagination parameters: PageSize must be between 1 and 100", badRequestResult.Value);
    }

    [Fact]
    public async Task SearchItems_HandlesMultipleFilters_Correctly()
    {
        //arrange
        var loopId = "loop123";
        var filter = new ItemSearchFilter
        {
            SearchText = "drill",
            Tags = new List<string> { "tools", "power" },
            IsAvailable = true,
            OwnerIds = new List<string> { "user1", "user2" },
            PageNumber = 1,
            PageSize = 20
        };

        var searchResult = new ItemSearchResult
        {
            Items = new List<SharedItem>
            {
                new SharedItem 
                { 
                    Id = "item1", 
                    Name = "Power Drill", 
                    Tags = new List<string> { "tools", "power" }, 
                    IsAvailable = true,
                    UserId = "user1"
                }
            },
            TotalCount = 1,
            PageNumber = 1,
            PageSize = 20,
            TotalPages = 1
        };

        _mockItemsService.Setup(s => s.SearchItemsAsync(loopId, filter))
            .ReturnsAsync(searchResult);

        //act
        var result = await _controller.SearchItems(loopId, filter);

        //assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedResult = Assert.IsType<ItemSearchResult>(okResult.Value);
        Assert.Single(returnedResult.Items);
        Assert.Equal("Power Drill", returnedResult.Items[0].Name);
        _mockItemsService.Verify(s => s.SearchItemsAsync(loopId, filter), Times.Once);
    }

    [Fact]
    public async Task SearchItems_ReturnsInternalServerError_WhenServiceThrowsException()
    {
        //arrange
        var loopId = "loop123";
        var filter = new ItemSearchFilter { PageNumber = 1, PageSize = 20 };

        _mockItemsService.Setup(s => s.SearchItemsAsync(loopId, filter))
            .ThrowsAsync(new Exception("Database error"));

        //act
        var result = await _controller.SearchItems(loopId, filter);

        //assert
        var statusResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, statusResult.StatusCode);
        Assert.Equal("Internal server error: Database error", statusResult.Value);
    }

    // GetDistinctOwners endpoint tests
    [Fact]
    public async Task GetDistinctOwners_ReturnsOk_WhenOwnersExist()
    {
        //arrange
        var loopId = "loop123";
        var expectedOwners = new List<string> { "user1", "user2", "user3" };

        _mockItemsService.Setup(s => s.GetDistinctOwnersInLoopAsync(loopId))
            .ReturnsAsync(expectedOwners);

        //act
        var result = await _controller.GetDistinctOwners(loopId);

        //assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedOwners = Assert.IsAssignableFrom<List<string>>(okResult.Value);
        Assert.Equal(3, returnedOwners.Count);
        Assert.Equal(expectedOwners, returnedOwners);
        _mockItemsService.Verify(s => s.GetDistinctOwnersInLoopAsync(loopId), Times.Once);
    }

    [Fact]
    public async Task GetDistinctOwners_ReturnsOk_WithEmptyList_WhenNoOwnersExist()
    {
        //arrange
        var loopId = "loop123";
        var emptyOwners = new List<string>();

        _mockItemsService.Setup(s => s.GetDistinctOwnersInLoopAsync(loopId))
            .ReturnsAsync(emptyOwners);

        //act
        var result = await _controller.GetDistinctOwners(loopId);

        //assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedOwners = Assert.IsAssignableFrom<List<string>>(okResult.Value);
        Assert.Empty(returnedOwners);
        _mockItemsService.Verify(s => s.GetDistinctOwnersInLoopAsync(loopId), Times.Once);
    }

    [Fact]
    public async Task GetDistinctOwners_ReturnsOk_WhenLoopIdIsEmpty()
    {
        //arrange
        var loopId = "";
        var emptyOwners = new List<string>();

        _mockItemsService.Setup(s => s.GetDistinctOwnersInLoopAsync(loopId))
            .ReturnsAsync(emptyOwners);

        //act
        var result = await _controller.GetDistinctOwners(loopId);

        //assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedOwners = Assert.IsAssignableFrom<List<string>>(okResult.Value);
        Assert.Empty(returnedOwners);
    }

    [Fact]
    public async Task GetDistinctOwners_ReturnsInternalServerError_WhenServiceThrowsException()
    {
        //arrange
        var loopId = "loop123";

        _mockItemsService.Setup(s => s.GetDistinctOwnersInLoopAsync(loopId))
            .ThrowsAsync(new Exception("Database error"));

        //act
        var result = await _controller.GetDistinctOwners(loopId);

        //assert
        var statusResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, statusResult.StatusCode);
        Assert.Equal("Internal server error: Database error", statusResult.Value);
    }

    [Fact]
    public async Task GetDistinctOwners_ReturnsDistinctOwners_WhenMultipleItemsPerOwner()
    {
        //arrange
        var loopId = "loop123";
        var expectedOwners = new List<string> { "user1", "user2" }; // Distinct owners

        _mockItemsService.Setup(s => s.GetDistinctOwnersInLoopAsync(loopId))
            .ReturnsAsync(expectedOwners);

        //act
        var result = await _controller.GetDistinctOwners(loopId);

        //assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedOwners = Assert.IsAssignableFrom<List<string>>(okResult.Value);
        Assert.Equal(2, returnedOwners.Count);
        Assert.Equal(expectedOwners.Distinct().Count(), returnedOwners.Count);
        _mockItemsService.Verify(s => s.GetDistinctOwnersInLoopAsync(loopId), Times.Once);
    }
}
