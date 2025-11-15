# LoopScore Feature Design

## Overview

The LoopScore feature implements a gamification system that awards points to users based on their participation in the LendingLoop platform. The system automatically calculates and updates scores when users complete borrowing and lending activities, and displays scores throughout the UI to recognize active community members.

### Key Design Principles

- **Automatic Calculation**: Scores are calculated automatically based on ItemRequest status changes, requiring no manual intervention
- **Real-time Updates**: Score changes are reflected immediately in the UI without page refreshes
- **Audit Trail**: All score changes are tracked with timestamps and reasons for transparency
- **Non-negative Scores**: Scores cannot go below zero to maintain positive reinforcement
- **Consistent Display**: Scores appear uniformly next to user names across all UI components
- **Milestone Recognition**: Badges are automatically awarded when users reach score milestones to celebrate achievements

## Architecture

### Backend Architecture (.NET 8 Web API)

The backend implements the scoring logic as part of the existing ItemRequest workflow:

```
ItemRequestService (existing)
    ↓ (triggers on status change)
LoopScoreService (new)
    ↓ (calculates and updates)
User Model (extended with score fields)
    ↓ (persists to)
MongoDB Users Collection
```

### Frontend Architecture (Angular)

The frontend displays scores using a reusable component and service:

```
LoopScoreService (new)
    ↓ (provides score data)
LoopScoreDisplayComponent (new)
    ↓ (renders score badge)
All User Display Components (updated)
    ↓ (embed score display)
User Interface
```

### Data Flow

1. **Score Calculation Flow**:
   - User completes an action (approve request, complete request, etc.)
   - ItemRequestService updates ItemRequest status
   - ItemRequestService calls LoopScoreService to calculate points
   - LoopScoreService determines point values based on action type
   - LoopScoreService updates User model and creates ScoreHistory entry
   - Changes are persisted to MongoDB

2. **Score Display Flow**:
   - Component requests user data (with score)
   - LoopScoreService provides score information
   - LoopScoreDisplayComponent renders score badge
   - Real-time updates via Angular change detection

## Components and Interfaces

### Backend Components

#### 1. Extended User Model

```csharp
public class User
{
    // Existing fields...
    
    [BsonElement("loopScore")]
    public int LoopScore { get; set; } = 0;
    
    [BsonElement("scoreHistory")]
    public List<ScoreHistoryEntry> ScoreHistory { get; set; } = new();
    
    [BsonElement("badges")]
    public List<BadgeAward> Badges { get; set; } = new();
    
    [BsonElement("consecutiveOnTimeReturns")]
    public int ConsecutiveOnTimeReturns { get; set; } = 0;
    
    [BsonElement("invitedBy")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? InvitedBy { get; set; }
}

public class BadgeAward
{
    [BsonElement("badgeType")]
    [BsonRepresentation(BsonType.String)]
    public BadgeType BadgeType { get; set; }
    
    [BsonElement("awardedAt")]
    public DateTime AwardedAt { get; set; }
}

public enum BadgeType
{
    // Milestone badges
    Bronze,   // 10 points
    Silver,   // 50 points
    Gold,     // 100 points
    
    // Achievement badges
    FirstLend,           // First lending transaction
    ReliableBorrower,    // 10 on-time returns
    GenerousLender,      // 50 completed lending transactions
    PerfectRecord,       // 25 consecutive on-time returns
    CommunityBuilder     // 10 active invited users
}

public class ScoreHistoryEntry
{
    [BsonElement("timestamp")]
    public DateTime Timestamp { get; set; }
    
    [BsonElement("points")]
    public int Points { get; set; }
    
    [BsonElement("actionType")]
    [BsonRepresentation(BsonType.String)]
    public ScoreActionType ActionType { get; set; }
    
    [BsonElement("itemRequestId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string ItemRequestId { get; set; } = string.Empty;
    
    [BsonElement("itemName")]
    public string ItemName { get; set; } = string.Empty;
}

public enum ScoreActionType
{
    BorrowCompleted,      // +1 point
    OnTimeReturn,         // +1 point
    LendApproved,         // +4 points
    LendCancelled         // -4 points (reversal)
}
```

#### 2. LoopScoreService Interface

```csharp
public interface ILoopScoreService
{
    Task AwardBorrowPointsAsync(string userId, string itemRequestId, string itemName);
    Task AwardOnTimeReturnPointsAsync(string userId, string itemRequestId, string itemName, bool isOnTime);
    Task AwardLendPointsAsync(string userId, string itemRequestId, string itemName);
    Task ReverseLendPointsAsync(string userId, string itemRequestId, string itemName);
    Task RecordCompletedLendingTransactionAsync(string userId, string itemRequestId);
    Task<int> GetUserScoreAsync(string userId);
    Task<List<ScoreHistoryEntry>> GetScoreHistoryAsync(string userId, int limit = 50);
    Task<List<BadgeAward>> GetUserBadgesAsync(string userId);
    Task<int> GetOnTimeReturnCountAsync(string userId);
    Task<int> GetCompletedLendingTransactionCountAsync(string userId);
    Task<int> GetActiveInvitedUsersCountAsync(string userId);
}
```

