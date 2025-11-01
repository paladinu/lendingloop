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
2. **Login Flow**: User submits credentials → API validates → Generates JWT token → Returns token to client → Client stores token in localStorage
3. **Protected Access**: Client includes JWT in requests → API validates token → Grants/denies access based on token validity
4. **Session Persistence**: On application load, client checks localStorage for valid token → Validates token with API → Restores user session if valid

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
- **LoginComponent**: Email/password login form with loading indicators during submission
- **RegisterComponent**: Registration form with all required fields and real-time password validation feedback
- **EmailVerificationComponent**: Handles email verification process with resend functionality
- **AuthLayoutComponent**: Wrapper for authentication-related pages
- **ToolbarComponent**: Reusable toolbar displaying user's first and last name with logout button, integrated on all authenticated pages for consistent navigation

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
    
    [HttpPost("resend-verification")]
    Task<ActionResult> ResendVerificationEmail(ResendVerificationRequest request)
    
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

public class ResendVerificationRequest
{
    public string Email { get; set; }
}

public class PasswordValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; }
}
```

## Security Implementation

### Password Security
- **Hashing**: Use BCrypt with salt for password hashing
- **Validation**: Implement password policy validation on both client and server with specific error messages for each requirement:
  - Minimum 8 characters
  - At least one lowercase letter
  - At least one uppercase letter
  - At least one special character
- **Storage**: Never store plain text passwords
- **Real-time Feedback**: Provide immediate validation feedback as users type passwords during registration

### JWT Token Management
- **Generation**: Create JWT tokens with user ID, email, and expiration
- **Validation**: Validate tokens on each protected API request
- **Expiration**: Set reasonable token expiration (e.g., 24 hours)
- **Storage**: Store tokens in localStorage for session persistence across browser refreshes
- **Session Restoration**: On application initialization, check for existing valid tokens and restore user session automatically
- **Token Invalidation**: Clear tokens from storage on logout and session expiration

### Email Verification
- **Token Generation**: Create cryptographically secure verification tokens
- **Expiration**: Set verification token expiration (e.g., 24 hours)
- **Single Use**: Invalidate tokens after successful verification
- **Resend Mechanism**: Provide API endpoint to resend verification emails for users who didn't receive or lost the original email
- **Login Prevention**: Block login attempts for unverified accounts with clear messaging directing users to verify their email

### Input Validation and Sanitization
- **Server-Side Validation**: All user inputs must be validated on the server regardless of client-side validation
- **SQL Injection Prevention**: Use parameterized queries and MongoDB driver's built-in protections
- **XSS Prevention**: Sanitize all user inputs before storage and output encoding when displaying user data
- **Email Validation**: Validate email format using standard regex patterns on both client and server
- **Field Length Limits**: Enforce maximum length constraints on all text fields to prevent buffer overflow attacks

### Audit Logging
- **Authentication Events**: Log all authentication attempts (successful and failed) with timestamps and IP addresses
- **Account Changes**: Log user registration, email verification, and password changes
- **Security Events**: Log suspicious activities such as multiple failed login attempts
- **Log Storage**: Store logs securely with appropriate retention policies for security monitoring and compliance

## Error Handling

### Authentication Errors
- **Invalid Credentials**: Return 401 Unauthorized with appropriate message
- **Unverified Email**: Return 403 Forbidden with verification required message
- **Expired Token**: Return 401 Unauthorized and prompt for re-login
- **Registration Conflicts**: Return 409 Conflict for duplicate email addresses

### Validation Errors
- **Password Policy**: Return detailed validation messages for each specific password requirement that is not met:
  - "Password must be at least 8 characters long"
  - "Password must contain at least one lowercase letter"
  - "Password must contain at least one uppercase letter"
  - "Password must contain at least one special character"
- **Required Fields**: Return field-specific validation errors indicating which fields are missing or invalid
- **Email Format**: Validate email format and return appropriate errors
- **Loading States**: Display loading indicators during form submission to provide user feedback

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
3. **Add Toolbar Component**: Create reusable toolbar with user info and logout functionality
4. **Update Items Service**: Include authentication tokens in API calls
5. **Update Item Display**: Show user names instead of user IDs where appropriate