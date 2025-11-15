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
    
    [BsonElement("loopScore")]
    public int LoopScore { get; set; } = 0;
    
    [BsonElement("scoreHistory")]
    public List<ScoreHistoryEntry> ScoreHistory { get; set; } = new();
    
    [BsonElement("badges")]
    public List<BadgeAward> Badges { get; set; } = new();
    
    [BsonElement("consecutiveOnTimeReturns")]
    public int ConsecutiveOnTimeReturns { get; set; } = 0;
    
    [BsonElement("invitedBy")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? InvitedBy { get; set; }
}

public class BadgeAward
{
    [BsonElement("badgeType")]
    [BsonRepresentation(BsonType.String)]
    public BadgeType BadgeType { get; set; }
    
    [BsonElement("awardedAt")]
    public DateTime AwardedAt { get; set; }
}

public enum BadgeType
{
    // Milestone badges
    Bronze,   // 10 points
    Silver,   // 50 points
    Gold,     // 100 points
    
    // Achievement badges
    FirstLend,           // First lending transaction
    ReliableBorrower,    // 10 on-time returns
    GenerousLender,      // 50 completed lending transactions
    PerfectRecord,       // 25 consecutive on-time returns
    CommunityBuilder     // 10 active invited users
}

public class ScoreHistoryEntry
{
    [BsonElement("timestamp")]
    public DateTime Timestamp { get; set; }
    
    [BsonElement("points")]
    public int Points { get; set; }
    
    [BsonElement("actionType")]
    [BsonRepresentation(BsonType.String)]
    public ScoreActionType ActionType { get; set; }
    
    [BsonElement("itemRequestId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string ItemRequestId { get; set; } = string.Empty;
    
    [BsonElement("itemName")]
    public string ItemName { get; set; } = string.Empty;
}

public enum ScoreActionType
{
    BorrowCompleted,
    OnTimeReturn,
    LendApproved,
    LendCancelled
}