using Api.Models;
using System.Net;
using System.Net.Mail;
using System.Text;

namespace Api.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;
    private readonly EmailConfiguration _emailConfig;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _emailConfig = LoadEmailConfiguration();
        
        // Validate configuration on startup
        ValidateConfiguration();
    }

    private EmailConfiguration LoadEmailConfiguration()
    {
        return new EmailConfiguration
        {
            SmtpHost = _configuration["Email:SmtpHost"] ?? "localhost",
            SmtpPort = int.Parse(_configuration["Email:SmtpPort"] ?? "587"),
            SmtpUsername = _configuration["Email:SmtpUsername"] ?? "",
            SmtpPassword = _configuration["Email:SmtpPassword"] ?? "",
            FromEmail = _configuration["Email:FromEmail"] ?? "noreply@example.com",
            FromName = _configuration["Email:FromName"] ?? "Shared Items App",
            BaseUrl = _configuration["Email:BaseUrl"] ?? "http://localhost:4200",
            EnableSsl = bool.Parse(_configuration["Email:EnableSsl"] ?? "true"),
            UseDefaultCredentials = bool.Parse(_configuration["Email:UseDefaultCredentials"] ?? "false"),
            TimeoutSeconds = int.Parse(_configuration["Email:TimeoutSeconds"] ?? "30"),
            MaxRetryAttempts = int.Parse(_configuration["Email:MaxRetryAttempts"] ?? "3"),
            RetryDelaySeconds = int.Parse(_configuration["Email:RetryDelaySeconds"] ?? "5"),
            TestMode = bool.Parse(_configuration["Email:TestMode"] ?? "false"),
            TestEmailAddress = _configuration["Email:TestEmailAddress"] ?? "test@example.com"
        };
    }

    private void ValidateConfiguration()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(_emailConfig.SmtpHost))
            errors.Add("Email:SmtpHost is required");

        if (_emailConfig.SmtpPort <= 0 || _emailConfig.SmtpPort > 65535)
            errors.Add("Email:SmtpPort must be between 1 and 65535");

        if (string.IsNullOrWhiteSpace(_emailConfig.FromEmail))
            errors.Add("Email:FromEmail is required");

        if (!IsValidEmail(_emailConfig.FromEmail))
            errors.Add("Email:FromEmail must be a valid email address");

        if (string.IsNullOrWhiteSpace(_emailConfig.BaseUrl))
            errors.Add("Email:BaseUrl is required");

        if (!Uri.TryCreate(_emailConfig.BaseUrl, UriKind.Absolute, out _))
            errors.Add("Email:BaseUrl must be a valid URL");

        if (_emailConfig.TimeoutSeconds <= 0)
            errors.Add("Email:TimeoutSeconds must be greater than 0");

        if (_emailConfig.MaxRetryAttempts < 0)
            errors.Add("Email:MaxRetryAttempts must be 0 or greater");

        if (_emailConfig.RetryDelaySeconds < 0)
            errors.Add("Email:RetryDelaySeconds must be 0 or greater");

        if (errors.Any())
        {
            var errorMessage = "Email service configuration errors: " + string.Join(", ", errors);
            _logger.LogError(errorMessage);
            throw new InvalidOperationException(errorMessage);
        }

        _logger.LogInformation("Email service configuration validated successfully");
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> SendVerificationEmailAsync(User user, string verificationToken)
    {
        try
        {
            if (user == null)
            {
                _logger.LogError("Cannot send verification email: user is null");
                return false;
            }

            if (string.IsNullOrWhiteSpace(user.Email))
            {
                _logger.LogError("Cannot send verification email: user email is null or empty");
                return false;
            }

            if (string.IsNullOrWhiteSpace(verificationToken))
            {
                _logger.LogError("Cannot send verification email: verification token is null or empty");
                return false;
            }

            var verificationUrl = $"{_emailConfig.BaseUrl}/verify-email?token={verificationToken}";
            var subject = "Verify Your Email Address";
            var body = GenerateVerificationEmailBody(user.FirstName, verificationUrl);

            return await SendEmailWithRetryAsync(user.Email, subject, body, "verification");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send verification email to {Email}", user.Email);
            return false;
        }
    }

    public async Task<bool> SendPasswordResetEmailAsync(User user, string resetToken)
    {
        try
        {
            if (user == null)
            {
                _logger.LogError("Cannot send password reset email: user is null");
                return false;
            }

            if (string.IsNullOrWhiteSpace(user.Email))
            {
                _logger.LogError("Cannot send password reset email: user email is null or empty");
                return false;
            }

            if (string.IsNullOrWhiteSpace(resetToken))
            {
                _logger.LogError("Cannot send password reset email: reset token is null or empty");
                return false;
            }

            var resetUrl = $"{_emailConfig.BaseUrl}/reset-password?token={resetToken}";
            var subject = "Reset Your Password";
            var body = GeneratePasswordResetEmailBody(user.FirstName, resetUrl);

            return await SendEmailWithRetryAsync(user.Email, subject, body, "password reset");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send password reset email to {Email}", user.Email);
            return false;
        }
    }

    private async Task<bool> SendEmailWithRetryAsync(string toEmail, string subject, string body, string emailType)
    {
        for (int attempt = 1; attempt <= _emailConfig.MaxRetryAttempts + 1; attempt++)
        {
            try
            {
                var success = await SendEmailAsync(toEmail, subject, body);
                if (success)
                {
                    if (attempt > 1)
                    {
                        _logger.LogInformation("Successfully sent {EmailType} email to {Email} on attempt {Attempt}", 
                            emailType, toEmail, attempt);
                    }
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Attempt {Attempt} failed to send {EmailType} email to {Email}", 
                    attempt, emailType, toEmail);
                
                if (attempt <= _emailConfig.MaxRetryAttempts)
                {
                    _logger.LogInformation("Retrying in {DelaySeconds} seconds...", _emailConfig.RetryDelaySeconds);
                    await Task.Delay(TimeSpan.FromSeconds(_emailConfig.RetryDelaySeconds));
                }
            }
        }

        _logger.LogError("Failed to send {EmailType} email to {Email} after {MaxAttempts} attempts", 
            emailType, toEmail, _emailConfig.MaxRetryAttempts + 1);
        return false;
    }

    private async Task<bool> SendEmailAsync(string toEmail, string subject, string body)
    {
        try
        {
            // Validate email address
            if (!IsValidEmail(toEmail))
            {
                _logger.LogError("Invalid email address: {Email}", toEmail);
                return false;
            }

            // Handle test mode
            if (_emailConfig.TestMode)
            {
                _logger.LogInformation("TEST MODE: Email would be sent to {Email} with subject: {Subject}", toEmail, subject);
                _logger.LogDebug("Email body: {Body}", body);
                return true;
            }

            // Check if SMTP is configured
            if (string.IsNullOrEmpty(_emailConfig.SmtpUsername) || string.IsNullOrEmpty(_emailConfig.SmtpPassword))
            {
                _logger.LogWarning("SMTP credentials not configured. Email would be sent to {Email} with subject: {Subject}", toEmail, subject);
                _logger.LogDebug("Email body: {Body}", body);
                return true; // Return true for development/testing purposes
            }

            using var client = new SmtpClient(_emailConfig.SmtpHost, _emailConfig.SmtpPort);
            client.EnableSsl = _emailConfig.EnableSsl;
            client.UseDefaultCredentials = _emailConfig.UseDefaultCredentials;
            client.Credentials = new NetworkCredential(_emailConfig.SmtpUsername, _emailConfig.SmtpPassword);
            client.Timeout = _emailConfig.TimeoutSeconds * 1000; // Convert to milliseconds

            using var message = new MailMessage();
            message.From = new MailAddress(_emailConfig.FromEmail, _emailConfig.FromName);
            message.To.Add(toEmail);
            message.Subject = subject;
            message.Body = body;
            message.IsBodyHtml = true;
            message.BodyEncoding = Encoding.UTF8;

            await client.SendMailAsync(message);
            _logger.LogInformation("Email sent successfully to {Email}", toEmail);
            return true;
        }
        catch (SmtpException ex)
        {
            _logger.LogError(ex, "SMTP error sending email to {Email}: {StatusCode}", toEmail, ex.StatusCode);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error sending email to {Email}", toEmail);
            return false;
        }
    }

    public async Task<bool> SendLoopInvitationEmailAsync(string recipientEmail, string recipientName, string inviterName, string loopName, string invitationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(recipientEmail))
            {
                _logger.LogError("Cannot send loop invitation email: recipient email is null or empty");
                return false;
            }

            if (string.IsNullOrWhiteSpace(invitationToken))
            {
                _logger.LogError("Cannot send loop invitation email: invitation token is null or empty");
                return false;
            }

            var invitationUrl = $"{_emailConfig.BaseUrl}/loops/accept-invitation?token={invitationToken}";
            var subject = $"You're invited to join {loopName}";
            var body = GenerateLoopInvitationEmailBody(recipientName, inviterName, loopName, invitationUrl);

            return await SendEmailWithRetryAsync(recipientEmail, subject, body, "loop invitation");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send loop invitation email to {Email}", recipientEmail);
            return false;
        }
    }

    public async Task<bool> SendItemRequestCreatedEmailAsync(string ownerEmail, string ownerName, string requesterName, string itemName, string requestId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(ownerEmail))
            {
                _logger.LogError("Cannot send item request created email: owner email is null or empty");
                return false;
            }

            if (string.IsNullOrWhiteSpace(itemName))
            {
                _logger.LogError("Cannot send item request created email: item name is null or empty");
                return false;
            }

            if (string.IsNullOrWhiteSpace(requestId))
            {
                _logger.LogError("Cannot send item request created email: request ID is null or empty");
                return false;
            }

            var requestUrl = $"{_emailConfig.BaseUrl}/item-requests/{requestId}";
            var subject = $"New request for your item: {itemName}";
            var body = GenerateItemRequestCreatedEmailBody(ownerName, requesterName, itemName, requestUrl);

            return await SendEmailWithRetryAsync(ownerEmail, subject, body, "item request created");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send item request created email to {Email}", ownerEmail);
            return false;
        }
    }

    public async Task<bool> SendItemRequestApprovedEmailAsync(string requesterEmail, string requesterName, string ownerName, string itemName)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(requesterEmail))
            {
                _logger.LogError("Cannot send item request approved email: requester email is null or empty");
                return false;
            }

            if (string.IsNullOrWhiteSpace(itemName))
            {
                _logger.LogError("Cannot send item request approved email: item name is null or empty");
                return false;
            }

            var subject = $"Your request for {itemName} has been approved";
            var body = GenerateItemRequestApprovedEmailBody(requesterName, ownerName, itemName);

            return await SendEmailWithRetryAsync(requesterEmail, subject, body, "item request approved");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send item request approved email to {Email}", requesterEmail);
            return false;
        }
    }

    public async Task<bool> SendItemRequestRejectedEmailAsync(string requesterEmail, string requesterName, string ownerName, string itemName)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(requesterEmail))
            {
                _logger.LogError("Cannot send item request rejected email: requester email is null or empty");
                return false;
            }

            if (string.IsNullOrWhiteSpace(itemName))
            {
                _logger.LogError("Cannot send item request rejected email: item name is null or empty");
                return false;
            }

            var subject = $"Your request for {itemName} was not approved";
            var body = GenerateItemRequestRejectedEmailBody(requesterName, ownerName, itemName);

            return await SendEmailWithRetryAsync(requesterEmail, subject, body, "item request rejected");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send item request rejected email to {Email}", requesterEmail);
            return false;
        }
    }

    public async Task<bool> SendItemRequestCompletedEmailAsync(string requesterEmail, string requesterName, string ownerName, string itemName)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(requesterEmail))
            {
                _logger.LogError("Cannot send item request completed email: requester email is null or empty");
                return false;
            }

            if (string.IsNullOrWhiteSpace(itemName))
            {
                _logger.LogError("Cannot send item request completed email: item name is null or empty");
                return false;
            }

            var subject = $"Your borrowing of {itemName} is complete";
            var body = GenerateItemRequestCompletedEmailBody(requesterName, ownerName, itemName);

            return await SendEmailWithRetryAsync(requesterEmail, subject, body, "item request completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send item request completed email to {Email}", requesterEmail);
            return false;
        }
    }

    public async Task<bool> SendItemRequestCancelledEmailAsync(string ownerEmail, string ownerName, string requesterName, string itemName)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(ownerEmail))
            {
                _logger.LogError("Cannot send item request cancelled email: owner email is null or empty");
                return false;
            }

            if (string.IsNullOrWhiteSpace(itemName))
            {
                _logger.LogError("Cannot send item request cancelled email: item name is null or empty");
                return false;
            }

            var subject = $"Request for {itemName} has been cancelled";
            var body = GenerateItemRequestCancelledEmailBody(ownerName, requesterName, itemName);

            return await SendEmailWithRetryAsync(ownerEmail, subject, body, "item request cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send item request cancelled email to {Email}", ownerEmail);
            return false;
        }
    }

    public async Task<bool> SendBadgeAwardEmailAsync(string recipientEmail, string recipientName, string badgeType, int currentScore)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(recipientEmail))
            {
                _logger.LogError("Cannot send badge award email: recipient email is null or empty");
                return false;
            }

            if (string.IsNullOrWhiteSpace(badgeType))
            {
                _logger.LogError("Cannot send badge award email: badge type is null or empty");
                return false;
            }

            var subject = $"Congratulations! You've earned a {badgeType} Badge!";
            var body = GenerateBadgeAwardEmailBody(recipientName, badgeType, currentScore);

            return await SendEmailWithRetryAsync(recipientEmail, subject, body, "badge award");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send badge award email to {Email}", recipientEmail);
            return false;
        }
    }

    public async Task<bool> TestEmailConfigurationAsync()
    {
        try
        {
            _logger.LogInformation("Testing email configuration...");
            
            var testSubject = "Email Configuration Test";
            var testBody = GenerateTestEmailBody();
            var testEmail = _emailConfig.TestEmailAddress;

            var result = await SendEmailAsync(testEmail, testSubject, testBody);
            
            if (result)
            {
                _logger.LogInformation("Email configuration test successful");
            }
            else
            {
                _logger.LogError("Email configuration test failed");
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Email configuration test failed with exception");
            return false;
        }
    }

    public EmailHealthStatus GetEmailHealthStatus()
    {
        try
        {
            var status = new EmailHealthStatus
            {
                IsConfigured = !string.IsNullOrEmpty(_emailConfig.SmtpUsername) && !string.IsNullOrEmpty(_emailConfig.SmtpPassword),
                TestMode = _emailConfig.TestMode,
                SmtpHost = _emailConfig.SmtpHost,
                SmtpPort = _emailConfig.SmtpPort,
                FromEmail = _emailConfig.FromEmail,
                ConfigurationErrors = new List<string>()
            };

            // Validate current configuration
            try
            {
                ValidateConfiguration();
            }
            catch (InvalidOperationException ex)
            {
                status.ConfigurationErrors.Add(ex.Message);
            }

            return status;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting email health status");
            return new EmailHealthStatus
            {
                IsConfigured = false,
                TestMode = false,
                ConfigurationErrors = new List<string> { "Error retrieving email configuration status" }
            };
        }
    }

    private string GenerateVerificationEmailBody(string firstName, string verificationUrl)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Verify Your Email Address</title>
    <style>
        body {{
            font-family: Arial, sans-serif;
            line-height: 1.6;
            color: #333;
            max-width: 600px;
            margin: 0 auto;
            padding: 20px;
        }}
        .header {{
            background-color: #007bff;
            color: white;
            padding: 20px;
            text-align: center;
            border-radius: 5px 5px 0 0;
        }}
        .content {{
            background-color: #f8f9fa;
            padding: 30px;
            border-radius: 0 0 5px 5px;
        }}
        .button {{
            display: inline-block;
            background-color: #28a745;
            color: white;
            padding: 12px 30px;
            text-decoration: none;
            border-radius: 5px;
            margin: 20px 0;
            font-weight: bold;
        }}
        .footer {{
            margin-top: 30px;
            padding-top: 20px;
            border-top: 1px solid #dee2e6;
            font-size: 14px;
            color: #6c757d;
        }}
    </style>
</head>
<body>
    <div class='header'>
        <h1>Welcome to Shared Items App!</h1>
    </div>
    <div class='content'>
        <h2>Hi {firstName},</h2>
        <p>Thank you for registering with Shared Items App! To complete your registration and start using the application, please verify your email address by clicking the button below:</p>
        
        <div style='text-align: center;'>
            <a href='{verificationUrl}' class='button'>Verify Email Address</a>
        </div>
        
        <p>If the button doesn't work, you can also copy and paste this link into your browser:</p>
        <p style='word-break: break-all; background-color: #e9ecef; padding: 10px; border-radius: 3px;'>{verificationUrl}</p>
        
        <p><strong>Important:</strong> This verification link will expire in 24 hours for security reasons.</p>
        
        <p>If you didn't create an account with us, please ignore this email.</p>
        
        <div class='footer'>
            <p>Best regards,<br>The Shared Items App Team</p>
            <p><em>This is an automated email. Please do not reply to this message.</em></p>
        </div>
    </div>
</body>
</html>";
    }

    private string GeneratePasswordResetEmailBody(string firstName, string resetUrl)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Reset Your Password</title>
    <style>
        body {{
            font-family: Arial, sans-serif;
            line-height: 1.6;
            color: #333;
            max-width: 600px;
            margin: 0 auto;
            padding: 20px;
        }}
        .header {{
            background-color: #dc3545;
            color: white;
            padding: 20px;
            text-align: center;
            border-radius: 5px 5px 0 0;
        }}
        .content {{
            background-color: #f8f9fa;
            padding: 30px;
            border-radius: 0 0 5px 5px;
        }}
        .button {{
            display: inline-block;
            background-color: #dc3545;
            color: white;
            padding: 12px 30px;
            text-decoration: none;
            border-radius: 5px;
            margin: 20px 0;
            font-weight: bold;
        }}
        .footer {{
            margin-top: 30px;
            padding-top: 20px;
            border-top: 1px solid #dee2e6;
            font-size: 14px;
            color: #6c757d;
        }}
    </style>