#### 3. LoopScoreService Implementation

The service will:
- Calculate points based on action type
- Update User.LoopScore field
- Add entries to User.ScoreHistory
- Check for milestone badges after each score update (Bronze, Silver, Gold)
- Check for achievement badges after relevant actions:
  - **FirstLend**: Awarded on first lending transaction (when AwardLendPointsAsync is called for the first time)
  - **ReliableBorrower**: Awarded when user completes 10 on-time returns
  - **GenerousLender**: Awarded when user completes 50 lending transactions (tracked via RecordCompletedLendingTransactionAsync)
  - **PerfectRecord**: Awarded when user completes 25 consecutive on-time returns
  - **CommunityBuilder**: Awarded when 10 users invited by the user become active (complete at least one transaction)
- Track consecutive on-time returns:
  - Increment User.ConsecutiveOnTimeReturns on each on-time return
  - Reset User.ConsecutiveOnTimeReturns to zero on late returns
  - Check for PerfectRecord badge when consecutive count reaches 25
- Track completed lending transactions separately from lending approvals
- Award badges automatically when thresholds are reached (10, 50, 100 points for milestones)
- Prevent duplicate badge awards
- Enforce minimum score of zero
- Use atomic MongoDB operations to prevent race conditions
- Send email notifications when badges are awarded

#### 4. Updated ItemRequestService

Integrate LoopScoreService calls at key points:
- **ApproveRequestAsync**: Award 4 points to lender
- **CompleteRequestAsync**: 
  - Award 1 point to borrower
  - Check for on-time return and award 1 additional point if applicable (pass isOnTime flag to AwardOnTimeReturnPointsAsync)
  - Record completed lending transaction for the owner (call RecordCompletedLendingTransactionAsync)
  - Check if borrower was invited by someone and if this is their first completed transaction, potentially triggering CommunityBuilder badge for inviter
- **CancelRequestAsync** (for approved requests): Reverse 4 points from lender

#### 5. UserController Extensions

Add new endpoints:
- `GET /api/users/{userId}/score` - Get current score
- `GET /api/users/{userId}/score-history` - Get score history with pagination
- `GET /api/users/{userId}/badges` - Get user's earned badges

### Frontend Components

#### 1. Extended UserProfile Interface

```typescript
export interface UserProfile {
    // Existing fields...
    loopScore: number;
    badges: BadgeAward[];
}

export interface ScoreHistoryEntry {
    timestamp: string;
    points: number;
    actionType: 'BorrowCompleted' | 'OnTimeReturn' | 'LendApproved' | 'LendCancelled';
    itemRequestId: string;
    itemName: string;
}

export interface BadgeAward {
    badgeType: 'Bronze' | 'Silver' | 'Gold' | 'FirstLend' | 'ReliableBorrower' | 'GenerousLender' | 'PerfectRecord' | 'CommunityBuilder';
    awardedAt: string;
}
```

#### 2. LoopScoreService

```typescript
@Injectable({ providedIn: 'root' })
export class LoopScoreService {
    getUserScore(userId: string): Observable<number>;
    getScoreHistory(userId: string, limit?: number): Observable<ScoreHistoryEntry[]>;
    getUserBadges(userId: string): Observable<BadgeAward[]>;
    getScoreExplanation(): ScoreRules;
    getBadgeMilestones(): BadgeMilestones;
}

export interface ScoreRules {
    borrowCompleted: number;
    onTimeReturn: number;
    lendApproved: number;
}

export interface BadgeMilestones {
    bronze: number;
    silver: number;
    gold: number;
}
```

#### 3. LoopScoreDisplayComponent

A reusable component that displays the score badge:

```typescript
@Component({
    selector: 'app-loop-score-display',
    template: `
        <span class="loop-score-badge" [attr.aria-label]="'LoopScore: ' + score">
            <span class="score-icon">⭐</span>
            <span class="score-value">{{ score }}</span>
        </span>
    `
})
export class LoopScoreDisplayComponent {
    @Input() score: number = 0;
    @Input() size: 'small' | 'medium' | 'large' = 'medium';
}
```

#### 4. ScoreHistoryComponent

Displays the user's score history on their profile:

