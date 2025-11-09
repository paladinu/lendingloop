export interface Notification {
    id: string;
    userId: string;
    type: NotificationType;
    message: string;
    itemId?: string;
    itemRequestId?: string;
    relatedUserId?: string;
    isRead: boolean;
    createdAt: Date;
}

export enum NotificationType {
    ItemRequestCreated = 'ItemRequestCreated',
    ItemRequestApproved = 'ItemRequestApproved',
    ItemRequestRejected = 'ItemRequestRejected',
    ItemRequestCompleted = 'ItemRequestCompleted',
    ItemRequestCancelled = 'ItemRequestCancelled'
}
