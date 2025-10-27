namespace Api.DTOs;

public class VerificationResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public UserProfile? User { get; set; }
}