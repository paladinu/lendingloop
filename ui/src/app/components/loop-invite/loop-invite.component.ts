import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { LoopService } from '../../services/loop.service';
import { LoopMember } from '../../models/loop-member.interface';
import { ToolbarComponent } from '../toolbar/toolbar.component';

@Component({
  selector: 'app-loop-invite',
  standalone: true,
  imports: [CommonModule, FormsModule, ToolbarComponent],
  templateUrl: './loop-invite.component.html',
  styleUrls: ['./loop-invite.component.css']
})
export class LoopInviteComponent implements OnInit {
  loopId: string | null = null;
  email = '';
  potentialInvitees: LoopMember[] = [];
  selectedUserIds: Set<string> = new Set();
  loading = false;
  loadingInvitees = false;
  error: string | null = null;
  success: string | null = null;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private loopService: LoopService
  ) {}

  ngOnInit(): void {
    this.loopId = this.route.snapshot.paramMap.get('id');
    if (this.loopId) {
      this.loadPotentialInvitees();
    }
  }

  loadPotentialInvitees(): void {
    if (!this.loopId) return;

    this.loadingInvitees = true;
    this.loopService.getPotentialInvitees(this.loopId).subscribe({
      next: (users) => {
        this.potentialInvitees = users;
        this.loadingInvitees = false;
      },
      error: (err) => {
        console.error('Error loading potential invitees:', err);
        this.loadingInvitees = false;
      }
    });
  }

  onEmailInvite(): void {
    if (!this.loopId || !this.email.trim()) {
      this.error = 'Email is required';
      return;
    }

    this.loading = true;
    this.error = null;
    this.success = null;

    this.loopService.inviteByEmail(this.loopId, this.email.trim()).subscribe({
      next: () => {
        this.success = `Invitation sent to ${this.email}`;
        this.email = '';
        this.loading = false;
      },
      error: (err) => {
        this.error = err.error?.message || 'Failed to send invitation';
        this.loading = false;
      }
    });
  }

  toggleUserSelection(userId: string): void {
    if (this.selectedUserIds.has(userId)) {
      this.selectedUserIds.delete(userId);
    } else {
      this.selectedUserIds.add(userId);
    }
  }

  onInviteSelectedUsers(): void {
    if (!this.loopId || this.selectedUserIds.size === 0) {
      this.error = 'Please select at least one user to invite';
      return;
    }

    this.loading = true;
    this.error = null;
    this.success = null;

    const invitations = Array.from(this.selectedUserIds).map(userId =>
      this.loopService.inviteUser(this.loopId!, userId).toPromise()
    );

    Promise.all(invitations)
      .then(() => {
        this.success = `Invited ${this.selectedUserIds.size} user(s)`;
        this.selectedUserIds.clear();
        this.loading = false;
        this.loadPotentialInvitees();
      })
      .catch((err) => {
        this.error = err.error?.message || 'Failed to send invitations';
        this.loading = false;
      });
  }

  navigateBack(): void {
    if (this.loopId) {
      this.router.navigate(['/loops', this.loopId]);
    }
  }
}
