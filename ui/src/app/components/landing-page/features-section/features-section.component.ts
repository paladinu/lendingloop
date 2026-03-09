import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';

export interface FeatureHighlight {
  icon: string;
  title: string;
  description: string;
}

@Component({
  selector: 'app-features-section',
  standalone: true,
  imports: [CommonModule],
  template: `
    <section class="features-section">
      <h2 class="features-heading">Why LendingLoop?</h2>
      <div class="features-grid">
        <div class="feature-card" *ngFor="let feature of features">
          <span class="material-icons feature-icon">{{ feature.icon }}</span>
          <h3 class="feature-title">{{ feature.title }}</h3>
          <p class="feature-description">{{ feature.description }}</p>
        </div>
      </div>
    </section>
  `,
  styles: [`
    .features-section {
      padding: 4rem 2rem;
      background: #fff;
      text-align: center;
    }
    .features-heading {
      font-size: 2rem;
      font-weight: 700;
      color: #1a1a2e;
      margin-bottom: 2.5rem;
    }
    .features-grid {
      display: grid;
      grid-template-columns: 1fr;
      gap: 1.5rem;
      max-width: 1000px;
      margin: 0 auto;
    }
    .feature-card {
      padding: 2rem 1.5rem;
      border: 1px solid #e0e0e0;
      border-radius: 8px;
      background: #fafafa;
    }
    .feature-icon {
      font-size: 2.5rem;
      color: #1976d2;
      margin-bottom: 1rem;
    }
    .feature-title {
      font-size: 1.2rem;
      font-weight: 600;
      color: #1a1a2e;
      margin-bottom: 0.5rem;
    }
    .feature-description {
      font-size: 0.95rem;
      color: #555;
      line-height: 1.6;
    }
    @media (min-width: 768px) {
      .features-grid {
        grid-template-columns: repeat(3, 1fr);
      }
    }
  `]
})
export class FeaturesSectionComponent {
  features: FeatureHighlight[] = [
    {
      icon: 'group',
      title: 'Trusted Loops',
      description: 'Create or join sharing groups with people you trust'
    },
    {
      icon: 'swap_horiz',
      title: 'Lend & Borrow Items',
      description: 'Share tools and equipment with your community'
    },
    {
      icon: 'check_circle',
      title: 'Owner-Controlled Approvals',
      description: 'You decide who borrows your items, every time'
    }
  ];
}
