import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../services/auth.service';
import { UserProfile } from '../../models/auth.interface';
import { ToolbarComponent } from '../toolbar/toolbar.component';
import { ScoreHistoryComponent } from '../score-history/score-history.component';
import { LoopScoreDisplayComponent } from '../loop-score-display/loop-score-display.component';
import { BadgeDisplayComponent } from '../badge-display/badge-display.component';

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [
    CommonModule,
    ToolbarComponent,
    ScoreHistoryComponent,
    LoopScoreDisplayComponent,
    BadgeDisplayComponent
  ],
  templateUrl: './profile.component.html',
  styleUrls: ['./profile.component.css']
})
export class ProfileComponent implements OnInit {
  currentUser: UserProfile | null = null;

  constructor(private authService: AuthService) {}

  ngOnInit(): void {
    // Refresh user data from server to get latest badges and score
    this.authService.refreshCurrentUser().subscribe({
      next: (user) => {
        this.currentUser = user;
      },
      error: (err) => {
        console.error('Error loading current user:', err);
        // Fallback to cached user if refresh fails
        this.authService.getCurrentUser().subscribe({
          next: (cachedUser) => {
            this.currentUser = cachedUser;
          }
        });
      }
    });
  }

  getUserDisplayName(): string {
    if (this.currentUser) {
      return `${this.currentUser.firstName} ${this.currentUser.lastName}`.trim();
    }
    return 'User';
  }
}