```typescript
@Component({
    selector: 'app-score-history',
    templateUrl: './score-history.component.html'
})
export class ScoreHistoryComponent implements OnInit {
    scoreHistory: ScoreHistoryEntry[] = [];
    scoreRules: ScoreRules;
    
    ngOnInit(): void {
        // Load score history and rules
    }
}
```

#### 5. BadgeDisplayComponent

A reusable component that displays earned badges:

```typescript
@Component({
    selector: 'app-badge-display',
    template: `
        <div class="badges-container">
            <div class="milestone-badges" *ngIf="milestoneBadges.length > 0">
                <h4>Milestone Badges</h4>
                <span *ngFor="let badge of milestoneBadges" 
                      class="badge-icon" 
                      [class.bronze]="badge.badgeType === 'Bronze'"
                      [class.silver]="badge.badgeType === 'Silver'"
                      [class.gold]="badge.badgeType === 'Gold'"
                      [attr.aria-label]="badge.badgeType + ' badge earned on ' + (badge.awardedAt | date)">
                    🏆
                </span>
            </div>
            <div class="achievement-badges" *ngIf="achievementBadges.length > 0">
                <h4>Achievement Badges</h4>
                <span *ngFor="let badge of achievementBadges" 
                      class="badge-icon achievement"
                      [class.first-lend]="badge.badgeType === 'FirstLend'"
                      [class.reliable-borrower]="badge.badgeType === 'ReliableBorrower'"
                      [class.generous-lender]="badge.badgeType === 'GenerousLender'"
                      [class.perfect-record]="badge.badgeType === 'PerfectRecord'"
                      [class.community-builder]="badge.badgeType === 'CommunityBuilder'"
                      [attr.aria-label]="getBadgeLabel(badge.badgeType) + ' earned on ' + (badge.awardedAt | date)">
                    {{ getBadgeIcon(badge.badgeType) }}
                </span>
            </div>
        </div>
    `
})
export class BadgeDisplayComponent implements OnInit {
    @Input() badges: BadgeAward[] = [];
    
    milestoneBadges: BadgeAward[] = [];
    achievementBadges: BadgeAward[] = [];
    
    ngOnInit(): void {
        this.categorizeBadges();
    }
    
    categorizeBadges(): void {
        this.milestoneBadges = this.badges.filter(b => 
            ['Bronze', 'Silver', 'Gold'].includes(b.badgeType)
        );
        this.achievementBadges = this.badges.filter(b => 
            ['FirstLend', 'ReliableBorrower', 'GenerousLender', 'PerfectRecord', 'CommunityBuilder'].includes(b.badgeType)
        );
    }
    
    getBadgeIcon(badgeType: string): string {
        const icons: Record<string, string> = {
            'FirstLend': '🎁',
            'ReliableBorrower': '⭐',
            'GenerousLender': '🤝',
            'PerfectRecord': '💯',
            'CommunityBuilder': '🌟'
        };
        return icons[badgeType] || '🏅';
    }
    
    getBadgeLabel(badgeType: string): string {
        const labels: Record<string, string> = {
            'FirstLend': 'First Lend',
            'ReliableBorrower': 'Reliable Borrower',
            'GenerousLender': 'Generous Lender',
            'PerfectRecord': 'Perfect Record',
            'CommunityBuilder': 'Community Builder'
        };
        return labels[badgeType] || badgeType;
    }
}
```

#### 6. Updated Components

The following existing components will be updated to display scores:
- **ItemCardComponent**: Show owner's score
- **ItemDetailComponent**: Show owner's score
- **LoopMembersComponent**: Show each member's score
- **ItemRequestListComponent**: Show requester's and owner's scores
- **UserProfileComponent**: Show user's own score prominently and display earned badges
- **ToolbarComponent**: Show current user's score

## Data Models

### MongoDB Schema Changes

#### Users Collection

```javascript
{
    _id: ObjectId,
    email: string,
    // ... existing fields ...
    loopScore: int,  // NEW: Current total score
    scoreHistory: [  // NEW: Array of score changes
        {
            timestamp: ISODate,
            points: int,
            actionType: string,
            itemRequestId: ObjectId,
            itemName: string
        }
    ],
    badges: [  // NEW: Array of earned badges
        {
            badgeType: string,  // "Bronze", "Silver", "Gold", "FirstLend", "ReliableBorrower", "GenerousLender", "PerfectRecord", "CommunityBuilder"
            awardedAt: ISODate
        }
    ],
    consecutiveOnTimeReturns: int,  // NEW: Track consecutive on-time returns for PerfectRecord badge
    invitedBy: ObjectId  // NEW: Track who invited this user for CommunityBuilder badge (nullable)
}
```

### Indexes

Add index for efficient score queries:
```javascript
db.users.createIndex({ "loopScore": -1 })  // For leaderboard queries (future)
```

