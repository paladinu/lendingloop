using Api.Models;

namespace Api.Services;

public interface IEmailService
{
    Task<bool> SendVerificationEmailAsync(User user, string verificationToken);
    Task<bool> SendPasswordResetEmailAsync(User user, string resetToken);
    Task<bool> SendLoopInvitationEmailAsync(string recipientEmail, string recipientName, string inviterName, string loopName, string invitationToken);
    Task<bool> TestEmailConfigurationAsync();
    EmailHealthStatus GetEmailHealthStatus();
}