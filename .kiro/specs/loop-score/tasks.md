# Implementation Plan

- [x] 1. Extend User model with Badge fields
  - Create `BadgeAward` class with badgeType and awardedAt properties in User.cs
  - Create `BadgeType` enum with values: Bronze, Silver, Gold, FirstLend, ReliableBorrower in User.cs
  - Add `Badges` list property to User.cs model with default empty list
  - Add BSON attributes for MongoDB serialization
  - _Requirements: 7.1, 7.2, 7.3, 7.4, 7.5, 8.1, 8.2, 8.3, 8.4, 8.5_

- [x] 2. Update LoopScoreService for badge functionality


- [x] 2.1 Update ILoopScoreService interface
  - Add GetUserBadgesAsync method signature
  - Add GetOnTimeReturnCountAsync method signature
  - _Requirements: 7.1, 7.2, 7.3, 7.4, 8.2_

- [x] 2.2 Update LoopScoreService class
  - Inject IEmailService dependency for badge notifications
  - Implement CheckAndAwardMilestoneBadgesAsync private helper method to check score against milestones (10, 50, 100) and award new badges
  - Implement CheckAndAwardAchievementBadgeAsync private helper method to award specific achievement badges
  - Call CheckAndAwardMilestoneBadgesAsync after each score update operation in AwardPointsAsync
  - In AwardLendPointsAsync, check and award FirstLend badge if this is user's first lending transaction
  - In AwardOnTimeReturnPointsAsync, check on-time return count and award ReliableBorrower badge when count reaches 10
  - Implement GetOnTimeReturnCountAsync to count ScoreHistory entries with actionType "OnTimeReturn"
  - Use MongoDB atomic operations ($addToSet) to prevent duplicate badge awards
  - Implement GetUserBadgesAsync to retrieve user's earned badges
  - Send email notification when a badge is awarded using IEmailService
  - _Requirements: 5.5, 7.1, 7.2, 7.3, 7.4, 7.5, 7.6, 8.1, 8.2, 8.3, 8.4, 8.5_

- [x] 2.3 Update LoopScoreService unit tests
  - Test CheckAndAwardMilestoneBadgesAsync awards Bronze badge when score reaches 10
  - Test CheckAndAwardMilestoneBadgesAsync awards Silver badge when score reaches 50
  - Test CheckAndAwardMilestoneBadgesAsync awards Gold badge when score reaches 100
  - Test CheckAndAwardMilestoneBadgesAsync does not award duplicate badges when score exceeds milestone
  - Test AwardLendPointsAsync awards FirstLend badge on first lending transaction
  - Test AwardLendPointsAsync does not award FirstLend badge on subsequent lends
  - Test AwardOnTimeReturnPointsAsync awards ReliableBorrower badge after 10 on-time returns
  - Test AwardOnTimeReturnPointsAsync does not award ReliableBorrower badge before threshold
  - Test GetOnTimeReturnCountAsync returns correct count
  - Test GetUserBadgesAsync returns all earned badges including achievements
  - Test that email is sent when badge is awarded
  - _Requirements: 7.1, 7.2, 7.3, 7.4, 7.6, 8.1, 8.2, 8.3, 8.4, 8.5_



- [x] 3. Add UserController badges endpoint


- [x] 3.1 Add GET /api/users/{userId}/badges endpoint



  - Create action method that calls LoopScoreService.GetUserBadgesAsync
  - Return badges as JSON array
  - Handle user not found with 404 response
  - _Requirements: 7.4_

- [x] 3.2 Update UserController unit tests




  - Test GetUserBadges returns badges when user exists
  - Test GetUserBadges returns 404 when user not found
  - _Requirements: 7.4_

- [x] 4. Update UserProfile interface in Angular
  - Add badges array property to UserProfile interface in auth.interface.ts
  - Create BadgeAward interface with badgeType and awardedAt properties
  - Create BadgeType type union ('Bronze' | 'Silver' | 'Gold' | 'FirstLend' | 'ReliableBorrower')
  - _Requirements: 7.4, 7.5, 8.3_

- [x] 5. Update LoopScoreService in Angular


- [x] 5.1 Add badge-related methods to LoopScoreService




  - Implement getUserBadges(userId: string): Observable<BadgeAward[]>
  - Implement getBadgeMilestones() to return badge milestone values (Bronze: 10, Silver: 50, Gold: 100)
  - _Requirements: 7.4_

