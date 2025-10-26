using Api.Models;
using Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ItemsController : ControllerBase
{
    private readonly IItemsService _itemsService;

    public ItemsController(IItemsService itemsService)
    {
        _itemsService = itemsService;
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
}