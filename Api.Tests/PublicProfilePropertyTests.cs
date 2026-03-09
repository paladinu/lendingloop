using FsCheck;
using FsCheck.Xunit;
using Api.Models;
using Api.DTOs;
using Api.Services;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using Moq;

namespace Api.Tests;

/// <summary>
/// Property-based tests for public profile functionality
/// </summary>
public class PublicProfilePropertyTests
{
    /// <summary>
    /// **Feature: view-item-owner-profile, Property 1: Loop membership validation**
    /// For any two distinct users who share at least one loop,
    /// when one user requests the other's public profile,
    /// the service should return a successful PublicProfileDto with valid profile data
    /// **Validates: Requirements 1.2, 4.5, 5.2**
    /// </summary>
    [Property(MaxTest = 100)]
    public bool LoopMembershipValidation_ReturnsSuccessfulProfileData_WhenUsersShareLoops(
        string requestingUserId,
        string targetUserId,
        string firstName,
        string lastName,
        int loopScore)
    {
        //arrange
        // Constrain inputs to valid ranges
        var validRequestingUserId = string.IsNullOrWhiteSpace(requestingUserId) 
            ? "user-requesting-" + Guid.NewGuid().ToString() 
            : requestingUserId.Trim();
        
        var validTargetUserId = string.IsNullOrWhiteSpace(targetUserId) 
            ? "user-target-" + Guid.NewGuid().ToString() 
            : targetUserId.Trim();
        
        // Ensure users are distinct
        if (validRequestingUserId == validTargetUserId)
        {
            validTargetUserId = validTargetUserId + "-different";
        }
        
        var validFirstName = string.IsNullOrWhiteSpace(firstName) ? "John" : firstName.Trim();
        var validLastName = string.IsNullOrWhiteSpace(lastName) ? "Doe" : lastName.Trim();
        var validLoopScore = Math.Abs(loopScore % 1000); // 0-999
        
        // Create target user with private information
        var targetUser = new User
        {
            Id = validTargetUserId,
            Email = $"{validTargetUserId}@example.com",
            FirstName = validFirstName,
            LastName = validLastName,
            StreetAddress = "123 Private Street",
            LoopScore = validLoopScore
        };
        
        // Setup mocks
        var mockDatabase = new Mock<IMongoDatabase>();
        var mockCollection = new Mock<IMongoCollection<User>>();
        var mockConfiguration = new Mock<IConfiguration>();
        var mockLoopService = new Mock<ILoopService>();
        var mockLoopScoreService = new Mock<ILoopScoreService>();
        
        mockConfiguration.Setup(c => c["MongoDB:UsersCollectionName"]).Returns("users");
        mockDatabase.Setup(db => db.GetCollection<User>("users", null))
            .Returns(mockCollection.Object);
        
        // Setup user lookup to return target user
        var mockCursor = new Mock<IAsyncCursor<User>>();
        mockCursor.Setup(c => c.Current).Returns(new List<User> { targetUser });
        mockCursor.SetupSequence(c => c.MoveNext(It.IsAny<CancellationToken>()))
            .Returns(true)
            .Returns(false);
        mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .ReturnsAsync(false);
        
        mockCollection.Setup(c => c.FindAsync(
            It.IsAny<FilterDefinition<User>>(),
            It.IsAny<FindOptions<User, User>>(),
            default))
            .ReturnsAsync(mockCursor.Object);
        
        // Setup loop membership validation - users share at least one loop
        mockLoopService.Setup(s => s.DoUsersShareLoopAsync(validRequestingUserId, validTargetUserId))
            .ReturnsAsync(true);
        
        // Setup loop score service
        mockLoopScoreService.Setup(s => s.GetUserScoreAsync(validTargetUserId))
            .ReturnsAsync(validLoopScore);
        
        var badges = new List<BadgeAward>
        {
            new BadgeAward { BadgeType = BadgeType.Bronze, AwardedAt = DateTime.UtcNow }
        };
        mockLoopScoreService.Setup(s => s.GetUserBadgesAsync(validTargetUserId))
            .ReturnsAsync(badges);
        
        var scoreHistory = new List<ScoreHistoryEntry>
        {
            new ScoreHistoryEntry
            {
                Timestamp = DateTime.UtcNow,
                Points = 10,
                ActionType = ScoreActionType.BorrowCompleted,
                ItemRequestId = "req123",
                ItemName = "Test Item"
            }
        };
        mockLoopScoreService.Setup(s => s.GetScoreHistoryAsync(validTargetUserId, It.IsAny<int>()))
            .ReturnsAsync(scoreHistory);
        
        var service = new UserService(
            mockDatabase.Object,
            mockConfiguration.Object,
            mockLoopService.Object,
            mockLoopScoreService.Object
        );
        
        //act
        PublicProfileDto? result = null;
        Exception? exception = null;
        
        try
        {
            result = service.GetPublicProfileAsync(validRequestingUserId, validTargetUserId).Result;
        }
        catch (Exception ex)
        {
            exception = ex;
        }
        
        //assert
        // Property 1: When users share loops, the API should return successful profile data
        if (exception != null)
        {
            return false; // Should not throw exception when users share loops
        }
        
        if (result == null)
        {
            return false; // Should return a valid result
        }
        
        // Verify required fields are present
        if (result.UserId != validTargetUserId)
        {
            return false;
        }
        
        if (result.FirstName != validFirstName)
        {
            return false;
        }
        
        if (result.LastName != validLastName)
        {
            return false;
        }
        
        if (result.LoopScore != validLoopScore)
        {
            return false;
        }
        
        // Verify badges and score history are included
        if (result.Badges == null)
        {
            return false;
        }
        
        if (result.ScoreHistory == null)
        {
            return false;
        }
        
        // Verify that PublicProfileDto does not expose private fields
        var resultType = result.GetType();
        if (resultType.GetProperty("Email") != null)
        {
            return false; // Email should not be exposed in public profile
        }
        
        if (resultType.GetProperty("StreetAddress") != null)
        {
            return false; // StreetAddress should not be exposed in public profile
        }
        
        return true;
    }
    
