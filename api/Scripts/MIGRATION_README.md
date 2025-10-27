# Database Migration Guide

This document explains how to run database migrations for the user authentication system.

## Overview

The migration process handles the transition from the old `ownerId` field to the new `userId` field in the Items collection, creates necessary database indexes, and ensures data integrity.

## Migration Components

### 1. DatabaseMigration.cs
Core migration logic that handles:
- Migrating `ownerId` to `userId` field
- Creating database indexes for performance
- Removing old `ownerId` fields
- Validating migration success

### 2. MigrationController.cs
REST API endpoints for running migrations (enabled only in development):
- `POST /api/migration/run-complete` - Runs the complete migration process
- `POST /api/migration/migrate-owner-to-user` - Migrates ownerId to userId
- `POST /api/migration/create-indexes` - Creates database indexes
- `POST /api/migration/remove-owner-id` - Removes ownerId fields
- `GET /api/migration/validate` - Validates migration status

### 3. MigrationConsole.cs
Standalone console utility for running migrations outside of the web API.

### 4. RunMigration.cs
Wrapper utility that calls the MigrationConsole for backwards compatibility.

## Running Migrations

### Method 1: Console Application
```bash
# Navigate to the API directory
cd api

# Build the project first
dotnet build

# Run the migration using the console utility
# Note: This method requires creating a separate console app that calls MigrationConsole.RunMigrationConsole()
```

### Method 2: API Endpoints (Development Only)
```bash
# Run complete migration
curl -X POST http://localhost:5000/api/migration/run-complete

# Validate migration
curl -X GET http://localhost:5000/api/migration/validate
```

### Method 3: Auto-run on Startup
Set `Migration:AutoRunOnStartup` to `true` in appsettings.json to automatically run migrations when the application starts.

## Configuration

### appsettings.json
```json
{
  "Migration": {
    "AutoRunOnStartup": "false",
    "EnableMigrationEndpoints": "false"
  }
}
```

### appsettings.Development.json
```json
{
  "Migration": {
    "AutoRunOnStartup": "false",
    "EnableMigrationEndpoints": "true"
  }
}
```

## Migration Process

1. **Migrate ownerId to userId**: Finds all items with `ownerId` but no `userId` and copies the value
2. **Create Indexes**: Creates performance indexes on:
   - `users.email` (unique)
   - `users.emailVerificationToken`
   - `items.userId`
   - `items.userId + items.isAvailable` (compound)
3. **Remove ownerId**: Removes the old `ownerId` field from all items
4. **Validate**: Ensures all items have `userId` and no items have `ownerId`

## Safety Features

- **Validation**: Each step includes validation to ensure data integrity
- **Logging**: Comprehensive logging of all migration operations
- **Error Handling**: Graceful error handling with detailed error messages
- **Idempotent**: Safe to run multiple times without data corruption

## Troubleshooting

### Common Issues

1. **Connection String Not Found**
   - Ensure MongoDB connection string is configured in appsettings.json
   - Check that the database name is specified

2. **Index Creation Fails**
   - May indicate duplicate data that violates unique constraints
   - Check for duplicate email addresses in users collection

3. **Migration Validation Fails**
   - Run individual migration steps to identify the issue
   - Check database manually for data inconsistencies

### Manual Verification

You can manually verify the migration using MongoDB commands:

```javascript
// Check for items without userId
db.items.countDocuments({ userId: { $exists: false } })

// Check for items with ownerId
db.items.countDocuments({ ownerId: { $exists: true } })

// List all indexes
db.users.getIndexes()
db.items.getIndexes()
```

## Security Considerations

- Migration endpoints are only enabled in development environment
- Production deployments should use the console application method
- Always backup your database before running migrations
- Test migrations on a copy of production data first