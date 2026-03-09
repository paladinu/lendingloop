import { TestBed } from '@angular/core/testing';
import { RouterModule } from '@angular/router';
import * as fc from 'fast-check';
import { FeaturesSectionComponent, FeatureHighlight } from './features-section/features-section.component';
import { HowItWorksSectionComponent, HowItWorksStep } from './how-it-works-section/how-it-works-section.component';
import { LandingNavbarComponent } from './landing-navbar/landing-navbar.component';
import { AuthService } from '../../services/auth.service';

fc.configureGlobal({ numRuns: 100 });

/**
 * Property-based tests for the Public Landing Page components.
 * These tests verify universal properties that must hold across all valid inputs.
 */

// ---------------------------------------------------------------------------
// Property 1: Features array always renders >= 3 highlights
// Feature: public-landing-page, Property 1: features array renders >= 3 highlights
// Validates: Requirements 3.2
// ---------------------------------------------------------------------------
describe('FeaturesSectionComponent - Property Tests', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [FeaturesSectionComponent]
    }).compileComponents();
  });

  afterEach(() => {
    TestBed.resetTestingModule();
  });

  /**
   * **Feature: public-landing-page, Property 1: features array renders >= 3 highlights**
   * **Validates: Requirements 3.2**
   *
   * For any features array of length >= 3, the rendered DOM should contain
   * exactly as many .feature-card elements as the array length, and that count
   * must be >= 3.
   */
  it('should render a .feature-card for every item in any features array of length >= 3', () => {
    fc.assert(
      fc.property(
        fc.array(
          fc.record({
            icon: fc.string(),
            title: fc.string({ minLength: 1 }),
            description: fc.string({ minLength: 1 })
          }),
          { minLength: 3 }
        ),
        (generatedFeatures: FeatureHighlight[]) => {
          //arrange
          const fixture = TestBed.createComponent(FeaturesSectionComponent);
          const component = fixture.componentInstance;

          //act
          component.features = generatedFeatures;
          fixture.detectChanges();

          //assert
          const cards = fixture.nativeElement.querySelectorAll('.feature-card');
          expect(cards.length).toBe(generatedFeatures.length);
          expect(cards.length).toBeGreaterThanOrEqual(3);

          fixture.destroy();
        }
      )
    );
  });
});

// ---------------------------------------------------------------------------
// Property 2: Steps array always renders >= 3 steps
// Feature: public-landing-page, Property 2: steps array renders >= 3 steps
// Validates: Requirements 4.2
// ---------------------------------------------------------------------------
describe('HowItWorksSectionComponent - Property 2: Steps count', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HowItWorksSectionComponent]
    }).compileComponents();
  });

  afterEach(() => {
    TestBed.resetTestingModule();
  });

  /**
   * **Feature: public-landing-page, Property 2: steps array renders >= 3 steps**
   * **Validates: Requirements 4.2**
   *
   * For any steps array of length >= 3, the rendered DOM should contain
   * exactly as many .how-it-works-step elements as the array length, and that
   * count must be >= 3.
   */
  it('should render a .how-it-works-step for every item in any steps array of length >= 3', () => {
    fc.assert(
      fc.property(
        fc.array(
          fc.record({
            stepNumber: fc.integer({ min: 1 }),
            title: fc.string({ minLength: 1 }),
            description: fc.string({ minLength: 1 })
          }),
          { minLength: 3 }
        ),
        (generatedSteps: HowItWorksStep[]) => {
          //arrange
          const fixture = TestBed.createComponent(HowItWorksSectionComponent);
          const component = fixture.componentInstance;

          //act
          component.steps = generatedSteps;
          fixture.detectChanges();

          //assert
          const stepEls = fixture.nativeElement.querySelectorAll('.how-it-works-step');
          expect(stepEls.length).toBe(generatedSteps.length);
          expect(stepEls.length).toBeGreaterThanOrEqual(3);

          fixture.destroy();
        }
      )
    );
  });
});

