# HTTPS Setup for Local Development

This document provides a quick reference for setting up HTTPS with custom domains for LendingLoop local development.

## Quick Start

### 1. Configure HOSTS File (One-Time)

**IMPORTANT**: Administrator/root privileges are required to edit the HOSTS file.

**HOSTS File Location:**
- Windows: `C:\Windows\System32\drivers\etc\hosts`
- macOS/Linux: `/etc/hosts`

**Windows - Automated (Recommended):**

Run PowerShell as Administrator and execute:
```powershell
.\configure-hosts.ps1
```

This script automatically adds the required entries and verifies the configuration.

**Windows - Manual:**
```powershell
# Run PowerShell as Administrator
Add-Content -Path "C:\Windows\System32\drivers\etc\hosts" -Value "`n127.0.0.1 local-www.lendingloop.com`n127.0.0.1 local-api.lendingloop.com"
```

**macOS/Linux:**
```bash
sudo sh -c 'echo "127.0.0.1 local-www.lendingloop.com" >> /etc/hosts'
sudo sh -c 'echo "127.0.0.1 local-api.lendingloop.com" >> /etc/hosts'
```

**Required Entries:**
```
127.0.0.1 local-www.lendingloop.com
127.0.0.1 local-api.lendingloop.com
```

### 2. Generate SSL Certificates (One-Time)

**Run the certificate generation script:**
```powershell
.\generate-certs.ps1
```

This creates:
- `certs/lendingloop-dev.pfx` - For .NET API
- `certs/lendingloop-dev.cer` - For trusting the certificate

**Certificate Password:** `dev-password-2024`

### 3. Convert to PEM for Angular (One-Time)

If you have OpenSSL:
```bash
openssl pkcs12 -in certs/lendingloop-dev.pfx -out certs/lendingloop-dev.pem -nodes -passin pass:dev-password-2024
openssl pkcs12 -in certs/lendingloop-dev.pfx -out certs/lendingloop-dev-key.pem -nocerts -nodes -passin pass:dev-password-2024
```

### 4. Trust the Certificate (Optional)

**Windows:**
- Double-click `certs/lendingloop-dev.cer`
- Install to "Trusted Root Certification Authorities"

**macOS:**
```bash
sudo security add-trusted-cert -d -r trustRoot -k /Library/Keychains/System.keychain certs/lendingloop-dev.cer
```

**Linux:**
```bash
sudo cp certs/lendingloop-dev.cer /usr/local/share/ca-certificates/lendingloop-dev.crt
sudo update-ca-certificates
```

## Development URLs

- **Frontend:** https://local-www.lendingloop.com
- **Backend:** https://local-api.lendingloop.com

## Browser Security Warnings

If you didn't trust the certificate, your browser will show a security warning. This is normal for self-signed certificates.

**To proceed:**
- Chrome/Edge: Click "Advanced" → "Proceed to local-www.lendingloop.com"
- Firefox: Click "Advanced" → "Accept the Risk and Continue"
- Safari: Click "Show Details" → "visit this website"

## Troubleshooting

**Domain doesn't resolve:**
```bash
# Test DNS resolution
ping local-www.lendingloop.com

# Should return: Reply from 127.0.0.1
```

**Flush DNS cache:**
- Windows: `ipconfig /flushdns`
- macOS: `sudo dscacheutil -flushcache; sudo killall -HUP mDNSResponder`
- Linux: `sudo systemd-resolve --flush-caches`

**Certificate errors:**
- Verify files exist in `certs/` directory
- Check certificate password is `dev-password-2024`
- Regenerate certificates: `.\generate-certs.ps1`

## Files Generated

```
certs/
├── lendingloop-dev.pfx      # .NET API certificate (with private key)
├── lendingloop-dev.cer      # Certificate for trusting
├── lendingloop-dev.pem      # Angular certificate (after conversion)
├── lendingloop-dev-key.pem  # Angular private key (after conversion)
└── README.md                # Detailed certificate documentation
```

## Security Notes

- Self-signed certificates are for **development only**
- Never commit certificates to version control (already in .gitignore)
- Each developer generates their own certificates
- Certificates are valid for 5 years

## Next Steps

After completing this setup:
1. Configure the .NET API to use the PFX certificate
2. Configure Angular to use the PEM certificate
3. Update CORS settings to allow the custom domains
4. Start developing with HTTPS!

For detailed instructions, see the main [README.md](README.md).
