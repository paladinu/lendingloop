using Api.Models;
using Api.Services;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using Moq;
using Xunit;

namespace Api.Tests;

public class UserServiceTests
{
    private readonly Mock<IMongoDatabase> _mockDatabase;
    private readonly Mock<IMongoCollection<User>> _mockCollection;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly UserService _service;

    public UserServiceTests()
    {
        _mockDatabase = new Mock<IMongoDatabase>();
        _mockCollection = new Mock<IMongoCollection<User>>();
        _mockConfiguration = new Mock<IConfiguration>();

        _mockConfiguration.Setup(c => c["MongoDB:UsersCollectionName"]).Returns("users");
        _mockDatabase.Setup(db => db.GetCollection<User>("users", null))
            .Returns(_mockCollection.Object);

        _service = new UserService(_mockDatabase.Object, _mockConfiguration.Object);
    }

    [Fact]
    public async Task GetUserByEmailAsync_ReturnsUser_WhenUserExists()
    {
        //arrange
        var email = "test@example.com";
        var expectedUser = new User { Id = "user123", Email = email };

        var mockCursor = new Mock<IAsyncCursor<User>>();
        mockCursor.Setup(c => c.Current).Returns(new List<User> { expectedUser });
        mockCursor.SetupSequence(c => c.MoveNext(It.IsAny<CancellationToken>()))
            .Returns(true)
            .Returns(false);
        mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .ReturnsAsync(false);

        _mockCollection.Setup(c => c.FindAsync(
            It.IsAny<FilterDefinition<User>>(),
            It.IsAny<FindOptions<User, User>>(),
            default))
            .ReturnsAsync(mockCursor.Object);

        //act
        var result = await _service.GetUserByEmailAsync(email);

        //assert
        Assert.NotNull(result);
        Assert.Equal(email, result.Email);
        Assert.Equal("user123", result.Id);
    }

