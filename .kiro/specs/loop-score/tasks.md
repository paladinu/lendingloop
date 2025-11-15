# Implementation Plan

- [x] 1. Extend User model with LoopScore fields


  - Add `LoopScore` integer property to User.cs model with default value of 0
  - Add `ScoreHistory` list property to User.cs model
  - Create `ScoreHistoryEntry` class with timestamp, points, actionType, itemRequestId, and itemName properties
  - Create `ScoreActionType` enum with values: BorrowCompleted, OnTimeReturn, LendApproved, LendCancelled
  - Add BSON attributes for MongoDB serialization
  - _Requirements: 1.4, 3.5_

- [x] 2. Implement LoopScoreService for score calculation

- [x] 2.1 Create ILoopScoreService interface


  - Define methods: AwardBorrowPointsAsync, AwardOnTimeReturnPointsAsync, AwardLendPointsAsync, ReverseLendPointsAsync
  - Define methods: GetUserScoreAsync, GetScoreHistoryAsync
  - _Requirements: 1.1, 1.2, 1.3, 3.1, 3.4_

- [x] 2.2 Implement LoopScoreService class


  - Inject IMongoDatabase and IConfiguration dependencies
  - Implement AwardBorrowPointsAsync to add 1 point and create history entry
  - Implement AwardOnTimeReturnPointsAsync to add 1 point for on-time returns
  - Implement AwardLendPointsAsync to add 4 points and create history entry
  - Implement ReverseLendPointsAsync to subtract 4 points (minimum 0) and create history entry
  - Use MongoDB atomic operations ($inc, $push, $max) to prevent race conditions
  - Implement GetUserScoreAsync to retrieve current score
  - Implement GetScoreHistoryAsync with pagination support
  - _Requirements: 1.1, 1.2, 1.3, 3.1, 3.4, 3.5, 5.1, 5.2, 5.3, 5.4, 5.5_

- [x] 2.3 Write unit tests for LoopScoreService


  - Test AwardBorrowPointsAsync increases score by 1 and creates history entry
  - Test AwardOnTimeReturnPointsAsync awards points only when on-time
  - Test AwardLendPointsAsync increases score by 4 and creates history entry
  - Test ReverseLendPointsAsync decreases score but not below zero
  - Test GetUserScoreAsync returns current score
  - Test GetScoreHistoryAsync returns entries in descending order
  - _Requirements: 1.1, 1.2, 1.3, 5.5_

- [x] 3. Integrate LoopScoreService into ItemRequestService

- [x] 3.1 Update ItemRequestService constructor


  - Inject ILoopScoreService dependency
  - _Requirements: 3.1_

- [x] 3.2 Update ApproveRequestAsync method


  - Call LoopScoreService.AwardLendPointsAsync for the owner after approval
  - Pass itemRequestId and item name to score service
  - _Requirements: 1.3, 5.1, 5.3_

- [x] 3.3 Update CompleteRequestAsync method


  - Call LoopScoreService.AwardBorrowPointsAsync for the requester
  - Check if return is on-time by comparing CompletedAt with ExpectedReturnDate
  - Call LoopScoreService.AwardOnTimeReturnPointsAsync if on-time
  - Pass itemRequestId and item name to score service
  - _Requirements: 1.1, 1.2, 3.1, 3.2, 4.4_

- [x] 3.4 Update CancelRequestAsync method (for approved requests)


  - Check if request status was Approved before cancellation
  - Call LoopScoreService.ReverseLendPointsAsync for the owner if it was approved
  - Pass itemRequestId and item name to score service
  - _Requirements: 5.4, 5.5_

- [x] 3.5 Update ItemRequestService unit tests


  - Test that ApproveRequestAsync calls AwardLendPointsAsync
  - Test that CompleteRequestAsync calls AwardBorrowPointsAsync
  - Test that CompleteRequestAsync calls AwardOnTimeReturnPointsAsync when on-time
  - Test that CompleteRequestAsync does not call AwardOnTimeReturnPointsAsync when late
  - Test that CancelRequestAsync calls ReverseLendPointsAsync for approved requests
  - _Requirements: 1.1, 1.2, 1.3, 5.4_

