# Development helper script to verify a user's email
# Usage: .\verify-user.ps1 -Email "user@example.com"

param(
    [Parameter(Mandatory=$true)]
    [string]$Email,
    
    [Parameter(Mandatory=$false)]
    [string]$ApiUrl = "http://localhost:8080"
)

Write-Host "Checking user status for: $Email" -ForegroundColor Cyan

# Check user status
$statusUrl = "$ApiUrl/api/auth/dev/user-status/$Email"
try {
    $status = Invoke-RestMethod -Uri $statusUrl -Method Get
    Write-Host "`nUser Status:" -ForegroundColor Green
    Write-Host "  Email: $($status.email)"
    Write-Host "  Name: $($status.firstName) $($status.lastName)"
    Write-Host "  Email Verified: $($status.isEmailVerified)"
    Write-Host "  Has Verification Token: $($status.hasVerificationToken)"
    Write-Host "  Created At: $($status.createdAt)"
    
    if ($status.isEmailVerified) {
        Write-Host "`nUser is already verified!" -ForegroundColor Green
        exit 0
    }
} catch {
    Write-Host "Error checking user status: $_" -ForegroundColor Red
    Write-Host "User may not exist or API is not running" -ForegroundColor Yellow
    exit 1
}

Write-Host "`nVerifying user email..." -ForegroundColor Cyan

# Verify user
$verifyUrl = "$ApiUrl/api/auth/dev/verify-user"
$body = @{
    email = $Email
} | ConvertTo-Json

try {
    $result = Invoke-RestMethod -Uri $verifyUrl -Method Post -Body $body -ContentType "application/json"
    Write-Host "`nSuccess!" -ForegroundColor Green
    Write-Host "  $($result.message)"
    Write-Host "  Email Verified: $($result.user.isEmailVerified)"
    Write-Host "`nYou can now log in with this account." -ForegroundColor Green
} catch {
    Write-Host "Error verifying user: $_" -ForegroundColor Red
    exit 1
}
