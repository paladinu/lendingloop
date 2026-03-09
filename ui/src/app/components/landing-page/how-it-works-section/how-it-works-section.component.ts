import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';

export interface HowItWorksStep {
  stepNumber: number;
  title: string;
  description: string;
}

@Component({
  selector: 'app-how-it-works-section',
  standalone: true,
  imports: [CommonModule],
  template: `
    <section class="how-it-works-section">
      <h2 class="how-it-works-heading">How It Works</h2>
      <div class="steps-container">
        <div class="how-it-works-step" *ngFor="let step of steps">
          <div class="step-number">{{ step.stepNumber }}</div>
          <h3 class="step-title">{{ step.title }}</h3>
          <p class="step-description">{{ step.description }}</p>
        </div>
      </div>
    </section>
  `,
  styles: [`
    .how-it-works-section {
      padding: 4rem 2rem;
      background: #f5f7fa;
      text-align: center;
    }
    .how-it-works-heading {
      font-size: 2rem;
      font-weight: 700;
      color: #1a1a2e;
      margin-bottom: 2.5rem;
    }
    .steps-container {
      display: grid;
      grid-template-columns: 1fr;
      gap: 1.5rem;
      max-width: 900px;
      margin: 0 auto;
    }
    .how-it-works-step {
      padding: 2rem 1.5rem;
      background: #fff;
      border-radius: 8px;
      border: 1px solid #e0e0e0;
    }
    .step-number {
      width: 48px;
      height: 48px;
      border-radius: 50%;
      background: #1976d2;
      color: #fff;
      font-size: 1.25rem;
      font-weight: 700;
      display: flex;
      align-items: center;
      justify-content: center;
      margin: 0 auto 1rem;
    }
    .step-title {
      font-size: 1.2rem;
      font-weight: 600;
      color: #1a1a2e;
      margin-bottom: 0.5rem;
    }
    .step-description {
      font-size: 0.95rem;
      color: #555;
      line-height: 1.6;
    }
    @media (min-width: 768px) {
      .steps-container {
        grid-template-columns: repeat(3, 1fr);
      }
    }
  `]
})
export class HowItWorksSectionComponent {
  steps: HowItWorksStep[] = [
    {
      stepNumber: 1,
      title: 'Create an Account',
      description: 'Sign up with your email in under a minute'
    },
    {
      stepNumber: 2,
      title: 'Join or Create a Loop',
      description: 'Connect with your community in a trusted sharing group'
    },
    {
      stepNumber: 3,
      title: 'Start Sharing',
      description: 'List your items and request to borrow from others'
    }
  ];
}
