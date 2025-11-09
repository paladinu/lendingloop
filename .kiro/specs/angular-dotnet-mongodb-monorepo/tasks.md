# Implementation Plan

## Status: 🔄 IN PROGRESS

All initial monorepo setup tasks are complete. Currently working on configuration updates to support dual-host deployment (eliminating proxy configuration).

---

## New Tasks - Custom Domain HTTPS Configuration

- [x] 18. Generate self-signed SSL certificates for local development





  - Create a certs directory in the project root
  - Generate self-signed certificate for local-www.lendingloop.com
  - Generate self-signed certificate for local-api.lendingloop.com
  - Add certs directory to .gitignore to prevent committing certificates
  - Document certificate generation commands in README
  - _Requirements: 3.2, 3.3_

- [x] 19. Configure Windows hosts file for custom domains





  - Add entry for 127.0.0.1 local-www.lendingloop.com
  - Add entry for 127.0.0.1 local-api.lendingloop.com
  - Document hosts file location (C:\Windows\System32\drivers\etc\hosts)
  - Document that administrator privileges are required to edit hosts file
  - _Requirements: 2.2, 3.2_

- [x] 20. Configure .NET API for HTTPS and custom domain





  - Update api/Properties/launchSettings.json to use https://local-api.lendingloop.com
  - Configure Kestrel in Program.cs to use the SSL certificate
  - Update CORS policy in Program.cs to allow https://local-www.lendingloop.com
  - Update appsettings.Development.json with new base URL for email links
  - _Requirements: 3.2, 3.3, 3.4_

- [x] 21. Configure Angular for HTTPS and custom domain





  - Update angular.json serve configuration to use SSL certificate
  - Configure serve options for host: local-www.lendingloop.com
  - Configure serve options for ssl: true with cert and key paths
  - Add serve configuration for custom domain in angular.json
  - _Requirements: 2.2, 2.3_

