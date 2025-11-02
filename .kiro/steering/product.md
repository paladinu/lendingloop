# Product Overview: LendingLoop

## Purpose

LendingLoop is a community-based tool lending platform that enables users to share items within trusted groups called "loops". The application facilitates borrowing and lending of items among community members, reducing waste and fostering collaborative consumption.

## Target Users

- **Primary Users**: Community members who want to share tools, equipment, and other items with trusted neighbors, friends, or community groups
- **Use Cases**:
  - Neighborhood tool sharing cooperatives
  - Community organizations managing shared resources
  - Friend groups coordinating item lending
  - Families managing shared items across households

## Core Value Proposition

LendingLoop solves the problem of underutilized items by creating a trusted sharing economy within defined communities. Users can:
- Access items they need without purchasing them
- Monetize or share idle items they own
- Build stronger community connections through resource sharing
- Reduce environmental impact through collaborative consumption

## Key Features

### 1. User Authentication & Security
- Secure email-based registration with verification
- Password-protected accounts with enforced security policies
- Session management across the application
- Persistent authentication state

### 2. Loop Management (Sharing Groups)
- Create custom loops for different communities or groups
- Invite members via email or from existing loops
- Manage multiple loops simultaneously
- View loop membership and activity

### 3. Item Management
- Add items with name, description, and optional images
- Edit item details after creation
- Control item availability status
- Upload and display item images
- Mark items as available or unavailable

### 4. Item Visibility Control
- Configure which loops can see each item
- Set items visible to all current loops
- Automatically include items in future loops
- Update visibility settings at any time
- Granular control over item sharing

### 5. Item Request System
- Request to borrow items from other loop members
- Owner approval workflow for all requests
- Track request status (Pending, Approved, Rejected, Cancelled, Completed)
- Items remain available until owner approves a request
- Complete requests when items are returned
- Prevent duplicate active requests per item

### 6. Loop Landing Pages
- Browse all items available within a specific loop
- Search and filter items by title, description, and tags
- View item details including owner and availability
- See items from all loop members
- Real-time search functionality

## Business Objectives

### Primary Goals
1. **Community Building**: Foster trust and collaboration within defined user groups
2. **Resource Optimization**: Maximize utilization of underused items
3. **User Engagement**: Encourage active participation in sharing economy
4. **Platform Growth**: Expand through organic loop creation and invitations

### Success Metrics
- Number of active loops
- Items shared per user
- Request approval rate
- User retention within loops
- Item utilization frequency

## Technical Architecture

### Technology Stack
- **Frontend**: Angular (latest stable) - Modern, responsive web interface
- **Backend**: .NET 8 Web API - Robust, scalable REST API
- **Database**: MongoDB - Flexible document storage for items, users, and loops
- **Authentication**: JWT-based token authentication

### Architecture Principles
- Monorepo structure for unified development
- RESTful API design
- Component-based frontend architecture
- Secure authentication and authorization
- Real-time data updates where appropriate

## User Experience Principles

### Trust & Safety
- Email verification ensures legitimate users
- Loop-based visibility controls who sees items
- Owner approval required for all borrow requests
- Clear request status tracking

### Simplicity
- Intuitive item creation and editing
- Streamlined request workflow
- Easy loop management
- Quick search and discovery

### Control
- Users control item visibility per loop
- Owners approve all borrow requests
- Flexible availability management
- Cancel or reject requests as needed

### Transparency
- Clear request status for all parties
- Visible item availability
- Loop membership information
- Request history tracking

## Design Decisions Rationale

### Why Loop-Based Sharing?
Loops create trusted communities where users feel comfortable sharing valuable items. This reduces risk and increases participation compared to open marketplaces.

### Why Owner Approval Required?
Requiring owner approval for all requests ensures owners maintain control over their items and can coordinate lending based on their schedule and preferences.

### Why Items Stay Available Until Approved?
Keeping items marked as available until approval allows multiple users to express interest, giving owners flexibility in choosing who to lend to based on timing or relationship.

### Why Email Verification?
Email verification reduces fake accounts and ensures users can be contacted about their items and requests, building trust in the platform.

### Why Multiple Loops Per User?
Users participate in different communities (neighborhood, work, family) and need to manage item visibility separately for each context.

## Future Considerations

- Mobile application for on-the-go access
- In-app messaging between users
- Item condition tracking and reporting
- Rating and review system
- Calendar integration for lending schedules
- Notification system for requests and returns
- Analytics dashboard for loop administrators
