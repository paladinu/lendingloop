import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute } from '@angular/router';
import { Location } from '@angular/common';
import { AuthService } from '../../services/auth.service';
import { UserService } from '../../services/user.service';
import { UserProfile, PublicProfile, ScoreHistoryEntry } from '../../models/auth.interface';
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
  profileData: UserProfile | PublicProfile | null = null;
  userId: string | null = null;
  isOwnProfile: boolean = false;
  isLoading: boolean = true;
  errorMessage: string | null = null;

  constructor(
    private authService: AuthService,
    private userService: UserService,
    private route: ActivatedRoute,
    private location: Location
  ) {}

  ngOnInit(): void {
    // Subscribe to route parameters to get userId
    this.route.paramMap.subscribe(params => {
      this.userId = params.get('userId');
      
      // Get current user from the BehaviorSubject to determine if viewing own profile
      this.authService.getCurrentUser().subscribe(currentUser => {
        // Determine if viewing own profile (no userId or userId matches current user)
        if (!this.userId || (currentUser && this.userId === currentUser.id)) {
          this.isOwnProfile = true;
        } else {
          this.isOwnProfile = false;
        }
        
        // Load profile data
        this.loadProfile();
      });
    });
  }

  private loadProfile(): void {
    this.isLoading = true;
    this.errorMessage = null;
    
    if (this.isOwnProfile) {
      // Load private profile for current user
      this.authService.refreshCurrentUser().subscribe({
        next: (user) => {
          this.profileData = user;
          this.currentUser = user;
          this.isLoading = false;
        },
        error: (err) => {
          console.error('Error loading current user:', err);
          this.errorMessage = 'Failed to load profile';
          this.isLoading = false;
        }
      });
    } else if (this.userId) {
      // Load public profile for another user
      this.userService.getPublicProfile(this.userId).subscribe({
        next: (profile) => {
          this.profileData = profile;
          this.isLoading = false;
        },
        error: (err) => {
          console.error('Error loading public profile:', err);
          // Set appropriate error message based on status code
          if (err.status === 403) {
            this.errorMessage = 'You can only view profiles of users in your loops';
          } else if (err.status === 404) {
            this.errorMessage = 'User not found';
          } else {
            this.errorMessage = 'Failed to load profile';
          }
          this.isLoading = false;
        }
      });
    }
  }

  getUserDisplayName(): string {
    if (this.profileData) {
      return `${this.profileData.firstName} ${this.profileData.lastName}`.trim();
    }
    return 'User';
  }

  getProfileUserId(): string {
    if (this.profileData) {
      // UserProfile has 'id', PublicProfile has 'userId'
      return (this.profileData as UserProfile).id || (this.profileData as PublicProfile).userId || '';
    }
    return '';
  }

  getScoreHistory(): ScoreHistoryEntry[] | null {
    if (this.profileData && 'scoreHistory' in this.profileData) {
      return (this.profileData as PublicProfile).scoreHistory;
    }
    return null;
  }

  goBack(): void {
    this.location.back();
  }
}
