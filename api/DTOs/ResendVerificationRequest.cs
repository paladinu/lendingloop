using System.ComponentModel.DataAnnotations;

namespace Api.DTOs;

public class ResendVerificationRequest
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address")]
    public string Email { get; set; } = string.Empty;
}