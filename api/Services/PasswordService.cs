using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using BCrypt.Net;

namespace Api.Services;

public class PasswordService : IPasswordService
{
    private const int MinPasswordLength = 8;
    private const string SpecialCharacters = @"!@#$%^&*()_+-=[]{}|;:,.<>?";
    
    public List<ValidationResult> ValidatePassword(string password)
    {
        var errors = new List<ValidationResult>();
        
        if (string.IsNullOrWhiteSpace(password))
        {
            errors.Add(new ValidationResult("Password is required"));
            return errors;
        }
        
        // Requirement 3.1: At least 8 characters
        if (password.Length < MinPasswordLength)
        {
            errors.Add(new ValidationResult($"Password must be at least {MinPasswordLength} characters long"));
        }
        
        // Requirement 3.2: At least one lowercase letter
        if (!password.Any(char.IsLower))
        {
            errors.Add(new ValidationResult("Password must contain at least one lowercase letter"));
        }
        
        // Requirement 3.3: At least one uppercase letter
        if (!password.Any(char.IsUpper))
        {
            errors.Add(new ValidationResult("Password must contain at least one uppercase letter"));
        }
        
        // Requirement 3.4: At least one special character
        if (!password.Any(c => SpecialCharacters.Contains(c)))
        {
            errors.Add(new ValidationResult("Password must contain at least one special character (!@#$%^&*()_+-=[]{}|;:,.<>?)"));
        }
        
        return errors;
    }
    
    public string HashPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new ArgumentException("Password cannot be null or empty", nameof(password));
        }
        
        // Requirement 7.1: Use secure hashing with salt
        return BCrypt.Net.BCrypt.HashPassword(password, BCrypt.Net.BCrypt.GenerateSalt(12));
    }
    
    public bool VerifyPassword(string password, string hash)
    {
        if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(hash))
        {
            return false;
        }
        
        try
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
        catch
        {
            return false;
        }
    }
    
    public int GetPasswordStrength(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            return 0;
        }
        
        int score = 0;
        
        // Length bonus
        if (password.Length >= 8) score++;
        if (password.Length >= 12) score++;
        
        // Character variety
        if (password.Any(char.IsLower)) score++;
        if (password.Any(char.IsUpper)) score++;
        if (password.Any(char.IsDigit)) score++;
        if (password.Any(c => SpecialCharacters.Contains(c))) score++;
        
        // Complexity patterns
        if (HasMixedCase(password)) score++;
        if (HasNumbersAndLetters(password)) score++;
        
        // Cap at 4 for very strong
        return Math.Min(score / 2, 4);
    }
    
    private static bool HasMixedCase(string password)
    {
        return password.Any(char.IsLower) && password.Any(char.IsUpper);
    }
    
    private static bool HasNumbersAndLetters(string password)
    {
        return password.Any(char.IsDigit) && password.Any(char.IsLetter);
    }
}