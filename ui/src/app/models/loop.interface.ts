export interface Loop {
  id?: string;
  name: string;
  description: string;
  creatorId: string;
  memberIds: string[];
  isPublic: boolean;
  isArchived: boolean;
  archivedAt?: Date;
  ownershipHistory: OwnershipTransfer[];
  createdAt: Date;
  updatedAt: Date;
  memberCount?: number;
  itemCount?: number;
}

export interface OwnershipTransfer {
  fromUserId: string;
  toUserId: string;
  transferredAt: Date;
  status: 'Pending' | 'Accepted' | 'Declined' | 'Cancelled';
  fromUserName?: string;
  toUserName?: string;
}

export interface LoopSettings {
  name: string;
  description: string;
  isPublic: boolean;
}
