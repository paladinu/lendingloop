using Api.DTOs;
using Api.Models;
using Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly ILoopScoreService _loopScoreService;
    private readonly IUserService _userService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(
        ILoopScoreService loopScoreService,
        IUserService userService,
        ILogger<UsersController> logger)
    {
        _loopScoreService = loopScoreService;
        _userService = userService;
        _logger = logger;
    }

    [HttpGet("{userId}/score")]
    public async Task<ActionResult<int>> GetUserScore(string userId)
    {
        try
        {
            // Verify user exists
            var user = await _userService.GetUserByIdAsync(userId);
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            var score = await _loopScoreService.GetUserScoreAsync(userId);
            return Ok(score);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving score for user {UserId}", userId);
            return StatusCode(500, new { message = "An error occurred while retrieving the user score" });
        }
    }

    [HttpGet("{userId}/score-history")]
    public async Task<ActionResult<List<ScoreHistoryEntry>>> GetScoreHistory(string userId, [FromQuery] int limit = 50)
    {
        try
        {
            // Verify user exists
            var user = await _userService.GetUserByIdAsync(userId);
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            var history = await _loopScoreService.GetScoreHistoryAsync(userId, limit);
            return Ok(history);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving score history for user {UserId}", userId);
            return StatusCode(500, new { message = "An error occurred while retrieving the score history" });
        }
    }

    [HttpGet("{userId}/badges")]
    public async Task<ActionResult<List<BadgeAward>>> GetUserBadges(string userId)
    {
        try
        {
            // Verify user exists
            var user = await _userService.GetUserByIdAsync(userId);
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            var badges = await _loopScoreService.GetUserBadgesAsync(userId);
            return Ok(badges);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving badges for user {UserId}", userId);
            return StatusCode(500, new { message = "An error occurred while retrieving the user badges" });
        }
    }

    [HttpGet("{userId}/badge-progress")]
    public async Task<ActionResult<Dictionary<BadgeType, BadgeProgress>>> GetBadgeProgress(string userId)
    {
        try
        {
            // Verify user exists
            var user = await _userService.GetUserByIdAsync(userId);
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            var progress = await _loopScoreService.GetAllBadgeProgressAsync(userId);
            return Ok(progress);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving badge progress for user {UserId}", userId);
            return StatusCode(500, new { message = "An error occurred while retrieving the badge progress" });
        }
    }

    [HttpGet("badges/rarity")]
    public async Task<ActionResult<Dictionary<BadgeType, BadgeRarity>>> GetBadgeRarities()
    {
        try
        {
            var rarities = await _loopScoreService.GetAllBadgeRaritiesAsync();
            return Ok(rarities);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving badge rarities");
            return StatusCode(500, new { message = "An error occurred while retrieving badge rarities" });
        }
    }

    [HttpGet("{userId}/public-profile")]
    public async Task<ActionResult<PublicProfileDto>> GetPublicProfile(string userId)
    {
        try
        {
            // Get requesting user ID from JWT claims
            var requestingUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            if (string.IsNullOrEmpty(requestingUserId))
            {
                return Unauthorized(new { message = "User not authenticated" });
            }

            // Call usersService.GetPublicProfileAsync
            var publicProfile = await _userService.GetPublicProfileAsync(requestingUserId, userId);
            
            return Ok(publicProfile);
        }
        catch (KeyNotFoundException ex)
        {
            // Handle NotFoundException → return 404
            _logger.LogWarning(ex, "User not found: {UserId}", userId);
            return NotFound(new { message = "User not found" });
        }
        catch (UnauthorizedAccessException ex)
        {
            // Handle UnauthorizedException → return 403
            _logger.LogWarning(ex, "Unauthorized access to profile: {UserId}", userId);
            return StatusCode(403, new { message = "You can only view profiles of users in your loops" });
        }
        catch (InvalidOperationException ex)
        {
            // Handle BadRequestException for viewing own profile
            _logger.LogWarning(ex, "Invalid operation: {Message}", ex.Message);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            // Handle other exceptions → return 500
            _logger.LogError(ex, "Error retrieving public profile for user {UserId}", userId);
            return StatusCode(500, new { message = "An error occurred while retrieving the public profile" });
        }
    }
}
