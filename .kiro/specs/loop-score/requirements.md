# Requirements Document

## Introduction

The LoopScore feature introduces a gamification system to encourage active participation in the LendingLoop platform. Users earn points for borrowing items, returning them on time, and lending items to others. The score is displayed next to the user's name throughout the application to recognize and incentivize community engagement.

## Glossary

- **LoopScore System**: The point-based gamification system that tracks and displays user participation scores
- **User**: A registered member of the LendingLoop platform who can borrow and lend items
- **Item Request**: A request from one user to borrow an item from another user
- **On-Time Return**: Completion of an item request where the borrower returns the item by the agreed-upon date
- **Late Return**: Completion of an item request where the borrower returns the item after the agreed-upon date
- **Consecutive On-Time Returns**: A sequence of on-time returns without any late returns in between
- **Lending Transaction**: An approved Item Request that reaches "Completed" status where the owner lent their item
- **Loan Event**: When an item owner approves a borrow request and lends their item to another user
- **Invited User**: A User who joined the platform through a loop invitation sent by another User
- **Active Invited User**: An invited User who has completed at least one Item Request as either borrower or lender
- **User Profile**: The data model containing user information including their LoopScore
- **UI Component**: Any visual element in the Angular frontend that displays user information
- **Badge**: A visual achievement award displayed on a User's profile to recognize milestones or accomplishments
- **Milestone Badge**: A badge awarded when a User reaches specific LoopScore point thresholds (Bronze, Silver, Gold)
- **Achievement Badge**: A badge awarded when a User completes specific accomplishments (First Lend, Reliable Borrower, Generous Lender, Perfect Record, Community Builder)

## Requirements

### Requirement 1

**User Story:** As a platform user, I want to earn points for participating in the sharing economy, so that I feel recognized for my contributions to the community

#### Acceptance Criteria

1. WHEN a User completes an Item Request as a borrower, THE LoopScore System SHALL increment the User's score by one point
2. WHEN a User returns an item on time, THE LoopScore System SHALL increment the User's score by one point
3. WHEN a User approves an Item Request as an item owner, THE LoopScore System SHALL increment the User's score by four points
4. THE LoopScore System SHALL store the score value as an integer in the User Profile
5. THE LoopScore System SHALL initialize new User accounts with a score of zero

### Requirement 2

**User Story:** As a platform user, I want to see LoopScores displayed next to user names, so that I can recognize active and trusted community members

#### Acceptance Criteria

1. WHEN a UI Component displays a User's name, THE LoopScore System SHALL display the User's score adjacent to the name
2. THE LoopScore System SHALL format the score display consistently across all UI Components
3. WHEN a User's score changes, THE LoopScore System SHALL update the displayed score in real-time without requiring a page refresh
4. THE LoopScore System SHALL display the score for all users including the current logged-in user
5. WHERE a User has a score of zero, THE LoopScore System SHALL display the zero value rather than hiding the score

### Requirement 3

**User Story:** As a system administrator, I want the LoopScore to be calculated automatically based on user actions, so that the scoring system requires no manual intervention

#### Acceptance Criteria

1. WHEN an Item Request status changes to "Completed", THE LoopScore System SHALL automatically calculate and award points to both borrower and lender
2. THE LoopScore System SHALL determine on-time return status by comparing the completion date to the agreed return date
3. IF an Item Request is cancelled or rejected, THEN THE LoopScore System SHALL NOT award any points to either party
4. THE LoopScore System SHALL persist score changes to the database immediately after calculation
5. THE LoopScore System SHALL maintain an audit trail of score changes for each User

### Requirement 4

**User Story:** As a borrower, I want to understand how to earn points, so that I can maximize my participation score

#### Acceptance Criteria

1. THE LoopScore System SHALL provide a scoring rules explanation accessible from the user interface
2. THE LoopScore System SHALL display point values for each action type (borrow: 1, return on-time: 1, lend: 4)
3. WHERE a User views their own profile, THE LoopScore System SHALL show a breakdown of how their score was earned
4. THE LoopScore System SHALL indicate whether a return was on-time or late in the request history
5. THE LoopScore System SHALL show the total score prominently on the user's profile page

### Requirement 5

**User Story:** As an item owner, I want to earn more points for lending items, so that I am incentivized to share my belongings with the community

#### Acceptance Criteria

1. WHEN a User approves an Item Request, THE LoopScore System SHALL award four points to the lending User
2. THE LoopScore System SHALL award lending points only once per Item Request regardless of loan duration
3. THE LoopScore System SHALL award lending points at the time of approval, not at completion
4. IF a User cancels an approved request before the item is borrowed, THEN THE LoopScore System SHALL deduct the four lending points
5. THE LoopScore System SHALL prevent negative scores by setting a minimum score of zero

### Requirement 6

**User Story:** As a platform user, I want to view the history of points I have received, so that I can track my participation over time

#### Acceptance Criteria

1. WHERE a User views their own profile, THE LoopScore System SHALL display a chronological list of all score changes
2. THE LoopScore System SHALL include the date, action type, and points awarded for each score change entry
3. THE LoopScore System SHALL display the most recent score changes first in the history list
4. THE LoopScore System SHALL include the associated Item Request details for each score change entry
5. WHERE a User has no score history, THE LoopScore System SHALL display a message indicating no activity has been recorded

