using Api.Models;
using MongoDB.Driver;

namespace Api.Services;

public class UserService : IUserService
{
    private readonly IMongoCollection<User> _usersCollection;

    public UserService(IMongoDatabase database, IConfiguration configuration)
    {
        var collectionName = configuration["MongoDB:UsersCollectionName"] ?? "users";
        _usersCollection = database.GetCollection<User>(collectionName);
        
        // Ensure indexes are created when service is instantiated
        _ = Task.Run(EnsureIndexesAsync);
    }

    public async Task<User?> GetUserByEmailAsync(string email)
    {
        return await _usersCollection.Find(u => u.Email == email).FirstOrDefaultAsync();
    }

    public async Task<User?> GetUserByIdAsync(string id)
    {
        return await _usersCollection.Find(u => u.Id == id).FirstOrDefaultAsync();
    }

    public async Task<User> CreateUserAsync(User user)
    {
        user.CreatedAt = DateTime.UtcNow;
        user.UpdatedAt = DateTime.UtcNow;
        await _usersCollection.InsertOneAsync(user);
        return user;
    }

    public async Task<User?> UpdateUserAsync(string id, User user)
    {
        user.UpdatedAt = DateTime.UtcNow;
        var filter = Builders<User>.Filter.Eq(u => u.Id, id);
        var options = new ReplaceOptions { IsUpsert = false };
        
        var result = await _usersCollection.ReplaceOneAsync(filter, user, options);
        return result.ModifiedCount > 0 ? user : null;
    }

    public async Task<bool> DeleteUserAsync(string id)
    {
        var result = await _usersCollection.DeleteOneAsync(u => u.Id == id);
        return result.DeletedCount > 0;
    }

    public async Task<User?> VerifyEmailAsync(string token)
    {
        var filter = Builders<User>.Filter.And(
            Builders<User>.Filter.Eq(u => u.EmailVerificationToken, token),
            Builders<User>.Filter.Gt(u => u.EmailVerificationExpiry, DateTime.UtcNow)
        );
        
        var update = Builders<User>.Update
            .Set(u => u.IsEmailVerified, true)
            .Unset(u => u.EmailVerificationToken)
            .Unset(u => u.EmailVerificationExpiry)
            .Set(u => u.UpdatedAt, DateTime.UtcNow);
        
        var options = new FindOneAndUpdateOptions<User>
        {
            ReturnDocument = ReturnDocument.After
        };

        return await _usersCollection.FindOneAndUpdateAsync(filter, update, options);
    }

    public async Task EnsureIndexesAsync()
    {
        try
        {
            // Skip index creation if collection is not initialized (e.g., in test scenarios)
            if (_usersCollection == null || _usersCollection.Database == null)
            {
                return;
            }

            // Verify database connection before creating indexes
            try
            {
                await _usersCollection.Database.ListCollectionNamesAsync();
            }
            catch
            {
                // Database not accessible, skip index creation
                return;
            }

            // Create unique index on email field
            var emailIndexKeys = Builders<User>.IndexKeys.Ascending(u => u.Email);
            var emailIndexOptions = new CreateIndexOptions { Unique = true };
            var emailIndexModel = new CreateIndexModel<User>(emailIndexKeys, emailIndexOptions);

            // Create index on email verification token for faster lookups
            var tokenIndexKeys = Builders<User>.IndexKeys.Ascending(u => u.EmailVerificationToken);
            var tokenIndexModel = new CreateIndexModel<User>(tokenIndexKeys);

            // Create compound index on verification token and expiry
            var verificationIndexKeys = Builders<User>.IndexKeys
                .Ascending(u => u.EmailVerificationToken)
                .Ascending(u => u.EmailVerificationExpiry);
            var verificationIndexModel = new CreateIndexModel<User>(verificationIndexKeys);

            await _usersCollection.Indexes.CreateManyAsync(new[]
            {
                emailIndexModel,
                tokenIndexModel,
                verificationIndexModel
            });
        }
        catch (Exception ex)
        {
            // Log the exception but don't fail the application startup
            Console.WriteLine($"Warning: Could not create indexes for Users collection: {ex.Message}");
        }
    }
}