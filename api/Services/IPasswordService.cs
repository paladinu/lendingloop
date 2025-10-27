using System.ComponentModel.DataAnnotations;

namespace Api.Services;

public interface IPasswordService
{
    /// <summary>
    /// Validates a password against the security policy
    /// </summary>
    /// <param name="password">The password to validate</param>
    /// <returns>List of validation errors, empty if password is valid</returns>
    List<ValidationResult> ValidatePassword(string password);
    
    /// <summary>
    /// Hashes a password using BCrypt
    /// </summary>
    /// <param name="password">The plain text password</param>
    /// <returns>The hashed password</returns>
    string HashPassword(string password);
    
    /// <summary>
    /// Verifies a password against its hash
    /// </summary>
    /// <param name="password">The plain text password</param>
    /// <param name="hash">The hashed password</param>
    /// <returns>True if password matches hash</returns>
    bool VerifyPassword(string password, string hash);
    
    /// <summary>
    /// Gets the password strength score (0-4)
    /// </summary>
    /// <param name="password">The password to evaluate</param>
    /// <returns>Strength score from 0 (weak) to 4 (very strong)</returns>
    int GetPasswordStrength(string password);
}