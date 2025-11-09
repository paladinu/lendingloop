# Implementation Plan

- [x] 1. Create backend notification infrastructure




- [x] 1.1 Create Notification model and NotificationType enum


  - Create `api/Models/Notification.cs` with all required properties
  - Define NotificationType enum with five notification types
  - _Requirements: 8.1, 8.4, 10.5_

- [x] 1.2 Create INotificationService interface


  - Define interface in `api/Services/INotificationService.cs`
  - Include methods for create, retrieve, mark as read, and delete operations
  - _Requirements: 8.1, 8.2, 8.5, 9.1_

- [x] 1.3 Implement NotificationService


  - Create `api/Services/NotificationService.cs` implementing INotificationService
  - Implement CreateNotificationAsync with message formatting
  - Implement GetUserNotificationsAsync with pagination and sorting
  - Implement GetUnreadCountAsync for badge display
  - Implement MarkAsReadAsync with user authorization
  - Implement MarkAllAsReadAsync for bulk operations
  - Implement DeleteNotificationAsync with user authorization
  - Create database indexes for efficient queries
  - _Requirements: 8.1, 8.2, 8.3, 8.4, 8.5, 9.1, 9.2, 9.3, 10.1, 10.2, 10.3, 10.4, 10.5_

- [x] 1.4 Register NotificationService in dependency injection


  - Add service registration in `api/Program.cs`
  - Add MongoDB collection name configuration to appsettings.json
  - _Requirements: 8.1_

- [x] 1.5 Write unit tests for NotificationService


  - Create `Api.Tests/NotificationServiceTests.cs`
  - Test notification creation with all fields
  - Test notification retrieval with pagination
  - Test unread count calculation
  - Test mark as read functionality
  - Test mark all as read functionality
  - Test delete notification with authorization
  - _Requirements: 8.1, 8.2, 8.3, 8.4, 8.5, 9.1, 9.2, 9.3_


- [x] 2. Create NotificationsController API endpoints




- [x] 2.1 Create NotificationsController with authentication


  - Create `api/Controllers/NotificationsController.cs`
  - Add JWT authentication requirement
  - Inject INotificationService
  - _Requirements: 8.2_

- [x] 2.2 Implement GET /api/notifications endpoint


  - Return user's notifications with optional limit parameter
  - Extract user ID from JWT claims
  - Return notifications ordered by creation date
  - _Requirements: 8.2, 8.3_

- [x] 2.3 Implement GET /api/notifications/unread-count endpoint


  - Return count of unread notifications for authenticated user
  - Extract user ID from JWT claims
  - _Requirements: 9.1_

- [x] 2.4 Implement PUT /api/notifications/{id}/read endpoint


  - Mark specific notification as read
  - Verify user authorization
  - Return updated notification
  - _Requirements: 8.5, 9.3_

- [x] 2.5 Implement PUT /api/notifications/mark-all-read endpoint


  - Mark all user notifications as read
  - Return success status
  - _Requirements: 8.5_

- [x] 2.6 Implement DELETE /api/notifications/{id} endpoint


  - Delete specific notification
  - Verify user authorization
  - Return success status
  - _Requirements: 8.1_

- [x] 2.7 Write unit tests for NotificationsController


  - Create `Api.Tests/NotificationsControllerTests.cs`
  - Test all endpoints with valid authentication
  - Test authorization failures
  - Test invalid notification IDs
  - _Requirements: 8.2, 8.5, 9.1_


- [x] 3. Extend EmailService with item request notification templates





- [x] 3.1 Update IEmailService interface with new methods


  - Add SendItemRequestCreatedEmailAsync method
  - Add SendItemRequestApprovedEmailAsync method
  - Add SendItemRequestRejectedEmailAsync method
  - Add SendItemRequestCompletedEmailAsync method
  - Add SendItemRequestCancelledEmailAsync method
  - _Requirements: 1.1, 2.1, 3.1, 4.1, 5.1, 6.1_

- [x] 3.2 Implement item request created email template


  - Create HTML email template in EmailService
  - Include requester name, item name, and request link
  - Use consistent branding with existing templates
  - _Requirements: 1.1, 1.2, 1.3, 1.4, 7.1, 7.2, 7.3, 7.4, 7.5_

- [x] 3.3 Implement item request approved email template


  - Create HTML email template in EmailService
  - Include owner name, item name, and approval message
  - _Requirements: 2.1, 2.3, 2.4, 2.5, 7.1, 7.2, 7.3, 7.4_

- [x] 3.4 Implement item request rejected email template


  - Create HTML email template in EmailService
  - Include owner name, item name, and rejection message
  - _Requirements: 3.1, 3.3, 3.4, 3.5, 7.1, 7.2, 7.3, 7.4_

