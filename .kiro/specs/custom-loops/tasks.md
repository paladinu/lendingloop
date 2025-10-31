# Implementation Plan: Custom Loops

- [x] 1. Create backend data models and enums


  - Create Loop model with MongoDB attributes
  - Create LoopInvitation model with InvitationStatus enum
  - Update SharedItem model to add loop visibility fields (visibleToLoopIds, visibleToAllLoops, visibleToFutureLoops, description, createdAt, updatedAt)
  - _Requirements: 1.1, 1.2, 2.1, 4.1, 4.2, 4.3, 4.4, 4.5_

- [x] 2. Implement LoopService with database operations


  - [x] 2.1 Create ILoopService interface and LoopService implementation


    - Implement CreateLoopAsync method to create loop with creator as first member
    - Implement GetLoopByIdAsync for retrieving loop details
    - Implement GetUserLoopsAsync to get all loops for a user
    - Implement GetLoopMembersAsync to retrieve member details
    - Implement IsUserLoopMemberAsync for authorization checks
    - Implement AddMemberToLoopAsync and RemoveMemberFromLoopAsync
    - Implement GetPotentialInviteesFromOtherLoopsAsync to find users from other loops
    - Create MongoDB indexes for loops collection (creatorId, memberIds, name)
    - _Requirements: 1.1, 1.2, 1.3, 3.1, 7.1, 7.2_

- [x] 3. Implement LoopInvitationService with email integration


  - [x] 3.1 Create ILoopInvitationService interface and LoopInvitationService implementation


    - Implement CreateEmailInvitationAsync to generate invitation with unique token
    - Implement CreateUserInvitationAsync for in-app invitations
    - Implement AcceptInvitationAsync for email-based acceptance
    - Implement AcceptInvitationByUserAsync for logged-in user acceptance
    - Implement GetPendingInvitationsForUserAsync and GetPendingInvitationsForLoopAsync
    - Implement ExpireOldInvitationsAsync for cleanup
    - Integrate with existing EmailService to send invitation emails
    - Create MongoDB indexes for loopInvitations collection
    - _Requirements: 2.1, 2.2, 2.3, 2.4, 2.5, 3.2, 3.3, 3.4, 3.5_

- [x] 4. Update ItemsService for loop visibility


  - [x] 4.1 Extend IItemsService interface and ItemsService implementation


    - Implement GetItemsByLoopIdAsync to query items visible to a loop
    - Implement UpdateItemVisibilityAsync to update loop visibility settings
    - Implement GetItemByIdAsync for single item retrieval
    - Update CreateItemAsync to handle new visibility fields
    - Create MongoDB indexes for items collection (visibleToLoopIds, compound userId+visibleToLoopIds)
    - _Requirements: 4.1, 4.2, 4.3, 4.4, 4.5, 5.1, 5.2, 8.1, 8.2, 8.3, 8.4, 8.5_

- [x] 5. Create LoopsController with API endpoints


  - [x] 5.1 Implement loop management endpoints


    - Create POST /api/loops endpoint for loop creation
    - Create GET /api/loops endpoint to get user's loops
    - Create GET /api/loops/{id} endpoint for loop details
    - Create GET /api/loops/{id}/members endpoint for loop members
    - Create GET /api/loops/{id}/items endpoint for loop items with search support
    - Add JWT authentication and authorization checks
    - Implement error handling and validation
    - _Requirements: 1.1, 1.2, 1.4, 5.1, 5.2, 5.3, 5.4, 5.5, 6.1, 6.2, 6.3, 6.4, 6.5, 7.1, 7.2, 7.3_
  
  - [x] 5.2 Implement invitation endpoints


    - Create POST /api/loops/{id}/invite-email endpoint for email invitations
    - Create POST /api/loops/{id}/invite-user endpoint for user invitations
    - Create GET /api/loops/{id}/potential-invitees endpoint
    - Create POST /api/loops/invitations/{token}/accept endpoint for email acceptance
    - Create POST /api/loops/invitations/{id}/accept-user endpoint for user acceptance
    - Create GET /api/loops/invitations/pending endpoint for pending invitations
    - Add authorization checks to ensure only loop members can invite
    - _Requirements: 2.1, 2.2, 2.3, 2.4, 2.5, 3.1, 3.2, 3.3, 3.4, 3.5_

- [x] 6. Update ItemsController for visibility management


  - Create PUT /api/items/{id}/visibility endpoint to update item loop visibility
  - Add authorization check to ensure user owns the item
  - Validate that user is member of specified loops
  - Implement error handling for invalid loop IDs
  - _Requirements: 4.1, 4.2, 4.3, 4.4, 4.5, 8.1, 8.2, 8.3, 8.4, 8.5_

- [x] 7. Register services in Program.cs


  - Register ILoopService and LoopService with dependency injection
  - Register ILoopInvitationService and LoopInvitationService
  - Update IItemsService registration if needed
  - Ensure MongoDB collections are properly configured
  - _Requirements: 1.1, 2.1, 4.1_

- [x] 8. Create frontend TypeScript models and interfaces


  - Create Loop interface with all properties
  - Create LoopInvitation interface with status enum
  - Update SharedItem interface to include loop visibility fields
  - Create LoopMember interface for member display
  - _Requirements: 1.1, 2.1, 3.1, 4.1, 5.1, 7.1_

