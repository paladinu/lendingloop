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
