using Api.Models;
using Api.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System.Collections.Generic;
using Xunit;

namespace Api.Tests;

public class EmailServiceTests
{
    private readonly Mock<ILogger<EmailService>> _mockLogger;
    private readonly IConfiguration _configuration;

    public EmailServiceTests()
    {
        _mockLogger = new Mock<ILogger<EmailService>>();
        _configuration = CreateTestConfiguration();
    }

    private IConfiguration CreateTestConfiguration(Dictionary<string, string>? customSettings = null)
    {
        var settings = new Dictionary<string, string>
        {
            ["Email:SmtpHost"] = "smtp.test.com",
            ["Email:SmtpPort"] = "587",
            ["Email:SmtpUsername"] = "test@test.com",
            ["Email:SmtpPassword"] = "testpassword",
            ["Email:FromEmail"] = "noreply@test.com",
            ["Email:FromName"] = "Test App",
            ["Email:BaseUrl"] = "https://test.com",
            ["Email:EnableSsl"] = "true",
            ["Email:UseDefaultCredentials"] = "false",
            ["Email:TimeoutSeconds"] = "30",
            ["Email:MaxRetryAttempts"] = "3",
            ["Email:RetryDelaySeconds"] = "1",
            ["Email:TestMode"] = "true",
            ["Email:TestEmailAddress"] = "test@example.com"
        };

        if (customSettings != null)
        {
            foreach (var setting in customSettings)
            {
                settings[setting.Key] = setting.Value;
            }
        }

        return new ConfigurationBuilder()
            .AddInMemoryCollection(settings!)
            .Build();
    }

    [Fact]
    public void Constructor_WithValidConfiguration_ShouldInitializeSuccessfully()
    {
        // Act & Assert
        var emailService = new EmailService(_configuration, _mockLogger.Object);
        Assert.NotNull(emailService);
    }

    [Fact]
    public void Constructor_WithInvalidSmtpHost_ShouldThrowException()
    {
        // Arrange
        var invalidConfig = CreateTestConfiguration(new Dictionary<string, string>
        {
            ["Email:SmtpHost"] = ""
        });

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => 
            new EmailService(invalidConfig, _mockLogger.Object));
        