- [x] 9. Implement LoopService in Angular


  - [x] 9.1 Create loop.service.ts with HTTP methods


    - Implement createLoop method
    - Implement getUserLoops method
    - Implement getLoopById method
    - Implement getLoopMembers method
    - Implement getLoopItems method with search parameter
    - Implement inviteByEmail method
    - Implement inviteUser method
    - Implement getPotentialInvitees method
    - Implement acceptInvitationByToken method
    - Implement acceptInvitationByUser method
    - Implement getPendingInvitations method
    - Add error handling and HTTP interceptor integration
    - _Requirements: 1.1, 2.1, 2.5, 3.1, 3.5, 5.1, 6.1, 7.1_

- [x] 10. Create LoopListComponent


  - [x] 10.1 Implement loop list display


    - Create component template with loop cards showing name, member count, item count
    - Implement component logic to fetch and display user's loops
    - Add navigation to loop detail page on click
    - Add "Create Loop" button linking to create form
    - Add loading state and error handling
    - Style component with responsive design
    - _Requirements: 7.1, 7.2, 7.3, 7.4_

- [x] 11. Create LoopCreateComponent


  - [x] 11.1 Implement loop creation form


    - Create form with loop name input and validation
    - Implement form submission to create loop
    - Add client-side validation for empty/invalid names
    - Navigate to loop detail page on successful creation
    - Display error messages for validation failures
    - Add cancel button to return to loop list
    - _Requirements: 1.1, 1.2, 1.4_

- [x] 12. Create LoopDetailComponent (landing page)


  - [x] 12.1 Implement loop landing page with items display


    - Create component template with loop header showing name and member count
    - Implement items grid/list display with item cards
    - Add search input field with real-time filtering
    - Implement search functionality across item titles, descriptions, and tags
    - Display item details (title, description, owner, date, image)
    - Show empty state message when no items exist
    - Add navigation to members view and invite page
    - Implement loading states and error handling
    - _Requirements: 5.1, 5.2, 5.3, 5.4, 5.5, 6.1, 6.2, 6.3, 6.4, 6.5, 7.3_

- [x] 13. Create LoopInviteComponent


  - [x] 13.1 Implement email invitation form


    - Create form with email input and validation
    - Implement email invitation submission
    - Display success/error messages
    - Add email format validation
    - _Requirements: 2.1, 2.2, 2.3, 2.4_
  
  - [x] 13.2 Implement user invitation from other loops

    - Fetch and display list of potential invitees from user's other loops
    - Add checkboxes or selection mechanism for multiple users
    - Implement batch invitation submission
    - Display invitation status and feedback
    - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5_

- [x] 14. Create LoopInvitationsComponent


  - [x] 14.1 Implement pending invitations display


    - Fetch and display pending invitations for logged-in user
    - Show invitation details (loop name, invited by, date)
    - Add "Accept" button for each invitation
    - Implement accept invitation action
    - Remove accepted invitations from list
    - Add loading and error states
    - _Requirements: 3.4, 3.5_

- [x] 15. Create AcceptInvitationComponent


  - [x] 15.1 Implement email invitation acceptance page


    - Extract token from query parameters
    - Call acceptInvitationByToken on component init
    - Display success message and redirect to loop on success
    - Display error message for expired/invalid tokens
    - Handle unauthenticated users by redirecting to login
    - _Requirements: 2.5_

- [x] 16. Create ItemVisibilityComponent


  - [x] 16.1 Implement item visibility settings form


    - Fetch user's loops and current item visibility settings
    - Display checkboxes for each loop
    - Add "Visible to all current loops" checkbox
    - Add "Visible to future loops" checkbox
    - Implement form submission to update visibility
    - Display success/error messages
    - Update items service to refresh item data
    - _Requirements: 4.1, 4.2, 4.3, 4.4, 4.5, 8.1, 8.2, 8.3, 8.4, 8.5_

- [x] 17. Update ItemsService in Angular


  - Add updateItemVisibility method to call PUT /api/items/{id}/visibility
  - Update SharedItem interface usage throughout the service
  - Ensure existing item methods handle new fields
  - _Requirements: 4.5, 8.3_

- [x] 18. Update item creation/edit forms



  - [x] 18.1 Add loop visibility controls to item forms


    - Add loop selection checkboxes to item creation form
    - Add "all loops" and "future loops" checkboxes
    - Update form submission to include visibility settings
    - Add description field to item form
    - Update validation to handle new fields
    - _Requirements: 4.1, 4.2, 4.3, 4.4, 4.5_

- [x] 19. Configure routing for loop features


  - Add routes for /loops, /loops/create, /loops/:id, /loops/:id/invite
  - Add route for /loops/invitations
  - Add route for /loops/accept-invitation with query param support
  - Add route for /items/:id/visibility
  - Add route guards for authenticated users
  - Update navigation menu to include loops link
  - _Requirements: 1.1, 2.1, 3.1, 4.1, 5.1, 7.1_

- [x] 20. Add navigation and UI integration


  - Add "Loops" link to main navigation menu
  - Add loop selector/switcher in header or sidebar
  - Update item cards to show which loops they're visible in
  - Add visual indicators for loop membership
  - Ensure consistent styling across loop components
  - _Requirements: 7.1, 7.3, 7.4_
