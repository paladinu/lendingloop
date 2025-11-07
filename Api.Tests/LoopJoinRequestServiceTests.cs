using Api.Models;
using Api.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Moq;
using Xunit;

namespace Api.Tests;

public class LoopJoinRequestServiceTests
{
    private readonly Mock<IMongoDatabase> _mockDatabase;
    private readonly Mock<IMongoCollection<LoopJoinRequest>> _mockCollection;
    private readonly Mock<ILoopService> _mockLoopService;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<ILogger<LoopJoinRequestService>> _mockLogger;
    private readonly LoopJoinRequestService _service;

    public LoopJoinRequestServiceTests()
    {
        _mockDatabase = new Mock<IMongoDatabase>();
        _mockCollection = new Mock<IMongoCollection<LoopJoinRequest>>();
        _mockLoopService = new Mock<ILoopService>();
        _mockConfiguration = new Mock<IConfiguration>();
        _mockLogger = new Mock<ILogger<LoopJoinRequestService>>();

        _mockConfiguration.Setup(c => c["MongoDB:LoopJoinRequestsCollectionName"])
            .Returns("loopJoinRequests");

        _mockDatabase.Setup(db => db.GetCollection<LoopJoinRequest>("loopJoinRequests", null))
            .Returns(_mockCollection.Object);

        _service = new LoopJoinRequestService(
            _mockDatabase.Object,
            _mockConfiguration.Object,
            _mockLoopService.Object,
            _mockLogger.Object
        );
    }

    [Fact]
    public async Task CreateJoinRequestAsync_CreatesRequest_WhenUserIsNotMember()
    {
        //arrange
        var loopId = "loop1";
        var userId = "user1";
        var message = "Please let me join";

        _mockLoopService.Setup(s => s.IsUserLoopMemberAsync(loopId, userId))
            .ReturnsAsync(false);

        _mockCollection.Setup(c => c.CountDocumentsAsync(
            It.IsAny<FilterDefinition<LoopJoinRequest>>(),
            null,
            default))
            .ReturnsAsync(0);

        _mockCollection.Setup(c => c.InsertOneAsync(
            It.IsAny<LoopJoinRequest>(),
            null,
            default))
            .Returns(Task.CompletedTask);

        //act
        var result = await _service.CreateJoinRequestAsync(loopId, userId, message);

        //assert
        Assert.NotNull(result);
        Assert.Equal(loopId, result.LoopId);
        Assert.Equal(userId, result.UserId);
        Assert.Equal(message, result.Message);
        Assert.Equal(JoinRequestStatus.Pending, result.Status);
        _mockCollection.Verify(c => c.InsertOneAsync(
            It.IsAny<LoopJoinRequest>(),
            null,
            default), Times.Once);
    }

    [Fact]
    public async Task CreateJoinRequestAsync_ThrowsException_WhenUserIsAlreadyMember()
    {
        //arrange
        var loopId = "loop1";
        var userId = "user1";
        var message = "Please let me join";

        _mockLoopService.Setup(s => s.IsUserLoopMemberAsync(loopId, userId))
            .ReturnsAsync(true);

        //act & assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateJoinRequestAsync(loopId, userId, message));
    }

    [Fact]
    public async Task CreateJoinRequestAsync_ThrowsException_WhenPendingRequestExists()
    {
        //arrange
        var loopId = "loop1";
        var userId = "user1";
        var message = "Please let me join";

        _mockLoopService.Setup(s => s.IsUserLoopMemberAsync(loopId, userId))
            .ReturnsAsync(false);

        _mockCollection.Setup(c => c.CountDocumentsAsync(
            It.IsAny<FilterDefinition<LoopJoinRequest>>(),
            null,
            default))
            .ReturnsAsync(1);

        //act & assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateJoinRequestAsync(loopId, userId, message));
    }

