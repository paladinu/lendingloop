using Api.Models;

namespace Api.Services;

public interface IUserService
{
    Task<User?> GetUserByEmailAsync(string email);
    Task<User?> GetUserByIdAsync(string id);
    Task<User> CreateUserAsync(User user);
    Task<User?> UpdateUserAsync(string id, User user);
    Task<bool> DeleteUserAsync(string id);
    Task<User?> VerifyEmailAsync(string token);
    Task EnsureIndexesAsync();
}
