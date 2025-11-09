# Design Document

## Overview

The Item Request Notifications feature extends the existing Item Request System by adding both email and in-app notification capabilities. When item request status changes occur (create, approve, reject, complete, cancel), the system will notify relevant users through two channels: email notifications for immediate awareness and in-app notifications for when users are actively using the application.

This design leverages the existing EmailService infrastructure for email delivery and introduces a new NotificationService for managing in-app notifications. The implementation follows the established patterns in the LendingLoop codebase including service-based architecture, JWT authentication, and RESTful API design.

## Architecture

### System Components

The notification system consists of the following layers:

1. **Data Layer**: MongoDB collection storing Notification documents
2. **Service Layer**: 
   - NotificationService: Manages in-app notification CRUD operations
   - EmailService: Existing service extended with new email templates
   - ItemRequestService: Modified to trigger notifications on status changes
3. **API Layer**: NotificationsController for in-app notification endpoints
4. **UI Layer**: Angular components and services for displaying notifications

### Notification Flow

```
[Item Request Status Change]
    ↓
[ItemRequestService]
    ↓
    ├─→ [NotificationService.CreateNotificationAsync()] → MongoDB
    └─→ [EmailService.SendItemRequestEmailAsync()] → SMTP Server
```

### Integration Points

- ItemRequestService methods (Create, Approve, Reject, Complete, Cancel) will be modified to call notification services
- Notifications are sent asynchronously and failures do not block request processing
- Email and in-app notifications are independent - failure of one does not affect the other

## Components and Interfaces

### Backend Components

#### 1. Notification Model (`api/Models/Notification.cs`)


```csharp
public class Notification
{
    public string? Id { get; set; }
    public string UserId { get; set; }
    public NotificationType Type { get; set; }
    public string Message { get; set; }
    public string? ItemId { get; set; }
    public string? ItemRequestId { get; set; }
    public string? RelatedUserId { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
}

public enum NotificationType
{
    ItemRequestCreated,
    ItemRequestApproved,
    ItemRequestRejected,
    ItemRequestCompleted,
    ItemRequestCancelled
}
```

#### 2. NotificationService Interface (`api/Services/INotificationService.cs`)

```csharp
public interface INotificationService
{
    Task<Notification> CreateNotificationAsync(string userId, NotificationType type, 
        string message, string? itemId = null, string? itemRequestId = null, 
        string? relatedUserId = null);
    Task<List<Notification>> GetUserNotificationsAsync(string userId, int limit = 50);
    Task<int> GetUnreadCountAsync(string userId);
    Task<Notification?> MarkAsReadAsync(string notificationId, string userId);
    Task<bool> MarkAllAsReadAsync(string userId);
    Task<bool> DeleteNotificationAsync(string notificationId, string userId);
}
```

#### 3. NotificationService Implementation (`api/Services/NotificationService.cs`)

Key responsibilities:
- Create notifications with proper type and message formatting
- Retrieve user notifications with pagination
- Track read/unread status
- Enforce user authorization for notification access
- Maintain database indexes for efficient queries


#### 4. NotificationsController (`api/Controllers/NotificationsController.cs`)

RESTful endpoints:
- `GET /api/notifications` - Get user's notifications (with optional limit)
- `GET /api/notifications/unread-count` - Get count of unread notifications
- `PUT /api/notifications/{id}/read` - Mark notification as read
- `PUT /api/notifications/mark-all-read` - Mark all notifications as read
- `DELETE /api/notifications/{id}` - Delete a notification

#### 5. Extended IEmailService (`api/Services/IEmailService.cs`)

New methods to add:
```csharp
Task<bool> SendItemRequestCreatedEmailAsync(string ownerEmail, string ownerName, 
    string requesterName, string itemName, string requestId);
Task<bool> SendItemRequestApprovedEmailAsync(string requesterEmail, string requesterName, 
    string ownerName, string itemName);
Task<bool> SendItemRequestRejectedEmailAsync(string requesterEmail, string requesterName, 
    string ownerName, string itemName);
Task<bool> SendItemRequestCompletedEmailAsync(string requesterEmail, string requesterName, 
    string ownerName, string itemName);
Task<bool> SendItemRequestCancelledEmailAsync(string ownerEmail, string ownerName, 
    string requesterName, string itemName);
```

