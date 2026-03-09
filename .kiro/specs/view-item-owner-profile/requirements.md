# Requirements Document

## Introduction

This feature extends the existing profile functionality to allow users to view other users' public profiles when clicking on an item owner's name. Currently, users can only view their own profile. This enhancement enables users to assess the reputation and trustworthiness of item owners before requesting to borrow items, while maintaining appropriate privacy boundaries.

## Glossary

- **Profile_Viewer**: The user who is viewing a profile
- **Profile_Owner**: The user whose profile is being viewed
- **Public_Profile**: A profile view containing only non-sensitive information (name, loop score, badges, score history)
- **Private_Profile**: A profile view containing all user information including email and address
- **Loop_Score**: A numerical reputation score representing a user's lending and borrowing activity
- **Badge**: An achievement or recognition earned through platform activity
- **Score_History**: A chronological record of loop score changes over time
- **Profile_API**: The backend service that retrieves and returns user profile data

## Requirements

### Requirement 1: View Public Profile

**User Story:** As a user browsing items, I want to click on an item owner's name to view their public profile, so that I can learn about their reputation before requesting an item.

#### Acceptance Criteria

1. WHEN a Profile_Viewer clicks on an owner name link in an item card, THE System SHALL navigate to the profile page for that Profile_Owner
2. WHEN the Profile_API receives a request for a public profile, THE System SHALL verify that the Profile_Viewer and Profile_Owner share at least one loop
3. WHEN the Profile_Viewer and Profile_Owner share at least one loop, THE System SHALL return the Profile_Owner's name, loop score, badges, and score history
4. WHEN the Profile_Viewer and Profile_Owner do not share any loops, THE System SHALL return an authorization error
5. WHEN displaying a public profile, THE System SHALL NOT display the Profile_Owner's email address
6. WHEN displaying a public profile, THE System SHALL NOT display the Profile_Owner's street address
7. THE Profile_API SHALL validate that the requested user exists before returning profile data

### Requirement 2: View Own Profile

**User Story:** As a user viewing my own profile, I want to see all my information including email and address, so that I can verify my account details.

#### Acceptance Criteria

1. WHEN a Profile_Viewer navigates to their own profile, THE System SHALL display their complete Private_Profile including email and street address
2. WHEN a Profile_Viewer navigates to /profile without a userId parameter, THE System SHALL display the Profile_Viewer's own Private_Profile
3. WHEN a Profile_Viewer navigates to /profile/:userId where userId matches their own user ID, THE System SHALL display their complete Private_Profile

### Requirement 3: Profile Route Handling

**User Story:** As a developer, I want the profile route to support both parameterized and non-parameterized access, so that the system can display the appropriate profile based on context.

#### Acceptance Criteria

1. THE System SHALL support the route pattern /profile/:userId where userId is optional
2. WHEN the userId parameter is absent, THE System SHALL default to displaying the current user's profile
3. WHEN the userId parameter is present and matches the current user, THE System SHALL display the Private_Profile
4. WHEN the userId parameter is present and does not match the current user, THE System SHALL display the Public_Profile
5. WHEN the userId parameter references a non-existent user, THE System SHALL display an appropriate error message
6. WHEN the userId parameter references a user who does not share any loops with the Profile_Viewer, THE System SHALL display an authorization error message

### Requirement 4: Privacy Protection

**User Story:** As a user, I want my email and address to remain private when others view my profile, so that my personal information is protected.

#### Acceptance Criteria

1. THE Profile_API SHALL NOT include email addresses in public profile responses
2. THE Profile_API SHALL NOT include street addresses in public profile responses
3. THE Profile_API SHALL only return private information when the requesting user is the Profile_Owner
4. WHEN the Profile_API receives a request for private profile data, THE System SHALL verify the requester's identity matches the Profile_Owner
5. THE Profile_API SHALL only return public profile data when the Profile_Viewer and Profile_Owner share at least one loop
6. WHEN users do not share any loops, THE Profile_API SHALL deny access to profile information

### Requirement 5: Loop Membership Validation

**User Story:** As a user, I want to only see profiles of users who are in my loops, so that my profile information is only visible to trusted community members.

#### Acceptance Criteria

1. WHEN the Profile_API receives a public profile request, THE System SHALL query the database to find loops shared between Profile_Viewer and Profile_Owner
2. WHEN the Profile_Viewer and Profile_Owner share at least one loop, THE System SHALL authorize the profile view request
3. WHEN the Profile_Viewer and Profile_Owner share zero loops, THE System SHALL return a 403 Forbidden error
4. WHEN viewing one's own profile, THE System SHALL bypass loop membership validation
5. THE System SHALL consider two users as sharing a loop when both users are members of the same loop

### Requirement 6: Reputation Information Display

**User Story:** As a user viewing another user's profile, I want to see their loop score, badges, and score history, so that I can assess their trustworthiness and lending patterns.

#### Acceptance Criteria

1. WHEN displaying any profile (public or private), THE System SHALL show the Profile_Owner's current loop score
2. WHEN displaying any profile (public or private), THE System SHALL show all badges earned by the Profile_Owner
3. WHEN displaying any profile (public or private), THE System SHALL show the Profile_Owner's complete score history
4. THE System SHALL format score history in chronological order with dates and score changes
5. WHEN a Profile_Owner has no badges, THE System SHALL display an appropriate message indicating no badges have been earned

### Requirement 7: Navigation and User Experience

**User Story:** As a user viewing another user's profile, I want clear navigation options, so that I can easily return to where I came from.

#### Acceptance Criteria

1. WHEN viewing any profile, THE System SHALL provide a visible back navigation option
2. WHEN a Profile_Viewer uses the back navigation, THE System SHALL return them to the previous page
3. WHEN viewing a public profile, THE System SHALL visually distinguish it from viewing one's own profile
4. THE System SHALL display the Profile_Owner's full name prominently on the profile page
