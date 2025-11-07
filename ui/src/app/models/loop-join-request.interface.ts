export interface LoopJoinRequest {
  id?: string;
  loopId: string;
  userId: string;
  message: string;
  status: 'Pending' | 'Approved' | 'Rejected';
  createdAt: Date;
  respondedAt?: Date;
  userName?: string;
  userEmail?: string;
  loopName?: string;
}
