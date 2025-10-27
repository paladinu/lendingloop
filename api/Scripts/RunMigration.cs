using MongoDB.Driver;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Api.Scripts;

namespace Api.Scripts;

/// <summary>
/// Utility class for running database migrations
/// Usage: Call RunDatabaseMigration() method from external console app
/// </summary>
public class RunMigration
{
    public static async Task RunMigrationCommand(string[] args)
    {
        if (args.Length > 0 && args[0] == "migrate")
        {
            await RunDatabaseMigration();
        }
        else
        {
            Console.WriteLine("Usage: dotnet run --project api -- migrate");
        }
    }

    private static async Task RunDatabaseMigration()
    {
        var exitCode = await MigrationConsole.RunMigrationConsole(new string[] { "migrate" });
        if (exitCode != 0)
        {
            Environment.Exit(exitCode);
        }
    }
}