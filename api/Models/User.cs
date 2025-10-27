using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Api.Models;

public class User
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }
    
    [BsonElement("email")]
    public string Email { get; set; } = string.Empty;
    
    [BsonElement("passwordHash")]
    public string PasswordHash { get; set; } = string.Empty;
    
    [BsonElement("firstName")]
    public string FirstName { get; set; } = string.Empty;
    
    [BsonElement("lastName")]
    public string LastName { get; set; } = string.Empty;
    
    [BsonElement("streetAddress")]
    public string StreetAddress { get; set; } = string.Empty;
    
    [BsonElement("isEmailVerified")]
    public bool IsEmailVerified { get; set; } = false;
    
    [BsonElement("emailVerificationToken")]
    public string? EmailVerificationToken { get; set; }
    
    [BsonElement("emailVerificationExpiry")]
    public DateTime? EmailVerificationExpiry { get; set; }
    
    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    [BsonElement("lastLoginAt")]
    public DateTime? LastLoginAt { get; set; }
}