## Error Handling

### Backend Error Scenarios

1. **User Not Found**: Return 404 when userId doesn't exist
2. **ItemRequest Not Found**: Log warning and skip score update
3. **Concurrent Updates**: Use MongoDB atomic operations ($inc) to prevent race conditions
4. **Negative Score Prevention**: Use $max operator to ensure score never goes below 0

### Frontend Error Scenarios

1. **Score Load Failure**: Display "—" or "N/A" instead of score
2. **Network Errors**: Show cached score if available, otherwise hide score display
3. **Invalid Score Data**: Default to 0 and log error

## Testing Strategy

### Backend Unit Tests

#### LoopScoreServiceTests.cs

Test cases:
- `AwardBorrowPointsAsync_IncreasesScoreByOne_AndCreatesHistoryEntry`
- `AwardOnTimeReturnPointsAsync_IncreasesScoreByOne_WhenReturnedOnTime`
- `AwardOnTimeReturnPointsAsync_DoesNotAwardPoints_WhenReturnedLate`
- `AwardOnTimeReturnPointsAsync_IncrementsConsecutiveOnTimeReturns_WhenOnTime`
- `AwardOnTimeReturnPointsAsync_ResetsConsecutiveOnTimeReturns_WhenLate`
- `AwardOnTimeReturnPointsAsync_AwardsPerfectRecordBadge_After25ConsecutiveOnTimeReturns`
- `AwardLendPointsAsync_IncreaseScoreByFour_AndCreatesHistoryEntry`
- `ReverseLendPointsAsync_DecreasesScoreByFour_ButNotBelowZero`
- `GetUserScoreAsync_ReturnsCurrentScore`
- `GetScoreHistoryAsync_ReturnsRecentEntries_InDescendingOrder`
- `AwardPoints_AwardsBronzeBadge_WhenScoreReaches10`
- `AwardPoints_AwardsSilverBadge_WhenScoreReaches50`
- `AwardPoints_AwardsGoldBadge_WhenScoreReaches100`
- `AwardPoints_DoesNotAwardDuplicateBadges_WhenScoreExceedsMilestone`
- `AwardLendPointsAsync_AwardsFirstLendBadge_OnFirstLendingTransaction`
- `AwardLendPointsAsync_DoesNotAwardFirstLendBadge_OnSubsequentLends`
- `AwardOnTimeReturnPointsAsync_AwardsReliableBorrowerBadge_After10OnTimeReturns`
- `AwardOnTimeReturnPointsAsync_DoesNotAwardReliableBorrowerBadge_BeforeThreshold`
- `RecordCompletedLendingTransactionAsync_AwardsGenerousLenderBadge_After50Transactions`
- `RecordCompletedLendingTransactionAsync_DoesNotAwardGenerousLenderBadge_BeforeThreshold`
- `GetCompletedLendingTransactionCountAsync_ReturnsCorrectCount`
- `GetActiveInvitedUsersCountAsync_ReturnsCorrectCount`
- `CompleteRequestAsync_AwardsCommunityBuilderBadge_WhenInviterHas10ActiveInvitees`
- `GetUserBadgesAsync_ReturnsAllEarnedBadges_IncludingAchievements`
- `GetOnTimeReturnCountAsync_ReturnsCorrectCount`

#### Updated ItemRequestServiceTests.cs

Test cases:
- `ApproveRequestAsync_AwardsLendPoints_ToOwner`
- `CompleteRequestAsync_AwardsBorrowPoints_ToRequester`
- `CompleteRequestAsync_AwardsOnTimeReturnPoints_WhenReturnedOnTime`
- `CompleteRequestAsync_DoesNotAwardOnTimePoints_WhenReturnedLate`
- `CompleteRequestAsync_RecordsCompletedLendingTransaction_ForOwner`
- `CompleteRequestAsync_ChecksCommunityBuilderBadge_WhenBorrowerWasInvited`
- `CancelRequestAsync_ReversesLendPoints_WhenRequestWasApproved`

#### UserControllerTests.cs

Test cases:
- `GetUserScore_ReturnsScore_WhenUserExists`
- `GetUserScore_Returns404_WhenUserNotFound`
- `GetScoreHistory_ReturnsHistory_WithPagination`
- `GetUserBadges_ReturnsBadges_WhenUserExists`
- `GetUserBadges_Returns404_WhenUserNotFound`

### Frontend Unit Tests

#### loop-score.service.spec.ts

Test cases:
- `getUserScore() should fetch user score from API`
- `getScoreHistory() should fetch score history with limit`
- `getUserBadges() should fetch user badges from API`
- `getScoreExplanation() should return score rules`
- `getBadgeMilestones() should return badge milestone values`

