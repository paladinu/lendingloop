using Api.Models;
using Api.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Moq;
using Xunit;

namespace Api.Tests;

public class LoopInvitationServiceTests
{
    private readonly Mock<IMongoDatabase> _mockDatabase;
    private readonly Mock<IMongoCollection<LoopInvitation>> _mockInvitationsCollection;
    private readonly Mock<IMongoCollection<User>> _mockUsersCollection;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<ILoopService> _mockLoopService;
    private readonly Mock<IUserService> _mockUserService;
    private readonly Mock<IEmailService> _mockEmailService;
    private readonly Mock<ILogger<LoopInvitationService>> _mockLogger;
    private readonly LoopInvitationService _service;

    public LoopInvitationServiceTests()
    {
        _mockDatabase = new Mock<IMongoDatabase>();
        _mockInvitationsCollection = new Mock<IMongoCollection<LoopInvitation>>();
        _mockUsersCollection = new Mock<IMongoCollection<User>>();
        _mockConfiguration = new Mock<IConfiguration>();
        _mockLoopService = new Mock<ILoopService>();
        _mockUserService = new Mock<IUserService>();
        _mockEmailService = new Mock<IEmailService>();
        _mockLogger = new Mock<ILogger<LoopInvitationService>>();

        _mockConfiguration.Setup(c => c["MongoDB:LoopInvitationsCollectionName"]).Returns("loopInvitations");
        _mockConfiguration.Setup(c => c["MongoDB:UsersCollectionName"]).Returns("users");
        _mockConfiguration.Setup(c => c["ASPNETCORE_ENVIRONMENT"]).Returns("Development");
        _mockConfiguration.Setup(c => c["Email:BaseUrl"]).Returns("http://localhost:4200");

        _mockDatabase.Setup(db => db.GetCollection<LoopInvitation>("loopInvitations", null))
            .Returns(_mockInvitationsCollection.Object);
        _mockDatabase.Setup(db => db.GetCollection<User>("users", null))
            .Returns(_mockUsersCollection.Object);

        _service = new LoopInvitationService(
            _mockDatabase.Object,
            _mockConfiguration.Object,
            _mockLoopService.Object,
            _mockUserService.Object,
            _mockEmailService.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task CreateEmailInvitationAsync_CreatesInvitation_ForNewEmail()
    {
        //arrange
        var loopId = "loop123";
        var invitedByUserId = "user123";
        var email = "newuser@example.com";

        _mockUserService.Setup(s => s.GetUserByEmailAsync(email))
            .ReturnsAsync((User)null!);

        _mockInvitationsCollection.Setup(c => c.InsertOneAsync(It.IsAny<LoopInvitation>(), null, default))
            .Returns(Task.CompletedTask);

        var loop = new Loop { Id = loopId, Name = "Test Loop" };
        var inviter = new User { Id = invitedByUserId, Email = "inviter@example.com", FirstName = "John", LastName = "Doe" };

        _mockLoopService.Setup(s => s.GetLoopByIdAsync(loopId))
            .ReturnsAsync(loop);
        _mockUserService.Setup(s => s.GetUserByIdAsync(invitedByUserId))
            .ReturnsAsync(inviter);

        _mockEmailService.Setup(s => s.SendLoopInvitationEmailAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>()))
            .ReturnsAsync(true);

        //act
        var result = await _service.CreateEmailInvitationAsync(loopId, invitedByUserId, email);

        //assert
        Assert.Equal(loopId, result.LoopId);
        Assert.Equal(invitedByUserId, result.InvitedByUserId);
        Assert.Equal(email, result.InvitedEmail);
        Assert.Equal(InvitationStatus.Pending, result.Status);
        Assert.NotNull(result.InvitationToken);
        Assert.True(result.ExpiresAt > DateTime.UtcNow);
    }

    [Fact]
    public async Task CreateEmailInvitationAsync_ThrowsException_WhenUserIsAlreadyMember()
    {
        //arrange
        var loopId = "loop123";
        var invitedByUserId = "user123";
        var email = "existing@example.com";
        var existingUser = new User { Id = "user456", Email = email };

        _mockUserService.Setup(s => s.GetUserByEmailAsync(email))
            .ReturnsAsync(existingUser);
        _mockLoopService.Setup(s => s.IsUserLoopMemberAsync(loopId, existingUser.Id!))
            .ReturnsAsync(true);

        //act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateEmailInvitationAsync(loopId, invitedByUserId, email));
    }

    [Fact]
    public async Task CreateEmailInvitationAsync_CreatesUserInvitation_WhenUserExistsButNotMember()
    {
        //arrange
        var loopId = "loop123";
        var invitedByUserId = "user123";
        var email = "existing@example.com";
        var existingUser = new User { Id = "user456", Email = email };

        _mockUserService.Setup(s => s.GetUserByEmailAsync(email))
            .ReturnsAsync(existingUser);
        _mockUserService.Setup(s => s.GetUserByIdAsync(existingUser.Id!))
            .ReturnsAsync(existingUser);
        _mockLoopService.Setup(s => s.IsUserLoopMemberAsync(loopId, existingUser.Id!))
            .ReturnsAsync(false);

        _mockInvitationsCollection.Setup(c => c.InsertOneAsync(It.IsAny<LoopInvitation>(), null, default))
            .Returns(Task.CompletedTask);

        //act
        var result = await _service.CreateEmailInvitationAsync(loopId, invitedByUserId, email);

        //assert
        Assert.Equal(loopId, result.LoopId);
        Assert.Equal(existingUser.Id, result.InvitedUserId);
        Assert.Equal(email, result.InvitedEmail);
    }

    [Fact]
    public async Task CreateUserInvitationAsync_CreatesInvitation_ForExistingUser()
    {
        //arrange
        var loopId = "loop123";
        var invitedByUserId = "user123";
        var invitedUserId = "user456";
        var invitedUser = new User { Id = invitedUserId, Email = "invited@example.com" };

        _mockLoopService.Setup(s => s.IsUserLoopMemberAsync(loopId, invitedUserId))
            .ReturnsAsync(false);
        _mockUserService.Setup(s => s.GetUserByIdAsync(invitedUserId))
            .ReturnsAsync(invitedUser);

        _mockInvitationsCollection.Setup(c => c.InsertOneAsync(It.IsAny<LoopInvitation>(), null, default))
            .Returns(Task.CompletedTask);

        //act
        var result = await _service.CreateUserInvitationAsync(loopId, invitedByUserId, invitedUserId);

        //assert
        Assert.Equal(loopId, result.LoopId);
        Assert.Equal(invitedByUserId, result.InvitedByUserId);
        Assert.Equal(invitedUserId, result.InvitedUserId);
        Assert.Equal(invitedUser.Email, result.InvitedEmail);
        Assert.Equal(InvitationStatus.Pending, result.Status);
    }

    [Fact]
    public async Task CreateUserInvitationAsync_ThrowsException_WhenUserIsAlreadyMember()
    {
        //arrange
        var loopId = "loop123";
        var invitedByUserId = "user123";
        var invitedUserId = "user456";

        _mockLoopService.Setup(s => s.IsUserLoopMemberAsync(loopId, invitedUserId))
            .ReturnsAsync(true);

        //act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateUserInvitationAsync(loopId, invitedByUserId, invitedUserId));
    }

    [Fact]
    public async Task CreateUserInvitationAsync_ThrowsException_WhenUserNotFound()
    {
        //arrange
        var loopId = "loop123";
        var invitedByUserId = "user123";
        var invitedUserId = "nonexistent";

        _mockLoopService.Setup(s => s.IsUserLoopMemberAsync(loopId, invitedUserId))
            .ReturnsAsync(false);
        _mockUserService.Setup(s => s.GetUserByIdAsync(invitedUserId))
            .ReturnsAsync((User)null!);

        //act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateUserInvitationAsync(loopId, invitedByUserId, invitedUserId));
    }

    [Fact]
    public async Task AcceptInvitationAsync_AcceptsInvitation_WithValidToken()
    {
        //arrange
        var token = "valid-token";
        var invitation = new LoopInvitation
        {
            Id = "inv123",
            LoopId = "loop123",
            InvitedEmail = "user@example.com",
            InvitedUserId = "user456",
            InvitationToken = token,
            Status = InvitationStatus.Pending,
            ExpiresAt = DateTime.UtcNow.AddDays(1)
        };

        var mockCursor = new Mock<IAsyncCursor<LoopInvitation>>();
        mockCursor.Setup(c => c.Current).Returns(new List<LoopInvitation> { invitation });
        mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .ReturnsAsync(false);

        _mockInvitationsCollection.Setup(c => c.FindAsync(
            It.IsAny<FilterDefinition<LoopInvitation>>(),
            It.IsAny<FindOptions<LoopInvitation, LoopInvitation>>(),
            default))
            .ReturnsAsync(mockCursor.Object);

        _mockLoopService.Setup(s => s.AddMemberToLoopAsync(invitation.LoopId, invitation.InvitedUserId!))
            .ReturnsAsync(new Loop());

        var acceptedInvitation = new LoopInvitation
        {
            Id = invitation.Id,
            Status = InvitationStatus.Accepted,
            AcceptedAt = DateTime.UtcNow
        };

        _mockInvitationsCollection.Setup(c => c.FindOneAndUpdateAsync(
            It.IsAny<FilterDefinition<LoopInvitation>>(),
            It.IsAny<UpdateDefinition<LoopInvitation>>(),
            It.IsAny<FindOneAndUpdateOptions<LoopInvitation>>(),
            default))
            .ReturnsAsync(acceptedInvitation);

        //act
        var result = await _service.AcceptInvitationAsync(token);

        //assert
        Assert.NotNull(result);
        Assert.Equal(InvitationStatus.Accepted, result.Status);
        Assert.NotNull(result.AcceptedAt);
    }

    [Fact]
    public async Task AcceptInvitationAsync_ReturnsNull_WithInvalidToken()
    {
        //arrange
        var token = "invalid-token";

        var mockCursor = new Mock<IAsyncCursor<LoopInvitation>>();
        mockCursor.Setup(c => c.Current).Returns(new List<LoopInvitation>());
        mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _mockInvitationsCollection.Setup(c => c.FindAsync(
            It.IsAny<FilterDefinition<LoopInvitation>>(),
            It.IsAny<FindOptions<LoopInvitation, LoopInvitation>>(),
            default))
            .ReturnsAsync(mockCursor.Object);

        //act
        var result = await _service.AcceptInvitationAsync(token);

        //assert
        Assert.Null(result);
    }

    [Fact]
    public async Task AcceptInvitationAsync_UsesCurrentUserId_WhenProvided()
    {
        //arrange
        var token = "valid-token";
        var currentUserId = "currentUser123";
        var currentUser = new User { Id = currentUserId, Email = "current@example.com" };
        var invitation = new LoopInvitation
        {
            Id = "inv123",
            LoopId = "loop123",
            InvitedEmail = "invited@example.com",
            InvitationToken = token,
            Status = InvitationStatus.Pending,
            ExpiresAt = DateTime.UtcNow.AddDays(1)
        };

        var mockCursor = new Mock<IAsyncCursor<LoopInvitation>>();
        mockCursor.Setup(c => c.Current).Returns(new List<LoopInvitation> { invitation });
        mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .ReturnsAsync(false);

        _mockInvitationsCollection.Setup(c => c.FindAsync(
            It.IsAny<FilterDefinition<LoopInvitation>>(),
            It.IsAny<FindOptions<LoopInvitation, LoopInvitation>>(),
            default))
            .ReturnsAsync(mockCursor.Object);

        _mockUserService.Setup(s => s.GetUserByIdAsync(currentUserId))
            .ReturnsAsync(currentUser);

        _mockLoopService.Setup(s => s.AddMemberToLoopAsync(invitation.LoopId, currentUserId))
            .ReturnsAsync(new Loop());

        var acceptedInvitation = new LoopInvitation
        {
            Id = invitation.Id,
            InvitedUserId = currentUserId,
            Status = InvitationStatus.Accepted
        };

        _mockInvitationsCollection.Setup(c => c.FindOneAndUpdateAsync(
            It.IsAny<FilterDefinition<LoopInvitation>>(),
            It.IsAny<UpdateDefinition<LoopInvitation>>(),
            It.IsAny<FindOneAndUpdateOptions<LoopInvitation>>(),
            default))
            .ReturnsAsync(acceptedInvitation);

        //act
        var result = await _service.AcceptInvitationAsync(token, currentUserId);

        //assert
        Assert.NotNull(result);
        Assert.Equal(currentUserId, result.InvitedUserId);
    }

    [Fact]
    public async Task AcceptInvitationByUserAsync_AcceptsInvitation_WhenUserMatches()
    {
        //arrange
        var invitationId = "inv123";
        var userId = "user456";
        var invitation = new LoopInvitation
        {
            Id = invitationId,
            LoopId = "loop123",
            InvitedUserId = userId,
            InvitedEmail = "user@example.com",
            Status = InvitationStatus.Pending,
            ExpiresAt = DateTime.UtcNow.AddDays(1)
        };

        var mockCursor = new Mock<IAsyncCursor<LoopInvitation>>();
        mockCursor.Setup(c => c.Current).Returns(new List<LoopInvitation> { invitation });
        mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .ReturnsAsync(false);

        _mockInvitationsCollection.Setup(c => c.FindAsync(
            It.IsAny<FilterDefinition<LoopInvitation>>(),
            It.IsAny<FindOptions<LoopInvitation, LoopInvitation>>(),
            default))
            .ReturnsAsync(mockCursor.Object);

        _mockLoopService.Setup(s => s.AddMemberToLoopAsync(invitation.LoopId, userId))
            .ReturnsAsync(new Loop());

        var acceptedInvitation = new LoopInvitation
        {
            Id = invitationId,
            Status = InvitationStatus.Accepted,
            AcceptedAt = DateTime.UtcNow
        };

        _mockInvitationsCollection.Setup(c => c.FindOneAndUpdateAsync(
            It.IsAny<FilterDefinition<LoopInvitation>>(),
            It.IsAny<UpdateDefinition<LoopInvitation>>(),
            It.IsAny<FindOneAndUpdateOptions<LoopInvitation>>(),
            default))
            .ReturnsAsync(acceptedInvitation);

        //act
        var result = await _service.AcceptInvitationByUserAsync(invitationId, userId);

        //assert
        Assert.NotNull(result);
        Assert.Equal(InvitationStatus.Accepted, result.Status);
    }

    [Fact]
    public async Task GetPendingInvitationsForUserAsync_ReturnsInvitations_ForUser()
    {
        //arrange
        var userId = "user123";
        var user = new User { Id = userId, Email = "user@example.com" };
        var invitations = new List<LoopInvitation>
        {
            new LoopInvitation { Id = "inv1", InvitedUserId = userId, Status = InvitationStatus.Pending },
            new LoopInvitation { Id = "inv2", InvitedEmail = user.Email, Status = InvitationStatus.Pending }
        };

        _mockUserService.Setup(s => s.GetUserByIdAsync(userId))
            .ReturnsAsync(user);

        var mockCursor = new Mock<IAsyncCursor<LoopInvitation>>();
        mockCursor.Setup(c => c.Current).Returns(invitations);
        mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .ReturnsAsync(false);

        _mockInvitationsCollection.Setup(c => c.FindAsync(
            It.IsAny<FilterDefinition<LoopInvitation>>(),
            It.IsAny<FindOptions<LoopInvitation, LoopInvitation>>(),
            default))
            .ReturnsAsync(mockCursor.Object);

        //act
        var result = await _service.GetPendingInvitationsForUserAsync(userId);

        //assert
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetPendingInvitationsForLoopAsync_ReturnsInvitations_ForLoop()
    {
        //arrange
        var loopId = "loop123";
        var invitations = new List<LoopInvitation>
        {
            new LoopInvitation { Id = "inv1", LoopId = loopId, Status = InvitationStatus.Pending },
            new LoopInvitation { Id = "inv2", LoopId = loopId, Status = InvitationStatus.Pending }
        };

        var mockCursor = new Mock<IAsyncCursor<LoopInvitation>>();
        mockCursor.Setup(c => c.Current).Returns(invitations);
        mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .ReturnsAsync(false);

        _mockInvitationsCollection.Setup(c => c.FindAsync(
            It.IsAny<FilterDefinition<LoopInvitation>>(),
            It.IsAny<FindOptions<LoopInvitation, LoopInvitation>>(),
            default))
            .ReturnsAsync(mockCursor.Object);

        //act
        var result = await _service.GetPendingInvitationsForLoopAsync(loopId);

        //assert
        Assert.Equal(2, result.Count);
        Assert.All(result, inv => Assert.Equal(loopId, inv.LoopId));
    }

    [Fact]
    public async Task ExpireOldInvitationsAsync_ExpiresOldInvitations()
    {
        //arrange
        _mockInvitationsCollection.Setup(c => c.UpdateManyAsync(
            It.IsAny<FilterDefinition<LoopInvitation>>(),
            It.IsAny<UpdateDefinition<LoopInvitation>>(),
            null,
            default))
            .ReturnsAsync(new UpdateResult.Acknowledged(5, 5, null));

        //act
        await _service.ExpireOldInvitationsAsync();

        //assert
        _mockInvitationsCollection.Verify(c => c.UpdateManyAsync(
            It.IsAny<FilterDefinition<LoopInvitation>>(),
            It.IsAny<UpdateDefinition<LoopInvitation>>(),
            null,
            default), Times.Once);
    }
}
