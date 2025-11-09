# lendingloop
Tool lending library

## Overview

This is a monorepo containing an Angular frontend and a .NET 8 Web API backend for managing shared items. The application allows users to view and add items to a shared collection.

## Technology Stack

- **Frontend**: Angular (latest stable version)
- **Backend**: C# .NET 8 Web API
- **Database**: MongoDB
- **Development URLs**:
  - UI: https://local-www.lendingloop.com
  - API: https://local-api.lendingloop.com
  - MongoDB: localhost:27017

## Prerequisites

Before running this application, ensure you have the following installed:

1. **MongoDB** - Must be installed and running on localhost:27017
   - Download from: https://www.mongodb.com/try/download/community
   - Verify it's running: `mongosh` or check your MongoDB service

2. **Node.js and npm** - Required for Angular development
   - Download from: https://nodejs.org/
   - Verify installation: `node --version` and `npm --version`

3. **Angular CLI** - Required to run the Angular development server
   - Install globally: `npm install -g @angular/cli`
   - Verify installation: `ng version`

4. **.NET 8 SDK** - Required for the API
   - Download from: https://dotnet.microsoft.com/download/dotnet/8.0
   - Verify installation: `dotnet --version`

5. **OpenSSL** (Optional) - Required for generating PEM certificates for Angular
   - Windows: Download from https://slproweb.com/products/Win32OpenSSL.html or use Git Bash
   - macOS: Pre-installed or install via Homebrew: `brew install openssl`
   - Linux: Usually pre-installed or install via package manager: `sudo apt-get install openssl`

## Local Development Setup

### 1. Configure Custom Domains (One-Time Setup)

The application uses custom local domains for a production-like development environment:
- **UI**: https://local-www.lendingloop.com
- **API**: https://local-api.lendingloop.com

#### Configure HOSTS File

You need to map these domains to localhost in your system's HOSTS file.

**IMPORTANT**: Administrator/root privileges are required to edit the HOSTS file.

**Windows - Automated (Recommended)**: 

Run PowerShell as Administrator and execute the provided script:
```powershell
.\configure-hosts.ps1
```

This script will:
- Check if you're running as Administrator
- Add the required domain entries to your HOSTS file
- Skip entries that already exist
- Flush your DNS cache
- Verify the configuration

**Windows - Manual**: 

If you prefer to edit manually, run PowerShell as Administrator:
```powershell
Add-Content -Path "C:\Windows\System32\drivers\etc\hosts" -Value "`n127.0.0.1 local-www.lendingloop.com`n127.0.0.1 local-api.lendingloop.com"
```

Or edit the file directly:
1. Open Notepad as Administrator
2. Open file: `C:\Windows\System32\drivers\etc\hosts`
3. Add these lines at the end:
   ```
   127.0.0.1 local-www.lendingloop.com
   127.0.0.1 local-api.lendingloop.com
   ```
4. Save the file

**macOS/Linux**: 

Run in terminal:
```bash
sudo sh -c 'echo "127.0.0.1 local-www.lendingloop.com" >> /etc/hosts'
sudo sh -c 'echo "127.0.0.1 local-api.lendingloop.com" >> /etc/hosts'
```

Or edit manually:
```bash
sudo nano /etc/hosts
# Add the two lines above, save and exit
```

### 2. Generate SSL Certificates (One-Time Setup)

The application requires HTTPS with self-signed certificates for local development.

#### Option A: Using PowerShell (Windows - Recommended)

Run the provided script from the project root:
```powershell
.\generate-certs.ps1
```

This will:
- Create a `certs` directory
- Generate a self-signed certificate for both domains
- Export to PFX format (for .NET API)
- Export to CER format (for trusting the certificate)

**Certificate Password**: `dev-password-2024`

#### Option B: Using OpenSSL (Cross-Platform)

If you have OpenSSL installed, you can generate certificates manually:

```bash
# Create certs directory
mkdir certs

# Generate certificate and private key
openssl req -x509 -newkey rsa:4096 -keyout certs/lendingloop-dev-key.pem -out certs/lendingloop-dev.pem -days 1825 -nodes -subj "/CN=local-www.lendingloop.com/CN=local-api.lendingloop.com"

# Generate PFX for .NET (when prompted, use password: dev-password-2024)
openssl pkcs12 -export -out certs/lendingloop-dev.pfx -inkey certs/lendingloop-dev-key.pem -in certs/lendingloop-dev.pem
```

#### Converting PFX to PEM (for Angular)

