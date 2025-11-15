import { Component, OnInit } from '@angular/core';
import { RouterOutlet, Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { AuthService } from './services/auth.service';
import { UserProfile } from './models/auth.interface';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, CommonModule],
  templateUrl: './app.component.html',
  styleUrl: './app.component.css'
})
export class AppComponent implements OnInit {
  title = 'Shared Items Manager';
  currentUser: UserProfile | null = null;
  isAuthenticated = false;

  constructor(
    private authService: AuthService,
    private router: Router
  ) { }

  ngOnInit(): void {
    // Subscribe to authentication state changes for display purposes only
    // Do NOT redirect from here - let the AuthGuard handle all redirects
    this.authService.currentUser$.subscribe({
      next: (user) => {
        this.currentUser = user;
        this.isAuthenticated = !!user && this.authService.isAuthenticated();
      },
      error: (error) => {
        console.error('Error in authentication state:', error);
        this.currentUser = null;
        this.isAuthenticated = false;
      }
    });
  }

  getUserDisplayName(): string {
    if (this.currentUser) {
      return `${this.currentUser.firstName} ${this.currentUser.lastName}`;
    }
    return 'User';
  }
}
