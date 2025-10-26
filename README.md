# lendingloop
Tool lending library

## Overview

This is a monorepo containing an Angular frontend and a .NET 8 Web API backend for managing shared items. The application allows users to view and add items to a shared collection.

## Technology Stack

- **Frontend**: Angular (latest stable version)
- **Backend**: C# .NET 8 Web API
- **Database**: MongoDB
- **Development Ports**:
  - UI: http://localhost:4200
  - API: http://localhost:8080
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

## Local Development Setup

### 1. Start MongoDB

Ensure MongoDB is running on localhost:27017. The exact command depends on your installation method:

- **Windows (Service)**: MongoDB typically runs as a service automatically
- **macOS (Homebrew)**: `brew services start mongodb-community`
- **Linux**: `sudo systemctl start mongod`

### 2. Start the API

Open a terminal and navigate to the api directory:

```bash
cd api
dotnet run
```

The API will start on http://localhost:8080

### 3. Start the UI

Open a separate terminal and navigate to the ui directory:

```bash
cd ui
ng serve
```

The UI will start on http://localhost:4200

### 4. Access the Application

Open your browser and navigate to http://localhost:4200

The Angular UI will automatically proxy API requests to http://localhost:8080

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

**MongoDB Connection Issues**
- Verify MongoDB is running: `mongosh` or check your MongoDB service status
- Ensure MongoDB is listening on port 27017

**API Won't Start**
- Verify .NET 8 SDK is installed: `dotnet --version`
- Check if port 8080 is already in use

**UI Won't Start**
- Verify Angular CLI is installed: `ng version`
- Check if port 4200 is already in use
- Run `npm install` in the ui directory if dependencies are missing

**CORS Errors**
- Ensure the API is running on port 8080
- Verify the proxy.conf.json is properly configured in the ui directory
