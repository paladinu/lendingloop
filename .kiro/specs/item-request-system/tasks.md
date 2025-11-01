# Implementation Plan

- [x] 1. Create ItemRequest model and enum


  - Create `api/Models/ItemRequest.cs` with all required properties (Id, ItemId, RequesterId, OwnerId, Status, RequestedAt, RespondedAt, CompletedAt)
  - Create `RequestStatus` enum with values: Pending, Approved, Rejected, Cancelled, Completed
  - Add MongoDB BSON attributes for proper serialization
  - _Requirements: 1.2, 2.2, 3.3, 4.3, 5.3, 6.3_

- [x] 2. Create ItemRequestService interface and implementation

- [x] 2.1 Create IItemRequestService interface


  - Define interface at `api/Services/IItemRequestService.cs` with all required methods
  - Include methods for create, approve, reject, cancel, complete, and query operations
  - _Requirements: 1.1, 2.1, 3.1, 4.1, 5.1, 6.1_

- [x] 2.2 Implement ItemRequestService with request creation


  - Create `api/Services/ItemRequestService.cs` with MongoDB collection setup
  - Implement `CreateRequestAsync` with validation for self-requests and item visibility
  - Implement database indexes for itemId, requesterId, ownerId+status, and requestedAt
  - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5_

- [x] 2.3 Implement request approval logic

  - Implement `ApproveRequestAsync` with owner authorization check
  - Implement `GetActiveRequestForItemAsync` to check for existing approved requests
  - Add validation to prevent multiple active requests per item
  - Coordinate with IItemsService to set item.isAvailable to false
  - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5, 7.1, 7.2_

- [x] 2.4 Implement request rejection logic

  - Implement `RejectRequestAsync` with owner authorization check
  - Ensure item.isAvailable remains true
  - Add status validation to only reject pending requests
  - _Requirements: 4.1, 4.2, 4.3, 4.4, 4.5_

- [x] 2.5 Implement request cancellation logic

  - Implement `CancelRequestAsync` with requester authorization check
  - Add status validation to only cancel pending requests
  - _Requirements: 5.1, 5.2, 5.3, 5.4, 5.5_

- [x] 2.6 Implement request completion logic

  - Implement `CompleteRequestAsync` with owner authorization check
  - Coordinate with IItemsService to set item.isAvailable to true
  - Add status validation to only complete approved requests
  - _Requirements: 6.1, 6.2, 6.3, 6.4, 6.5_

- [x] 2.7 Implement query methods

  - Implement `GetRequestsByRequesterAsync` to return all requests by a requester
  - Implement `GetPendingRequestsByOwnerAsync` to return pending requests for owner's items
  - Implement `GetRequestsByItemIdAsync` to return all requests for a specific item
  - Implement `GetRequestByIdAsync` to return a single request
  - Add sorting by requestedAt in descending order
  - _Requirements: 2.1, 2.2, 2.3, 2.4_

- [x] 2.8 Write unit tests for ItemRequestService


  - Create `Api.Tests/ItemRequestServiceTests.cs` following AAA pattern
  - Test request creation with valid and invalid scenarios
  - Test approval, rejection, cancellation, and completion flows
  - Test authorization checks and status validations
  - Test duplicate approval prevention
  - _Requirements: All requirements 1-7_

- [x] 3. Create ItemRequestController with API endpoints

- [x] 3.1 Create ItemRequestController with create endpoint


  - Create `api/Controllers/ItemRequestController.cs` with authorization
  - Implement POST `/api/itemrequests` endpoint to create requests
  - Extract userId from JWT claims
  - Return appropriate status codes (201, 400, 403, 404, 500)
  - _Requirements: 1.1, 1.2, 1.4, 1.5_

- [x] 3.2 Add query endpoints

  - Implement GET `/api/itemrequests/my-requests` for requester's requests
  - Implement GET `/api/itemrequests/pending` for owner's pending requests
  - Implement GET `/api/itemrequests/item/{itemId}` for item-specific requests
  - _Requirements: 2.1, 2.2, 2.3, 2.4_

- [x] 3.3 Add action endpoints

  - Implement PUT `/api/itemrequests/{id}/approve` for approval
  - Implement PUT `/api/itemrequests/{id}/reject` for rejection
  - Implement PUT `/api/itemrequests/{id}/cancel` for cancellation
  - Implement PUT `/api/itemrequests/{id}/complete` for completion
  - Return appropriate error responses for authorization and validation failures
  - _Requirements: 3.1-3.5, 4.1-4.5, 5.1-5.5, 6.1-6.5_

- [x] 3.4 Write integration tests for ItemRequestController


  - Create `Api.Tests/ItemRequestControllerTests.cs` following AAA pattern
  - Test all endpoints with valid authentication
  - Test authorization failures (403 responses)
  - Test validation failures (400 responses)
  - _Requirements: All requirements 1-7_

- [x] 4. Register ItemRequestService in dependency injection


  - Update `api/Program.cs` to register IItemRequestService and ItemRequestService
  - Ensure MongoDB database is injected correctly
  - _Requirements: All requirements_

