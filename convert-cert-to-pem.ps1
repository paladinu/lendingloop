# Convert PFX certificate to PEM format for Angular
# This script extracts the certificate and private key from the PFX file

Write-Host "=== Converting PFX to PEM Format ===" -ForegroundColor Cyan
Write-Host ""

$pfxPath = "certs/lendingloop-dev.pfx"
$certPemPath = "certs/lendingloop-dev.pem"
$keyPemPath = "certs/lendingloop-dev-key.pem"
$password = "dev-password-2024"

# Check if OpenSSL is available (via Git Bash or direct installation)
$opensslAvailable = $false
try {
    $null = bash -c "openssl version" 2>&1
    $opensslAvailable = $true
    Write-Host "OpenSSL found via bash" -ForegroundColor Green
} catch {
    try {
        $null = openssl version 2>&1
        $opensslAvailable = $true
        Write-Host "OpenSSL found in PATH" -ForegroundColor Green
    } catch {
        Write-Host "OpenSSL not found" -ForegroundColor Yellow
    }
}

if ($opensslAvailable) {
    Write-Host "Converting using OpenSSL..." -ForegroundColor Cyan
    
    # Extract certificate
    try {
        bash -c "openssl pkcs12 -in $pfxPath -out $certPemPath -nokeys -nodes -passin pass:$password" 2>&1 | Out-Null
        Write-Host "Certificate exported to: $certPemPath" -ForegroundColor Green
    } catch {
        Write-Host "Error exporting certificate: $_" -ForegroundColor Red
    }
    
    # Extract private key
    try {
        bash -c "openssl pkcs12 -in $pfxPath -out $keyPemPath -nocerts -nodes -passin pass:$password" 2>&1 | Out-Null
        Write-Host "Private key exported to: $keyPemPath" -ForegroundColor Green
    } catch {
        Write-Host "Error exporting private key: $_" -ForegroundColor Red
    }
    
    Write-Host ""
    Write-Host "Conversion complete! PEM files are ready for Angular." -ForegroundColor Green
} else {
    Write-Host ""
    Write-Host "OpenSSL is required to convert PFX to PEM format." -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Installation options:" -ForegroundColor Cyan
    Write-Host "1. Install Git for Windows (includes OpenSSL): https://git-scm.com/download/win" -ForegroundColor White
    Write-Host "2. Install OpenSSL directly: https://slproweb.com/products/Win32OpenSSL.html" -ForegroundColor White
    Write-Host ""
    Write-Host "After installation, run this script again." -ForegroundColor White
    exit 1
}