</head>
<body>
    <div class='header'>
        <h1>Password Reset Request</h1>
    </div>
    <div class='content'>
        <h2>Hi {firstName},</h2>
        <p>We received a request to reset your password for your Shared Items App account. If you made this request, click the button below to reset your password:</p>
        
        <div style='text-align: center;'>
            <a href='{resetUrl}' class='button'>Reset Password</a>
        </div>
        
        <p>If the button doesn't work, you can also copy and paste this link into your browser:</p>
        <p style='word-break: break-all; background-color: #e9ecef; padding: 10px; border-radius: 3px;'>{resetUrl}</p>
        
        <p><strong>Important:</strong> This password reset link will expire in 1 hour for security reasons.</p>
        
        <p>If you didn't request a password reset, please ignore this email. Your password will remain unchanged.</p>
        
        <div class='footer'>
            <p>Best regards,<br>The Shared Items App Team</p>
            <p><em>This is an automated email. Please do not reply to this message.</em></p>
        </div>
    </div>
</body>
</html>";
    }

    private string GenerateLoopInvitationEmailBody(string recipientName, string inviterName, string loopName, string invitationUrl)
    {
        var displayName = string.IsNullOrWhiteSpace(recipientName) ? "there" : recipientName;
        
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Loop Invitation</title>
    <style>
        body {{
            font-family: Arial, sans-serif;
            line-height: 1.6;
            color: #333;
            max-width: 600px;
            margin: 0 auto;
            padding: 20px;
        }}
        .header {{
            background-color: #6f42c1;
            color: white;
            padding: 20px;
            text-align: center;
            border-radius: 5px 5px 0 0;
        }}
        .content {{
            background-color: #f8f9fa;
            padding: 30px;
            border-radius: 0 0 5px 5px;
        }}
        .button {{
            display: inline-block;
            background-color: #6f42c1;
            color: white;
            padding: 12px 30px;
            text-decoration: none;
            border-radius: 5px;
            margin: 20px 0;
            font-weight: bold;
        }}
        .footer {{
            margin-top: 30px;
            padding-top: 20px;
            border-top: 1px solid #dee2e6;
            font-size: 14px;
            color: #6c757d;
        }}
    </style>
</head>
<body>
    <div class='header'>
        <h1>You're Invited to Join a Loop!</h1>
    </div>
    <div class='content'>
        <h2>Hi {displayName},</h2>
        <p><strong>{inviterName}</strong> has invited you to join the loop <strong>{loopName}</strong> on Shared Items App!</p>
        
        <p>Loops are sharing groups where members can share items with each other. By joining this loop, you'll be able to see and access items shared by other members.</p>
        
        <div style='text-align: center;'>
            <a href='{invitationUrl}' class='button'>Accept Invitation</a>
        </div>
        
        <p>If the button doesn't work, you can also copy and paste this link into your browser:</p>
        <p style='word-break: break-all; background-color: #e9ecef; padding: 10px; border-radius: 3px;'>{invitationUrl}</p>
        
        <p><strong>Important:</strong> This invitation link will expire in 30 days for security reasons.</p>
        
        <p>If you don't have an account yet, you'll be prompted to create one when you accept the invitation.</p>
        
        <div class='footer'>
            <p>Best regards,<br>The Shared Items App Team</p>
            <p><em>This is an automated email. Please do not reply to this message.</em></p>
        </div>
    </div>
</body>
</html>";
    }

    private string GenerateItemRequestCreatedEmailBody(string ownerName, string requesterName, string itemName, string requestUrl)
    {
        var displayName = string.IsNullOrWhiteSpace(ownerName) ? "there" : ownerName;
        
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>New Item Request</title>
    <style>
        body {{
            font-family: Arial, sans-serif;
            line-height: 1.6;
            color: #333;
            max-width: 600px;
            margin: 0 auto;
            padding: 20px;
        }}
        .header {{
            background-color: #007bff;
            color: white;
            padding: 20px;
            text-align: center;
            border-radius: 5px 5px 0 0;
        }}
        .content {{
            background-color: #f8f9fa;
            padding: 30px;
            border-radius: 0 0 5px 5px;
        }}
        .button {{
            display: inline-block;
            background-color: #007bff;
            color: white;
            padding: 12px 30px;
            text-decoration: none;
            border-radius: 5px;
            margin: 20px 0;
            font-weight: bold;
        }}
        .footer {{
            margin-top: 30px;
            padding-top: 20px;
            border-top: 1px solid #dee2e6;
            font-size: 14px;
            color: #6c757d;
        }}
    </style>
</head>
<body>
    <div class='header'>
        <h1>New Request for Your Item</h1>
    </div>
    <div class='content'>
        <h2>Hi {displayName},</h2>
        <p><strong>{requesterName}</strong> has requested to borrow your item <strong>{itemName}</strong>.</p>
        
        <p>You can review the request details and decide whether to approve or decline it.</p>
        
        <div style='text-align: center;'>
            <a href='{requestUrl}' class='button'>View Request</a>
        </div>
        
        <p>If the button doesn't work, you can also copy and paste this link into your browser:</p>
        <p style='word-break: break-all; background-color: #e9ecef; padding: 10px; border-radius: 3px;'>{requestUrl}</p>
        
        <div class='footer'>
            <p>Best regards,<br>The LendingLoop Team</p>
            <p><em>This is an automated email. Please do not reply to this message.</em></p>
        </div>
    </div>
</body>
</html>";
    }

    private string GenerateItemRequestApprovedEmailBody(string requesterName, string ownerName, string itemName)
    {
        var displayName = string.IsNullOrWhiteSpace(requesterName) ? "there" : requesterName;
        
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Request Approved</title>
    <style>
        body {{
            font-family: Arial, sans-serif;
            line-height: 1.6;
            color: #333;
            max-width: 600px;
            margin: 0 auto;
            padding: 20px;
        }}
        .header {{
            background-color: #28a745;
            color: white;
            padding: 20px;
            text-align: center;
            border-radius: 5px 5px 0 0;
        }}
        .content {{
            background-color: #f8f9fa;
            padding: 30px;
            border-radius: 0 0 5px 5px;
        }}
        .success {{
            background-color: #d4edda;
            color: #155724;
            padding: 15px;
            border-radius: 5px;
            margin: 20px 0;
            border: 1px solid #c3e6cb;
        }}
        .footer {{
            margin-top: 30px;
            padding-top: 20px;
            border-top: 1px solid #dee2e6;
            font-size: 14px;
            color: #6c757d;
        }}
    </style>
</head>
<body>
    <div class='header'>
        <h1>Request Approved!</h1>
    </div>
    <div class='content'>
        <h2>Hi {displayName},</h2>
        <div class='success'>
            <p><strong>Good news!</strong> {ownerName} has approved your request to borrow <strong>{itemName}</strong>.</p>
        </div>
        
        <p>You can now coordinate with {ownerName} to arrange pickup of the item.</p>
        
        <p>Please remember to return the item in good condition and mark the request as complete when you're done.</p>
        
        <div class='footer'>
            <p>Best regards,<br>The LendingLoop Team</p>
            <p><em>This is an automated email. Please do not reply to this message.</em></p>
        </div>
    </div>
</body>
</html>";
    }

    private string GenerateItemRequestRejectedEmailBody(string requesterName, string ownerName, string itemName)
    {
        var displayName = string.IsNullOrWhiteSpace(requesterName) ? "there" : requesterName;
        
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Request Not Approved</title>
    <style>
        body {{
            font-family: Arial, sans-serif;
            line-height: 1.6;
            color: #333;
            max-width: 600px;
            margin: 0 auto;
            padding: 20px;
        }}
        .header {{
            background-color: #dc3545;
            color: white;
            padding: 20px;
            text-align: center;
            border-radius: 5px 5px 0 0;
        }}
        .content {{
            background-color: #f8f9fa;
            padding: 30px;
            border-radius: 0 0 5px 5px;
        }}
        .info {{
            background-color: #fff3cd;
            color: #856404;
            padding: 15px;
            border-radius: 5px;
            margin: 20px 0;
            border: 1px solid #ffeaa7;
        }}
        .footer {{
            margin-top: 30px;
            padding-top: 20px;
            border-top: 1px solid #dee2e6;
            font-size: 14px;
            color: #6c757d;
        }}
    </style>
</head>
<body>
    <div class='header'>
        <h1>Request Update</h1>
    </div>
    <div class='content'>
        <h2>Hi {displayName},</h2>
        <div class='info'>
            <p>{ownerName} has declined your request to borrow <strong>{itemName}</strong>.</p>
        </div>
        
        <p>Unfortunately, the item is not available at this time. This could be due to scheduling conflicts or other reasons.</p>
        
        <p>Don't worry! There are many other items available in your loops. Feel free to browse and request other items that might meet your needs.</p>
        
        <div class='footer'>
            <p>Best regards,<br>The LendingLoop Team</p>
            <p><em>This is an automated email. Please do not reply to this message.</em></p>
        </div>
    </div>
</body>
</html>";
    }

    private string GenerateItemRequestCompletedEmailBody(string requesterName, string ownerName, string itemName)
    {
        var displayName = string.IsNullOrWhiteSpace(requesterName) ? "there" : requesterName;
        
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Borrowing Complete</title>
    <style>
        body {{
            font-family: Arial, sans-serif;
            line-height: 1.6;
            color: #333;
            max-width: 600px;
            margin: 0 auto;
            padding: 20px;
        }}
        .header {{
            background-color: #17a2b8;
            color: white;
            padding: 20px;
            text-align: center;
            border-radius: 5px 5px 0 0;
        }}
        .content {{
            background-color: #f8f9fa;
            padding: 30px;
            border-radius: 0 0 5px 5px;
        }}
        .success {{
            background-color: #d1ecf1;
            color: #0c5460;
            padding: 15px;
            border-radius: 5px;
            margin: 20px 0;
            border: 1px solid #bee5eb;
        }}
        .footer {{
            margin-top: 30px;
            padding-top: 20px;
            border-top: 1px solid #dee2e6;
            font-size: 14px;
            color: #6c757d;
        }}
    </style>
</head>
<body>
    <div class='header'>
        <h1>Borrowing Complete</h1>
    </div>
    <div class='content'>
        <h2>Hi {displayName},</h2>
        <div class='success'>
            <p>{ownerName} has marked your borrowing of <strong>{itemName}</strong> as complete.</p>
        </div>
        
        <p>Thank you for returning the item! We hope it was helpful for your needs.</p>
        
        <p>Your participation in the sharing community helps everyone access the items they need while reducing waste.</p>
        
        <p>Feel free to browse and request other items whenever you need them!</p>
        
        <div class='footer'>
            <p>Best regards,<br>The LendingLoop Team</p>
            <p><em>This is an automated email. Please do not reply to this message.</em></p>
        </div>
    </div>
</body>
</html>";
    }

    private string GenerateItemRequestCancelledEmailBody(string ownerName, string requesterName, string itemName)
    {
        var displayName = string.IsNullOrWhiteSpace(ownerName) ? "there" : ownerName;
        
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Request Cancelled</title>
    <style>
        body {{
            font-family: Arial, sans-serif;
            line-height: 1.6;
            color: #333;
            max-width: 600px;
            margin: 0 auto;
            padding: 20px;
        }}
        .header {{
            background-color: #6c757d;
            color: white;
            padding: 20px;
            text-align: center;
            border-radius: 5px 5px 0 0;
        }}
        .content {{
            background-color: #f8f9fa;
            padding: 30px;
            border-radius: 0 0 5px 5px;
        }}
        .info {{
            background-color: #e2e3e5;
            color: #383d41;
            padding: 15px;
            border-radius: 5px;
            margin: 20px 0;
            border: 1px solid #d6d8db;
        }}
        .footer {{
            margin-top: 30px;
            padding-top: 20px;
            border-top: 1px solid #dee2e6;
            font-size: 14px;
            color: #6c757d;
        }}
    </style>
</head>
<body>
    <div class='header'>
        <h1>Request Cancelled</h1>
    </div>
    <div class='content'>
        <h2>Hi {displayName},</h2>
        <div class='info'>
            <p>{requesterName} has cancelled their request to borrow your item <strong>{itemName}</strong>.</p>
        </div>
        
        <p>Your item is now available for other requests. No further action is needed from you.</p>
        
        <p>Thank you for being part of the sharing community!</p>
        
        <div class='footer'>
            <p>Best regards,<br>The LendingLoop Team</p>
            <p><em>This is an automated email. Please do not reply to this message.</em></p>
        </div>
    </div>
</body>
</html>";
    }

    private string GenerateTestEmailBody()
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Email Configuration Test</title>
    <style>
        body {{
            font-family: Arial, sans-serif;
            line-height: 1.6;
            color: #333;
            max-width: 600px;
            margin: 0 auto;
            padding: 20px;
        }}
        .header {{
            background-color: #17a2b8;
            color: white;
            padding: 20px;
            text-align: center;
            border-radius: 5px 5px 0 0;
        }}
        .content {{
            background-color: #f8f9fa;
            padding: 30px;
            border-radius: 0 0 5px 5px;
        }}
        .success {{
            background-color: #d4edda;
            color: #155724;
            padding: 15px;
            border-radius: 5px;
            margin: 20px 0;
            border: 1px solid #c3e6cb;
        }}
        .footer {{
            margin-top: 30px;
            padding-top: 20px;
            border-top: 1px solid #dee2e6;
            font-size: 14px;
            color: #6c757d;
        }}
    </style>
