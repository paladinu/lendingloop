using MongoDB.Bson.Serialization.Attributes;

namespace Api.Models;

public class OwnershipTransfer
{
    [BsonElement("fromUserId")]
    public string FromUserId { get; set; } = string.Empty;
    
    [BsonElement("toUserId")]
    public string ToUserId { get; set; } = string.Empty;
    
    [BsonElement("transferredAt")]
    public DateTime TransferredAt { get; set; } = DateTime.UtcNow;
    
    [BsonElement("status")]
    public TransferStatus Status { get; set; } = TransferStatus.Pending;
}

public enum TransferStatus
{
    Pending,
    Accepted,
    Declined,
    Cancelled
}
