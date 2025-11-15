using Api.DTOs;
using Api.Models;
using MongoDB.Driver;
using System.Security.Cryptography;

namespace Api.Services;

public class LoopInvitationService : ILoopInvitationService
{
    private readonly IMongoCollection<LoopInvitation> _invitationsCollection;
    private readonly IMongoCollection<User> _usersCollection;
    private readonly ILoopService _loopService;
    private readonly IUserService _userService;
    private readonly IEmailService _emailService;
    private readonly ILogger<LoopInvitationService> _logger;
    private readonly IConfiguration _configuration;

    public LoopInvitationService(
        IMongoDatabase database,
        IConfiguration configuration,
        ILoopService loopService,
        IUserService userService,
        IEmailService emailService,
        ILogger<LoopInvitationService> logger)
    {
        var invitationsCollectionName = configuration["MongoDB:LoopInvitationsCollectionName"] ?? "loopInvitations";
        var usersCollectionName = configuration["MongoDB:UsersCollectionName"] ?? "users";
        
        _invitationsCollection = database.GetCollection<LoopInvitation>(invitationsCollectionName);
        _usersCollection = database.GetCollection<User>(usersCollectionName);
        _loopService = loopService;
        _userService = userService;
        _emailService = emailService;
        _logger = logger;
        _configuration = configuration;
        
        // Ensure indexes are created when service is instantiated
        _ = Task.Run(EnsureIndexesAsync);
    }

    public async Task<LoopInvitation> CreateEmailInvitationAsync(string loopId, string invitedByUserId, string email)
    {
        // Check if user with this email already exists and is a loop member
        var existingUser = await _userService.GetUserByEmailAsync(email);
        if (existingUser != null)
        {
            var isMember = await _loopService.IsUserLoopMemberAsync(loopId, existingUser.Id!);
            if (isMember)
            {
                // User is already a member, add them directly
                _logger.LogInformation("User {Email} is already a member of loop {LoopId}", email, loopId);
                throw new InvalidOperationException("User is already a member of this loop");
            }
            
            // User exists but is not a member, create user invitation instead
            return await CreateUserInvitationAsync(loopId, invitedByUserId, existingUser.Id!);
        }

        var invitation = new LoopInvitation
        {
            LoopId = loopId,
            InvitedByUserId = invitedByUserId,
            InvitedEmail = email,
            InvitationToken = GenerateSecureToken(),
            Status = InvitationStatus.Pending,
            ExpiresAt = DateTime.UtcNow.AddDays(30),
            CreatedAt = DateTime.UtcNow
        };

        await _invitationsCollection.InsertOneAsync(invitation);
        _logger.LogInformation("Email invitation created for {Email} to loop {LoopId}", email, loopId);

        // In development, log the invitation token and acceptance URL
        var environment = _configuration["ASPNETCORE_ENVIRONMENT"] ?? "Development";
        if (environment == "Development")
        {
            var baseUrl = _configuration["Email:BaseUrl"] ?? "http://localhost:4200";
            var acceptanceUrl = $"{baseUrl}/loops/accept-invitation?token={invitation.InvitationToken}";
            _logger.LogInformation("🔗 DEV MODE: Invitation token: {Token}", invitation.InvitationToken);
            _logger.LogInformation("🔗 DEV MODE: Acceptance URL: {Url}", acceptanceUrl);
        }

        // Send invitation email
        var loop = await _loopService.GetLoopByIdAsync(loopId);
        var inviter = await _userService.GetUserByIdAsync(invitedByUserId);
        
        if (loop != null && inviter != null)
        {
            var inviterName = $"{inviter.FirstName} {inviter.LastName}".Trim();
            if (string.IsNullOrEmpty(inviterName))
            {
                inviterName = inviter.Email;
            }
            
            await _emailService.SendLoopInvitationEmailAsync(
                email,
                "",
                inviterName,
                loop.Name,
                invitation.InvitationToken
            );
        }

        return invitation;
    }

