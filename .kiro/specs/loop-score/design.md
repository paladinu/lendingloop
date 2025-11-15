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
    ReliableBorrower     // 10 on-time returns
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
    Task AwardOnTimeReturnPointsAsync(string userId, string itemRequestId, string itemName);
    Task AwardLendPointsAsync(string userId, string itemRequestId, string itemName);
    Task ReverseLendPointsAsync(string userId, string itemRequestId, string itemName);
    Task<int> GetUserScoreAsync(string userId);
    Task<List<ScoreHistoryEntry>> GetScoreHistoryAsync(string userId, int limit = 50);
    Task<List<BadgeAward>> GetUserBadgesAsync(string userId);
    Task<int> GetOnTimeReturnCountAsync(string userId);
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
- Award badges automatically when thresholds are reached (10, 50, 100 points for milestones)
- Prevent duplicate badge awards
- Enforce minimum score of zero
- Use atomic MongoDB operations to prevent race conditions
- Send email notifications when badges are awarded

#### 4. Updated ItemRequestService

Integrate LoopScoreService calls at key points:
- **ApproveRequestAsync**: Award 4 points to lender
- **CompleteRequestAsync**: Award 1 point to borrower, check for on-time return and award 1 additional point if applicable
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
    badgeType: 'Bronze' | 'Silver' | 'Gold' | 'FirstLend' | 'ReliableBorrower';
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
            ['FirstLend', 'ReliableBorrower'].includes(b.badgeType)
        );
    }
    
    getBadgeIcon(badgeType: string): string {
        const icons: Record<string, string> = {
            'FirstLend': '🎁',
            'ReliableBorrower': '⭐'
        };
        return icons[badgeType] || '🏅';
    }
    
    getBadgeLabel(badgeType: string): string {
        const labels: Record<string, string> = {
            'FirstLend': 'First Lend',
            'ReliableBorrower': 'Reliable Borrower'
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
            badgeType: string,  // "Bronze", "Silver", "Gold", "FirstLend", "ReliableBorrower"
            awardedAt: ISODate
        }
    ]
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
- `GetUserBadgesAsync_ReturnsAllEarnedBadges_IncludingAchievements`
- `GetOnTimeReturnCountAsync_ReturnsCorrectCount`

#### Updated ItemRequestServiceTests.cs

Test cases:
- `ApproveRequestAsync_AwardsLendPoints_ToOwner`
- `CompleteRequestAsync_AwardsBorrowPoints_ToRequester`
- `CompleteRequestAsync_AwardsOnTimeReturnPoints_WhenReturnedOnTime`
- `CompleteRequestAsync_DoesNotAwardOnTimePoints_WhenReturnedLate`
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
2. User B completes the request on time → User B gains 2 points (1 for borrow + 1 for on-time)
3. User B completes the request late → User B gains 1 point (only borrow, no on-time bonus)
4. User A cancels an approved request → User A loses 4 points (but not below 0)

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

## Migration Strategy

### Database Migration

1. Add `loopScore`, `scoreHistory`, and `badges` fields to existing User documents
2. Initialize all existing users with `loopScore: 0`, `scoreHistory: []`, and `badges: []`
3. Optionally: Calculate historical scores from completed ItemRequests and award appropriate badges (one-time script)

### Rollout Plan

1. **Phase 1**: Deploy backend changes with score calculation and badge awards
2. **Phase 2**: Deploy frontend score display components
3. **Phase 3**: Add score history and badge display to user profiles
4. **Phase 4**: (Future) Add leaderboards and additional achievements

## Badge Award Logic

### Milestone Badge Thresholds

- **Bronze Badge**: Awarded when user reaches 10 points
- **Silver Badge**: Awarded when user reaches 50 points
- **Gold Badge**: Awarded when user reaches 100 points

### Achievement Badge Criteria

- **FirstLend Badge**: Awarded when user completes their first lending transaction (first time AwardLendPointsAsync is called)
- **ReliableBorrower Badge**: Awarded when user completes 10 on-time returns (tracked via ScoreHistory entries with actionType "OnTimeReturn")

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
3. Use atomic MongoDB operations to prevent duplicate awards
4. Send email notification for each badge awarded

## Future Enhancements

- **Leaderboards**: Display top scorers within each loop
- **Additional Milestone Badges**: Platinum badge at 250 points, Diamond at 500 points
- **More Achievement Badges**: 
  - "Generous Lender" for 50 lending transactions
  - "Perfect Record" for 25 consecutive on-time returns
  - "Community Builder" for inviting 10 users who complete transactions
- **Streak Bonuses**: Extra points for consecutive on-time returns
- **Decay System**: Reduce points over time to encourage ongoing participation
- **Custom Point Values**: Allow loop admins to configure point values
- **Badge Showcase**: Allow users to feature their favorite badge on their profile
- **Badge Notifications**: Push notifications when badges are earned
