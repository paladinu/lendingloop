# HOSTS File Configuration Guide

This guide explains how to configure your system's HOSTS file to enable custom local domains for LendingLoop development.

## Overview

LendingLoop uses custom local domain names instead of `localhost` to provide a production-like development environment:
- **Frontend (UI)**: https://local-www.lendingloop.com
- **Backend (API)**: https://local-api.lendingloop.com

These domains must be mapped to `127.0.0.1` (localhost) in your system's HOSTS file.

## Why Custom Domains?

Using custom domains provides several benefits:
- Simulates production environment where frontend and backend are on separate domains
- Enables proper CORS testing
- Allows HTTPS with self-signed certificates
- Avoids cookie and authentication issues that can occur with `localhost`
- Provides consistent development experience across team members

## HOSTS File Location

The HOSTS file location varies by operating system:

- **Windows**: `C:\Windows\System32\drivers\etc\hosts`
- **macOS**: `/etc/hosts`
- **Linux**: `/etc/hosts`

## Administrator Privileges Required

**IMPORTANT**: Editing the HOSTS file requires administrator or root privileges because it's a system-level configuration file that affects network name resolution.

### Windows
- Right-click PowerShell or Command Prompt
- Select "Run as Administrator"
- You'll see a User Account Control (UAC) prompt - click "Yes"

### macOS/Linux
- Use `sudo` before commands to run with elevated privileges
- You'll be prompted for your password

## Configuration Methods

### Method 1: Automated Script (Windows - Recommended)

We provide a PowerShell script that automates the entire process:

```powershell
# 1. Right-click PowerShell and select "Run as Administrator"
# 2. Navigate to the project directory
# 3. Run the script:
.\configure-hosts.ps1
```

**What the script does:**
- ✓ Checks if you're running as Administrator
- ✓ Adds required domain entries to HOSTS file
- ✓ Skips entries that already exist (safe to run multiple times)
- ✓ Flushes DNS cache automatically
- ✓ Verifies the configuration
- ✓ Provides clear feedback and next steps

### Method 2: Manual Command (Windows)

Run PowerShell as Administrator and execute:

```powershell
Add-Content -Path "C:\Windows\System32\drivers\etc\hosts" -Value "`n127.0.0.1 local-www.lendingloop.com`n127.0.0.1 local-api.lendingloop.com"
```

Then flush DNS cache:
```powershell
ipconfig /flushdns
```

### Method 3: Manual File Edit (Windows)

1. Open Notepad as Administrator:
   - Press Windows key
   - Type "Notepad"
   - Right-click on Notepad
   - Select "Run as Administrator"

2. Open the HOSTS file:
   - Click File → Open
   - Navigate to: `C:\Windows\System32\drivers\etc`
   - Change file filter to "All Files (*.*)"
   - Select `hosts` file and click Open

3. Add these lines at the end of the file:
   ```
   127.0.0.1 local-www.lendingloop.com
   127.0.0.1 local-api.lendingloop.com
   ```

4. Save the file (Ctrl+S)

5. Flush DNS cache:
   - Open Command Prompt as Administrator
   - Run: `ipconfig /flushdns`

### Method 4: macOS/Linux

Run in terminal:

```bash
# Add entries
sudo sh -c 'echo "127.0.0.1 local-www.lendingloop.com" >> /etc/hosts'
sudo sh -c 'echo "127.0.0.1 local-api.lendingloop.com" >> /etc/hosts'

# Flush DNS cache
# macOS:
sudo dscacheutil -flushcache; sudo killall -HUP mDNSResponder

# Linux:
sudo systemd-resolve --flush-caches
```

Or edit manually:
```bash
sudo nano /etc/hosts
# Add the two lines, save (Ctrl+O, Enter) and exit (Ctrl+X)
```

## Verification

After configuring the HOSTS file, verify the setup:

### Test DNS Resolution

**Windows:**
```powershell
ping local-www.lendingloop.com
ping local-api.lendingloop.com
```

