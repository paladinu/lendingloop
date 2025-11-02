# Design Document

## Overview

The item management feature will enable users to edit all properties of their shared items through a comprehensive edit interface. This design leverages the existing architecture patterns established in the item-add and item-visibility components, consolidating editing functionality into a single, user-friendly screen.

The solution consists of:
- A new Angular component (`item-edit`) for the edit UI
- A new backend API endpoint for updating all item fields
- Updates to the existing ItemsService (both frontend and backend) to support comprehensive item updates
- Routing configuration to enable navigation to the edit screen
- Integration with existing components (item-card, main) to provide edit access points

## Architecture

### Frontend Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                     Main Component                           │
│  - Displays item cards with edit button for owned items     │
└────────────────────┬────────────────────────────────────────┘
                     │
                     │ navigates to
                     ▼
┌─────────────────────────────────────────────────────────────┐
│                  Item Edit Component                         │
│  - Loads existing item data                                  │
│  - Displays form with all editable fields                    │
│  - Handles image upload                                      │
│  - Integrates VisibilitySelectorComponent                    │
│  - Validates and submits updates                             │
└────────────────────┬────────────────────────────────────────┘
                     │
                     │ uses
                     ▼
┌─────────────────────────────────────────────────────────────┐
│                   ItemsService                               │
│  - getItemById(id): Observable<SharedItem>                   │
│  - updateItem(id, updates): Observable<SharedItem>  [NEW]    │
│  - uploadItemImage(id, file): Observable<SharedItem>         │
└────────────────────┬────────────────────────────────────────┘
                     │
                     │ HTTP calls
                     ▼
┌─────────────────────────────────────────────────────────────┐
│                  Backend API                                 │
│  PUT /api/items/{id}                              [NEW]      │
│  POST /api/items/{id}/image                                  │
│  GET /api/items/{id}                                         │
└─────────────────────────────────────────────────────────────┘
```

### Backend Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                 ItemsController                              │
│  - UpdateItem(id, request): ActionResult<SharedItem> [NEW]   │
│  - Validates ownership                                       │
│  - Delegates to ItemsService                                 │
└────────────────────┬────────────────────────────────────────┘
                     │
                     │ calls
                     ▼
┌─────────────────────────────────────────────────────────────┐
│                  ItemsService                                │
│  - UpdateItemAsync(id, userId, updates) [NEW]                │
│  - Performs MongoDB update operation                         │
│  - Returns updated item                                      │
└────────────────────┬────────────────────────────────────────┘
                     │
                     │ updates
                     ▼
┌─────────────────────────────────────────────────────────────┐
│              MongoDB Items Collection                        │
│  - Stores SharedItem documents                               │
└─────────────────────────────────────────────────────────────┘
```

## Components and Interfaces

### Frontend Components

#### ItemEditComponent

**Location**: `ui/src/app/components/item-edit/`

**Purpose**: Provides a comprehensive form for editing all item properties

**Key Properties**:
```typescript
itemId: string | null
item: SharedItem | null
itemName: string
itemDescription: string
isAvailable: boolean
selectedImageFile: File | null
currentImageUrl: string | null
loops: Loop[]
selectedLoopIds: string[]
visibleToAllLoops: boolean
visibleToFutureLoops: boolean
loading: boolean
error: string
success: string
```

**Key Methods**:
```typescript
ngOnInit(): void
loadItemData(): void
loadLoops(): void
onFileSelected(event: Event): void
onVisibilitySelectionChange(selection): void
updateItem(): void
onCancel(): void
```

**Template Features**:
- Material Design form fields for name and description
- Checkbox/toggle for availability status
- Image preview with file upload button
- Integrated VisibilitySelectorComponent
- Save and Cancel buttons
- Loading spinner and error/success messages

**Styling**: Follows existing patterns from item-add component

#### Updates to ItemCardComponent

**Changes**: Add edit button that appears for item owners

**New Output**:
```typescript
@Output() editItem = new EventEmitter<string>()
```

#### Updates to MainComponent

**Changes**: Handle edit button click from ItemCardComponent

**New Method**:
```typescript
onEditItem(itemId: string): void {
  this.router.navigate(['/items', itemId, 'edit']);
}
```

### Frontend Services

#### ItemsService Updates

