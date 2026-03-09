# Implementation Plan: View Item Owner Profile

## Overview

This implementation plan breaks down the view-item-owner-profile feature into discrete coding tasks. The approach follows an incremental pattern: backend API first, then frontend integration, followed by testing and refinement. Each task builds on previous work to ensure no orphaned code.

## Tasks

- [x] 1. Implement backend loop membership validation service
  - Add `DoUsersShareLoopAsync(string userId1, string userId2)` method to `ILoopsService` interface
  - Implement method in `LoopsService` to query MongoDB for shared loops between two users
  - Return true if users share at least one loop, false otherwise
  - _Requirements: 5.1, 5.2, 5.3, 5.5_

- [x] 1.1 Write unit tests for loop membership validation
  - Test DoUsersShareLoopAsync returns true when users share one loop
  - Test DoUsersShareLoopAsync returns true when users share multiple loops
  - Test DoUsersShareLoopAsync returns false when users share no loops
  - Test edge case where both userIds are the same
  - _Requirements: 5.2, 5.3_

- [x] 2. Create public profile DTOs and interfaces
  - Create `PublicProfileDto` class in api/Models with userId, firstName, lastName, loopScore, badges, scoreHistory properties
  - Ensure BadgeDto and ScoreHistoryEntryDto already exist (from loop-score feature)
  - _Requirements: 1.3, 6.1, 6.2, 6.3_

- [x] 3. Implement public profile service method
  - [x] 3.1 Add `GetPublicProfileAsync(string requestingUserId, string targetUserId)` to `IUsersService` interface
    - _Requirements: 1.2, 1.3, 1.7, 4.1, 4.2, 4.5_
  
  - [x] 3.2 Implement GetPublicProfileAsync in UsersService
    - Validate target user exists (throw NotFoundException if not)
    - Check if requesting user equals target user (throw BadRequestException, should use /api/users/me)
    - Call DoUsersShareLoopAsync to validate loop membership
    - Throw UnauthorizedException if users don't share loops
    - Fetch user data and loop score data
    - Return PublicProfileDto with only public fields (no email, no streetAddress)
    - _Requirements: 1.2, 1.3, 1.4, 1.5, 1.6, 1.7, 4.1, 4.2, 4.5, 4.6_

- [x] 3.3 Write unit tests for GetPublicProfileAsync
  - Test with valid users and shared loops returns PublicProfileDto
  - Test with non-existent user throws NotFoundException
  - Test with no shared loops throws UnauthorizedException
  - Test response excludes email and streetAddress fields
  - Test response includes loopScore, badges, and scoreHistory
  - _Requirements: 1.2, 1.3, 1.4, 1.5, 1.6, 1.7_

- [x] 3.4 Write property test for loop membership validation
  - **Property 1: Loop membership validation**
  - **Validates: Requirements 1.2, 4.5, 5.2**
  - Generate random pairs of users with at least one shared loop
  - Assert API logic returns successful profile data
  - _Requirements: 1.2, 4.5, 5.2_

- [x] 3.5 Write property test for no shared loops
  - **Property 2: No shared loops returns forbidden**
  - **Validates: Requirements 1.4, 3.6, 4.6, 5.3**
  - Generate random pairs of users with zero shared loops
  - Assert service throws UnauthorizedException
  - _Requirements: 1.4, 3.6, 4.6, 5.3_

- [x] 3.6 Write property test for public profile field exclusion
  - **Property 3: Public profiles exclude private fields**
  - **Validates: Requirements 1.5, 1.6, 4.1, 4.2**
  - Generate random valid public profile requests
  - Assert response does not contain email or streetAddress
  - _Requirements: 1.5, 1.6, 4.1, 4.2_

- [x] 3.7 Write property test for non-existent user
  - **Property 4: Non-existent user returns not found**
  - **Validates: Requirements 1.7, 3.5**
  - Generate random non-existent userIds
  - Assert service throws NotFoundException
  - _Requirements: 1.7, 3.5_

- [x] 3.8 Write property test for required public fields
  - **Property 5: Public profile contains required fields**
  - **Validates: Requirements 1.3, 6.1, 6.2, 6.3**
  - Generate random valid public profile requests
  - Assert response contains userId, firstName, lastName, loopScore, badges, scoreHistory
  - _Requirements: 1.3, 6.1, 6.2, 6.3_

- [x] 4. Implement public profile API endpoint
  - [x] 4.1 Add GET /api/users/:userId/public-profile endpoint to UsersController
    - Extract userId from route parameters
    - Get requesting user ID from JWT claims
    - Call usersService.GetPublicProfileAsync(requestingUserId, targetUserId)
    - Return 200 OK with PublicProfileDto on success
    - Handle NotFoundException → return 404
    - Handle UnauthorizedException → return 403
    - Handle other exceptions → return 500
    - _Requirements: 1.2, 1.3, 1.4, 1.7_

- [x] 4.2 Write unit tests for public profile controller endpoint
  - Test GET with valid userId and shared loops returns 200 with PublicProfileDto
  - Test GET with non-existent userId returns 404
  - Test GET with no shared loops returns 403
  - Test GET with unauthenticated request returns 401
  - Test response does not include email or address fields
  - _Requirements: 1.2, 1.3, 1.4, 1.7_

- [x] 5. Checkpoint - Ensure all backend tests pass
  - Run `dotnet test` from `/api.tests` directory
  - Verify all tests pass before proceeding to frontend
  - Ask user if any questions arise

