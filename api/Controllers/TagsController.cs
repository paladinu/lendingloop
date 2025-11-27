using Api.Models;
using Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TagsController : ControllerBase
{
    private readonly ITagsService _tagsService;
    private readonly ILogger<TagsController> _logger;

    public TagsController(ITagsService tagsService, ILogger<TagsController> logger)
    {
        _tagsService = tagsService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<List<SystemTag>>> GetAllTags()
    {
        try
        {
            var tags = await _tagsService.GetAllActiveTagsAsync();
            return Ok(tags);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving system tags");
            return StatusCode(500, new { message = "An error occurred while retrieving tags" });
        }
    }

    [HttpGet("statistics")]
    [Authorize]
    public async Task<ActionResult<Dictionary<string, int>>> GetTagStatistics()
    {
        try
        {
            var statistics = await _tagsService.GetTagUsageStatisticsAsync();
            return Ok(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving tag statistics");
            return StatusCode(500, new { message = "An error occurred while retrieving tag statistics" });
        }
    }
}
