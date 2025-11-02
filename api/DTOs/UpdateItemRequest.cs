namespace Api.DTOs;

public class UpdateItemRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsAvailable { get; set; }
    public List<string> VisibleToLoopIds { get; set; } = new();
    public bool VisibleToAllLoops { get; set; }
    public bool VisibleToFutureLoops { get; set; }
}
