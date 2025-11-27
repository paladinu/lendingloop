using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Api.Models;

public class SystemTag
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }
    
    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;
    
    [BsonElement("displayName")]
    public string DisplayName { get; set; } = string.Empty;
    
    [BsonElement("category")]
    public string Category { get; set; } = string.Empty;
    
    [BsonElement("usageCount")]
    public int UsageCount { get; set; } = 0;
    
    [BsonElement("isActive")]
    public bool IsActive { get; set; } = true;
    
    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
