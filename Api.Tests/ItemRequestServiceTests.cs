using Api.Models;
using Api.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Moq;
using Xunit;

namespace Api.Tests;

public class ItemRequestServiceTests
{
    private readonly Mock<IMongoDatabase> _mockDatabase;
    private readonly Mock<IMongoCollection<ItemRequest>> _mockRequestsCollection;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<IItemsService> _mockItemsService;
    private readonly Mock<INotificationService> _mockNotificationService;
    private readonly Mock<IEmailService> _mockEmailService;
    private readonly Mock<IUserService> _mockUserService;
    private readonly Mock<ILogger<ItemRequestService>> _mockLogger;
    private readonly ItemRequestService _service;

    public ItemRequestServiceTests()
    {
        _mockDatabase = new Mock<IMongoDatabase>();
        _mockRequestsCollection = new Mock<IMongoCollection<ItemRequest>>();
        _mockConfiguration = new Mock<IConfiguration>();
        _mockItemsService = new Mock<IItemsService>();
        _mockNotificationService = new Mock<INotificationService>();
        _mockEmailService = new Mock<IEmailService>();
        _mockUserService = new Mock<IUserService>();
        _mockLogger = new Mock<ILogger<ItemRequestService>>();

        _mockConfiguration.Setup(c => c["MongoDB:ItemRequestsCollectionName"]).Returns("itemrequests");
        _mockDatabase.Setup(db => db.GetCollection<ItemRequest>("itemrequests", null))
            .Returns(_mockRequestsCollection.Object);

        _service = new ItemRequestService(
            _mockDatabase.Object, 
            _mockConfiguration.Object, 
            _mockItemsService.Object,
            _mockNotificationService.Object,
            _mockEmailService.Object,
            _mockUserService.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task CreateRequestAsync_ValidRequest_CreatesRequest()
    {
        //arrange
        var itemId = "item123";
        var requesterId = "requester123";
        var ownerId = "owner123";
        var item = new SharedItem { Id = itemId, UserId = ownerId, Name = "Test Item" };

        _mockItemsService.Setup(s => s.GetItemByIdAsync(itemId)).ReturnsAsync(item);
        _mockRequestsCollection.Setup(c => c.InsertOneAsync(It.IsAny<ItemRequest>(), null, default))
            .Returns(Task.CompletedTask);

        //act
        var result = await _service.CreateRequestAsync(itemId, requesterId);

        //assert
        Assert.NotNull(result);
        Assert.Equal(itemId, result.ItemId);
        Assert.Equal(requesterId, result.RequesterId);
        Assert.Equal(ownerId, result.OwnerId);
        Assert.Equal(RequestStatus.Pending, result.Status);
        _mockRequestsCollection.Verify(c => c.InsertOneAsync(It.IsAny<ItemRequest>(), null, default), Times.Once);
    }

    [Fact]
    public async Task CreateRequestAsync_ItemNotFound_ThrowsArgumentException()
    {
        //arrange
        var itemId = "nonexistent";
        var requesterId = "requester123";

        _mockItemsService.Setup(s => s.GetItemByIdAsync(itemId)).ReturnsAsync((SharedItem?)null);

        //act & assert
        await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateRequestAsync(itemId, requesterId));
    }

    [Fact]
    public async Task CreateRequestAsync_RequesterIsOwner_ThrowsInvalidOperationException()
    {
        //arrange
        var itemId = "item123";
        var userId = "user123";
        var item = new SharedItem { Id = itemId, UserId = userId, Name = "Test Item" };

        _mockItemsService.Setup(s => s.GetItemByIdAsync(itemId)).ReturnsAsync(item);

        //act & assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CreateRequestAsync(itemId, userId));
    }

    [Fact]
    public async Task ApproveRequestAsync_ValidApproval_UpdatesRequestAndItemAvailability()
    {
        //arrange
        var requestId = "request123";
        var ownerId = "owner123";
        var itemId = "item123";
        var request = new ItemRequest
        {
            Id = requestId,
            ItemId = itemId,
            RequesterId = "requester123",
            OwnerId = ownerId,
            Status = RequestStatus.Pending
        };

        var mockCursor = new Mock<IAsyncCursor<ItemRequest>>();
        mockCursor.Setup(c => c.Current).Returns(new List<ItemRequest> { request });
        mockCursor.SetupSequence(c => c.MoveNext(It.IsAny<CancellationToken>())).Returns(true).Returns(false);
        mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true).ReturnsAsync(false);

        _mockRequestsCollection.Setup(c => c.FindAsync(
            It.IsAny<FilterDefinition<ItemRequest>>(),
            It.IsAny<FindOptions<ItemRequest, ItemRequest>>(),
            It.IsAny<CancellationToken>()
        )).ReturnsAsync(mockCursor.Object);

        // Mock for GetActiveRequestForItemAsync (no active request)
        var mockEmptyCursor = new Mock<IAsyncCursor<ItemRequest>>();
        mockEmptyCursor.Setup(c => c.Current).Returns(new List<ItemRequest>());
        mockEmptyCursor.SetupSequence(c => c.MoveNext(It.IsAny<CancellationToken>())).Returns(true).Returns(false);
        mockEmptyCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true).ReturnsAsync(false);

        _mockRequestsCollection.SetupSequence(c => c.FindAsync(
            It.IsAny<FilterDefinition<ItemRequest>>(),
            It.IsAny<FindOptions<ItemRequest, ItemRequest>>(),
            It.IsAny<CancellationToken>()
        )).ReturnsAsync(mockCursor.Object).ReturnsAsync(mockEmptyCursor.Object);

        var approvedRequest = new ItemRequest
        {
            Id = requestId,
            ItemId = itemId,
            RequesterId = "requester123",
            OwnerId = ownerId,
            Status = RequestStatus.Approved,
            RespondedAt = DateTime.UtcNow
        };

        _mockRequestsCollection.Setup(c => c.FindOneAndUpdateAsync(
            It.IsAny<FilterDefinition<ItemRequest>>(),
            It.IsAny<UpdateDefinition<ItemRequest>>(),
            It.IsAny<FindOneAndUpdateOptions<ItemRequest, ItemRequest>>(),
            It.IsAny<CancellationToken>()
        )).ReturnsAsync(approvedRequest);

        _mockItemsService.Setup(s => s.GetItemByIdAsync(itemId))
            .ReturnsAsync(new SharedItem { Id = itemId, IsAvailable = true });
        _mockItemsService.Setup(s => s.UpdateItemAvailabilityAsync(itemId, false))
            .ReturnsAsync(new SharedItem { Id = itemId, IsAvailable = false });

        //act
        var result = await _service.ApproveRequestAsync(requestId, ownerId);