    /// <summary>
    /// **Feature: view-item-owner-profile, Property 2: No shared loops returns forbidden**
    /// For any two distinct users who do not share any loops,
    /// when one user requests the other's public profile,
    /// the service should throw UnauthorizedAccessException
    /// **Validates: Requirements 1.4, 3.6, 4.6, 5.3**
    /// </summary>
    [Property(MaxTest = 100)]
    public bool NoSharedLoops_ThrowsUnauthorizedAccessException_WhenUsersDoNotShareLoops(
        string requestingUserId,
        string targetUserId,
        string firstName,
        string lastName,
        int loopScore)
    {
        //arrange
        // Constrain inputs to valid ranges
        var validRequestingUserId = string.IsNullOrWhiteSpace(requestingUserId) 
            ? "user-requesting-" + Guid.NewGuid().ToString() 
            : requestingUserId.Trim();
        
        var validTargetUserId = string.IsNullOrWhiteSpace(targetUserId) 
            ? "user-target-" + Guid.NewGuid().ToString() 
            : targetUserId.Trim();
        
        // Ensure users are distinct
        if (validRequestingUserId == validTargetUserId)
        {
            validTargetUserId = validTargetUserId + "-different";
        }
        
        var validFirstName = string.IsNullOrWhiteSpace(firstName) ? "John" : firstName.Trim();
        var validLastName = string.IsNullOrWhiteSpace(lastName) ? "Doe" : lastName.Trim();
        var validLoopScore = Math.Abs(loopScore % 1000); // 0-999
        
        // Create target user
        var targetUser = new User
        {
            Id = validTargetUserId,
            Email = $"{validTargetUserId}@example.com",
            FirstName = validFirstName,
            LastName = validLastName,
            StreetAddress = "123 Private Street",
            LoopScore = validLoopScore
        };
        
        // Setup mocks
        var mockDatabase = new Mock<IMongoDatabase>();
        var mockCollection = new Mock<IMongoCollection<User>>();
        var mockConfiguration = new Mock<IConfiguration>();
        var mockLoopService = new Mock<ILoopService>();
        var mockLoopScoreService = new Mock<ILoopScoreService>();
        
        mockConfiguration.Setup(c => c["MongoDB:UsersCollectionName"]).Returns("users");
        mockDatabase.Setup(db => db.GetCollection<User>("users", null))
            .Returns(mockCollection.Object);
        
        // Setup user lookup to return target user
        var mockCursor = new Mock<IAsyncCursor<User>>();
        mockCursor.Setup(c => c.Current).Returns(new List<User> { targetUser });
        mockCursor.SetupSequence(c => c.MoveNext(It.IsAny<CancellationToken>()))
            .Returns(true)
            .Returns(false);
        mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .ReturnsAsync(false);
        
        mockCollection.Setup(c => c.FindAsync(
            It.IsAny<FilterDefinition<User>>(),
            It.IsAny<FindOptions<User, User>>(),
            default))
            .ReturnsAsync(mockCursor.Object);
        
        // Setup loop membership validation - users DO NOT share any loops
        mockLoopService.Setup(s => s.DoUsersShareLoopAsync(validRequestingUserId, validTargetUserId))
            .ReturnsAsync(false);
        
        var service = new UserService(
            mockDatabase.Object,
            mockConfiguration.Object,
            mockLoopService.Object,
            mockLoopScoreService.Object
        );
        
        //act
        Exception? exception = null;
        
        try
        {
            var result = service.GetPublicProfileAsync(validRequestingUserId, validTargetUserId).Result;
        }
        catch (AggregateException ex)
        {
            // Unwrap AggregateException from .Result
            exception = ex.InnerException;
        }
        catch (Exception ex)
        {
            exception = ex;
        }
        
        //assert
        // Property 2: When users do not share loops, the service should throw UnauthorizedAccessException
        if (exception == null)
        {
            return false; // Should throw exception when users don't share loops
        }
        
        if (exception is not UnauthorizedAccessException)
        {
            return false; // Should specifically throw UnauthorizedAccessException
        }
        
        // Verify the exception message is appropriate
        if (string.IsNullOrWhiteSpace(exception.Message))
        {
            return false; // Exception should have a meaningful message
        }
        
        return true;
    }
    