- [x] 5.2 Update LoopScoreService unit tests




  - Test getUserBadges makes correct HTTP GET request
  - Test getBadgeMilestones returns badge milestone values
  - _Requirements: 7.4_



- [x] 6. Create BadgeDisplayComponent



- [x] 6.1 Generate BadgeDisplayComponent




  - Create component files: badge-display.component.ts/html/css
  - Add @Input() badges: BadgeAward[] property with default empty array
  - _Requirements: 7.4_

- [x] 6.2 Implement component template
  - Categorize badges into milestone badges and achievement badges
  - Display milestone badges section with trophy icon (🏆) for Bronze, Silver, Gold
  - Display achievement badges section with specific icons (🎁 for FirstLend, ⭐ for ReliableBorrower)
  - Apply CSS classes based on badge type
  - Add aria-label for accessibility with badge type and date
  - Show empty state when no badges earned
  - _Requirements: 7.4, 8.3_

- [x] 6.3 Style the badge display
  - Create CSS for bronze, silver, and gold milestone badge colors
  - Create CSS for achievement badge styling (first-lend, reliable-borrower)
  - Style badge container layout with separate sections for milestone and achievement badges
  - Add hover effects to show badge details
  - _Requirements: 7.4, 8.3_

- [x] 6.4 Write unit tests for BadgeDisplayComponent
  - Test component displays all earned badges
  - Test component categorizes badges into milestone and achievement sections
  - Test component applies correct CSS class for each badge type
  - Test component displays correct icons for achievement badges
  - Test component displays correct labels for achievement badges
  - Test component shows empty state when no badges
  - Test aria-label is set correctly with badge type and date
  - _Requirements: 7.4, 8.3_



- [x] 7. Add BadgeDisplayComponent to user profile page




  - Import BadgeDisplayComponent in profile component
  - Add badge-display component to profile template near user's score
  - Pass user's badges array to badge-display component
  - Add section heading "My Badges" above badge display
  - _Requirements: 7.4_

- [x] 8. Update AuthService to include badges in cached user data



  - Ensure badges are stored and retrieved from localStorage
  - Update currentUserSubject to emit badge changes
  - _Requirements: 7.5_

- [x] 9. Run all backend tests and verify passing





  - Execute dotnet test from /Api.Tests directory
  - Verify all LoopScoreService tests pass
  - Verify all UserController tests pass
  - Fix any failing tests
  - _Requirements: All_

- [x] 10. Run all frontend tests and verify passing
  - Execute npm test from /ui directory
  - Verify all LoopScoreService tests pass
  - Verify all BadgeDisplayComponent tests pass
  - Fix any failing tests
  - _Requirements: All_

- [x] 11. Add achievement badge types to backend User model


  - Update BadgeType enum in User.cs to include FirstLend and ReliableBorrower values
  - Verify BSON serialization works correctly for new badge types
  - _Requirements: 8.1, 8.2, 8.5_

- [x] 12. Implement achievement badge logic in LoopScoreService



- [x] 12.1 Add GetOnTimeReturnCountAsync method

  - Implement method to count ScoreHistory entries with actionType "OnTimeReturn"
  - Add method signature to ILoopScoreService interface
  - _Requirements: 8.2_


- [x] 12.2 Implement CheckAndAwardAchievementBadgeAsync helper method

  - Create private helper method that awards a specific achievement badge if not already earned
  - Use MongoDB atomic operations ($addToSet) to prevent duplicate awards
  - Send email notification when badge is awarded
  - _Requirements: 8.1, 8.2, 8.4, 8.5_


- [x] 12.3 Add FirstLend badge logic to AwardLendPointsAsync

  - Check if user already has FirstLend badge
  - If not, award FirstLend badge when AwardLendPointsAsync is called
  - Call CheckAndAwardAchievementBadgeAsync with BadgeType.FirstLend
  - _Requirements: 8.1, 8.4, 8.5_


- [x] 12.4 Add ReliableBorrower badge logic to AwardOnTimeReturnPointsAsync

  - Call GetOnTimeReturnCountAsync to get current on-time return count
  - If count reaches exactly 10, award ReliableBorrower badge
  - Call CheckAndAwardAchievementBadgeAsync with BadgeType.ReliableBorrower
  - _Requirements: 8.2, 8.4, 8.5_

