namespace Api.DTOs;

public class AuthResponse
{
    public string Token { get; set; } = string.Empty;
    public UserProfile User { get; set; } = new();
    public DateTime ExpiresAt { get; set; }
}