#### 6. Modified ItemRequestService

Each status change method will be updated to:
1. Perform the existing business logic
2. Retrieve user details (names, emails) for notification context
3. Create in-app notification via NotificationService
4. Send email notification via EmailService
5. Log any notification failures without blocking the request operation


### Frontend Components

#### 1. Notification Interface (`ui/src/app/models/notification.interface.ts`)

```typescript
export interface Notification {
    id: string;
    userId: string;
    type: NotificationType;
    message: string;
    itemId?: string;
    itemRequestId?: string;
    relatedUserId?: string;
    isRead: boolean;
    createdAt: Date;
}

export enum NotificationType {
    ItemRequestCreated = 'ItemRequestCreated',
    ItemRequestApproved = 'ItemRequestApproved',
    ItemRequestRejected = 'ItemRequestRejected',
    ItemRequestCompleted = 'ItemRequestCompleted',
    ItemRequestCancelled = 'ItemRequestCancelled'
}
```

#### 2. NotificationService (`ui/src/app/services/notification.service.ts`)

Methods:
- `getNotifications(limit?: number): Observable<Notification[]>`
- `getUnreadCount(): Observable<number>`
- `markAsRead(notificationId: string): Observable<Notification>`
- `markAllAsRead(): Observable<boolean>`
- `deleteNotification(notificationId: string): Observable<boolean>`

#### 3. UI Components

**NotificationBellComponent**: Displays notification icon with unread count badge
- Shows bell icon in toolbar/header
- Displays unread count badge when count > 0
- Opens notification dropdown on click
- Polls for unread count periodically (every 30 seconds)

**NotificationDropdownComponent**: Displays recent notifications in dropdown
- Shows list of recent notifications (limit 10)
- Displays notification message, type icon, and timestamp
- Marks notifications as read when viewed
- Provides link to full notifications page
- Shows "No notifications" message when empty

**NotificationsPageComponent**: Full page view of all notifications
- Displays all user notifications with pagination
- Allows filtering by read/unread status
- Provides mark all as read action
- Allows individual notification deletion
- Shows notification details with links to related items/requests


## Data Models

### Notification Collection Schema

```json
{
  "_id": "ObjectId",
  "userId": "string (ObjectId reference)",
  "type": "string (enum: ItemRequestCreated, ItemRequestApproved, etc.)",
  "message": "string",
  "itemId": "string (ObjectId reference, nullable)",
  "itemRequestId": "string (ObjectId reference, nullable)",
  "relatedUserId": "string (ObjectId reference, nullable)",
  "isRead": "boolean",
  "createdAt": "ISODate"
}
```

### Database Indexes

```csharp
// Compound index on userId + createdAt for user notification queries
userId (ascending) + createdAt (descending)

// Compound index on userId + isRead for unread count queries
userId (ascending) + isRead (ascending)

// Index on createdAt for cleanup/archival operations
createdAt (descending)
```

### Email Templates

Five new HTML email templates will be added to EmailService:

1. **Item Request Created**: Notifies owner of new request
   - Subject: "New request for your item: {itemName}"
   - Includes requester name, item name, link to view request

2. **Item Request Approved**: Notifies requester of approval
   - Subject: "Your request for {itemName} has been approved"
   - Includes owner name, item name, pickup instructions

3. **Item Request Rejected**: Notifies requester of rejection
   - Subject: "Your request for {itemName} was not approved"
   - Includes owner name, item name, encouragement to browse other items

4. **Item Request Completed**: Notifies requester of completion
   - Subject: "Your borrowing of {itemName} is complete"
   - Includes owner name, item name, thank you message

5. **Item Request Cancelled**: Notifies owner of cancellation
   - Subject: "Request for {itemName} has been cancelled"
   - Includes requester name, item name, item availability status


## Error Handling

### Backend Error Scenarios

1. **Notification Creation Failures**
   - Database connection issues → Log error, continue request processing
   - Invalid user ID → Log error, continue request processing
   - Notification service unavailable → Log error, continue request processing

2. **Email Sending Failures**
   - SMTP connection issues → Retry via EmailService, log if all retries fail
   - Invalid email address → Log error, continue request processing
   - Email service not configured → Log warning, continue request processing

