# Project Structure and Command Guidelines

This is a monorepo containing an Angular frontend and .NET backend.

## Directory Structure

- `/ui` - Angular frontend application
- `/api` - .NET 8 Web API backend
- `/api.tests` - .NET test project

## Command Execution Rules

### Angular Commands (Frontend)
**ALWAYS run Angular/npm commands from the `/ui` directory using the `path` parameter.**

Examples:
- `npm install` → Run in `/ui`
- `npm start` or `ng serve` → Run in `/ui`
- `ng generate` → Run in `/ui`
- `npm test` → Run in `/ui` (uses Jest)
- `npm run test:watch` → Run in `/ui` (Jest watch mode)
- `npm run test:coverage` → Run in `/ui` (Jest with coverage)

### .NET Commands (Backend)
**ALWAYS run dotnet commands from the `/api` directory using the `path` parameter.**

Examples:
- `dotnet build` → Run in `/api`
- `dotnet run` → Run in `/api`
- `dotnet add package` → Run in `/api`
- `dotnet ef migrations` → Run in `/api`

### .NET Test Commands
**ALWAYS run test commands from the `/api.tests` directory using the `path` parameter.**

Examples:
- `dotnet test` → Run in `/api.tests`
- `dotnet add reference` → Run in `/api.tests`

## Important Notes

- **NEVER use `cd` commands** - Always use the `path` parameter in tool calls
- The Angular dev server proxies API requests to the .NET backend (see `/ui/proxy.conf.json`)
- Both applications need to be running for full functionality
