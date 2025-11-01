import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { LoopService } from '../../services/loop.service';
import { LoopInvitation } from '../../models/loop-invitation.interface';
import { ToolbarComponent } from '../toolbar/toolbar.component';

@Component({
  selector: 'app-loop-invitations',
  standalone: true,
  imports: [CommonModule, ToolbarComponent],
  templateUrl: './loop-invitations.component.html',
  styleUrls: ['./loop-invitations.component.css']
})
export class LoopInvitationsComponent implements OnInit {
  invitations: LoopInvitation[] = [];
  loading = false;
  error: string | null = null;

  constructor(
    private loopService: LoopService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.loadInvitations();
  }

  loadInvitations(): void {
    this.loading = true;
    this.error = null;

    this.loopService.getPendingInvitations().subscribe({
      next: (invitations) => {
        this.invitations = invitations;
        this.loading = false;
      },
      error: (err) => {
        this.error = 'Failed to load invitations';
        this.loading = false;
        console.error('Error loading invitations:', err);
      }
    });
  }

  acceptInvitation(invitationId: string | undefined): void {
    if (!invitationId) return;

    this.loopService.acceptInvitationByUser(invitationId).subscribe({
      next: (invitation) => {
        this.invitations = this.invitations.filter(inv => inv.id !== invitationId);
        this.router.navigate(['/loops', invitation.loopId]);
      },
      error: (err) => {
        this.error = err.error?.message || 'Failed to accept invitation';
        console.error('Error accepting invitation:', err);
      }
    });
  }

  navigateToHome(): void {
    this.router.navigate(['/']);
  }
}
