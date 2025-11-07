using Api.Models;
using Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LoopsController : ControllerBase
{
    private readonly ILoopService _loopService;
    private readonly ILoopInvitationService _invitationService;
    private readonly IItemsService _itemsService;
    private readonly IUserService _userService;
    private readonly ILogger<LoopsController> _logger;

    public LoopsController(
        ILoopService loopService,
        ILoopInvitationService invitationService,
        IItemsService itemsService,
        IUserService userService,
        ILogger<LoopsController> logger)
    {
        _loopService = loopService;
        _invitationService = invitationService;
        _itemsService = itemsService;
        _userService = userService;
        _logger = logger;
    }

    private string GetUserId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
    }

    [HttpPost]
    public async Task<IActionResult> CreateLoop([FromBody] CreateLoopRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { message = "Loop name is required" });
        }

        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        try
        {
            var loop = await _loopService.CreateLoopAsync(request.Name, userId);
            return Ok(loop);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating loop");
            return StatusCode(500, new { message = "An error occurred while creating the loop" });
        }
    }

    // Public Loops Discovery - Use explicit route to avoid conflicts
    [Route("public")]
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetPublicLoops([FromQuery] int skip = 0, [FromQuery] int limit = 20)
    {
        try
        {
            var loops = await _loopService.GetPublicLoopsAsync(skip, limit);
            
            var enrichedLoops = new List<object>();
            foreach (var loop in loops)
            {
                var items = await _itemsService.GetItemsByLoopIdAsync(loop.Id!);
                enrichedLoops.Add(new
                {
                    loop.Id,
                    loop.Name,
                    loop.Description,
                    loop.CreatedAt,
                    MemberCount = loop.MemberIds.Count,
                    ItemCount = items.Count
                });
            }

            return Ok(enrichedLoops);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting public loops");
            return StatusCode(500, new { message = "An error occurred while retrieving public loops" });
        }
    }

    [Route("public/search")]
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> SearchPublicLoops([FromQuery] string q, [FromQuery] int skip = 0, [FromQuery] int limit = 20)
    {
        if (string.IsNullOrWhiteSpace(q))
        {
            return BadRequest(new { message = "Search query is required" });
        }

        try
        {
            var loops = await _loopService.SearchPublicLoopsAsync(q, skip, limit);
            
            var enrichedLoops = new List<object>();
            foreach (var loop in loops)
            {
                var items = await _itemsService.GetItemsByLoopIdAsync(loop.Id!);
                enrichedLoops.Add(new
                {
                    loop.Id,
                    loop.Name,
                    loop.Description,
                    loop.CreatedAt,
                    MemberCount = loop.MemberIds.Count,
                    ItemCount = items.Count
                });
            }

            return Ok(enrichedLoops);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching public loops");
            return StatusCode(500, new { message = "An error occurred while searching public loops" });
        }
    }

    [Route("archived")]
    [HttpGet]
    public async Task<IActionResult> GetArchivedLoops()
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        try
        {
            var loops = await _loopService.GetArchivedLoopsAsync(userId);
            
            var enrichedLoops = new List<object>();
            foreach (var loop in loops)
            {
                enrichedLoops.Add(new
                {
                    loop.Id,
                    loop.Name,
                    loop.Description,
                    loop.CreatorId,
                    loop.MemberIds,
                    loop.IsArchived,
                    loop.ArchivedAt,
                    loop.CreatedAt,
                    loop.UpdatedAt,
                    MemberCount = loop.MemberIds.Count
                });
            }

            return Ok(enrichedLoops);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting archived loops for user {UserId}", userId);
            return StatusCode(500, new { message = "An error occurred while retrieving archived loops" });
        }
    }

    [Route("invitations/pending")]
    [HttpGet]
    public async Task<IActionResult> GetPendingInvitations()
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        try
        {
            var invitations = await _invitationService.GetPendingInvitationsForUserAsync(userId);
            
            // Enrich invitations with loop and inviter information
            var enrichedInvitations = new List<object>();
            foreach (var invitation in invitations)
            {
                var loop = await _loopService.GetLoopByIdAsync(invitation.LoopId);
                var inviter = await _userService.GetUserByIdAsync(invitation.InvitedByUserId);
                
                var inviterName = inviter != null
                    ? $"{inviter.FirstName} {inviter.LastName}".Trim()
                    : "Unknown";

                if (string.IsNullOrEmpty(inviterName) || inviterName == "Unknown")
                {
                    inviterName = inviter?.Email ?? "Unknown";
                }

                enrichedInvitations.Add(new
                {
                    invitation.Id,
                    invitation.LoopId,
                    LoopName = loop?.Name ?? "Unknown",
                    invitation.InvitedByUserId,
                    InvitedByUserName = inviterName,
                    invitation.InvitedEmail,
                    invitation.InvitedUserId,
                    invitation.InvitationToken,
                    invitation.Status,
                    invitation.ExpiresAt,
                    invitation.CreatedAt,
                    invitation.AcceptedAt
                });
            }

            return Ok(enrichedInvitations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pending invitations for user {UserId}", userId);
            return StatusCode(500, new { message = "An error occurred while retrieving invitations" });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetUserLoops()
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        try
        {
            var loops = await _loopService.GetUserLoopsAsync(userId);
            
            // Enrich loops with member count and item count
            var enrichedLoops = new List<object>();
            foreach (var loop in loops)
            {
                var items = await _itemsService.GetItemsByLoopIdAsync(loop.Id!);
                enrichedLoops.Add(new
                {
                    loop.Id,
                    loop.Name,
                    loop.CreatorId,
                    loop.MemberIds,
                    loop.CreatedAt,
                    loop.UpdatedAt,
                    MemberCount = loop.MemberIds.Count,
                    ItemCount = items.Count
                });
            }

            return Ok(enrichedLoops);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user loops");
            return StatusCode(500, new { message = "An error occurred while retrieving loops" });
        }
    }

    [HttpGet("{id:regex(^[[0-9a-fA-F]]{{24}}$)}")]
    public async Task<IActionResult> GetLoopById(string id)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        try
        {
            var loop = await _loopService.GetLoopByIdAsync(id);
            if (loop == null)
            {
                return NotFound(new { message = "Loop not found" });
            }

            // Check if user is a member
            if (!loop.MemberIds.Contains(userId))
            {
                return Forbid();
            }

            var items = await _itemsService.GetItemsByLoopIdAsync(id);
            return Ok(new
            {
                loop.Id,
                loop.Name,
                loop.CreatorId,
                loop.MemberIds,
                loop.CreatedAt,
                loop.UpdatedAt,
                MemberCount = loop.MemberIds.Count,
                ItemCount = items.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting loop {LoopId}", id);
            return StatusCode(500, new { message = "An error occurred while retrieving the loop" });
        }
    }

    [HttpGet("{id:regex(^[[0-9a-fA-F]]{{24}}$)}/members")]
    public async Task<IActionResult> GetLoopMembers(string id)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        try
        {
            var isMember = await _loopService.IsUserLoopMemberAsync(id, userId);
            if (!isMember)
            {
                return Forbid();
            }

            var members = await _loopService.GetLoopMembersAsync(id);
            var memberDtos = members.Select(m => new
            {
                m.Id,
                m.Email,
                m.FirstName,
                m.LastName
            });

            return Ok(memberDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting loop members for loop {LoopId}", id);
            return StatusCode(500, new { message = "An error occurred while retrieving loop members" });
        }
    }

    [HttpGet("{id:regex(^[[0-9a-fA-F]]{{24}}$)}/items")]
    public async Task<IActionResult> GetLoopItems(string id, [FromQuery] string? search = null)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        try
        {
            var isMember = await _loopService.IsUserLoopMemberAsync(id, userId);
            if (!isMember)
            {
                return Forbid();
            }

            var items = await _itemsService.GetItemsByLoopIdAsync(id);

            // Apply search filter if provided
            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchLower = search.ToLower();
                items = items.Where(item =>
                    item.Name.ToLower().Contains(searchLower) ||
                    item.Description.ToLower().Contains(searchLower)
                ).ToList();
            }

            // Enrich items with owner information
            var enrichedItems = new List<object>();
            foreach (var item in items)
            {
                var owner = await _userService.GetUserByIdAsync(item.UserId);
                var ownerName = owner != null
                    ? $"{owner.FirstName} {owner.LastName}".Trim()
                    : "Unknown";

                if (string.IsNullOrEmpty(ownerName) || ownerName == "Unknown")
                {
                    ownerName = owner?.Email ?? "Unknown";
                }

                enrichedItems.Add(new
                {
                    item.Id,
                    item.Name,
                    item.Description,
                    item.UserId,
                    item.IsAvailable,
                    item.ImageUrl,
                    item.VisibleToLoopIds,
                    item.VisibleToAllLoops,
                    item.VisibleToFutureLoops,
                    item.CreatedAt,
                    item.UpdatedAt,
                    OwnerName = ownerName
                });
            }

            return Ok(new
            {
                items = enrichedItems,
                totalCount = enrichedItems.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting items for loop {LoopId}", id);
            return StatusCode(500, new { message = "An error occurred while retrieving loop items" });
        }
    }

    [HttpPost("{id:regex(^[[0-9a-fA-F]]{{24}}$)}/invite-email")]
    public async Task<IActionResult> InviteByEmail(string id, [FromBody] InviteByEmailRequest request)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return BadRequest(new { message = "Email is required" });
        }

        try
        {
            var isMember = await _loopService.IsUserLoopMemberAsync(id, userId);
            if (!isMember)
            {
                return Forbid();
            }

            var invitation = await _invitationService.CreateEmailInvitationAsync(id, userId, request.Email);
            return Ok(invitation);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating email invitation for loop {LoopId}", id);
            return StatusCode(500, new { message = "An error occurred while sending the invitation" });
        }
    }

    [HttpPost("{id:regex(^[[0-9a-fA-F]]{{24}}$)}/invite-user")]
    public async Task<IActionResult> InviteUser(string id, [FromBody] InviteUserRequest request)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(request.UserId))
        {
            return BadRequest(new { message = "User ID is required" });
        }

        try
        {
            var isMember = await _loopService.IsUserLoopMemberAsync(id, userId);
            if (!isMember)
            {
                return Forbid();
            }

            var invitation = await _invitationService.CreateUserInvitationAsync(id, userId, request.UserId);
            return Ok(invitation);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user invitation for loop {LoopId}", id);
            return StatusCode(500, new { message = "An error occurred while sending the invitation" });
        }
    }

    [HttpGet("{id:regex(^[[0-9a-fA-F]]{{24}}$)}/potential-invitees")]
    public async Task<IActionResult> GetPotentialInvitees(string id)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        try
        {
            var isMember = await _loopService.IsUserLoopMemberAsync(id, userId);
            if (!isMember)
            {
                return Forbid();
            }

            var users = await _loopService.GetPotentialInviteesFromOtherLoopsAsync(userId, id);
            var userDtos = users.Select(u => new
            {
                u.Id,
                u.Email,
                u.FirstName,
                u.LastName
            });

            return Ok(userDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting potential invitees for loop {LoopId}", id);
            return StatusCode(500, new { message = "An error occurred while retrieving potential invitees" });
        }
    }

    [HttpPost("invitations/{token}/accept")]
    [AllowAnonymous]
    public async Task<IActionResult> AcceptInvitationByToken(string token)
    {
        try
        {
            // Try to get current user if authenticated (for dev convenience)
            string? currentUserId = null;
            if (User.Identity?.IsAuthenticated == true)
            {
                currentUserId = GetUserId();
                _logger.LogInformation("Authenticated user {UserId} accepting invitation by token", currentUserId);
            }

            var invitation = await _invitationService.AcceptInvitationAsync(token, currentUserId);
            if (invitation == null)
            {
                return StatusCode(410, new { message = "Invitation not found or has expired" });
            }

            return Ok(invitation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error accepting invitation by token");
            return StatusCode(500, new { message = "An error occurred while accepting the invitation" });
        }
    }

    [HttpPost("invitations/{id}/accept-user")]
    public async Task<IActionResult> AcceptInvitationByUser(string id)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        try
        {
            var invitation = await _invitationService.AcceptInvitationByUserAsync(id, userId);
            if (invitation == null)
            {
                return StatusCode(410, new { message = "Invitation not found or has expired" });
            }

            return Ok(invitation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error accepting invitation {InvitationId}", id);
            return StatusCode(500, new { message = "An error occurred while accepting the invitation" });
        }
    }

    // Loop Settings Management
    [HttpPut("{id:regex(^[[0-9a-fA-F]]{{24}}$)}/settings")]
    public async Task<IActionResult> UpdateLoopSettings(string id, [FromBody] UpdateLoopSettingsRequest request)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        try
        {
            var isOwner = await _loopService.IsLoopOwnerAsync(id, userId);
            if (!isOwner)
            {
                return StatusCode(403, new { message = "Only the loop owner can update settings" });
            }

            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return BadRequest(new { message = "Loop name is required" });
            }

            if (request.Description.Length > 500)
            {
                return BadRequest(new { message = "Description must be 500 characters or less" });
            }

            var loop = await _loopService.UpdateLoopSettingsAsync(id, request.Name, request.Description, request.IsPublic);
            if (loop == null)
            {
                return NotFound(new { message = "Loop not found" });
            }

            return Ok(loop);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating loop settings for loop {LoopId}", id);
            return StatusCode(500, new { message = "An error occurred while updating loop settings" });
        }
    }

    [HttpGet("{id:regex(^[[0-9a-fA-F]]{{24}}$)}/settings")]
    public async Task<IActionResult> GetLoopSettings(string id)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        try
        {
            var loop = await _loopService.GetLoopByIdAsync(id);
            if (loop == null)
            {
                return NotFound(new { message = "Loop not found" });
            }

            var isMember = loop.MemberIds.Contains(userId);
            if (!isMember)
            {
                return Forbid();
            }

            return Ok(new
            {
                loop.Name,
                loop.Description,
                loop.IsPublic
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting loop settings for loop {LoopId}", id);
            return StatusCode(500, new { message = "An error occurred while retrieving loop settings" });
        }
    }

    // Loop Archival
    [HttpPost("{id:regex(^[[0-9a-fA-F]]{{24}}$)}/archive")]
    public async Task<IActionResult> ArchiveLoop(string id)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        try
        {
            var isOwner = await _loopService.IsLoopOwnerAsync(id, userId);
            if (!isOwner)
            {
                return StatusCode(403, new { message = "Only the loop owner can archive the loop" });
            }

            var loop = await _loopService.ArchiveLoopAsync(id);
            if (loop == null)
            {
                return NotFound(new { message = "Loop not found" });
            }

            return Ok(loop);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error archiving loop {LoopId}", id);
            return StatusCode(500, new { message = "An error occurred while archiving the loop" });
        }
    }

    [HttpPost("{id:regex(^[[0-9a-fA-F]]{{24}}$)}/restore")]
    public async Task<IActionResult> RestoreLoop(string id)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        try
        {
            var isOwner = await _loopService.IsLoopOwnerAsync(id, userId);
            if (!isOwner)
            {
                return StatusCode(403, new { message = "Only the loop owner can restore the loop" });
            }

            var loop = await _loopService.RestoreLoopAsync(id);
            if (loop == null)
            {
                return NotFound(new { message = "Loop not found" });
            }

            return Ok(loop);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error restoring loop {LoopId}", id);
            return StatusCode(500, new { message = "An error occurred while restoring the loop" });
        }
    }

    // Loop Deletion
    [HttpDelete("{id:regex(^[[0-9a-fA-F]]{{24}}$)}")]
    public async Task<IActionResult> DeleteLoop(string id)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        try
        {
            var isOwner = await _loopService.IsLoopOwnerAsync(id, userId);
            if (!isOwner)
            {
                return StatusCode(403, new { message = "Only the loop owner can delete the loop" });
            }

            var success = await _loopService.DeleteLoopAsync(id);
            if (!success)
            {
                return NotFound(new { message = "Loop not found" });
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting loop {LoopId}", id);
            return StatusCode(500, new { message = "An error occurred while deleting the loop" });
        }
    }

    // Ownership Transfer
    [HttpPost("{id:regex(^[[0-9a-fA-F]]{{24}}$)}/transfer-ownership")]
    public async Task<IActionResult> InitiateOwnershipTransfer(string id, [FromBody] TransferOwnershipRequest request)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        try
        {
            var isOwner = await _loopService.IsLoopOwnerAsync(id, userId);
            if (!isOwner)
            {
                return StatusCode(403, new { message = "Only the loop owner can transfer ownership" });
            }

            var loop = await _loopService.GetLoopByIdAsync(id);
            if (loop == null)
            {
                return NotFound(new { message = "Loop not found" });
            }

            if (!loop.MemberIds.Contains(request.NewOwnerId))
            {
                return BadRequest(new { message = "New owner must be a member of the loop" });
            }

            var updatedLoop = await _loopService.InitiateOwnershipTransferAsync(id, userId, request.NewOwnerId);
            if (updatedLoop == null)
            {
                return Conflict(new { message = "A pending ownership transfer already exists" });
            }

            return Ok(updatedLoop);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initiating ownership transfer for loop {LoopId}", id);
            return StatusCode(500, new { message = "An error occurred while initiating ownership transfer" });
        }
    }

    [HttpPost("{id:regex(^[[0-9a-fA-F]]{{24}}$)}/transfer-ownership/accept")]
    public async Task<IActionResult> AcceptOwnershipTransfer(string id)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        try
        {
            var loop = await _loopService.AcceptOwnershipTransferAsync(id, userId);
            if (loop == null)
            {
                return NotFound(new { message = "No pending transfer found for this user" });
            }

            return Ok(loop);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error accepting ownership transfer for loop {LoopId}", id);
            return StatusCode(500, new { message = "An error occurred while accepting ownership transfer" });
        }
    }

    [HttpPost("{id:regex(^[[0-9a-fA-F]]{{24}}$)}/transfer-ownership/decline")]
    public async Task<IActionResult> DeclineOwnershipTransfer(string id)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        try
        {
            var loop = await _loopService.DeclineOwnershipTransferAsync(id, userId);
            if (loop == null)
            {
                return NotFound(new { message = "No pending transfer found for this user" });
            }

            return Ok(loop);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error declining ownership transfer for loop {LoopId}", id);
            return StatusCode(500, new { message = "An error occurred while declining ownership transfer" });
        }
    }

    [HttpPost("{id:regex(^[[0-9a-fA-F]]{{24}}$)}/transfer-ownership/cancel")]
    public async Task<IActionResult> CancelOwnershipTransfer(string id)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        try
        {
            var loop = await _loopService.CancelOwnershipTransferAsync(id, userId);
            if (loop == null)
            {
                return NotFound(new { message = "No pending transfer found initiated by this user" });
            }

            return Ok(loop);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling ownership transfer for loop {LoopId}", id);
            return StatusCode(500, new { message = "An error occurred while cancelling ownership transfer" });
        }
    }

    [HttpGet("{id:regex(^[[0-9a-fA-F]]{{24}}$)}/transfer-ownership/pending")]
    public async Task<IActionResult> GetPendingOwnershipTransfer(string id)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        try
        {
            var isMember = await _loopService.IsUserLoopMemberAsync(id, userId);
            if (!isMember)
            {
                return Forbid();
            }

            var transfer = await _loopService.GetPendingOwnershipTransferAsync(id);
            if (transfer == null)
            {
                return Ok(new { hasPendingTransfer = false });
            }

            // Enrich with user names
            var fromUser = await _userService.GetUserByIdAsync(transfer.FromUserId);
            var toUser = await _userService.GetUserByIdAsync(transfer.ToUserId);

            return Ok(new
            {
                hasPendingTransfer = true,
                transfer.FromUserId,
                FromUserName = fromUser != null ? $"{fromUser.FirstName} {fromUser.LastName}".Trim() : "Unknown",
                transfer.ToUserId,
                ToUserName = toUser != null ? $"{toUser.FirstName} {toUser.LastName}".Trim() : "Unknown",
                transfer.TransferredAt,
                transfer.Status
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pending ownership transfer for loop {LoopId}", id);
            return StatusCode(500, new { message = "An error occurred while retrieving pending transfer" });
        }
    }

    // Member Management
    [HttpDelete("{id:regex(^[[0-9a-fA-F]]{{24}}$)}/members/{memberId:regex(^[[0-9a-fA-F]]{{24}}$)}")]
    public async Task<IActionResult> RemoveMember(string id, string memberId)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        try
        {
            var isOwner = await _loopService.IsLoopOwnerAsync(id, userId);
            if (!isOwner)
            {
                return StatusCode(403, new { message = "Only the loop owner can remove members" });
            }

            var loop = await _loopService.GetLoopByIdAsync(id);
            if (loop == null)
            {
                return NotFound(new { message = "Loop not found" });
            }

            if (!loop.MemberIds.Contains(memberId))
            {
                return NotFound(new { message = "Member not found in loop" });
            }

            if (loop.CreatorId == memberId)
            {
                return BadRequest(new { message = "Cannot remove the loop owner" });
            }

            await _loopService.RemoveMemberFromLoopAsync(id, memberId);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing member {MemberId} from loop {LoopId}", memberId, id);
            return StatusCode(500, new { message = "An error occurred while removing the member" });
        }
    }

    [HttpPost("{id:regex(^[[0-9a-fA-F]]{{24}}$)}/leave")]
    public async Task<IActionResult> LeaveLoop(string id)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        try
        {
            var loop = await _loopService.LeaveLoopAsync(id, userId);
            if (loop == null)
            {
                var isOwner = await _loopService.IsLoopOwnerAsync(id, userId);
                if (isOwner)
                {
                    return BadRequest(new { message = "Loop owner must transfer ownership before leaving" });
                }
                return NotFound(new { message = "Loop not found or you are not a member" });
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error leaving loop {LoopId}", id);
            return StatusCode(500, new { message = "An error occurred while leaving the loop" });
        }
    }
}

