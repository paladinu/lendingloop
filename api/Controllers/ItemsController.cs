using Api.Models;
using Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
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
            var items = await _itemsService.GetAllItemsAsync();
            return Ok(items);
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

            // Update item with image URL
            var imageUrl = $"/{uploadPath}/{uniqueFileName}";
            var updatedItem = await _itemsService.UpdateItemImageAsync(id, imageUrl);

            if (updatedItem == null)
            {
                // Clean up uploaded file if item not found
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
                return NotFound($"Item with id {id} not found.");
            }

            return Ok(updatedItem);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }
}