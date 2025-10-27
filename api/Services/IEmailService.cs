using Api.Models;

namespace Api.Services;

public interface IEmailService
{
    Task<bool> SendVerificationEmailAsync(User user, string verificationToken);
    Task<bool> SendPasswordResetEmailAsync(User user, string resetToken);
    Task<bool> TestEmailConfigurationAsync();
    EmailHealthStatus GetEmailHealthStatus();
}