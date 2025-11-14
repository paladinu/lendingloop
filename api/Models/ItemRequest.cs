using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Api.Models;

public class ItemRequest
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }
    
    [BsonElement("itemId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string ItemId { get; set; } = string.Empty;
    
    [BsonElement("requesterId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string RequesterId { get; set; } = string.Empty;
    
    [BsonElement("ownerId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string OwnerId { get; set; } = string.Empty;
    
    [BsonElement("status")]
    [BsonRepresentation(BsonType.String)]
    public RequestStatus Status { get; set; } = RequestStatus.Pending;
    
    [BsonElement("message")]
    public string? Message { get; set; }
    
    [BsonElement("requestedAt")]
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    
    [BsonElement("respondedAt")]
    public DateTime? RespondedAt { get; set; }
    
    [BsonElement("completedAt")]
    public DateTime? CompletedAt { get; set; }
}

public enum RequestStatus
{
    Pending,
    Approved,
    Rejected,
    Cancelled,
    Completed
}
