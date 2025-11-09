# Configure Windows HOSTS file for LendingLoop local development
# This script adds custom domain entries to the Windows HOSTS file
# 
# IMPORTANT: This script must be run as Administrator
#
# Usage: Run PowerShell as Administrator, then execute:
#   .\configure-hosts.ps1

# Check if running as Administrator
$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)

if (-not $isAdmin) {
    Write-Host "ERROR: This script must be run as Administrator!" -ForegroundColor Red
    Write-Host ""
    Write-Host "To run as Administrator:" -ForegroundColor Yellow
    Write-Host "1. Right-click on PowerShell" -ForegroundColor Yellow
    Write-Host "2. Select 'Run as Administrator'" -ForegroundColor Yellow
    Write-Host "3. Navigate to this directory and run: .\configure-hosts.ps1" -ForegroundColor Yellow
    Write-Host ""
    exit 1
}

# HOSTS file location
$hostsPath = "C:\Windows\System32\drivers\etc\hosts"

# Domain entries to add
$entries = @(
    "127.0.0.1 local-www.lendingloop.com",
    "127.0.0.1 local-api.lendingloop.com"
)

Write-Host "Configuring Windows HOSTS file for LendingLoop..." -ForegroundColor Cyan
Write-Host "HOSTS file location: $hostsPath" -ForegroundColor Gray
Write-Host ""

# Read current HOSTS file content
$hostsContent = Get-Content $hostsPath -Raw

# Track if any changes were made
$changesMade = $false

# Check and add each entry
foreach ($entry in $entries) {
    $domain = $entry.Split(" ")[1]
    
    # Check if entry already exists (case-insensitive)
    if ($hostsContent -match "(?i)127\.0\.0\.1\s+$domain") {
        Write-Host "Already configured: $domain" -ForegroundColor Green
    } else {
        # Add entry to HOSTS file
        Add-Content -Path $hostsPath -Value $entry
        Write-Host "Added: $entry" -ForegroundColor Yellow
        $changesMade = $true
    }
}

Write-Host ""

if ($changesMade) {
    Write-Host "HOSTS file updated successfully!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Flushing DNS cache..." -ForegroundColor Cyan
    ipconfig /flushdns | Out-Null
    Write-Host "DNS cache flushed" -ForegroundColor Green
} else {
    Write-Host "No changes needed - all entries already exist." -ForegroundColor Green
}

Write-Host ""
Write-Host "Verifying configuration..." -ForegroundColor Cyan

# Test DNS resolution
foreach ($entry in $entries) {
    $domain = $entry.Split(" ")[1]
    $pingResult = Test-Connection -ComputerName $domain -Count 1 -Quiet -ErrorAction SilentlyContinue
    if ($pingResult) {
        Write-Host "$domain resolves correctly" -ForegroundColor Green
    } else {
        Write-Host "$domain may not be resolving correctly (this may be normal if services aren't running)" -ForegroundColor Yellow
    }
}

Write-Host ""
Write-Host "Configuration complete!" -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "1. Generate SSL certificates: .\generate-certs.ps1" -ForegroundColor White
Write-Host "2. Start MongoDB: Ensure MongoDB is running on localhost:27017" -ForegroundColor White
Write-Host "3. Start the API: cd api && dotnet run" -ForegroundColor White
Write-Host "4. Start the UI: cd ui && ng serve" -ForegroundColor White
Write-Host "5. Open browser: https://local-www.lendingloop.com" -ForegroundColor White
Write-Host ""