- [x] 5. Create frontend ItemRequest interface and enum


  - Create `ui/src/app/models/item-request.interface.ts` with ItemRequest interface
  - Create RequestStatus enum matching backend values
  - Add optional display fields (itemName, requesterName, ownerName)
  - _Requirements: 2.2, 8.1, 8.2_

- [x] 6. Create ItemRequestService in Angular

- [x] 6.1 Implement ItemRequestService with HTTP methods


  - Create `ui/src/app/services/item-request.service.ts`
  - Implement createRequest, getMyRequests, getPendingRequests, getRequestsForItem methods
  - Implement approveRequest, rejectRequest, cancelRequest, completeRequest methods
  - Add error handling following existing pattern (401 redirect, error messages)
  - _Requirements: 1.1, 2.1, 3.1, 4.1, 5.1, 6.1_

- [x] 6.2 Write unit tests for ItemRequestService


  - Create `ui/src/app/services/item-request.service.spec.ts`
  - Test HTTP request formatting and error handling
  - Mock HttpClient responses
  - _Requirements: All requirements 1-6_

- [x] 7. Create ItemRequestButton component

- [x] 7.1 Implement ItemRequestButton component


  - Create `ui/src/app/components/item-request-button/item-request-button.component.ts`
  - Accept @Input() itemId and @Input() ownerId
  - Check for existing requests on init using getRequestsForItem
  - Display "Request Item" button when no request exists
  - Display "Pending Request" badge when pending request exists
  - Display "Currently Borrowed" badge when approved request exists
  - Disable button when request exists
  - Emit @Output() requestCreated event after successful request
  - _Requirements: 1.1, 8.1, 8.2, 8.3_

- [x] 7.2 Create ItemRequestButton template


  - Create `ui/src/app/components/item-request-button/item-request-button.component.html`
  - Add button with conditional styling based on request status
  - Add loading state during API calls
  - Add error message display
  - _Requirements: 8.1, 8.2, 8.3_

- [x] 7.3 Write unit tests for ItemRequestButton component


  - Create `ui/src/app/components/item-request-button/item-request-button.component.spec.ts`
  - Test button states based on request status
  - Test click handler and event emission
  - _Requirements: 8.1, 8.2, 8.3_

- [x] 8. Create ItemRequestList component for owners

- [x] 8.1 Implement ItemRequestList component


  - Create `ui/src/app/components/item-request-list/item-request-list.component.ts`
  - Load pending requests on init using getPendingRequests
  - Implement approve, reject, and complete action handlers
  - Refresh list after actions
  - Display error messages using toast/snackbar
  - _Requirements: 2.1, 2.2, 2.3, 3.1, 4.1, 6.1_

- [x] 8.2 Create ItemRequestList template


  - Create `ui/src/app/components/item-request-list/item-request-list.component.html`
  - Display list of pending requests with item name and requester name
  - Add approve and reject buttons for pending requests
  - Add complete button for approved requests
  - Show empty state when no requests exist
  - _Requirements: 2.2, 2.3_

- [x] 8.3 Write unit tests for ItemRequestList component


  - Create `ui/src/app/components/item-request-list/item-request-list.component.spec.ts`
  - Test request list display
  - Test action button handlers
  - _Requirements: 2.1, 2.2, 3.1, 4.1, 6.1_

- [x] 9. Create MyRequests component for requesters


- [x] 9.1 Implement MyRequests component


  - Create `ui/src/app/components/my-requests/my-requests.component.ts`
  - Load user's requests on init using getMyRequests
  - Implement cancel action handler for pending requests
  - Display requests grouped by status
  - Refresh list after cancellation
  - _Requirements: 2.1, 2.4, 5.1_

- [x] 9.2 Create MyRequests template


  - Create `ui/src/app/components/my-requests/my-requests.component.html`
  - Display list of all user's requests with item name and status
  - Add cancel button for pending requests
  - Show status badges with appropriate colors
  - Show empty state when no requests exist
  - _Requirements: 2.4_

- [x] 9.3 Write unit tests for MyRequests component


  - Create `ui/src/app/components/my-requests/my-requests.component.spec.ts`
  - Test request list display and filtering
  - Test cancel action handler
  - _Requirements: 2.1, 2.4, 5.1_

- [x] 10. Integrate ItemRequestButton into existing item display components


  - Add ItemRequestButton component to item cards in loop view
  - Pass itemId and ownerId as inputs
  - Handle requestCreated event to refresh item list
  - Only show button when viewing other users' items
  - _Requirements: 1.1, 8.1, 8.2, 8.3_

- [x] 11. Add navigation and routing for request components


  - Add routes for ItemRequestList and MyRequests components
  - Add navigation links in toolbar/menu
  - Update app routing module
  - _Requirements: 2.1, 2.4_

- [x] 12. Add request count badges to navigation


  - Update ItemRequestService to include method for getting pending request count
  - Display badge on navigation showing number of pending requests for owners
  - Update badge count after request actions
  - _Requirements: 2.1_