**macOS/Linux:**
```bash
ping -c 1 local-www.lendingloop.com
ping -c 1 local-api.lendingloop.com
```

**Expected output:**
```
Reply from 127.0.0.1: bytes=32 time<1ms TTL=128
```

### View HOSTS File Contents

**Windows:**
```powershell
Get-Content C:\Windows\System32\drivers\etc\hosts
```

**macOS/Linux:**
```bash
cat /etc/hosts
```

Look for these lines:
```
127.0.0.1 local-www.lendingloop.com
127.0.0.1 local-api.lendingloop.com
```

## Troubleshooting

### Domain Not Resolving

**Problem**: `ping local-www.lendingloop.com` fails or resolves to wrong IP

**Solutions**:
1. Verify HOSTS file entries are correct (no typos)
2. Ensure there are no duplicate entries
3. Flush DNS cache:
   - Windows: `ipconfig /flushdns`
   - macOS: `sudo dscacheutil -flushcache; sudo killall -HUP mDNSResponder`
   - Linux: `sudo systemd-resolve --flush-caches`
4. Restart your browser
5. Try rebooting your computer

### Permission Denied

**Problem**: "Access denied" or "Permission denied" when editing HOSTS file

**Solutions**:
- Windows: Ensure you're running PowerShell or Notepad as Administrator
- macOS/Linux: Use `sudo` before commands
- Check if antivirus software is blocking HOSTS file modifications

### Changes Not Taking Effect

**Problem**: Added entries but domains still don't resolve

**Solutions**:
1. Flush DNS cache (see commands above)
2. Close and reopen your browser
3. Verify entries were actually saved to the file
4. Check for syntax errors (each entry should be on its own line)
5. Ensure there are no extra spaces or special characters

### Antivirus Blocking Changes

**Problem**: Antivirus software prevents HOSTS file modification

**Solutions**:
- Temporarily disable antivirus protection
- Add HOSTS file to antivirus exclusions
- Use antivirus software's "allow" or "trust" feature for the change

## Security Considerations

### Is This Safe?

Yes, modifying the HOSTS file for local development is safe and common practice:
- You're only mapping domains to localhost (127.0.0.1)
- Changes only affect your local machine
- No external network traffic is affected
- Entries can be easily removed

### Malware Concerns

The HOSTS file is sometimes targeted by malware to redirect traffic. To protect yourself:
- Only add entries you understand and trust
- Regularly review your HOSTS file contents
- Use antivirus software that monitors HOSTS file changes
- Only run scripts from trusted sources (like this project)

### Removing Entries

If you need to remove the LendingLoop entries later:

**Windows (Automated):**
```powershell
# Run PowerShell as Administrator
$hostsPath = "C:\Windows\System32\drivers\etc\hosts"
$content = Get-Content $hostsPath | Where-Object { $_ -notmatch "local-www.lendingloop.com" -and $_ -notmatch "local-api.lendingloop.com" }
Set-Content -Path $hostsPath -Value $content
ipconfig /flushdns
```

**Manual:**
- Open HOSTS file as Administrator
- Delete the two LendingLoop lines
- Save the file
- Flush DNS cache

## Next Steps

After configuring the HOSTS file:

1. ✓ HOSTS file configured
2. → Generate SSL certificates: `.\generate-certs.ps1`
3. → Configure .NET API for HTTPS
4. → Configure Angular for HTTPS
5. → Start development!

For complete setup instructions, see [README.md](README.md).

## Additional Resources

- [Microsoft: HOSTS file documentation](https://docs.microsoft.com/en-us/troubleshoot/windows-server/networking/hosts-file-description)
- [Wikipedia: hosts (file)](https://en.wikipedia.org/wiki/Hosts_(file))
- Project setup guide: [README.md](README.md)
- HTTPS setup guide: [HTTPS-SETUP.md](HTTPS-SETUP.md)

