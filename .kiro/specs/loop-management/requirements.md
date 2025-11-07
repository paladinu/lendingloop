# Requirements Document

## Introduction

This document specifies the technical requirements for implementing advanced loop management features for the LendingLoop platform. These features extend the existing custom loops functionality to provide loop descriptions, privacy settings, ownership transfer, and loop archival capabilities.

## Glossary

- **Loop**: A sharing group where members can view and borrow items from each other
- **Loop Owner**: The user who created the loop and has administrative privileges
- **Loop Privacy**: Settings that control whether a loop is discoverable and joinable by others
- **Loop Archive**: A soft-delete state where a loop is hidden but data is preserved
- **System**: The Loop Management application

## Requirements

### Requirement 1

**User Story:** As a loop owner, I want to add a description to my loop, so that members and potential members understand the purpose of the sharing group

#### Acceptance Criteria

1. WHEN a loop owner creates a new loop, THE System SHALL provide an optional description field with a maximum length of 500 characters
2. WHEN a loop owner updates the loop description, THE System SHALL save the new description and update the loop's updatedAt timestamp
3. THE System SHALL display the loop description on the loop detail page for all members
4. THE System SHALL display the loop description in loop search results when applicable
5. IF a loop has no description, THEN THE System SHALL display a default message indicating no description is available

### Requirement 2

**User Story:** As a loop owner, I want to set my loop as private, so that only invited members can join and the loop is not discoverable by others

#### Acceptance Criteria

1. WHEN a loop owner creates a new loop, THE System SHALL default the privacy setting to private
2. THE System SHALL allow the loop owner to toggle between private and public privacy settings
3. WHEN a loop is set to private, THE System SHALL prevent the loop from appearing in public loop searches or discovery features
4. WHEN a loop is set to private, THE System SHALL require an invitation for new members to join
5. WHEN a loop is set to public, THE System SHALL allow the loop to appear in search results and allow users to request to join

### Requirement 3

**User Story:** As a loop owner, I want to set my loop as public, so that other users can discover and request to join my sharing group

#### Acceptance Criteria

1. THE System SHALL allow the loop owner to change a private loop to public
2. WHEN a loop is public, THE System SHALL display the loop in public search results with name, description, and member count
3. WHEN a user views a public loop they are not a member of, THE System SHALL display a "Request to Join" button
4. WHEN a user requests to join a public loop, THE System SHALL create a join request that requires owner approval
5. THE System SHALL notify the loop owner when a user requests to join their public loop

### Requirement 4

**User Story:** As a loop owner, I want to transfer ownership of my loop to another member, so that I can step down from administrative responsibilities

#### Acceptance Criteria

1. THE System SHALL allow the current loop owner to select another loop member as the new owner
2. WHEN the owner initiates a transfer, THE System SHALL require confirmation from both the current owner and the new owner
3. WHEN the new owner accepts the transfer, THE System SHALL update the loop's creatorId to the new owner's ID
4. THE System SHALL maintain the original owner as a regular member after the transfer
5. THE System SHALL record the ownership transfer in the loop's history with timestamp and user IDs

### Requirement 5

**User Story:** As a loop owner, I want to archive my loop, so that I can hide it from active use without permanently deleting the data

#### Acceptance Criteria

1. THE System SHALL allow the loop owner to archive a loop through the loop settings
2. WHEN a loop is archived, THE System SHALL set an isArchived flag to true and record the archivedAt timestamp
3. WHEN a loop is archived, THE System SHALL hide the loop from the user's active loops list
4. WHEN a loop is archived, THE System SHALL prevent members from viewing loop items or creating new item requests
5. THE System SHALL allow the loop owner to view archived loops in a separate "Archived Loops" section

### Requirement 6

**User Story:** As a loop owner, I want to restore an archived loop, so that I can reactivate a sharing group that was previously archived

#### Acceptance Criteria

1. THE System SHALL allow the loop owner to restore an archived loop from the archived loops list
2. WHEN a loop is restored, THE System SHALL set the isArchived flag to false and clear the archivedAt timestamp
3. WHEN a loop is restored, THE System SHALL make the loop visible in the user's active loops list again
4. WHEN a loop is restored, THE System SHALL allow members to view items and create requests as before
5. THE System SHALL preserve all loop data including members, items, and history during archive and restore operations

### Requirement 7

**User Story:** As a loop owner, I want to permanently delete my loop, so that I can remove a sharing group that is no longer needed

#### Acceptance Criteria

1. THE System SHALL allow the loop owner to permanently delete a loop through the loop settings
2. WHEN the owner initiates deletion, THE System SHALL display a confirmation dialog warning that the action is irreversible
3. WHEN the owner confirms deletion, THE System SHALL remove the loop from the database
4. WHEN a loop is deleted, THE System SHALL remove the loop ID from all items' visibleToLoopIds arrays
5. WHEN a loop is deleted, THE System SHALL delete all associated loop invitations and join requests

### Requirement 8

**User Story:** As a loop owner, I want to configure loop settings, so that I can customize how my sharing group operates

#### Acceptance Criteria

1. THE System SHALL provide a loop settings page accessible only to the loop owner
2. THE System SHALL display all configurable settings including name, description, and privacy
3. WHEN the owner updates any setting, THE System SHALL validate the changes before saving
4. WHEN settings are saved, THE System SHALL update the loop's updatedAt timestamp
5. THE System SHALL display success or error messages after settings updates

### Requirement 9

**User Story:** As a loop member, I want to leave a loop, so that I can remove myself from a sharing group I no longer wish to participate in

#### Acceptance Criteria

1. THE System SHALL allow any loop member except the owner to leave a loop
2. WHEN a member initiates leaving, THE System SHALL display a confirmation dialog
3. WHEN the member confirms, THE System SHALL remove the member's ID from the loop's memberIds array
4. WHEN a member leaves, THE System SHALL remove the loop ID from all of the member's items' visibleToLoopIds arrays
5. IF the member has active item requests in the loop, THEN THE System SHALL cancel those requests before removing the member

### Requirement 10

**User Story:** As a loop owner, I want to remove a member from my loop, so that I can manage who participates in my sharing group

#### Acceptance Criteria

1. THE System SHALL allow the loop owner to remove any member except themselves from the loop
2. WHEN the owner initiates member removal, THE System SHALL display a confirmation dialog
3. WHEN the owner confirms removal, THE System SHALL remove the member's ID from the loop's memberIds array
4. WHEN a member is removed, THE System SHALL remove the loop ID from all of the removed member's items' visibleToLoopIds arrays
5. IF the removed member has active item requests in the loop, THEN THE System SHALL cancel those requests