- [x] 3.5 Implement item request completed email template


  - Create HTML email template in EmailService
  - Include owner name, item name, and completion message
  - _Requirements: 4.1, 4.3, 4.4, 4.5, 7.1, 7.2, 7.3, 7.4_

- [x] 3.6 Implement item request cancelled email template


  - Create HTML email template in EmailService
  - Include requester name, item name, and cancellation message
  - _Requirements: 5.1, 5.3, 5.4, 5.5, 7.1, 7.2, 7.3, 7.4_

- [x] 3.7 Write unit tests for new email methods


  - Add tests to `Api.Tests/EmailServiceTests.cs`
  - Test each email type with valid data
  - Test handling of null/empty parameters
  - Test test mode configuration
  - _Requirements: 1.1, 2.1, 3.1, 4.1, 5.1, 6.2, 6.3_


- [x] 4. Integrate notifications into ItemRequestService




- [x] 4.1 Update ItemRequestService constructor to inject notification services


  - Inject INotificationService
  - Inject IEmailService
  - Inject IUserService for retrieving user details
  - _Requirements: 1.1, 2.1, 3.1, 4.1, 5.1, 6.1_

- [x] 4.2 Add notification helper method to ItemRequestService


  - Create private method to send both email and in-app notifications
  - Handle exceptions gracefully without blocking request operations
  - Log notification failures
  - Retrieve user details (names, emails) for notification context
  - _Requirements: 1.5, 1.6, 2.6, 3.6, 4.6, 5.6, 6.1_

- [x] 4.3 Integrate notifications into CreateRequestAsync


  - Call notification helper after successful request creation
  - Send notification to item owner
  - Include requester name and item name in notification
  - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5, 1.6_

- [x] 4.4 Integrate notifications into ApproveRequestAsync


  - Call notification helper after successful approval
  - Send notification to requester
  - Include owner name and item name in notification
  - _Requirements: 2.1, 2.2, 2.3, 2.4, 2.5, 2.6_

- [x] 4.5 Integrate notifications into RejectRequestAsync


  - Call notification helper after successful rejection
  - Send notification to requester
  - Include owner name and item name in notification
  - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5, 3.6_

- [x] 4.6 Integrate notifications into CompleteRequestAsync


  - Call notification helper after successful completion
  - Send notification to requester
  - Include owner name and item name in notification
  - _Requirements: 4.1, 4.2, 4.3, 4.4, 4.5, 4.6_

- [x] 4.7 Integrate notifications into CancelRequestAsync


  - Call notification helper after successful cancellation
  - Send notification to owner
  - Include requester name and item name in notification
  - _Requirements: 5.1, 5.2, 5.3, 5.4, 5.5, 5.6_

- [x] 4.8 Write integration tests for ItemRequestService notifications


  - Add tests to `Api.Tests/ItemRequestServiceTests.cs`
  - Verify notification created for each request status change
  - Verify email sent for each request status change
  - Verify request processing continues if notification fails
  - _Requirements: 1.1, 1.5, 1.6, 2.1, 2.6, 3.1, 3.6, 4.1, 4.6, 5.1, 5.6_


- [x] 5. Create frontend notification models and service








- [x] 5.1 Create Notification interface and NotificationType enum

  - Create `ui/src/app/models/notification.interface.ts`
  - Define Notification interface with all properties
  - Define NotificationType enum matching backend
  - _Requirements: 8.4, 10.5_


- [x] 5.2 Create NotificationService

  - Create `ui/src/app/services/notification.service.ts`
  - Implement getNotifications method with optional limit
  - Implement getUnreadCount method
  - Implement markAsRead method
  - Implement markAllAsRead method
  - Implement deleteNotification method
  - Add proper error handling
  - _Requirements: 8.2, 8.5, 9.1_


- [x] 5.3 Write unit tests for NotificationService



  - Create `ui/src/app/services/notification.service.spec.ts`
  - Test HTTP request formatting for all methods
  - Test error handling
  - Test response mapping
  - _Requirements: 8.2, 8.5, 9.1_

- [x] 6. Create notification bell component for toolbar




- [x] 6.1 Generate NotificationBellComponent


  - Create component in `ui/src/app/components/notification-bell/`
  - Add component to toolbar module
  - _Requirements: 9.4_


- [x] 6.2 Implement notification bell UI

  - Add bell icon to component template
  - Add unread count badge
  - Style badge to display count prominently
  - Hide badge when count is zero
  - _Requirements: 9.4_

