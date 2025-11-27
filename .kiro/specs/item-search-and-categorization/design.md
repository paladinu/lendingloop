# Design Document

## Overview

This feature adds item categorization through predefined system tags and advanced search/filtering capabilities to the LendingLoop platform. The design extends the existing SharedItem model to include tags and enhances the loop landing page with comprehensive filtering options. The system will maintain a centralized list of predefined tags that users can select from, ensuring consistency and preventing inappropriate categorization.

## Architecture

### High-Level Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                      Angular Frontend                        │
│  ┌────────────────┐  ┌──────────────┐  ┌─────────────────┐ │
│  │ Item Add/Edit  │  │ Loop Detail  │  │ Filter Controls │ │
│  │   Component    │  │  Component   │  │   Component     │ │
│  └────────┬───────┘  └──────┬───────┘  └────────┬────────┘ │
│           │                  │                    │          │
│           └──────────────────┼────────────────────┘          │
│                              │                               │
│                    ┌─────────▼──────────┐                   │
│                    │   Items Service    │                   │
│                    │   Tags Service     │                   │
│                    └─────────┬──────────┘                   │
└──────────────────────────────┼────────────────────────────────┘
                               │ HTTP/REST
┌──────────────────────────────▼────────────────────────────────┐
│                      .NET Backend API                         │
│  ┌────────────────┐  ┌──────────────┐  ┌─────────────────┐  │
│  │ Items          │  │ Tags         │  │ Search/Filter   │  │
│  │ Controller     │  │ Controller   │  │ Logic           │  │
│  └────────┬───────┘  └──────┬───────┘  └────────┬────────┘  │
│           │                  │                    │           │
│  ┌────────▼──────────────────▼────────────────────▼────────┐ │
│  │              Items Service (Enhanced)                   │ │
│  │              Tags Service (New)                         │ │
│  └────────┬────────────────────────────────────────────────┘ │
└───────────┼──────────────────────────────────────────────────┘
            │
┌───────────▼──────────────────────────────────────────────────┐
│                      MongoDB Database                         │
│  ┌────────────────┐  ┌──────────────────────────────────┐   │
│  │ items          │  │ systemTags (new collection)      │   │
│  │ (+ tags field) │  │                                  │   │
│  └────────────────┘  └──────────────────────────────────┘   │
└───────────────────────────────────────────────────────────────┘
```

### Data Flow

1. **Tag Selection Flow**: User selects tags from predefined list → Frontend validates selection → Backend stores tags with item
2. **Search/Filter Flow**: User applies filters → Frontend sends filter criteria → Backend queries MongoDB with filters → Results returned and displayed
3. **Tag Management Flow**: Admin updates tag configuration → Backend reloads tag list → New tags available for selection

## Components and Interfaces

### Backend Components

#### 1. SystemTag Model (New)

```csharp
public class SystemTag
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }
    
    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;
    
    [BsonElement("displayName")]
    public string DisplayName { get; set; } = string.Empty;
    
    [BsonElement("category")]
    public string Category { get; set; } = string.Empty;
    
    [BsonElement("usageCount")]
    public int UsageCount { get; set; } = 0;
    
    [BsonElement("isActive")]
    public bool IsActive { get; set; } = true;
    
    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
```

#### 2. SharedItem Model (Enhanced)

Add tags field to existing model:

```csharp
[BsonElement("tags")]
public List<string> Tags { get; set; } = new();
```

#### 3. ItemSearchFilter DTO (New)

```csharp
public class ItemSearchFilter
{
    public string? SearchText { get; set; }
    public List<string> Tags { get; set; } = new();
    public bool? IsAvailable { get; set; }
    public List<string> OwnerIds { get; set; } = new();
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}
```

#### 4. ItemSearchResult DTO (New)

```csharp
public class ItemSearchResult
{
    public List<SharedItem> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}
