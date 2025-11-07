# Implementation Plan: Loop Management

- [x] 1. Update backend data models



  - Add new fields to Loop model: description, isPublic, isArchived, archivedAt, ownershipHistory
  - Create OwnershipTransfer class with TransferStatus enum
  - Create LoopJoinRequest model with JoinRequestStatus enum
  - _Requirements: 1.1, 2.1, 2.2, 3.1, 4.1, 5.1, 7.1_

- [x] 2. Extend LoopService with settings management


  - [x] 2.1 Implement loop settings methods


    - Add UpdateLoopSettingsAsync method to update name, description, and isPublic
    - Add IsLoopOwnerAsync method for authorization checks
    - Update MongoDB update operations to set updatedAt timestamp
    - _Requirements: 1.1, 1.2, 1.3, 2.1, 2.2, 3.1, 8.1, 8.2, 8.3, 8.4_

- [x] 3. Implement loop archival functionality


  - [x] 3.1 Add archival methods to LoopService


    - Implement ArchiveLoopAsync to set isArchived flag and archivedAt timestamp
    - Implement RestoreLoopAsync to clear isArchived flag and archivedAt
    - Implement GetArchivedLoopsAsync to query archived loops for a user
    - Add database index on isArchived field
    - _Requirements: 5.1, 5.2, 5.3, 5.4, 5.5, 6.1, 6.2, 6.3, 6.4, 6.5_

- [x] 4. Implement loop deletion with cascade cleanup


  - [x] 4.1 Add deletion method to LoopService


    - Implement DeleteLoopAsync to remove loop from database
    - Update ItemsService to remove loopId from all items' visibleToLoopIds arrays
    - Update LoopInvitationService to delete associated invitations
    - Implement cascade cleanup for join requests (will be created in task 6)
    - _Requirements: 7.1, 7.2, 7.3, 7.4, 7.5_

- [x] 5. Implement ownership transfer workflow


  - [x] 5.1 Add ownership transfer methods to LoopService


    - Implement InitiateOwnershipTransferAsync to create pending transfer in ownershipHistory
    - Implement AcceptOwnershipTransferAsync to update creatorId and mark transfer as accepted
    - Implement DeclineOwnershipTransferAsync to mark transfer as declined
    - Implement CancelOwnershipTransferAsync to mark transfer as cancelled
    - Implement GetPendingOwnershipTransferAsync to retrieve pending transfer
    - Ensure only one pending transfer exists at a time
    - _Requirements: 4.1, 4.2, 4.3, 4.4, 4.5_

- [x] 6. Create LoopJoinRequestService for public loops


  - [x] 6.1 Implement ILoopJoinRequestService interface and service


    - Create MongoDB collection for loopJoinRequests
    - Implement CreateJoinRequestAsync to create join request
    - Implement GetJoinRequestByIdAsync for single request retrieval
    - Implement GetPendingJoinRequestsForLoopAsync for owner view
    - Implement GetUserJoinRequestsAsync for user's requests
    - Implement ApproveJoinRequestAsync to add user to loop and update request status
    - Implement RejectJoinRequestAsync to update request status
    - Implement HasPendingJoinRequestAsync to prevent duplicate requests
    - Create database indexes for loopJoinRequests collection
    - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5_

- [x] 7. Extend LoopService with public loop discovery


  - [x] 7.1 Add public loop query methods


    - Implement GetPublicLoopsAsync with pagination (skip, limit)
    - Implement SearchPublicLoopsAsync with text search on name and description
    - Add compound index on isPublic and isArchived fields
    - Add text index on description field
    - _Requirements: 3.1, 3.2_

- [x] 8. Implement member management in LoopService


  - [x] 8.1 Add member removal and leave methods


    - Update RemoveMemberFromLoopAsync to handle item visibility cleanup
    - Create LeaveLoopAsync method for non-owner members
    - Update ItemsService to remove loopId from member's items when they leave/are removed
    - Cancel active item requests when member leaves/is removed
    - Prevent owner from leaving without transferring ownership
    - _Requirements: 9.1, 9.2, 9.3, 9.4, 9.5, 10.1, 10.2, 10.3, 10.4, 10.5_

