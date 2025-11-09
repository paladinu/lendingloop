# Requirements Document

## Introduction

This document specifies the technical requirements for the monorepo structure, development environment configuration, and initial CRUD operations for the LendingLoop platform.

## Glossary

- **Monorepo**: A single Git repository containing multiple related projects (UI and API)
- **Angular UI**: The Angular frontend application running on local-www.lendingloop.com
- **API**: The C# .NET 8 Web API backend running on local-api.lendingloop.com
- **CORS (Cross-Origin Resource Sharing)**: A security mechanism that allows the Angular UI to make requests to the API running on a different host
- **SharedItem**: A data model representing an item that users can share within the LendingLoop platform

## Requirements

### Requirement 1

**User Story:** As a developer, I want a monorepo structure with separate UI and API directories, so that I can manage both frontend and backend code in a single repository.

#### Acceptance Criteria

1. THE Monorepo SHALL contain a top-level directory named "ui" for the Angular application.
2. THE Monorepo SHALL contain a top-level directory named "api" for the C# .NET 8 Web API project.
3. THE Monorepo SHALL contain a root-level README.md file with local development instructions.
4. THE Monorepo SHALL contain a .gitignore file that excludes node_modules, bin, obj, and other build artifacts.
5. THE Monorepo SHALL exclude Docker-related files such as docker-compose.yml and docker directories from the repository structure.

### Requirement 2

**User Story:** As a developer, I want the Angular UI configured to run on local-www.lendingloop.com, so that I can develop the frontend with a production-like environment.

#### Acceptance Criteria

1. THE Angular UI SHALL be configured to run on local-www.lendingloop.com using the ng serve command.
2. THE Angular UI SHALL use a self-signed certificate to run on HTTPS.
3. THE Angular UI SHALL use the latest stable version of Angular framework.
4. THE Angular UI SHALL make direct HTTP requests to https://local-api.lendingloop.com without using a proxy configuration.
5. WHEN a developer runs "ng serve" from the ui directory, THE Angular UI SHALL start the development server successfully.

### Requirement 3

**User Story:** As a developer, I want the .NET API configured to run on local-api.lendingloop.com with CORS enabled, so that the Angular UI can communicate with the backend.

#### Acceptance Criteria

1. THE API SHALL be built using C# .NET 8 Web API framework.
2. THE API SHALL be configured to run on host local-api.lendingloop.com using the dotnet run command.
3. THE API SHALL enable CORS to allow requests from https://local-www.lendingloop.com.
4. WHEN a developer runs "dotnet run" from the api directory, THE API SHALL start the web server on local-api.lendingloop.com.
5. THE API SHALL use a self-signed certificate to run on HTTPS.

### Requirement 4

**User Story:** As a developer, I want the API configured to connect to a local MongoDB instance, so that I can persist and retrieve data during development.

#### Acceptance Criteria

1. THE API SHALL use the MongoDB C# driver for database operations.
2. THE API SHALL contain an appsettings.Development.json file with a connection string pointing to mongodb://localhost:27017.
3. THE API SHALL establish a connection to the MongoDB instance at localhost:27017.
4. WHEN the API starts, THE API SHALL successfully connect to the local MongoDB database.

### Requirement 5

**User Story:** As a developer, I want a SharedItem data model with basic properties, so that I can represent items that users can share.

#### Acceptance Criteria

1. THE API SHALL define a SharedItem class with an Id property of type string representing a MongoDB ObjectId.
2. THE API SHALL define a SharedItem class with a Name property of type string.
3. THE API SHALL define a SharedItem class with an OwnerId property of type string.
4. THE API SHALL define a SharedItem class with an IsAvailable property of type boolean.
5. THE API SHALL configure MongoDB serialization attributes for the SharedItem class to enable proper database storage and retrieval.

### Requirement 6

**User Story:** As a user, I want to retrieve a list of all shared items, so that I can see what items are available.

#### Acceptance Criteria

1. THE API SHALL expose a GET endpoint at /api/items that returns all SharedItem objects.
2. WHEN a GET request is made to /api/items, THE API SHALL return a JSON array of SharedItem objects with HTTP status 200.
3. THE Angular UI SHALL fetch the list of items from the GET /api/items endpoint on component initialization.
4. THE Angular UI SHALL display the list of items showing the Name and IsAvailable properties.

### Requirement 7

**User Story:** As a user, I want to add a new shared item, so that I can contribute items to the shared collection.

#### Acceptance Criteria

1. THE API SHALL expose a POST endpoint at /api/items that accepts a SharedItem object in the request body.
2. WHEN a POST request is made to /api/items with valid data, THE API SHALL create a new SharedItem in MongoDB and return HTTP status 201.
3. THE Angular UI SHALL provide a form with an input field for the item name and a submit button.
4. WHEN a user submits the form, THE Angular UI SHALL send a POST request to /api/items with the new item data.
5. WHEN the POST request succeeds, THE Angular UI SHALL refresh the list of items to display the newly added item.

### Requirement 8

**User Story:** As a developer, I want clear setup instructions in the README, so that I can quickly get the application running locally.

#### Acceptance Criteria

1. THE README.md SHALL document the prerequisite that MongoDB must be installed and running on localhost:27017.
2. THE README.md SHALL provide instructions to start the API using "dotnet run" from the ./api directory.
3. THE README.md SHALL provide instructions to start the UI using "ng serve" from the ./ui directory.
4. THE README.md SHALL document the technology stack including Angular, .NET 8, and MongoDB.
5. THE README.md SHALL specify that the UI runs on https://local-www.lendingloop.com and the API runs on https://local-api.lendingloop.com.
6. THE README.md SHALL specify that the two hostnames local-www.lendingloop.com and local-api.lendingloop.com need to be configured via HOSTS file or other name resolution and provide a sample powershell script to add them.

### Requirement 9

**User Story:** As a user, I want to be able to optionally upload an image of my item and have it displayed alongside the item, so that I can visually identify items in the shared collection.

#### Acceptance Criteria

1. THE SharedItem class SHALL include an ImageUrl property of type string to store the location of the uploaded image.
2. THE API SHALL expose a POST endpoint at /api/items/{id}/image that accepts an image file upload for a specific SharedItem.
3. WHEN an image is uploaded, THE API SHALL store the image file and update the SharedItem ImageUrl property with the file location.
4. THE Angular UI SHALL provide an optional file input control in the add item form to allow image selection.
5. WHEN a SharedItem has an associated ImageUrl, THE Angular UI SHALL display the image alongside the item name and availability status in the items list.