    /// <summary>
    /// **Feature: view-item-owner-profile, Property 3: Public profiles exclude private fields**
    /// For any public profile response (when viewing another user's profile),
    /// the response should not contain email or streetAddress fields
    /// **Validates: Requirements 1.5, 1.6, 4.1, 4.2**
    /// </summary>
    [Property(MaxTest = 100)]
    public bool PublicProfileExcludesPrivateFields_DoesNotContainEmailOrAddress_ForAllValidRequests(
        string requestingUserId,
        string targetUserId,
        string firstName,
        string lastName,
        string email,
        string streetAddress,
        int loopScore)
    {
        //arrange
        // Constrain inputs to valid ranges
        var validRequestingUserId = string.IsNullOrWhiteSpace(requestingUserId) 
            ? "user-requesting-" + Guid.NewGuid().ToString() 
            : requestingUserId.Trim();
        
        var validTargetUserId = string.IsNullOrWhiteSpace(targetUserId) 
            ? "user-target-" + Guid.NewGuid().ToString() 
            : targetUserId.Trim();
        
        // Ensure users are distinct
        if (validRequestingUserId == validTargetUserId)
        {
            validTargetUserId = validTargetUserId + "-different";
        }
        
        var validFirstName = string.IsNullOrWhiteSpace(firstName) ? "John" : firstName.Trim();
        var validLastName = string.IsNullOrWhiteSpace(lastName) ? "Doe" : lastName.Trim();
        var validEmail = string.IsNullOrWhiteSpace(email) 
            ? $"{validTargetUserId}@example.com" 
            : email.Trim();
        var validStreetAddress = string.IsNullOrWhiteSpace(streetAddress) 
            ? "123 Private Street" 
            : streetAddress.Trim();
        var validLoopScore = Math.Abs(loopScore % 1000); // 0-999
        
        // Create target user with private information (email and streetAddress)
        var targetUser = new User
        {
            Id = validTargetUserId,
            Email = validEmail,
            FirstName = validFirstName,
            LastName = validLastName,
            StreetAddress = validStreetAddress,
            LoopScore = validLoopScore
        };
        
        // Setup mocks
        var mockDatabase = new Mock<IMongoDatabase>();
        var mockCollection = new Mock<IMongoCollection<User>>();
        var mockConfiguration = new Mock<IConfiguration>();
        var mockLoopService = new Mock<ILoopService>();
        var mockLoopScoreService = new Mock<ILoopScoreService>();
        
        mockConfiguration.Setup(c => c["MongoDB:UsersCollectionName"]).Returns("users");
        mockDatabase.Setup(db => db.GetCollection<User>("users", null))
            .Returns(mockCollection.Object);
        
        // Setup user lookup to return target user
        var mockCursor = new Mock<IAsyncCursor<User>>();
        mockCursor.Setup(c => c.Current).Returns(new List<User> { targetUser });
        mockCursor.SetupSequence(c => c.MoveNext(It.IsAny<CancellationToken>()))
            .Returns(true)
            .Returns(false);
        mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .ReturnsAsync(false);
        
        mockCollection.Setup(c => c.FindAsync(
            It.IsAny<FilterDefinition<User>>(),
            It.IsAny<FindOptions<User, User>>(),
            default))
            .ReturnsAsync(mockCursor.Object);
        
        // Setup loop membership validation - users share at least one loop
        mockLoopService.Setup(s => s.DoUsersShareLoopAsync(validRequestingUserId, validTargetUserId))
            .ReturnsAsync(true);
        
        // Setup loop score service
        mockLoopScoreService.Setup(s => s.GetUserScoreAsync(validTargetUserId))
            .ReturnsAsync(validLoopScore);
        
        var badges = new List<BadgeAward>
        {
            new BadgeAward { BadgeType = BadgeType.Bronze, AwardedAt = DateTime.UtcNow }
        };
        mockLoopScoreService.Setup(s => s.GetUserBadgesAsync(validTargetUserId))
            .ReturnsAsync(badges);
        
        var scoreHistory = new List<ScoreHistoryEntry>
        {
            new ScoreHistoryEntry
            {
                Timestamp = DateTime.UtcNow,
                Points = 10,
                ActionType = ScoreActionType.BorrowCompleted,
                ItemRequestId = "req123",
                ItemName = "Test Item"
            }
        };
        mockLoopScoreService.Setup(s => s.GetScoreHistoryAsync(validTargetUserId, It.IsAny<int>()))
            .ReturnsAsync(scoreHistory);
        
        var service = new UserService(
            mockDatabase.Object,
            mockConfiguration.Object,
            mockLoopService.Object,
            mockLoopScoreService.Object
        );
        
        //act
        PublicProfileDto? result = null;
        Exception? exception = null;
        
        try
        {
            result = service.GetPublicProfileAsync(validRequestingUserId, validTargetUserId).Result;
        }
        catch (Exception ex)
        {
            exception = ex;
        }
        
        //assert
        // Property 3: Public profile responses should never contain email or streetAddress fields
        
        // First, verify the request succeeded (users share loops)
        if (exception != null || result == null)
        {
            return false; // Should succeed when users share loops
        }
        
        // Verify that PublicProfileDto type does not have Email or StreetAddress properties
        var resultType = result.GetType();
        
        if (resultType.GetProperty("Email") != null)
        {
            return false; // Email property should not exist in PublicProfileDto
        }
        
        if (resultType.GetProperty("StreetAddress") != null)
        {
            return false; // StreetAddress property should not exist in PublicProfileDto
        }
        
        // Note: We don't check if email/address VALUES appear in other fields
        // because legitimate fields (like lastName) could coincidentally match
        // The important check is that Email and StreetAddress PROPERTIES don't exist
        
        // Verify that required public fields ARE present
        if (result.UserId != validTargetUserId)
        {
            return false;
        }
        
        if (result.FirstName != validFirstName)
        {
            return false;
        }
        
        if (result.LastName != validLastName)
        {
            return false;
        }
        
        if (result.LoopScore != validLoopScore)
        {
            return false;
        }
        
        if (result.Badges == null)
        {
            return false;
        }
        
        if (result.ScoreHistory == null)
        {
            return false;
        }
        
        return true;
    }
    
