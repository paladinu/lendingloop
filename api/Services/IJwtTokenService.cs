using Api.Models;
using System.Security.Claims;

namespace Api.Services;

public interface IJwtTokenService
{
    /// <summary>
    /// Generates a JWT token for the specified user
    /// </summary>
    /// <param name="user">The user to generate token for</param>
    /// <returns>JWT token string</returns>
    string GenerateToken(User user);
    
    /// <summary>
    /// Validates a JWT token and returns the claims principal
    /// </summary>
    /// <param name="token">The JWT token to validate</param>
    /// <returns>Claims principal if valid, null if invalid</returns>
    ClaimsPrincipal? ValidateToken(string token);
    
    /// <summary>
    /// Gets the user ID from a JWT token
    /// </summary>
    /// <param name="token">The JWT token</param>
    /// <returns>User ID if valid token, null otherwise</returns>
    string? GetUserIdFromToken(string token);
    
    /// <summary>
    /// Gets the token expiration time
    /// </summary>
    /// <returns>Token expiration time in hours</returns>
    int GetTokenExpirationHours();
}