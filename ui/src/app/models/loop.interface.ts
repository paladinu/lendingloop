export interface Loop {
  id?: string;
  name: string;
  creatorId: string;
  memberIds: string[];
  createdAt: Date;
  updatedAt: Date;
  memberCount?: number;
  itemCount?: number;
}