    /// <summary>
    /// **Feature: view-item-owner-profile, Property 4: Non-existent user returns not found**
    /// For any non-existent userId,
    /// when a user requests that profile,
    /// the service should throw KeyNotFoundException
    /// **Validates: Requirements 1.7, 3.5**
    /// </summary>
    [Property(MaxTest = 100)]
    public bool NonExistentUser_ThrowsKeyNotFoundException_ForAllNonExistentUserIds(
        string requestingUserId,
        string nonExistentUserId)
    {
        //arrange
        // Constrain inputs to valid ranges
        var validRequestingUserId = string.IsNullOrWhiteSpace(requestingUserId) 
            ? "user-requesting-" + Guid.NewGuid().ToString() 
            : requestingUserId.Trim();
        
        var validNonExistentUserId = string.IsNullOrWhiteSpace(nonExistentUserId) 
            ? "nonexistent-user-" + Guid.NewGuid().ToString() 
            : nonExistentUserId.Trim();
        
        // Ensure users are distinct
        if (validRequestingUserId == validNonExistentUserId)
        {
            validNonExistentUserId = validNonExistentUserId + "-nonexistent";
        }
        
        // Setup mocks
        var mockDatabase = new Mock<IMongoDatabase>();
        var mockCollection = new Mock<IMongoCollection<User>>();
        var mockConfiguration = new Mock<IConfiguration>();
        var mockLoopService = new Mock<ILoopService>();
        var mockLoopScoreService = new Mock<ILoopScoreService>();
        
        mockConfiguration.Setup(c => c["MongoDB:UsersCollectionName"]).Returns("users");
        mockDatabase.Setup(db => db.GetCollection<User>("users", null))
            .Returns(mockCollection.Object);
        
        // Setup user lookup to return null (user does not exist)
        var mockCursor = new Mock<IAsyncCursor<User>>();
        mockCursor.Setup(c => c.Current).Returns(new List<User>()); // Empty list = no user found
        mockCursor.SetupSequence(c => c.MoveNext(It.IsAny<CancellationToken>()))
            .Returns(false); // No results
        mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false); // No results
        