- [x] 13. Add unit tests for achievement badge functionality


  - Test AwardLendPointsAsync awards FirstLend badge on first lending transaction
  - Test AwardLendPointsAsync does not award FirstLend badge on subsequent lends
  - Test AwardOnTimeReturnPointsAsync awards ReliableBorrower badge after 10 on-time returns
  - Test AwardOnTimeReturnPointsAsync does not award ReliableBorrower badge before threshold
  - Test GetOnTimeReturnCountAsync returns correct count
  - Test CheckAndAwardAchievementBadgeAsync prevents duplicate badge awards
  - Test email is sent when achievement badges are awarded
  - _Requirements: 8.1, 8.2, 8.4, 8.5_

- [x] 14. Update frontend BadgeType to include achievement badges


  - Update BadgeType type union in auth.interface.ts to include 'FirstLend' and 'ReliableBorrower'
  - Verify BadgeAward interface works with new badge types
  - _Requirements: 8.3_

- [x] 15. Update BadgeDisplayComponent for achievement badges



- [x] 15.1 Add badge categorization logic

  - Implement categorizeBadges() method to separate milestone and achievement badges
  - Create milestoneBadges and achievementBadges arrays
  - Call categorizeBadges() in ngOnInit()
  - _Requirements: 8.3_


- [x] 15.2 Add helper methods for achievement badge display

  - Implement getBadgeIcon(badgeType: string) to return appropriate emoji (🎁 for FirstLend, ⭐ for ReliableBorrower)
  - Implement getBadgeLabel(badgeType: string) to return human-readable labels
  - _Requirements: 8.3_


- [x] 15.3 Update component template for achievement badges

  - Add separate section for achievement badges
  - Display achievement badges with specific icons using getBadgeIcon()
  - Apply CSS classes for achievement badge types (first-lend, reliable-borrower)
  - Add section heading "Achievement Badges"
  - _Requirements: 8.3_

- [x] 15.4 Add CSS styling for achievement badges


  - Create CSS classes for .first-lend and .reliable-borrower
  - Style achievement badge section separately from milestone badges
  - Ensure visual distinction between milestone and achievement badges
  - _Requirements: 8.3_

- [x] 16. Add unit tests for achievement badge display


  - Test component categorizes badges into milestone and achievement sections correctly
  - Test getBadgeIcon() returns correct icons for FirstLend and ReliableBorrower
  - Test getBadgeLabel() returns correct labels for achievement badges
  - Test achievement badges display with correct CSS classes
  - Test achievement badge section only shows when achievement badges exist
  - _Requirements: 8.3_

- [x] 17. Run all backend tests and verify achievement badge tests pass


  - Execute dotnet test from /Api.Tests directory
  - Verify all new achievement badge tests pass
  - Verify existing tests still pass
  - Fix any failing tests
  - _Requirements: 8.1, 8.2, 8.4, 8.5_

- [x] 18. Run all frontend tests and verify achievement badge tests pass



  - Execute npm test from /ui directory
  - Verify all new BadgeDisplayComponent tests pass
  - Verify existing tests still pass
  - Fix any failing tests
  - _Requirements: 8.3_

- [x] 19. Add additional achievement badge types to backend User model




  - Update BadgeType enum in User.cs to include GenerousLender, PerfectRecord, and CommunityBuilder values
  - Verify BSON serialization works correctly for new badge types
  - _Requirements: 9.1, 9.4, 10.1, 10.4, 11.1, 11.4_


- [x] 20. Add tracking fields to User model for new achievement badges



  - Add consecutiveOnTimeReturns field to User.cs to track consecutive on-time returns for PerfectRecord badge
  - Add invitedBy field to User.cs to track who invited the user for CommunityBuilder badge
  - Add BSON attributes for MongoDB serialization
  - _Requirements: 10.2, 11.2_


- [x] 21. Extend ILoopScoreService interface for new achievement badges



  - Add GetCompletedLendingTransactionCountAsync method signature
  - Add GetActiveInvitedUsersCountAsync method signature
  - Add RecordCompletedLendingTransactionAsync method signature
  - _Requirements: 9.2, 11.2, 11.3_


- [x] 22. Implement GenerousLender badge logic in LoopScoreService




