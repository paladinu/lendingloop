# Design Document

## Overview

The user authentication system will provide secure login, registration, and session management for the shared items application. The system will integrate with the existing MongoDB database by creating a new Users collection and modifying the existing Items collection to reference authenticated users through a userId field instead of the current ownerId field.

## Architecture

### High-Level Architecture

```
┌─────────────────┐    ┌──────────────────┐    ┌─────────────────┐
│   Angular UI    │    │   .NET Core API  │    │    MongoDB      │
│                 │    │                  │    │                 │
│ ┌─────────────┐ │    │ ┌──────────────┐ │    │ ┌─────────────┐ │
│ │Auth Guards  │ │    │ │Auth          │ │    │ │Users        │ │
│ │& Services   │ │◄──►│ │Controllers   │ │◄──►│ │Collection   │ │
│ └─────────────┘ │    │ └──────────────┘ │    │ └─────────────┘ │
│ ┌─────────────┐ │    │ ┌──────────────┐ │    │ ┌─────────────┐ │
│ │Login/       │ │    │ │JWT Token     │ │    │ │Items        │ │
│ │Register UI  │ │    │ │Service       │ │    │ │Collection   │ │
│ └─────────────┘ │    │ └──────────────┘ │    │ │(Modified)   │ │
└─────────────────┘    └──────────────────┘    │ └─────────────┘ │
                                               └─────────────────┘
```

### Authentication Flow

1. **Registration Flow**: User submits registration → API validates → Creates user record → Sends verification email → User verifies → Account activated
2. **Login Flow**: User submits credentials → API validates → Generates JWT token → Returns token to client → Client stores token
3. **Protected Access**: Client includes JWT in requests → API validates token → Grants/denies access based on token validity

## Components and Interfaces

### Frontend Components (Angular)

#### AuthService
```typescript
interface AuthService {
  login(email: string, password: string): Observable<AuthResponse>
  register(userData: RegisterRequest): Observable<RegisterResponse>
  logout(): void
  getCurrentUser(): Observable<User | null>
  isAuthenticated(): boolean
  verifyEmail(token: string): Observable<VerificationResponse>
}
```

#### AuthGuard
```typescript
interface AuthGuard {
  canActivate(): boolean
  canActivateChild(): boolean
}
```

#### Components
- **LoginComponent**: Email/password login form
- **RegisterComponent**: Registration form with all required fields
- **EmailVerificationComponent**: Handles email verification process
- **AuthLayoutComponent**: Wrapper for authentication-related pages

### Backend Controllers (.NET Core)

#### AuthController
```csharp
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    [HttpPost("login")]
    Task<ActionResult<AuthResponse>> Login(LoginRequest request)
    
    [HttpPost("register")]
    Task<ActionResult<RegisterResponse>> Register(RegisterRequest request)
    
    [HttpPost("verify-email")]
    Task<ActionResult<VerificationResponse>> VerifyEmail(VerifyEmailRequest request)
    
    [HttpPost("logout")]
    Task<ActionResult> Logout()
    
    [HttpGet("me")]
    [Authorize]
    Task<ActionResult<UserProfile>> GetCurrentUser()
}
```

#### Updated ItemsController
```csharp
[ApiController]
[Route("api/items")]
[Authorize] // Now requires authentication
public class ItemsController : ControllerBase
{
    // All existing methods now require authentication
    // Items will be filtered by authenticated user's userId
}
```

## Data Models

### User Collection (New)
```json
{
  "_id": "ObjectId",
  "email": "string (unique, indexed)",
  "passwordHash": "string",
  "firstName": "string",
  "lastName": "string",
  "streetAddress": "string",
  "isEmailVerified": "boolean",
  "emailVerificationToken": "string (nullable)",
  "emailVerificationExpiry": "DateTime (nullable)",
  "createdAt": "DateTime",
  "updatedAt": "DateTime",
  "lastLoginAt": "DateTime (nullable)"
}
```

### Updated Items Collection
```json
{
  "_id": "ObjectId",
  "name": "string",
  "userId": "ObjectId (references Users._id)", // Changed from ownerId
  "isAvailable": "boolean",
  "createdAt": "DateTime",
  "updatedAt": "DateTime"
}
```

### DTOs and Request/Response Models

#### Authentication DTOs
```csharp
public class LoginRequest
{
    public string Email { get; set; }
    public string Password { get; set; }
}

public class RegisterRequest
{
    public string Email { get; set; }
    public string Password { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string StreetAddress { get; set; }
}

public class AuthResponse
{
    public string Token { get; set; }
    public UserProfile User { get; set; }
    public DateTime ExpiresAt { get; set; }
}

public class UserProfile
{
    public string Id { get; set; }
    public string Email { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string StreetAddress { get; set; }
    public bool IsEmailVerified { get; set; }
}
```

## Security Implementation

### Password Security
- **Hashing**: Use BCrypt with salt for password hashing
- **Validation**: Implement password policy validation on both client and server
- **Storage**: Never store plain text passwords

### JWT Token Management
- **Generation**: Create JWT tokens with user ID, email, and expiration
- **Validation**: Validate tokens on each protected API request
- **Expiration**: Set reasonable token expiration (e.g., 24 hours)
- **Storage**: Store tokens securely in HTTP-only cookies or secure localStorage

### Email Verification
- **Token Generation**: Create cryptographically secure verification tokens
- **Expiration**: Set verification token expiration (e.g., 24 hours)
- **Single Use**: Invalidate tokens after successful verification

## Error Handling

### Authentication Errors
- **Invalid Credentials**: Return 401 Unauthorized with appropriate message
- **Unverified Email**: Return 403 Forbidden with verification required message
- **Expired Token**: Return 401 Unauthorized and prompt for re-login
- **Registration Conflicts**: Return 409 Conflict for duplicate email addresses

### Validation Errors
- **Password Policy**: Return detailed validation messages for password requirements
- **Required Fields**: Return field-specific validation errors
- **Email Format**: Validate email format and return appropriate errors

## Testing Strategy

### Unit Tests
- **Password Validation**: Test all password policy rules
- **JWT Token Generation/Validation**: Test token lifecycle
- **User Registration Logic**: Test registration validation and user creation
- **Authentication Logic**: Test login validation and session management

### Integration Tests
- **Authentication Flow**: End-to-end login/logout testing
- **Registration Flow**: Complete registration and verification process
- **Protected Routes**: Verify route protection and access control
- **Database Operations**: Test user CRUD operations and data integrity

### Security Tests
- **Password Hashing**: Verify passwords are properly hashed
- **Token Security**: Test JWT token validation and expiration
- **Input Validation**: Test for SQL injection and XSS prevention
- **Session Management**: Test session timeout and cleanup

## Migration Strategy

### Database Migration
1. **Create Users Collection**: Add new collection with proper indexes
2. **Update Items Collection**: 
   - Add userId field to existing items
   - Migrate existing ownerId values to userId format
   - Remove ownerId field after migration
   - Add index on userId field

### API Migration
1. **Add Authentication Middleware**: Implement JWT validation
2. **Update Items Controller**: Add [Authorize] attributes and user filtering
3. **Maintain Backward Compatibility**: Temporarily support both ownerId and userId during transition

### Frontend Migration
1. **Add Authentication Components**: Implement login/register UI
2. **Add Route Guards**: Protect existing routes with authentication
3. **Update Items Service**: Include authentication tokens in API calls
4. **Update Item Display**: Show user names instead of user IDs where appropriate