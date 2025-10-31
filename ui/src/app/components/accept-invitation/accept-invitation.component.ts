import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { LoopService } from '../../services/loop.service';

@Component({
  selector: 'app-accept-invitation',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './accept-invitation.component.html',
  styleUrls: ['./accept-invitation.component.css']
})
export class AcceptInvitationComponent implements OnInit {
  loading = true;
  success = false;
  error: string | null = null;
  loopId: string | null = null;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private loopService: LoopService
  ) {}

  ngOnInit(): void {
    const token = this.route.snapshot.queryParamMap.get('token');
    if (token) {
      this.acceptInvitation(token);
    } else {
      this.error = 'Invalid invitation link';
      this.loading = false;
    }
  }

  acceptInvitation(token: string): void {
    this.loopService.acceptInvitationByToken(token).subscribe({
      next: (invitation) => {
        this.success = true;
        this.loopId = invitation.loopId;
        this.loading = false;
        setTimeout(() => {
          this.router.navigate(['/loops', invitation.loopId]);
        }, 2000);
      },
      error: (err) => {
        this.error = err.error?.message || 'Failed to accept invitation. The link may have expired.';
        this.loading = false;
      }
    });
  }

  navigateToLoops(): void {
    this.router.navigate(['/loops']);
  }
}