// Separate controller for join requests
[ApiController]
[Route("api/loops")]
[Authorize]
public class LoopJoinRequestsController : ControllerBase
{
    private readonly ILoopJoinRequestService _joinRequestService;
    private readonly ILoopService _loopService;
    private readonly IUserService _userService;
    private readonly ILogger<LoopJoinRequestsController> _logger;

    public LoopJoinRequestsController(
        ILoopJoinRequestService joinRequestService,
        ILoopService loopService,
        IUserService userService,
        ILogger<LoopJoinRequestsController> logger)
    {
        _joinRequestService = joinRequestService;
        _loopService = loopService;
        _userService = userService;
        _logger = logger;
    }

    private string GetUserId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
    }

    [HttpPost("{id:regex(^[[0-9a-fA-F]]{{24}}$)}/join-requests")]
    public async Task<IActionResult> CreateJoinRequest(string id, [FromBody] CreateJoinRequestRequest request)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        try
        {
            var loop = await _loopService.GetLoopByIdAsync(id);
            if (loop == null)
            {
                return NotFound(new { message = "Loop not found" });
            }

            if (!loop.IsPublic)
            {
                return StatusCode(403, new { message = "Cannot request to join a private loop" });
            }

            var joinRequest = await _joinRequestService.CreateJoinRequestAsync(id, userId, request.Message ?? string.Empty);
            return Ok(joinRequest);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating join request for loop {LoopId}", id);
            return StatusCode(500, new { message = "An error occurred while creating the join request" });
        }
    }

    [HttpGet("{id:regex(^[[0-9a-fA-F]]{{24}}$)}/join-requests")]
    public async Task<IActionResult> GetLoopJoinRequests(string id)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        try
        {
            var isOwner = await _loopService.IsLoopOwnerAsync(id, userId);
            if (!isOwner)
            {
                return StatusCode(403, new { message = "Only the loop owner can view join requests" });
            }

            var requests = await _joinRequestService.GetPendingJoinRequestsForLoopAsync(id);
            
            // Enrich with user information
            var enrichedRequests = new List<object>();
            foreach (var request in requests)
            {
                var user = await _userService.GetUserByIdAsync(request.UserId);
                enrichedRequests.Add(new
                {
                    request.Id,
                    request.LoopId,
                    request.UserId,
                    UserName = user != null ? $"{user.FirstName} {user.LastName}".Trim() : "Unknown",
                    UserEmail = user?.Email ?? "Unknown",
                    request.Message,
                    request.Status,
                    request.CreatedAt
                });
            }

            return Ok(enrichedRequests);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting join requests for loop {LoopId}", id);
            return StatusCode(500, new { message = "An error occurred while retrieving join requests" });
        }
    }

    [HttpPost("join-requests/{requestId:regex(^[[0-9a-fA-F]]{{24}}$)}/approve")]
    public async Task<IActionResult> ApproveJoinRequest(string requestId)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        try
        {
            var request = await _joinRequestService.ApproveJoinRequestAsync(requestId, userId);
            if (request == null)
            {
                return NotFound(new { message = "Join request not found or you are not authorized" });
            }

            return Ok(request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving join request {RequestId}", requestId);
            return StatusCode(500, new { message = "An error occurred while approving the join request" });
        }
    }

    [HttpPost("join-requests/{requestId:regex(^[[0-9a-fA-F]]{{24}}$)}/reject")]
    public async Task<IActionResult> RejectJoinRequest(string requestId)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        try
        {
            var request = await _joinRequestService.RejectJoinRequestAsync(requestId, userId);
            if (request == null)
            {
                return NotFound(new { message = "Join request not found or you are not authorized" });
            }

            return Ok(request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting join request {RequestId}", requestId);
            return StatusCode(500, new { message = "An error occurred while rejecting the join request" });
        }
    }

    [HttpGet("join-requests/my-requests")]
    public async Task<IActionResult> GetMyJoinRequests()
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        try
        {
            var requests = await _joinRequestService.GetUserJoinRequestsAsync(userId);
            
            // Enrich with loop information
            var enrichedRequests = new List<object>();
            foreach (var request in requests)
            {
                var loop = await _loopService.GetLoopByIdAsync(request.LoopId);
                enrichedRequests.Add(new
                {
                    request.Id,
                    request.LoopId,
                    LoopName = loop?.Name ?? "Unknown",
                    request.Message,
                    request.Status,
                    request.CreatedAt,
                    request.RespondedAt
                });
            }

            return Ok(enrichedRequests);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting join requests for user {UserId}", userId);
            return StatusCode(500, new { message = "An error occurred while retrieving your join requests" });
        }
    }
}

public class CreateLoopRequest
{
    public string Name { get; set; } = string.Empty;
}

public class InviteByEmailRequest
{
    public string Email { get; set; } = string.Empty;
}

public class InviteUserRequest
{
    public string UserId { get; set; } = string.Empty;
}

public class UpdateLoopSettingsRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsPublic { get; set; }
}

public class TransferOwnershipRequest
{
    public string NewOwnerId { get; set; } = string.Empty;
}

public class CreateJoinRequestRequest
{
    public string? Message { get; set; }
}
