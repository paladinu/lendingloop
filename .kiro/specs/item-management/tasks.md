# Implementation Plan

- [x] 1. Create backend API endpoint for item updates


  - Add UpdateItemRequest DTO class in api/DTOs folder
  - Implement UpdateItem endpoint in ItemsController with ownership validation
  - Add UpdateItemAsync method to IItemsService interface
  - Implement UpdateItemAsync in ItemsService with MongoDB update logic
  - Ensure updatedAt timestamp is set to current UTC time
  - _Requirements: 1.3, 2.3, 2.4, 3.3, 5.5, 6.2, 6.3_

- [x] 1.1 Write unit tests for backend update functionality


  - Create tests in ItemsControllerTests.cs for UpdateItem endpoint (authentication, ownership, validation)
  - Create tests in ItemsServiceTests.cs for UpdateItemAsync method (ownership checks, field updates, timestamp)
  - _Requirements: 1.3, 2.3, 2.4, 3.3, 5.5, 6.2, 6.3_

- [x] 2. Update frontend ItemsService with update method


  - Add updateItem method to ItemsService that calls PUT /api/items/{id}
  - Ensure proper error handling for 401, 403, 404, and 500 responses
  - _Requirements: 6.2, 6.3_

- [x] 2.1 Write unit tests for ItemsService update method


  - Add tests to items.service.spec.ts for updateItem method
  - Test error handling scenarios (401, 403, 404, network errors)
  - _Requirements: 6.2, 6.3_

- [x] 3. Create ItemEditComponent with form structure


  - Generate item-edit component in ui/src/app/components
  - Set up component imports (CommonModule, FormsModule, Material modules, ToolbarComponent)
  - Create component properties for form fields (itemName, itemDescription, isAvailable, etc.)
  - Implement ngOnInit to extract itemId from route parameters
  - Add loadItemData method that calls ItemsService.getItemById
  - Add loadLoops method that calls LoopService.getUserLoops
  - Pre-populate form fields with loaded item data
  - _Requirements: 1.1, 1.2, 1.4, 2.1, 3.1, 4.1, 5.1_

- [x] 4. Build ItemEditComponent template with form fields


  - Create HTML template with Material form fields for name and description
  - Add checkbox or toggle for availability status
  - Add image preview section with current image display
  - Add file input for image upload with change handler
  - Integrate VisibilitySelectorComponent for loop visibility settings
  - Add Save and Cancel buttons
  - Add loading spinner, error message, and success message displays
  - _Requirements: 2.1, 3.1, 4.1, 5.1, 6.1, 7.1_

- [x] 5. Implement form validation and submission logic


  - Add onFileSelected method to handle image file selection
  - Add onVisibilitySelectionChange method to update visibility state
  - Implement updateItem method that validates name is not empty
  - Call ItemsService.updateItem with form data
  - Handle successful update with success message and navigation to main page
  - Handle update errors with appropriate error messages
  - Implement onCancel method that navigates back without saving
  - _Requirements: 2.2, 2.3, 2.4, 2.5, 3.2, 3.3, 4.2, 4.3, 4.4, 4.5, 5.2, 5.3, 5.4, 5.5, 6.2, 6.3, 6.4, 6.5, 7.2, 7.3, 7.4_

- [x] 6. Add image upload functionality to edit flow


  - After successful item update, check if new image file was selected
  - If image selected, call ItemsService.uploadItemImage
  - Handle image upload success and errors appropriately
  - Display appropriate messages for image upload failures
  - _Requirements: 4.2, 4.3, 4.4, 4.5_

- [x] 7. Add routing configuration for edit component


  - Add route for 'items/:id/edit' in app routing configuration
  - Apply AuthGuard to protect the route
  - Ensure route is properly configured for navigation
  - _Requirements: 1.2_

- [x] 8. Add edit button to ItemCardComponent


  - Add edit button to item card template that shows only for item owners
  - Create editItem output event emitter
  - Emit itemId when edit button is clicked
  - Style edit button consistently with existing UI
  - _Requirements: 1.1_

- [x] 9. Wire edit navigation in MainComponent


  - Add onEditItem method to MainComponent
  - Handle editItem event from ItemCardComponent
  - Navigate to edit route with item ID
  - _Requirements: 1.1, 1.2_

- [x] 10. Write frontend component tests


  - Create item-edit.component.spec.ts with tests for component initialization, form validation, update submission, error handling, and navigation
  - Update item-card.component.spec.ts to test edit button visibility and click behavior
  - Update main.component.spec.ts to test edit navigation
  - _Requirements: All requirements_

- [x] 11. Add component styling



  - Create item-edit.component.css with styles matching item-add component
  - Ensure responsive design for mobile and desktop
  - Style form fields, buttons, and messages consistently
  - _Requirements: 2.1, 3.1, 4.1, 5.1, 6.1, 7.1_
