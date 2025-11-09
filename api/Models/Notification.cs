using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Api.Models;

public class Notification
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }
    
    [BsonElement("userId")]
    public string UserId { get; set; } = string.Empty;
    
    [BsonElement("type")]
    public NotificationType Type { get; set; }
    
    [BsonElement("message")]
    public string Message { get; set; } = string.Empty;
    
    [BsonElement("itemId")]
    public string? ItemId { get; set; }
    
    [BsonElement("itemRequestId")]
    public string? ItemRequestId { get; set; }
    
    [BsonElement("relatedUserId")]
    public string? RelatedUserId { get; set; }
    
    [BsonElement("isRead")]
    public bool IsRead { get; set; } = false;
    
    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public enum NotificationType
{
    ItemRequestCreated,
    ItemRequestApproved,
    ItemRequestRejected,
    ItemRequestCompleted,
    ItemRequestCancelled
}
