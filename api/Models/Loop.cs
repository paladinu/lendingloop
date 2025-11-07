using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Api.Models;

public class Loop
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }
    
    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;
    
    [BsonElement("description")]
    public string Description { get; set; } = string.Empty;
    
    [BsonElement("creatorId")]
    public string CreatorId { get; set; } = string.Empty;
    
    [BsonElement("memberIds")]
    public List<string> MemberIds { get; set; } = new();
    
    [BsonElement("isPublic")]
    public bool IsPublic { get; set; } = false;
    
    [BsonElement("isArchived")]
    public bool IsArchived { get; set; } = false;
    
    [BsonElement("archivedAt")]
    public DateTime? ArchivedAt { get; set; }
    
    [BsonElement("ownershipHistory")]
    public List<OwnershipTransfer> OwnershipHistory { get; set; } = new();
    
    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