        mockCollection.Setup(c => c.FindAsync(
            It.IsAny<FilterDefinition<User>>(),
            It.IsAny<FindOptions<User, User>>(),
            default))
            .ReturnsAsync(mockCursor.Object);
        
        var service = new UserService(
            mockDatabase.Object,
            mockConfiguration.Object,
            mockLoopService.Object,
            mockLoopScoreService.Object
        );
        
        //act
        Exception? exception = null;
        
        try
        {
            var result = service.GetPublicProfileAsync(validRequestingUserId, validNonExistentUserId).Result;
        }
        catch (AggregateException ex)
        {
            // Unwrap AggregateException from .Result
            exception = ex.InnerException;
        }
        catch (Exception ex)
        {
            exception = ex;
        }
        
        //assert
        // Property 4: When requesting a non-existent user's profile, the service should throw KeyNotFoundException
        if (exception == null)
        {
            return false; // Should throw exception when user doesn't exist
        }
        
        if (exception is not KeyNotFoundException)
        {
            return false; // Should specifically throw KeyNotFoundException
        }
        
        // Verify the exception message is appropriate
        if (string.IsNullOrWhiteSpace(exception.Message))
        {
            return false; // Exception should have a meaningful message
        }
        
        // Verify the message indicates user not found
        if (!exception.Message.Contains("User not found", StringComparison.OrdinalIgnoreCase))
        {
            return false; // Message should indicate user was not found
        }
        
