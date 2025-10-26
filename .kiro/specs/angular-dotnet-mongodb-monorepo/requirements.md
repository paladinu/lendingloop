# Requirements Document

## Introduction

This document specifies the requirements for a monorepo containing an Angular UI and a C# .NET Core API, configured for seamless local development against a locally installed MongoDB instance. The system enables users to manage shared items through a simple web interface with full-stack CRUD operations.

## Glossary

- **Monorepo**: A single Git repository containing multiple related projects (UI and API).
- **Angular UI**: The frontend web application built with Angular framework.
- **API**: The backend C# .NET 8 Web API application.
- **MongoDB**: The NoSQL database system running locally on the host machine.
- **SharedItem**: The core data entity representing items that can be shared between users.
- **CORS**: Cross-Origin Resource Sharing, a security mechanism for web browsers.
- **Proxy Configuration**: Angular development server configuration that forwards API requests to the backend.

## Requirements

### Requirement 1

**User Story:** As a developer, I want a monorepo structure with separate UI and API directories, so that I can manage both frontend and backend code in a single repository.

#### Acceptance Criteria

1. THE Monorepo SHALL contain a top-level directory named "ui" for the Angular application.
2. THE Monorepo SHALL contain a top-level directory named "api" for the C# .NET 8 Web API project.
3. THE Monorepo SHALL contain a root-level README.md file with local development instructions.
4. THE Monorepo SHALL contain a .gitignore file that excludes node_modules, bin, obj, and other build artifacts.
5. THE Monorepo SHALL NOT contain Docker-related files such as docker-compose.yml or a docker directory.

### Requirement 2

**User Story:** As a developer, I want the Angular UI configured to run on port 4200 with API proxy support, so that I can develop the frontend without CORS issues.

#### Acceptance Criteria

1. THE Angular UI SHALL be configured to run on host port 4200 using the ng serve command.
2. THE Angular UI SHALL contain a proxy.conf.json file that forwards requests from /api/* to http://localhost:8080.
3. THE Angular UI SHALL use the latest stable version of Angular framework.
4. WHEN a developer runs "ng serve" from the ui directory, THE Angular UI SHALL start the development server successfully.

### Requirement 3

**User Story:** As a developer, I want the .NET API configured to run on port 8080 with CORS enabled, so that the Angular UI can communicate with the backend.

#### Acceptance Criteria

1. THE API SHALL be built using C# .NET 8 Web API framework.
2. THE API SHALL be configured to run on host port 8080 using the dotnet run command.
3. THE API SHALL enable CORS to allow requests from http://localhost:4200.
4. WHEN a developer runs "dotnet run" from the api directory, THE API SHALL start the web server on port 8080.

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

1. THE API SHALL define a SharedItem class with an Id property of type string representing the MongoDB ObjectId.
2. THE API SHALL define a SharedItem class with a Name property of type string.
3. THE API SHALL define a SharedItem class with an OwnerId property of type string.
4. THE API SHALL define a SharedItem class with an IsAvailable property of type boolean.
5. THE API SHALL configure MongoDB serialization for the SharedItem class.

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
5. THE README.md SHALL specify that the UI runs on http://localhost:4200 and the API runs on http://localhost:8080.
