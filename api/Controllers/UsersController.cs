using Api.Models;
using Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
}
