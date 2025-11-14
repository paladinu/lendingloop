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

- [x] 13. Add message field to ItemRequest model







  - Update `api/Models/ItemRequest.cs` to include optional Message property (string, nullable)
  - Add BSON attribute for MongoDB serialization
  - _Requirements: 9.2_

- [x] 14. Update ItemRequestService to support messages







- [x] 14.1 Update CreateRequestAsync to accept message parameter



  - Modify `IItemRequestService.CreateRequestAsync` signature to include optional message parameter
  - Implement message validation (max 500 characters) in `ItemRequestService.CreateRequestAsync`
  - Sanitize message content to prevent XSS attacks (encode HTML entities)
  - Store message with ItemRequest in database
  - _Requirements: 9.1, 9.2, 9.5_


- [x] 14.2 Write unit tests for message validation




  - Add tests to `Api.Tests/ItemRequestServiceTests.cs` for message validation
  - Test message length validation (reject messages over 500 characters)
  - Test message sanitization (verify HTML entities are encoded)
  - Test null/empty messages are accepted
  - _Requirements: 9.1, 9.5_

- [x] 15. Update ItemRequestController to accept messages







  - Modify POST `/api/itemrequests` endpoint to accept optional message in request body
  - Create request DTO with itemId and optional message fields
  - Pass message to ItemRequestService.CreateRequestAsync
  - Return 400 Bad Request if message exceeds 500 characters
  - _Requirements: 9.1, 9.2_

- [x] 16. Update frontend ItemRequest interface






  - Add optional message property to `ui/src/app/models/item-request.interface.ts`
  - _Requirements: 9.2_

- [x] 17. Update ItemRequestService in Angular





- [x] 17.1 Modify createRequest method to accept message




  - Update `ui/src/app/services/item-request.service.ts` createRequest method signature
  - Include message in HTTP POST request body
  - _Requirements: 9.1, 9.2_

- [x] 17.2 Update ItemRequestService unit tests





  - Update `ui/src/app/services/item-request.service.spec.ts` to test message parameter
  - Verify message is included in HTTP request body
  - _Requirements: 9.1, 9.2_

- [x] 18. Add message input to ItemRequestButton component





- [x] 18.1 Create request dialog/modal component




  - Create `ui/src/app/components/item-request-dialog/item-request-dialog.component.ts`
  - Add textarea for message input with 500 character limit
  - Add character counter showing remaining characters
  - Add submit and cancel buttons
  - Display item name in dialog title
  - _Requirements: 9.1, 9.3_

- [x] 18.2 Update ItemRequestButton to open dialog




  - Modify `ui/src/app/components/item-request-button/item-request-button.component.ts`
  - Open dialog when "Request Item" button is clicked
  - Pass message from dialog to createRequest method
  - Handle dialog cancellation (don't create request)
  - _Requirements: 9.1, 9.3_

- [x] 18.3 Write unit tests for request dialog





  - Create `ui/src/app/components/item-request-dialog/item-request-dialog.component.spec.ts`
  - Test character limit enforcement
  - Test submit and cancel actions
  - _Requirements: 9.1_

- [x] 19. Display messages in ItemRequestList component







  - Update `ui/src/app/components/item-request-list/item-request-list.component.html`
  - Display requester's message below request details if message exists
  - Show "No message provided" or hide section if no message
  - Style message display to be visually distinct
  - _Requirements: 9.3_

- [x] 20. Display messages in MyRequests component







  - Update `ui/src/app/components/my-requests/my-requests.component.html`
  - Display the message included with each request
  - Show "No message" if no message was provided
  - _Requirements: 9.4_

- [x] 21. Add expected return date field to ItemRequest model



  - Update `api/Models/ItemRequest.cs` to include optional ExpectedReturnDate property (DateTime?, nullable)
  - Add BSON attribute for MongoDB serialization
  - _Requirements: 11.2_

- [x] 22. Update ItemRequestService to support expected return date


- [x] 22.1 Update CreateRequestAsync to accept expectedReturnDate parameter



  - Modify `IItemRequestService.CreateRequestAsync` signature to include optional expectedReturnDate parameter
  - Implement validation in `ItemRequestService.CreateRequestAsync` to reject past dates
  - Store expectedReturnDate with ItemRequest in database
  - _Requirements: 11.1, 11.2, 11.3_

- [x] 22.2 Write unit tests for expected return date validation



  - Add tests to `Api.Tests/ItemRequestServiceTests.cs` for expectedReturnDate validation
  - Test past date rejection (should return validation error)
  - Test future date acceptance
  - Test null/empty expectedReturnDate is accepted
  - _Requirements: 11.1, 11.3_


- [x] 23. Update ItemRequestController to accept expected return date


  - Modify POST `/api/itemrequests` endpoint to accept optional expectedReturnDate in request body
  - Update request DTO to include expectedReturnDate field
  - Pass expectedReturnDate to ItemRequestService.CreateRequestAsync
  - Return 400 Bad Request if expectedReturnDate is in the past
  - _Requirements: 11.1, 11.2, 11.3_


- [x] 24. Update frontend ItemRequest interface


  - Add optional expectedReturnDate property to `ui/src/app/models/item-request.interface.ts`
  - _Requirements: 11.2_

- [x] 25. Update ItemRequestService in Angular



- [x] 25.1 Modify createRequest method to accept expectedReturnDate


  - Update `ui/src/app/services/item-request.service.ts` createRequest method signature
  - Include expectedReturnDate in HTTP POST request body
  - _Requirements: 11.1, 11.2_



- [ ] 25.2 Update ItemRequestService unit tests

  - Update `ui/src/app/services/item-request.service.spec.ts` to test expectedReturnDate parameter
  - Verify expectedReturnDate is included in HTTP request body
  - _Requirements: 11.1, 11.2_


- [ ] 26. Add expected return date input to ItemRequestDialog component

- [x] 26.1 Add date picker to request dialog


  - Update `ui/src/app/components/item-request-dialog/item-request-dialog.component.ts`
  - Add date picker input for expected return date
  - Set minimum date to tomorrow (prevent past dates)
  - Make field optional
  - _Requirements: 11.1, 11.4_

- [x] 26.2 Update dialog template



  - Update `ui/src/app/components/item-request-dialog/item-request-dialog.component.html`
  - Add date picker field below message textarea
  - Add label "Expected Return Date (Optional)"
  - Style date picker consistently with message input
  - _Requirements: 11.1, 11.4_



- [ ] 26.3 Update ItemRequestButton to pass expectedReturnDate

  - Modify `ui/src/app/components/item-request-button/item-request-button.component.ts`
  - Pass expectedReturnDate from dialog to createRequest method
  - _Requirements: 11.1_



- [ ] 26.4 Write unit tests for date picker

  - Update `ui/src/app/components/item-request-dialog/item-request-dialog.component.spec.ts`
  - Test date picker validation (minimum date enforcement)
  - Test submit with and without expectedReturnDate
  - _Requirements: 11.1_


- [x] 27. Display expected return date in ItemRequestList component


  - Update `ui/src/app/components/item-request-list/item-request-list.component.html`
  - Display expected return date below message if provided
  - Format date in user-friendly format (e.g., "Expected return: Jan 15, 2026")
  - Show "No return date specified" or hide section if not provided
  - _Requirements: 11.4_


- [x] 28. Display expected return date in MyRequests component



  - Update `ui/src/app/components/my-requests/my-requests.component.html`
  - Display expected return date for each request
  - Format date consistently with ItemRequestList
  - Show "No return date" if not provided
  - _Requirements: 11.5_
