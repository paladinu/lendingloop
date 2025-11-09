# Requirements Document

## Introduction

This feature adds notification capabilities to the Item Request System, enabling users to receive timely updates about item request activities through both email and in-app notifications. When a user creates, approves, rejects, or completes an item request, relevant parties will receive notifications to keep them informed of the request status changes. This enhancement improves user engagement and reduces the need for users to constantly check the application for updates.

## Glossary

- **Item Request System**: The existing system that manages borrowing requests between users within loops
- **Requester**: The user who creates an item request to borrow an item
- **Owner**: The user who owns the item and must approve or reject borrow requests
- **Email Service**: The existing service that handles sending emails via SMTP
- **Item Request Service**: The backend service that manages item request business logic
- **Notification**: A message sent to inform users about item request status changes, delivered via email and/or in-app
- **In-App Notification**: A notification displayed within the application interface that users can view when logged in
- **Notification Service**: The backend service that manages creating and retrieving notifications

## Requirements

### Requirement 1

**User Story:** As an item owner, I want to receive notifications when someone requests to borrow my item, so that I can respond promptly without constantly checking the application

#### Acceptance Criteria

1. WHEN a Requester creates a new item request, THE Item Request System SHALL send an email notification to the Owner
2. WHEN a Requester creates a new item request, THE Item Request System SHALL create an in-app notification for the Owner
3. THE Item Request System SHALL include the requester's name in both notification types
4. THE Item Request System SHALL include the item name in both notification types
5. THE Item Request System SHALL include a direct link to view the request in the email notification
6. IF the email fails to send, THEN THE Item Request System SHALL log the error and continue processing the request

### Requirement 2

**User Story:** As a requester, I want to receive notifications when the owner approves my request, so that I know I can pick up the item

#### Acceptance Criteria

1. WHEN an Owner approves an item request, THE Item Request System SHALL send an email notification to the Requester
2. WHEN an Owner approves an item request, THE Item Request System SHALL create an in-app notification for the Requester
3. THE Item Request System SHALL include the owner's name in both notification types
4. THE Item Request System SHALL include the item name in both notification types
5. THE Item Request System SHALL include a message indicating the request was approved in both notification types
6. IF the email fails to send, THEN THE Item Request System SHALL log the error and continue processing the approval

### Requirement 3

**User Story:** As a requester, I want to receive notifications when the owner rejects my request, so that I know the item is not available and can make alternative arrangements

#### Acceptance Criteria

1. WHEN an Owner rejects an item request, THE Item Request System SHALL send an email notification to the Requester
2. WHEN an Owner rejects an item request, THE Item Request System SHALL create an in-app notification for the Requester
3. THE Item Request System SHALL include the owner's name in both notification types
4. THE Item Request System SHALL include the item name in both notification types
5. THE Item Request System SHALL include a message indicating the request was rejected in both notification types
6. IF the email fails to send, THEN THE Item Request System SHALL log the error and continue processing the rejection

### Requirement 4

**User Story:** As a requester, I want to receive notifications when the owner marks my request as completed, so that I have confirmation that the borrowing transaction is finished

#### Acceptance Criteria

1. WHEN an Owner completes an item request, THE Item Request System SHALL send an email notification to the Requester
2. WHEN an Owner completes an item request, THE Item Request System SHALL create an in-app notification for the Requester
3. THE Item Request System SHALL include the owner's name in both notification types
4. THE Item Request System SHALL include the item name in both notification types
5. THE Item Request System SHALL include a message indicating the request was completed in both notification types
6. IF the email fails to send, THEN THE Item Request System SHALL log the error and continue processing the completion

### Requirement 5

**User Story:** As an item owner, I want to receive notifications when a requester cancels their request, so that I am aware the item is available again for other requests

#### Acceptance Criteria

1. WHEN a Requester cancels an item request, THE Item Request System SHALL send an email notification to the Owner
2. WHEN a Requester cancels an item request, THE Item Request System SHALL create an in-app notification for the Owner
3. THE Item Request System SHALL include the requester's name in both notification types
4. THE Item Request System SHALL include the item name in both notification types
5. THE Item Request System SHALL include a message indicating the request was cancelled in both notification types
6. IF the email fails to send, THEN THE Item Request System SHALL log the error and continue processing the cancellation

### Requirement 6

**User Story:** As a system administrator, I want email notifications to use the existing email service infrastructure, so that configuration and maintenance remain centralized

#### Acceptance Criteria

1. THE Item Request System SHALL use the existing Email Service for sending all notifications
2. THE Item Request System SHALL respect the Email Service test mode configuration
3. THE Item Request System SHALL use the Email Service retry mechanism for failed email deliveries
4. THE Item Request System SHALL use HTML email templates consistent with existing email templates
5. THE Item Request System SHALL not duplicate email configuration or SMTP logic

### Requirement 7

**User Story:** As a user, I want notification emails to have clear and professional formatting, so that I can easily understand the information and take appropriate action

#### Acceptance Criteria

1. THE Item Request System SHALL format notification emails using HTML templates
2. THE Item Request System SHALL include the application branding in notification emails
3. THE Item Request System SHALL use clear subject lines that indicate the notification type
4. THE Item Request System SHALL include all relevant information (item name, user names, status) in the email body
5. WHERE applicable, THE Item Request System SHALL include action links to view requests or items in the application

### Requirement 8

**User Story:** As a user, I want to view my in-app notifications in a centralized location, so that I can see all my recent activity and updates

#### Acceptance Criteria

1. THE Item Request System SHALL store in-app notifications in the database with a reference to the recipient user
2. THE Item Request System SHALL provide an API endpoint to retrieve notifications for the authenticated user
3. THE Item Request System SHALL order notifications by creation date with newest first
4. THE Item Request System SHALL include notification type, message, creation timestamp, and read status in the notification data
5. THE Item Request System SHALL support marking notifications as read

### Requirement 9

**User Story:** As a user, I want to see a visual indicator when I have unread notifications, so that I know there are updates requiring my attention

#### Acceptance Criteria

1. THE Item Request System SHALL provide an API endpoint to retrieve the count of unread notifications for the authenticated user
2. THE Item Request System SHALL mark new notifications as unread by default
3. WHEN a user views a notification, THE Item Request System SHALL mark it as read
4. THE Item Request System SHALL display a badge or indicator in the UI showing the count of unread notifications
5. THE Item Request System SHALL update the unread count in real-time when notifications are marked as read

### Requirement 10

**User Story:** As a user, I want in-app notifications to include relevant context and actions, so that I can quickly understand and respond to updates

#### Acceptance Criteria

1. THE Item Request System SHALL include the item name in in-app notifications
2. THE Item Request System SHALL include the relevant user name (requester or owner) in in-app notifications
3. THE Item Request System SHALL include a link or reference to the related item request in in-app notifications
4. THE Item Request System SHALL use clear, concise language in notification messages
5. THE Item Request System SHALL categorize notifications by type (request created, approved, rejected, completed, cancelled)
