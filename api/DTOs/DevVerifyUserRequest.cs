using System.ComponentModel.DataAnnotations;

namespace Api.DTOs;

public class DevVerifyUserRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}
