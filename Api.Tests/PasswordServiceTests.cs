using Api.Services;
using Xunit;

namespace Api.Tests;

public class PasswordServiceTests
{
    private readonly PasswordService _service;

    public PasswordServiceTests()
    {
        _service = new PasswordService();
    }

    [Fact]
    public void HashPassword_ReturnsNonEmptyHash_WhenPasswordProvided()
    {
        // Arrange
        var password = "TestPassword123!";

        // Act
        var hash = _service.HashPassword(password);

        // Assert
        Assert.NotNull(hash);
        Assert.NotEmpty(hash);
    }

    [Fact]
    public void HashPassword_ReturnsDifferentHashes_ForSamePassword()
    {
        // Arrange
        var password = "TestPassword123!";

        // Act
        var hash1 = _service.HashPassword(password);
        var hash2 = _service.HashPassword(password);

        // Assert
        Assert.NotEqual(hash1, hash2); // Different salts should produce different hashes
    }

    [Fact]
    public void VerifyPassword_ReturnsTrue_WhenPasswordMatches()
    {
        // Arrange
        var password = "TestPassword123!";
        var hash = _service.HashPassword(password);

        // Act
        var result = _service.VerifyPassword(password, hash);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void VerifyPassword_ReturnsFalse_WhenPasswordDoesNotMatch()
    {
        // Arrange
        var correctPassword = "TestPassword123!";
        var wrongPassword = "WrongPassword456!";
        var hash = _service.HashPassword(correctPassword);

        // Act
        var result = _service.VerifyPassword(wrongPassword, hash);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void VerifyPassword_ReturnsFalse_WhenHashIsInvalid()
    {
        // Arrange
        var password = "TestPassword123!";
        var invalidHash = "invalid_hash_string";

        // Act
        var result = _service.VerifyPassword(password, invalidHash);

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData("short")]
    [InlineData("verylongpasswordthatexceedsreasonablelimits")]
    public void HashPassword_HandlesVariousPasswordLengths_Successfully(string password)
    {
        // Arrange & Act
        var hash = _service.HashPassword(password);

        // Assert
        Assert.NotNull(hash);
        Assert.NotEmpty(hash);
    }

    [Fact]
    public void HashPassword_ThrowsException_WhenPasswordIsEmpty()
    {
        // Arrange
        var password = "";

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _service.HashPassword(password));
    }

    [Fact]
    public void HashPassword_ThrowsException_WhenPasswordIsNull()
    {
        // Arrange
        string password = null!;

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _service.HashPassword(password));
    }

    [Fact]
    public void VerifyPassword_WorksWithSpecialCharacters_InPassword()
    {
        // Arrange
        var password = "P@ssw0rd!#$%^&*()_+-=[]{}|;:',.<>?/~`";
        var hash = _service.HashPassword(password);

        // Act
        var result = _service.VerifyPassword(password, hash);

        // Assert
        Assert.True(result);
    }
}
