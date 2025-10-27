using MongoDB.Driver;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Api.Scripts;

namespace Api.Scripts;

/// <summary>
/// Standalone console application for running database migrations
/// This can be compiled as a separate executable or called from external tools
/// </summary>
public class MigrationConsole
{
    public static async Task<int> RunMigrationConsole(string[] args)
    {
        try
        {
            // Build configuration
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false)
                .AddJsonFile("appsettings.Development.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            // Setup logging
            using var loggerFactory = LoggerFactory.Create(builder =>
                builder.AddConsole().SetMinimumLevel(LogLevel.Information));
            var logger = loggerFactory.CreateLogger<DatabaseMigration>();

            // Setup MongoDB connection
            var mongoConnectionString = configuration.GetConnectionString("MongoDB") 
                ?? configuration["MongoDB:ConnectionString"];
            var mongoDatabaseName = configuration["MongoDB:DatabaseName"];

            if (string.IsNullOrEmpty(mongoConnectionString) || string.IsNullOrEmpty(mongoDatabaseName))
            {
                Console.WriteLine("Error: MongoDB connection string or database name not configured");
                return 1;
            }

            var mongoClient = new MongoClient(mongoConnectionString);
            var database = mongoClient.GetDatabase(mongoDatabaseName);

            // Run migration
            var migration = new DatabaseMigration(database, logger);
            
            Console.WriteLine("Starting database migration...");
            await migration.RunCompleteMigration();
            Console.WriteLine("Database migration completed successfully!");
            
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Migration failed: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            return 1;
        }
    }
}