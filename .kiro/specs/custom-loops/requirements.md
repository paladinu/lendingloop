# Requirements Document

## Introduction

The Custom Loops feature enables users to create and manage sharing groups called "loops" where members can share items with each other. Users can invite others via email or from existing loops, control item visibility across loops, and browse all items available within their loops through a searchable interface.

## Glossary

- **Loop**: A sharing group where members can view and access items shared by other members
- **Loop Member**: A user who has accepted an invitation and belongs to a loop
- **Loop Creator**: The user who initially creates a loop
- **Item**: A shareable resource that can be made visible to one or more loops
- **Loop Invitation**: An email-based or in-app request to join a loop
- **Loop Landing Page**: The main interface displaying all items available within a specific loop
- **Item Visibility Setting**: Configuration that determines which loops can view a specific item
- **System**: The Custom Loops application

## Requirements

### Requirement 1

**User Story:** As a user, I want to create a new loop, so that I can start a sharing group with other users

#### Acceptance Criteria

1. WHEN a user submits a loop creation request with a valid loop name, THE System SHALL create a new loop with the user as the creator and first member
2. THE System SHALL assign a unique identifier to each created loop
3. THE System SHALL store the creation timestamp and creator information for each loop
4. IF a user attempts to create a loop with an empty or invalid name, THEN THE System SHALL reject the request and display a validation error message

### Requirement 2

**User Story:** As a loop member, I want to invite someone via email to join my loop, so that I can expand my sharing group

#### Acceptance Criteria

1. WHEN a loop member submits an email invitation with a valid email address, THE System SHALL send an invitation email to the specified recipient
2. THE System SHALL include a unique invitation link in the email that expires after 30 days
3. THE System SHALL record the invitation status as pending until accepted or expired
4. IF the invited email address already belongs to a loop member, THEN THE System SHALL add them directly to the loop without requiring email confirmation
5. WHEN a recipient clicks the invitation link, THE System SHALL add the user to the loop and update the invitation status to accepted

### Requirement 3

**User Story:** As a loop member, I want to invite users from my other loops, so that I can quickly build my sharing group from trusted connections

#### Acceptance Criteria

1. WHEN a loop member views the invitation interface, THE System SHALL display a list of all users from the member's other loops
2. THE System SHALL allow the member to select one or more users from the list to invite
3. WHEN the member confirms the selection, THE System SHALL send in-app invitations to the selected users
4. THE System SHALL notify invited users through the application interface
5. WHEN an invited user accepts the invitation, THE System SHALL add them to the loop immediately

### Requirement 4

**User Story:** As a user, I want to choose which loops can see my item when I add it, so that I can control who has access to my shared items

#### Acceptance Criteria

1. WHEN a user adds a new item, THE System SHALL display a list of all loops the user belongs to
2. THE System SHALL allow the user to select one or more loops where the item will be visible
3. THE System SHALL provide an option to make the item visible in all current loops
4. THE System SHALL provide an option to automatically include the item in all future loops the user joins
5. THE System SHALL save the visibility settings with the item and enforce them when displaying items to loop members

### Requirement 5

**User Story:** As a loop member, I want to view all items shared by loop members on the loop landing page, so that I can see what is available to me

#### Acceptance Criteria

1. WHEN a loop member navigates to a loop landing page, THE System SHALL display all items that are visible to that loop
2. THE System SHALL include items from all members of the loop who have granted visibility to that loop
3. THE System SHALL display item details including title, description, owner, and date added
4. THE System SHALL order items by most recently added first
5. IF a loop has no items, THEN THE System SHALL display a message indicating the loop is empty

### Requirement 6

**User Story:** As a loop member, I want to search for items on the loop landing page, so that I can quickly find specific items I need

#### Acceptance Criteria

1. WHEN a loop member enters a search query on the loop landing page, THE System SHALL filter displayed items based on the query
2. THE System SHALL search across item titles, descriptions, and tags
3. THE System SHALL update the displayed results in real-time as the user types
4. THE System SHALL display a count of matching items
5. WHEN the search query is cleared, THE System SHALL display all items in the loop again

### Requirement 7

**User Story:** As a loop member, I want to see which loops I belong to, so that I can navigate between my different sharing groups

#### Acceptance Criteria

1. WHEN a user views their loops list, THE System SHALL display all loops the user is a member of
2. THE System SHALL show the loop name, member count, and item count for each loop
3. THE System SHALL allow the user to select a loop to navigate to its landing page
4. THE System SHALL indicate which loop is currently active in the interface

### Requirement 8

**User Story:** As a user, I want to update the visibility settings of my existing items, so that I can control access as my loop memberships change

#### Acceptance Criteria

1. WHEN a user views their item details, THE System SHALL display the current loop visibility settings
2. THE System SHALL allow the user to modify which loops can see the item
3. WHEN the user saves updated visibility settings, THE System SHALL immediately apply the changes
4. THE System SHALL remove the item from loop landing pages where visibility has been revoked
5. THE System SHALL add the item to loop landing pages where visibility has been granted
