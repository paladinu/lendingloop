# Implementation Plan

- [x] 1. Set up monorepo structure and root-level files





  - Create .gitignore file with exclusions for node_modules, bin, obj, dist, and IDE files
  - Create root README.md with prerequisites, technology stack, and local development instructions
  - _Requirements: 1.3, 1.4, 1.5, 8.1, 8.2, 8.3, 8.4, 8.5_

- [ ] 2. Initialize .NET API project structure
  - Create api directory and initialize .NET 8 Web API project using dotnet new webapi
  - Add MongoDB.Driver NuGet package to the project
  - Configure Api.csproj with necessary package references
  - _Requirements: 3.1, 4.2_

- [ ] 3. Implement SharedItem data model
  - Create Models/SharedItem.cs with Id, Name, OwnerId, and IsAvailable properties
  - Add MongoDB BSON attributes for proper serialization
  - _Requirements: 5.1, 5.2, 5.3, 5.4, 5.5_

- [ ] 4. Configure API settings and MongoDB connection
  - Create appsettings.Development.json with MongoDB connection string (mongodb://localhost:27017)
  - Update appsettings.json with MongoDB configuration structure
  - Configure MongoDB client and database registration in Program.cs
  - _Requirements: 4.1, 4.2, 4.3, 4.4_

- [ ] 5. Implement ItemsService for data access
  - Create Services/IItemsService.cs interface with GetAllItemsAsync and CreateItemAsync methods
  - Create Services/ItemsService.cs implementing the interface with MongoDB operations
  - Register ItemsService in Program.cs dependency injection container
  - _Requirements: 6.1, 6.2, 7.1, 7.2_

- [ ] 6. Implement ItemsController with CRUD endpoints
  - Create Controllers/ItemsController.cs with GET /api/items endpoint
  - Add POST /api/items endpoint to ItemsController
  - Inject IItemsService into the controller
  - _Requirements: 6.1, 6.2, 7.1, 7.2_

- [ ] 7. Configure CORS and API port settings
  - Add CORS policy in Program.cs to allow http://localhost:4200
  - Configure Kestrel to listen on port 8080 in Program.cs or launchSettings.json
  - _Requirements: 3.2, 3.3, 3.4_

- [ ] 8. Initialize Angular UI project structure
  - Create ui directory and initialize Angular project using ng new
  - Configure angular.json to use standalone components
  - Set up project with HttpClient and routing if needed
  - _Requirements: 2.1, 2.3_

- [ ] 9. Create proxy configuration for Angular
  - Create proxy.conf.json in ui directory to forward /api/* to http://localhost:8080
  - Update angular.json to reference proxy.conf.json in serve configuration
  - _Requirements: 2.2, 2.4_

- [ ] 10. Implement SharedItem TypeScript interface and ItemsService
  - Create shared-item.interface.ts with SharedItem interface matching the C# model
  - Create services/items.service.ts with getItems() and createItem() methods using HttpClient
  - _Requirements: 6.3, 7.4_

- [ ] 11. Implement AppComponent with items list and form
  - Update app.component.ts to inject ItemsService and load items on initialization
  - Create app.component.html template with items list display and add item form
  - Add basic styling in app.component.css for readable UI
  - Implement loadItems() method to fetch items from API
  - Implement addItem() method to create new items and refresh the list
  - _Requirements: 6.3, 6.4, 7.3, 7.4, 7.5_

- [ ] 12. Verify and test the complete application
  - Start MongoDB locally and verify it's running on port 27017
  - Run the API using dotnet run and verify it starts on port 8080
  - Run the UI using ng serve and verify it starts on port 4200
  - Test GET /api/items endpoint returns data correctly
  - Test POST /api/items endpoint creates items successfully
  - Verify items display in the UI and new items can be added through the form
  - _Requirements: All requirements_
