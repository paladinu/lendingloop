# Implementation Plan

- [x] 1. Set up authentication infrastructure and data models





- [x] 1.1 Create User model and MongoDB configuration


  - Create User.cs model class with all required properties (email, passwordHash, firstName, lastName, streetAddress, verification fields)
  - Add User collection configuration to MongoDB context
  - Create database indexes for email field (unique) and userId references
  - _Requirements: 4.1, 4.2, 7.2_

- [x] 1.2 Create authentication DTOs and request/response models


  - Create LoginRequest, RegisterRequest, AuthResponse, UserProfile DTOs
  - Create VerifyEmailRequest and VerificationResponse DTOs
  - Add validation attributes to all DTO properties
  - _Requirements: 2.1, 4.1, 5.2_

- [x] 1.3 Update SharedItem model to use userId instead of ownerId


  - Modify SharedItem.cs to replace ownerId with userId property
  - Update MongoDB collection mapping for Items collection
  - _Requirements: 4.4, 7.2_

- [x] 2. Implement password security and validation





- [x] 2.1 Create password policy validation service


  - Implement PasswordValidator class with all security requirements (8+ chars, upper/lower case, special character)
  - Create password hashing service using BCrypt
  - Add password strength validation methods
  - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5, 7.1_

- [x] 2.2 Implement JWT token service


  - Create JwtTokenService for generating and validating JWT tokens
  - Configure JWT authentication middleware in Program.cs
  - Add token expiration and security configuration
  - _Requirements: 2.2, 6.2, 7.3_

- [x] 3. Create authentication API endpoints





- [x] 3.1 Implement user registration endpoint


  - Create AuthController with Register action
  - Add user registration logic with validation
  - Implement duplicate email checking
  - Generate email verification tokens
  - _Requirements: 4.1, 4.2, 4.3, 4.4, 4.5_

- [x] 3.2 Implement login endpoint


  - Create Login action in AuthController
  - Add credential validation and authentication logic
  - Generate JWT tokens for successful logins
  - Handle invalid credentials and unverified accounts
  - _Requirements: 2.1, 2.2, 2.3, 2.4, 2.5_

- [x] 3.3 Implement email verification endpoint


  - Create VerifyEmail action in AuthController
  - Add token validation and account activation logic
  - Handle expired and invalid verification tokens
  - _Requirements: 5.1, 5.2, 5.3, 5.4_

- [x] 3.4 Implement logout and user profile endpoints


  - Create Logout action for session termination
  - Create GetCurrentUser action for authenticated user profile
  - Add proper authorization attributes
  - _Requirements: 6.1, 6.2, 6.3_

- [x] 4. Update existing Items API for authentication





- [x] 4.1 Add authentication requirements to ItemsController


  - Add [Authorize] attribute to ItemsController
  - Update GetItems to filter by authenticated user's userId
  - Update CreateItem to set userId from authenticated user
  - _Requirements: 1.2, 1.3, 4.4_

- [x] 4.2 Update Items API to use userId field


  - Modify all Items endpoints to work with userId instead of ownerId
  - Update item creation and retrieval logic
  - _Requirements: 4.4, 7.2_

- [x] 5. Create Angular authentication services and guards





- [x] 5.1 Create AuthService for authentication operations


  - Implement login, register, logout methods
  - Add JWT token storage and management
  - Create getCurrentUser and isAuthenticated methods
  - Add email verification functionality
  - _Requirements: 2.1, 2.2, 4.1, 5.3, 6.1_

- [x] 5.2 Create AuthGuard for route protection


  - Implement CanActivate guard to protect routes
  - Add automatic redirection to login for unauthenticated users
  - _Requirements: 1.1, 1.3_

- [x] 5.3 Create HTTP interceptor for JWT tokens


  - Implement interceptor to automatically add JWT tokens to API requests
  - Add token refresh logic and error handling
  - _Requirements: 2.2, 6.2, 7.3_

- [x] 6. Create authentication UI components





- [x] 6.1 Create LoginComponent


  - Build login form with email and password fields
  - Add form validation and error handling
  - Implement login submission and success/error feedback
  - _Requirements: 2.1, 2.3, 2.4, 2.5_