    public async Task<LoopInvitation> CreateUserInvitationAsync(string loopId, string invitedByUserId, string invitedUserId)
    {
        // Check if user is already a member
        var isMember = await _loopService.IsUserLoopMemberAsync(loopId, invitedUserId);
        if (isMember)
        {
            _logger.LogInformation("User {UserId} is already a member of loop {LoopId}", invitedUserId, loopId);
            throw new InvalidOperationException("User is already a member of this loop");
        }

        var invitedUser = await _userService.GetUserByIdAsync(invitedUserId);
        if (invitedUser == null)
        {
            throw new InvalidOperationException("Invited user not found");
        }

        var invitation = new LoopInvitation
        {
            LoopId = loopId,
            InvitedByUserId = invitedByUserId,
            InvitedEmail = invitedUser.Email,
            InvitedUserId = invitedUserId,
            InvitationToken = GenerateSecureToken(),
            Status = InvitationStatus.Pending,
            ExpiresAt = DateTime.UtcNow.AddDays(30),
            CreatedAt = DateTime.UtcNow
        };

        await _invitationsCollection.InsertOneAsync(invitation);
        _logger.LogInformation("User invitation created for {UserId} to loop {LoopId}", invitedUserId, loopId);

        return invitation;
    }

    public async Task<LoopInvitation?> AcceptInvitationAsync(string token, string? currentUserId = null)
    {
        var filter = Builders<LoopInvitation>.Filter.And(
            Builders<LoopInvitation>.Filter.Eq(i => i.InvitationToken, token),
            Builders<LoopInvitation>.Filter.Eq(i => i.Status, InvitationStatus.Pending),
            Builders<LoopInvitation>.Filter.Gt(i => i.ExpiresAt, DateTime.UtcNow)
        );

        var invitation = await _invitationsCollection.Find(filter).FirstOrDefaultAsync();
        if (invitation == null)
        {
            _logger.LogWarning("Invitation not found or expired for token");
            return null;
        }

        // Determine which user to use for accepting the invitation
        string? userId = null;

        // Priority 1: Use current authenticated user if provided (for dev convenience)
        // In dev, this allows any logged-in user to accept the invitation
        if (!string.IsNullOrEmpty(currentUserId))
        {
            var currentUser = await _userService.GetUserByIdAsync(currentUserId);
            if (currentUser != null)
            {
                userId = currentUserId;
                _logger.LogInformation("Using authenticated user {UserId} ({Email}) to accept invitation for {InvitedEmail}", 
                    userId, currentUser.Email, invitation.InvitedEmail);
            }
        }

        // Priority 2: Use invitation's user ID if available
        if (string.IsNullOrEmpty(userId) && !string.IsNullOrEmpty(invitation.InvitedUserId))
        {
            userId = invitation.InvitedUserId;
        }

        // Priority 3: Find user by invitation email
        if (string.IsNullOrEmpty(userId))
        {
            var user = await _userService.GetUserByEmailAsync(invitation.InvitedEmail);
            userId = user?.Id;
        }

        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning("Cannot accept invitation: user not found for email {Email}", invitation.InvitedEmail);
            return null;
        }

        // Add user to loop
        await _loopService.AddMemberToLoopAsync(invitation.LoopId, userId);

        // Update invitation status and link it to the accepting user
        var update = Builders<LoopInvitation>.Update
            .Set(i => i.Status, InvitationStatus.Accepted)
            .Set(i => i.AcceptedAt, DateTime.UtcNow)
            .Set(i => i.InvitedUserId, userId);

        var options = new FindOneAndUpdateOptions<LoopInvitation>
        {
            ReturnDocument = ReturnDocument.After
        };

        var updatedInvitation = await _invitationsCollection.FindOneAndUpdateAsync(
            Builders<LoopInvitation>.Filter.Eq(i => i.Id, invitation.Id),
            update,
            options
        );

