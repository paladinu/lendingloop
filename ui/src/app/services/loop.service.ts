import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Loop, LoopSettings, OwnershipTransfer } from '../models/loop.interface';
import { LoopInvitation } from '../models/loop-invitation.interface';
import { LoopMember } from '../models/loop-member.interface';
import { SharedItem } from '../models/shared-item.interface';
import { LoopJoinRequest } from '../models/loop-join-request.interface';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class LoopService {
  private apiUrl = `${environment.apiUrl}/api/loops`;

  constructor(private http: HttpClient) { }

  createLoop(name: string): Observable<Loop> {
    return this.http.post<Loop>(this.apiUrl, { name });
  }

  getUserLoops(): Observable<Loop[]> {
    return this.http.get<Loop[]>(this.apiUrl);
  }

  getLoopById(id: string): Observable<Loop> {
    return this.http.get<Loop>(`${this.apiUrl}/${id}`);
  }

  getLoopMembers(loopId: string): Observable<LoopMember[]> {
    return this.http.get<LoopMember[]>(`${this.apiUrl}/${loopId}/members`);
  }

  getLoopItems(loopId: string, search?: string): Observable<{ items: SharedItem[], totalCount: number }> {
    let url = `${this.apiUrl}/${loopId}/items`;
    if (search) {
      url += `?search=${encodeURIComponent(search)}`;
    }
    return this.http.get<{ items: SharedItem[], totalCount: number }>(url);
  }

  inviteByEmail(loopId: string, email: string): Observable<LoopInvitation> {
    return this.http.post<LoopInvitation>(`${this.apiUrl}/${loopId}/invite-email`, { email });
  }

  inviteUser(loopId: string, userId: string): Observable<LoopInvitation> {
    return this.http.post<LoopInvitation>(`${this.apiUrl}/${loopId}/invite-user`, { userId });
  }

  getPotentialInvitees(loopId: string): Observable<LoopMember[]> {
    return this.http.get<LoopMember[]>(`${this.apiUrl}/${loopId}/potential-invitees`);
  }

  acceptInvitationByToken(token: string): Observable<LoopInvitation> {
    return this.http.post<LoopInvitation>(`${this.apiUrl}/invitations/${token}/accept`, {});
  }

  acceptInvitationByUser(invitationId: string): Observable<LoopInvitation> {
    return this.http.post<LoopInvitation>(`${this.apiUrl}/invitations/${invitationId}/accept-user`, {});
  }

  getPendingInvitations(): Observable<LoopInvitation[]> {
    return this.http.get<LoopInvitation[]>(`${this.apiUrl}/invitations/pending`);
  }

  // Loop settings management
  updateLoopSettings(loopId: string, settings: LoopSettings): Observable<Loop> {
    return this.http.put<Loop>(`${this.apiUrl}/${loopId}/settings`, settings);
  }

  getLoopSettings(loopId: string): Observable<LoopSettings> {
    return this.http.get<LoopSettings>(`${this.apiUrl}/${loopId}/settings`);
  }

  isLoopOwner(loopId: string): Observable<boolean> {
    return this.http.get<boolean>(`${this.apiUrl}/${loopId}/is-owner`);
  }

  // Loop archival
  archiveLoop(loopId: string): Observable<Loop> {
    return this.http.post<Loop>(`${this.apiUrl}/${loopId}/archive`, {});
  }

  restoreLoop(loopId: string): Observable<Loop> {
    return this.http.post<Loop>(`${this.apiUrl}/${loopId}/restore`, {});
  }

  getArchivedLoops(): Observable<Loop[]> {
    return this.http.get<Loop[]>(`${this.apiUrl}/archived`);
  }

  deleteLoop(loopId: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${loopId}`);
  }

  // Ownership transfer
  initiateOwnershipTransfer(loopId: string, newOwnerId: string): Observable<Loop> {
    return this.http.post<Loop>(`${this.apiUrl}/${loopId}/transfer-ownership`, { newOwnerId });
  }

  acceptOwnershipTransfer(loopId: string): Observable<Loop> {
    return this.http.post<Loop>(`${this.apiUrl}/${loopId}/transfer-ownership/accept`, {});
  }

  declineOwnershipTransfer(loopId: string): Observable<Loop> {
    return this.http.post<Loop>(`${this.apiUrl}/${loopId}/transfer-ownership/decline`, {});
  }

  cancelOwnershipTransfer(loopId: string): Observable<Loop> {
    return this.http.post<Loop>(`${this.apiUrl}/${loopId}/transfer-ownership/cancel`, {});
  }

  getPendingOwnershipTransfer(loopId: string): Observable<{ hasPendingTransfer: boolean; fromUserId?: string; fromUserName?: string; toUserId?: string; toUserName?: string; transferredAt?: Date; status?: string }> {
    return this.http.get<any>(`${this.apiUrl}/${loopId}/transfer-ownership/pending`);
  }

  // Public loop discovery
  getPublicLoops(skip: number = 0, limit: number = 20): Observable<Loop[]> {
    return this.http.get<Loop[]>(`${this.apiUrl}/public?skip=${skip}&limit=${limit}`);
  }

  searchPublicLoops(searchTerm: string, skip: number = 0, limit: number = 20): Observable<Loop[]> {
    return this.http.get<Loop[]>(`${this.apiUrl}/public/search?q=${encodeURIComponent(searchTerm)}&skip=${skip}&limit=${limit}`);
  }

  // Join requests
  createJoinRequest(loopId: string, message: string): Observable<LoopJoinRequest> {
    return this.http.post<LoopJoinRequest>(`${this.apiUrl}/${loopId}/join-requests`, { message });
  }

  getLoopJoinRequests(loopId: string): Observable<LoopJoinRequest[]> {
    return this.http.get<LoopJoinRequest[]>(`${this.apiUrl}/${loopId}/join-requests`);
  }

  approveJoinRequest(requestId: string): Observable<LoopJoinRequest> {
    return this.http.post<LoopJoinRequest>(`${this.apiUrl}/join-requests/${requestId}/approve`, {});
  }

  rejectJoinRequest(requestId: string): Observable<LoopJoinRequest> {
    return this.http.post<LoopJoinRequest>(`${this.apiUrl}/join-requests/${requestId}/reject`, {});
  }

  getMyJoinRequests(): Observable<LoopJoinRequest[]> {
    return this.http.get<LoopJoinRequest[]>(`${this.apiUrl}/join-requests/my-requests`);
  }

  // Member management
  removeMember(loopId: string, userId: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${loopId}/members/${userId}`);
  }

  leaveLoop(loopId: string): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/${loopId}/leave`, {});
  }
}
