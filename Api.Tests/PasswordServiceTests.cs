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
        //arrange
        var password = "TestPassword123!";

        //act
        var hash = _service.HashPassword(password);

        //assert
        Assert.NotNull(hash);
        Assert.NotEmpty(hash);
    }

    [Fact]
    public void HashPassword_ReturnsDifferentHashes_ForSamePassword()
    {
        //arrange
        var password = "TestPassword123!";

        //act
        var hash1 = _service.HashPassword(password);
        var hash2 = _service.HashPassword(password);

        //assert
        Assert.NotEqual(hash1, hash2); // Different salts should produce different hashes
    }

    [Fact]
    public void VerifyPassword_ReturnsTrue_WhenPasswordMatches()
    {
        //arrange
        var password = "TestPassword123!";
        var hash = _service.HashPassword(password);

        //act
        var result = _service.VerifyPassword(password, hash);

        //assert
        Assert.True(result);
    }

    [Fact]
    public void VerifyPassword_ReturnsFalse_WhenPasswordDoesNotMatch()
    {
        //arrange
        var correctPassword = "TestPassword123!";
        var wrongPassword = "WrongPassword456!";
        var hash = _service.HashPassword(correctPassword);

        //act
        var result = _service.VerifyPassword(wrongPassword, hash);

        //assert
        Assert.False(result);
    }

    [Fact]
    public void VerifyPassword_ReturnsFalse_WhenHashIsInvalid()
    {
        //arrange
        var password = "TestPassword123!";
        var invalidHash = "invalid_hash_string";

        //act
        var result = _service.VerifyPassword(password, invalidHash);

        //assert
        Assert.False(result);
    }

    [Theory]
    [InlineData("short")]
    [InlineData("verylongpasswordthatexceedsreasonablelimits")]
    public void HashPassword_HandlesVariousPasswordLengths_Successfully(string password)
    {
        //arrange & Act
        var hash = _service.HashPassword(password);

        //assert
        Assert.NotNull(hash);
        Assert.NotEmpty(hash);
    }

    [Fact]
    public void HashPassword_ThrowsException_WhenPasswordIsEmpty()
    {
        //arrange
        var password = "";

        //act & Assert
        Assert.Throws<ArgumentException>(() => _service.HashPassword(password));
    }

    [Fact]
    public void HashPassword_ThrowsException_WhenPasswordIsNull()
    {
        //arrange
        string password = null!;

        //act & Assert
        Assert.Throws<ArgumentException>(() => _service.HashPassword(password));
    }

    [Fact]
    public void VerifyPassword_WorksWithSpecialCharacters_InPassword()
    {
        //arrange
        var password = "P@ssw0rd!#$%^&*()_+-=[]{}|;:',.<>?/~`";
        var hash = _service.HashPassword(password);

        //act
        var result = _service.VerifyPassword(password, hash);

        //assert
        Assert.True(result);
    }

    [Fact]
    public void ValidatePassword_ReturnsNoErrors_WhenPasswordMeetsAllRequirements()
    {
        //arrange
        var password = "ValidPass123!";

        //act
        var errors = _service.ValidatePassword(password);

        //assert
        Assert.Empty(errors);
    }

    [Fact]
    public void ValidatePassword_ReturnsError_WhenPasswordIsTooShort()
    {
        //arrange
        var password = "Short1!";

        //act
        var errors = _service.ValidatePassword(password);

        //assert
        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.ErrorMessage!.Contains("at least 8 characters"));
    }

    [Fact]
    public void ValidatePassword_ReturnsError_WhenPasswordLacksLowercase()
    {
        //arrange
        var password = "UPPERCASE123!";

        //act
        var errors = _service.ValidatePassword(password);

        //assert
        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.ErrorMessage!.Contains("lowercase letter"));
    }

    [Fact]
    public void ValidatePassword_ReturnsError_WhenPasswordLacksUppercase()
    {
        //arrange
        var password = "lowercase123!";

        //act
        var errors = _service.ValidatePassword(password);

        //assert
        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.ErrorMessage!.Contains("uppercase letter"));
    }

    [Fact]
    public void ValidatePassword_ReturnsError_WhenPasswordLacksSpecialCharacter()
    {
        //arrange
        var password = "NoSpecial123";

        //act
        var errors = _service.ValidatePassword(password);

        //assert
        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.ErrorMessage!.Contains("special character"));
    }

    [Fact]
    public void ValidatePassword_ReturnsMultipleErrors_WhenPasswordFailsMultipleRequirements()
    {
        //arrange
        var password = "short";

        //act
        var errors = _service.ValidatePassword(password);

        //assert
        Assert.True(errors.Count >= 3); // Should fail length, uppercase, and special character
    }

    [Fact]
    public void ValidatePassword_ReturnsError_WhenPasswordIsEmpty()
    {
        //arrange
        var password = "";

        //act
        var errors = _service.ValidatePassword(password);

        //assert
        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.ErrorMessage!.Contains("required"));
    }

    [Fact]
    public void ValidatePassword_ReturnsError_WhenPasswordIsNull()
    {
        //arrange
        string password = null!;

        //act
        var errors = _service.ValidatePassword(password);

        //assert
        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.ErrorMessage!.Contains("required"));
    }

    [Fact]
    public void GetPasswordStrength_ReturnsZero_WhenPasswordIsEmpty()
    {
        //arrange
        var password = "";

        //act
        var strength = _service.GetPasswordStrength(password);

        //assert
        Assert.Equal(0, strength);
    }

    [Fact]
    public void GetPasswordStrength_ReturnsHigherScore_ForStrongerPassword()
    {
        //arrange
        var weakPassword = "password";
        var strongPassword = "StrongP@ssw0rd123!";

        //act
        var weakScore = _service.GetPasswordStrength(weakPassword);
        var strongScore = _service.GetPasswordStrength(strongPassword);

        //assert
        Assert.True(strongScore > weakScore);
    }

    [Fact]
    public void GetPasswordStrength_ReturnsMaximumFour_ForVeryStrongPassword()
    {
        //arrange
        var veryStrongPassword = "VeryStr0ng!P@ssw0rd#2024";

        //act
        var strength = _service.GetPasswordStrength(veryStrongPassword);

        //assert
        Assert.True(strength <= 4);
    }
}