        _logger.LogInformation("Invitation {InvitationId} accepted by user {UserId}", invitation.Id, userId);
        return updatedInvitation;
    }

    public async Task<LoopInvitation?> AcceptInvitationByUserAsync(string invitationId, string userId)
    {
        var filter = Builders<LoopInvitation>.Filter.And(
            Builders<LoopInvitation>.Filter.Eq(i => i.Id, invitationId),
            Builders<LoopInvitation>.Filter.Eq(i => i.Status, InvitationStatus.Pending),
            Builders<LoopInvitation>.Filter.Gt(i => i.ExpiresAt, DateTime.UtcNow)
        );

        var invitation = await _invitationsCollection.Find(filter).FirstOrDefaultAsync();
        if (invitation == null)
        {
            _logger.LogWarning("Invitation {InvitationId} not found or expired", invitationId);
            return null;
        }

        // Verify the invitation is for this user
        if (invitation.InvitedUserId != userId)
        {
            var user = await _userService.GetUserByIdAsync(userId);
            if (user == null || user.Email != invitation.InvitedEmail)
            {
                _logger.LogWarning("User {UserId} attempted to accept invitation not meant for them", userId);
                return null;
            }
        }

        // Add user to loop
        await _loopService.AddMemberToLoopAsync(invitation.LoopId, userId);

        // Update invitation status
        var update = Builders<LoopInvitation>.Update
            .Set(i => i.Status, InvitationStatus.Accepted)
            .Set(i => i.AcceptedAt, DateTime.UtcNow)
            .Set(i => i.InvitedUserId, userId);

        var options = new FindOneAndUpdateOptions<LoopInvitation>
        {
            ReturnDocument = ReturnDocument.After
        };

        var updatedInvitation = await _invitationsCollection.FindOneAndUpdateAsync(
            Builders<LoopInvitation>.Filter.Eq(i => i.Id, invitationId),
            update,
            options
        );

        _logger.LogInformation("Invitation {InvitationId} accepted by user {UserId}", invitationId, userId);
        return updatedInvitation;
    }

    public async Task<List<LoopInvitation>> GetPendingInvitationsForUserAsync(string userId)
    {
        var user = await _userService.GetUserByIdAsync(userId);
        if (user == null)
        {
            return new List<LoopInvitation>();
        }

        var filter = Builders<LoopInvitation>.Filter.And(
            Builders<LoopInvitation>.Filter.Or(
                Builders<LoopInvitation>.Filter.Eq(i => i.InvitedUserId, userId),
                Builders<LoopInvitation>.Filter.Eq(i => i.InvitedEmail, user.Email)
            ),
            Builders<LoopInvitation>.Filter.Eq(i => i.Status, InvitationStatus.Pending),
            Builders<LoopInvitation>.Filter.Gt(i => i.ExpiresAt, DateTime.UtcNow)
        );

        return await _invitationsCollection.Find(filter).ToListAsync();
    }

    public async Task<List<LoopInvitation>> GetPendingInvitationsForLoopAsync(string loopId)
    {
        var filter = Builders<LoopInvitation>.Filter.And(
            Builders<LoopInvitation>.Filter.Eq(i => i.LoopId, loopId),
            Builders<LoopInvitation>.Filter.Eq(i => i.Status, InvitationStatus.Pending),
            Builders<LoopInvitation>.Filter.Gt(i => i.ExpiresAt, DateTime.UtcNow)
        );

        return await _invitationsCollection.Find(filter).ToListAsync();
    }

    public async Task ExpireOldInvitationsAsync()
    {
        var filter = Builders<LoopInvitation>.Filter.And(
            Builders<LoopInvitation>.Filter.Eq(i => i.Status, InvitationStatus.Pending),
            Builders<LoopInvitation>.Filter.Lt(i => i.ExpiresAt, DateTime.UtcNow)
        );

        var update = Builders<LoopInvitation>.Update.Set(i => i.Status, InvitationStatus.Expired);

        var result = await _invitationsCollection.UpdateManyAsync(filter, update);
        _logger.LogInformation("Expired {Count} old invitations", result.ModifiedCount);
    }

    public async Task DeleteInvitationsForLoopAsync(string loopId)
    {
        var filter = Builders<LoopInvitation>.Filter.Eq(i => i.LoopId, loopId);
        var result = await _invitationsCollection.DeleteManyAsync(filter);
        _logger.LogInformation("Deleted {Count} invitations for loop {LoopId}", result.DeletedCount, loopId);
    }

    private string GenerateSecureToken()
    {
        var bytes = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(bytes);
        }
        return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").Replace("=", "");
    }

    private async Task EnsureIndexesAsync()
    {
        try
        {
            // Skip index creation if collection is not initialized (e.g., in test scenarios)
            if (_invitationsCollection == null || _invitationsCollection.Database == null)
            {
                return;
            }

            // Verify database connection before creating indexes
            try
            {
                await _invitationsCollection.Database.ListCollectionNamesAsync();
            }
            catch
            {
                // Database not accessible, skip index creation
                return;
            }

            // Create index on loopId
            var loopIdIndexKeys = Builders<LoopInvitation>.IndexKeys.Ascending(i => i.LoopId);
            var loopIdIndexModel = new CreateIndexModel<LoopInvitation>(loopIdIndexKeys);

            // Create index on invitedEmail
            var emailIndexKeys = Builders<LoopInvitation>.IndexKeys.Ascending(i => i.InvitedEmail);
            var emailIndexModel = new CreateIndexModel<LoopInvitation>(emailIndexKeys);

            // Create index on invitedUserId
            var userIdIndexKeys = Builders<LoopInvitation>.IndexKeys.Ascending(i => i.InvitedUserId);
            var userIdIndexModel = new CreateIndexModel<LoopInvitation>(userIdIndexKeys);

            // Create unique index on invitationToken
            var tokenIndexKeys = Builders<LoopInvitation>.IndexKeys.Ascending(i => i.InvitationToken);
            var tokenIndexOptions = new CreateIndexOptions { Unique = true };
            var tokenIndexModel = new CreateIndexModel<LoopInvitation>(tokenIndexKeys, tokenIndexOptions);

            // Create index on status
            var statusIndexKeys = Builders<LoopInvitation>.IndexKeys.Ascending(i => i.Status);
            var statusIndexModel = new CreateIndexModel<LoopInvitation>(statusIndexKeys);

            // Create index on expiresAt for cleanup queries
            var expiresAtIndexKeys = Builders<LoopInvitation>.IndexKeys.Ascending(i => i.ExpiresAt);
            var expiresAtIndexModel = new CreateIndexModel<LoopInvitation>(expiresAtIndexKeys);

            // Create compound index on invitedUserId + status
            var compoundIndexKeys = Builders<LoopInvitation>.IndexKeys
                .Ascending(i => i.InvitedUserId)
                .Ascending(i => i.Status);
            var compoundIndexModel = new CreateIndexModel<LoopInvitation>(compoundIndexKeys);

            await _invitationsCollection.Indexes.CreateManyAsync(new[]
            {
                loopIdIndexModel,
                emailIndexModel,
                userIdIndexModel,
                tokenIndexModel,
                statusIndexModel,
                expiresAtIndexModel,
                compoundIndexModel
            });

            _logger.LogInformation("Indexes created successfully for LoopInvitations collection");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not create indexes for LoopInvitations collection");
        }
    }

    public async Task<LoopInvitationResponse> EnrichLoopInvitationAsync(LoopInvitation invitation)
    {
        var response = new LoopInvitationResponse
        {
            Id = invitation.Id,
            LoopId = invitation.LoopId,
            InvitedByUserId = invitation.InvitedByUserId,
            InvitedEmail = invitation.InvitedEmail,
            InvitedUserId = invitation.InvitedUserId,
            InvitationToken = invitation.InvitationToken,
            Status = invitation.Status,
            ExpiresAt = invitation.ExpiresAt,
            CreatedAt = invitation.CreatedAt,
            AcceptedAt = invitation.AcceptedAt
        };

        // Populate loop name
        var loop = await _loopService.GetLoopByIdAsync(invitation.LoopId);
        if (loop != null)
        {
            response.LoopName = loop.Name;
        }

        // Populate inviter name and score
        var inviter = await _userService.GetUserByIdAsync(invitation.InvitedByUserId);
        if (inviter != null)
        {
            response.InvitedByUserName = $"{inviter.FirstName} {inviter.LastName}".Trim();
            response.InvitedByUserLoopScore = inviter.LoopScore;
        }

        return response;
    }

    public async Task<List<LoopInvitationResponse>> EnrichLoopInvitationsAsync(List<LoopInvitation> invitations)
    {
        var enrichedInvitations = new List<LoopInvitationResponse>();
        
        foreach (var invitation in invitations)
        {
            var enriched = await EnrichLoopInvitationAsync(invitation);
            enrichedInvitations.Add(enriched);
        }

        return enrichedInvitations;
    }
}