3. **Notification Retrieval Failures**
   - Invalid notification ID → 404 Not Found
   - Unauthorized access → 403 Forbidden
   - Database query errors → 500 Internal Server Error

4. **Authorization Failures**
   - User accessing another user's notifications → 403 Forbidden
   - Unauthenticated requests → 401 Unauthorized

### Frontend Error Handling

- Display user-friendly error messages for notification operations
- Gracefully handle notification service unavailability
- Show fallback UI when notifications cannot be loaded
- Retry failed notification count requests
- Handle 401 errors by redirecting to login

### Notification Failure Strategy

Critical principle: **Notification failures must never block item request operations**

- All notification operations are wrapped in try-catch blocks
- Failures are logged but do not throw exceptions to calling code
- Email and in-app notifications are independent
- Request processing continues even if both notification types fail


## Testing Strategy

### Backend Unit Tests

#### NotificationService Tests (`Api.Tests/NotificationServiceTests.cs`)

Test categories:
1. **Notification Creation Tests**
   - Valid notification creation with all fields
   - Notification creation with optional fields null
   - Multiple notifications for same user

2. **Notification Retrieval Tests**
   - Get notifications for user with results
   - Get notifications for user with no results
   - Pagination with limit parameter
   - Ordering by creation date (newest first)

3. **Unread Count Tests**
   - Count unread notifications correctly
   - Return zero when no unread notifications
   - Update count after marking as read

4. **Mark as Read Tests**
   - Mark single notification as read
   - Verify user authorization
   - Handle non-existent notification ID

5. **Mark All as Read Tests**
   - Mark all user notifications as read
   - Only affect authenticated user's notifications

6. **Delete Notification Tests**
   - Delete notification successfully
   - Verify user authorization
   - Handle non-existent notification ID

#### Email Service Tests (`Api.Tests/EmailServiceTests.cs`)

New test categories:
1. **Item Request Email Tests**
   - Send request created email with valid data
   - Send approval email with valid data
   - Send rejection email with valid data
   - Send completion email with valid data
   - Send cancellation email with valid data
   - Handle null/empty parameters gracefully
   - Respect test mode configuration


#### ItemRequestService Integration Tests

Test categories:
1. **Notification Integration Tests**
   - Verify notification created when request is created
   - Verify notification created when request is approved
   - Verify notification created when request is rejected
   - Verify notification created when request is completed
   - Verify notification created when request is cancelled
   - Verify request processing continues if notification fails

2. **Email Integration Tests**
   - Verify email sent when request is created
   - Verify email sent when request is approved
   - Verify email sent when request is rejected
   - Verify email sent when request is completed
   - Verify email sent when request is cancelled
   - Verify request processing continues if email fails

### Frontend Unit Tests

#### NotificationService Tests (`ui/src/app/services/notification.service.spec.ts`)
- HTTP request formatting for all endpoints
- Error handling for failed requests
- Response mapping to Notification interface

#### Component Tests

**NotificationBellComponent Tests**
- Display unread count badge correctly
- Hide badge when count is zero
- Toggle dropdown on click
- Poll for unread count updates

**NotificationDropdownComponent Tests**
- Display notifications list
- Show empty state when no notifications
- Mark notification as read on click
- Navigate to notifications page

**NotificationsPageComponent Tests**
- Display all notifications
- Filter by read/unread status
- Mark all as read functionality
- Delete notification functionality


### Manual Testing Scenarios

1. **End-to-end notification flow**
   - Create item request → Verify owner receives email and in-app notification
   - Approve request → Verify requester receives email and in-app notification
   - Complete request → Verify requester receives email and in-app notification

2. **Notification UI interactions**
   - Verify unread count badge displays correctly
   - Click notification bell → Dropdown opens with recent notifications
   - Click notification → Marks as read and navigates to related item/request
   - Mark all as read → All notifications marked, badge disappears

3. **Email delivery verification**
   - Check email inbox for all notification types
   - Verify email formatting and content
   - Verify links in emails work correctly

4. **Error scenarios**
   - Disable email service → Verify requests still process
   - Simulate notification service failure → Verify requests still process
   - Invalid email address → Verify graceful handling