### Requirement 7

**User Story:** As a platform user, I want to earn badges for reaching score milestones, so that I feel recognized for my long-term participation in the community

#### Acceptance Criteria

1. WHEN a User's LoopScore reaches 10 points, THE LoopScore System SHALL award a Bronze badge to the User
2. WHEN a User's LoopScore reaches 50 points, THE LoopScore System SHALL award a Silver badge to the User
3. WHEN a User's LoopScore reaches 100 points, THE LoopScore System SHALL award a Gold badge to the User
4. THE LoopScore System SHALL display earned badges on the User's profile page
5. THE LoopScore System SHALL store badge awards with the timestamp when each badge was earned
6. THE LoopScore System SHALL email the user letting them know about their achievement.

### Requirement 8

**User Story:** As a platform user, I want to earn special achievement badges for specific accomplishments, so that I can showcase my unique contributions to the community

#### Acceptance Criteria

1. WHEN a User completes their first lending transaction, THE LoopScore System SHALL award a "First Lend" achievement badge to the User
2. WHEN a User completes ten on-time returns, THE LoopScore System SHALL award a "Reliable Borrower" achievement badge to the User
3. THE LoopScore System SHALL display achievement badges alongside milestone badges on the User's profile page
4. THE LoopScore System SHALL store achievement badge awards with the timestamp when each badge was earned
5. THE LoopScore System SHALL prevent duplicate achievement badge awards for the same accomplishment

### Requirement 9

**User Story:** As an active lender, I want to earn recognition for consistently sharing my items with the community, so that I feel valued for my generosity

#### Acceptance Criteria

1. WHEN a User completes fifty lending transactions, THE LoopScore System SHALL award a "Generous Lender" achievement badge to the User
2. THE LoopScore System SHALL count lending transactions by tracking approved Item Requests that reach "Completed" status
3. THE LoopScore System SHALL display the "Generous Lender" badge on the User's profile page alongside other achievement badges
4. THE LoopScore System SHALL store the "Generous Lender" badge award with the timestamp when earned
5. THE LoopScore System SHALL send an email notification to the User when the "Generous Lender" badge is awarded

### Requirement 10

**User Story:** As a reliable borrower, I want to earn recognition for my consistent on-time returns, so that other users can trust me with their valuable items

#### Acceptance Criteria

1. WHEN a User completes twenty-five consecutive on-time returns without any late returns, THE LoopScore System SHALL award a "Perfect Record" achievement badge to the User
2. THE LoopScore System SHALL reset the consecutive on-time return count to zero when a User completes a late return
3. THE LoopScore System SHALL display the "Perfect Record" badge on the User's profile page alongside other achievement badges
4. THE LoopScore System SHALL store the "Perfect Record" badge award with the timestamp when earned
5. THE LoopScore System SHALL send an email notification to the User when the "Perfect Record" badge is awarded

### Requirement 11

**User Story:** As a community builder, I want to earn recognition for growing the LendingLoop platform, so that I feel rewarded for bringing new active members to the community

#### Acceptance Criteria

1. WHEN ten Users invited by a User each complete at least one lending or borrowing transaction, THE LoopScore System SHALL award a "Community Builder" achievement badge to the inviting User
2. THE LoopScore System SHALL track which User sent each invitation through the loop invitation system
3. THE LoopScore System SHALL count an invited User as active when they complete their first Item Request as either borrower or lender
4. THE LoopScore System SHALL display the "Community Builder" badge on the User's profile page alongside other achievement badges
5. THE LoopScore System SHALL store the "Community Builder" badge award with the timestamp when earned
6. THE LoopScore System SHALL send an email notification to the User when the "Community Builder" badge is awarded

### Requirement 12

**User Story:** As a platform user, I want to see all available achievement badges including those I haven't earned yet, so that I understand what goals I can work toward

#### Acceptance Criteria

1. THE LoopScore System SHALL display all available achievement badges on the User's profile page
2. WHERE a User has not earned a specific badge, THE LoopScore System SHALL display the badge in a greyed-out or disabled visual state
3. THE LoopScore System SHALL display earned badges in full color with the awarded timestamp
4. THE LoopScore System SHALL show descriptive text for each badge explaining how to earn it
5. THE LoopScore System SHALL visually distinguish between earned and unearned badges through styling

### Requirement 13

**User Story:** As a platform user, I want to see my progress toward earning unearned achievement badges, so that I can track how close I am to achieving each goal

#### Acceptance Criteria

1. WHERE a User has not earned an achievement badge, THE LoopScore System SHALL display the User's current progress toward that badge's requirement
2. THE LoopScore System SHALL format progress as a fraction showing current count versus required count (e.g., "7/10 on-time returns")
3. THE LoopScore System SHALL calculate progress for Reliable Borrower badge by counting completed on-time returns
4. THE LoopScore System SHALL calculate progress for Generous Lender badge by counting completed lending transactions
5. THE LoopScore System SHALL calculate progress for Perfect Record badge by showing current consecutive on-time returns count
6. THE LoopScore System SHALL calculate progress for Community Builder badge by counting active invited users
7. WHERE a badge has been earned, THE LoopScore System SHALL NOT display progress information
8. THE LoopScore System SHALL update progress information in real-time as the User completes relevant actions