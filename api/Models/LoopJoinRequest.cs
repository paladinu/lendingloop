using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Api.Models;

public class LoopJoinRequest
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }
    
    [BsonElement("loopId")]
    public string LoopId { get; set; } = string.Empty;
    
    [BsonElement("userId")]
    public string UserId { get; set; } = string.Empty;
    
    [BsonElement("message")]
    public string Message { get; set; } = string.Empty;
    
    [BsonElement("status")]
    public JoinRequestStatus Status { get; set; } = JoinRequestStatus.Pending;
    
    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    [BsonElement("respondedAt")]
    public DateTime? RespondedAt { get; set; }
}

public enum JoinRequestStatus
{
    Pending,
    Approved,
    Rejected
}