#### loop-score-display.component.spec.ts

Test cases:
- `should display score value correctly`
- `should apply correct size class`
- `should show 0 for new users`

#### score-history.component.spec.ts

Test cases:
- `should load and display score history`
- `should format timestamps correctly`
- `should display action types with appropriate labels`
- `should show empty state when no history`

#### badge-display.component.spec.ts

Test cases:
- `should display all earned badges`
- `should categorize badges into milestone and achievement sections`
- `should apply correct CSS class for each badge type`
- `should display correct icons for achievement badges`
- `should display correct labels for achievement badges`
- `should show empty state when no badges earned`
- `should format badge awarded dates correctly`

### Integration Tests

#### Score Calculation Flow

Test the complete flow:
1. User A approves a borrow request → User A gains 4 points
2. User B completes the request on time → User B gains 2 points (1 for borrow + 1 for on-time), consecutive on-time returns increments
3. User B completes the request late → User B gains 1 point (only borrow, no on-time bonus), consecutive on-time returns resets to 0
4. User A cancels an approved request → User A loses 4 points (but not below 0)
5. User A completes 50 lending transactions → User A earns GenerousLender badge
6. User B completes 25 consecutive on-time returns → User B earns PerfectRecord badge
7. User C invites 10 users who each complete at least one transaction → User C earns CommunityBuilder badge

## Implementation Notes

### Point Calculation Logic

**Borrow Completed** (+1 point):
- Awarded when ItemRequest.Status changes to Completed
- Awarded to the requester (borrower)

**On-Time Return** (+1 point):
- Awarded when ItemRequest.Status changes to Completed
- Awarded to the requester (borrower)
- Only if CompletedAt <= ExpectedReturnDate
- If ExpectedReturnDate is null, consider it on-time

**Lend Approved** (+4 points):
- Awarded when ItemRequest.Status changes to Approved
- Awarded to the owner (lender)

**Lend Cancelled** (-4 points):
- Deducted when an Approved ItemRequest is cancelled
- Deducted from the owner (lender)
- Score cannot go below 0

### UI Display Guidelines

**Score Badge Styling**:
- Small: 16px height, used in lists and cards
- Medium: 24px height, used in headers and profiles
- Large: 32px height, used in user profile page