</head>
<body>
    <div class='header'>
        <h1>Email Configuration Test</h1>
    </div>
    <div class='content'>
        <div class='success'>
            <h3>✅ Success!</h3>
            <p>Your email configuration is working correctly.</p>
        </div>
        
        <p>This is a test email to verify that your Shared Items App email service is configured properly.</p>
        
        <p><strong>Test Details:</strong></p>
        <ul>
            <li>SMTP Host: {_emailConfig.SmtpHost}</li>
            <li>SMTP Port: {_emailConfig.SmtpPort}</li>
            <li>From Email: {_emailConfig.FromEmail}</li>
            <li>SSL Enabled: {_emailConfig.EnableSsl}</li>
            <li>Test Mode: {_emailConfig.TestMode}</li>
            <li>Timestamp: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</li>
        </ul>
        
        <p>If you received this email, your email service is ready to send verification and password reset emails to users.</p>
        
        <div class='footer'>
            <p>Best regards,<br>The Shared Items App Team</p>
            <p><em>This is an automated test email.</em></p>
        </div>
    </div>
</body>
</html>";
    }

    private string GenerateBadgeAwardEmailBody(string recipientName, string badgeType, int currentScore)
    {
        var displayName = string.IsNullOrWhiteSpace(recipientName) ? "there" : recipientName;
        var badgeEmoji = badgeType switch
        {
            "Bronze" => "🥉",
            "Silver" => "🥈",
            "Gold" => "🥇",
            _ => "🏆"
        };
        
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Badge Earned!</title>
    <style>
        body {{
            font-family: Arial, sans-serif;
            line-height: 1.6;
            color: #333;
            max-width: 600px;
            margin: 0 auto;
            padding: 20px;
        }}
        .header {{
            background: linear-gradient(135deg, #FFD700 0%, #FFA500 100%);
            color: white;
            padding: 30px;
            text-align: center;
            border-radius: 5px 5px 0 0;
        }}
        .badge-icon {{
            font-size: 72px;
            margin: 20px 0;
        }}
        .content {{
            background-color: #f8f9fa;
            padding: 30px;
            border-radius: 0 0 5px 5px;
        }}
        .achievement {{
            background-color: #fff;
            padding: 20px;
            border-radius: 5px;
            margin: 20px 0;
            border-left: 4px solid #FFD700;
        }}
        .score {{
            font-size: 24px;
            font-weight: bold;
            color: #FFA500;
            text-align: center;
            margin: 20px 0;
        }}
        .footer {{
            margin-top: 30px;
            padding-top: 20px;
            border-top: 1px solid #dee2e6;
            color: #6c757d;
            font-size: 14px;
        }}
    </style>
</head>
<body>
    <div class='header'>
        <h1>🎉 Congratulations! 🎉</h1>
        <div class='badge-icon'>{badgeEmoji}</div>
        <h2>You've earned a {badgeType} Badge!</h2>
    </div>
    <div class='content'>
        <p>Hi {displayName},</p>
        
        <div class='achievement'>
            <p><strong>Amazing achievement!</strong> You've reached a new milestone in your LendingLoop journey.</p>
            <p>Your dedication to sharing and community building has earned you the <strong>{badgeType} Badge</strong>!</p>
        </div>
        
        <div class='score'>
            Your Current LoopScore: ⭐ {currentScore}
        </div>
        
        <p>Keep up the great work! Continue lending and borrowing items to earn even more badges and build a stronger community.</p>
        
        <div class='footer'>
            <p>Best regards,<br>The LendingLoop Team</p>
            <p><em>This is an automated notification from LendingLoop.</em></p>
        </div>
    </div>
</body>
</html>";
    }
}

public class EmailConfiguration
{
    public string SmtpHost { get; set; } = string.Empty;
    public int SmtpPort { get; set; }
    public string SmtpUsername { get; set; } = string.Empty;
    public string SmtpPassword { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
    public bool EnableSsl { get; set; }
    public bool UseDefaultCredentials { get; set; }
    public int TimeoutSeconds { get; set; }
    public int MaxRetryAttempts { get; set; }
    public int RetryDelaySeconds { get; set; }
    public bool TestMode { get; set; }
    public string TestEmailAddress { get; set; } = string.Empty;
}

public class EmailHealthStatus
{
    public bool IsConfigured { get; set; }
    public bool TestMode { get; set; }
    public string SmtpHost { get; set; } = string.Empty;
    public int SmtpPort { get; set; }
    public string FromEmail { get; set; } = string.Empty;
    public List<string> ConfigurationErrors { get; set; } = new();
}