- [x] 9. Update LoopsController with settings endpoints


  - [x] 9.1 Add loop settings management endpoints


    - Create PUT /api/loops/{id}/settings endpoint with authorization check
    - Create GET /api/loops/{id}/settings endpoint
    - Validate description length (max 500 characters)
    - Validate user is loop owner for updates
    - Return appropriate error codes for validation failures
    - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5, 2.1, 2.2, 3.1, 8.1, 8.2, 8.3, 8.4_

- [x] 10. Add archival endpoints to LoopsController


  - [x] 10.1 Implement archive and restore endpoints

    - Create POST /api/loops/{id}/archive endpoint with owner authorization
    - Create POST /api/loops/{id}/restore endpoint with owner authorization
    - Create GET /api/loops/archived endpoint to get user's archived loops
    - Update GET /api/loops endpoint to exclude archived loops by default
    - _Requirements: 5.1, 5.2, 5.3, 5.4, 5.5, 6.1, 6.2, 6.3, 6.4, 6.5_

- [x] 11. Add deletion endpoint to LoopsController


  - [x] 11.1 Implement permanent deletion endpoint

    - Create DELETE /api/loops/{id} endpoint with owner authorization
    - Add confirmation requirement in API documentation
    - Call cascade cleanup methods from LoopService
    - Return 204 No Content on successful deletion
    - _Requirements: 7.1, 7.2, 7.3, 7.4, 7.5_

- [x] 12. Add ownership transfer endpoints to LoopsController


  - [x] 12.1 Implement ownership transfer workflow endpoints

    - Create POST /api/loops/{id}/transfer-ownership endpoint to initiate transfer
    - Create POST /api/loops/{id}/transfer-ownership/accept endpoint
    - Create POST /api/loops/{id}/transfer-ownership/decline endpoint
    - Create POST /api/loops/{id}/transfer-ownership/cancel endpoint
    - Create GET /api/loops/{id}/transfer-ownership/pending endpoint
    - Validate target user is a loop member
    - Authorize current owner for initiate/cancel, new owner for accept/decline
    - _Requirements: 4.1, 4.2, 4.3, 4.4, 4.5_

- [x] 13. Create LoopJoinRequestsController


  - [x] 13.1 Implement join request endpoints


    - Create POST /api/loops/{id}/join-requests endpoint to create request
    - Create GET /api/loops/{id}/join-requests endpoint for owner to view pending requests
    - Create POST /api/loops/join-requests/{requestId}/approve endpoint
    - Create POST /api/loops/join-requests/{requestId}/reject endpoint
    - Create GET /api/loops/join-requests/my-requests endpoint for user's requests
    - Validate loop is public before allowing join request
    - Authorize loop owner for approve/reject actions
    - Prevent duplicate pending requests
    - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5_

- [x] 14. Add public loop discovery endpoints to LoopsController


  - [x] 14.1 Implement public loop query endpoints

    - Create GET /api/loops/public endpoint with pagination parameters
    - Create GET /api/loops/public/search endpoint with query parameter
    - Return limited loop information for non-members (exclude memberIds)
    - Include member count and item count in response
    - _Requirements: 3.1, 3.2_

- [x] 15. Add member management endpoints to LoopsController


  - [x] 15.1 Implement member removal and leave endpoints

    - Create DELETE /api/loops/{id}/members/{userId} endpoint for owner to remove members
    - Create POST /api/loops/{id}/leave endpoint for members to leave
    - Validate owner cannot leave without transferring ownership
    - Call cascade cleanup for item visibility and requests
    - _Requirements: 9.1, 9.2, 9.3, 9.4, 9.5, 10.1, 10.2, 10.3, 10.4, 10.5_

- [x] 16. Register new services in Program.cs



  - Register ILoopJoinRequestService and LoopJoinRequestService with dependency injection
  - Ensure MongoDB collections are properly configured
  - _Requirements: 3.1_

