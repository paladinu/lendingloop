using Api.Models;
using Api.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System.Collections.Generic;
using Xunit;

namespace Api.Tests;

public class EmailServiceIntegrationTests
{
    private readonly Mock<ILogger<EmailService>> _mockLogger;

    public EmailServiceIntegrationTests()
    {
        _mockLogger = new Mock<ILogger<EmailService>>();
    }

    private IConfiguration CreateIntegrationTestConfiguration()
    {
        var settings = new Dictionary<string, string>
        {
            ["Email:SmtpHost"] = "smtp.gmail.com",
            ["Email:SmtpPort"] = "587",
            ["Email:SmtpUsername"] = "", // Would be set in actual integration tests
            ["Email:SmtpPassword"] = "", // Would be set in actual integration tests
            ["Email:FromEmail"] = "noreply@test.com",
            ["Email:FromName"] = "Test App",
            ["Email:BaseUrl"] = "https://test.com",
            ["Email:EnableSsl"] = "true",
            ["Email:UseDefaultCredentials"] = "false",
            ["Email:TimeoutSeconds"] = "30",
            ["Email:MaxRetryAttempts"] = "2",
            ["Email:RetryDelaySeconds"] = "1",
            ["Email:TestMode"] = "false", // Set to false for real SMTP testing
            ["Email:TestEmailAddress"] = "integration.test@example.com"
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(settings!)
            .Build();
    }

    [Fact]
    public async Task SendVerificationEmailAsync_WithoutCredentials_ShouldReturnTrueInDevelopmentMode()
    {
        //arrange
        var configuration = CreateIntegrationTestConfiguration();
        var emailService = new EmailService(configuration, _mockLogger.Object);
        var user = new User
        {
            Email = "test@example.com",
            FirstName = "John",
            LastName = "Doe"
        };
        var token = "integration-test-token-123";

        //act
        var result = await emailService.SendVerificationEmailAsync(user, token);

        //assert
        // Should return true because when credentials are empty, it logs the email instead of sending
        Assert.True(result);
    }

    [Fact]
    public async Task SendPasswordResetEmailAsync_WithoutCredentials_ShouldReturnTrueInDevelopmentMode()
    {
        //arrange
        var configuration = CreateIntegrationTestConfiguration();
        var emailService = new EmailService(configuration, _mockLogger.Object);
        var user = new User
        {
            Email = "test@example.com",
            FirstName = "John",
            LastName = "Doe"
        };
        var token = "integration-reset-token-123";

        //act
        var result = await emailService.SendPasswordResetEmailAsync(user, token);

        //assert
        // Should return true because when credentials are empty, it logs the email instead of sending
        Assert.True(result);
    }

    [Fact]
    public async Task TestEmailConfigurationAsync_WithoutCredentials_ShouldReturnTrueInDevelopmentMode()
    {
        //arrange
        var configuration = CreateIntegrationTestConfiguration();
        var emailService = new EmailService(configuration, _mockLogger.Object);

        //act
        var result = await emailService.TestEmailConfigurationAsync();

        //assert
        // Should return true because when credentials are empty, it logs the email instead of sending
        Assert.True(result);
    }

    [Fact]
    public void GetEmailHealthStatus_WithIntegrationConfiguration_ShouldReturnCorrectStatus()
    {
        //arrange
        var configuration = CreateIntegrationTestConfiguration();
        var emailService = new EmailService(configuration, _mockLogger.Object);

        //act
        var status = emailService.GetEmailHealthStatus();

        //assert
        Assert.NotNull(status);
        Assert.False(status.IsConfigured); // False because credentials are empty
        Assert.False(status.TestMode);
        Assert.Equal("smtp.gmail.com", status.SmtpHost);
        Assert.Equal(587, status.SmtpPort);
        Assert.Equal("noreply@test.com", status.FromEmail);
        Assert.Empty(status.ConfigurationErrors);
    }

    [Fact]
    public async Task EmailService_WithRetryLogic_ShouldHandleFailuresGracefully()
    {
        //arrange
        var configuration = CreateIntegrationTestConfiguration();
        var emailService = new EmailService(configuration, _mockLogger.Object);
        var user = new User
        {
            Email = "invalid-email-format", // This will cause validation to fail
            FirstName = "John"
        };
        var token = "test-token";

        //act
        var result = await emailService.SendVerificationEmailAsync(user, token);

        //assert
        Assert.False(result); // Should fail due to invalid email format
    }

    [Fact]
    public async Task EmailService_WithLongTimeout_ShouldHandleTimeoutGracefully()
    {
        //arrange
        var settings = new Dictionary<string, string>
        {
            ["Email:SmtpHost"] = "nonexistent.smtp.server.com", // Non-existent server
            ["Email:SmtpPort"] = "587",
            ["Email:SmtpUsername"] = "test@test.com",
            ["Email:SmtpPassword"] = "testpassword",
            ["Email:FromEmail"] = "noreply@test.com",
            ["Email:FromName"] = "Test App",
            ["Email:BaseUrl"] = "https://test.com",
            ["Email:EnableSsl"] = "true",
            ["Email:UseDefaultCredentials"] = "false",
            ["Email:TimeoutSeconds"] = "5", // Short timeout for testing
            ["Email:MaxRetryAttempts"] = "1",
            ["Email:RetryDelaySeconds"] = "1",
            ["Email:TestMode"] = "false",
            ["Email:TestEmailAddress"] = "test@example.com"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings!)
            .Build();

        var emailService = new EmailService(configuration, _mockLogger.Object);
        var user = new User
        {
            Email = "test@example.com",
            FirstName = "John"
        };
        var token = "test-token";

        //act
        var result = await emailService.SendVerificationEmailAsync(user, token);

        //assert
        Assert.False(result); // Should fail due to non-existent SMTP server
    }

    // Note: The following test would require actual SMTP credentials to run
    // It's commented out to prevent failures in CI/CD environments
    /*
    [Fact]
    [Trait("Category", "RequiresSmtpCredentials")]
    public async Task SendVerificationEmailAsync_WithRealSmtpCredentials_ShouldSendActualEmail()
    {
        // This test would require real SMTP credentials to be configured
        // It should only be run manually with proper credentials
        
        //arrange
        var settings = new Dictionary<string, string>
        {
            ["Email:SmtpHost"] = "smtp.gmail.com",
            ["Email:SmtpPort"] = "587",
            ["Email:SmtpUsername"] = "your-actual-email@gmail.com",
            ["Email:SmtpPassword"] = "your-actual-app-password",
            ["Email:FromEmail"] = "your-actual-email@gmail.com",
            ["Email:FromName"] = "Test App",
            ["Email:BaseUrl"] = "https://test.com",
            ["Email:EnableSsl"] = "true",
            ["Email:UseDefaultCredentials"] = "false",
            ["Email:TimeoutSeconds"] = "30",
            ["Email:MaxRetryAttempts"] = "3",
            ["Email:RetryDelaySeconds"] = "5",
            ["Email:TestMode"] = "false",
            ["Email:TestEmailAddress"] = "recipient@example.com"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings!)
            .Build();

        var emailService = new EmailService(configuration, _mockLogger.Object);
        var user = new User
        {
            Email = "recipient@example.com",
            FirstName = "Test",
            LastName = "User"
        };
        var token = "real-verification-token-123";

        //act
        var result = await emailService.SendVerificationEmailAsync(user, token);

        //assert
        Assert.True(result);
    }
    */
}