- [x] 6. Create frontend public profile interface and service method
  - [x] 6.1 Create PublicProfile interface in ui/src/app/models/auth.interface.ts
    - Add userId, firstName, lastName, loopScore, badges, scoreHistory properties
    - _Requirements: 1.3, 6.1, 6.2, 6.3_
  
  - [x] 6.2 Add getPublicProfile(userId: string) method to UsersService
    - Make HTTP GET request to `${environment.apiUrl}/api/users/${userId}/public-profile`
    - Return Observable<PublicProfile>
    - _Requirements: 1.1, 1.2_

- [x] 6.3 Write unit tests for UsersService.getPublicProfile
  - Test getPublicProfile calls correct API endpoint with userId
  - Test getPublicProfile returns PublicProfile observable
  - Test getPublicProfile handles HTTP errors correctly
  - _Requirements: 1.1, 1.2_

- [x] 7. Update Angular routing configuration
  - Update profile route in app.routes.ts from `/profile` to `/profile/:userId`
  - Make userId parameter optional
  - Keep AuthGuard on the route
  - _Requirements: 3.1, 3.2_

- [x] 8. Update ProfileComponent to support parameterized routes
  - [x] 8.1 Add route parameter handling to ProfileComponent
    - Inject ActivatedRoute, Location services
    - Add userId, isOwnProfile, errorMessage properties
    - Subscribe to route.paramMap in ngOnInit to get userId parameter
    - Determine if viewing own profile (no userId or userId matches current user)
    - _Requirements: 2.2, 2.3, 3.2, 3.3, 3.4_
  
  - [x] 8.2 Implement loadProfile logic
    - If viewing own profile, call authService.refreshCurrentUser()
    - If viewing another user's profile, call usersService.getPublicProfile(userId)
    - Handle success: set profileData and isLoading = false
    - Handle errors: set appropriate errorMessage based on status code (403, 404, other)
    - _Requirements: 1.1, 2.1, 2.2, 2.3, 3.4, 3.5, 3.6_
  
  - [x] 8.3 Add goBack() method
    - Call location.back() to return to previous page
    - _Requirements: 7.2_

- [x] 8.4 Write unit tests for ProfileComponent
  - Test component loads private profile when no userId parameter
  - Test component loads private profile when userId matches current user
  - Test component loads public profile when userId differs from current user
  - Test component displays error message on 403 response
  - Test component displays error message on 404 response
  - Test component shows loading state while fetching
  - Test goBack() calls location.back()
  - Test isOwnProfile is true when viewing own profile
  - Test isOwnProfile is false when viewing another user's profile
  - _Requirements: 2.1, 2.2, 2.3, 3.4, 3.5, 3.6, 7.2_

- [x] 9. Update profile template for conditional rendering
  - [x] 9.1 Add back button with goBack() click handler
    - Display back button at top of profile
    - _Requirements: 7.1, 7.2_
  
  - [x] 9.2 Update header to show "My Profile" vs "User Profile"
    - Use isOwnProfile flag to determine header text
    - _Requirements: 7.3_
  
  - [x] 9.3 Add error message display
    - Show errorMessage when present
    - Hide profile content when error exists
    - _Requirements: 3.5, 3.6_
  
  - [x] 9.4 Conditionally render private information
    - Wrap email and streetAddress in *ngIf="isOwnProfile"
    - Ensure these fields only display when viewing own profile
    - _Requirements: 1.5, 1.6, 2.1, 4.1, 4.2_
  
  - [x] 9.5 Always render public information
    - Display name, loop score, badges, score history for all profiles
    - _Requirements: 1.3, 6.1, 6.2, 6.3, 6.4, 7.4_
  
  - [x] 9.6 Handle empty badges case
    - Display "No badges earned yet" message when badges array is empty
    - _Requirements: 6.5_

- [x] 9.7 Write property test for public profile UI rendering
  - **Property 3: Public profiles exclude private fields (UI)**
  - **Validates: Requirements 1.5, 1.6, 4.1, 4.2**
  - Generate random public profile data
  - Assert template does not render email or address fields when isOwnProfile is false
  - _Requirements: 1.5, 1.6, 4.1, 4.2_

- [x] 9.8 Write property test for private profile UI rendering
  - **Property 6: Private profile contains all fields (UI)**
  - **Validates: Requirements 2.1, 4.3**
  - Generate random private profile data
  - Assert template renders email and address fields when isOwnProfile is true
  - _Requirements: 2.1, 4.3_

- [x] 10. Add CSS styling for profile page enhancements
  - Style back button for visibility and usability
  - Style error messages with appropriate colors
  - Ensure visual distinction between own profile and public profile views
  - Style "No badges earned" message
  - _Requirements: 7.1, 7.3_

- [x] 11. Final checkpoint - Ensure all tests pass
  - Run `dotnet test` from `/api.tests` to verify backend tests pass
  - Run `npm test` from `/ui` to verify frontend tests pass
  - Manually test navigation flow: item card → profile → back
  - Test viewing own profile (with and without userId parameter)
  - Test viewing another user's profile (shared loops)
  - Test error cases (no shared loops, non-existent user)
  - Ask user if any questions arise

## Notes

- All tasks are required for comprehensive testing and quality assurance
- Each task references specific requirements for traceability
- Checkpoints ensure incremental validation at backend and frontend boundaries
- Property tests validate universal correctness properties with minimum 100 iterations
- Unit tests validate specific examples and edge cases
- The item card component already has the owner name link implemented, so no changes needed there
- The existing /api/users/me endpoint remains unchanged for fetching current user's full profile