- [x] 22. Create Angular environment configuration files





  - Create ui/src/environments/environment.ts with production API URL configuration
  - Create ui/src/environments/environment.development.ts with API URL (https://local-api.lendingloop.com)
  - Add environment configuration to angular.json for build replacements
  - _Requirements: 2.2, 3.2_

- [x] 23. Update Angular services to use environment configuration






  - Update auth.service.ts to use environment.apiUrl instead of hardcoded URL
  - Update items.service.ts to use environment.apiUrl instead of hardcoded URL
  - Update item-request.service.ts to use environment.apiUrl instead of hardcoded URL
  - Update notification.service.ts to use environment.apiUrl instead of hardcoded URL
  - Update user.service.ts to use environment.apiUrl instead of hardcoded URL
  - Update loop.service.ts to use environment.apiUrl instead of relative path
  - _Requirements: 2.2, 6.3, 7.4_

- [x] 24. Remove proxy configuration from Angular





  - Remove proxy.conf.json file from ui directory
  - Remove proxyConfig reference from angular.json serve configuration
  - _Requirements: 2.2_

- [x] 25. Update README.md with custom domain HTTPS setup





  - Add prerequisites section for SSL certificate generation (OpenSSL or PowerShell)
  - Document step-by-step SSL certificate generation process
  - Document hosts file configuration with administrator instructions
  - Update development setup to use https://local-www.lendingloop.com and https://local-api.lendingloop.com
  - Add browser security exception instructions for self-signed certificates
  - Add CORS configuration explanation
  - Add troubleshooting section for certificate and DNS issues
  - _Requirements: 8.1, 8.2, 8.3_

- [x] 26. Update project structure steering documentation





  - Update .kiro/steering/project-structure.md to reflect custom domain configuration
  - Remove proxy-related instructions
  - Add guidance on using environment files for API URLs
  - Document the custom domain URLs (https://local-www.lendingloop.com and https://local-api.lendingloop.com)
  - Add notes about SSL certificate requirements
  - _Requirements: 8.1_

---

## Completed Tasks

- [x] 1. Set up monorepo structure and root-level files
  - Create .gitignore file with exclusions for node_modules, bin, obj, dist, and IDE files
  - Create root README.md with prerequisites, technology stack, and local development instructions
  - _Requirements: 1.3, 1.4, 1.5, 8.1, 8.2, 8.3, 8.4, 8.5_

- [x] 2. Initialize .NET API project structure
  - Create api directory and initialize .NET 8 Web API project using dotnet new webapi
  - Add MongoDB.Driver NuGet package to the project
  - Configure Api.csproj with necessary package references
  - _Requirements: 3.1, 4.2_

- [x] 3. Implement SharedItem data model
  - Create Models/SharedItem.cs with Id, Name, OwnerId, and IsAvailable properties
  - Add MongoDB BSON attributes for proper serialization
  - _Requirements: 5.1, 5.2, 5.3, 5.4, 5.5_

- [x] 4. Configure API settings and MongoDB connection
  - Create appsettings.Development.json with MongoDB connection string (mongodb://localhost:27017)
  - Update appsettings.json with MongoDB configuration structure
  - Configure MongoDB client and database registration in Program.cs
  - _Requirements: 4.1, 4.2, 4.3, 4.4_

- [x] 5. Implement ItemsService for data access
  - Create Services/IItemsService.cs interface with GetAllItemsAsync and CreateItemAsync methods
  - Create Services/ItemsService.cs implementing the interface with MongoDB operations
  - Register ItemsService in Program.cs dependency injection container
  - _Requirements: 6.1, 6.2, 7.1, 7.2_

- [x] 6. Implement ItemsController with CRUD endpoints
  - Create Controllers/ItemsController.cs with GET /api/items endpoint
  - Add POST /api/items endpoint to ItemsController
  - Inject IItemsService into the controller
  - _Requirements: 6.1, 6.2, 7.1, 7.2_

- [x] 7. Configure CORS and API port settings
  - Add CORS policy in Program.cs to allow http://localhost:4200
  - Configure Kestrel to listen on port 8080 in Program.cs or launchSettings.json
  - _Requirements: 3.2, 3.3, 3.4_

- [x] 8. Initialize Angular UI project structure
  - Create ui directory and initialize Angular project using ng new
  - Configure angular.json to use standalone components
  - Set up project with HttpClient and routing if needed
  - _Requirements: 2.1, 2.3_

- [x] 9. Create proxy configuration for Angular
  - Create proxy.conf.json in ui directory to forward /api/* to http://localhost:8080
  - Update angular.json to reference proxy.conf.json in serve configuration
  - _Requirements: 2.2, 2.4_

- [x] 10. Implement SharedItem TypeScript interface and ItemsService
  - Create shared-item.interface.ts with SharedItem interface matching the C# model
  - Create services/items.service.ts with getItems() and createItem() methods using HttpClient
  - _Requirements: 6.3, 7.4_

- [x] 11. Implement AppComponent with items list and form
  - Update app.component.ts to inject ItemsService and load items on initialization
  - Create app.component.html template with items list display and add item form
  - Add basic styling in app.component.css for readable UI
  - Implement loadItems() method to fetch items from API
  - Implement addItem() method to create new items and refresh the list
  - _Requirements: 6.3, 6.4, 7.3, 7.4, 7.5_

- [x] 12. Verify and test the complete application
  - Start MongoDB locally and verify it's running on port 27017
  - Run the API using dotnet run and verify it starts on port 8080
  - Run the UI using ng serve and verify it starts on port 4200
  - Test GET /api/items endpoint returns data correctly
  - Test POST /api/items endpoint creates items successfully
  - Verify items display in the UI and new items can be added through the form
  - _Requirements: All requirements_

- [x] 13. Integrate Angular Material for UI components
  - Add @angular/material and @angular/cdk packages to ui/package.json
  - Configure Angular Material theme in styles.css
  - Update app.config.ts to include provideAnimations for Material animations
  - Replace custom form inputs with Material form fields (mat-form-field, mat-input)
  - Replace custom buttons with Material buttons (mat-button, mat-raised-button)
  - Replace custom item cards with Material cards (mat-card)
  - Update app.component.ts to import necessary Material modules
  - Remove or minimize custom CSS in favor of Material Design components
  - _Requirements: 6.3, 6.4, 7.3_

- [x] 14. Add ImageUrl property to SharedItem model
  - Update Models/SharedItem.cs to include ImageUrl property with BsonElement attribute
  - Update ui/src/app/models/shared-item.interface.ts to include optional imageUrl property
  - _Requirements: 9.1_

- [x] 15. Implement image upload endpoint in API
  - Add POST /api/items/{id}/image endpoint to ItemsController
  - Implement file validation logic (file type and size checks)
  - Create file storage logic to save uploaded images to the uploads/images directory
  - Update ItemsService to include UpdateItemImageAsync method
  - Configure static file serving in Program.cs to serve uploaded images
  - Add FileStorage configuration section to appsettings.Development.json
  - _Requirements: 9.2, 9.3_

- [x] 16. Implement image upload functionality in Angular UI
  - Add file input control to the add item form in app.component.html
  - Implement onFileSelected method in app.component.ts to handle file selection
  - Update addItem method to accept optional image file parameter
  - Add uploadItemImage method to items.service.ts that sends multipart form data
  - Update the item creation flow to upload image after item is created
  - _Requirements: 9.4_

- [x] 17. Display item images in the UI
  - Update app.component.html to conditionally display images when ImageUrl is present
  - Add image styling using Material card components for proper layout
  - Handle missing images gracefully with placeholder or no image display
  - _Requirements: 9.5_

---

## Evolution Beyond Original Spec

The application has been extended with additional features implemented in separate specs:

- **Authentication System**: User registration, login, email verification, JWT tokens
- **Loop Management**: Create loops, invite members, manage loop settings, archive loops
- **Item Visibility Controls**: Configure which loops can see each item
- **Item Request System**: Request to borrow items, owner approval workflow
- **Notification System**: Real-time notifications for requests and loop activities
- **Advanced UI**: Routing, guards, interceptors, comprehensive component library

For ongoing development, refer to the specific feature specs in `.kiro/specs/`.
