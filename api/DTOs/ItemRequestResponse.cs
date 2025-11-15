using Api.Models;

namespace Api.DTOs;

public class ItemRequestResponse
{
    public string? Id { get; set; }
    public string ItemId { get; set; } = string.Empty;
    public string RequesterId { get; set; } = string.Empty;
    public string OwnerId { get; set; } = string.Empty;
    public RequestStatus Status { get; set; }
    public string? Message { get; set; }
    public DateTime? ExpectedReturnDate { get; set; }
    public DateTime RequestedAt { get; set; }
    public DateTime? RespondedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    
    // Populated fields for display
    public string? ItemName { get; set; }
    public string? RequesterName { get; set; }
    public int? RequesterLoopScore { get; set; }
    public string? OwnerName { get; set; }
    public int? OwnerLoopScore { get; set; }
}