- [x] 22.1 Add RecordCompletedLendingTransactionAsync method


  - Implement method to track completed lending transactions (when ItemRequest reaches Completed status and user is owner)
  - Check if user has completed 50 lending transactions
  - Award GenerousLender badge if threshold reached and badge not already earned
  - Use MongoDB atomic operations ($addToSet) to prevent duplicate awards
  - Send email notification when badge is awarded
  - _Requirements: 9.1, 9.2, 9.4, 9.5_



- [x] 22.2 Add GetCompletedLendingTransactionCountAsync method



  - Implement method to count ItemRequests with status "Completed" where user is the owner
  - This count is separate from lending approvals

  - _Requirements: 9.2_


- [x] 23. Implement PerfectRecord badge logic in LoopScoreService



- [x] 23.1 Update AwardOnTimeReturnPointsAsync to track consecutive returns


  - Increment User.ConsecutiveOnTimeReturns field when on-time return is recorded
  - Check if consecutive count reaches 25 and award PerfectRecord badge if not already earned
  - Use MongoDB atomic operations to update consecutive count and award badge
  - Send email notification when badge is awarded
  - _Requirements: 10.1, 10.2, 10.4, 10.5_



- [x] 23.2 Update AwardOnTimeReturnPointsAsync to reset consecutive count on late returns


  - When a late return occurs (isOnTime parameter is false), reset User.ConsecutiveOnTimeReturns to zero
  - This ensures the consecutive streak is broken by late returns
  - _Requirements: 10.2_


- [x] 24. Implement CommunityBuilder badge logic



- [x] 24.1 Add GetActiveInvitedUsersCountAsync method to LoopScoreService


  - Implement method to count users where InvitedBy matches the given userId
  - Filter to only count users who have at least one ScoreHistory entry (indicating they completed a transaction)

  - _Requirements: 11.2, 11.3_


- [x] 24.2 Add CommunityBuilder badge check to CompleteRequestAsync in ItemRequestService


  - When a request is completed, check if the requester has an InvitedBy field set
  - If so, check if this is the requester's first completed transaction (first ScoreHistory entry)
  - If it's their first transaction, call GetActiveInvitedUsersCountAsync for the inviter
  - If inviter has 10 active invited users and doesn't have CommunityBuilder badge, award it
  - Send email notification to inviter when badge is awarded
  - _Requirements: 11.1, 11.3, 11.4, 11.5, 11.6_


- [x] 25. Update ItemRequestService to record completed lending transactions




  - In CompleteRequestAsync, after awarding points, call RecordCompletedLendingTransactionAsync for the owner
  - This tracks completed lending transactions separately from approvals for GenerousLender badge
  - _Requirements: 9.2_

- [x] 26. Add unit tests for GenerousLender badge functionality




  - Test RecordCompletedLendingTransactionAsync awards GenerousLender badge after 50 transactions
  - Test RecordCompletedLendingTransactionAsync does not award badge before threshold
  - Test GetCompletedLendingTransactionCountAsync returns correct count
  - Test email is sent when GenerousLender badge is awarded
  - Test duplicate badge awards are prevented
  - _Requirements: 9.1, 9.2, 9.4, 9.5_


- [x] 27. Add unit tests for PerfectRecord badge functionality




  - Test AwardOnTimeReturnPointsAsync increments consecutiveOnTimeReturns on each on-time return
  - Test AwardOnTimeReturnPointsAsync resets consecutiveOnTimeReturns to zero on late returns
  - Test AwardOnTimeReturnPointsAsync awards PerfectRecord badge after 25 consecutive on-time returns
  - Test AwardOnTimeReturnPointsAsync does not award badge before threshold
  - Test email is sent when PerfectRecord badge is awarded
  - Test duplicate badge awards are prevented
  - _Requirements: 10.1, 10.2, 10.4, 10.5_


- [x] 28. Add unit tests for CommunityBuilder badge functionality



  - Test GetActiveInvitedUsersCountAsync returns correct count of active invited users
  - Test GetActiveInvitedUsersCountAsync only counts users with at least one ScoreHistory entry
  - Test CompleteRequestAsync awards CommunityBuilder badge when inviter has 10 active invitees
  - Test CompleteRequestAsync only checks for badge on invitee's first completed transaction
  - Test email is sent to inviter when CommunityBuilder badge is awarded
  - Test duplicate badge awards are prevented
  - _Requirements: 11.1, 11.2, 11.3, 11.4, 11.5, 11.6_




