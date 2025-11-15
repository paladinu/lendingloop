export interface SharedItem {
    id?: string;
    name: string;
    description: string;
    userId: string;
    isAvailable: boolean;
    imageUrl?: string;
    visibleToLoopIds: string[];
    visibleToAllLoops: boolean;
    visibleToFutureLoops: boolean;
    createdAt: Date;
    updatedAt: Date;
    ownerName?: string;
    ownerScore?: number;
}