export interface LoopInvitation {
  id?: string;
  loopId: string;
  loopName?: string;
  invitedByUserId: string;
  invitedByUserName?: string;
  invitedByUserLoopScore?: number;
  invitedEmail: string;
  invitedUserId?: string;
  invitationToken: string;
  status: 'Pending' | 'Accepted' | 'Expired' | 'Declined';
  expiresAt: Date;
  createdAt: Date;
  acceptedAt?: Date;
}
