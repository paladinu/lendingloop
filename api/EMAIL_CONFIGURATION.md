# Email Service Configuration and Testing

This document explains how to configure and test the email service in the Shared Items API.

## Configuration

The email service is configured through the `appsettings.json` and `appsettings.Development.json` files. Here are the available configuration options:

### Basic Configuration
```json
{
  "Email": {
    "SmtpHost": "smtp.gmail.com",
    "SmtpPort": "587",
    "SmtpUsername": "your-email@gmail.com",
    "SmtpPassword": "your-app-password",
    "FromEmail": "noreply@yourdomain.com",
    "FromName": "Your App Name",
    "BaseUrl": "https://yourdomain.com"
  }
}
```

### Advanced Configuration
```json
{
  "Email": {
    "SmtpHost": "smtp.gmail.com",
    "SmtpPort": "587",
    "SmtpUsername": "your-email@gmail.com",
    "SmtpPassword": "your-app-password",
    "FromEmail": "noreply@yourdomain.com",
    "FromName": "Your App Name",
    "BaseUrl": "https://yourdomain.com",
    "EnableSsl": "true",
    "UseDefaultCredentials": "false",
    "TimeoutSeconds": "30",
    "MaxRetryAttempts": "3",
    "RetryDelaySeconds": "5",
    "TestMode": "false",
    "TestEmailAddress": "test@example.com"
  }
}
```

## Configuration Options

| Option | Description | Default | Required |
|--------|-------------|---------|----------|
| `SmtpHost` | SMTP server hostname | `localhost` | Yes |
| `SmtpPort` | SMTP server port | `587` | Yes |
| `SmtpUsername` | SMTP authentication username | `""` | No* |
| `SmtpPassword` | SMTP authentication password | `""` | No* |
| `FromEmail` | Email address to send from | `noreply@example.com` | Yes |
| `FromName` | Display name for sender | `Shared Items App` | Yes |
| `BaseUrl` | Base URL for email links | `http://localhost:4200` | Yes |
| `EnableSsl` | Enable SSL/TLS encryption | `true` | No |
| `UseDefaultCredentials` | Use default Windows credentials | `false` | No |
| `TimeoutSeconds` | SMTP connection timeout | `30` | No |
| `MaxRetryAttempts` | Number of retry attempts | `3` | No |
| `RetryDelaySeconds` | Delay between retries | `5` | No |
| `TestMode` | Enable test mode (logs instead of sending) | `false` | No |
| `TestEmailAddress` | Email address for test emails | `test@example.com` | No |

*Note: If `SmtpUsername` and `SmtpPassword` are empty, the service will log emails instead of sending them (useful for development).

## Test Mode

When `TestMode` is set to `true`, the email service will:
- Log email details instead of sending actual emails
- Always return success for email operations
- Include email content in debug logs

This is useful for development and testing environments where you don't want to send real emails.

## Development Setup

For development, you can use the following configuration in `appsettings.Development.json`:

```json
{
  "Email": {
    "SmtpHost": "smtp.gmail.com",
    "SmtpPort": "587",
    "SmtpUsername": "",
    "SmtpPassword": "",
    "FromEmail": "noreply@localhost",
    "FromName": "Shared Items App (Development)",
    "BaseUrl": "http://localhost:4200",
    "TestMode": "true",
    "TestEmailAddress": "test@localhost.com"
  }
}
```

This configuration will log all emails to the console instead of sending them.

## Production Setup

For production, ensure you have proper SMTP credentials:

1. **Gmail**: Use an App Password (not your regular password)
2. **SendGrid**: Use your API key as the password
3. **AWS SES**: Use your SMTP credentials
4. **Other providers**: Follow their SMTP configuration guidelines

Example production configuration:
```json
{
  "Email": {
    "SmtpHost": "smtp.gmail.com",
    "SmtpPort": "587",
    "SmtpUsername": "your-app@gmail.com",
    "SmtpPassword": "your-16-character-app-password",
    "FromEmail": "noreply@yourdomain.com",
    "FromName": "Your App Name",
    "BaseUrl": "https://yourdomain.com",
    "TestMode": "false",
    "MaxRetryAttempts": "3",
    "RetryDelaySeconds": "5"
  }
}
```

## Testing the Email Service

### API Endpoints

The email service provides two endpoints for testing and monitoring:

#### Test Email Configuration
```http
POST /api/auth/test-email
Authorization: Bearer <your-jwt-token>
```

This endpoint sends a test email to verify the configuration is working.

#### Get Email Health Status
```http
GET /api/auth/email-health
Authorization: Bearer <your-jwt-token>
```

This endpoint returns the current email service configuration status:
```json
{
  "isConfigured": true,
  "testMode": false,
  "smtpHost": "smtp.gmail.com",
  "smtpPort": 587,
  "fromEmail": "noreply@yourdomain.com",
  "configurationErrors": [],
  "timestamp": "2024-01-01T12:00:00.000Z"
}
```

### Unit Tests

Run the email service unit tests:
```bash
dotnet test Api.Tests/Api.Tests.csproj --filter "EmailService"
```

### Integration Tests

The integration tests verify the email service behavior with different configurations:
```bash
dotnet test Api.Tests/Api.Tests.csproj --filter "EmailServiceIntegration"
```

## Error Handling

The email service includes comprehensive error handling:

1. **Configuration Validation**: Validates all settings on startup
2. **Retry Logic**: Automatically retries failed email sends
3. **Timeout Handling**: Prevents hanging on slow SMTP servers
4. **Logging**: Detailed logging for troubleshooting

## Security Considerations

1. **Never commit SMTP credentials** to version control
2. **Use environment variables** or secure configuration providers for production
3. **Enable SSL/TLS** for SMTP connections
4. **Use App Passwords** instead of regular passwords for Gmail
5. **Restrict API endpoints** to authenticated users only

## Troubleshooting

### Common Issues

1. **Authentication Failed**: Check username/password and enable "Less secure app access" or use App Passwords
2. **Connection Timeout**: Verify SMTP host and port, check firewall settings
3. **SSL/TLS Errors**: Ensure `EnableSsl` is set correctly for your SMTP provider
4. **Rate Limiting**: Some providers limit email sending rates

### Debugging

1. Enable detailed logging in `appsettings.json`:
```json
{
  "Logging": {
    "LogLevel": {
      "Api.Services.EmailService": "Debug"
    }
  }
}
```

2. Use test mode to verify email content without sending
3. Check the email health endpoint for configuration issues
4. Review application logs for detailed error messages

## Environment Variables

You can override configuration using environment variables:
```bash
Email__SmtpUsername=your-email@gmail.com
Email__SmtpPassword=your-app-password
Email__TestMode=false
```

This is the recommended approach for production deployments.