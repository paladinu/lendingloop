import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Loop } from '../models/loop.interface';
import { LoopInvitation } from '../models/loop-invitation.interface';
import { LoopMember } from '../models/loop-member.interface';
import { SharedItem } from '../models/shared-item.interface';

@Injectable({
  providedIn: 'root'
})
export class LoopService {
  private apiUrl = '/api/loops';

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
}
