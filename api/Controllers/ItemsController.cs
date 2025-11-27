using Api.DTOs;
using Api.Models;
using Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ItemsController : ControllerBase
{
    private readonly IItemsService _itemsService;
    private readonly IWebHostEnvironment _environment;
    private readonly IConfiguration _configuration;

    public ItemsController(IItemsService itemsService, IWebHostEnvironment environment, IConfiguration configuration)
    {
        _itemsService = itemsService;
        _environment = environment;
        _configuration = configuration;
    }

    [HttpGet]
    public async Task<ActionResult<List<SharedItem>>> GetAllItems()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User ID not found in token");
            }

            var items = await _itemsService.GetItemsByUserIdAsync(userId);
            return Ok(items);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<SharedItem>> GetItemById(string id)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User ID not found in token");
            }

            var item = await _itemsService.GetItemByIdAsync(id);
            if (item == null)
            {
                return NotFound($"Item with id {id} not found.");
            }

            // Only allow users to view their own items
            if (item.UserId != userId)
            {
                return Forbid();
            }

            return Ok(item);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    [HttpPost]
    public async Task<ActionResult<SharedItem>> CreateItem([FromBody] SharedItem item)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(item.Name))
            {
                return BadRequest("Item name is required.");
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User ID not found in token");
            }

            // Set the userId from the authenticated user
            item.UserId = userId;

            var createdItem = await _itemsService.CreateItemAsync(item);
            return CreatedAtAction(nameof(GetAllItems), new { id = createdItem.Id }, createdItem);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    [HttpPost("{id}/image")]
    public async Task<ActionResult<SharedItem>> UploadItemImage(string id, IFormFile file)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User ID not found in token");
            }

            // Validate file exists
            if (file == null || file.Length == 0)
            {
                return BadRequest("No file uploaded.");
            }

            // Validate file type
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(fileExtension))
            {
                return BadRequest("Invalid file type. Only jpg, jpeg, png, and gif are allowed.");
            }

            // Validate file size
            var maxFileSizeBytes = _configuration.GetValue<long>("FileStorage:MaxFileSizeBytes", 5242880);
            if (file.Length > maxFileSizeBytes)
            {
                return BadRequest($"File size exceeds maximum allowed size of {maxFileSizeBytes / 1024 / 1024}MB.");
            }

            // Create upload directory if it doesn't exist
            var uploadPath = _configuration["FileStorage:UploadPath"] ?? "uploads/images";
            var fullUploadPath = Path.Combine(_environment.ContentRootPath, uploadPath);
            Directory.CreateDirectory(fullUploadPath);

            // Generate unique filename
            var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
            var filePath = Path.Combine(fullUploadPath, uniqueFileName);

            // Save file
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Update item with image URL (store as relative path) - only allow users to update their own items
            var imageUrl = $"/{uploadPath}/{uniqueFileName}";
            var updatedItem = await _itemsService.UpdateItemImageAsync(id, imageUrl, userId);

            if (updatedItem == null)
            {
                // Clean up uploaded file if item not found or user doesn't own it
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
                return NotFound($"Item with id {id} not found or you don't have permission to update it.");
            }

            return Ok(updatedItem);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<SharedItem>> UpdateItem(string id, [FromBody] UpdateItemRequest request)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User ID not found in token");
            }

            // Validate name is not empty
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return BadRequest("Item name is required.");
            }

            // Validate that the item exists and belongs to the user
            var item = await _itemsService.GetItemByIdAsync(id);
            if (item == null)
            {
                return NotFound($"Item with id {id} not found.");
            }

            if (item.UserId != userId)
            {
                return Forbid();
            }

            // Update item
            var updatedItem = await _itemsService.UpdateItemAsync(
                id,
                userId,
                request.Name,
                request.Description,
                request.IsAvailable,
                request.VisibleToLoopIds,
                request.VisibleToAllLoops,
                request.VisibleToFutureLoops,
                request.Tags
            );

            if (updatedItem == null)
            {
                return NotFound($"Item with id {id} not found or you don't have permission to update it.");
            }

            return Ok(updatedItem);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    [HttpPut("{id}/visibility")]
    public async Task<ActionResult<SharedItem>> UpdateItemVisibility(string id, [FromBody] UpdateItemVisibilityRequest request)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User ID not found in token");
            }

            // Validate that the item exists and belongs to the user
            var item = await _itemsService.GetItemByIdAsync(id);
            if (item == null)
            {
                return NotFound($"Item with id {id} not found.");
            }

            if (item.UserId != userId)
            {
                return Forbid();
            }

            // Update visibility settings
            var updatedItem = await _itemsService.UpdateItemVisibilityAsync(
                id,
                userId,
                request.VisibleToLoopIds ?? new List<string>(),
                request.VisibleToAllLoops,
                request.VisibleToFutureLoops
            );

            if (updatedItem == null)
            {
                return NotFound($"Item with id {id} not found or you don't have permission to update it.");
            }

            return Ok(updatedItem);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    [HttpPost("search/{loopId}")]
    public async Task<ActionResult<ItemSearchResult>> SearchItems(string loopId, [FromBody] ItemSearchFilter filter)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User ID not found in token");
            }

            // Validate pagination parameters
            if (filter.PageNumber < 1)
            {
                return BadRequest("Invalid pagination parameters: PageNumber must be at least 1");
            }

            if (filter.PageSize < 1 || filter.PageSize > 100)
            {
                return BadRequest("Invalid pagination parameters: PageSize must be between 1 and 100");
            }

            // TODO: Verify user belongs to the loop (requires ILoopService)
            // For now, we'll allow the search and let the service filter by loop visibility

            var result = await _itemsService.SearchItemsAsync(loopId, filter);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    [HttpGet("loop/{loopId}/owners")]
    public async Task<ActionResult<List<string>>> GetDistinctOwners(string loopId)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User ID not found in token");
            }

            // TODO: Verify user belongs to the loop (requires ILoopService)
            // For now, we'll allow the query

            var ownerIds = await _itemsService.GetDistinctOwnersInLoopAsync(loopId);
            return Ok(ownerIds);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }
}

public class UpdateItemVisibilityRequest
{
    public List<string>? VisibleToLoopIds { get; set; }
    public bool VisibleToAllLoops { get; set; }
    public bool VisibleToFutureLoops { get; set; }
}