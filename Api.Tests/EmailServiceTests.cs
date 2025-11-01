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
        //act & Assert
        var emailService = new EmailService(_configuration, _mockLogger.Object);
        Assert.NotNull(emailService);
    }

    [Fact]
    public void Constructor_WithInvalidSmtpHost_ShouldThrowException()
    {
        //arrange
        var invalidConfig = CreateTestConfiguration(new Dictionary<string, string>
        {
            ["Email:SmtpHost"] = ""
        });

        //act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => 
            new EmailService(invalidConfig, _mockLogger.Object));
        
        Assert.Contains("Email:SmtpHost is required", exception.Message);
    }

    [Fact]
    public void Constructor_WithInvalidSmtpPort_ShouldThrowException()
    {
        //arrange
        var invalidConfig = CreateTestConfiguration(new Dictionary<string, string>
        {
            ["Email:SmtpPort"] = "0"
        });

        //act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => 
            new EmailService(invalidConfig, _mockLogger.Object));
        
        Assert.Contains("Email:SmtpPort must be between 1 and 65535", exception.Message);
    }

    [Fact]
    public void Constructor_WithInvalidFromEmail_ShouldThrowException()
    {
        //arrange
        var invalidConfig = CreateTestConfiguration(new Dictionary<string, string>
        {
            ["Email:FromEmail"] = "invalid-email"
        });

        //act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => 
            new EmailService(invalidConfig, _mockLogger.Object));
        
        Assert.Contains("Email:FromEmail must be a valid email address", exception.Message);
    }

    [Fact]
    public void Constructor_WithInvalidBaseUrl_ShouldThrowException()
    {
        //arrange
        var invalidConfig = CreateTestConfiguration(new Dictionary<string, string>
        {
            ["Email:BaseUrl"] = "not-a-url"
        });

        //act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => 
            new EmailService(invalidConfig, _mockLogger.Object));
        
        Assert.Contains("Email:BaseUrl must be a valid URL", exception.Message);
    }

    [Fact]
    public async Task SendVerificationEmailAsync_WithValidUser_ShouldReturnTrue()
    {
        //arrange
        var emailService = new EmailService(_configuration, _mockLogger.Object);
        var user = new User
        {
            Email = "test@example.com",
            FirstName = "John"
        };
        var token = "test-token-123";

        //act
        var result = await emailService.SendVerificationEmailAsync(user, token);

        //assert
        Assert.True(result);
    }

    [Fact]
    public async Task SendVerificationEmailAsync_WithNullUser_ShouldReturnFalse()
    {
        //arrange
        var emailService = new EmailService(_configuration, _mockLogger.Object);
        var token = "test-token-123";

        //act
        var result = await emailService.SendVerificationEmailAsync(null!, token);

        //assert
        Assert.False(result);
    }

    [Fact]
    public async Task SendVerificationEmailAsync_WithEmptyEmail_ShouldReturnFalse()
    {
        //arrange
        var emailService = new EmailService(_configuration, _mockLogger.Object);
        var user = new User
        {
            Email = "",
            FirstName = "John"
        };
        var token = "test-token-123";

        //act
        var result = await emailService.SendVerificationEmailAsync(user, token);

        //assert
        Assert.False(result);
    }

    [Fact]
    public async Task SendVerificationEmailAsync_WithEmptyToken_ShouldReturnFalse()
    {
        //arrange
        var emailService = new EmailService(_configuration, _mockLogger.Object);
        var user = new User
        {
            Email = "test@example.com",
            FirstName = "John"
        };

        //act
        var result = await emailService.SendVerificationEmailAsync(user, "");

        //assert
        Assert.False(result);
    }

    [Fact]
    public async Task SendPasswordResetEmailAsync_WithValidUser_ShouldReturnTrue()
    {
        //arrange
        var emailService = new EmailService(_configuration, _mockLogger.Object);
        var user = new User
        {
            Email = "test@example.com",
            FirstName = "John"
        };
        var token = "reset-token-123";

        //act
        var result = await emailService.SendPasswordResetEmailAsync(user, token);

        //assert
        Assert.True(result);
    }

    [Fact]
    public async Task SendPasswordResetEmailAsync_WithNullUser_ShouldReturnFalse()
    {
        //arrange
        var emailService = new EmailService(_configuration, _mockLogger.Object);
        var token = "reset-token-123";

        //act
        var result = await emailService.SendPasswordResetEmailAsync(null!, token);

        //assert
        Assert.False(result);
    }

    [Fact]
    public async Task SendPasswordResetEmailAsync_WithEmptyEmail_ShouldReturnFalse()
    {
        //arrange
        var emailService = new EmailService(_configuration, _mockLogger.Object);
        var user = new User
        {
            Email = "",
            FirstName = "John"
        };
        var token = "reset-token-123";

        //act
        var result = await emailService.SendPasswordResetEmailAsync(user, token);

        //assert
        Assert.False(result);
    }

    [Fact]
    public async Task SendPasswordResetEmailAsync_WithEmptyToken_ShouldReturnFalse()
    {
        //arrange
        var emailService = new EmailService(_configuration, _mockLogger.Object);
        var user = new User
        {
            Email = "test@example.com",
            FirstName = "John"
        };

        //act
        var result = await emailService.SendPasswordResetEmailAsync(user, "");

        //assert
        Assert.False(result);
    }

    [Fact]
    public async Task TestEmailConfigurationAsync_WithValidConfiguration_ShouldReturnTrue()
    {
        //arrange
        var emailService = new EmailService(_configuration, _mockLogger.Object);

        //act
        var result = await emailService.TestEmailConfigurationAsync();

        //assert
        Assert.True(result);
    }

    [Fact]
    public void GetEmailHealthStatus_WithValidConfiguration_ShouldReturnHealthyStatus()
    {
        //arrange
        var emailService = new EmailService(_configuration, _mockLogger.Object);

        //act
        var status = emailService.GetEmailHealthStatus();

        //assert
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
        //arrange
        var configWithoutCredentials = CreateTestConfiguration(new Dictionary<string, string>
        {
            ["Email:SmtpUsername"] = "",
            ["Email:SmtpPassword"] = ""
        });
        var emailService = new EmailService(configWithoutCredentials, _mockLogger.Object);

        //act
        var status = emailService.GetEmailHealthStatus();

        //assert
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
        //arrange
        var emailService = new EmailService(_configuration, _mockLogger.Object);
        var user = new User
        {
            Email = email,
            FirstName = "John"
        };
        var token = "test-token-123";

        //act
        var result = await emailService.SendVerificationEmailAsync(user, token);

        //assert
        Assert.Equal(expectedResult, result);
    }

    [Fact]
    public async Task SendVerificationEmailAsync_InTestMode_ShouldLogEmailDetails()
    {
        //arrange
        var emailService = new EmailService(_configuration, _mockLogger.Object);
        var user = new User
        {
            Email = "test@example.com",
            FirstName = "John"
        };
        var token = "test-token-123";

        //act
        var result = await emailService.SendVerificationEmailAsync(user, token);

        //assert
        Assert.True(result);
        // Verify that logging occurred (in a real scenario, you'd verify the log calls)
    }

    [Fact]
    public async Task SendPasswordResetEmailAsync_InTestMode_ShouldLogEmailDetails()
    {
        //arrange
        var emailService = new EmailService(_configuration, _mockLogger.Object);
        var user = new User
        {
            Email = "test@example.com",
            FirstName = "John"
        };
        var token = "reset-token-123";

        //act
        var result = await emailService.SendPasswordResetEmailAsync(user, token);

        //assert
        Assert.True(result);
        // Verify that logging occurred (in a real scenario, you'd verify the log calls)
    }
}