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

    [HttpGet("{id}")]
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

    [HttpGet("{id}/members")]
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

    [HttpGet("{id}/items")]
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

    [HttpPost("{id}/invite-email")]
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

    [HttpPost("{id}/invite-user")]
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

    [HttpGet("{id}/potential-invitees")]
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
            var invitation = await _invitationService.AcceptInvitationAsync(token);
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

    [HttpGet("invitations/pending")]
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