- [x] 6.3 Implement notification bell logic


  - Fetch unread count on component init
  - Poll for unread count every 30 seconds
  - Toggle dropdown on bell click
  - Update count when notifications are marked as read
  - _Requirements: 9.1, 9.4, 9.5_


- [x] 6.4 Write unit tests for NotificationBellComponent

  - Create component test file
  - Test unread count display
  - Test badge visibility logic
  - Test dropdown toggle
  - Test polling mechanism
  - _Requirements: 9.1, 9.4, 9.5_

- [x] 7. Create notification dropdown component

- [x] 7.1 Generate NotificationDropdownComponent


  - Create component in `ui/src/app/components/notification-dropdown/`
  - Add component to shared module
  - _Requirements: 8.2_

- [x] 7.2 Implement notification dropdown UI

  - Create dropdown template with notification list
  - Display notification message, type icon, and timestamp
  - Add "View All" link to notifications page
  - Show "No notifications" empty state
  - Limit display to 10 most recent notifications
  - _Requirements: 8.2, 8.3, 10.1, 10.2, 10.4_

- [x] 7.3 Implement notification dropdown logic

  - Fetch recent notifications when dropdown opens
  - Mark notification as read when clicked
  - Navigate to related item/request on notification click
  - Handle notification type icons and styling
  - _Requirements: 8.2, 8.5, 9.3, 10.3, 10.5_

- [x] 7.4 Write unit tests for NotificationDropdownComponent


  - Create component test file
  - Test notification list display
  - Test empty state
  - Test mark as read on click
  - Test navigation on notification click
  - _Requirements: 8.2, 8.5, 9.3_

- [x] 8. Create notifications page component



- [x] 8.1 Generate NotificationsPageComponent


  - Create component in `ui/src/app/components/notifications-page/`
  - Add route to app routing module
  - Add AuthGuard to route
  - _Requirements: 8.2_

- [x] 8.2 Implement notifications page UI

  - Create page template with full notification list
  - Add filter controls for read/unread status
  - Add "Mark All as Read" button
  - Display notification details with timestamps
  - Add delete button for individual notifications
  - Show empty state when no notifications
  - _Requirements: 8.2, 8.3, 8.5, 10.1, 10.2, 10.4_


- [x] 8.3 Implement notifications page logic
  - Fetch all user notifications on page load
  - Implement read/unread filter functionality
  - Implement mark all as read action
  - Implement delete notification action
  - Navigate to related items/requests on notification click
  - Refresh list after actions
  - _Requirements: 8.2, 8.3, 8.5, 10.3_


- [x] 8.4 Write unit tests for NotificationsPageComponent


  - Create component test file
  - Test notification list display
  - Test filter functionality
  - Test mark all as read
  - Test delete notification
  - Test navigation
  - _Requirements: 8.2, 8.5_


- [x] 9. Integrate notification bell into application toolbar








- [x] 9.1 Add NotificationBellComponent to toolbar


  - Import NotificationBellComponent in toolbar component
  - Add notification bell to toolbar template
  - Position bell icon appropriately in toolbar layout
  - _Requirements: 9.4_


- [x] 9.2 Style notification bell integration









  - Ensure bell icon matches toolbar styling
  - Ensure badge is visible and prominent
  - Ensure dropdown positioning works correctly
  - Test responsive behavior on mobile
  - _Requirements: 9.4_

- [-] 10. End-to-end testing and polish



- [x] 10.1 Test complete notification flow




  - Create item request and verify owner receives notifications
  - Approve request and verify requester receives notifications
  - Complete request and verify requester receives notifications
  - Test rejection and cancellation flows
  - Verify email delivery for all notification types
  - _Requirements: 1.1, 2.1, 3.1, 4.1, 5.1_

- [x] 10.2 Test notification UI interactions





  - Verify unread count updates correctly
  - Test notification dropdown functionality
  - Test notifications page functionality
  - Test mark as read and delete operations
  - Verify navigation to related items/requests
  - _Requirements: 8.2, 8.5, 9.1, 9.3, 9.4, 9.5_

- [x] 10.3 Test error scenarios





  - Verify request processing continues if email fails
  - Verify request processing continues if notification creation fails
  - Test graceful handling of notification service unavailability
  - Test authorization on notification endpoints
  - _Requirements: 1.5, 1.6, 2.6, 3.6, 4.6, 5.6_



- [x] 10.4 Verify all tests pass













  - Run backend tests: `dotnet test --verbosity minimal --nologo` from `/Api.Tests`
  - Run frontend tests: `npm test -- --silent` from `/ui`
  - Fix any failing tests
  - Ensure all new functionality is covered by tests
  - _Requirements: All_