    [Fact]
    public async Task GetUserByEmailAsync_ReturnsNull_WhenUserDoesNotExist()
    {
        //arrange
        var email = "nonexistent@example.com";

        var mockCursor = new Mock<IAsyncCursor<User>>();
        mockCursor.Setup(c => c.Current).Returns(new List<User>());
        mockCursor.SetupSequence(c => c.MoveNext(It.IsAny<CancellationToken>()))
            .Returns(false);
        mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _mockCollection.Setup(c => c.FindAsync(
            It.IsAny<FilterDefinition<User>>(),
            It.IsAny<FindOptions<User, User>>(),
            default))
            .ReturnsAsync(mockCursor.Object);

        //act
        var result = await _service.GetUserByEmailAsync(email);

        //assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetUserByIdAsync_ReturnsUser_WhenUserExists()
    {
        //arrange
        var userId = "user123";
        var expectedUser = new User { Id = userId, Email = "test@example.com" };

        var mockCursor = new Mock<IAsyncCursor<User>>();
        mockCursor.Setup(c => c.Current).Returns(new List<User> { expectedUser });
        mockCursor.SetupSequence(c => c.MoveNext(It.IsAny<CancellationToken>()))
            .Returns(true)
            .Returns(false);
        mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .ReturnsAsync(false);

        _mockCollection.Setup(c => c.FindAsync(
            It.IsAny<FilterDefinition<User>>(),
            It.IsAny<FindOptions<User, User>>(),
            default))
            .ReturnsAsync(mockCursor.Object);

        //act
        var result = await _service.GetUserByIdAsync(userId);

        //assert
        Assert.NotNull(result);
        Assert.Equal(userId, result.Id);
    }

    [Fact]
    public async Task CreateUserAsync_SetsTimestamps_WhenCreatingUser()
    {
        //arrange
        var user = new User
        {
            Email = "newuser@example.com",
            PasswordHash = "hashedpassword",
            FirstName = "John",
            LastName = "Doe"
        };

        _mockCollection.Setup(c => c.InsertOneAsync(It.IsAny<User>(), null, default))
            .Returns(Task.CompletedTask);

        //act
        var result = await _service.CreateUserAsync(user);

        //assert
        Assert.NotEqual(default(DateTime), result.CreatedAt);
        Assert.NotEqual(default(DateTime), result.UpdatedAt);
        Assert.True((result.UpdatedAt - result.CreatedAt).TotalMilliseconds < 100);
    }

    [Fact]
    public async Task CreateUserAsync_PreservesUserProperties_WhenCreatingUser()
    {
        //arrange
        var user = new User
        {
            Email = "newuser@example.com",
            PasswordHash = "hashedpassword",
            FirstName = "John",
            LastName = "Doe",
            StreetAddress = "123 Main St"
        };

        _mockCollection.Setup(c => c.InsertOneAsync(It.IsAny<User>(), null, default))
            .Returns(Task.CompletedTask);

        //act
        var result = await _service.CreateUserAsync(user);

        //assert
        Assert.Equal("newuser@example.com", result.Email);
        Assert.Equal("hashedpassword", result.PasswordHash);
        Assert.Equal("John", result.FirstName);
        Assert.Equal("Doe", result.LastName);
        Assert.Equal("123 Main St", result.StreetAddress);
    }

    [Fact]
    public async Task UpdateUserAsync_UpdatesTimestamp_WhenUpdatingUser()
    {
        //arrange
        var userId = "user123";
        var user = new User
        {
            Id = userId,
            Email = "updated@example.com",
            FirstName = "Jane"
        };

        _mockCollection.Setup(c => c.ReplaceOneAsync(
            It.IsAny<FilterDefinition<User>>(),
            It.IsAny<User>(),
            It.IsAny<ReplaceOptions>(),
            default))
            .ReturnsAsync(new ReplaceOneResult.Acknowledged(1, 1, null));

        //act
        var result = await _service.UpdateUserAsync(userId, user);

        //assert
        Assert.NotNull(result);
        Assert.NotEqual(default(DateTime), result.UpdatedAt);
    }

    [Fact]
    public async Task UpdateUserAsync_ReturnsNull_WhenUserNotFound()
    {
        //arrange
        var userId = "nonexistent";
        var user = new User { Email = "test@example.com" };

        _mockCollection.Setup(c => c.ReplaceOneAsync(
            It.IsAny<FilterDefinition<User>>(),
            It.IsAny<User>(),
            It.IsAny<ReplaceOptions>(),
            default))
            .ReturnsAsync(new ReplaceOneResult.Acknowledged(0, 0, null));

        //act
        var result = await _service.UpdateUserAsync(userId, user);

        //assert
        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteUserAsync_ReturnsTrue_WhenUserDeleted()
    {
        //arrange
        var userId = "user123";

        _mockCollection.Setup(c => c.DeleteOneAsync(
            It.IsAny<FilterDefinition<User>>(),
            default))
            .ReturnsAsync(new DeleteResult.Acknowledged(1));

        //act
        var result = await _service.DeleteUserAsync(userId);

        //assert
        Assert.True(result);
    }

    [Fact]
    public async Task DeleteUserAsync_ReturnsFalse_WhenUserNotFound()
    {
        //arrange
        var userId = "nonexistent";

        _mockCollection.Setup(c => c.DeleteOneAsync(
            It.IsAny<FilterDefinition<User>>(),
            default))
            .ReturnsAsync(new DeleteResult.Acknowledged(0));

        //act
        var result = await _service.DeleteUserAsync(userId);

        //assert
        Assert.False(result);
    }

    [Fact]
    public async Task VerifyEmailAsync_VerifiesUser_WhenTokenIsValid()
    {
        //arrange
        var token = "valid-token";
        var verifiedUser = new User
        {
            Id = "user123",
            Email = "test@example.com",
            IsEmailVerified = true,
            EmailVerificationToken = null,
            EmailVerificationExpiry = null
        };

        _mockCollection.Setup(c => c.FindOneAndUpdateAsync(
            It.IsAny<FilterDefinition<User>>(),
            It.IsAny<UpdateDefinition<User>>(),
            It.IsAny<FindOneAndUpdateOptions<User>>(),
            default))
            .ReturnsAsync(verifiedUser);

        //act
        var result = await _service.VerifyEmailAsync(token);

        //assert
        Assert.NotNull(result);
        Assert.True(result.IsEmailVerified);
        Assert.Null(result.EmailVerificationToken);
        Assert.Null(result.EmailVerificationExpiry);
    }

    [Fact]
    public async Task VerifyEmailAsync_ReturnsNull_WhenTokenIsInvalid()
    {
        //arrange
        var token = "invalid-token";

        _mockCollection.Setup(c => c.FindOneAndUpdateAsync(
            It.IsAny<FilterDefinition<User>>(),
            It.IsAny<UpdateDefinition<User>>(),
            It.IsAny<FindOneAndUpdateOptions<User>>(),
            default))
            .ReturnsAsync((User)null!);

        //act
        var result = await _service.VerifyEmailAsync(token);

        //assert
        Assert.Null(result);
    }

    [Fact]
    public async Task VerifyEmailAsync_ReturnsNull_WhenTokenIsExpired()
    {
        //arrange
        var token = "expired-token";

        _mockCollection.Setup(c => c.FindOneAndUpdateAsync(
            It.IsAny<FilterDefinition<User>>(),
            It.IsAny<UpdateDefinition<User>>(),
            It.IsAny<FindOneAndUpdateOptions<User>>(),
            default))
            .ReturnsAsync((User)null!);

        //act
        var result = await _service.VerifyEmailAsync(token);

        //assert
        Assert.Null(result);
    }
}