        return true;
    }
    
    /// <summary>
    /// **Feature: view-item-owner-profile, Property 5: Public profile contains required fields**
    /// For any successful public profile response,
    /// the response should contain userId, firstName, lastName, loopScore, badges array, and scoreHistory array
    /// **Validates: Requirements 1.3, 6.1, 6.2, 6.3**
    /// </summary>
    [Property(MaxTest = 100)]
    public bool PublicProfileContainsRequiredFields_HasAllRequiredFields_ForAllValidRequests(
        string requestingUserId,
        string targetUserId,
        string firstName,
        string lastName,
        int loopScore,
        int badgeCount,
        int historyCount)
    {
        //arrange
        // Constrain inputs to valid ranges
        var validRequestingUserId = string.IsNullOrWhiteSpace(requestingUserId) 
            ? "user-requesting-" + Guid.NewGuid().ToString() 
            : requestingUserId.Trim();
        
        var validTargetUserId = string.IsNullOrWhiteSpace(targetUserId) 
            ? "user-target-" + Guid.NewGuid().ToString() 
            : targetUserId.Trim();
        
        // Ensure users are distinct
        if (validRequestingUserId == validTargetUserId)
        {
            validTargetUserId = validTargetUserId + "-different";
        }
        
        var validFirstName = string.IsNullOrWhiteSpace(firstName) ? "John" : firstName.Trim();
        var validLastName = string.IsNullOrWhiteSpace(lastName) ? "Doe" : lastName.Trim();
        var validLoopScore = Math.Abs(loopScore % 1000); // 0-999
        var validBadgeCount = Math.Abs(badgeCount % 10); // 0-9 badges
        var validHistoryCount = Math.Abs(historyCount % 20); // 0-19 history entries
        
        // Create target user
        var targetUser = new User
        {
            Id = validTargetUserId,
            Email = $"{validTargetUserId}@example.com",
            FirstName = validFirstName,
            LastName = validLastName,
            StreetAddress = "123 Private Street",
            LoopScore = validLoopScore
        };
        
        // Setup mocks
        var mockDatabase = new Mock<IMongoDatabase>();
        var mockCollection = new Mock<IMongoCollection<User>>();
        var mockConfiguration = new Mock<IConfiguration>();
        var mockLoopService = new Mock<ILoopService>();
        var mockLoopScoreService = new Mock<ILoopScoreService>();
        
        mockConfiguration.Setup(c => c["MongoDB:UsersCollectionName"]).Returns("users");
        mockDatabase.Setup(db => db.GetCollection<User>("users", null))
            .Returns(mockCollection.Object);
        
        // Setup user lookup to return target user
        var mockCursor = new Mock<IAsyncCursor<User>>();
        mockCursor.Setup(c => c.Current).Returns(new List<User> { targetUser });
        mockCursor.SetupSequence(c => c.MoveNext(It.IsAny<CancellationToken>()))
            .Returns(true)
            .Returns(false);
        mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .ReturnsAsync(false);
        
        mockCollection.Setup(c => c.FindAsync(
            It.IsAny<FilterDefinition<User>>(),
            It.IsAny<FindOptions<User, User>>(),
            default))
            .ReturnsAsync(mockCursor.Object);
        
        // Setup loop membership validation - users share at least one loop
        mockLoopService.Setup(s => s.DoUsersShareLoopAsync(validRequestingUserId, validTargetUserId))
            .ReturnsAsync(true);
        
        // Setup loop score service
        mockLoopScoreService.Setup(s => s.GetUserScoreAsync(validTargetUserId))
            .ReturnsAsync(validLoopScore);
        
        // Generate random badges
        var badges = new List<BadgeAward>();
        var badgeTypes = Enum.GetValues<BadgeType>();
        for (int i = 0; i < validBadgeCount && i < badgeTypes.Length; i++)
        {
            badges.Add(new BadgeAward 
            { 
                BadgeType = badgeTypes[i], 
                AwardedAt = DateTime.UtcNow.AddDays(-i) 
            });
        }
        mockLoopScoreService.Setup(s => s.GetUserBadgesAsync(validTargetUserId))
            .ReturnsAsync(badges);
        
        // Generate random score history
        var scoreHistory = new List<ScoreHistoryEntry>();
        var actionTypes = Enum.GetValues<ScoreActionType>();
        for (int i = 0; i < validHistoryCount; i++)
        {
            scoreHistory.Add(new ScoreHistoryEntry
            {
                Timestamp = DateTime.UtcNow.AddDays(-i),
                Points = (i % 2 == 0) ? 10 : -5,
                ActionType = actionTypes[i % actionTypes.Length],
                ItemRequestId = $"req{i}",
                ItemName = $"Item {i}"
            });
        }
        mockLoopScoreService.Setup(s => s.GetScoreHistoryAsync(validTargetUserId, It.IsAny<int>()))
            .ReturnsAsync(scoreHistory);
        
        var service = new UserService(
            mockDatabase.Object,
            mockConfiguration.Object,
            mockLoopService.Object,
            mockLoopScoreService.Object
        );
        
        //act
        PublicProfileDto? result = null;
        Exception? exception = null;
        
        try
        {
            result = service.GetPublicProfileAsync(validRequestingUserId, validTargetUserId).Result;
        }
        catch (Exception ex)
        {
            exception = ex;
        }
        
        //assert
        // Property 5: Public profile response should contain all required fields
        
        // First, verify the request succeeded
        if (exception != null)
        {
            return false; // Should not throw exception when users share loops
        }
        
        if (result == null)
        {
            return false; // Should return a valid result
        }
        
        // Verify userId field is present and correct
        if (string.IsNullOrWhiteSpace(result.UserId))
        {
            return false; // UserId should not be null or empty
        }
        
        if (result.UserId != validTargetUserId)
        {
            return false; // UserId should match target user
        }
        
        // Verify firstName field is present and correct
        if (string.IsNullOrWhiteSpace(result.FirstName))
        {
            return false; // FirstName should not be null or empty
        }
        
        if (result.FirstName != validFirstName)
        {
            return false; // FirstName should match target user
        }
        
        // Verify lastName field is present and correct
        if (string.IsNullOrWhiteSpace(result.LastName))
        {
            return false; // LastName should not be null or empty
        }
        
        if (result.LastName != validLastName)
        {
            return false; // LastName should match target user
        }
        
        // Verify loopScore field is present and correct
        if (result.LoopScore != validLoopScore)
        {
            return false; // LoopScore should match expected value
        }
        
        // Verify badges field is present (not null)
        // The property is that badges array exists, not that it has a specific count
        if (result.Badges == null)
        {
            return false; // Badges array should not be null
        }
        
        // Verify scoreHistory field is present (not null)
        // The property is that scoreHistory array exists, not that it has a specific count
        if (result.ScoreHistory == null)
        {
            return false; // ScoreHistory array should not be null
        }
        
        // Verify that all required properties exist on the DTO type
        var resultType = result.GetType();
        
        if (resultType.GetProperty("UserId") == null)
        {
            return false; // UserId property must exist
        }
        
        if (resultType.GetProperty("FirstName") == null)
        {
            return false; // FirstName property must exist
        }
        
        if (resultType.GetProperty("LastName") == null)
        {
            return false; // LastName property must exist
        }
        
        if (resultType.GetProperty("LoopScore") == null)
        {
            return false; // LoopScore property must exist
        }
        
        if (resultType.GetProperty("Badges") == null)
        {
            return false; // Badges property must exist
        }
        
        if (resultType.GetProperty("ScoreHistory") == null)
        {
            return false; // ScoreHistory property must exist
        }
        
        return true;
    }
}
