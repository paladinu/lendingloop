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
    // Subscribe to authentication state changes
    this.authService.currentUser$.subscribe({
      next: (user) => {
        console.log('App component - user state changed:', user);
        this.currentUser = user;
        this.isAuthenticated = !!user && this.authService.isAuthenticated();
        console.log('App component - isAuthenticated:', this.isAuthenticated);
        console.log('App component - current route:', this.router.url);

        // Only redirect to login if we're sure the user is not authenticated
        // and we're not in the middle of a login process
        if (!this.isAuthenticated && !this.isOnAuthRoute() && this.router.url !== '/') {
          console.log('App component - redirecting to login');
          this.router.navigate(['/login']);
        }
      },
      error: (error) => {
        console.error('Error in authentication state:', error);
        this.currentUser = null;
        this.isAuthenticated = false;
      }
    });

    // Initial authentication check - only run this once on app startup
    setTimeout(() => {
      this.checkInitialAuthState();
    }, 100);
  }

  private checkInitialAuthState(): void {
    console.log('App component - checking initial auth state');
    console.log('App component - isAuthenticated:', this.authService.isAuthenticated());
    console.log('App component - current route:', this.router.url);

    // Only redirect if we're on the root route and not authenticated
    if (!this.authService.isAuthenticated() && this.router.url === '/') {
      console.log('App component - not authenticated and on root, redirecting to login');
      this.router.navigate(['/login']);
    }
  }

  private isOnAuthRoute(): boolean {
    const currentUrl = this.router.url;
    return currentUrl.includes('/auth/') ||
      currentUrl === '/login' ||
      currentUrl === '/register' ||
      currentUrl === '/verify-email';
  }

  getUserDisplayName(): string {
    if (this.currentUser) {
      return `${this.currentUser.firstName} ${this.currentUser.lastName}`;
    }
    return 'User';
  }
}