## Security Considerations

### Authentication & Authorization

- All notification endpoints require JWT authentication
- Users can only access their own notifications
- Notification creation is internal to ItemRequestService (not exposed via API)
- User ID validation on all notification operations

### Data Privacy

- Notifications contain minimal sensitive information
- Email addresses are not exposed in notifications
- User IDs are validated before notification creation
- Notifications are user-scoped and isolated

### Input Validation

- Validate notification IDs are valid ObjectIds
- Sanitize notification messages before storage
- Validate user IDs match authenticated user
- Prevent notification injection attacks


## Performance Considerations

### Database Optimization

- Create compound indexes on userId + createdAt for efficient queries
- Create compound index on userId + isRead for unread count queries
- Limit notification queries with default pagination (50 items)
- Consider archival strategy for old notifications (future enhancement)

### Caching Strategy

- Cache unread count in frontend for 30 seconds between polls
- No backend caching initially due to real-time nature of notifications
- Consider Redis caching for unread counts if performance issues arise

### API Response Optimization

- Return only necessary fields in notification list endpoints
- Use MongoDB projection to limit data transfer
- Implement pagination for large notification lists
- Limit dropdown to 10 most recent notifications

### Email Performance

- Email sending is asynchronous and non-blocking
- Leverage existing EmailService retry mechanism
- Email failures are logged but don't impact user experience
- Consider background job queue for email sending (future enhancement)

### Frontend Performance

- Lazy load notifications page component
- Use virtual scrolling for large notification lists (future enhancement)
- Debounce notification polling to prevent excessive API calls
- Cache notification list between page navigations


## Implementation Notes

### Dependency Injection

The following services need to be registered in `Program.cs`:

```csharp
builder.Services.AddScoped<INotificationService, NotificationService>();
```

The ItemRequestService constructor will be updated to inject:
- INotificationService
- IEmailService
- IUserService (to retrieve user details for notifications)

### MongoDB Configuration

Add to `appsettings.json`:

```json
{
  "MongoDB": {
    "NotificationsCollectionName": "notifications"
  }
}
```

### Message Templates

Notification messages will follow consistent patterns:

- Request Created: "{RequesterName} requested to borrow your {ItemName}"
- Request Approved: "{OwnerName} approved your request for {ItemName}"
- Request Rejected: "{OwnerName} declined your request for {ItemName}"
- Request Completed: "{OwnerName} marked your borrowing of {ItemName} as complete"
- Request Cancelled: "{RequesterName} cancelled their request for {ItemName}"

### Angular Routing

Add new route for notifications page:

```typescript
{ path: 'notifications', component: NotificationsPageComponent, canActivate: [AuthGuard] }
```

### Toolbar Integration

The NotificationBellComponent will be added to the existing toolbar component alongside other navigation elements.


## Future Enhancements

1. **Real-time Notifications**: Implement WebSocket or SignalR for instant notification delivery without polling
2. **Notification Preferences**: Allow users to configure which notifications they want to receive
3. **Push Notifications**: Add browser push notifications for desktop/mobile
4. **Notification Grouping**: Group related notifications (e.g., multiple requests for same item)
5. **Notification Archival**: Automatically archive or delete old notifications after X days
6. **Rich Notifications**: Include item images and more detailed information in notifications
7. **Notification History**: Separate archive view for old/deleted notifications
8. **Email Digest**: Option to receive daily/weekly email digest instead of individual emails
9. **SMS Notifications**: Add SMS notification option for critical updates
10. **Notification Analytics**: Track notification delivery rates and user engagement

## Migration Strategy

Since this is a new feature, no data migration is required. The implementation steps are:

1. Create Notification model and database collection
2. Implement NotificationService with database operations
3. Add new email templates to EmailService
4. Modify ItemRequestService to trigger notifications
5. Create NotificationsController with API endpoints
6. Implement frontend notification service
7. Create notification UI components
8. Integrate notification bell into toolbar
9. Add notifications page route
10. Test end-to-end notification flow

The feature can be deployed incrementally:
- Phase 1: Backend notification infrastructure
- Phase 2: Email notifications
- Phase 3: In-app notification UI
- Phase 4: Polish and optimization
