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


## Testing Guidelines

### Backend Testing (.NET)
**All services in the `/api` project MUST have corresponding unit tests in `/api.tests`.**

- Create test files following the pattern: `{ServiceName}Tests.cs`
- Use xUnit as the testing framework
- Mock dependencies using Moq
- Test all public methods and edge cases
- Aim for high code coverage on business logic
- Testing project folder strutcture should match that of the project under test

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

### Testing Best Practices
- **ALL tests MUST follow the Arrange-Act-Assert (AAA) pattern with comments**
  - Use `//arrange` for test setup and initialization
  - Use `//act` for executing the method under test
  - Use `//assert` for verifying the results
- Write tests for new features before marking tasks as complete
- Keep tests focused and isolated
- Use descriptive test names that explain what is being tested
- Mock external dependencies (database, HTTP calls, etc.)
- Test both success and failure scenarios
- Avoid testing framework internals or private methods
- All new services MUST have unit tests before being considered complete
- Existing services without tests should have tests added when modified
