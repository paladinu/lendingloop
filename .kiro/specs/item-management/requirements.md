# Requirements Document

## Introduction

This document specifies the technical requirements for implementing a comprehensive item editing interface that allows users to modify all item properties in a single screen.

## Glossary

- **Item Management System**: The Angular frontend and .NET backend components that handle CRUD operations for shared items
- **Edit Screen**: A dedicated UI component that displays a form for modifying all properties of a shared item

## Requirements

### Requirement 1

**User Story:** As an item owner, I want to access an edit screen for my items, so that I can modify item details after creation

#### Acceptance Criteria

1. WHEN the Item Owner views their own shared item, THE Item Management System SHALL display an edit button or action
2. WHEN the Item Owner clicks the edit action, THE Item Management System SHALL navigate to a dedicated edit screen for that item
3. WHEN a non-owner attempts to access the edit screen for an item, THE Item Management System SHALL deny access and display an authorization error
4. WHEN the Item Owner navigates to the edit screen, THE Item Management System SHALL pre-populate the form with the current item data

### Requirement 2

**User Story:** As an item owner, I want to edit the name and description of my item, so that I can keep the information accurate and up-to-date

#### Acceptance Criteria

1. WHEN the Item Owner views the edit screen, THE Item Management System SHALL display editable text fields for the item name and description
2. WHEN the Item Owner modifies the name field, THE Item Management System SHALL validate that the name is not empty
3. WHEN the Item Owner submits changes with a valid name, THE Item Management System SHALL update the item name in the database
4. WHEN the Item Owner submits changes with valid description, THE Item Management System SHALL update the item description in the database
5. IF the Item Owner submits an empty name, THEN THE Item Management System SHALL display a validation error and prevent submission

### Requirement 3

**User Story:** As an item owner, I want to change my item's availability status, so that I can indicate when the item is not available for lending

#### Acceptance Criteria

1. WHEN the Item Owner views the edit screen, THE Item Management System SHALL display a toggle or checkbox for the availability status
2. WHEN the Item Owner changes the availability status, THE Item Management System SHALL update the visual state of the control
3. WHEN the Item Owner submits changes, THE Item Management System SHALL update the isAvailable field in the database
4. WHEN the availability status is updated, THE Item Management System SHALL set the updatedAt timestamp to the current UTC time

### Requirement 4

**User Story:** As an item owner, I want to update my item's image, so that I can provide better visual representation of the item

#### Acceptance Criteria

1. WHEN the Item Owner views the edit screen, THE Item Management System SHALL display the current item image if one exists
2. WHEN the Item Owner selects a new image file, THE Item Management System SHALL validate that the file type is jpg, jpeg, png, or gif
3. WHEN the Item Owner selects a new image file, THE Item Management System SHALL validate that the file size does not exceed the configured maximum
4. WHEN the Item Owner uploads a valid image, THE Item Management System SHALL save the file to the server and update the imageUrl field
5. IF the Item Owner uploads an invalid file type, THEN THE Item Management System SHALL display an error message and prevent upload

### Requirement 5

**User Story:** As an item owner, I want to modify visibility settings from the edit screen, so that I can control which loops can see my item without navigating to a separate page

#### Acceptance Criteria

1. WHEN the Item Owner views the edit screen, THE Item Management System SHALL display the current visibility settings
2. WHEN the Item Owner modifies the visible-to-all-loops setting, THE Item Management System SHALL update the form state accordingly
3. WHEN the Item Owner modifies the visible-to-future-loops setting, THE Item Management System SHALL update the form state accordingly
4. WHEN the Item Owner selects specific loops for visibility, THE Item Management System SHALL update the visibleToLoopIds list
5. WHEN the Item Owner submits changes, THE Item Management System SHALL update all visibility fields in the database

### Requirement 6

**User Story:** As an item owner, I want to save all my changes at once, so that I can efficiently update multiple fields in a single operation

#### Acceptance Criteria

1. WHEN the Item Owner modifies any field on the edit screen, THE Item Management System SHALL enable the save button
2. WHEN the Item Owner clicks the save button with valid data, THE Item Management System SHALL send an update request to the backend API
3. WHEN the backend receives a valid update request, THE Item Management System SHALL update all modified fields in the database
4. WHEN the update is successful, THE Item Management System SHALL display a success message and navigate back to the previous screen
5. IF the update fails, THEN THE Item Management System SHALL display an error message and keep the user on the edit screen

### Requirement 7

**User Story:** As an item owner, I want to cancel my edits without saving, so that I can discard unwanted changes

#### Acceptance Criteria

1. WHEN the Item Owner views the edit screen, THE Item Management System SHALL display a cancel button
2. WHEN the Item Owner clicks the cancel button, THE Item Management System SHALL navigate back to the previous screen without saving changes
3. WHEN the Item Owner has unsaved changes and clicks cancel, THE Item Management System SHALL discard all modifications
4. WHEN the Item Owner navigates away from the edit screen, THE Item Management System SHALL not persist any unsaved changes