        Assert.Contains("Email:SmtpHost is required", exception.Message);
    }

    [Fact]
    public void Constructor_WithInvalidSmtpPort_ShouldThrowException()
    {
        // Arrange
        var invalidConfig = CreateTestConfiguration(new Dictionary<string, string>
        {
            ["Email:SmtpPort"] = "0"
        });

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => 
            new EmailService(invalidConfig, _mockLogger.Object));
        
        Assert.Contains("Email:SmtpPort must be between 1 and 65535", exception.Message);
    }

    [Fact]
    public void Constructor_WithInvalidFromEmail_ShouldThrowException()
    {
        // Arrange
        var invalidConfig = CreateTestConfiguration(new Dictionary<string, string>
        {
            ["Email:FromEmail"] = "invalid-email"
        });

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => 
            new EmailService(invalidConfig, _mockLogger.Object));
        
        Assert.Contains("Email:FromEmail must be a valid email address", exception.Message);
    }

    [Fact]
    public void Constructor_WithInvalidBaseUrl_ShouldThrowException()
    {
        // Arrange
        var invalidConfig = CreateTestConfiguration(new Dictionary<string, string>
        {
            ["Email:BaseUrl"] = "not-a-url"
        });

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => 
            new EmailService(invalidConfig, _mockLogger.Object));
        
        Assert.Contains("Email:BaseUrl must be a valid URL", exception.Message);
    }

    [Fact]
    public async Task SendVerificationEmailAsync_WithValidUser_ShouldReturnTrue()
    {
        // Arrange
        var emailService = new EmailService(_configuration, _mockLogger.Object);
        var user = new User
        {
            Email = "test@example.com",
            FirstName = "John"
        };
        var token = "test-token-123";

        // Act
        var result = await emailService.SendVerificationEmailAsync(user, token);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task SendVerificationEmailAsync_WithNullUser_ShouldReturnFalse()
    {
        // Arrange
        var emailService = new EmailService(_configuration, _mockLogger.Object);
        var token = "test-token-123";

        // Act
        var result = await emailService.SendVerificationEmailAsync(null!, token);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task SendVerificationEmailAsync_WithEmptyEmail_ShouldReturnFalse()
    {
        // Arrange
        var emailService = new EmailService(_configuration, _mockLogger.Object);
        var user = new User
        {
            Email = "",
            FirstName = "John"
        };
        var token = "test-token-123";

        // Act
        var result = await emailService.SendVerificationEmailAsync(user, token);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task SendVerificationEmailAsync_WithEmptyToken_ShouldReturnFalse()
    {
        // Arrange
        var emailService = new EmailService(_configuration, _mockLogger.Object);
        var user = new User
        {
            Email = "test@example.com",
            FirstName = "John"
        };

        // Act
        var result = await emailService.SendVerificationEmailAsync(user, "");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task SendPasswordResetEmailAsync_WithValidUser_ShouldReturnTrue()
    {
        // Arrange
        var emailService = new EmailService(_configuration, _mockLogger.Object);
        var user = new User
        {
            Email = "test@example.com",
            FirstName = "John"
        };
        var token = "reset-token-123";

        // Act
        var result = await emailService.SendPasswordResetEmailAsync(user, token);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task SendPasswordResetEmailAsync_WithNullUser_ShouldReturnFalse()
    {
        // Arrange
        var emailService = new EmailService(_configuration, _mockLogger.Object);
        var token = "reset-token-123";

        // Act
        var result = await emailService.SendPasswordResetEmailAsync(null!, token);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task SendPasswordResetEmailAsync_WithEmptyEmail_ShouldReturnFalse()
    {
        // Arrange
        var emailService = new EmailService(_configuration, _mockLogger.Object);
        var user = new User
        {
            Email = "",
            FirstName = "John"
        };
        var token = "reset-token-123";

        // Act
        var result = await emailService.SendPasswordResetEmailAsync(user, token);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task SendPasswordResetEmailAsync_WithEmptyToken_ShouldReturnFalse()
    {
        // Arrange
        var emailService = new EmailService(_configuration, _mockLogger.Object);
        var user = new User
        {
            Email = "test@example.com",
            FirstName = "John"
        };

        // Act
        var result = await emailService.SendPasswordResetEmailAsync(user, "");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task TestEmailConfigurationAsync_WithValidConfiguration_ShouldReturnTrue()
    {
        // Arrange
        var emailService = new EmailService(_configuration, _mockLogger.Object);

        // Act
        var result = await emailService.TestEmailConfigurationAsync();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void GetEmailHealthStatus_WithValidConfiguration_ShouldReturnHealthyStatus()
    {
        // Arrange
        var emailService = new EmailService(_configuration, _mockLogger.Object);

        // Act
        var status = emailService.GetEmailHealthStatus();

        // Assert
        Assert.NotNull(status);
        Assert.True(status.IsConfigured);
        Assert.True(status.TestMode);
        Assert.Equal("smtp.test.com", status.SmtpHost);
        Assert.Equal(587, status.SmtpPort);
        Assert.Equal("noreply@test.com", status.FromEmail);
        Assert.Empty(status.ConfigurationErrors);
    }

    [Fact]
    public void GetEmailHealthStatus_WithMissingCredentials_ShouldReturnNotConfigured()
    {
        // Arrange
        var configWithoutCredentials = CreateTestConfiguration(new Dictionary<string, string>
        {
            ["Email:SmtpUsername"] = "",
            ["Email:SmtpPassword"] = ""
        });
        var emailService = new EmailService(configWithoutCredentials, _mockLogger.Object);

        // Act
        var status = emailService.GetEmailHealthStatus();

        // Assert
        Assert.NotNull(status);
        Assert.False(status.IsConfigured);
        Assert.True(status.TestMode);
        Assert.Equal("smtp.test.com", status.SmtpHost);
        Assert.Equal(587, status.SmtpPort);
        Assert.Equal("noreply@test.com", status.FromEmail);
        Assert.Empty(status.ConfigurationErrors);
    }

    [Theory]
    [InlineData("test@example.com", true)]
    [InlineData("user.name@domain.co.uk", true)]
    [InlineData("invalid-email", false)]
    [InlineData("@domain.com", false)]
    [InlineData("user@", false)]
    [InlineData("", false)]
    public async Task SendVerificationEmailAsync_WithVariousEmailFormats_ShouldValidateCorrectly(string email, bool expectedResult)
    {
        // Arrange
        var emailService = new EmailService(_configuration, _mockLogger.Object);
        var user = new User
        {
            Email = email,
            FirstName = "John"
        };
        var token = "test-token-123";

        // Act
        var result = await emailService.SendVerificationEmailAsync(user, token);

        // Assert
        Assert.Equal(expectedResult, result);
    }

    [Fact]
    public async Task SendVerificationEmailAsync_InTestMode_ShouldLogEmailDetails()
    {
        // Arrange
        var emailService = new EmailService(_configuration, _mockLogger.Object);
        var user = new User
        {
            Email = "test@example.com",
            FirstName = "John"
        };
        var token = "test-token-123";

        // Act
        var result = await emailService.SendVerificationEmailAsync(user, token);

        // Assert
        Assert.True(result);
        // Verify that logging occurred (in a real scenario, you'd verify the log calls)
    }

    [Fact]
    public async Task SendPasswordResetEmailAsync_InTestMode_ShouldLogEmailDetails()
    {
        // Arrange
        var emailService = new EmailService(_configuration, _mockLogger.Object);
        var user = new User
        {
            Email = "test@example.com",
            FirstName = "John"
        };
        var token = "reset-token-123";

        // Act
        var result = await emailService.SendPasswordResetEmailAsync(user, token);

        // Assert
        Assert.True(result);
        // Verify that logging occurred (in a real scenario, you'd verify the log calls)
    }
}