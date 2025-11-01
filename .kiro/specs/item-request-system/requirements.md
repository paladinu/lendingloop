# Requirements Document

## Introduction

This document defines the requirements for an Item Request System that enables users to request items from other users within their loops. The system implements a request-approval workflow where items remain marked as available until the owner explicitly approves a request. This ensures owners maintain control over their items while facilitating borrowing within trusted communities.

## Glossary

- **Item Request System**: The software system that manages the creation, approval, rejection, and cancellation of item borrow requests
- **Requester**: A user who submits a request to borrow an item from another user
- **Owner**: A user who owns a shared item and has the authority to approve or reject borrow requests
- **Item Request**: A formal request from a Requester to borrow a specific SharedItem from an Owner
- **SharedItem**: An item that a user has made available for lending within their loops
- **Request Status**: The current state of an Item Request (Pending, Approved, Rejected, Cancelled, Completed)
- **Pending Request**: An Item Request that has been submitted but not yet approved or rejected by the Owner
- **Loop**: A trusted group of users who can view and request each other's shared items
- **API**: The backend REST API that handles Item Request operations
- **UI**: The Angular frontend application that displays and manages Item Requests

## Requirements

### Requirement 1: Create Item Request

**User Story:** As a Requester, I want to request an item from another user, so that I can borrow items I need from my community.

#### Acceptance Criteria

1. WHEN a Requester submits a request for a SharedItem, THE Item Request System SHALL create a new Item Request with status "Pending"
2. WHEN a Requester submits a request, THE Item Request System SHALL record the Requester's user ID, Owner's user ID, SharedItem ID, and request timestamp
3. WHEN a Requester submits a request, THE Item Request System SHALL keep the SharedItem's isAvailable property set to true
4. IF a Requester attempts to request their own SharedItem, THEN THE Item Request System SHALL reject the request with an error message
5. IF a Requester attempts to request a SharedItem they do not have visibility to, THEN THE Item Request System SHALL reject the request with an authorization error

### Requirement 2: View Item Requests

**User Story:** As an Owner, I want to view all pending requests for my items, so that I can decide which requests to approve.

#### Acceptance Criteria

1. WHEN an Owner requests their pending Item Requests, THE Item Request System SHALL return all Item Requests for their SharedItems with status "Pending"
2. WHEN an Owner views an Item Request, THE Item Request System SHALL display the Requester's name, SharedItem name, and request timestamp
3. THE Item Request System SHALL sort Item Requests by request timestamp in descending order
4. WHEN a Requester views their Item Requests, THE Item Request System SHALL return all Item Requests they have created regardless of status

### Requirement 3: Approve Item Request

**User Story:** As an Owner, I want to approve a request for my item, so that I can lend it to the requester.

#### Acceptance Criteria

1. WHEN an Owner approves a Pending Item Request, THE Item Request System SHALL update the Item Request status to "Approved"
2. WHEN an Owner approves an Item Request, THE Item Request System SHALL set the SharedItem's isAvailable property to false
3. WHEN an Owner approves an Item Request, THE Item Request System SHALL record the approval timestamp
4. IF an Owner attempts to approve an Item Request for a SharedItem they do not own, THEN THE Item Request System SHALL reject the action with an authorization error
5. IF an Owner attempts to approve an Item Request that is not in "Pending" status, THEN THE Item Request System SHALL reject the action with a validation error

### Requirement 4: Reject Item Request

**User Story:** As an Owner, I want to reject a request for my item, so that I can decline requests I cannot fulfill.

#### Acceptance Criteria

1. WHEN an Owner rejects a Pending Item Request, THE Item Request System SHALL update the Item Request status to "Rejected"
2. WHEN an Owner rejects an Item Request, THE Item Request System SHALL keep the SharedItem's isAvailable property set to true
3. WHEN an Owner rejects an Item Request, THE Item Request System SHALL record the rejection timestamp
4. IF an Owner attempts to reject an Item Request for a SharedItem they do not own, THEN THE Item Request System SHALL reject the action with an authorization error
5. IF an Owner attempts to reject an Item Request that is not in "Pending" status, THEN THE Item Request System SHALL reject the action with a validation error

### Requirement 5: Cancel Item Request

**User Story:** As a Requester, I want to cancel my pending request, so that I can withdraw requests I no longer need.

#### Acceptance Criteria

1. WHEN a Requester cancels a Pending Item Request, THE Item Request System SHALL update the Item Request status to "Cancelled"
2. WHEN a Requester cancels an Item Request, THE Item Request System SHALL keep the SharedItem's isAvailable property unchanged
3. WHEN a Requester cancels an Item Request, THE Item Request System SHALL record the cancellation timestamp
4. IF a Requester attempts to cancel an Item Request they did not create, THEN THE Item Request System SHALL reject the action with an authorization error
5. IF a Requester attempts to cancel an Item Request that is not in "Pending" status, THEN THE Item Request System SHALL reject the action with a validation error

### Requirement 6: Complete Item Request

**User Story:** As an Owner, I want to mark an approved request as completed when the item is returned, so that the item becomes available again.

#### Acceptance Criteria

1. WHEN an Owner marks an Approved Item Request as completed, THE Item Request System SHALL update the Item Request status to "Completed"
2. WHEN an Owner completes an Item Request, THE Item Request System SHALL set the SharedItem's isAvailable property to true
3. WHEN an Owner completes an Item Request, THE Item Request System SHALL record the completion timestamp
4. IF an Owner attempts to complete an Item Request for a SharedItem they do not own, THEN THE Item Request System SHALL reject the action with an authorization error
5. IF an Owner attempts to complete an Item Request that is not in "Approved" status, THEN THE Item Request System SHALL reject the action with a validation error

### Requirement 7: Prevent Multiple Active Requests

**User Story:** As an Owner, I want to ensure only one request can be approved at a time for each item, so that I don't accidentally lend the same item to multiple people.

#### Acceptance Criteria

1. WHEN an Owner approves an Item Request for a SharedItem, THE Item Request System SHALL verify no other Approved Item Request exists for that SharedItem
2. IF an Owner attempts to approve an Item Request when another Approved Item Request exists for the same SharedItem, THEN THE Item Request System SHALL reject the action with a validation error
3. WHEN a Requester submits a request for a SharedItem with an existing Approved Item Request, THE Item Request System SHALL allow the request creation but mark it as Pending

### Requirement 8: Display Request Status on Items

**User Story:** As a Requester, I want to see if I have a pending request for an item, so that I don't accidentally submit duplicate requests.

#### Acceptance Criteria

1. WHEN a Requester views a SharedItem, THE Item Request System SHALL indicate whether the Requester has a Pending Item Request for that SharedItem
2. WHEN a Requester views a SharedItem, THE Item Request System SHALL indicate whether the Requester has an Approved Item Request for that SharedItem
3. WHEN a Requester views a SharedItem with a Pending or Approved Item Request, THE UI SHALL disable the request button
