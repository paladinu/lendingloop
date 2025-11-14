export interface ItemRequest {
    id?: string;
    itemId: string;
    requesterId: string;
    ownerId: string;
    status: RequestStatus;
    message?: string;
    expectedReturnDate?: Date;
    requestedAt: Date;
    respondedAt?: Date;
    completedAt?: Date;
    // Populated fields for display
    itemName?: string;
    requesterName?: string;
    ownerName?: string;
}

export enum RequestStatus {
    Pending = 'Pending',
    Approved = 'Approved',
    Rejected = 'Rejected',
    Cancelled = 'Cancelled',
    Completed = 'Completed'
}
