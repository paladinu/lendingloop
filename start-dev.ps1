# Start Development Environment Script
# This script starts both the API and UI services

Write-Host "Starting LendingLoop Development Environment..." -ForegroundColor Green

# Check if MongoDB is running (optional check)
Write-Host ""
Write-Host "Note: Make sure MongoDB is running on localhost:27017" -ForegroundColor Yellow

# Start API in a new window
Write-Host ""
Write-Host "Starting API on http://localhost:8080..." -ForegroundColor Cyan
$apiPath = Join-Path $PSScriptRoot "api"
$apiCommand = "cd '$apiPath'; dotnet run"
Start-Process powershell -ArgumentList "-NoExit", "-Command", $apiCommand

# Wait a moment before starting UI
Start-Sleep -Seconds 2

# Check if node_modules exists, if not, install dependencies
if (-not (Test-Path "$PSScriptRoot\ui\node_modules")) {
    Write-Host ""
    Write-Host "Installing UI dependencies (first time only)..." -ForegroundColor Yellow
    Set-Location "$PSScriptRoot\ui"
    npm install
    Set-Location $PSScriptRoot
}

# Start UI in a new window
Write-Host ""
Write-Host "Starting UI on http://localhost:4200..." -ForegroundColor Cyan
$uiPath = Join-Path $PSScriptRoot "ui"
$uiCommand = "cd '$uiPath'; npm start"
Start-Process powershell -ArgumentList "-NoExit", "-Command", $uiCommand

Write-Host ""
Write-Host "Both services are starting in separate windows!" -ForegroundColor Green
Write-Host "  API: http://localhost:8080" -ForegroundColor White
Write-Host "  UI:  http://localhost:4200" -ForegroundColor White
Write-Host ""
Write-Host "Press Ctrl+C to stop this script (services will continue running)" -ForegroundColor Gray
