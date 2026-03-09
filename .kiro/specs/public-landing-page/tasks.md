# Implementation Plan: Public Landing Page

## Overview

Implement a publicly accessible marketing landing page at the root route (`/`). This involves creating a new `authenticatedRedirectGuard`, five new Angular standalone components, and updating the root route in `app.routes.ts` to replace the existing redirect.

## Tasks

- [x] 1. Create the `authenticatedRedirectGuard`
  - Create `ui/src/app/guards/authenticated-redirect.guard.ts` as a functional `CanActivateFn`
  - Inject `AuthService` and `Router`; redirect authenticated users to `/loops` and return `false`, otherwise return `true`
  - Wrap the `isAuthenticated()` call in a try/catch that defaults to `return true` on error
  - _Requirements: 1.1, 1.2, 1.3_

  - [x] 1.1 Write unit tests for `authenticatedRedirectGuard`
    - Test: unauthenticated user → returns `true` (no redirect)
    - Test: authenticated user → navigates to `/loops` and returns `false`
    - Test: `isAuthenticated()` throws → returns `true` (fail open)
    - _Requirements: 1.1, 1.2, 1.3_

- [x] 2. Update root route in `app.routes.ts`
  - Replace `{ path: '', redirectTo: 'loops', pathMatch: 'full' }` with `{ path: '', component: LandingPageComponent, canActivate: [authenticatedRedirectGuard] }`
  - Import `LandingPageComponent` and `authenticatedRedirectGuard`
  - _Requirements: 1.1, 1.3_

- [x] 3. Implement `LandingNavbarComponent`
  - Create `ui/src/app/components/landing-page/landing-navbar/landing-navbar.component.ts` as a standalone component
  - Inject `AuthService`; expose `isAuthenticated` boolean to the template
  - Template: brand name, conditional login + register links (unauthenticated) or "Go to App" link to `/loops` (authenticated)
  - _Requirements: 6.1, 6.2, 6.3, 6.4, 6.5_

  - [x] 3.1 Write unit tests for `LandingNavbarComponent`
    - Test: unauthenticated → renders brand name, login link, register CTA; no "Go to App" link
    - Test: authenticated → renders "Go to App" link to `/loops`; no login/register links
    - _Requirements: 6.1–6.5_

  - [x] 3.2 Write property test for `LandingNavbarComponent` (Property 4)
    - **Property 4: Nav bar shows correct links based on auth state**
    - For both `isAuthenticated = true` and `false`, assert correct links are present/absent
    - **Validates: Requirements 6.5**

- [x] 4. Implement `HeroSectionComponent`
  - Create `ui/src/app/components/landing-page/hero-section/hero-section.component.ts` as a standalone component
  - Static content: headline, subheadline, primary CTA to `/register`, secondary login link to `/login`
  - _Requirements: 2.1, 2.2, 2.3, 2.4, 2.5_

  - [x] 4.1 Write unit tests for `HeroSectionComponent`
    - Test: renders headline, subheadline, primary CTA linking to `/register`, secondary link to `/login`
    - _Requirements: 2.1–2.5_

- [x] 5. Implement `FeaturesSectionComponent`
  - Create `ui/src/app/components/landing-page/features-section/features-section.component.ts` as a standalone component
  - Define `FeatureHighlight` interface (`icon`, `title`, `description`) in the component file
  - Static `features` array with at least 3 entries: Trusted Loops, Lend & Borrow Items, Owner-Controlled Approvals
  - Render each feature as a card using `*ngFor`
  - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5_

  - [x] 5.1 Write unit tests for `FeaturesSectionComponent`
    - Test: renders at least 3 feature cards; each card has a title and description
    - Test: loop-based sharing, item lending, and owner approval features are present
    - _Requirements: 3.1–3.5_

  - [x] 5.2 Write property test for `FeaturesSectionComponent` (Property 1)
    - **Property 1: Features array always renders >= 3 highlights**
    - Generate `FeatureHighlight` arrays of length >= 3; assert rendered count equals array length
    - **Validates: Requirements 3.2**

- [x] 6. Implement `HowItWorksSectionComponent`
  - Create `ui/src/app/components/landing-page/how-it-works-section/how-it-works-section.component.ts` as a standalone component
  - Define `HowItWorksStep` interface (`stepNumber`, `title`, `description`) in the component file
  - Static `steps` array with at least 3 entries: Create an Account, Join or Create a Loop, Start Sharing
  - Render each step with its number, title, and description using `*ngFor`
  - _Requirements: 4.1, 4.2, 4.3_

  - [x] 6.1 Write unit tests for `HowItWorksSectionComponent`
    - Test: renders at least 3 steps; each step shows a step number, title, and description
    - _Requirements: 4.1–4.3_

  - [x] 6.2 Write property test for `HowItWorksSectionComponent` (Property 2)
    - **Property 2: Steps array always renders >= 3 steps**
    - Generate `HowItWorksStep` arrays of length >= 3; assert rendered count equals array length
    - **Validates: Requirements 4.2**

  - [x] 6.3 Write property test for `HowItWorksSectionComponent` (Property 3)
    - **Property 3: Every step renders with a number, title, and description**
    - Generate arbitrary `HowItWorksStep` objects; assert all three fields appear in the rendered DOM
    - **Validates: Requirements 4.3**

- [x] 7. Implement `CtaSectionComponent`
  - Create `ui/src/app/components/landing-page/cta-section/cta-section.component.ts` as a standalone component
  - Static content: motivational message and "Join LendingLoop Today" CTA button linking to `/register`
  - _Requirements: 5.1, 5.2, 5.3_

  - [x] 7.1 Write unit tests for `CtaSectionComponent`
    - Test: renders motivational message and CTA button linking to `/register`
    - _Requirements: 5.1–5.3_

- [x] 8. Implement `LandingPageComponent` and wire all sections together
  - Create `ui/src/app/components/landing-page/landing-page.component.ts` as a standalone component
  - Import and compose: `LandingNavbarComponent`, `HeroSectionComponent`, `FeaturesSectionComponent`, `HowItWorksSectionComponent`, `CtaSectionComponent`
  - Apply responsive CSS: stack columns vertically below 768px, multi-column at 768px and above; no horizontal overflow from 320px to 1920px
  - _Requirements: 1.4, 7.1, 7.2, 7.3_

  - [x] 8.1 Write unit tests for `LandingPageComponent`
    - Test: all five child section components are rendered
    - _Requirements: 1.4_

- [x] 9. Checkpoint — Ensure all tests pass
  - Run `npm test` from `/ui` and verify all tests pass. Ask the user if any questions arise.

- [x] 10. Write property-based tests file
  - Create `ui/src/app/components/landing-page/landing-page.component.property.spec.ts`
  - Configure `fast-check` with `fc.configureGlobal({ numRuns: 100 })`
  - Consolidate all four property tests (Properties 1–4) into this file, referencing the individual component implementations from tasks 3–6
  - _Requirements: 3.2, 4.2, 4.3, 6.5_

- [x] 11. Final checkpoint — Ensure all tests pass
  - Run `npm test` from `/ui` and verify all tests pass. Ask the user if any questions arise.

## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- The `authenticatedRedirectGuard` replaces the existing root redirect — the `AuthGuard` is NOT applied to `/`
- All landing page content is static; no API calls are made
- Use `fast-check` for property-based tests (already available in the project)
- Property tests are consolidated in `landing-page.component.property.spec.ts` but reference individual components