- [x] 29. Update ItemRequestServiceTests for new LoopScoreService calls


  - Test CompleteRequestAsync calls RecordCompletedLendingTransactionAsync for owner
  - Test CompleteRequestAsync checks for CommunityBuilder badge when requester was invited
  - _Requirements: 9.2, 11.3_


- [x] 30. Update frontend BadgeType to include new achievement badges



  - Update BadgeType type union in auth.interface.ts to include 'GenerousLender', 'PerfectRecord', and 'CommunityBuilder'
  - Verify BadgeAward interface works with new badge types
  - _Requirements: 9.3, 10.3, 11.4_

- [x] 31. Update BadgeDisplayComponent for new achievement badges





- [x] 31.1 Update getBadgeIcon helper method


  - Add icon mappings for GenerousLender (🤝), PerfectRecord (💯), and CommunityBuilder (🌟)
  - _Requirements: 9.3, 10.3, 11.4_





- [x] 31.2 Update getBadgeLabel helper method

  - Add label mappings for GenerousLender ("Generous Lender"), PerfectRecord ("Perfect Record"), and CommunityBuilder ("Community Builder")


  - _Requirements: 9.3, 10.3, 11.4_



- [x] 31.3 Update component CSS

  - Add CSS classes for .generous-lender, .perfect-record, and .community-builder
  - Style new achievement badges consistently with existing achievement badges

  - _Requirements: 9.3, 10.3, 11.4_

- [x] 32. Add unit tests for new achievement badge display



  - Test getBadgeIcon() returns correct icons for GenerousLender, PerfectRecord, and CommunityBuilder
  - Test getBadgeLabel() returns correct labels for new achievement badges
  - Test new achievement badges display with correct CSS classes
  - Test component correctly categorizes new badges as achievement badges
  - _Requirements: 9.3, 10.3, 11.4_


- [x] 33. Run all backend tests and verify new achievement badge tests pass



  - Execute dotnet test from /Api.Tests directory
  - Verify all new GenerousLender, PerfectRecord, and CommunityBuilder tests pass
  - Verify existing tests still pass
  - Fix any failing tests
  - _Requirements: 9.1, 9.2, 9.4, 9.5, 10.1, 10.2, 10.4, 10.5, 11.1, 11.2, 11.3, 11.4, 11.5, 11.6_


- [x] 34. Run all frontend tests and verify new achievement badge tests pass




  - Execute npm test from /ui directory
  - Verify all new BadgeDisplayComponent tests for new badges pass
  - Verify existing tests still pass


  - Fix any failing tests
  - _Requirements: 9.3, 10.3, 11.4_

- [x] 35. Add badge metadata support to LoopScoreService




- [x] 35.1 Create BadgeMetadata interface in Angular

  - Create BadgeMetadata interface in auth.interface.ts with properties: badgeType, name, description, category, requirement, icon
  - Export interface for use in components and services
  - _Requirements: 12.1, 12.4_




- [x] 35.2 Implement getAllBadgeMetadata method in LoopScoreService

  - Add getAllBadgeMetadata() method that returns array of BadgeMetadata for all available badges
  - Include metadata for all milestone badges (Bronze, Silver, Gold) with point requirements
  - Include metadata for all achievement badges (FirstLend, ReliableBorrower, GenerousLender, PerfectRecord, CommunityBuilder) with descriptions
  - Specify category ('milestone' or 'achievement') for each badge
  - Provide clear requirement text explaining how to earn each badge
  - _Requirements: 12.1, 12.4_




- [x] 35.3 Add unit tests for getAllBadgeMetadata

  - Test getAllBadgeMetadata returns metadata for all 8 badge types
  - Test each badge has required properties (badgeType, name, description, category, requirement, icon)
  - Test milestone badges have category 'milestone'
  - Test achievement badges have category 'achievement'
  - _Requirements: 12.1, 12.4_



- [x] 36. Refactor BadgeDisplayComponent to show all badges


- [x] 36.1 Update BadgeDisplayComponent inputs and properties

  - Change @Input() badges to @Input() earnedBadges for clarity
  - Add @Input() showAllBadges: boolean = true to control display mode


  - Add allBadgeMetadata: BadgeMetadata[] property
  - Add displayBadges: DisplayBadge[] property to hold combined earned/unearned badge data
  - Create DisplayBadge interface with properties: metadata, earned, awardedAt
  - _Requirements: 12.1, 12.2, 12.3_



