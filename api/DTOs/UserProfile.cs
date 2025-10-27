namespace Api.DTOs;

public class UserProfile
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string StreetAddress { get; set; } = string.Empty;
    public bool IsEmailVerified { get; set; }
}