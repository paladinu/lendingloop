import { Component } from '@angular/core';
import { RouterModule } from '@angular/router';

@Component({
  selector: 'app-cta-section',
  standalone: true,
  imports: [RouterModule],
  template: `
    <section class="cta-section">
      <p class="cta-message">Ready to start sharing with your community?</p>
      <a routerLink="/register" class="cta-button">Join LendingLoop Today</a>
    </section>
  `,
  styles: [`
    .cta-section {
      padding: 4rem 2rem;
      background: #1976d2;
      text-align: center;
    }
    .cta-message {
      font-size: 1.5rem;
      font-weight: 600;
      color: #fff;
      margin-bottom: 1.5rem;
    }
    .cta-button {
      display: inline-block;
      padding: 0.875rem 2rem;
      background: #fff;
      color: #1976d2;
      font-size: 1rem;
      font-weight: 700;
      border-radius: 4px;
      text-decoration: none;
    }
    .cta-button:hover {
      background: #e3f2fd;
    }
  `]
})
export class CtaSectionComponent {}