If you used the PowerShell script, convert the PFX to PEM format for Angular:

```bash
# Extract certificate
openssl pkcs12 -in certs/lendingloop-dev.pfx -out certs/lendingloop-dev.pem -nodes -passin pass:dev-password-2024

# Extract private key
openssl pkcs12 -in certs/lendingloop-dev.pfx -out certs/lendingloop-dev-key.pem -nocerts -nodes -passin pass:dev-password-2024
```

#### Trust the Certificate (Optional - Eliminates Browser Warnings)

**Windows**:
1. Double-click `certs/lendingloop-dev.cer`
2. Click "Install Certificate"
3. Choose "Current User" or "Local Machine"
4. Select "Place all certificates in the following store"
5. Browse and select "Trusted Root Certification Authorities"
6. Click "Next" and "Finish"

**macOS**:
```bash
sudo security add-trusted-cert -d -r trustRoot -k /Library/Keychains/System.keychain certs/lendingloop-dev.cer
```

**Linux**:
```bash
sudo cp certs/lendingloop-dev.cer /usr/local/share/ca-certificates/lendingloop-dev.crt
sudo update-ca-certificates
```

### 3. Start MongoDB

Ensure MongoDB is running on localhost:27017. The exact command depends on your installation method:

- **Windows (Service)**: MongoDB typically runs as a service automatically
- **macOS (Homebrew)**: `brew services start mongodb-community`
- **Linux**: `sudo systemctl start mongod`

### 4. Start the API

Open a terminal and navigate to the api directory:

```bash
cd api
dotnet run
```

The API will start on https://local-api.lendingloop.com

### 5. Start the UI

Open a separate terminal and navigate to the ui directory:

```bash
cd ui
ng serve
```

The UI will start on https://local-www.lendingloop.com

### 6. Access the Application

Open your browser and navigate to https://local-www.lendingloop.com

**Note**: If you didn't trust the certificate, your browser will show a security warning. Click "Advanced" and "Proceed to local-www.lendingloop.com" (or similar option depending on your browser).

## Project Structure

```
/
├── api/          # .NET 8 Web API backend
├── ui/           # Angular frontend
├── .gitignore    # Git ignore rules for both projects
└── README.md     # This file
```

## Development Workflow

1. Make sure MongoDB is running
2. Start the API in one terminal: `cd api && dotnet run`
3. Start the UI in another terminal: `cd ui && ng serve`
4. Open http://localhost:4200 in your browser
5. Both the API and UI support hot-reload during development

## API Endpoints

- `GET /api/items` - Retrieve all shared items
- `POST /api/items` - Create a new shared item

## Troubleshooting

**Custom Domain Not Resolving**
- Verify HOSTS file entries: `ping local-www.lendingloop.com` should resolve to 127.0.0.1
- On Windows, ensure you edited the HOSTS file as Administrator
- Try flushing DNS cache:
  - Windows: `ipconfig /flushdns`
  - macOS: `sudo dscacheutil -flushcache; sudo killall -HUP mDNSResponder`
  - Linux: `sudo systemd-resolve --flush-caches`

**Certificate Issues**
- If you see "NET::ERR_CERT_AUTHORITY_INVALID", the certificate is not trusted (this is normal for self-signed certs)
- Click "Advanced" in your browser and proceed anyway, or trust the certificate using the instructions above
- Verify certificate files exist in the `certs` directory
- Ensure certificate password matches in configuration files: `dev-password-2024`

**MongoDB Connection Issues**
- Verify MongoDB is running: `mongosh` or check your MongoDB service status
- Ensure MongoDB is listening on port 27017

**API Won't Start**
- Verify .NET 8 SDK is installed: `dotnet --version`
- Check if HTTPS port 443 is already in use
- Verify certificate path in `appsettings.Development.json` is correct
- Ensure certificate password is correct

**UI Won't Start**
- Verify Angular CLI is installed: `ng version`
- Check if HTTPS port 443 is already in use
- Run `npm install` in the ui directory if dependencies are missing
- Verify certificate paths in `angular.json` are correct

**CORS Errors**
- Ensure the API is running on https://local-api.lendingloop.com
- Verify CORS policy in API's `Program.cs` allows https://local-www.lendingloop.com
- Check browser console for specific CORS error messages

**Browser Shows "Connection Refused"**
- Verify both custom domains are in your HOSTS file
- Ensure the API and UI are actually running (check terminal output)
- Try accessing the API directly: https://local-api.lendingloop.com/api/items
