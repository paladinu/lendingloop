# Generate Self-Signed SSL Certificates for LendingLoop Development
# This script creates SSL certificates for local development with custom domains

Write-Host "=== LendingLoop Certificate Generation ===" -ForegroundColor Cyan
Write-Host ""

# Check if running as Administrator (recommended but not required)
$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    Write-Host "Note: Not running as Administrator. Certificate will be created in CurrentUser store." -ForegroundColor Yellow
    Write-Host ""
}

# Create certs directory if it doesn't exist
$certsDir = Join-Path $PSScriptRoot "certs"
if (-not (Test-Path $certsDir)) {
    New-Item -ItemType Directory -Path $certsDir -Force | Out-Null
    Write-Host "Created certs directory" -ForegroundColor Green
}

# Certificate parameters
$dnsNames = @("local-www.lendingloop.com", "local-api.lendingloop.com")
$certPassword = "dev-password-2024"
$certStore = if ($isAdmin) { "cert:\LocalMachine\My" } else { "cert:\CurrentUser\My" }

Write-Host "Generating self-signed certificate for:" -ForegroundColor Cyan
foreach ($dns in $dnsNames) {
    Write-Host "  - $dns" -ForegroundColor White
}
Write-Host ""

# Generate the certificate
try {
    $cert = New-SelfSignedCertificate `
        -DnsName $dnsNames `
        -CertStoreLocation $certStore `
        -NotAfter (Get-Date).AddYears(5) `
        -KeyExportPolicy Exportable `
        -KeySpec Signature `
        -KeyLength 2048 `
        -KeyAlgorithm RSA `
        -HashAlgorithm SHA256 `
        -FriendlyName "LendingLoop Development Certificate"
    
    Write-Host "Certificate created successfully!" -ForegroundColor Green
    Write-Host "Thumbprint: $($cert.Thumbprint)" -ForegroundColor Gray
    Write-Host ""
    
    # Export to PFX (for .NET API)
    $pfxPath = Join-Path $certsDir "lendingloop-dev.pfx"
    $securePwd = ConvertTo-SecureString -String $certPassword -Force -AsPlainText
    Export-PfxCertificate -Cert $cert -FilePath $pfxPath -Password $securePwd | Out-Null
    Write-Host "Exported PFX certificate: $pfxPath" -ForegroundColor Green
    
    # Export to CER (public key)
    $cerPath = Join-Path $certsDir "lendingloop-dev.cer"
    Export-Certificate -Cert $cert -FilePath $cerPath | Out-Null
    Write-Host "Exported CER certificate: $cerPath" -ForegroundColor Green
    
    Write-Host ""
    Write-Host "=== Certificate Information ===" -ForegroundColor Cyan
    Write-Host "PFX Password: $certPassword" -ForegroundColor Yellow
    Write-Host "Valid Until: $($cert.NotAfter.ToString('yyyy-MM-dd'))" -ForegroundColor Gray
    Write-Host ""
    
    Write-Host "=== Next Steps ===" -ForegroundColor Cyan
    Write-Host "1. The PFX file is ready for use with the .NET API" -ForegroundColor White
    Write-Host "2. For Angular, you'll need to convert the PFX to PEM format" -ForegroundColor White
    Write-Host "3. If you have OpenSSL installed, run:" -ForegroundColor White
    Write-Host "   openssl pkcs12 -in certs/lendingloop-dev.pfx -out certs/lendingloop-dev.pem -nodes -passin pass:$certPassword" -ForegroundColor Gray
    Write-Host "   openssl pkcs12 -in certs/lendingloop-dev.pfx -out certs/lendingloop-dev-key.pem -nocerts -nodes -passin pass:$certPassword" -ForegroundColor Gray
    Write-Host ""
    Write-Host "4. Trust the certificate (optional, to avoid browser warnings):" -ForegroundColor White
    Write-Host "   - Double-click certs/lendingloop-dev.cer" -ForegroundColor Gray
    Write-Host "   - Click 'Install Certificate'" -ForegroundColor Gray
    Write-Host "   - Choose 'Current User' or 'Local Machine'" -ForegroundColor Gray
    Write-Host "   - Place in 'Trusted Root Certification Authorities'" -ForegroundColor Gray
    Write-Host ""
    
} catch {
    Write-Host "Error generating certificate: $_" -ForegroundColor Red
    exit 1
}

Write-Host "Certificate generation complete!" -ForegroundColor Green
