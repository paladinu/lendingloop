import { Component } from '@angular/core';
import { RouterModule } from '@angular/router';

@Component({
  selector: 'app-hero-section',
  standalone: true,
  imports: [RouterModule],
  template: `
    <section class="hero-section">
      <div class="hero-content">
        <h1 class="hero-headline">Share More. Buy Less. Build Community.</h1>
        <p class="hero-subheadline">LendingLoop lets you borrow and lend tools and items within trusted groups called loops.</p>
        <div class="hero-actions">
          <a routerLink="/register" class="cta-primary">Get Started Free</a>
          <a routerLink="/login" class="cta-secondary">Already have an account? Log in</a>
        </div>
      </div>
    </section>
  `,
  styles: [`
    .hero-section {
      display: flex;
      justify-content: center;
      align-items: center;
      padding: 4rem 2rem;
      background: linear-gradient(135deg, #e3f2fd 0%, #f5f5f5 100%);
      text-align: center;
    }
    .hero-content {
      max-width: 700px;
    }
    .hero-headline {
      font-size: 2.5rem;
      font-weight: 800;
      color: #1a1a2e;
      margin-bottom: 1rem;
      line-height: 1.2;
    }
    .hero-subheadline {
      font-size: 1.2rem;
      color: #555;
      margin-bottom: 2rem;
      line-height: 1.6;
    }
    .hero-actions {
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: 1rem;
    }
    .cta-primary {
      background: #1976d2;
      color: #fff;
      padding: 0.75rem 2rem;
      border-radius: 4px;
      text-decoration: none;
      font-size: 1.1rem;
      font-weight: 600;
    }
    .cta-secondary {
      color: #1976d2;
      text-decoration: none;
      font-size: 0.95rem;
    }
    @media (min-width: 768px) {
      .hero-headline {
        font-size: 3rem;
      }
      .hero-actions {
        flex-direction: row;
        justify-content: center;
      }
    }
  `]
})
export class HeroSectionComponent {}