- [x] 17. Update frontend TypeScript models


  - Update Loop interface with new fields: description, isPublic, isArchived, archivedAt, ownershipHistory
  - Create OwnershipTransfer interface with status enum
  - Create LoopJoinRequest interface with status enum
  - Create LoopSettings interface for settings form
  - _Requirements: 1.1, 2.1, 3.1, 4.1, 5.1_

- [x] 18. Extend LoopService in Angular


  - [x] 18.1 Add settings management methods

    - Implement updateLoopSettings method
    - Implement getLoopSettings method
    - Implement isLoopOwner method
    - _Requirements: 1.1, 1.2, 8.1, 8.2_
  
  - [x] 18.2 Add archival methods

    - Implement archiveLoop method
    - Implement restoreLoop method
    - Implement getArchivedLoops method
    - Implement deleteLoop method
    - _Requirements: 5.1, 5.2, 6.1, 7.1_
  
  - [x] 18.3 Add ownership transfer methods

    - Implement initiateOwnershipTransfer method
    - Implement acceptOwnershipTransfer method
    - Implement declineOwnershipTransfer method
    - Implement cancelOwnershipTransfer method
    - Implement getPendingOwnershipTransfer method
    - _Requirements: 4.1, 4.2, 4.3_
  
  - [x] 18.4 Add public loop discovery methods

    - Implement getPublicLoops method with pagination
    - Implement searchPublicLoops method
    - _Requirements: 3.1, 3.2_
  
  - [x] 18.5 Add join request methods

    - Implement createJoinRequest method
    - Implement getLoopJoinRequests method
    - Implement approveJoinRequest method
    - Implement rejectJoinRequest method
    - Implement getMyJoinRequests method
    - _Requirements: 3.1, 3.3, 3.4, 3.5_
  
  - [x] 18.6 Add member management methods

    - Implement removeMember method
    - Implement leaveLoop method
    - _Requirements: 9.1, 10.1_

- [x] 19. Create LoopSettingsComponent


  - [x] 19.1 Implement loop settings form


    - Create component with form for name, description, and privacy toggle
    - Load current loop settings on init
    - Implement form validation (description max 500 chars)
    - Implement save settings functionality
    - Add archive/restore button based on loop state
    - Add delete loop button with confirmation dialog
    - Add "Transfer Ownership" button linking to transfer component
    - Display success/error messages
    - Restrict access to loop owner only
    - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5, 2.1, 2.2, 3.1, 5.1, 7.1, 8.1, 8.2, 8.3, 8.4_

- [x] 20. Create ArchivedLoopsComponent


  - [x] 20.1 Implement archived loops list


    - Fetch and display user's archived loops
    - Show loop name, archived date, member count
    - Add "Restore" button for each loop
    - Add "Delete Permanently" button with confirmation
    - Implement restore functionality
    - Implement delete functionality
    - Navigate to loop detail on restore
    - Display empty state when no archived loops
    - _Requirements: 5.5, 6.1, 6.2, 6.3, 6.4, 6.5, 7.1, 7.2, 7.3_

- [x] 21. Create PublicLoopsComponent

  - [x] 21.1 Implement public loop discovery

    - Fetch and display public loops with pagination
    - Add search input with real-time filtering
    - Display loop cards with name, description, member count
    - Add "Request to Join" button for non-member loops
    - Disable button if user already has pending request
    - Implement join request creation with optional message
    - Add "View Loop" button for loops user is already a member of
    - Implement infinite scroll or pagination controls
    - Display loading states and empty state
    - _Requirements: 3.1, 3.2, 3.3, 3.4_

- [x] 22. Create LoopJoinRequestsComponent

  - [x] 22.1 Implement join requests management for owners

    - Fetch and display pending join requests for loop
    - Show requester name, email, message, and request date
    - Add "Approve" and "Reject" buttons for each request
    - Implement approve functionality to add user to loop
    - Implement reject functionality
    - Remove request from list after action
    - Display empty state when no pending requests
    - Restrict access to loop owner only
    - _Requirements: 3.4, 3.5_

