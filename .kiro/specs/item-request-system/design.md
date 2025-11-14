# Design Document

## Overview

The Item Request System enables users to request items from other users within their loops through a request-approval workflow. The system is built on the existing LendingLoop architecture using .NET 8 Web API backend with MongoDB for data persistence and Angular frontend for the user interface. The design follows the established patterns in the codebase including service-based architecture, JWT authentication, and RESTful API design.

## Architecture

### System Components

The Item Request System consists of three main layers:

1. **Data Layer**: MongoDB collection storing ItemRequest documents
2. **Service Layer**: Business logic for managing item requests (ItemRequestService)
3. **API Layer**: RESTful endpoints for CRUD operations (ItemRequestController)
4. **UI Layer**: Angular service and components for user interactions

### Request Lifecycle

```
[Requester creates request] → Pending
    ↓
[Owner approves] → Approved (item.isAvailable = false)
    ↓
[Owner completes] → Completed (item.isAvailable = true)

[Owner rejects] → Rejected (item.isAvailable = true)
[Requester cancels] → Cancelled (item.isAvailable unchanged)
```

## Components and Interfaces

### Backend Components

#### 1. ItemRequest Model (`api/Models/ItemRequest.cs`)

```csharp
public class ItemRequest
{
    public string? Id { get; set; }
    public string ItemId { get; set; }
    public string RequesterId { get; set; }
    public string OwnerId { get; set; }
    public RequestStatus Status { get; set; }
    public string? Message { get; set; }
    public DateTime RequestedAt { get; set; }
    public DateTime? RespondedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public enum RequestStatus
{
    Pending,
    Approved,
    Rejected,
    Cancelled,
    Completed
}
```

#### 2. ItemRequestService Interface (`api/Services/IItemRequestService.cs`)

```csharp
public interface IItemRequestService
{
    Task<ItemRequest> CreateRequestAsync(string itemId, string requesterId, string? message = null);
    Task<List<ItemRequest>> GetRequestsByRequesterAsync(string requesterId);
    Task<List<ItemRequest>> GetPendingRequestsByOwnerAsync(string ownerId);
    Task<List<ItemRequest>> GetRequestsByItemIdAsync(string itemId);
    Task<ItemRequest?> GetRequestByIdAsync(string requestId);
    Task<ItemRequest?> ApproveRequestAsync(string requestId, string ownerId);
    Task<ItemRequest?> RejectRequestAsync(string requestId, string ownerId);
    Task<ItemRequest?> CancelRequestAsync(string requestId, string requesterId);
    Task<ItemRequest?> CompleteRequestAsync(string requestId, string ownerId);
    Task<ItemRequest?> GetActiveRequestForItemAsync(string itemId);
}
```

#### 3. ItemRequestService Implementation (`api/Services/ItemRequestService.cs`)

Key responsibilities:
- Validate business rules (ownership, status transitions, duplicate approvals)
- Coordinate with ItemsService to update item availability
- Manage request status transitions
- Enforce authorization rules
- Validate and sanitize request messages (max 500 characters)

#### 4. ItemRequestController (`api/Controllers/ItemRequestController.cs`)

RESTful endpoints:
- `POST /api/itemrequests` - Create new request
- `GET /api/itemrequests/my-requests` - Get requester's requests
- `GET /api/itemrequests/pending` - Get owner's pending requests
- `GET /api/itemrequests/item/{itemId}` - Get requests for specific item
- `PUT /api/itemrequests/{id}/approve` - Approve request
- `PUT /api/itemrequests/{id}/reject` - Reject request
- `PUT /api/itemrequests/{id}/cancel` - Cancel request
- `PUT /api/itemrequests/{id}/complete` - Complete request

### Frontend Components

#### 1. ItemRequest Interface (`ui/src/app/models/item-request.interface.ts`)

```typescript
export interface ItemRequest {
    id?: string;
    itemId: string;
    requesterId: string;
    ownerId: string;
    status: RequestStatus;
    message?: string;
    requestedAt: Date;
    respondedAt?: Date;
    completedAt?: Date;
    // Populated fields for display
    itemName?: string;
    requesterName?: string;
    ownerName?: string;
}

export enum RequestStatus {
    Pending = 'Pending',
    Approved = 'Approved',
    Rejected = 'Rejected',
    Cancelled = 'Cancelled',
    Completed = 'Completed'
}
```

#### 2. ItemRequestService (`ui/src/app/services/item-request.service.ts`)

Methods:
- `createRequest(itemId: string, message?: string): Observable<ItemRequest>`
- `getMyRequests(): Observable<ItemRequest[]>`
- `getPendingRequests(): Observable<ItemRequest[]>`
- `getRequestsForItem(itemId: string): Observable<ItemRequest[]>`
- `approveRequest(requestId: string): Observable<ItemRequest>`
- `rejectRequest(requestId: string): Observable<ItemRequest>`
- `cancelRequest(requestId: string): Observable<ItemRequest>`
- `completeRequest(requestId: string): Observable<ItemRequest>`

#### 3. UI Components

**ItemRequestButtonComponent**: Displays request button on item cards with status indicators
- Shows "Request Item" button for available items
- Opens dialog/modal to collect optional message when clicked
- Shows "Pending Request" badge if user has pending request
- Shows "Currently Borrowed" badge if user has approved request
- Disables button when request exists

