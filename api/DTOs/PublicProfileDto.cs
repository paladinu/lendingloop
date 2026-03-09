namespace Api.DTOs;

public class PublicProfileDto
{
    public string UserId { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public int LoopScore { get; set; }
    public List<BadgeDto> Badges { get; set; } = new();
    public List<ScoreHistoryEntryDto> ScoreHistory { get; set; } = new();
}