```

#### 5. ITagsService Interface (New)

```csharp
public interface ITagsService
{
    Task<List<SystemTag>> GetAllActiveTagsAsync();
    Task<SystemTag?> GetTagByNameAsync(string name);
    Task InitializeDefaultTagsAsync();
    Task IncrementTagUsageAsync(string tagName);
    Task DecrementTagUsageAsync(string tagName);
    Task<Dictionary<string, int>> GetTagUsageStatisticsAsync();
}
```

#### 6. TagsService Implementation (New)

Manages predefined system tags with the following responsibilities:
- Load and cache system tags from database
- Initialize default tags on first run
- Track tag usage statistics
- Provide tag search/filter capabilities

#### 7. IItemsService Interface (Enhanced)

Add new methods:

```csharp
Task<ItemSearchResult> SearchItemsAsync(string loopId, ItemSearchFilter filter);
Task<List<string>> GetDistinctOwnersInLoopAsync(string loopId);
```

#### 8. ItemsService Implementation (Enhanced)

Add search and filter logic:
- Build MongoDB filter expressions from search criteria
- Implement tag filtering (AND logic for multiple tags)
- Implement availability filtering
- Implement owner filtering
- Combine text search with filters
- Implement pagination

#### 9. TagsController (New)

```csharp
[ApiController]
[Route("api/[controller]")]
public class TagsController : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<SystemTag>>> GetAllTags();
    
    [HttpGet("statistics")]
    public async Task<ActionResult<Dictionary<string, int>>> GetTagStatistics();
}
```

#### 10. ItemsController (Enhanced)

Add search endpoint:

```csharp
[HttpPost("search/{loopId}")]
public async Task<ActionResult<ItemSearchResult>> SearchItems(
    string loopId, 
    [FromBody] ItemSearchFilter filter);

