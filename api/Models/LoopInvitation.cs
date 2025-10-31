using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Api.Models;

public class LoopInvitation
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }
    
    [BsonElement("loopId")]
    public string LoopId { get; set; } = string.Empty;
    
    [BsonElement("invitedByUserId")]
    public string InvitedByUserId { get; set; } = string.Empty;
    
    [BsonElement("invitedEmail")]
    public string InvitedEmail { get; set; } = string.Empty;
    
    [BsonElement("invitedUserId")]
    public string? InvitedUserId { get; set; }
    
    [BsonElement("invitationToken")]
    public string InvitationToken { get; set; } = string.Empty;
    
    [BsonElement("status")]
    public InvitationStatus Status { get; set; } = InvitationStatus.Pending;
    
    [BsonElement("expiresAt")]
    public DateTime ExpiresAt { get; set; }
    
    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    [BsonElement("acceptedAt")]
    public DateTime? AcceptedAt { get; set; }
}

public enum InvitationStatus
{
    Pending,
    Accepted,
    Expired,
    Declined
}