- [x] 36.2 Implement badge preparation logic

  - Inject LoopScoreService into BadgeDisplayComponent constructor
  - In ngOnInit, call loopScoreService.getAllBadgeMetadata() to get all badge metadata
  - Implement prepareDisplayBadges() method that maps badge metadata to DisplayBadge objects
  - For each badge metadata, check if user has earned it by matching badgeType in earnedBadges array
  - Set earned flag and awardedAt timestamp for earned badges
  - _Requirements: 12.1, 12.2, 12.3_


- [x] 36.3 Create FilterByCategoryPipe for template

  - Generate FilterByCategoryPipe to filter DisplayBadge array by category
  - Implement transform method that filters badges by 'milestone' or 'achievement' category
  - Register pipe in appropriate module
  - _Requirements: 12.1_


- [x] 36.4 Update component template to display all badges


  - Update template to iterate over displayBadges instead of earnedBadges
  - Use FilterByCategoryPipe to separate milestone and achievement badges
  - Display badge icon, name, and description for all badges
  - Show requirement text for unearned badges using *ngIf="!badge.earned"
  - Show earned date for earned badges using *ngIf="badge.earned"
  - Apply [class.earned] and [class.unearned] based on badge.earned flag
  - Update aria-label to include earned status and requirement/date
  - _Requirements: 12.1, 12.2, 12.3, 12.4_

- [x] 37. Add CSS styling for earned and unearned badge states



- [x] 37.1 Create base badge-item styles



  - Style .badge-item with flexbox layout, padding, border-radius
  - Add transition for smooth state changes
  - _Requirements: 12.2, 12.5_