- [x] 6.2 Create RegisterComponent


  - Build registration form with all required fields (email, firstName, lastName, streetAddress, password)
  - Add client-side password policy validation with real-time feedback
  - Implement registration submission and verification email notification
  - _Requirements: 4.1, 4.2, 3.1, 3.2, 3.3, 3.4, 3.5, 5.1_

- [x] 6.3 Create EmailVerificationComponent


  - Build email verification page that handles verification tokens
  - Add verification success/failure messaging
  - Implement resend verification email functionality
  - _Requirements: 5.2, 5.3, 5.5_

- [x] 6.4 Create AuthLayoutComponent and navigation updates


  - Create layout component for authentication pages
  - Add logout functionality to main navigation
  - Update app routing to include authentication routes
  - _Requirements: 1.1, 6.1, 6.3_

- [x] 7. Update existing UI for authenticated users





- [x] 7.1 Update ItemsService to work with authentication


  - Modify ItemsService to include authentication tokens
  - Update error handling for authentication failures
  - _Requirements: 1.2, 2.2_

- [x] 7.2 Update AppComponent for authenticated access


  - Add authentication checks to main app component
  - Update item display to show user information
  - Add route guards to protect item management features
  - _Requirements: 1.1, 1.2, 1.4_

- [x] 7.3 Update item display to show user names instead of IDs


  - Modify item cards to display user first/last names
  - Update item creation to associate with authenticated user
  - _Requirements: 4.4, 7.2_

- [x] 7.4 Create reusable ToolbarComponent for user information and logout
  - Extract toolbar from MainComponent into a standalone, reusable ToolbarComponent
  - Component should display user's first and last name with dropdown menu
  - Include logout button with proper functionality in the dropdown
  - Add navigation buttons for Loops and Invitations
  - Integrate toolbar into all authenticated pages (MainComponent, LoopListComponent, LoopDetailComponent, LoopCreateComponent, LoopInviteComponent, LoopInvitationsComponent, LoopMembersComponent, ItemAddComponent, ItemVisibilityComponent, AcceptInvitationComponent)
  - Ensure consistent appearance and functionality across the application
  - Remove duplicate toolbar code from MainComponent after extraction
  - _Requirements: 8.1, 8.2, 8.3, 8.4_

- [x] 7.5 Add toolbar to remaining authenticated pages





  - Add toolbar component to ItemRequestListComponent
  - Add toolbar component to MyRequestsComponent
  - Ensure consistent appearance with other pages
  - _Requirements: 8.1, 8.2, 8.4_

- [-] 8. Implement email service for verification


- [x] 8.1 Create email service for sending verification emails



  - Implement email service using SMTP or email provider
  - Create email templates for verification messages
  - Add email sending logic to registration process
  - _Requirements: 5.1, 5.5_

- [x] 8.2 Add email service configuration and testing






  - Configure email service settings (SMTP, API keys, etc.)
  - Add email service testing and error handling
  - _Requirements: 5.1, 5.5_

- [x] 9. Database migration and data cleanup





- [x] 9.1 Create database migration scripts


  - Create script to add userId field to existing Items
  - Create script to remove ownerId field after migration
  - Add proper database indexes for performance
  - _Requirements: 4.4, 7.2_

- [x] 9.2 Update application configuration


  - Update appsettings.json with JWT and email configuration
  - Add authentication middleware configuration
  - Update CORS settings for authentication endpoints
  - _Requirements: 2.2, 7.3, 7.4_

- [x] 10. Testing and validation




- [x] 10.1 Create unit tests for authentication services


  - Write tests for password validation logic
  - Test JWT token generation and validation
  - Test user registration and login flows
  - _Requirements: All requirements_

- [x] 10.2 Create integration tests for authentication API


  - Test complete registration and verification flow
  - Test login and logout functionality
  - Test protected route access and authorization
  - _Requirements: All requirements_

- [x] 10.3 Create end-to-end tests for authentication UI


  - Test user registration through UI
  - Test login and logout through UI
  - Test protected route navigation and access
  - _Requirements: All requirements_