**Color Scheme**:
- Background: Gold/yellow gradient (#FFD700 to #FFA500)
- Text: Dark brown (#4A3000)
- Icon: Star emoji (⭐) or custom SVG

**Placement**:
- Always to the right of the user's name
- Consistent spacing (8px margin-left)
- Vertically centered with name text

### Performance Considerations

1. **Caching**: Cache user scores in AuthService to avoid repeated API calls
2. **Batch Updates**: When displaying multiple users, fetch scores in batch if possible
3. **Optimistic Updates**: Update score immediately in UI, sync with server in background
4. **Debouncing**: Debounce score history requests when scrolling

### Security Considerations

1. **Authorization**: Users can only view their own detailed score history
2. **Public Scores**: All users can see other users' total scores (read-only)
3. **Score Manipulation**: Only ItemRequestService can modify scores, not direct API calls
4. **Audit Trail**: ScoreHistory provides tamper-evident record of all changes

## Integration Requirements

### Loop Invitation System Integration

The CommunityBuilder badge requires integration with the existing loop invitation system:

**Required Changes**:
1. When a user accepts a loop invitation, set the `User.InvitedBy` field to the inviter's user ID
2. The invitation system should track which user sent each invitation
3. When a user completes their first transaction (as borrower or lender), trigger a check for the CommunityBuilder badge for their inviter

**Existing System Analysis Needed**:
- Review current loop invitation implementation (LoopInvitationService)
- Determine how invitations are tracked and accepted
- Identify where to set the `InvitedBy` field during user registration/invitation acceptance
- Ensure invitation tracking persists through the user registration flow

**Alternative Approach** (if invitation system doesn't track inviter):
- Add an `inviterId` parameter to the invitation acceptance flow
- Store invitation records with inviter information
- Populate `User.InvitedBy` when user accepts invitation

## Migration Strategy

### Database Migration

1. Add `loopScore`, `scoreHistory`, `badges`, `consecutiveOnTimeReturns`, and `invitedBy` fields to existing User documents
2. Initialize all existing users with `loopScore: 0`, `scoreHistory: []`, `badges: []`, `consecutiveOnTimeReturns: 0`, and `invitedBy: null`
3. Optionally: Calculate historical scores from completed ItemRequests and award appropriate badges (one-time script)
4. **Important**: The `invitedBy` field requires integration with the existing loop invitation system to populate correctly for new users

### Rollout Plan

1. **Phase 1**: Deploy backend changes with score calculation and badge awards (excluding CommunityBuilder badge)
2. **Phase 2**: Deploy frontend score display components
3. **Phase 3**: Add score history and badge display to user profiles
4. **Phase 4**: Integrate with loop invitation system and enable CommunityBuilder badge
5. **Phase 5**: (Future) Add leaderboards and additional achievements

## Badge Award Logic

### Milestone Badge Thresholds

- **Bronze Badge**: Awarded when user reaches 10 points
- **Silver Badge**: Awarded when user reaches 50 points
- **Gold Badge**: Awarded when user reaches 100 points

### Achievement Badge Criteria

- **FirstLend Badge**: Awarded when user completes their first lending transaction (first time AwardLendPointsAsync is called)
- **ReliableBorrower Badge**: Awarded when user completes 10 on-time returns (tracked via ScoreHistory entries with actionType "OnTimeReturn")
- **GenerousLender Badge**: Awarded when user completes 50 lending transactions (tracked by counting ItemRequests that reach "Completed" status where user is the owner)
- **PerfectRecord Badge**: Awarded when user completes 25 consecutive on-time returns without any late returns (tracked via User.ConsecutiveOnTimeReturns field)
- **CommunityBuilder Badge**: Awarded when 10 users invited by the user each complete at least one transaction as borrower or lender (tracked via User.InvitedBy field)

### Award Rules

1. Badges are checked and awarded automatically after relevant actions:
   - Milestone badges: After each score update
   - FirstLend: When AwardLendPointsAsync is called
   - ReliableBorrower: When AwardOnTimeReturnPointsAsync is called
2. Each badge can only be awarded once per user
3. Badges are never removed, even if score decreases or behavior changes
4. Badge awards are timestamped for display purposes
5. Users can earn multiple badges as they progress through milestones and achievements
6. Email notifications are sent when badges are awarded

### Implementation Details

**For Milestone Badges** (after each score update operation):
1. Check current user score against badge thresholds (10, 50, 100)
2. Identify any new milestone badges that should be awarded
3. Filter out badges already earned by the user
4. Add new badge awards to user's badges array with current timestamp
5. Use atomic MongoDB operations to prevent duplicate awards

**For Achievement Badges**:
1. **FirstLend**: Check if user already has FirstLend badge; if not, award it when AwardLendPointsAsync is called
2. **ReliableBorrower**: Count ScoreHistory entries with actionType "OnTimeReturn"; if count reaches 10 and badge not yet awarded, award it
3. **GenerousLender**: Count completed lending transactions (via RecordCompletedLendingTransactionAsync); if count reaches 50 and badge not yet awarded, award it
4. **PerfectRecord**: Track User.ConsecutiveOnTimeReturns field; increment on on-time returns, reset on late returns; if count reaches 25 and badge not yet awarded, award it
5. **CommunityBuilder**: When a user completes their first transaction, check if they were invited (User.InvitedBy is set); if so, count how many active invited users the inviter has; if count reaches 10 and badge not yet awarded, award it to the inviter
6. Use atomic MongoDB operations to prevent duplicate awards
7. Send email notification for each badge awarded

## Design Rationale for New Achievement Badges

### Generous Lender Badge (50 Lending Transactions)

**Purpose**: Recognize users who consistently share their items with the community over time.

**Design Decision**: Track completed lending transactions separately from lending approvals because:
- Approvals can be cancelled before the item is actually lent
- Completed transactions represent actual sharing behavior
- This metric better reflects sustained generosity

**Implementation**: Use a separate tracking method `RecordCompletedLendingTransactionAsync` called when ItemRequest reaches "Completed" status, counting transactions where the user is the owner.

### Perfect Record Badge (25 Consecutive On-Time Returns)

**Purpose**: Recognize borrowers who consistently return items on time, building trust in the community.

**Design Decision**: Track consecutive on-time returns rather than total on-time returns because:
- Consecutive tracking emphasizes sustained reliability
- Resets on late returns to maintain high standards
- Creates a more challenging and meaningful achievement
- Encourages users to maintain their streak

**Implementation**: Add `ConsecutiveOnTimeReturns` field to User model, increment on each on-time return, reset to zero on late returns. This approach is more efficient than counting ScoreHistory entries and provides real-time tracking.

### Community Builder Badge (10 Active Invited Users)

**Purpose**: Recognize users who grow the platform by inviting new members who become active participants.

**Design Decision**: Require invited users to complete at least one transaction because:
- Ensures invited users are genuinely engaged, not just registered
- Rewards quality invitations over quantity
- Aligns with platform goal of active participation
- Prevents gaming the system with inactive accounts

**Implementation**: Add `InvitedBy` field to User model to track invitation relationships. When a user completes their first transaction, check if they were invited and potentially award CommunityBuilder badge to the inviter. This requires integration with the existing loop invitation system.

**Integration Considerations**:
- The `InvitedBy` field should be set when a user accepts a loop invitation
- Existing invitation system may need updates to track the inviter
- Badge check should occur on first completed transaction (as either borrower or lender)
- Count active invited users by querying users where `InvitedBy` matches the inviter's ID and they have at least one ScoreHistory entry

## Design Gap Analysis: Requirement 12 (Display All Available Badges)

### Current Design Limitation

The existing BadgeDisplayComponent design only displays **earned badges**. However, Requirement 12 specifies that users should see **all available achievement badges** including those they haven't earned yet, so they understand what goals to work toward.

### Required Design Changes

#### 1. Badge Metadata Service Extension

Add a new method to LoopScoreService to provide complete badge information:

```typescript
export interface BadgeMetadata {
    badgeType: BadgeType;
    name: string;
    description: string;
    category: 'milestone' | 'achievement';
    requirement: string; // e.g., "Reach 10 points", "Complete 10 on-time returns"
    icon: string;
}

@Injectable({ providedIn: 'root' })
export class LoopScoreService {
    // Existing methods...
    
    getAllBadgeMetadata(): BadgeMetadata[] {
        return [
            // Milestone badges
            { badgeType: 'Bronze', name: 'Bronze Badge', description: 'Awarded for reaching 10 points', category: 'milestone', requirement: 'Reach 10 points', icon: '🏆' },
            { badgeType: 'Silver', name: 'Silver Badge', description: 'Awarded for reaching 50 points', category: 'milestone', requirement: 'Reach 50 points', icon: '🏆' },
            { badgeType: 'Gold', name: 'Gold Badge', description: 'Awarded for reaching 100 points', category: 'milestone', requirement: 'Reach 100 points', icon: '🏆' },
            // Achievement badges
            { badgeType: 'FirstLend', name: 'First Lend', description: 'Complete your first lending transaction', category: 'achievement', requirement: 'Lend an item for the first time', icon: '🎁' },
            { badgeType: 'ReliableBorrower', name: 'Reliable Borrower', description: 'Return items on time consistently', category: 'achievement', requirement: 'Complete 10 on-time returns', icon: '⭐' },
            { badgeType: 'GenerousLender', name: 'Generous Lender', description: 'Share your items frequently', category: 'achievement', requirement: 'Complete 50 lending transactions', icon: '🤝' },
            { badgeType: 'PerfectRecord', name: 'Perfect Record', description: 'Maintain a perfect return streak', category: 'achievement', requirement: 'Complete 25 consecutive on-time returns', icon: '💯' },
            { badgeType: 'CommunityBuilder', name: 'Community Builder', description: 'Grow the LendingLoop community', category: 'achievement', requirement: 'Invite 10 users who become active', icon: '🌟' }
        ];
    }
}
```

#### 2. Enhanced BadgeDisplayComponent

Update the component to display all badges with earned/unearned states:

```typescript
@Component({
    selector: 'app-badge-display',
    templateUrl: './badge-display.component.html',
    styleUrls: ['./badge-display.component.css']
})
export class BadgeDisplayComponent implements OnInit {
    @Input() earnedBadges: BadgeAward[] = [];
    @Input() showAllBadges: boolean = true; // New input to control display mode
    
    allBadgeMetadata: BadgeMetadata[] = [];
    displayBadges: DisplayBadge[] = [];
    
    constructor(private loopScoreService: LoopScoreService) {}
    
    ngOnInit(): void {
        this.allBadgeMetadata = this.loopScoreService.getAllBadgeMetadata();
        this.prepareDisplayBadges();
    }
    
    prepareDisplayBadges(): void {
        this.displayBadges = this.allBadgeMetadata.map(metadata => {
            const earnedBadge = this.earnedBadges.find(b => b.badgeType === metadata.badgeType);
            return {
                metadata: metadata,
                earned: !!earnedBadge,
                awardedAt: earnedBadge?.awardedAt
            };
        });
    }
}

interface DisplayBadge {
    metadata: BadgeMetadata;
    earned: boolean;
    awardedAt?: string;
}
```

#### 3. Updated Component Template

```html
<div class="badges-container">
    <div class="milestone-badges">
        <h4>Milestone Badges</h4>
        <div class="badge-grid">
            <div *ngFor="let badge of displayBadges | filterByCategory:'milestone'" 
                 class="badge-item"
                 [class.earned]="badge.earned"
                 [class.unearned]="!badge.earned"
                 [attr.aria-label]="getBadgeAriaLabel(badge)">
                <span class="badge-icon">{{ badge.metadata.icon }}</span>
                <span class="badge-name">{{ badge.metadata.name }}</span>
                <span class="badge-description">{{ badge.metadata.description }}</span>
                <span class="badge-requirement" *ngIf="!badge.earned">{{ badge.metadata.requirement }}</span>
                <span class="badge-earned-date" *ngIf="badge.earned">Earned: {{ badge.awardedAt | date }}</span>
            </div>
        </div>
    </div>
    
    <div class="achievement-badges">
        <h4>Achievement Badges</h4>
        <div class="badge-grid">
            <div *ngFor="let badge of displayBadges | filterByCategory:'achievement'" 
                 class="badge-item"
                 [class.earned]="badge.earned"
                 [class.unearned]="!badge.earned"
                 [attr.aria-label]="getBadgeAriaLabel(badge)">
                <span class="badge-icon">{{ badge.metadata.icon }}</span>
                <span class="badge-name">{{ badge.metadata.name }}</span>
                <span class="badge-description">{{ badge.metadata.description }}</span>
                <span class="badge-requirement" *ngIf="!badge.earned">{{ badge.metadata.requirement }}</span>
                <span class="badge-earned-date" *ngIf="badge.earned">Earned: {{ badge.awardedAt | date }}</span>
            </div>
        </div>
    </div>
</div>
```

#### 4. CSS Styling for Earned/Unearned States

```css
.badge-item {
    display: flex;
    flex-direction: column;
    align-items: center;
    padding: 16px;
    border-radius: 8px;
    transition: all 0.3s ease;
}

.badge-item.earned {
    background-color: #fff;
    border: 2px solid #FFD700;
    opacity: 1;
}

.badge-item.earned .badge-icon {
    font-size: 48px;
    filter: none;
}

.badge-item.unearned {
    background-color: #f5f5f5;
    border: 2px solid #ddd;
    opacity: 0.6;
}

.badge-item.unearned .badge-icon {
    font-size: 48px;
    filter: grayscale(100%);
}

.badge-name {
    font-weight: bold;
    margin-top: 8px;
    color: #333;
}

.badge-description {
    font-size: 12px;
    color: #666;
    text-align: center;
    margin-top: 4px;
}

.badge-requirement {
    font-size: 11px;
    color: #999;
    font-style: italic;
    text-align: center;
    margin-top: 4px;
}

.badge-earned-date {
    font-size: 11px;
    color: #4CAF50;
    margin-top: 4px;
}

.badge-grid {
    display: grid;
    grid-template-columns: repeat(auto-fill, minmax(150px, 1fr));
    gap: 16px;
    margin-top: 12px;
}
```

### Design Rationale

**Why Show All Badges?**
- **Goal Clarity**: Users can see what achievements are possible and work toward specific goals
- **Motivation**: Seeing unearned badges creates aspirational targets that encourage engagement
- **Transparency**: Users understand the complete badge system rather than discovering badges by accident
- **Gamification Best Practice**: Most successful gamification systems show locked/unearned achievements

**Why Use Greyed-Out Styling?**
- **Visual Hierarchy**: Clearly distinguishes earned from unearned badges without hiding information
- **Industry Standard**: Common pattern in gaming and achievement systems (Steam, Xbox, PlayStation)
- **Accessibility**: Maintains visibility for screen readers while providing visual distinction

**Why Include Descriptions and Requirements?**
- **User Education**: Explains how to earn each badge without requiring external documentation
- **Reduced Support**: Users don't need to ask how to earn badges
- **Engagement**: Clear requirements encourage users to pursue specific badges

### Implementation Impact

This design change requires updates to:
1. **LoopScoreService**: Add `getAllBadgeMetadata()` method
2. **BadgeDisplayComponent**: Refactor to display all badges with earned/unearned states
3. **Component Template**: Update to show badge metadata and requirements
4. **Component CSS**: Add styling for unearned badges
5. **Unit Tests**: Add tests for displaying unearned badges and badge metadata

## Future Enhancements

- **Leaderboards**: Display top scorers within each loop
- **Additional Milestone Badges**: Platinum badge at 250 points, Diamond at 500 points
- **More Achievement Badges**: 
  - "Super Lender" for 100 lending transactions
  - "Legendary Borrower" for 50 consecutive on-time returns
  - "Community Champion" for inviting 25 active users
- **Streak Bonuses**: Extra points for consecutive on-time returns
- **Decay System**: Reduce points over time to encourage ongoing participation
- **Custom Point Values**: Allow loop admins to configure point values
- **Badge Showcase**: Allow users to feature their favorite badge on their profile
- **Badge Notifications**: Push notifications when badges are earned
- **Badge Rarity Display**: Show how many users have earned each badge
- **Progress Tracking**: Show progress toward unearned badges (e.g., "7/10 on-time returns")
