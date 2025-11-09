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
- Both applications need to be running for full functionality

## Custom Domain Configuration

### Local Development URLs
The application uses custom local domains for a production-like development environment:
- **Frontend (Angular UI)**: https://local-www.lendingloop.com
- **Backend (.NET API)**: https://local-api.lendingloop.com

### HOSTS File Configuration
Before running the application, you MUST configure your system's HOSTS file to map these domains to localhost:

**Windows**: `C:\Windows\System32\drivers\etc\hosts`
**Mac/Linux**: `/etc/hosts`

Add these entries:
```
127.0.0.1 local-www.lendingloop.com
127.0.0.1 local-api.lendingloop.com
```

**Quick Setup**: Run the provided PowerShell script as Administrator:
```powershell
.\configure-hosts.ps1
```

### SSL Certificate Requirements
Both the Angular UI and .NET API use HTTPS with self-signed certificates for local development.

**Certificate Generation**: Use the provided PowerShell script:
```powershell
.\generate-certs.ps1
```

This creates certificates in the `/certs` directory. You'll need to:
1. Accept browser security warnings when accessing https://local-www.lendingloop.com
2. Accept browser security warnings when accessing https://local-api.lendingloop.com

**Note**: Certificate files are in `.gitignore` and should never be committed to version control.

### Environment Configuration for API URLs

The Angular application uses environment files to configure the API URL:

- **Development**: `ui/src/environments/environment.development.ts`
  - Uses `https://local-api.lendingloop.com` for API calls
  
- **Production**: `ui/src/environments/environment.ts`
  - Configure with production API URL when deploying

**Important**: All Angular services should use `environment.apiUrl` instead of hardcoded URLs:

```typescript
import { environment } from '../../environments/environment';

// Correct approach
this.http.get(`${environment.apiUrl}/api/items`);

// Avoid hardcoding
// this.http.get('https://local-api.lendingloop.com/api/items');
```

### CORS Configuration
The .NET API is configured with CORS to allow requests from `https://local-www.lendingloop.com`. This is necessary because the frontend and backend run on different domains.

The Angular UI makes direct HTTP requests to the API domain - there is no proxy configuration.


## Testing Guidelines

### CRITICAL: Test Requirements for Task Completion
**A task is NOT considered complete until ALL tests pass:**
- Backend tests: `dotnet test` from `/api.tests` MUST show all tests passing
- Frontend tests: `npm test` from `/ui` MUST show all tests passing
- Both test suites MUST be run and verified before marking any task as complete
- If implementing a task breaks existing tests, those tests MUST be fixed as part of the task
- New functionality MUST include corresponding tests

### Backend Testing (.NET)
**All services in the `/api` project MUST have corresponding unit tests in `/api.tests`.**

- Create test files following the pattern: `{ServiceName}Tests.cs`
- Use xUnit as the testing framework
- Mock dependencies using Moq
- Test all public methods and edge cases
- Aim for high code coverage on business logic
- Testing project folder structure should match that of the project under test
- **REQUIRED**: Run `dotnet test` from `/api.tests` after implementation to verify all tests pass

Example test structure:
```csharp
public class ItemsServiceTests
{
    [Fact]
    public async Task GetItemByIdAsync_ReturnsItem_WhenItemExists()
    {
        //arrange
        //act
        //assert
    }
}
```

### Frontend Testing (Angular)
**All services and components in the `/ui` project SHOULD have corresponding unit tests.**

- Test files should be named: `{component/service}.spec.ts`
- Use Jest as the testing framework (configured in the project)
- Mock HTTP calls and dependencies
- Test component logic, not implementation details
- Focus on user interactions and state changes
- **REQUIRED**: Run `npm test` from `/ui` after implementation to verify all tests pass

Example test structure:
```typescript
describe('ItemsService', () => {
  it('should fetch items successfully', () => {
    //arrange
    //act
    //assert
  });
});
```

### Test Execution
- Run backend tests: `dotnet test` from `/api.tests`
- Run frontend tests: `npm test` from `/ui`
- Run frontend tests with coverage: `npm run test:coverage` from `/ui`
- **MANDATORY**: Both test suites must pass before considering a task complete

### Test Output Verbosity Control
**To keep sessions efficient, use minimal verbosity flags when running tests:**

#### .NET Tests
- Use `dotnet test --verbosity minimal` for concise output
- Use `dotnet test --nologo --verbosity quiet` for even less output
- Only show full output when debugging specific test failures

#### Jest/Angular Tests
- Use `npm test -- --silent` to suppress verbose console output
- Use `npm test -- --reporters=jest-silent-reporter` if available
- Default `npm test` already provides reasonable output

#### When to Show Full Output
- Only request verbose output when investigating specific test failures
- For successful test runs, minimal output showing pass/fail counts is sufficient
- Focus on the summary line (e.g., "Passed! 45 tests passed")

#### Reporting Test Results
When tests pass, simply state:
- "All backend tests pass (X tests)"
- "All frontend tests pass (X tests)"

When tests fail, show only:
- The failed test names
- The specific error messages
- Relevant stack traces (not full output)

### Testing Best Practices
- **ALL tests MUST follow the Arrange-Act-Assert (AAA) pattern with comments**
  - Use `//arrange` for test setup and initialization
  - Use `//act` for executing the method under test
  - Use `//assert` for verifying the results
- Write tests for new features as part of the implementation
- Keep tests focused and isolated
- Use descriptive test names that explain what is being tested
- Mock external dependencies (database, HTTP calls, etc.)
- Test both success and failure scenarios
- Avoid testing framework internals or private methods
- All new services MUST have unit tests before being considered complete
- Existing services without tests should have tests added when modified
- **If your changes break existing tests, fix them immediately as part of your task**

### Task Completion Checklist
Before marking any task as complete, verify:
1. ✅ All code changes are implemented
2. ✅ Backend tests pass: `dotnet test` from `/api.tests` shows 0 failures
3. ✅ Frontend tests pass: `npm test` from `/ui` shows 0 failures
4. ✅ New functionality has corresponding tests
5. ✅ Any broken tests have been fixed