        //assert
        Assert.NotNull(result);
        Assert.Equal(RequestStatus.Approved, result.Status);
        _mockItemsService.Verify(s => s.UpdateItemAvailabilityAsync(itemId, false), Times.Once);
    }

    [Fact]
    public async Task ApproveRequestAsync_NonOwner_ThrowsUnauthorizedAccessException()
    {
        //arrange
        var requestId = "request123";
        var ownerId = "owner123";
        var nonOwnerId = "other123";
        var request = new ItemRequest
        {
            Id = requestId,
            ItemId = "item123",
            RequesterId = "requester123",
            OwnerId = ownerId,
            Status = RequestStatus.Pending
        };

        var mockCursor = new Mock<IAsyncCursor<ItemRequest>>();
        mockCursor.Setup(c => c.Current).Returns(new List<ItemRequest> { request });
        mockCursor.SetupSequence(c => c.MoveNext(It.IsAny<CancellationToken>())).Returns(true).Returns(false);
        mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true).ReturnsAsync(false);

        _mockRequestsCollection.Setup(c => c.FindAsync(
            It.IsAny<FilterDefinition<ItemRequest>>(),
            It.IsAny<FindOptions<ItemRequest, ItemRequest>>(),
            It.IsAny<CancellationToken>()
        )).ReturnsAsync(mockCursor.Object);

        //act & assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _service.ApproveRequestAsync(requestId, nonOwnerId));
    }

    [Fact]
    public async Task ApproveRequestAsync_NonPendingRequest_ThrowsInvalidOperationException()
    {
        //arrange
        var requestId = "request123";
        var ownerId = "owner123";
        var request = new ItemRequest
        {
            Id = requestId,
            ItemId = "item123",
            RequesterId = "requester123",
            OwnerId = ownerId,
            Status = RequestStatus.Approved
        };

        var mockCursor = new Mock<IAsyncCursor<ItemRequest>>();
        mockCursor.Setup(c => c.Current).Returns(new List<ItemRequest> { request });
        mockCursor.SetupSequence(c => c.MoveNext(It.IsAny<CancellationToken>())).Returns(true).Returns(false);
        mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true).ReturnsAsync(false);

        _mockRequestsCollection.Setup(c => c.FindAsync(
            It.IsAny<FilterDefinition<ItemRequest>>(),
            It.IsAny<FindOptions<ItemRequest, ItemRequest>>(),
            It.IsAny<CancellationToken>()
        )).ReturnsAsync(mockCursor.Object);

        //act & assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.ApproveRequestAsync(requestId, ownerId));
    }

    [Fact]
    public async Task ApproveRequestAsync_ExistingApprovedRequest_ThrowsInvalidOperationException()
    {
        //arrange
        var requestId = "request123";
        var ownerId = "owner123";
        var itemId = "item123";
        var request = new ItemRequest
        {
            Id = requestId,
            ItemId = itemId,
            RequesterId = "requester123",
            OwnerId = ownerId,
            Status = RequestStatus.Pending
        };

        var existingApprovedRequest = new ItemRequest
        {
            Id = "other123",
            ItemId = itemId,
            RequesterId = "other456",
            OwnerId = ownerId,
            Status = RequestStatus.Approved
        };

        var mockCursor = new Mock<IAsyncCursor<ItemRequest>>();
        mockCursor.Setup(c => c.Current).Returns(new List<ItemRequest> { request });
        mockCursor.SetupSequence(c => c.MoveNext(It.IsAny<CancellationToken>())).Returns(true).Returns(false);
        mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true).ReturnsAsync(false);

        var mockActiveCursor = new Mock<IAsyncCursor<ItemRequest>>();
        mockActiveCursor.Setup(c => c.Current).Returns(new List<ItemRequest> { existingApprovedRequest });
        mockActiveCursor.SetupSequence(c => c.MoveNext(It.IsAny<CancellationToken>())).Returns(true).Returns(false);
        mockActiveCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true).ReturnsAsync(false);

        _mockRequestsCollection.SetupSequence(c => c.FindAsync(
            It.IsAny<FilterDefinition<ItemRequest>>(),
            It.IsAny<FindOptions<ItemRequest, ItemRequest>>(),
            It.IsAny<CancellationToken>()
        )).ReturnsAsync(mockCursor.Object).ReturnsAsync(mockActiveCursor.Object);

        //act & assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.ApproveRequestAsync(requestId, ownerId));
    }

    [Fact]
    public async Task RejectRequestAsync_ValidRejection_UpdatesRequestStatus()
    {
        //arrange
        var requestId = "request123";
        var ownerId = "owner123";
        var request = new ItemRequest
        {
            Id = requestId,
            ItemId = "item123",
            RequesterId = "requester123",
            OwnerId = ownerId,
            Status = RequestStatus.Pending
        };

        var mockCursor = new Mock<IAsyncCursor<ItemRequest>>();
        mockCursor.Setup(c => c.Current).Returns(new List<ItemRequest> { request });
        mockCursor.SetupSequence(c => c.MoveNext(It.IsAny<CancellationToken>())).Returns(true).Returns(false);
        mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true).ReturnsAsync(false);

        _mockRequestsCollection.Setup(c => c.FindAsync(
            It.IsAny<FilterDefinition<ItemRequest>>(),
            It.IsAny<FindOptions<ItemRequest, ItemRequest>>(),
            It.IsAny<CancellationToken>()
        )).ReturnsAsync(mockCursor.Object);

        var rejectedRequest = new ItemRequest
        {
            Id = requestId,
            ItemId = "item123",
            RequesterId = "requester123",
            OwnerId = ownerId,
            Status = RequestStatus.Rejected,
            RespondedAt = DateTime.UtcNow
        };

        _mockRequestsCollection.Setup(c => c.FindOneAndUpdateAsync(
            It.IsAny<FilterDefinition<ItemRequest>>(),
            It.IsAny<UpdateDefinition<ItemRequest>>(),
            It.IsAny<FindOneAndUpdateOptions<ItemRequest, ItemRequest>>(),
            It.IsAny<CancellationToken>()
        )).ReturnsAsync(rejectedRequest);

        //act
        var result = await _service.RejectRequestAsync(requestId, ownerId);

        //assert
        Assert.NotNull(result);
        Assert.Equal(RequestStatus.Rejected, result.Status);
        Assert.NotNull(result.RespondedAt);
    }

    [Fact]
    public async Task CancelRequestAsync_ValidCancellation_UpdatesRequestStatus()
    {
        //arrange
        var requestId = "request123";
        var requesterId = "requester123";
        var request = new ItemRequest
        {
            Id = requestId,
            ItemId = "item123",
            RequesterId = requesterId,
            OwnerId = "owner123",
            Status = RequestStatus.Pending
        };

        var mockCursor = new Mock<IAsyncCursor<ItemRequest>>();
        mockCursor.Setup(c => c.Current).Returns(new List<ItemRequest> { request });
        mockCursor.SetupSequence(c => c.MoveNext(It.IsAny<CancellationToken>())).Returns(true).Returns(false);
        mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true).ReturnsAsync(false);

        _mockRequestsCollection.Setup(c => c.FindAsync(
            It.IsAny<FilterDefinition<ItemRequest>>(),
            It.IsAny<FindOptions<ItemRequest, ItemRequest>>(),
            It.IsAny<CancellationToken>()
        )).ReturnsAsync(mockCursor.Object);

        var cancelledRequest = new ItemRequest
        {
            Id = requestId,
            ItemId = "item123",
            RequesterId = requesterId,
            OwnerId = "owner123",
            Status = RequestStatus.Cancelled,
            RespondedAt = DateTime.UtcNow
        };

        _mockRequestsCollection.Setup(c => c.FindOneAndUpdateAsync(
            It.IsAny<FilterDefinition<ItemRequest>>(),
            It.IsAny<UpdateDefinition<ItemRequest>>(),
            It.IsAny<FindOneAndUpdateOptions<ItemRequest, ItemRequest>>(),
            It.IsAny<CancellationToken>()
        )).ReturnsAsync(cancelledRequest);

        //act
        var result = await _service.CancelRequestAsync(requestId, requesterId);

        //assert
        Assert.NotNull(result);
        Assert.Equal(RequestStatus.Cancelled, result.Status);
        Assert.NotNull(result.RespondedAt);
    }

    [Fact]
    public async Task CancelRequestAsync_NonRequester_ThrowsUnauthorizedAccessException()
    {
        //arrange
        var requestId = "request123";
        var requesterId = "requester123";
        var nonRequesterId = "other123";
        var request = new ItemRequest
        {
            Id = requestId,
            ItemId = "item123",
            RequesterId = requesterId,
            OwnerId = "owner123",
            Status = RequestStatus.Pending
        };

        var mockCursor = new Mock<IAsyncCursor<ItemRequest>>();
        mockCursor.Setup(c => c.Current).Returns(new List<ItemRequest> { request });
        mockCursor.SetupSequence(c => c.MoveNext(It.IsAny<CancellationToken>())).Returns(true).Returns(false);
        mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true).ReturnsAsync(false);

        _mockRequestsCollection.Setup(c => c.FindAsync(
            It.IsAny<FilterDefinition<ItemRequest>>(),
            It.IsAny<FindOptions<ItemRequest, ItemRequest>>(),
            It.IsAny<CancellationToken>()
        )).ReturnsAsync(mockCursor.Object);

        //act & assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _service.CancelRequestAsync(requestId, nonRequesterId));
    }

    [Fact]
    public async Task CompleteRequestAsync_ValidCompletion_UpdatesRequestAndItemAvailability()
    {
        //arrange
        var requestId = "request123";
        var ownerId = "owner123";
        var itemId = "item123";
        var request = new ItemRequest
        {
            Id = requestId,
            ItemId = itemId,
            RequesterId = "requester123",
            OwnerId = ownerId,
            Status = RequestStatus.Approved
        };

        var mockCursor = new Mock<IAsyncCursor<ItemRequest>>();
        mockCursor.Setup(c => c.Current).Returns(new List<ItemRequest> { request });
        mockCursor.SetupSequence(c => c.MoveNext(It.IsAny<CancellationToken>())).Returns(true).Returns(false);
        mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true).ReturnsAsync(false);

        _mockRequestsCollection.Setup(c => c.FindAsync(
            It.IsAny<FilterDefinition<ItemRequest>>(),
            It.IsAny<FindOptions<ItemRequest, ItemRequest>>(),
            It.IsAny<CancellationToken>()
        )).ReturnsAsync(mockCursor.Object);

        var completedRequest = new ItemRequest
        {
            Id = requestId,
            ItemId = itemId,
            RequesterId = "requester123",
            OwnerId = ownerId,
            Status = RequestStatus.Completed,
            CompletedAt = DateTime.UtcNow
        };

        _mockRequestsCollection.Setup(c => c.FindOneAndUpdateAsync(
            It.IsAny<FilterDefinition<ItemRequest>>(),
            It.IsAny<UpdateDefinition<ItemRequest>>(),
            It.IsAny<FindOneAndUpdateOptions<ItemRequest, ItemRequest>>(),
            It.IsAny<CancellationToken>()
        )).ReturnsAsync(completedRequest);

        _mockItemsService.Setup(s => s.GetItemByIdAsync(itemId))
            .ReturnsAsync(new SharedItem { Id = itemId, IsAvailable = false });
        _mockItemsService.Setup(s => s.UpdateItemAvailabilityAsync(itemId, true))
            .ReturnsAsync(new SharedItem { Id = itemId, IsAvailable = true });

        //act
        var result = await _service.CompleteRequestAsync(requestId, ownerId);

        //assert
        Assert.NotNull(result);
        Assert.Equal(RequestStatus.Completed, result.Status);
        Assert.NotNull(result.CompletedAt);
        _mockItemsService.Verify(s => s.UpdateItemAvailabilityAsync(itemId, true), Times.Once);
    }

    [Fact]
    public async Task CompleteRequestAsync_NonApprovedRequest_ThrowsInvalidOperationException()
    {
        //arrange
        var requestId = "request123";
        var ownerId = "owner123";
        var request = new ItemRequest
        {
            Id = requestId,
            ItemId = "item123",
            RequesterId = "requester123",
            OwnerId = ownerId,
            Status = RequestStatus.Pending
        };

        var mockCursor = new Mock<IAsyncCursor<ItemRequest>>();
        mockCursor.Setup(c => c.Current).Returns(new List<ItemRequest> { request });
        mockCursor.SetupSequence(c => c.MoveNext(It.IsAny<CancellationToken>())).Returns(true).Returns(false);
        mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true).ReturnsAsync(false);

        _mockRequestsCollection.Setup(c => c.FindAsync(
            It.IsAny<FilterDefinition<ItemRequest>>(),
            It.IsAny<FindOptions<ItemRequest, ItemRequest>>(),
            It.IsAny<CancellationToken>()
        )).ReturnsAsync(mockCursor.Object);

        //act & assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CompleteRequestAsync(requestId, ownerId));
    }

    [Fact]
    public async Task CreateRequestAsync_SendsNotificationToOwner()
    {
        //arrange
        var itemId = "item123";
        var requesterId = "requester123";
        var ownerId = "owner123";
        var item = new SharedItem { Id = itemId, UserId = ownerId, Name = "Test Item" };
        var owner = new User { Id = ownerId, FirstName = "Owner", LastName = "Name", Email = "owner@test.com" };
        var requester = new User { Id = requesterId, FirstName = "Requester", LastName = "Name", Email = "requester@test.com" };

        _mockItemsService.Setup(s => s.GetItemByIdAsync(itemId)).ReturnsAsync(item);
        _mockUserService.Setup(s => s.GetUserByIdAsync(ownerId)).ReturnsAsync(owner);
        _mockUserService.Setup(s => s.GetUserByIdAsync(requesterId)).ReturnsAsync(requester);
        _mockRequestsCollection.Setup(c => c.InsertOneAsync(It.IsAny<ItemRequest>(), null, default))
            .Returns(Task.CompletedTask);
        _mockNotificationService.Setup(s => s.CreateNotificationAsync(
            It.IsAny<string>(), It.IsAny<NotificationType>(), It.IsAny<string>(), 
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new Notification());
        _mockEmailService.Setup(s => s.SendItemRequestCreatedEmailAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        //act
        var result = await _service.CreateRequestAsync(itemId, requesterId);

        //assert
        _mockNotificationService.Verify(s => s.CreateNotificationAsync(
            ownerId, NotificationType.ItemRequestCreated, It.IsAny<string>(), 
            itemId, It.IsAny<string>(), requesterId), Times.Once);
        _mockEmailService.Verify(s => s.SendItemRequestCreatedEmailAsync(
            owner.Email, "Owner Name", "Requester Name", item.Name, It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task ApproveRequestAsync_SendsNotificationToRequester()
    {
        //arrange
        var requestId = "request123";
        var ownerId = "owner123";
        var requesterId = "requester123";
        var itemId = "item123";
        var request = new ItemRequest
        {
            Id = requestId,
            ItemId = itemId,
            RequesterId = requesterId,
            OwnerId = ownerId,
            Status = RequestStatus.Pending
        };
        var item = new SharedItem { Id = itemId, UserId = ownerId, Name = "Test Item", IsAvailable = true };
        var owner = new User { Id = ownerId, FirstName = "Owner", LastName = "Name", Email = "owner@test.com" };
        var requester = new User { Id = requesterId, FirstName = "Requester", LastName = "Name", Email = "requester@test.com" };

        var mockCursor = new Mock<IAsyncCursor<ItemRequest>>();
        mockCursor.Setup(c => c.Current).Returns(new List<ItemRequest> { request });
        mockCursor.SetupSequence(c => c.MoveNext(It.IsAny<CancellationToken>())).Returns(true).Returns(false);
        mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true).ReturnsAsync(false);

        var mockEmptyCursor = new Mock<IAsyncCursor<ItemRequest>>();
        mockEmptyCursor.Setup(c => c.Current).Returns(new List<ItemRequest>());
        mockEmptyCursor.SetupSequence(c => c.MoveNext(It.IsAny<CancellationToken>())).Returns(true).Returns(false);
        mockEmptyCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true).ReturnsAsync(false);

        _mockRequestsCollection.SetupSequence(c => c.FindAsync(
            It.IsAny<FilterDefinition<ItemRequest>>(),
            It.IsAny<FindOptions<ItemRequest, ItemRequest>>(),
            It.IsAny<CancellationToken>()
        )).ReturnsAsync(mockCursor.Object).ReturnsAsync(mockEmptyCursor.Object);

        var approvedRequest = new ItemRequest
        {
            Id = requestId,
            ItemId = itemId,
            RequesterId = requesterId,
            OwnerId = ownerId,
            Status = RequestStatus.Approved,
            RespondedAt = DateTime.UtcNow
        };

        _mockRequestsCollection.Setup(c => c.FindOneAndUpdateAsync(
            It.IsAny<FilterDefinition<ItemRequest>>(),
            It.IsAny<UpdateDefinition<ItemRequest>>(),
            It.IsAny<FindOneAndUpdateOptions<ItemRequest, ItemRequest>>(),
            It.IsAny<CancellationToken>()
        )).ReturnsAsync(approvedRequest);

        _mockItemsService.Setup(s => s.GetItemByIdAsync(itemId)).ReturnsAsync(item);
        _mockItemsService.Setup(s => s.UpdateItemAvailabilityAsync(itemId, false))
            .ReturnsAsync(new SharedItem { Id = itemId, IsAvailable = false });
        _mockUserService.Setup(s => s.GetUserByIdAsync(ownerId)).ReturnsAsync(owner);
        _mockUserService.Setup(s => s.GetUserByIdAsync(requesterId)).ReturnsAsync(requester);
        _mockNotificationService.Setup(s => s.CreateNotificationAsync(
            It.IsAny<string>(), It.IsAny<NotificationType>(), It.IsAny<string>(), 
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new Notification());
        _mockEmailService.Setup(s => s.SendItemRequestApprovedEmailAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        //act
        var result = await _service.ApproveRequestAsync(requestId, ownerId);

        //assert
        _mockNotificationService.Verify(s => s.CreateNotificationAsync(
            requesterId, NotificationType.ItemRequestApproved, It.IsAny<string>(), 
            itemId, requestId, ownerId), Times.Once);
        _mockEmailService.Verify(s => s.SendItemRequestApprovedEmailAsync(
            requester.Email, "Requester Name", "Owner Name", item.Name), Times.Once);
    }

    [Fact]
    public async Task RejectRequestAsync_SendsNotificationToRequester()
    {
        //arrange
        var requestId = "request123";
        var ownerId = "owner123";
        var requesterId = "requester123";
        var itemId = "item123";
        var request = new ItemRequest
        {
            Id = requestId,
            ItemId = itemId,
            RequesterId = requesterId,
            OwnerId = ownerId,
            Status = RequestStatus.Pending
        };
        var item = new SharedItem { Id = itemId, UserId = ownerId, Name = "Test Item" };
        var owner = new User { Id = ownerId, FirstName = "Owner", LastName = "Name", Email = "owner@test.com" };
        var requester = new User { Id = requesterId, FirstName = "Requester", LastName = "Name", Email = "requester@test.com" };

        var mockCursor = new Mock<IAsyncCursor<ItemRequest>>();
        mockCursor.Setup(c => c.Current).Returns(new List<ItemRequest> { request });
        mockCursor.SetupSequence(c => c.MoveNext(It.IsAny<CancellationToken>())).Returns(true).Returns(false);
        mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true).ReturnsAsync(false);

        _mockRequestsCollection.Setup(c => c.FindAsync(
            It.IsAny<FilterDefinition<ItemRequest>>(),
            It.IsAny<FindOptions<ItemRequest, ItemRequest>>(),
            It.IsAny<CancellationToken>()
        )).ReturnsAsync(mockCursor.Object);

        var rejectedRequest = new ItemRequest
        {
            Id = requestId,
            ItemId = itemId,
            RequesterId = requesterId,
            OwnerId = ownerId,
            Status = RequestStatus.Rejected,
            RespondedAt = DateTime.UtcNow
        };

        _mockRequestsCollection.Setup(c => c.FindOneAndUpdateAsync(
            It.IsAny<FilterDefinition<ItemRequest>>(),
            It.IsAny<UpdateDefinition<ItemRequest>>(),
            It.IsAny<FindOneAndUpdateOptions<ItemRequest, ItemRequest>>(),
            It.IsAny<CancellationToken>()
        )).ReturnsAsync(rejectedRequest);

        _mockItemsService.Setup(s => s.GetItemByIdAsync(itemId)).ReturnsAsync(item);
        _mockUserService.Setup(s => s.GetUserByIdAsync(ownerId)).ReturnsAsync(owner);
        _mockUserService.Setup(s => s.GetUserByIdAsync(requesterId)).ReturnsAsync(requester);
        _mockNotificationService.Setup(s => s.CreateNotificationAsync(
            It.IsAny<string>(), It.IsAny<NotificationType>(), It.IsAny<string>(), 
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new Notification());
        _mockEmailService.Setup(s => s.SendItemRequestRejectedEmailAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        //act
        var result = await _service.RejectRequestAsync(requestId, ownerId);

        //assert
        _mockNotificationService.Verify(s => s.CreateNotificationAsync(
            requesterId, NotificationType.ItemRequestRejected, It.IsAny<string>(), 
            itemId, requestId, ownerId), Times.Once);
        _mockEmailService.Verify(s => s.SendItemRequestRejectedEmailAsync(
            requester.Email, "Requester Name", "Owner Name", item.Name), Times.Once);
    }

    [Fact]
    public async Task CompleteRequestAsync_SendsNotificationToRequester()
    {
        //arrange
        var requestId = "request123";
        var ownerId = "owner123";
        var requesterId = "requester123";
        var itemId = "item123";
        var request = new ItemRequest
        {
            Id = requestId,
            ItemId = itemId,
            RequesterId = requesterId,
            OwnerId = ownerId,
            Status = RequestStatus.Approved
        };
        var item = new SharedItem { Id = itemId, UserId = ownerId, Name = "Test Item", IsAvailable = false };
        var owner = new User { Id = ownerId, FirstName = "Owner", LastName = "Name", Email = "owner@test.com" };
        var requester = new User { Id = requesterId, FirstName = "Requester", LastName = "Name", Email = "requester@test.com" };

        var mockCursor = new Mock<IAsyncCursor<ItemRequest>>();
        mockCursor.Setup(c => c.Current).Returns(new List<ItemRequest> { request });
        mockCursor.SetupSequence(c => c.MoveNext(It.IsAny<CancellationToken>())).Returns(true).Returns(false);
        mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true).ReturnsAsync(false);

        _mockRequestsCollection.Setup(c => c.FindAsync(
            It.IsAny<FilterDefinition<ItemRequest>>(),
            It.IsAny<FindOptions<ItemRequest, ItemRequest>>(),
            It.IsAny<CancellationToken>()
        )).ReturnsAsync(mockCursor.Object);

        var completedRequest = new ItemRequest
        {
            Id = requestId,
            ItemId = itemId,
            RequesterId = requesterId,
            OwnerId = ownerId,
            Status = RequestStatus.Completed,
            CompletedAt = DateTime.UtcNow
        };

        _mockRequestsCollection.Setup(c => c.FindOneAndUpdateAsync(
            It.IsAny<FilterDefinition<ItemRequest>>(),
            It.IsAny<UpdateDefinition<ItemRequest>>(),
            It.IsAny<FindOneAndUpdateOptions<ItemRequest, ItemRequest>>(),
            It.IsAny<CancellationToken>()
        )).ReturnsAsync(completedRequest);

        _mockItemsService.Setup(s => s.GetItemByIdAsync(itemId)).ReturnsAsync(item);
        _mockItemsService.Setup(s => s.UpdateItemAvailabilityAsync(itemId, true))
            .ReturnsAsync(new SharedItem { Id = itemId, IsAvailable = true });
        _mockUserService.Setup(s => s.GetUserByIdAsync(ownerId)).ReturnsAsync(owner);
        _mockUserService.Setup(s => s.GetUserByIdAsync(requesterId)).ReturnsAsync(requester);
        _mockNotificationService.Setup(s => s.CreateNotificationAsync(
            It.IsAny<string>(), It.IsAny<NotificationType>(), It.IsAny<string>(), 
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new Notification());
        _mockEmailService.Setup(s => s.SendItemRequestCompletedEmailAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        //act
        var result = await _service.CompleteRequestAsync(requestId, ownerId);

        //assert
        _mockNotificationService.Verify(s => s.CreateNotificationAsync(
            requesterId, NotificationType.ItemRequestCompleted, It.IsAny<string>(), 
            itemId, requestId, ownerId), Times.Once);
        _mockEmailService.Verify(s => s.SendItemRequestCompletedEmailAsync(
            requester.Email, "Requester Name", "Owner Name", item.Name), Times.Once);
    }

    [Fact]
    public async Task CancelRequestAsync_SendsNotificationToOwner()
    {
        //arrange
        var requestId = "request123";
        var requesterId = "requester123";
        var ownerId = "owner123";
        var itemId = "item123";
        var request = new ItemRequest
        {
            Id = requestId,
            ItemId = itemId,
            RequesterId = requesterId,
            OwnerId = ownerId,
            Status = RequestStatus.Pending
        };
        var item = new SharedItem { Id = itemId, UserId = ownerId, Name = "Test Item" };
        var owner = new User { Id = ownerId, FirstName = "Owner", LastName = "Name", Email = "owner@test.com" };
        var requester = new User { Id = requesterId, FirstName = "Requester", LastName = "Name", Email = "requester@test.com" };

        var mockCursor = new Mock<IAsyncCursor<ItemRequest>>();
        mockCursor.Setup(c => c.Current).Returns(new List<ItemRequest> { request });
        mockCursor.SetupSequence(c => c.MoveNext(It.IsAny<CancellationToken>())).Returns(true).Returns(false);
        mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true).ReturnsAsync(false);

        _mockRequestsCollection.Setup(c => c.FindAsync(
            It.IsAny<FilterDefinition<ItemRequest>>(),
            It.IsAny<FindOptions<ItemRequest, ItemRequest>>(),
            It.IsAny<CancellationToken>()
        )).ReturnsAsync(mockCursor.Object);

        var cancelledRequest = new ItemRequest
        {
            Id = requestId,
            ItemId = itemId,
            RequesterId = requesterId,
            OwnerId = ownerId,
            Status = RequestStatus.Cancelled,
            RespondedAt = DateTime.UtcNow
        };

        _mockRequestsCollection.Setup(c => c.FindOneAndUpdateAsync(
            It.IsAny<FilterDefinition<ItemRequest>>(),
            It.IsAny<UpdateDefinition<ItemRequest>>(),
            It.IsAny<FindOneAndUpdateOptions<ItemRequest, ItemRequest>>(),
            It.IsAny<CancellationToken>()
        )).ReturnsAsync(cancelledRequest);

        _mockItemsService.Setup(s => s.GetItemByIdAsync(itemId)).ReturnsAsync(item);
        _mockUserService.Setup(s => s.GetUserByIdAsync(ownerId)).ReturnsAsync(owner);
        _mockUserService.Setup(s => s.GetUserByIdAsync(requesterId)).ReturnsAsync(requester);
        _mockNotificationService.Setup(s => s.CreateNotificationAsync(
            It.IsAny<string>(), It.IsAny<NotificationType>(), It.IsAny<string>(), 
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new Notification());
        _mockEmailService.Setup(s => s.SendItemRequestCancelledEmailAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        //act
        var result = await _service.CancelRequestAsync(requestId, requesterId);

        //assert
        _mockNotificationService.Verify(s => s.CreateNotificationAsync(
            ownerId, NotificationType.ItemRequestCancelled, It.IsAny<string>(), 
            itemId, requestId, requesterId), Times.Once);
        _mockEmailService.Verify(s => s.SendItemRequestCancelledEmailAsync(
            owner.Email, "Owner Name", "Requester Name", item.Name), Times.Once);
    }

    [Fact]
    public async Task CreateRequestAsync_ContinuesWhenNotificationFails()
    {
        //arrange
        var itemId = "item123";
        var requesterId = "requester123";
        var ownerId = "owner123";
        var item = new SharedItem { Id = itemId, UserId = ownerId, Name = "Test Item" };

        _mockItemsService.Setup(s => s.GetItemByIdAsync(itemId)).ReturnsAsync(item);
        _mockUserService.Setup(s => s.GetUserByIdAsync(It.IsAny<string>())).ReturnsAsync((User?)null);
        _mockRequestsCollection.Setup(c => c.InsertOneAsync(It.IsAny<ItemRequest>(), null, default))
            .Returns(Task.CompletedTask);

        //act
        var result = await _service.CreateRequestAsync(itemId, requesterId);

        //assert
        Assert.NotNull(result);
        Assert.Equal(itemId, result.ItemId);
        Assert.Equal(requesterId, result.RequesterId);
        _mockNotificationService.Verify(s => s.CreateNotificationAsync(
            It.IsAny<string>(), It.IsAny<NotificationType>(), It.IsAny<string>(), 
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ApproveRequestAsync_ContinuesWhenEmailFails()
    {
        //arrange
        var requestId = "request123";
        var ownerId = "owner123";
        var requesterId = "requester123";
        var itemId = "item123";
        var request = new ItemRequest
        {
            Id = requestId,
            ItemId = itemId,
            RequesterId = requesterId,
            OwnerId = ownerId,
            Status = RequestStatus.Pending
        };
        var item = new SharedItem { Id = itemId, UserId = ownerId, Name = "Test Item", IsAvailable = true };
        var owner = new User { Id = ownerId, FirstName = "Owner", LastName = "Name", Email = "owner@test.com" };
        var requester = new User { Id = requesterId, FirstName = "Requester", LastName = "Name", Email = "requester@test.com" };

        var mockCursor = new Mock<IAsyncCursor<ItemRequest>>();
        mockCursor.Setup(c => c.Current).Returns(new List<ItemRequest> { request });
        mockCursor.SetupSequence(c => c.MoveNext(It.IsAny<CancellationToken>())).Returns(true).Returns(false);
        mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true).ReturnsAsync(false);

        var mockEmptyCursor = new Mock<IAsyncCursor<ItemRequest>>();
        mockEmptyCursor.Setup(c => c.Current).Returns(new List<ItemRequest>());
        mockEmptyCursor.SetupSequence(c => c.MoveNext(It.IsAny<CancellationToken>())).Returns(true).Returns(false);
        mockEmptyCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true).ReturnsAsync(false);

        _mockRequestsCollection.SetupSequence(c => c.FindAsync(
            It.IsAny<FilterDefinition<ItemRequest>>(),
            It.IsAny<FindOptions<ItemRequest, ItemRequest>>(),
            It.IsAny<CancellationToken>()
        )).ReturnsAsync(mockCursor.Object).ReturnsAsync(mockEmptyCursor.Object);

        var approvedRequest = new ItemRequest
        {
            Id = requestId,
            ItemId = itemId,
            RequesterId = requesterId,
            OwnerId = ownerId,
            Status = RequestStatus.Approved,
            RespondedAt = DateTime.UtcNow
        };

        _mockRequestsCollection.Setup(c => c.FindOneAndUpdateAsync(
            It.IsAny<FilterDefinition<ItemRequest>>(),
            It.IsAny<UpdateDefinition<ItemRequest>>(),
            It.IsAny<FindOneAndUpdateOptions<ItemRequest, ItemRequest>>(),
            It.IsAny<CancellationToken>()
        )).ReturnsAsync(approvedRequest);

        _mockItemsService.Setup(s => s.GetItemByIdAsync(itemId)).ReturnsAsync(item);
        _mockItemsService.Setup(s => s.UpdateItemAvailabilityAsync(itemId, false))
            .ReturnsAsync(new SharedItem { Id = itemId, IsAvailable = false });
        _mockUserService.Setup(s => s.GetUserByIdAsync(ownerId)).ReturnsAsync(owner);
        _mockUserService.Setup(s => s.GetUserByIdAsync(requesterId)).ReturnsAsync(requester);
        _mockNotificationService.Setup(s => s.CreateNotificationAsync(
            It.IsAny<string>(), It.IsAny<NotificationType>(), It.IsAny<string>(), 
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new Notification());
        _mockEmailService.Setup(s => s.SendItemRequestApprovedEmailAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(false);

        //act
        var result = await _service.ApproveRequestAsync(requestId, ownerId);

        //assert
        Assert.NotNull(result);
        Assert.Equal(RequestStatus.Approved, result.Status);
    }

    [Fact]
    public async Task CreateRequestAsync_ContinuesWhenEmailServiceThrowsException()
    {
        //arrange
        var itemId = "item123";
        var requesterId = "requester123";
        var ownerId = "owner123";
        var item = new SharedItem { Id = itemId, UserId = ownerId, Name = "Test Item" };
        var owner = new User { Id = ownerId, FirstName = "Owner", LastName = "Name", Email = "owner@test.com" };
        var requester = new User { Id = requesterId, FirstName = "Requester", LastName = "Name", Email = "requester@test.com" };

        _mockItemsService.Setup(s => s.GetItemByIdAsync(itemId)).ReturnsAsync(item);
        _mockUserService.Setup(s => s.GetUserByIdAsync(ownerId)).ReturnsAsync(owner);
        _mockUserService.Setup(s => s.GetUserByIdAsync(requesterId)).ReturnsAsync(requester);
        _mockRequestsCollection.Setup(c => c.InsertOneAsync(It.IsAny<ItemRequest>(), null, default))
            .Returns(Task.CompletedTask);
        _mockNotificationService.Setup(s => s.CreateNotificationAsync(
            It.IsAny<string>(), It.IsAny<NotificationType>(), It.IsAny<string>(), 
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new Notification());
        _mockEmailService.Setup(s => s.SendItemRequestCreatedEmailAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception("SMTP server unavailable"));

        //act
        var result = await _service.CreateRequestAsync(itemId, requesterId);

        //assert
        Assert.NotNull(result);
        Assert.Equal(itemId, result.ItemId);
        Assert.Equal(requesterId, result.RequesterId);
        Assert.Equal(RequestStatus.Pending, result.Status);
    }

    [Fact]
    public async Task RejectRequestAsync_ContinuesWhenNotificationServiceThrowsException()
    {
        //arrange
        var requestId = "request123";
        var ownerId = "owner123";
        var requesterId = "requester123";
        var itemId = "item123";
        var request = new ItemRequest
        {
            Id = requestId,
            ItemId = itemId,
            RequesterId = requesterId,
            OwnerId = ownerId,
            Status = RequestStatus.Pending
        };
        var item = new SharedItem { Id = itemId, UserId = ownerId, Name = "Test Item" };
        var owner = new User { Id = ownerId, FirstName = "Owner", LastName = "Name", Email = "owner@test.com" };
        var requester = new User { Id = requesterId, FirstName = "Requester", LastName = "Name", Email = "requester@test.com" };

        var mockCursor = new Mock<IAsyncCursor<ItemRequest>>();
        mockCursor.Setup(c => c.Current).Returns(new List<ItemRequest> { request });
        mockCursor.SetupSequence(c => c.MoveNext(It.IsAny<CancellationToken>())).Returns(true).Returns(false);
        mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true).ReturnsAsync(false);

        _mockRequestsCollection.Setup(c => c.FindAsync(
            It.IsAny<FilterDefinition<ItemRequest>>(),
            It.IsAny<FindOptions<ItemRequest, ItemRequest>>(),
            It.IsAny<CancellationToken>()
        )).ReturnsAsync(mockCursor.Object);

        var rejectedRequest = new ItemRequest
        {
            Id = requestId,
            ItemId = itemId,
            RequesterId = requesterId,
            OwnerId = ownerId,
            Status = RequestStatus.Rejected,
            RespondedAt = DateTime.UtcNow
        };

        _mockRequestsCollection.Setup(c => c.FindOneAndUpdateAsync(
            It.IsAny<FilterDefinition<ItemRequest>>(),
            It.IsAny<UpdateDefinition<ItemRequest>>(),
            It.IsAny<FindOneAndUpdateOptions<ItemRequest, ItemRequest>>(),
            It.IsAny<CancellationToken>()
        )).ReturnsAsync(rejectedRequest);

        _mockItemsService.Setup(s => s.GetItemByIdAsync(itemId)).ReturnsAsync(item);
        _mockUserService.Setup(s => s.GetUserByIdAsync(ownerId)).ReturnsAsync(owner);
        _mockUserService.Setup(s => s.GetUserByIdAsync(requesterId)).ReturnsAsync(requester);
        _mockNotificationService.Setup(s => s.CreateNotificationAsync(
            It.IsAny<string>(), It.IsAny<NotificationType>(), It.IsAny<string>(), 
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception("Database connection failed"));
        _mockEmailService.Setup(s => s.SendItemRequestRejectedEmailAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        //act
        var result = await _service.RejectRequestAsync(requestId, ownerId);

        //assert
        Assert.NotNull(result);
        Assert.Equal(RequestStatus.Rejected, result.Status);
        Assert.NotNull(result.RespondedAt);
    }

    [Fact]
    public async Task CompleteRequestAsync_ContinuesWhenBothNotificationAndEmailFail()
    {
        //arrange
        var requestId = "request123";
        var ownerId = "owner123";
        var requesterId = "requester123";
        var itemId = "item123";
        var request = new ItemRequest
        {
            Id = requestId,
            ItemId = itemId,
            RequesterId = requesterId,
            OwnerId = ownerId,
            Status = RequestStatus.Approved
        };
        var item = new SharedItem { Id = itemId, UserId = ownerId, Name = "Test Item", IsAvailable = false };
        var owner = new User { Id = ownerId, FirstName = "Owner", LastName = "Name", Email = "owner@test.com" };
        var requester = new User { Id = requesterId, FirstName = "Requester", LastName = "Name", Email = "requester@test.com" };

        var mockCursor = new Mock<IAsyncCursor<ItemRequest>>();
        mockCursor.Setup(c => c.Current).Returns(new List<ItemRequest> { request });
        mockCursor.SetupSequence(c => c.MoveNext(It.IsAny<CancellationToken>())).Returns(true).Returns(false);
        mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true).ReturnsAsync(false);

        _mockRequestsCollection.Setup(c => c.FindAsync(
            It.IsAny<FilterDefinition<ItemRequest>>(),
            It.IsAny<FindOptions<ItemRequest, ItemRequest>>(),
            It.IsAny<CancellationToken>()
        )).ReturnsAsync(mockCursor.Object);

        var completedRequest = new ItemRequest
        {
            Id = requestId,
            ItemId = itemId,
            RequesterId = requesterId,
            OwnerId = ownerId,
            Status = RequestStatus.Completed,
            CompletedAt = DateTime.UtcNow
        };

        _mockRequestsCollection.Setup(c => c.FindOneAndUpdateAsync(
            It.IsAny<FilterDefinition<ItemRequest>>(),
            It.IsAny<UpdateDefinition<ItemRequest>>(),
            It.IsAny<FindOneAndUpdateOptions<ItemRequest, ItemRequest>>(),
            It.IsAny<CancellationToken>()
        )).ReturnsAsync(completedRequest);

        _mockItemsService.Setup(s => s.GetItemByIdAsync(itemId)).ReturnsAsync(item);
        _mockItemsService.Setup(s => s.UpdateItemAvailabilityAsync(itemId, true))
            .ReturnsAsync(new SharedItem { Id = itemId, IsAvailable = true });
        _mockUserService.Setup(s => s.GetUserByIdAsync(ownerId)).ReturnsAsync(owner);
        _mockUserService.Setup(s => s.GetUserByIdAsync(requesterId)).ReturnsAsync(requester);
        _mockNotificationService.Setup(s => s.CreateNotificationAsync(
            It.IsAny<string>(), It.IsAny<NotificationType>(), It.IsAny<string>(), 
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception("Notification service unavailable"));
        _mockEmailService.Setup(s => s.SendItemRequestCompletedEmailAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception("Email service unavailable"));

        //act
        var result = await _service.CompleteRequestAsync(requestId, ownerId);

        //assert
        Assert.NotNull(result);
        Assert.Equal(RequestStatus.Completed, result.Status);
        Assert.NotNull(result.CompletedAt);
        _mockItemsService.Verify(s => s.UpdateItemAvailabilityAsync(itemId, true), Times.Once);
    }

    [Fact]
    public async Task CancelRequestAsync_ContinuesWhenItemServiceReturnsNull()
    {
        //arrange
        var requestId = "request123";
        var requesterId = "requester123";
        var ownerId = "owner123";
        var itemId = "item123";
        var request = new ItemRequest
        {
            Id = requestId,
            ItemId = itemId,
            RequesterId = requesterId,
            OwnerId = ownerId,
            Status = RequestStatus.Pending
        };

        var mockCursor = new Mock<IAsyncCursor<ItemRequest>>();
        mockCursor.Setup(c => c.Current).Returns(new List<ItemRequest> { request });
        mockCursor.SetupSequence(c => c.MoveNext(It.IsAny<CancellationToken>())).Returns(true).Returns(false);
        mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true).ReturnsAsync(false);

        _mockRequestsCollection.Setup(c => c.FindAsync(
            It.IsAny<FilterDefinition<ItemRequest>>(),
            It.IsAny<FindOptions<ItemRequest, ItemRequest>>(),
            It.IsAny<CancellationToken>()
        )).ReturnsAsync(mockCursor.Object);

        var cancelledRequest = new ItemRequest
        {
            Id = requestId,
            ItemId = itemId,
            RequesterId = requesterId,
            OwnerId = ownerId,
            Status = RequestStatus.Cancelled,
            RespondedAt = DateTime.UtcNow
        };

        _mockRequestsCollection.Setup(c => c.FindOneAndUpdateAsync(
            It.IsAny<FilterDefinition<ItemRequest>>(),
            It.IsAny<UpdateDefinition<ItemRequest>>(),
            It.IsAny<FindOneAndUpdateOptions<ItemRequest, ItemRequest>>(),
            It.IsAny<CancellationToken>()
        )).ReturnsAsync(cancelledRequest);

        _mockItemsService.Setup(s => s.GetItemByIdAsync(itemId)).ReturnsAsync((SharedItem?)null);
        _mockUserService.Setup(s => s.GetUserByIdAsync(It.IsAny<string>())).ReturnsAsync((User?)null);

        //act
        var result = await _service.CancelRequestAsync(requestId, requesterId);

        //assert
        Assert.NotNull(result);
        Assert.Equal(RequestStatus.Cancelled, result.Status);
        Assert.NotNull(result.RespondedAt);
        _mockNotificationService.Verify(s => s.CreateNotificationAsync(
            It.IsAny<string>(), It.IsAny<NotificationType>(), It.IsAny<string>(), 
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task CreateRequestAsync_WithValidMessage_StoresMessage()
    {
        //arrange
        var itemId = "item123";
        var requesterId = "requester123";
        var ownerId = "owner123";
        var message = "I need this for the weekend project";
        var item = new SharedItem { Id = itemId, UserId = ownerId, Name = "Test Item" };

        _mockItemsService.Setup(s => s.GetItemByIdAsync(itemId)).ReturnsAsync(item);
        _mockRequestsCollection.Setup(c => c.InsertOneAsync(It.IsAny<ItemRequest>(), null, default))
            .Returns(Task.CompletedTask);

        //act
        var result = await _service.CreateRequestAsync(itemId, requesterId, message);

        //assert
        Assert.NotNull(result);
        Assert.Equal(message, result.Message);
        _mockRequestsCollection.Verify(c => c.InsertOneAsync(
            It.Is<ItemRequest>(r => r.Message == message), null, default), Times.Once);
    }

    [Fact]
    public async Task CreateRequestAsync_WithNullMessage_AcceptsRequest()
    {
        //arrange
        var itemId = "item123";
        var requesterId = "requester123";
        var ownerId = "owner123";
        var item = new SharedItem { Id = itemId, UserId = ownerId, Name = "Test Item" };

        _mockItemsService.Setup(s => s.GetItemByIdAsync(itemId)).ReturnsAsync(item);
        _mockRequestsCollection.Setup(c => c.InsertOneAsync(It.IsAny<ItemRequest>(), null, default))
            .Returns(Task.CompletedTask);

        //act
        var result = await _service.CreateRequestAsync(itemId, requesterId, null);

        //assert
        Assert.NotNull(result);
        Assert.Null(result.Message);
        _mockRequestsCollection.Verify(c => c.InsertOneAsync(It.IsAny<ItemRequest>(), null, default), Times.Once);
    }

    [Fact]
    public async Task CreateRequestAsync_WithEmptyMessage_AcceptsRequest()
    {
        //arrange
        var itemId = "item123";
        var requesterId = "requester123";
        var ownerId = "owner123";
        var item = new SharedItem { Id = itemId, UserId = ownerId, Name = "Test Item" };

        _mockItemsService.Setup(s => s.GetItemByIdAsync(itemId)).ReturnsAsync(item);
        _mockRequestsCollection.Setup(c => c.InsertOneAsync(It.IsAny<ItemRequest>(), null, default))
            .Returns(Task.CompletedTask);

        //act
        var result = await _service.CreateRequestAsync(itemId, requesterId, "");

        //assert
        Assert.NotNull(result);
        Assert.Null(result.Message);
        _mockRequestsCollection.Verify(c => c.InsertOneAsync(It.IsAny<ItemRequest>(), null, default), Times.Once);
    }

    [Fact]
    public async Task CreateRequestAsync_WithWhitespaceMessage_AcceptsRequest()
    {
        //arrange
        var itemId = "item123";
        var requesterId = "requester123";
        var ownerId = "owner123";
        var item = new SharedItem { Id = itemId, UserId = ownerId, Name = "Test Item" };

        _mockItemsService.Setup(s => s.GetItemByIdAsync(itemId)).ReturnsAsync(item);
        _mockRequestsCollection.Setup(c => c.InsertOneAsync(It.IsAny<ItemRequest>(), null, default))
            .Returns(Task.CompletedTask);

        //act
        var result = await _service.CreateRequestAsync(itemId, requesterId, "   ");

        //assert
        Assert.NotNull(result);
        Assert.Null(result.Message);
        _mockRequestsCollection.Verify(c => c.InsertOneAsync(It.IsAny<ItemRequest>(), null, default), Times.Once);
    }

    [Fact]
    public async Task CreateRequestAsync_WithMessageOver500Characters_ThrowsArgumentException()
    {
        //arrange
        var itemId = "item123";
        var requesterId = "requester123";
        var ownerId = "owner123";
        var message = new string('a', 501); // 501 characters
        var item = new SharedItem { Id = itemId, UserId = ownerId, Name = "Test Item" };

        _mockItemsService.Setup(s => s.GetItemByIdAsync(itemId)).ReturnsAsync(item);

        //act & assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => 
            _service.CreateRequestAsync(itemId, requesterId, message));
        Assert.Equal("message", exception.ParamName);
        Assert.Contains("500 characters", exception.Message);
    }

    [Fact]
    public async Task CreateRequestAsync_WithMessageExactly500Characters_AcceptsRequest()
    {
        //arrange
        var itemId = "item123";
        var requesterId = "requester123";
        var ownerId = "owner123";
        var message = new string('a', 500); // Exactly 500 characters
        var item = new SharedItem { Id = itemId, UserId = ownerId, Name = "Test Item" };

        _mockItemsService.Setup(s => s.GetItemByIdAsync(itemId)).ReturnsAsync(item);
        _mockRequestsCollection.Setup(c => c.InsertOneAsync(It.IsAny<ItemRequest>(), null, default))
            .Returns(Task.CompletedTask);

        //act
        var result = await _service.CreateRequestAsync(itemId, requesterId, message);

        //assert
        Assert.NotNull(result);
        Assert.Equal(message, result.Message);
        _mockRequestsCollection.Verify(c => c.InsertOneAsync(It.IsAny<ItemRequest>(), null, default), Times.Once);
    }

    [Fact]
    public async Task CreateRequestAsync_WithHtmlInMessage_SanitizesMessage()
    {
        //arrange
        var itemId = "item123";
        var requesterId = "requester123";
        var ownerId = "owner123";
        var message = "<script>alert('xss')</script>I need this item";
        var expectedSanitized = "&lt;script&gt;alert(&#39;xss&#39;)&lt;/script&gt;I need this item";
        var item = new SharedItem { Id = itemId, UserId = ownerId, Name = "Test Item" };

        _mockItemsService.Setup(s => s.GetItemByIdAsync(itemId)).ReturnsAsync(item);
        _mockRequestsCollection.Setup(c => c.InsertOneAsync(It.IsAny<ItemRequest>(), null, default))
            .Returns(Task.CompletedTask);

        //act
        var result = await _service.CreateRequestAsync(itemId, requesterId, message);

        //assert
        Assert.NotNull(result);
        Assert.Equal(expectedSanitized, result.Message);
        Assert.DoesNotContain("<script>", result.Message);
        _mockRequestsCollection.Verify(c => c.InsertOneAsync(
            It.Is<ItemRequest>(r => r.Message == expectedSanitized), null, default), Times.Once);
    }

    [Fact]
    public async Task CreateRequestAsync_WithSpecialCharactersInMessage_SanitizesMessage()
    {
        //arrange
        var itemId = "item123";
        var requesterId = "requester123";
        var ownerId = "owner123";
        var message = "I need this & that <item> for \"testing\"";
        var expectedSanitized = "I need this &amp; that &lt;item&gt; for &quot;testing&quot;";
        var item = new SharedItem { Id = itemId, UserId = ownerId, Name = "Test Item" };

        _mockItemsService.Setup(s => s.GetItemByIdAsync(itemId)).ReturnsAsync(item);
        _mockRequestsCollection.Setup(c => c.InsertOneAsync(It.IsAny<ItemRequest>(), null, default))
            .Returns(Task.CompletedTask);

        //act
        var result = await _service.CreateRequestAsync(itemId, requesterId, message);

        //assert
        Assert.NotNull(result);
        Assert.Equal(expectedSanitized, result.Message);
        _mockRequestsCollection.Verify(c => c.InsertOneAsync(
            It.Is<ItemRequest>(r => r.Message == expectedSanitized), null, default), Times.Once);
    }
}
