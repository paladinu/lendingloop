using Api.Controllers;
using Api.DTOs;
using Api.Models;
using Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;
using Xunit;

namespace Api.Tests;

public class ItemRequestControllerTests
{
    private readonly Mock<IItemRequestService> _mockItemRequestService;
    private readonly ItemRequestController _controller;
    private readonly string _testUserId = "user123";

    public ItemRequestControllerTests()
    {
        _mockItemRequestService = new Mock<IItemRequestService>();
        _controller = new ItemRequestController(_mockItemRequestService.Object);

        // Setup HttpContext with authenticated user
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
    public async Task CreateRequest_ValidRequest_ReturnsCreatedResult()
    {
        //arrange
        var dto = new CreateItemRequestDto { ItemId = "item123" };
        var expectedRequest = new ItemRequest
        {
            Id = "request123",
            ItemId = "item123",
            RequesterId = _testUserId,
            OwnerId = "owner123",
            Status = RequestStatus.Pending
        };

        _mockItemRequestService.Setup(s => s.CreateRequestAsync("item123", _testUserId, null, null))
            .ReturnsAsync(expectedRequest);

        //act
        var result = await _controller.CreateRequest(dto);

        //assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var returnedRequest = Assert.IsType<ItemRequest>(createdResult.Value);
        Assert.Equal(expectedRequest.Id, returnedRequest.Id);
        Assert.Equal(expectedRequest.ItemId, returnedRequest.ItemId);
    }

    [Fact]
    public async Task CreateRequest_ItemNotFound_ReturnsNotFound()
    {
        //arrange
        var dto = new CreateItemRequestDto { ItemId = "nonexistent" };

        _mockItemRequestService.Setup(s => s.CreateRequestAsync("nonexistent", _testUserId, null, null))
            .ThrowsAsync(new ArgumentException("Item not found"));

        //act
        var result = await _controller.CreateRequest(dto);

        //assert
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task CreateRequest_SelfRequest_ReturnsBadRequest()
    {
        //arrange
        var dto = new CreateItemRequestDto { ItemId = "item123" };

        _mockItemRequestService.Setup(s => s.CreateRequestAsync("item123", _testUserId, null, null))
            .ThrowsAsync(new InvalidOperationException("Cannot request your own item"));

        //act
        var result = await _controller.CreateRequest(dto);

        //assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task CreateRequest_EmptyItemId_ReturnsBadRequest()
    {
        //arrange
        var dto = new CreateItemRequestDto { ItemId = "" };

        //act
        var result = await _controller.CreateRequest(dto);

        //assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetMyRequests_ReturnsListOfRequests()
    {
        //arrange
        var expectedRequests = new List<ItemRequest>
        {
            new ItemRequest { Id = "req1", RequesterId = _testUserId, Status = RequestStatus.Pending },
            new ItemRequest { Id = "req2", RequesterId = _testUserId, Status = RequestStatus.Approved }
        };

        var enrichedRequests = new List<ItemRequestResponse>
        {
            new ItemRequestResponse { Id = "req1", RequesterId = _testUserId, Status = RequestStatus.Pending },
            new ItemRequestResponse { Id = "req2", RequesterId = _testUserId, Status = RequestStatus.Approved }
        };

        _mockItemRequestService.Setup(s => s.GetRequestsByRequesterAsync(_testUserId))
            .ReturnsAsync(expectedRequests);
        
        _mockItemRequestService.Setup(s => s.EnrichItemRequestsAsync(expectedRequests))
            .ReturnsAsync(enrichedRequests);

        //act
        var result = await _controller.GetMyRequests();

        //assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedRequests = Assert.IsType<List<ItemRequestResponse>>(okResult.Value);
        Assert.Equal(2, returnedRequests.Count);
    }

    [Fact]
    public async Task GetPendingRequests_ReturnsListOfPendingRequests()
    {
        //arrange
        var expectedRequests = new List<ItemRequest>
        {
            new ItemRequest { Id = "req1", OwnerId = _testUserId, Status = RequestStatus.Pending },
            new ItemRequest { Id = "req2", OwnerId = _testUserId, Status = RequestStatus.Pending }
        };

        var enrichedRequests = new List<ItemRequestResponse>
        {
            new ItemRequestResponse { Id = "req1", OwnerId = _testUserId, Status = RequestStatus.Pending },
            new ItemRequestResponse { Id = "req2", OwnerId = _testUserId, Status = RequestStatus.Pending }
        };

        _mockItemRequestService.Setup(s => s.GetPendingRequestsByOwnerAsync(_testUserId))
            .ReturnsAsync(expectedRequests);
        
        _mockItemRequestService.Setup(s => s.EnrichItemRequestsAsync(expectedRequests))
            .ReturnsAsync(enrichedRequests);

        //act
        var result = await _controller.GetPendingRequests();

        //assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedRequests = Assert.IsType<List<ItemRequestResponse>>(okResult.Value);
        Assert.Equal(2, returnedRequests.Count);
        Assert.All(returnedRequests, r => Assert.Equal(RequestStatus.Pending, r.Status));
    }

    [Fact]
    public async Task GetRequestsForItem_ReturnsListOfRequests()
    {
        //arrange
        var itemId = "item123";
        var expectedRequests = new List<ItemRequest>
        {
            new ItemRequest { Id = "req1", ItemId = itemId, Status = RequestStatus.Pending },
            new ItemRequest { Id = "req2", ItemId = itemId, Status = RequestStatus.Approved }
        };

        _mockItemRequestService.Setup(s => s.GetRequestsByItemIdAsync(itemId))
            .ReturnsAsync(expectedRequests);

        //act
        var result = await _controller.GetRequestsForItem(itemId);

        //assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedRequests = Assert.IsType<List<ItemRequest>>(okResult.Value);
        Assert.Equal(2, returnedRequests.Count);
    }

    [Fact]
    public async Task ApproveRequest_ValidApproval_ReturnsOkResult()
    {
        //arrange
        var requestId = "request123";
        var approvedRequest = new ItemRequest
        {
            Id = requestId,
            OwnerId = _testUserId,
            Status = RequestStatus.Approved,
            RespondedAt = DateTime.UtcNow
        };

        _mockItemRequestService.Setup(s => s.ApproveRequestAsync(requestId, _testUserId))
            .ReturnsAsync(approvedRequest);

        //act
        var result = await _controller.ApproveRequest(requestId);

        //assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedRequest = Assert.IsType<ItemRequest>(okResult.Value);
        Assert.Equal(RequestStatus.Approved, returnedRequest.Status);
    }

    [Fact]
    public async Task ApproveRequest_NonOwner_ReturnsForbid()
    {
        //arrange
        var requestId = "request123";

        _mockItemRequestService.Setup(s => s.ApproveRequestAsync(requestId, _testUserId))
            .ThrowsAsync(new UnauthorizedAccessException("Only the item owner can approve requests"));

        //act
        var result = await _controller.ApproveRequest(requestId);

        //assert
        Assert.IsType<ForbidResult>(result.Result);
    }

    [Fact]
    public async Task ApproveRequest_NonPendingRequest_ReturnsBadRequest()
    {
        //arrange
        var requestId = "request123";

        _mockItemRequestService.Setup(s => s.ApproveRequestAsync(requestId, _testUserId))
            .ThrowsAsync(new InvalidOperationException("Only pending requests can be approved"));

        //act
        var result = await _controller.ApproveRequest(requestId);

        //assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task ApproveRequest_RequestNotFound_ReturnsNotFound()
    {
        //arrange
        var requestId = "nonexistent";

        _mockItemRequestService.Setup(s => s.ApproveRequestAsync(requestId, _testUserId))
            .ReturnsAsync((ItemRequest?)null);

        //act
        var result = await _controller.ApproveRequest(requestId);

        //assert
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task RejectRequest_ValidRejection_ReturnsOkResult()
    {
        //arrange
        var requestId = "request123";
        var rejectedRequest = new ItemRequest
        {
            Id = requestId,
            OwnerId = _testUserId,
            Status = RequestStatus.Rejected,
            RespondedAt = DateTime.UtcNow
        };

        _mockItemRequestService.Setup(s => s.RejectRequestAsync(requestId, _testUserId))
            .ReturnsAsync(rejectedRequest);

        //act
        var result = await _controller.RejectRequest(requestId);

        //assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedRequest = Assert.IsType<ItemRequest>(okResult.Value);
        Assert.Equal(RequestStatus.Rejected, returnedRequest.Status);
    }

    [Fact]
    public async Task RejectRequest_NonOwner_ReturnsForbid()
    {
        //arrange
        var requestId = "request123";

        _mockItemRequestService.Setup(s => s.RejectRequestAsync(requestId, _testUserId))
            .ThrowsAsync(new UnauthorizedAccessException("Only the item owner can reject requests"));

        //act
        var result = await _controller.RejectRequest(requestId);

        //assert
        Assert.IsType<ForbidResult>(result.Result);
    }

    [Fact]
    public async Task CancelRequest_ValidCancellation_ReturnsOkResult()
    {
        //arrange
        var requestId = "request123";
        var cancelledRequest = new ItemRequest
        {
            Id = requestId,
            RequesterId = _testUserId,
            Status = RequestStatus.Cancelled,
            RespondedAt = DateTime.UtcNow
        };

        _mockItemRequestService.Setup(s => s.CancelRequestAsync(requestId, _testUserId))
            .ReturnsAsync(cancelledRequest);

        //act
        var result = await _controller.CancelRequest(requestId);

        //assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedRequest = Assert.IsType<ItemRequest>(okResult.Value);
        Assert.Equal(RequestStatus.Cancelled, returnedRequest.Status);
    }

    [Fact]
    public async Task CancelRequest_NonRequester_ReturnsForbid()
    {
        //arrange
        var requestId = "request123";

        _mockItemRequestService.Setup(s => s.CancelRequestAsync(requestId, _testUserId))
            .ThrowsAsync(new UnauthorizedAccessException("Only the requester can cancel their request"));

        //act
        var result = await _controller.CancelRequest(requestId);

        //assert
        Assert.IsType<ForbidResult>(result.Result);
    }

    [Fact]
    public async Task CompleteRequest_ValidCompletion_ReturnsOkResult()
    {
        //arrange
        var requestId = "request123";
        var completedRequest = new ItemRequest
        {
            Id = requestId,
            OwnerId = _testUserId,
            Status = RequestStatus.Completed,
            CompletedAt = DateTime.UtcNow
        };

        _mockItemRequestService.Setup(s => s.CompleteRequestAsync(requestId, _testUserId))
            .ReturnsAsync(completedRequest);

        //act
        var result = await _controller.CompleteRequest(requestId);

        //assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedRequest = Assert.IsType<ItemRequest>(okResult.Value);
        Assert.Equal(RequestStatus.Completed, returnedRequest.Status);
    }

    [Fact]
    public async Task CompleteRequest_NonApprovedRequest_ReturnsBadRequest()
    {
        //arrange
        var requestId = "request123";

        _mockItemRequestService.Setup(s => s.CompleteRequestAsync(requestId, _testUserId))
            .ThrowsAsync(new InvalidOperationException("Only approved requests can be completed"));

        //act
        var result = await _controller.CompleteRequest(requestId);

        //assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task CreateRequest_WithValidMessage_ReturnsCreatedResult()
    {
        //arrange
        var message = "I need this for a weekend project";
        var dto = new CreateItemRequestDto { ItemId = "item123", Message = message };
        var expectedRequest = new ItemRequest
        {
            Id = "request123",
            ItemId = "item123",
            RequesterId = _testUserId,
            OwnerId = "owner123",
            Status = RequestStatus.Pending,
            Message = message
        };

        _mockItemRequestService.Setup(s => s.CreateRequestAsync("item123", _testUserId, message, null))
            .ReturnsAsync(expectedRequest);

        //act
        var result = await _controller.CreateRequest(dto);

        //assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var returnedRequest = Assert.IsType<ItemRequest>(createdResult.Value);
        Assert.Equal(expectedRequest.Message, returnedRequest.Message);
    }

    [Fact]
    public async Task CreateRequest_WithMessageExceeding500Characters_ReturnsBadRequest()
    {
        //arrange
        var longMessage = new string('a', 501);
        var dto = new CreateItemRequestDto { ItemId = "item123", Message = longMessage };

        //act
        var result = await _controller.CreateRequest(dto);

        //assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("Message cannot exceed 500 characters", badRequestResult.Value);
    }

    [Fact]
    public async Task CreateRequest_WithMessageExactly500Characters_ReturnsCreatedResult()
    {
        //arrange
        var message = new string('a', 500);
        var dto = new CreateItemRequestDto { ItemId = "item123", Message = message };
        var expectedRequest = new ItemRequest
        {
            Id = "request123",
            ItemId = "item123",
            RequesterId = _testUserId,
            OwnerId = "owner123",
            Status = RequestStatus.Pending,
            Message = message
        };

        _mockItemRequestService.Setup(s => s.CreateRequestAsync("item123", _testUserId, message, null))
            .ReturnsAsync(expectedRequest);

        //act
        var result = await _controller.CreateRequest(dto);

        //assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var returnedRequest = Assert.IsType<ItemRequest>(createdResult.Value);
        Assert.Equal(500, returnedRequest.Message?.Length);
    }

    [Fact]
    public async Task CreateRequest_WithNullMessage_ReturnsCreatedResult()
    {
        //arrange
        var dto = new CreateItemRequestDto { ItemId = "item123", Message = null };
        var expectedRequest = new ItemRequest
        {
            Id = "request123",
            ItemId = "item123",
            RequesterId = _testUserId,
            OwnerId = "owner123",
            Status = RequestStatus.Pending,
            Message = null
        };

        _mockItemRequestService.Setup(s => s.CreateRequestAsync("item123", _testUserId, null, null))
            .ReturnsAsync(expectedRequest);

        //act
        var result = await _controller.CreateRequest(dto);

        //assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var returnedRequest = Assert.IsType<ItemRequest>(createdResult.Value);
        Assert.Null(returnedRequest.Message);
    }

    [Fact]
    public async Task CreateRequest_WithEmptyMessage_ReturnsCreatedResult()
    {
        //arrange
        var dto = new CreateItemRequestDto { ItemId = "item123", Message = "" };
        var expectedRequest = new ItemRequest
        {
            Id = "request123",
            ItemId = "item123",
            RequesterId = _testUserId,
            OwnerId = "owner123",
            Status = RequestStatus.Pending,
            Message = ""
        };

        _mockItemRequestService.Setup(s => s.CreateRequestAsync("item123", _testUserId, "", null))
            .ReturnsAsync(expectedRequest);

        //act
        var result = await _controller.CreateRequest(dto);

        //assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var returnedRequest = Assert.IsType<ItemRequest>(createdResult.Value);
        Assert.Equal("", returnedRequest.Message);
    }
}
