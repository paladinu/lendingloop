# Implementation Plan

- [x] 1. Extend User model with Badge fields




  - Create `BadgeAward` class with badgeType and awardedAt properties in User.cs
  - Create `BadgeType` enum with values: Bronze, Silver, Gold in User.cs
  - Add `Badges` list property to User.cs model with default empty list
  - Add BSON attributes for MongoDB serialization
  - _Requirements: 7.1, 7.2, 7.3, 7.4, 7.5_

- [x] 2. Update LoopScoreService for badge functionality


- [x] 2.1 Update ILoopScoreService interface




  - Add GetUserBadgesAsync method signature
  - _Requirements: 7.1, 7.2, 7.3, 7.4_

- [x] 2.2 Update LoopScoreService class




  - Inject IEmailService dependency for badge notifications
  - Implement CheckAndAwardBadgesAsync private helper method to check score against milestones (10, 50, 100) and award new badges
  - Call CheckAndAwardBadgesAsync after each score update operation in AwardPointsAsync
  - Use MongoDB atomic operations ($addToSet) to prevent duplicate badge awards
  - Implement GetUserBadgesAsync to retrieve user's earned badges
  - Send email notification when a badge is awarded using IEmailService
  - _Requirements: 5.5, 7.1, 7.2, 7.3, 7.4, 7.5, 7.6_

- [x] 2.3 Update LoopScoreService unit tests




  - Test CheckAndAwardBadgesAsync awards Bronze badge when score reaches 10
  - Test CheckAndAwardBadgesAsync awards Silver badge when score reaches 50
  - Test CheckAndAwardBadgesAsync awards Gold badge when score reaches 100
  - Test CheckAndAwardBadgesAsync does not award duplicate badges when score exceeds milestone
  - Test GetUserBadgesAsync returns all earned badges
  - Test that email is sent when badge is awarded
  - _Requirements: 7.1, 7.2, 7.3, 7.4, 7.6_



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
  - Create BadgeType type union ('Bronze' | 'Silver' | 'Gold')
  - _Requirements: 7.4, 7.5_

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




  - Display badges with trophy icon (🏆)
  - Apply CSS classes based on badge type (bronze, silver, gold)
  - Add aria-label for accessibility with badge type and date
  - Show empty state when no badges earned
  - _Requirements: 7.4_

- [x] 6.3 Style the badge display




  - Create CSS for bronze, silver, and gold badge colors
  - Style badge container layout
  - Add hover effects to show badge details
  - _Requirements: 7.4_

- [x] 6.4 Write unit tests for BadgeDisplayComponent




  - Test component displays all earned badges
  - Test component applies correct CSS class for each badge type
  - Test component shows empty state when no badges
  - Test aria-label is set correctly with badge type and date
  - _Requirements: 7.4_



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
