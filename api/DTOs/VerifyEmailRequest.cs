using System.ComponentModel.DataAnnotations;

namespace Api.DTOs;

public class VerifyEmailRequest
{
    [Required(ErrorMessage = "Verification token is required")]
    public string Token { get; set; } = string.Empty;
}