- [x] 23. Create MyJoinRequestsComponent

  - [x] 23.1 Implement user's join requests view

    - Fetch and display user's join requests across all loops
    - Show loop name, status, request date, and response date
    - Display status badge (Pending, Approved, Rejected)
    - Add link to loop detail for approved requests
    - Display empty state when no requests
    - _Requirements: 3.4_

- [x] 24. Create OwnershipTransferComponent

  - [x] 24.1 Implement ownership transfer interface

    - Fetch loop members excluding current owner
    - Display member selection dropdown or list
    - Add "Transfer Ownership" button to initiate transfer
    - Display pending transfer status if one exists
    - Show "Accept" button if user is the target of pending transfer
    - Show "Decline" button if user is the target of pending transfer
    - Show "Cancel" button if user is the initiator of pending transfer
    - Implement initiate, accept, decline, and cancel actions
    - Display confirmation dialogs for all actions
    - Navigate back to loop detail on successful transfer
    - Restrict access to loop owner and transfer target
    - _Requirements: 4.1, 4.2, 4.3, 4.4, 4.5_

- [x] 25. Create LoopMembersComponent

  - [x] 25.1 Implement loop members management

    - Fetch and display loop members with name and email
    - Highlight loop owner in the list
    - Add "Remove" button next to each member (owner view only)
    - Add "Leave Loop" button for non-owner members
    - Implement remove member functionality with confirmation
    - Implement leave loop functionality with confirmation
    - Prevent owner from leaving (show message to transfer ownership first)
    - Update member list after actions
    - Display member count
    - _Requirements: 9.1, 9.2, 9.3, 9.4, 9.5, 10.1, 10.2, 10.3, 10.4, 10.5_

- [x] 26. Update LoopListComponent

  - [x] 26.1 Add archived loops link and filter

    - Update component to exclude archived loops from main list
    - Add "View Archived Loops" link/button
    - Update loop cards to show privacy indicator (public/private icon)
    - _Requirements: 2.1, 3.1, 5.3, 5.5_

- [x] 27. Update LoopDetailComponent

  - [x] 27.1 Add settings and management links

    - Add "Settings" button in header (visible to owner only)
    - Add "Members" link to view/manage members
    - Add "Join Requests" badge with count (visible to owner only)
    - Display loop description below loop name
    - Show public/private indicator
    - Restrict access to archived loops (show message to owner)
    - _Requirements: 1.3, 2.1, 3.1, 5.4_

- [x] 28. Update LoopCreateComponent

  - [x] 28.1 Add description and privacy fields

    - Add description textarea to creation form
    - Add privacy toggle (public/private) with default to private
    - Add character counter for description (max 500)
    - Update form validation
    - Update form submission to include new fields
    - _Requirements: 1.1, 2.1, 2.2_

- [x] 29. Configure routing for new components

  - Add route for /loops/settings/:id (LoopSettingsComponent)
  - Add route for /loops/archived (ArchivedLoopsComponent)
  - Add route for /loops/public (PublicLoopsComponent)
  - Add route for /loops/:id/join-requests (LoopJoinRequestsComponent)
  - Add route for /loops/my-join-requests (MyJoinRequestsComponent)
  - Add route for /loops/:id/transfer-ownership (OwnershipTransferComponent)
  - Add route for /loops/:id/members (LoopMembersComponent)
  - Add route guards for owner-only routes
  - _Requirements: 1.1, 2.1, 3.1, 4.1, 5.1, 9.1, 10.1_

- [x] 30. Update navigation and UI integration


  - Add "Discover Public Loops" link to main navigation or loops page
  - Add "My Join Requests" link to user menu or loops page
  - Add "Archived Loops" link to loops page
  - Update loop cards to show public/private badge
  - Add settings icon to loop cards for owner
  - Ensure consistent styling across new components
  - _Requirements: 2.1, 3.1, 5.5_
