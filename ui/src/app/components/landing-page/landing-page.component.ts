import { Component } from '@angular/core';
import { LandingNavbarComponent } from './landing-navbar/landing-navbar.component';
import { HeroSectionComponent } from './hero-section/hero-section.component';
import { FeaturesSectionComponent } from './features-section/features-section.component';
import { HowItWorksSectionComponent } from './how-it-works-section/how-it-works-section.component';
import { CtaSectionComponent } from './cta-section/cta-section.component';

@Component({
  selector: 'app-landing-page',
  standalone: true,
  imports: [
    LandingNavbarComponent,
    HeroSectionComponent,
    FeaturesSectionComponent,
    HowItWorksSectionComponent,
    CtaSectionComponent
  ],
  template: `
    <div class="landing-page">
      <app-landing-navbar></app-landing-navbar>
      <app-hero-section></app-hero-section>
      <app-features-section></app-features-section>
      <app-how-it-works-section></app-how-it-works-section>
      <app-cta-section></app-cta-section>
    </div>
  `,
  styles: [`
    .landing-page {
      display: flex;
      flex-direction: column;
      min-height: 100vh;
      width: 100%;
      overflow-x: hidden;
      box-sizing: border-box;
    }

    :host {
      display: block;
      width: 100%;
      max-width: 100vw;
      overflow-x: hidden;
    }

    @media (max-width: 767px) {
      .landing-page {
        flex-direction: column;
      }
    }

    @media (min-width: 768px) {
      .landing-page {
        flex-direction: column;
      }
    }
  `]
})
export class LandingPageComponent {}