- [x] 4. Add UserController endpoints for score access

- [x] 4.1 Add GET /api/users/{userId}/score endpoint


  - Create action method that calls LoopScoreService.GetUserScoreAsync
  - Return score as JSON response
  - Handle user not found with 404 response
  - _Requirements: 1.4, 4.5_

- [x] 4.2 Add GET /api/users/{userId}/score-history endpoint

  - Create action method that calls LoopScoreService.GetScoreHistoryAsync
  - Accept optional limit query parameter for pagination
  - Return score history as JSON array
  - Handle user not found with 404 response
  - _Requirements: 3.5, 4.3, 6.1, 6.2, 6.3, 6.4, 6.5_

- [x] 4.3 Write unit tests for UserController score endpoints


  - Test GetUserScore returns score when user exists
  - Test GetUserScore returns 404 when user not found
  - Test GetScoreHistory returns history with pagination
  - Test GetScoreHistory returns 404 when user not found
  - _Requirements: 4.5, 6.1_

- [x] 5. Register LoopScoreService in dependency injection


  - Add ILoopScoreService and LoopScoreService to Program.cs service registration
  - Ensure service is registered as scoped or singleton as appropriate
  - _Requirements: 3.1_

- [x] 6. Update UserProfile interface in Angular


  - Add loopScore number property to UserProfile interface in auth.interface.ts
  - Create ScoreHistoryEntry interface with timestamp, points, actionType, itemRequestId, itemName
  - Create ScoreActionType type union
  - _Requirements: 1.4, 2.1, 6.1_

- [x] 7. Create LoopScoreService in Angular

- [x] 7.1 Generate LoopScoreService


  - Create service file: loop-score.service.ts
  - Inject HttpClient dependency
  - Define API_URL using environment.apiUrl
  - _Requirements: 2.1, 2.2_

- [x] 7.2 Implement score retrieval methods

  - Implement getUserScore(userId: string): Observable<number>
  - Implement getScoreHistory(userId: string, limit?: number): Observable<ScoreHistoryEntry[]>
  - Implement getScoreExplanation() to return static score rules object
  - _Requirements: 2.1, 2.2, 4.1, 4.2, 6.1_

- [x] 7.3 Write unit tests for LoopScoreService


  - Test getUserScore makes correct HTTP GET request
  - Test getScoreHistory makes correct HTTP GET request with limit parameter
  - Test getScoreExplanation returns score rules object
  - _Requirements: 2.1, 4.1_

- [x] 8. Create LoopScoreDisplayComponent

- [x] 8.1 Generate LoopScoreDisplayComponent


  - Create component files: loop-score-display.component.ts/html/css
  - Add @Input() score: number property with default 0
  - Add @Input() size: 'small' | 'medium' | 'large' property with default 'medium'
  - _Requirements: 2.1, 2.2_

- [x] 8.2 Implement component template


  - Create score badge HTML with star icon and score value
  - Add aria-label for accessibility
  - Apply CSS classes based on size input
  - _Requirements: 2.1, 2.2, 2.5_

- [x] 8.3 Style the score badge


  - Create CSS for small, medium, and large sizes
  - Style badge with gold/yellow gradient background
  - Add star icon styling
  - Ensure proper spacing and alignment
  - _Requirements: 2.1, 2.2_

- [x] 8.4 Write unit tests for LoopScoreDisplayComponent


  - Test component displays score value correctly
  - Test component applies correct size class
  - Test component shows 0 for new users
  - Test aria-label is set correctly
  - _Requirements: 2.1, 2.5_

- [x] 9. Create ScoreHistoryComponent for user profile

- [x] 9.1 Generate ScoreHistoryComponent


  - Create component files: score-history.component.ts/html/css
  - Inject LoopScoreService and AuthService
  - Add scoreHistory: ScoreHistoryEntry[] property
  - Add scoreRules property from getScoreExplanation()
  - _Requirements: 4.3, 6.1, 6.2, 6.3_

