namespace Api.DTOs;

public class CreateItemRequestDto
{
    public string ItemId { get; set; } = string.Empty;
    public string? Message { get; set; }
}
