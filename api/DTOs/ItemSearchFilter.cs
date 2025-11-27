namespace Api.DTOs;

public class ItemSearchFilter
{
    public string? SearchText { get; set; }
    public List<string> Tags { get; set; } = new();
    public bool? IsAvailable { get; set; }
    public List<string> OwnerIds { get; set; } = new();
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}