**New Method**:
```typescript
updateItem(itemId: string, updates: Partial<SharedItem>): Observable<SharedItem> {
  return this.http.put<SharedItem>(`${this.apiUrl}/${itemId}`, updates)
    .pipe(catchError(error => this.handleError(error)));
}
```

**Request Payload**:
```typescript
interface UpdateItemRequest {
  name: string;
  description: string;
  isAvailable: boolean;
  visibleToLoopIds: string[];
  visibleToAllLoops: boolean;
  visibleToFutureLoops: boolean;
}
```

### Backend Components

#### ItemsController Updates

**New Endpoint**:
```csharp
[HttpPut("{id}")]
public async Task<ActionResult<SharedItem>> UpdateItem(string id, [FromBody] UpdateItemRequest request)
```

**Request DTO**:
```csharp
public class UpdateItemRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsAvailable { get; set; }
    public List<string> VisibleToLoopIds { get; set; } = new();
    public bool VisibleToAllLoops { get; set; }
    public bool VisibleToFutureLoops { get; set; }
}
```

**Validation Logic**:
- Verify user is authenticated
- Verify item exists
- Verify user owns the item
- Validate name is not empty
- Validate visibility settings are consistent

#### IItemsService Interface Updates

**New Method Signature**:
```csharp
Task<SharedItem?> UpdateItemAsync(
    string itemId,
    string userId,
    string name,
    string description,
    bool isAvailable,
    List<string> visibleToLoopIds,
    bool visibleToAllLoops,
    bool visibleToFutureLoops
);
```

#### ItemsService Implementation

**New Method**:
```csharp
public async Task<SharedItem?> UpdateItemAsync(
    string itemId,
    string userId,
    string name,
    string description,
    bool isAvailable,
    List<string> visibleToLoopIds,
    bool visibleToAllLoops,
    bool visibleToFutureLoops)
{
    var filter = Builders<SharedItem>.Filter.And(
        Builders<SharedItem>.Filter.Eq(item => item.Id, itemId),
        Builders<SharedItem>.Filter.Eq(item => item.UserId, userId)
    );

    var update = Builders<SharedItem>.Update
        .Set(item => item.Name, name)
        .Set(item => item.Description, description)
        .Set(item => item.IsAvailable, isAvailable)
        .Set(item => item.VisibleToLoopIds, visibleToLoopIds)
        .Set(item => item.VisibleToAllLoops, visibleToAllLoops)
        .Set(item => item.VisibleToFutureLoops, visibleToFutureLoops)
        .Set(item => item.UpdatedAt, DateTime.UtcNow);

    var options = new FindOneAndUpdateOptions<SharedItem>
    {
        ReturnDocument = ReturnDocument.After
    };

    return await _itemsCollection.FindOneAndUpdateAsync(filter, update, options);
}
```

### Routing Configuration

**New Route**:
```typescript
{
  path: 'items/:id/edit',
  component: ItemEditComponent,
  canActivate: [AuthGuard]
}
```

## Data Models

### SharedItem (No Changes Required)

The existing SharedItem model already contains all necessary fields:
- `id`: string
- `name`: string
- `description`: string
- `userId`: string
- `isAvailable`: boolean
- `imageUrl`: string | null
- `visibleToLoopIds`: string[]
- `visibleToAllLoops`: boolean
- `visibleToFutureLoops`: boolean
- `createdAt`: DateTime
- `updatedAt`: DateTime

### UpdateItemRequest DTO (New)

Backend DTO for update requests:
```csharp
public class UpdateItemRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsAvailable { get; set; }
    public List<string> VisibleToLoopIds { get; set; } = new();
    public bool VisibleToAllLoops { get; set; }
    public bool VisibleToFutureLoops { get; set; }
}
```

## Error Handling

### Frontend Error Scenarios

1. **Item Not Found (404)**
   - Display: "Item not found"
   - Action: Redirect to main page after 2 seconds

2. **Unauthorized Access (403)**
   - Display: "You do not have permission to edit this item"
   - Action: Redirect to main page after 2 seconds

3. **Authentication Required (401)**
   - Handled by ItemsService error handler
   - Action: Logout user and redirect to login

4. **Validation Errors (400)**
   - Display specific validation message
   - Action: Keep user on form to correct errors

5. **Network/Server Errors (500)**
   - Display: "Failed to update item. Please try again."
   - Action: Keep user on form to retry

6. **Image Upload Errors**
   - Display specific error (file type, size, etc.)
   - Action: Allow user to select different file

### Backend Error Scenarios