    [Fact]
    public async Task GetJoinRequestByIdAsync_ReturnsRequest_WhenRequestExists()
    {
        //arrange
        var requestId = "req1";
        var expectedRequest = new LoopJoinRequest
        {
            Id = requestId,
            LoopId = "loop1",
            UserId = "user1",
            Status = JoinRequestStatus.Pending
        };

        var mockCursor = new Mock<IAsyncCursor<LoopJoinRequest>>();
        mockCursor.Setup(c => c.Current).Returns(new[] { expectedRequest });
        mockCursor.SetupSequence(c => c.MoveNext(It.IsAny<CancellationToken>()))
            .Returns(true)
            .Returns(false);
        mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .ReturnsAsync(false);

        _mockCollection.Setup(c => c.FindAsync(
            It.IsAny<FilterDefinition<LoopJoinRequest>>(),
            It.IsAny<FindOptions<LoopJoinRequest, LoopJoinRequest>>(),
            default))
            .ReturnsAsync(mockCursor.Object);

        //act
        var result = await _service.GetJoinRequestByIdAsync(requestId);

        //assert
        Assert.NotNull(result);
        Assert.Equal(requestId, result.Id);
    }

    [Fact]
    public async Task GetPendingJoinRequestsForLoopAsync_ReturnsRequests_WhenPendingRequestsExist()
    {
        //arrange
        var loopId = "loop1";
        var requests = new List<LoopJoinRequest>
        {
            new LoopJoinRequest { Id = "req1", LoopId = loopId, Status = JoinRequestStatus.Pending },
            new LoopJoinRequest { Id = "req2", LoopId = loopId, Status = JoinRequestStatus.Pending }
        };

        var mockCursor = new Mock<IAsyncCursor<LoopJoinRequest>>();
        mockCursor.Setup(c => c.Current).Returns(requests);
        mockCursor.SetupSequence(c => c.MoveNext(It.IsAny<CancellationToken>()))
            .Returns(true)
            .Returns(false);
        mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .ReturnsAsync(false);

        _mockCollection.Setup(c => c.FindAsync(
            It.IsAny<FilterDefinition<LoopJoinRequest>>(),
            It.IsAny<FindOptions<LoopJoinRequest, LoopJoinRequest>>(),
            default))
            .ReturnsAsync(mockCursor.Object);

        //act
        var result = await _service.GetPendingJoinRequestsForLoopAsync(loopId);

        //assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task ApproveJoinRequestAsync_ApprovesRequest_WhenOwnerIsValid()
    {
        //arrange
        var requestId = "req1";
        var ownerId = "owner1";
        var loopId = "loop1";
        var userId = "user1";

        var joinRequest = new LoopJoinRequest
        {
            Id = requestId,
            LoopId = loopId,
            UserId = userId,
            Status = JoinRequestStatus.Pending
        };

        var mockCursor = new Mock<IAsyncCursor<LoopJoinRequest>>();
        mockCursor.Setup(c => c.Current).Returns(new[] { joinRequest });
        mockCursor.SetupSequence(c => c.MoveNext(It.IsAny<CancellationToken>()))
            .Returns(true)
            .Returns(false);
        mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .ReturnsAsync(false);

        _mockCollection.Setup(c => c.FindAsync(
            It.IsAny<FilterDefinition<LoopJoinRequest>>(),
            It.IsAny<FindOptions<LoopJoinRequest, LoopJoinRequest>>(),
            default))
            .ReturnsAsync(mockCursor.Object);

        _mockLoopService.Setup(s => s.IsLoopOwnerAsync(loopId, ownerId))
            .ReturnsAsync(true);

        _mockLoopService.Setup(s => s.AddMemberToLoopAsync(loopId, userId))
            .ReturnsAsync(new Loop { Id = loopId });

        var approvedRequest = new LoopJoinRequest
        {
            Id = requestId,
            LoopId = loopId,
            UserId = userId,
            Status = JoinRequestStatus.Approved,
            RespondedAt = DateTime.UtcNow
        };

        _mockCollection.Setup(c => c.FindOneAndUpdateAsync(
            It.IsAny<FilterDefinition<LoopJoinRequest>>(),
            It.IsAny<UpdateDefinition<LoopJoinRequest>>(),
            It.IsAny<FindOneAndUpdateOptions<LoopJoinRequest, LoopJoinRequest>>(),
            default))
            .ReturnsAsync(approvedRequest);

        //act
        var result = await _service.ApproveJoinRequestAsync(requestId, ownerId);

        //assert
        Assert.NotNull(result);
        Assert.Equal(JoinRequestStatus.Approved, result.Status);
        _mockLoopService.Verify(s => s.AddMemberToLoopAsync(loopId, userId), Times.Once);
    }

    [Fact]
    public async Task ApproveJoinRequestAsync_ReturnsNull_WhenRequestNotFound()
    {
        //arrange
        var requestId = "req1";
        var ownerId = "owner1";

        var mockCursor = new Mock<IAsyncCursor<LoopJoinRequest>>();
        mockCursor.Setup(c => c.Current).Returns(new List<LoopJoinRequest>());
        mockCursor.SetupSequence(c => c.MoveNext(It.IsAny<CancellationToken>()))
            .Returns(true)
            .Returns(false);
        mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .ReturnsAsync(false);

        _mockCollection.Setup(c => c.FindAsync(
            It.IsAny<FilterDefinition<LoopJoinRequest>>(),
            It.IsAny<FindOptions<LoopJoinRequest, LoopJoinRequest>>(),
            default))
            .ReturnsAsync(mockCursor.Object);

        //act
        var result = await _service.ApproveJoinRequestAsync(requestId, ownerId);

        //assert
        Assert.Null(result);
    }

    [Fact]
    public async Task RejectJoinRequestAsync_RejectsRequest_WhenOwnerIsValid()
    {
        //arrange
        var requestId = "req1";
        var ownerId = "owner1";
        var loopId = "loop1";
        var userId = "user1";

        var joinRequest = new LoopJoinRequest
        {
            Id = requestId,
            LoopId = loopId,
            UserId = userId,
            Status = JoinRequestStatus.Pending
        };

        var mockCursor = new Mock<IAsyncCursor<LoopJoinRequest>>();
        mockCursor.Setup(c => c.Current).Returns(new[] { joinRequest });
        mockCursor.SetupSequence(c => c.MoveNext(It.IsAny<CancellationToken>()))
            .Returns(true)
            .Returns(false);
        mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .ReturnsAsync(false);

        _mockCollection.Setup(c => c.FindAsync(
            It.IsAny<FilterDefinition<LoopJoinRequest>>(),
            It.IsAny<FindOptions<LoopJoinRequest, LoopJoinRequest>>(),
            default))
            .ReturnsAsync(mockCursor.Object);

        _mockLoopService.Setup(s => s.IsLoopOwnerAsync(loopId, ownerId))
            .ReturnsAsync(true);

        var rejectedRequest = new LoopJoinRequest
        {
            Id = requestId,
            LoopId = loopId,
            UserId = userId,
            Status = JoinRequestStatus.Rejected,
            RespondedAt = DateTime.UtcNow
        };

        _mockCollection.Setup(c => c.FindOneAndUpdateAsync(
            It.IsAny<FilterDefinition<LoopJoinRequest>>(),
            It.IsAny<UpdateDefinition<LoopJoinRequest>>(),
            It.IsAny<FindOneAndUpdateOptions<LoopJoinRequest, LoopJoinRequest>>(),
            default))
            .ReturnsAsync(rejectedRequest);

        //act
        var result = await _service.RejectJoinRequestAsync(requestId, ownerId);

        //assert
        Assert.NotNull(result);
        Assert.Equal(JoinRequestStatus.Rejected, result.Status);
    }

    [Fact]
    public async Task HasPendingJoinRequestAsync_ReturnsTrue_WhenPendingRequestExists()
    {
        //arrange
        var loopId = "loop1";
        var userId = "user1";

        _mockCollection.Setup(c => c.CountDocumentsAsync(
            It.IsAny<FilterDefinition<LoopJoinRequest>>(),
            null,
            default))
            .ReturnsAsync(1);

        //act
        var result = await _service.HasPendingJoinRequestAsync(loopId, userId);

        //assert
        Assert.True(result);
    }

    [Fact]
    public async Task HasPendingJoinRequestAsync_ReturnsFalse_WhenNoPendingRequestExists()
    {
        //arrange
        var loopId = "loop1";
        var userId = "user1";

        _mockCollection.Setup(c => c.CountDocumentsAsync(
            It.IsAny<FilterDefinition<LoopJoinRequest>>(),
            null,
            default))
            .ReturnsAsync(0);

        //act
        var result = await _service.HasPendingJoinRequestAsync(loopId, userId);

        //assert
        Assert.False(result);
    }
}
