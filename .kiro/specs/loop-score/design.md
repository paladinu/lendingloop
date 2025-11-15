# LoopScore Feature Design

## Overview

The LoopScore feature implements a gamification system that awards points to users based on their participation in the LendingLoop platform. The system automatically calculates and updates scores when users complete borrowing and lending activities, and displays scores throughout the UI to recognize active community members.

### Key Design Principles

- **Automatic Calculation**: Scores are calculated automatically based on ItemRequest status changes, requiring no manual intervention
- **Real-time Updates**: Score changes are reflected immediately in the UI without page refreshes
- **Audit Trail**: All score changes are tracked with timestamps and reasons for transparency
- **Non-negative Scores**: Scores cannot go below zero to maintain positive reinforcement
- **Consistent Display**: Scores appear uniformly next to user names across all UI components

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
}
```

#### 3. LoopScoreService Implementation

The service will:
- Calculate points based on action type
- Update User.LoopScore field
- Add entries to User.ScoreHistory
- Enforce minimum score of zero
- Use atomic MongoDB operations to prevent race conditions

#### 4. Updated ItemRequestService

Integrate LoopScoreService calls at key points:
- **ApproveRequestAsync**: Award 4 points to lender
- **CompleteRequestAsync**: Award 1 point to borrower, check for on-time return and award 1 additional point if applicable
- **CancelRequestAsync** (for approved requests): Reverse 4 points from lender

#### 5. UserController Extensions

Add new endpoints:
- `GET /api/users/{userId}/score` - Get current score
- `GET /api/users/{userId}/score-history` - Get score history with pagination

### Frontend Components

#### 1. Extended UserProfile Interface

```typescript
export interface UserProfile {
    // Existing fields...
    loopScore: number;
}

export interface ScoreHistoryEntry {
    timestamp: string;
    points: number;
    actionType: 'BorrowCompleted' | 'OnTimeReturn' | 'LendApproved' | 'LendCancelled';
    itemRequestId: string;
    itemName: string;
}
```

#### 2. LoopScoreService

```typescript
@Injectable({ providedIn: 'root' })
export class LoopScoreService {
    getUserScore(userId: string): Observable<number>;
    getScoreHistory(userId: string, limit?: number): Observable<ScoreHistoryEntry[]>;
    getScoreExplanation(): ScoreRules;
}

export interface ScoreRules {
    borrowCompleted: number;
    onTimeReturn: number;
    lendApproved: number;
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

#### 5. Updated Components

The following existing components will be updated to display scores:
- **ItemCardComponent**: Show owner's score
- **ItemDetailComponent**: Show owner's score
- **LoopMembersComponent**: Show each member's score
- **ItemRequestListComponent**: Show requester's and owner's scores
- **UserProfileComponent**: Show user's own score prominently
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

### Frontend Unit Tests

#### loop-score.service.spec.ts

Test cases:
- `getUserScore() should fetch user score from API`
- `getScoreHistory() should fetch score history with limit`
- `getScoreExplanation() should return score rules`

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

1. Add `loopScore` and `scoreHistory` fields to existing User documents
2. Initialize all existing users with `loopScore: 0` and `scoreHistory: []`
3. Optionally: Calculate historical scores from completed ItemRequests (one-time script)

### Rollout Plan

1. **Phase 1**: Deploy backend changes with score calculation
2. **Phase 2**: Deploy frontend score display components
3. **Phase 3**: Add score history view to user profiles
4. **Phase 4**: (Future) Add leaderboards and achievements

## Future Enhancements

- **Leaderboards**: Display top scorers within each loop
- **Achievements**: Award badges for milestones (10 points, 50 points, etc.)
- **Streak Bonuses**: Extra points for consecutive on-time returns
- **Decay System**: Reduce points over time to encourage ongoing participation
- **Custom Point Values**: Allow loop admins to configure point values
- **Score Tiers**: Bronze/Silver/Gold/Platinum levels based on score ranges