1. **Missing User ID in Token**
   - Return: 401 Unauthorized
   - Message: "User ID not found in token"

2. **Item Not Found**
   - Return: 404 Not Found
   - Message: "Item with id {id} not found"

3. **Ownership Validation Failure**
   - Return: 403 Forbidden
   - Message: "You do not have permission to update this item"

4. **Empty Name Validation**
   - Return: 400 Bad Request
   - Message: "Item name is required"

5. **Database Update Failure**
   - Return: 500 Internal Server Error
   - Message: "Internal server error: {exception message}"

## Testing Strategy

### Frontend Unit Tests

**ItemEditComponent Tests** (`item-edit.component.spec.ts`):
- Should load item data on initialization
- Should pre-populate form fields with item data
- Should validate required name field
- Should handle file selection for image upload
- Should update visibility settings when selector changes
- Should call ItemsService.updateItem with correct parameters
- Should navigate to main page on successful update
- Should display error message on update failure
- Should navigate to main page on cancel
- Should handle 403 forbidden error appropriately
- Should handle 404 not found error appropriately

**ItemsService Tests** (update existing `items.service.spec.ts`):
- Should call PUT /api/items/{id} with correct payload
- Should handle 401 authentication errors
- Should handle 403 forbidden errors
- Should handle 404 not found errors
- Should handle network errors

### Backend Unit Tests

**ItemsController Tests** (update existing `ItemsControllerTests.cs`):
- Should return 401 when user is not authenticated
- Should return 404 when item does not exist
- Should return 403 when user does not own item
- Should return 400 when name is empty
- Should return 200 with updated item on success
- Should update all fields correctly
- Should set updatedAt timestamp

**ItemsService Tests** (update existing `ItemsServiceTests.cs`):
- Should update item when user owns it
- Should return null when item does not exist
- Should return null when user does not own item
- Should update all specified fields
- Should set updatedAt to current UTC time
- Should use correct MongoDB filter and update builders

### Integration Tests

**End-to-End Flow**:
1. User creates an item
2. User navigates to edit screen
3. User modifies all fields
4. User saves changes
5. Verify item is updated in database
6. Verify updated item displays correctly on main page

**Image Upload Flow**:
1. User navigates to edit screen
2. User selects new image
3. User saves changes
4. Verify item fields are updated
5. User uploads image separately (existing flow)
6. Verify image URL is updated

## Design Decisions and Rationales

### 1. Single Update Endpoint vs. Multiple Endpoints

**Decision**: Create a single PUT endpoint that updates all editable fields

**Rationale**:
- Simplifies frontend logic - one API call instead of multiple
- Reduces network overhead
- Provides atomic updates - all fields succeed or fail together
- Follows RESTful conventions for resource updates
- Image upload remains separate due to multipart/form-data requirements

### 2. Reuse VisibilitySelectorComponent

**Decision**: Integrate existing VisibilitySelectorComponent into edit form

**Rationale**:
- Maintains UI consistency across add and edit flows
- Reduces code duplication
- Leverages tested component
- Simplifies maintenance

### 3. Separate Image Upload

**Decision**: Keep image upload as a separate operation after main update

**Rationale**:
- Image upload requires multipart/form-data encoding
- Main update uses JSON encoding
- Existing image upload endpoint works well
- Allows users to update text fields without re-uploading image
- Simplifies error handling (image upload failure doesn't rollback other changes)

### 4. Ownership Validation in Backend

**Decision**: Validate item ownership in both controller and service layer

**Rationale**:
- Defense in depth security approach
- Controller validation provides early rejection
- Service validation ensures data integrity at database level
- Prevents unauthorized updates even if controller is bypassed

### 5. Form Pre-population

**Decision**: Load item data and pre-populate form on component initialization

**Rationale**:
- Provides better user experience
- Shows current values clearly
- Allows users to see what they're changing
- Reduces cognitive load

### 6. Navigation After Save

**Decision**: Navigate back to main page after successful update

**Rationale**:
- Matches existing pattern from item-add component
- Provides clear feedback that operation completed
- Returns user to context where they can see updated item
- Prevents accidental duplicate submissions

### 7. Availability Toggle

**Decision**: Use checkbox/toggle for availability status

**Rationale**:
- Binary state (available/unavailable) suits toggle UI
- Matches Material Design patterns
- Clear visual indication of current state
- Easy to understand and use
