using Api.Models;

namespace Api.DTOs;

public class LoopInvitationResponse
{
    public string? Id { get; set; }
    public string LoopId { get; set; } = string.Empty;
    public string? LoopName { get; set; }
    public string InvitedByUserId { get; set; } = string.Empty;
    public string? InvitedByUserName { get; set; }
    public int? InvitedByUserLoopScore { get; set; }
    public string InvitedEmail { get; set; } = string.Empty;
    public string? InvitedUserId { get; set; }
    public string InvitationToken { get; set; } = string.Empty;
    public InvitationStatus Status { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? AcceptedAt { get; set; }
}
