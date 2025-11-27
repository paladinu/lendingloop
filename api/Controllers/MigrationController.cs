using Microsoft.AspNetCore.Mvc;
using Api.Scripts;
using MongoDB.Driver;
using Microsoft.Extensions.Logging;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
#if DEBUG
public class MigrationController : ControllerBase
#else
internal class MigrationController : ControllerBase
#endif
{
    private readonly DatabaseMigration _migration;
    private readonly ILogger<MigrationController> _logger;

    public MigrationController(IMongoDatabase database, ILogger<MigrationController> logger, ILoggerFactory loggerFactory)
    {
        var migrationLogger = loggerFactory.CreateLogger<DatabaseMigration>();
        _migration = new DatabaseMigration(database, migrationLogger);
        _logger = logger;
    }

    /// <summary>
    /// Runs the complete database migration process
    /// </summary>
    [HttpPost("run-complete")]
    public async Task<IActionResult> RunCompleteMigration()
    {
        try
        {
            await _migration.RunCompleteMigration();
            return Ok(new { message = "Database migration completed successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to run complete migration");
            return StatusCode(500, new { message = "Migration failed", error = ex.Message });
        }
    }

    /// <summary>
    /// Migrates ownerId to userId field
    /// </summary>
    [HttpPost("migrate-owner-to-user")]
    public async Task<IActionResult> MigrateOwnerIdToUserId()
    {
        try
        {
            await _migration.MigrateItemsOwnerIdToUserId();
            return Ok(new { message = "Successfully migrated ownerId to userId" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to migrate ownerId to userId");
            return StatusCode(500, new { message = "Migration failed", error = ex.Message });
        }
    }

    /// <summary>
    /// Creates database indexes
    /// </summary>
    [HttpPost("create-indexes")]
    public async Task<IActionResult> CreateIndexes()
    {
        try
        {
            await _migration.CreateDatabaseIndexes();
            return Ok(new { message = "Database indexes created successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create database indexes");
            return StatusCode(500, new { message = "Index creation failed", error = ex.Message });
        }
    }

    /// <summary>
    /// Removes ownerId field from items
    /// </summary>
    [HttpPost("remove-owner-id")]
    public async Task<IActionResult> RemoveOwnerIdField()
    {
        try
        {
            await _migration.RemoveOwnerIdField();
            return Ok(new { message = "Successfully removed ownerId field" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove ownerId field");
            return StatusCode(500, new { message = "Field removal failed", error = ex.Message });
        }
    }

    /// <summary>
    /// Validates the migration status
    /// </summary>
    [HttpGet("validate")]
    public async Task<IActionResult> ValidateMigration()
    {
        try
        {
            var isValid = await _migration.ValidateMigration();
            return Ok(new { isValid, message = isValid ? "Migration is valid" : "Migration validation failed" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate migration");
            return StatusCode(500, new { message = "Validation failed", error = ex.Message });
        }
    }

    /// <summary>
    /// Runs the tags migration process
    /// </summary>
    [HttpPost("run-tags-migration")]
    public async Task<IActionResult> RunTagsMigration()
    {
        try
        {
            await _migration.RunTagsMigration();
            return Ok(new { message = "Tags migration completed successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to run tags migration");
            return StatusCode(500, new { message = "Tags migration failed", error = ex.Message });
        }
    }

    /// <summary>
    /// Adds tags field to existing items
    /// </summary>
    [HttpPost("add-tags-field")]
    public async Task<IActionResult> AddTagsField()
    {
        try
        {
            await _migration.AddTagsFieldToItems();
            return Ok(new { message = "Successfully added tags field to items" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add tags field");
            return StatusCode(500, new { message = "Tags field addition failed", error = ex.Message });
        }
    }

    /// <summary>
    /// Creates indexes for tags
    /// </summary>
    [HttpPost("create-tags-indexes")]
    public async Task<IActionResult> CreateTagsIndexes()
    {
        try
        {
            await _migration.CreateTagsIndexes();
            return Ok(new { message = "Tags indexes created successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create tags indexes");
            return StatusCode(500, new { message = "Tags index creation failed", error = ex.Message });
        }
    }
}