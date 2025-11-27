using Api.Models;

namespace Api.DTOs;

public class ItemSearchResult
{
    public List<SharedItem> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}