[HttpGet("loop/{loopId}/owners")]
public async Task<ActionResult<List<string>>> GetDistinctOwners(string loopId);
```

### Frontend Components

#### 1. SystemTag Interface (New)

```typescript
export interface SystemTag {
    id: string;
    name: string;
    displayName: string;
    category: string;
    usageCount: number;
    isActive: boolean;
}
```

#### 2. SharedItem Interface (Enhanced)

Add tags field:

```typescript
tags: string[];
```

#### 3. ItemSearchFilter Interface (New)

```typescript
export interface ItemSearchFilter {
    searchText?: string;
    tags: string[];
    isAvailable?: boolean;
    ownerIds: string[];
    pageNumber: number;
    pageSize: number;
}
```

#### 4. ItemSearchResult Interface (New)

```typescript
export interface ItemSearchResult {
    items: SharedItem[];
    totalCount: number;
    pageNumber: number;
    pageSize: number;
    totalPages: number;
}
```

#### 5. TagsService (New)

```typescript
@Injectable({ providedIn: 'root' })
export class TagsService {
    getAllTags(): Observable<SystemTag[]>;
    getTagStatistics(): Observable<Map<string, number>>;
    searchTags(query: string): SystemTag[];
}
```

#### 6. ItemsService (Enhanced)

Add search method:

```typescript
searchItems(loopId: string, filter: ItemSearchFilter): Observable<ItemSearchResult>;
getDistinctOwners(loopId: string): Observable<string[]>;
```

#### 7. TagSelectorComponent (New)

Reusable component for tag selection:
- Display available tags in a searchable list
- Allow multi-select with visual feedback
- Show selected tags as chips
- Enforce 10-tag limit
- Emit selection changes to parent

#### 8. ItemFilterComponent (New)

Comprehensive filter controls:
- Tag filter (multi-select)
- Availability filter (checkbox)
- Owner filter (multi-select dropdown)
- Clear all filters button
- Display active filter count

#### 9. Item Add/Edit Components (Enhanced)

Integrate TagSelectorComponent for tag selection during item creation/editing.

#### 10. Loop Detail Component (Enhanced)

- Integrate ItemFilterComponent
- Implement search with debouncing
- Handle filter state management
- Update item list based on filters
- Display filter results count

#### 11. Item Card Component (Enhanced)

- Display up to 5 tags as chips
- Show "+N more" indicator for additional tags
- Make tags clickable to apply as filter
- Consistent tag styling

## Data Models

### MongoDB Collections

#### items Collection (Enhanced)

```json
{
    "_id": "ObjectId",
    "name": "string",
    "description": "string",
    "userId": "string",
    "isAvailable": "boolean",
    "imageUrl": "string",
    "visibleToLoopIds": ["string"],
    "visibleToAllLoops": "boolean",
    "visibleToFutureLoops": "boolean",
    "tags": ["string"],
    "createdAt": "DateTime",
    "updatedAt": "DateTime"
}
```

**New Index**: Create index on `tags` field for efficient filtering.

#### systemTags Collection (New)

```json
{
    "_id": "ObjectId",
    "name": "string",
    "displayName": "string",
    "category": "string",
    "usageCount": "number",
    "isActive": "boolean",
    "createdAt": "DateTime"
}
```

**Indexes**:
- `name` (unique)
- `category`
- `isActive`

### Default System Tags

Initial predefined tags organized by category:

**Tools & Equipment**
- power-tools, hand-tools, garden-tools, automotive-tools, measuring-tools

**Home & Garden**
- lawn-care, cleaning-equipment, ladders, pressure-washers, painting-supplies

**Electronics**
- cameras, audio-equipment, projectors, gaming-consoles, computers

**Sports & Recreation**
- camping-gear, sports-equipment, bikes, water-sports, winter-sports

**Kitchen & Appliances**
- kitchen-appliances, cookware, baking-equipment, party-supplies

**Baby & Kids**
- baby-gear, toys, car-seats, strollers

**Books & Media**
- books, movies, board-games, educational

**Other**
- furniture, storage, seasonal, party-decorations, miscellaneous

## Correctness Properties

*A property is a characteristic or behavior that should hold true across all valid executions of a system-essentially, a formal statement about what the system should do. Properties serve as the bridge between human-readable specifications and machine-verifiable correctness guarantees.*


### Property Reflection

After analyzing all acceptance criteria, several properties can be consolidated to avoid redundancy:

- Properties 3.3 and 5.4 (filter deselection updates) are subsumed by Property 6.1 (combined filters)
- Properties 4.2, 4.3, and 4.4 (availability filtering) can be combined into one comprehensive property
- Properties 2.3, 6.4, and 10.2 (clearing filters) represent the same round-trip behavior
- Properties 7.1 and 7.2 (tag display) can be combined into one property about display limits

### Correctness Properties

Property 1: Tag selection limit enforcement
*For any* item and any set of tags, selecting up to 10 tags should be accepted, and attempting to select more than 10 tags should be rejected with a validation error
**Validates: Requirements 1.3, 1.4**

Property 2: Tag list alphabetical ordering
*For any* set of system tags, when displayed in the selection interface, the tags should be ordered alphabetically by display name
**Validates: Requirements 1.5**

Property 3: Tag search filtering
*For any* search query string, the filtered tag list should contain only tags whose name or display name contains the search text (case-insensitive)
**Validates: Requirements 2.2, 2.4**

Property 4: Tag filter AND logic
*For any* set of selected tags and any collection of items, the filtered results should contain only items that have all selected tags
**Validates: Requirements 3.2**

Property 5: Filter result count accuracy
*For any* active filter combination, the displayed count should equal the actual number of items in the filtered results
**Validates: Requirements 3.5**

Property 6: Availability filter correctness
*For any* availability filter selection (available, unavailable, or both), the results should contain only items matching the selected availability state(s)
**Validates: Requirements 4.2, 4.3, 4.4**

Property 7: Owner filter accuracy
*For any* loop, the owner filter list should contain exactly the set of users who own at least one item visible in that loop
**Validates: Requirements 5.2**

Property 8: Owner filter alphabetical ordering
*For any* loop, the owner names in the filter interface should be displayed in alphabetical order
**Validates: Requirements 5.5**

Property 9: Owner filter correctness
*For any* set of selected owner IDs, the filtered results should contain only items owned by users in the selected set
**Validates: Requirements 5.3**

Property 10: Combined filter AND logic
*For any* combination of active filters (tags, availability, owners, search text), the results should contain only items that satisfy all active filter criteria simultaneously
**Validates: Requirements 6.1, 6.2, 6.3**

Property 11: Filter clear round-trip
*For any* loop with items, applying filters then clearing all filters should return the same set of items as the initial unfiltered state
**Validates: Requirements 6.4, 10.2, 10.4**

Property 12: Tag display limit
*For any* item card, if the item has 5 or fewer tags, all tags should be displayed; if the item has more than 5 tags, exactly 5 tags should be displayed with an indicator showing the count of additional tags
**Validates: Requirements 7.1, 7.2**

Property 13: Tag click applies filter
*For any* tag displayed on an item card, clicking that tag should add it to the active tag filters
**Validates: Requirements 7.3**

Property 14: Inactive tag exclusion
*For any* system tag marked as inactive, it should not appear in the tag selection interface for new items, but should remain associated with items that already have it
**Validates: Requirements 8.3**

Property 15: Tag usage count accuracy
*For any* system tag, its usage count should equal the number of items that currently have that tag assigned
**Validates: Requirements 8.5**

Property 16: Pagination correctness
*For any* search result set with more than 50 items, the results should be divided into pages of 50 items each, with the total page count equal to ceiling(totalItems / 50)
**Validates: Requirements 9.3**

## Error Handling

### Backend Error Scenarios

1. **Invalid Tag Selection**
   - Error: User attempts to add more than 10 tags
   - Response: 400 Bad Request with validation message
   - Message: "Items cannot have more than 10 tags"

2. **Non-existent Tag**
   - Error: User attempts to add a tag that doesn't exist in system tags
   - Response: 400 Bad Request
   - Message: "One or more selected tags are invalid"

3. **Invalid Filter Parameters**
   - Error: Invalid pagination parameters (page < 1, pageSize < 1 or > 100)
   - Response: 400 Bad Request
   - Message: "Invalid pagination parameters"

4. **Database Query Timeout**
   - Error: Search query takes too long
   - Response: 504 Gateway Timeout
   - Message: "Search request timed out. Please try with more specific filters"

5. **Unauthorized Access**
   - Error: User attempts to filter items in a loop they don't belong to
   - Response: 403 Forbidden
   - Message: "You do not have access to this loop"

### Frontend Error Handling

1. **Tag Selection Errors**
   - Display inline validation message when 10-tag limit reached
   - Disable tag selection controls when limit reached
   - Show error toast for API failures

2. **Search/Filter Errors**
   - Display error message in results area
   - Provide "Try Again" button
   - Log errors to console for debugging
   - Maintain last successful filter state

3. **Network Errors**
   - Show loading spinner during requests
   - Display timeout message after 30 seconds
   - Provide retry mechanism
   - Cache last successful results

4. **Empty Results**
   - Display friendly "No items found" message
   - Suggest clearing filters
   - Show active filter summary

## Testing Strategy

### Unit Testing

**Backend Unit Tests (C# with xUnit and Moq)**

1. **TagsService Tests**
   - Test tag initialization with default tags
   - Test tag usage increment/decrement
   - Test tag statistics calculation
   - Test active/inactive tag filtering
   - Mock MongoDB collection

2. **ItemsService Search Tests**
   - Test search with text query
   - Test tag filtering (single and multiple tags)
   - Test availability filtering
   - Test owner filtering
   - Test combined filters
   - Test pagination logic
   - Mock MongoDB collection and filter builders

3. **TagsController Tests**
   - Test GET all tags endpoint
   - Test GET statistics endpoint
   - Mock TagsService

4. **ItemsController Search Tests**
   - Test POST search endpoint
   - Test GET distinct owners endpoint
   - Mock ItemsService and authentication

**Frontend Unit Tests (TypeScript with Jest)**

1. **TagsService Tests**
   - Test getAllTags API call
   - Test tag search/filter logic
   - Test statistics retrieval
   - Mock HttpClient

2. **ItemsService Tests**
   - Test searchItems API call
   - Test getDistinctOwners API call
   - Mock HttpClient

3. **TagSelectorComponent Tests**
   - Test tag display and selection
   - Test 10-tag limit enforcement
   - Test tag search functionality
   - Mock TagsService

4. **ItemFilterComponent Tests**
   - Test filter state management
   - Test clear all filters
   - Test filter change emissions
   - Mock services

5. **Loop Detail Component Tests**
   - Test search with debouncing
   - Test filter application
   - Test result display
   - Mock ItemsService and TagsService

6. **Item Card Component Tests**
   - Test tag display (5-tag limit)
   - Test "+N more" indicator
   - Test tag click behavior
   - Mock data

### Property-Based Testing

**Property-Based Testing Library**: Use FsCheck for C# backend tests

**Configuration**: Each property test should run a minimum of 100 iterations

**Test Tagging Format**: `**Feature: item-search-and-categorization, Property {number}: {property_text}**`

**Backend Property Tests**

1. Tag selection limit property test
2. Tag list ordering property test
3. Tag search filtering property test
4. Tag filter AND logic property test
5. Filter count accuracy property test
6. Availability filter property test
7. Owner filter accuracy property test
8. Owner list ordering property test
9. Owner filter correctness property test
10. Combined filter AND logic property test
11. Filter clear round-trip property test
12. Tag display limit property test
13. Inactive tag exclusion property test
14. Tag usage count accuracy property test
15. Pagination correctness property test

**Frontend Property Tests**

Property tests for frontend will focus on:
- Filter state management logic
- Tag selection validation
- Search/filter combination logic
- Pagination calculations

### Integration Testing

1. **End-to-End Tag Flow**
   - Create item with tags
   - Verify tags stored correctly
   - Search by tags
   - Verify results match

2. **End-to-End Filter Flow**
   - Apply multiple filters
   - Verify combined results
   - Clear filters
   - Verify all items returned

3. **Tag Usage Statistics**
   - Add items with tags
   - Verify usage counts increment
   - Remove items
   - Verify usage counts decrement

## Implementation Notes

### Database Migration

1. **Add tags field to existing items**
   - Run migration script to add empty tags array to all existing items
   - Create index on tags field

2. **Initialize systemTags collection**
   - Create collection with default tags
   - Set up indexes

### Performance Considerations

1. **MongoDB Indexes**
   - Create compound index on (tags, isAvailable, userId) for efficient filtering
   - Create text index on (name, description) for text search
   - Monitor index usage and query performance

2. **Frontend Optimization**
   - Implement debouncing for search input (300ms)
   - Cache tag list in service (refresh on app load)
   - Use virtual scrolling for large result sets
   - Lazy load item images

3. **API Response Size**
   - Limit page size to maximum 100 items
   - Consider implementing field selection to reduce payload
   - Use compression for API responses

### Security Considerations

1. **Input Validation**
   - Validate tag names against system tag list
   - Sanitize search text to prevent injection
   - Validate pagination parameters

2. **Authorization**
   - Verify user belongs to loop before returning items
   - Ensure users can only edit tags on their own items
   - Protect tag statistics endpoint (admin only)

### Backwards Compatibility

1. **Existing Items**
   - Items without tags should display normally
   - Empty tag array is valid state
   - No breaking changes to existing API endpoints

2. **API Versioning**
   - New search endpoint doesn't affect existing endpoints
   - Existing GET items endpoints continue to work
   - Tags field is optional in responses

## Future Enhancements

1. **Tag Categories**
   - Group tags by category in UI
   - Allow filtering by category
   - Category-based tag suggestions

2. **Smart Tag Suggestions**
   - Suggest tags based on item name/description
   - ML-based tag recommendations
   - Popular tags for similar items

3. **Saved Searches**
   - Allow users to save filter combinations
   - Quick access to frequent searches
   - Share saved searches with loop members

4. **Advanced Search**
   - Boolean operators (AND, OR, NOT)
   - Date range filtering
   - Distance/location filtering

5. **Tag Analytics**
   - Most popular tags dashboard
   - Tag usage trends over time
   - Tag co-occurrence analysis
