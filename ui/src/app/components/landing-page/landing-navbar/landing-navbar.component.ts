import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { AuthService } from '../../../services/auth.service';

@Component({
  selector: 'app-landing-navbar',
  standalone: true,
  imports: [CommonModule, RouterModule],
  template: `
    <nav class="landing-navbar">
      <div class="navbar-brand">
        <span class="brand-name">LendingLoop</span>
      </div>
      <div class="navbar-links">
        <ng-container *ngIf="!isAuthenticated">
          <a routerLink="/login" class="nav-link login-link">Log in</a>
          <a routerLink="/register" class="nav-link register-cta">Get Started</a>
        </ng-container>
        <ng-container *ngIf="isAuthenticated">
          <a routerLink="/loops" class="nav-link go-to-app-link">Go to App</a>
        </ng-container>
      </div>
    </nav>
  `,
  styles: [`
    .landing-navbar {
      display: flex;
      align-items: center;
      justify-content: space-between;
      padding: 1rem 2rem;
      background: #fff;
      box-shadow: 0 1px 4px rgba(0,0,0,0.08);
    }
    .brand-name {
      font-size: 1.4rem;
      font-weight: 700;
      color: #1976d2;
    }
    .navbar-links {
      display: flex;
      align-items: center;
      gap: 1rem;
    }
    .nav-link {
      text-decoration: none;
      color: #333;
      font-weight: 500;
    }
    .register-cta {
      background: #1976d2;
      color: #fff;
      padding: 0.4rem 1rem;
      border-radius: 4px;
    }
  `]
})
export class LandingNavbarComponent {
  isAuthenticated: boolean;

  constructor(private authService: AuthService) {
    this.isAuthenticated = this.authService.isAuthenticated();
  }
}
