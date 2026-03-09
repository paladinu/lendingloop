using Api.Models;

namespace Api.DTOs;

public class ScoreHistoryEntryDto
{
    public DateTime Timestamp { get; set; }
    public int Points { get; set; }
    public string ActionType { get; set; } = string.Empty;
    public string ItemRequestId { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    
    public static ScoreHistoryEntryDto FromScoreHistoryEntry(ScoreHistoryEntry entry)
    {
        return new ScoreHistoryEntryDto
        {
            Timestamp = entry.Timestamp,
            Points = entry.Points,
            ActionType = entry.ActionType.ToString(),
            ItemRequestId = entry.ItemRequestId,
            ItemName = entry.ItemName
        };
    }
}
