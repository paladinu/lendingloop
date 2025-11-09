using Api.Models;

namespace Api.Services;

public interface IEmailService
{
    Task<bool> SendVerificationEmailAsync(User user, string verificationToken);
    Task<bool> SendPasswordResetEmailAsync(User user, string resetToken);
    Task<bool> SendLoopInvitationEmailAsync(string recipientEmail, string recipientName, string inviterName, string loopName, string invitationToken);
    Task<bool> SendItemRequestCreatedEmailAsync(string ownerEmail, string ownerName, string requesterName, string itemName, string requestId);
    Task<bool> SendItemRequestApprovedEmailAsync(string requesterEmail, string requesterName, string ownerName, string itemName);
    Task<bool> SendItemRequestRejectedEmailAsync(string requesterEmail, string requesterName, string ownerName, string itemName);
    Task<bool> SendItemRequestCompletedEmailAsync(string requesterEmail, string requesterName, string ownerName, string itemName);
    Task<bool> SendItemRequestCancelledEmailAsync(string ownerEmail, string ownerName, string requesterName, string itemName);
    Task<bool> TestEmailConfigurationAsync();
    EmailHealthStatus GetEmailHealthStatus();
}