**ItemRequestListComponent**: Displays list of requests for owners
- Shows pending requests with approve/reject actions
- Displays requester's message if provided
- Shows approved requests with complete action
- Shows historical requests (rejected, cancelled, completed)

**MyRequestsComponent**: Displays requester's requests
- Shows all requests created by user
- Displays the message included with each request
- Allows cancellation of pending requests
- Shows status of all requests

## Data Models

### ItemRequest Collection Schema

```json
{
  "_id": "ObjectId",
  "itemId": "string (ObjectId reference)",
  "requesterId": "string (ObjectId reference)",
  "ownerId": "string (ObjectId reference)",
  "status": "string (enum: Pending, Approved, Rejected, Cancelled, Completed)",
  "message": "string (nullable, max 500 characters)",
  "requestedAt": "ISODate",
  "respondedAt": "ISODate (nullable)",
  "completedAt": "ISODate (nullable)"
}
```

### Database Indexes

```csharp
// Index on itemId for fast item-specific queries
itemId (ascending)

// Index on requesterId for requester's request list
requesterId (ascending)

// Compound index on ownerId + status for pending requests query
ownerId (ascending) + status (ascending)

// Index on status for filtering
status (ascending)

// Index on requestedAt for sorting
requestedAt (descending)
```

### Modified SharedItem Model

No schema changes required. The `isAvailable` field is updated through ItemsService when requests are approved/completed.

## Error Handling

### Backend Error Scenarios

1. **Unauthorized Request Creation**
   - User requests their own item → 400 Bad Request
   - User requests item without visibility → 403 Forbidden

2. **Invalid Status Transitions**
   - Approve non-pending request → 400 Bad Request
   - Complete non-approved request → 400 Bad Request
   - Cancel non-pending request → 400 Bad Request

3. **Authorization Failures**
   - Non-owner approves/rejects/completes → 403 Forbidden
   - Non-requester cancels → 403 Forbidden

4. **Duplicate Approval Prevention**
   - Approve when another approved request exists → 409 Conflict

5. **Resource Not Found**
   - Request ID doesn't exist → 404 Not Found
   - Item ID doesn't exist → 404 Not Found

### Frontend Error Handling

- Display user-friendly error messages using toast notifications
- Handle 401 errors by redirecting to login (existing pattern)
- Show validation errors inline on forms
- Disable buttons during API calls to prevent duplicate submissions
- Refresh data after successful operations

## Testing Strategy

### Backend Unit Tests (`Api.Tests/ItemRequestServiceTests.cs`)

Test categories:
1. **Request Creation Tests**
   - Valid request creation
   - Self-request rejection
   - Unauthorized item access rejection

2. **Request Approval Tests**
   - Valid approval updates item availability
   - Non-owner approval rejection
   - Non-pending approval rejection
   - Duplicate approval prevention

3. **Request Rejection Tests**
   - Valid rejection keeps item available
   - Authorization validation
   - Status validation

4. **Request Cancellation Tests**
   - Valid cancellation by requester
   - Authorization validation
   - Status validation

5. **Request Completion Tests**
   - Valid completion restores availability
   - Authorization validation
   - Status validation

6. **Query Tests**
   - Get requests by requester
   - Get pending requests by owner
   - Get requests by item
   - Get active request for item

### Backend Integration Tests

- Test controller endpoints with authentication
- Verify status code responses
- Test authorization rules
- Verify database state changes

### Frontend Unit Tests

1. **ItemRequestService Tests** (`ui/src/app/services/item-request.service.spec.ts`)
   - HTTP request formatting
   - Error handling
   - Response mapping

2. **Component Tests**
   - ItemRequestButtonComponent: Button states and click handlers
   - ItemRequestListComponent: Request display and action handlers
   - MyRequestsComponent: Request list display and filtering

### Manual Testing Scenarios

1. End-to-end request workflow (create → approve → complete)
2. Multiple users requesting same item
3. Request cancellation flow
4. Request rejection flow
5. UI state updates after actions
6. Error message display

## Security Considerations

### Authentication & Authorization

- All endpoints require JWT authentication
- Owner verification for approve/reject/complete actions
- Requester verification for cancel action
- Item visibility validation before request creation

### Data Validation

- Validate item exists before creating request
- Validate user IDs match authenticated user
- Validate status transitions follow business rules
- Prevent race conditions with database transactions where needed

### Input Sanitization

- Use MongoDB parameterized queries to prevent injection
- Validate ObjectId formats
- Sanitize user input in error messages
- Sanitize request messages to prevent XSS attacks (encode HTML entities)
- Enforce 500 character limit on request messages

## Performance Considerations

### Database Optimization

- Create indexes on frequently queried fields (itemId, requesterId, ownerId, status)
- Use compound indexes for common query patterns
- Limit result sets with pagination if needed in future

### Caching Strategy

- No caching initially; requests are dynamic and change frequently
- Consider caching user/item names for display if performance issues arise

### API Response Optimization

- Return only necessary fields in list endpoints
- Use projection in MongoDB queries
- Consider pagination for large result sets in future iterations

## Future Enhancements

1. **Notifications**: Email/push notifications when requests are created, approved, or rejected
2. **Request Expiration**: Auto-cancel requests after X days
3. **Request Queue**: Allow multiple pending requests with automatic approval of next in queue
4. **Borrowing Duration**: Add expected return date to requests
5. **Ratings**: Allow users to rate borrowing experiences
6. **Message Threading**: Allow back-and-forth messaging between requester and owner
