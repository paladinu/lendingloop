# PowerShell script to drop the old MongoDB index
# This script connects to MongoDB and drops the old name_text index

Write-Host "Dropping old MongoDB index on loops collection..." -ForegroundColor Cyan

# Get MongoDB connection string from appsettings (adjust path if needed)
$appsettingsPath = "api/appsettings.Development.json"

if (Test-Path $appsettingsPath) {
    $appsettings = Get-Content $appsettingsPath | ConvertFrom-Json
    $connectionString = $appsettings.ConnectionStrings.MongoDB
    $databaseName = $appsettings.MongoDB.DatabaseName
    
    Write-Host "Database: $databaseName" -ForegroundColor Yellow
    
    # Run the MongoDB script
    if (Get-Command mongosh -ErrorAction SilentlyContinue) {
        mongosh $connectionString --file drop-old-loop-index.js
    } else {
        Write-Host "mongosh not found. Please install MongoDB Shell or run manually:" -ForegroundColor Red
        Write-Host "  mongosh `"$connectionString`" --eval `"db.loops.dropIndex('name_text')`"" -ForegroundColor Yellow
    }
} else {
    Write-Host "Could not find appsettings file. Please run manually:" -ForegroundColor Red
    Write-Host "  mongosh <your-connection-string> --eval `"db.loops.dropIndex('name_text')`"" -ForegroundColor Yellow
}

Write-Host "`nAfter dropping the index, restart your API to create the new compound text index." -ForegroundColor Green