- [x] 37.2 Style earned badges


  - Create .badge-item.earned class with full opacity, colored border (#FFD700)
  - Style earned badge icons at full size with no filters
  - Use bright background color (#fff) for earned badges
  - _Requirements: 12.2, 12.3, 12.5_


- [x] 37.3 Style unearned badges


  - Create .badge-item.unearned class with reduced opacity (0.6), grey border (#ddd)
  - Apply grayscale filter to unearned badge icons
  - Use muted background color (#f5f5f5) for unearned badges
  - _Requirements: 12.2, 12.5_


- [x] 37.4 Style badge text elements



  - Style .badge-name with bold font and appropriate color
  - Style .badge-description with smaller font size and grey color
  - Style .badge-requirement with italic font and muted color for unearned badges
  - Style .badge-earned-date with green color (#4CAF50) for earned badges
  - _Requirements: 12.3, 12.4_


- [x] 37.5 Create responsive badge grid layout



  - Style .badge-grid with CSS Grid layout
  - Use grid-template-columns with auto-fill and minmax for responsive design
  - Add appropriate gap between badge items
  - _Requirements: 12.1_

- [x] 38. Add unit tests for displaying all badges



- [x] 38.1 Test badge metadata retrieval



  - Test component calls loopScoreService.getAllBadgeMetadata() in ngOnInit
  - Test allBadgeMetadata is populated with badge data
  - _Requirements: 12.1_


- [x] 38.2 Test prepareDisplayBadges logic


  - Test prepareDisplayBadges creates DisplayBadge for each badge metadata
  - Test earned flag is true when badge is in earnedBadges array
  - Test earned flag is false when badge is not in earnedBadges array
  - Test awardedAt is set correctly for earned badges
  - Test awardedAt is undefined for unearned badges
  - _Requirements: 12.2, 12.3_


- [x] 38.3 Test template rendering of all badges


  - Test component displays all 8 badges (3 milestone + 5 achievement)
  - Test earned badges have .earned CSS class
  - Test unearned badges have .unearned CSS class
  - Test requirement text is shown for unearned badges
  - Test earned date is shown for earned badges
  - Test requirement text is not shown for earned badges
  - Test earned date is not shown for unearned badges
  - _Requirements: 12.1, 12.2, 12.3, 12.4, 12.5_


- [x] 38.4 Test FilterByCategoryPipe


  - Test pipe filters badges by 'milestone' category correctly
  - Test pipe filters badges by 'achievement' category correctly
  - Test pipe returns empty array when no badges match category
  - _Requirements: 12.1_


- [x] 38.5 Test accessibility features


  - Test aria-label includes badge name and earned status
  - Test aria-label includes requirement for unearned badges
  - Test aria-label includes earned date for earned badges
  - _Requirements: 12.1, 12.3_

- [x] 39. Update parent components to use refactored BadgeDisplayComponent




- [x] 39.1 Update user profile component



  - Change [badges] input to [earnedBadges] in profile template
  - Ensure earnedBadges is passed from user profile data
  - Verify showAllBadges defaults to true to display all badges
  - _Requirements: 12.1_


- [x] 39.2 Test profile component integration



  - Test profile component passes earnedBadges to badge-display component
  - Test badge-display component receives correct earned badges
  - _Requirements: 12.1_

- [x] 40. Run all frontend tests and verify Requirement 12 implementation




  - Execute npm test from /ui directory
  - Verify all new BadgeDisplayComponent tests for displaying all badges pass
  - Verify FilterByCategoryPipe tests pass
  - Verify LoopScoreService getAllBadgeMetadata tests pass
  - Verify existing tests still pass
  - Fix any failing tests
  - _Requirements: 12.1, 12.2, 12.3, 12.4, 12.5_

- [x] 41. Add BadgeProgress model to backend




  - Create BadgeProgress class in Models folder with CurrentCount, RequiredCount, and DisplayText properties
  - Add BSON attributes for MongoDB serialization
  - _Requirements: 13.1, 13.2_

- [x] 42. Extend ILoopScoreService interface for progress tracking




  - Add GetBadgeProgressAsync(string userId, BadgeType badgeType) method signature
  - Add GetAllBadgeProgressAsync(string userId) method signature returning Dictionary<BadgeType, BadgeProgress>
  - _Requirements: 13.1, 13.2, 13.8_

- [x] 43. Implement badge progress calculation in LoopScoreService




- [x] 43.1 Implement GetBadgeProgressAsync method



  - For ReliableBorrower: Count ScoreHistory entries with actionType "OnTimeReturn", required count 10
  - For GenerousLender: Call GetCompletedLendingTransactionCountAsync, required count 50
  - For PerfectRecord: Return User.ConsecutiveOnTimeReturns, required count 25
  - For CommunityBuilder: Call GetActiveInvitedUsersCountAsync, required count 10
  - For FirstLend: Return 0/1 (binary badge)
  - Generate displayText in format "X/Y [badge description]"
  - _Requirements: 13.1, 13.2, 13.3, 13.4, 13.5, 13.6_

- [x] 43.2 Implement GetAllBadgeProgressAsync method


  - Call GetBadgeProgressAsync for each achievement badge type
  - Return dictionary mapping BadgeType to BadgeProgress
  - Optimize with batch database queries where possible
  - _Requirements: 13.1, 13.2, 13.8_

- [x] 44. Add UserController endpoint for badge progress




  - Create GET /api/users/{userId}/badge-progress endpoint
  - Call LoopScoreService.GetAllBadgeProgressAsync
  - Return progress as JSON dictionary
  - Handle user not found with 404 response
  - _Requirements: 13.1, 13.8_

- [x] 45. Add unit tests for badge progress backend




  - Test GetBadgeProgressAsync returns correct progress for ReliableBorrower badge
  - Test GetBadgeProgressAsync returns correct progress for GenerousLender badge
  - Test GetBadgeProgressAsync returns correct progress for PerfectRecord badge
  - Test GetBadgeProgressAsync returns correct progress for CommunityBuilder badge
  - Test GetBadgeProgressAsync returns 0 when user has no relevant actions
  - Test GetAllBadgeProgressAsync returns progress for all achievement badges
  - Test UserController endpoint returns badge progress when user exists
  - Test UserController endpoint returns 404 when user not found
  - _Requirements: 13.1, 13.2, 13.3, 13.4, 13.5, 13.6, 13.8_

- [x] 46. Add BadgeProgress interface to Angular




  - Create BadgeProgress interface in auth.interface.ts with currentCount, requiredCount, displayText properties
  - Update BadgeMetadata interface to include hasProgress boolean property
  - _Requirements: 13.1, 13.2_

- [x] 47. Update LoopScoreService in Angular for progress tracking





- [x] 47.1 Add getBadgeProgress method


  - Implement getBadgeProgress(userId: string): Observable<Map<BadgeType, BadgeProgress>>
  - Make HTTP GET request to /api/users/{userId}/badge-progress
  - Convert response object to Map for easier lookup
  - _Requirements: 13.1, 13.8_

- [x] 47.2 Update getAllBadgeMetadata to include hasProgress flag


  - Set hasProgress: true for ReliableBorrower, GenerousLender, PerfectRecord, CommunityBuilder
  - Set hasProgress: false for FirstLend and all milestone badges
  - _Requirements: 13.1_

- [x] 47.3 Add unit tests for progress tracking methods



  - Test getBadgeProgress makes correct HTTP GET request
  - Test getBadgeProgress converts response to Map correctly
  - Test getAllBadgeMetadata includes hasProgress flag for each badge
  - _Requirements: 13.1, 13.8_

- [x] 48. Update BadgeDisplayComponent for progress tracking







- [x] 48.1 Add progress tracking inputs and properties



  - Add @Input() userId: string property (required to fetch progress)
  - Add @Input() showProgress: boolean = true property to control progress display
  - Add badgeProgress: Map<BadgeType, BadgeProgress> property
  - Update DisplayBadge interface to include progress?: BadgeProgress property
  - _Requirements: 13.1, 13.8_

- [x] 48.2 Implement progress loading in ngOnInit



  - Check if showProgress is true and userId is provided
  - Call loopScoreService.getBadgeProgress(userId) and subscribe to result
  - Store progress in badgeProgress Map
  - Call prepareDisplayBadges after progress is loaded
  - _Requirements: 13.1, 13.8_

- [x] 48.3 Update prepareDisplayBadges to include progress


  - For each badge, lookup progress from badgeProgress Map
  - Add progress to DisplayBadge object if available
  - _Requirements: 13.1, 13.2_


- [x] 48.4 Add getProgressText helper method

  - Return empty string if badge is earned or has no progress
  - Return progress.displayText for unearned badges with progress
  - _Requirements: 13.2, 13.7_

- [x] 49. Update BadgeDisplayComponent template for progress






  - Add badge-progress span element for unearned badges with progress
  - Show progress text using getProgressText() method
  - Only display progress when showProgress is true and badge has progress data
  - Show requirement text for unearned badges without progress
  - Ensure progress display is mutually exclusive with requirement text
  - _Requirements: 13.1, 13.2, 13.7_

- [x] 50. Add CSS styling for progress display




  - Create .badge-progress class with blue color (#2196F3), light blue background (#E3F2FD)
  - Style with padding, border-radius, and centered text
  - Add appropriate font size and weight for readability
  - Ensure progress text is visually distinct from requirement text
  - _Requirements: 13.1, 13.2_

- [x] 51. Update parent components to pass userId to BadgeDisplayComponent




  - Update user profile component to pass [userId] input to badge-display
  - Ensure userId is available from user profile data or auth service
  - Verify showProgress defaults to true
  - _Requirements: 13.1, 13.8_

- [ ] 52. Add unit tests for progress display in BadgeDisplayComponent


  - Test component fetches badge progress when userId is provided and showProgress is true
  - Test component does not fetch progress when userId is missing
  - Test component does not fetch progress when showProgress is false
  - Test prepareDisplayBadges includes progress data in DisplayBadge objects
  - Test getProgressText returns correct text for unearned badges with progress
  - Test getProgressText returns empty string for earned badges
  - Test getProgressText returns empty string for badges without progress
  - Test template displays progress for unearned badges with progress data
  - Test template does not display progress for earned badges
  - Test template displays requirement text for badges without progress
  - Test component handles API errors gracefully
  - _Requirements: 13.1, 13.2, 13.7, 13.8_

- [x] 53. Run all backend tests and verify progress tracking tests pass




  - Execute dotnet test from /Api.Tests directory
  - Verify all new badge progress tests pass
  - Verify existing tests still pass
  - Fix any failing tests
  - _Requirements: 13.1, 13.2, 13.3, 13.4, 13.5, 13.6, 13.8_

- [x] 54. Run all frontend tests and verify progress tracking tests pass





  - Execute npm test from /ui directory
  - Verify all new LoopScoreService progress tests pass
  - Verify all new BadgeDisplayComponent progress tests pass
  - Verify existing tests still pass
  - Fix any failing tests
  - _Requirements: 13.1, 13.2, 13.7, 13.8_
