using Api.DTOs;
using Api.Models;
using Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]s")]
[Authorize]
public class ItemRequestController : ControllerBase
{
    private readonly IItemRequestService _itemRequestService;

    public ItemRequestController(IItemRequestService itemRequestService)
    {
        _itemRequestService = itemRequestService;
    }

    [HttpPost]
    public async Task<ActionResult<ItemRequest>> CreateRequest([FromBody] CreateItemRequestDto dto)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User ID not found in token");
            }

            if (string.IsNullOrWhiteSpace(dto.ItemId))
            {
                return BadRequest("Item ID is required");
            }

            if (!string.IsNullOrEmpty(dto.Message) && dto.Message.Length > 500)
            {
                return BadRequest("Message cannot exceed 500 characters");
            }

            if (dto.ExpectedReturnDate.HasValue && dto.ExpectedReturnDate.Value.Date < DateTime.UtcNow.Date)
            {
                return BadRequest("Expected return date cannot be in the past");
            }

            var request = await _itemRequestService.CreateRequestAsync(dto.ItemId, userId, dto.Message, dto.ExpectedReturnDate);
            return CreatedAtAction(nameof(GetRequestById), new { id = request.Id }, request);
        }
        catch (ArgumentException ex)
        {
            return NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    [HttpGet("my-requests")]
    public async Task<ActionResult<List<ItemRequest>>> GetMyRequests()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User ID not found in token");
            }

            var requests = await _itemRequestService.GetRequestsByRequesterAsync(userId);
            return Ok(requests);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    [HttpGet("pending")]
    public async Task<ActionResult<List<ItemRequest>>> GetPendingRequests()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User ID not found in token");
            }

            var requests = await _itemRequestService.GetPendingRequestsByOwnerAsync(userId);
            return Ok(requests);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    [HttpGet("item/{itemId}")]
    public async Task<ActionResult<List<ItemRequest>>> GetRequestsForItem(string itemId)
    {
        try
        {
            var requests = await _itemRequestService.GetRequestsByItemIdAsync(itemId);
            return Ok(requests);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ItemRequest>> GetRequestById(string id)
    {
        try
        {
            var request = await _itemRequestService.GetRequestByIdAsync(id);
            if (request == null)
            {
                return NotFound($"Request with id {id} not found");
            }

            return Ok(request);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    [HttpPut("{id}/approve")]
    public async Task<ActionResult<ItemRequest>> ApproveRequest(string id)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User ID not found in token");
            }

            var request = await _itemRequestService.ApproveRequestAsync(id, userId);
            if (request == null)
            {
                return NotFound($"Request with id {id} not found");
            }

            return Ok(request);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    [HttpPut("{id}/reject")]
    public async Task<ActionResult<ItemRequest>> RejectRequest(string id)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User ID not found in token");
            }

            var request = await _itemRequestService.RejectRequestAsync(id, userId);
            if (request == null)
            {
                return NotFound($"Request with id {id} not found");
            }

            return Ok(request);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    [HttpPut("{id}/cancel")]
    public async Task<ActionResult<ItemRequest>> CancelRequest(string id)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User ID not found in token");
            }

            var request = await _itemRequestService.CancelRequestAsync(id, userId);
            if (request == null)
            {
                return NotFound($"Request with id {id} not found");
            }

            return Ok(request);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    [HttpPut("{id}/complete")]
    public async Task<ActionResult<ItemRequest>> CompleteRequest(string id)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User ID not found in token");
            }

            var request = await _itemRequestService.CompleteRequestAsync(id, userId);
            if (request == null)
            {
                return NotFound($"Request with id {id} not found");
            }

            return Ok(request);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }
}