// ---------------------------------------------------------------------------
// Property 3: Every step renders with a number, title, and description
// Feature: public-landing-page, Property 3: every step renders with number, title, and description
// Validates: Requirements 4.3
// ---------------------------------------------------------------------------
describe('HowItWorksSectionComponent - Property 3: Step fields', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HowItWorksSectionComponent]
    }).compileComponents();
  });

  afterEach(() => {
    TestBed.resetTestingModule();
  });

  /**
   * **Feature: public-landing-page, Property 3: every step renders with number, title, and description**
   * **Validates: Requirements 4.3**
   *
   * For any arbitrary HowItWorksStep, the rendered DOM should contain
   * non-empty .step-number, .step-title, and .step-description elements.
   */
  it('should render .step-number, .step-title, and .step-description for any valid step', () => {
    fc.assert(
      fc.property(
        fc.record({
          stepNumber: fc.integer({ min: 1, max: 999 }),
          title: fc.string({ minLength: 1, maxLength: 100 }).filter(s => s.trim().length > 0),
          description: fc.string({ minLength: 1, maxLength: 200 }).filter(s => s.trim().length > 0)
        }),
        (step: HowItWorksStep) => {
          //arrange
          const fixture = TestBed.createComponent(HowItWorksSectionComponent);
          const component = fixture.componentInstance;

          //act
          component.steps = [step];
          fixture.detectChanges();

          //assert
          const nativeEl: HTMLElement = fixture.nativeElement;

          const stepNumberEl = nativeEl.querySelector('.step-number');
          const stepTitleEl = nativeEl.querySelector('.step-title');
          const stepDescriptionEl = nativeEl.querySelector('.step-description');

          expect(stepNumberEl).not.toBeNull();
          expect(stepNumberEl?.textContent?.trim().length).toBeGreaterThan(0);

          expect(stepTitleEl).not.toBeNull();
          expect(stepTitleEl?.textContent?.trim().length).toBeGreaterThan(0);

          expect(stepDescriptionEl).not.toBeNull();
          expect(stepDescriptionEl?.textContent?.trim().length).toBeGreaterThan(0);

          fixture.destroy();
        }
      )
    );
  });
});

// ---------------------------------------------------------------------------
// Property 4: Nav bar shows correct links based on auth state
// Feature: public-landing-page, Property 4: nav bar shows correct links based on auth state
// Validates: Requirements 6.5
// ---------------------------------------------------------------------------
describe('LandingNavbarComponent - Property 4: Auth state links', () => {
  /**
   * **Feature: public-landing-page, Property 4: nav bar shows correct links based on auth state**
   * **Validates: Requirements 6.5**
   *
   * For any boolean auth state, the navbar must show the correct set of links:
   * - authenticated  → .go-to-app-link present; .login-link and .register-cta absent
   * - unauthenticated → .login-link and .register-cta present; .go-to-app-link absent
   */
  it('should show correct links for any auth state', async () => {
    await fc.assert(
      fc.asyncProperty(
        fc.boolean(),
        async (isAuthenticated: boolean) => {
          //arrange
          const mockAuthService = {
            isAuthenticated: jest.fn().mockReturnValue(isAuthenticated)
          };

          await TestBed.configureTestingModule({
            imports: [LandingNavbarComponent, RouterModule.forRoot([])],
            providers: [{ provide: AuthService, useValue: mockAuthService }]
          }).compileComponents();

          //act
          const fixture = TestBed.createComponent(LandingNavbarComponent);
          fixture.detectChanges();

          const nativeEl: HTMLElement = fixture.nativeElement;
          const goToAppLink = nativeEl.querySelector('.go-to-app-link');
          const loginLink = nativeEl.querySelector('.login-link');
          const registerCta = nativeEl.querySelector('.register-cta');

          //assert
          if (isAuthenticated) {
            expect(goToAppLink).not.toBeNull();
            expect(loginLink).toBeNull();
            expect(registerCta).toBeNull();
          } else {
            expect(goToAppLink).toBeNull();
            expect(loginLink).not.toBeNull();
            expect(registerCta).not.toBeNull();
          }

          fixture.destroy();
          TestBed.resetTestingModule();
        }
      )
    );
  });
});