- [x] 9.2 Implement component logic

  - Load current user ID from AuthService
  - Call LoopScoreService.getScoreHistory() in ngOnInit
  - Load score rules from LoopScoreService.getScoreExplanation()
  - Handle empty history state
  - _Requirements: 6.1, 6.2, 6.3, 6.4, 6.5_

- [x] 9.3 Create component template


  - Display score rules explanation section
  - Display chronological list of score history entries
  - Show date, action type, points, and item name for each entry
  - Display most recent entries first
  - Show empty state message when no history exists
  - _Requirements: 4.1, 4.2, 4.3, 6.1, 6.2, 6.3, 6.4, 6.5_

- [x] 9.4 Style the score history view


  - Create CSS for history list layout
  - Style individual history entries
  - Add icons or colors for different action types
  - Format timestamps in readable format
  - _Requirements: 6.1, 6.2_

- [x] 9.5 Write unit tests for ScoreHistoryComponent


  - Test component loads score history on init
  - Test component displays history entries correctly
  - Test component shows empty state when no history
  - Test component formats timestamps correctly
  - _Requirements: 6.1, 6.5_

- [x] 10. Update existing components to display LoopScore


- [x] 10.1 Update ItemCardComponent


  - Import LoopScoreDisplayComponent
  - Add score display next to owner name in template
  - Pass owner's loopScore to display component
  - _Requirements: 2.1, 2.2, 2.4_

- [x] 10.2 Update ItemDetailComponent


  - Import LoopScoreDisplayComponent
  - Add score display next to owner name in template
  - Pass owner's loopScore to display component
  - _Requirements: 2.1, 2.2, 2.4_

- [x] 10.3 Update LoopMembersComponent


  - Import LoopScoreDisplayComponent
  - Add score display next to each member's name in template
  - Pass each member's loopScore to display component
  - _Requirements: 2.1, 2.2, 2.4_

- [x] 10.4 Update ItemRequestListComponent

  - Import LoopScoreDisplayComponent
  - Add score display next to requester and owner names
  - Pass loopScore values to display component
  - _Requirements: 2.1, 2.2, 2.4_

- [x] 10.5 Update ToolbarComponent


  - Import LoopScoreDisplayComponent
  - Add score display next to current user's name
  - Subscribe to AuthService.currentUser$ to get loopScore
  - _Requirements: 2.1, 2.2, 2.4_

- [x] 10.6 Update component unit tests

  - Update ItemCardComponent tests to verify score display
  - Update ItemDetailComponent tests to verify score display
  - Update LoopMembersComponent tests to verify score display
  - Update ItemRequestListComponent tests to verify score display
  - Update ToolbarComponent tests to verify score display
  - _Requirements: 2.1, 2.4_

- [x] 11. Add ScoreHistoryComponent to user profile page


  - Import ScoreHistoryComponent in user profile module
  - Add score-history component to user profile template
  - Position below user information section
  - Add section heading "My LoopScore History"
  - _Requirements: 4.3, 4.5, 6.1_

- [x] 12. Update AuthService to include loopScore in cached user data

  - Verify UserProfile interface includes loopScore
  - Ensure loopScore is stored and retrieved from localStorage
  - Update currentUserSubject to emit loopScore changes
  - _Requirements: 1.4, 2.3, 2.4_

- [x] 13. Run all backend tests and verify passing



  - Execute dotnet test from /Api.Tests directory
  - Verify all LoopScoreService tests pass
  - Verify all updated ItemRequestService tests pass
  - Verify all UserController tests pass
  - Fix any failing tests
  - _Requirements: All_

- [x] 14. Run all frontend tests and verify passing



  - Execute npm test from /ui directory
  - Verify all LoopScoreService tests pass
  - Verify all component tests pass
  - Fix any failing tests
  - _Requirements: All_
