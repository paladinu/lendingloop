import { ComponentFixture, TestBed } from '@angular/core/testing';
import { RouterModule } from '@angular/router';
import { LandingNavbarComponent } from './landing-navbar.component';
import { AuthService } from '../../../services/auth.service';

describe('LandingNavbarComponent', () => {
  let component: LandingNavbarComponent;
  let fixture: ComponentFixture<LandingNavbarComponent>;
  let mockAuthService: { isAuthenticated: jest.Mock };

  function createComponent(isAuthenticated: boolean): void {
    mockAuthService = {
      isAuthenticated: jest.fn().mockReturnValue(isAuthenticated)
    };

    TestBed.configureTestingModule({
      imports: [
        LandingNavbarComponent,
        RouterModule.forRoot([])
      ],
      providers: [
        { provide: AuthService, useValue: mockAuthService }
      ]
    });

    fixture = TestBed.createComponent(LandingNavbarComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  }

  afterEach(() => {
    TestBed.resetTestingModule();
  });

  describe('unauthenticated user', () => {
    beforeEach(() => {
      createComponent(false);
    });

    it('should render the brand name', () => {
      //arrange
      const compiled = fixture.nativeElement as HTMLElement;

      //act
      const brandEl = compiled.querySelector('.brand-name');

      //assert
      expect(brandEl).toBeTruthy();
      expect(brandEl?.textContent).toContain('LendingLoop');
    });

    it('should render a login link', () => {
      //arrange
      const compiled = fixture.nativeElement as HTMLElement;

      //act
      const loginLink = compiled.querySelector('.login-link') as HTMLAnchorElement;

      //assert
      expect(loginLink).toBeTruthy();
      expect(loginLink.getAttribute('href')).toBe('/login');
    });

    it('should render a register CTA link', () => {
      //arrange
      const compiled = fixture.nativeElement as HTMLElement;

      //act
      const registerLink = compiled.querySelector('.register-cta') as HTMLAnchorElement;

      //assert
      expect(registerLink).toBeTruthy();
      expect(registerLink.getAttribute('href')).toBe('/register');
    });

    it('should NOT render the "Go to App" link', () => {
      //arrange
      const compiled = fixture.nativeElement as HTMLElement;

      //act
      const goToAppLink = compiled.querySelector('.go-to-app-link');

      //assert
      expect(goToAppLink).toBeNull();
    });
  });

  describe('authenticated user', () => {
    beforeEach(() => {
      createComponent(true);
    });

    it('should render a "Go to App" link pointing to /loops', () => {
      //arrange
      const compiled = fixture.nativeElement as HTMLElement;

      //act
      const goToAppLink = compiled.querySelector('.go-to-app-link') as HTMLAnchorElement;

      //assert
      expect(goToAppLink).toBeTruthy();
      expect(goToAppLink.getAttribute('href')).toBe('/loops');
    });

    it('should NOT render the login link', () => {
      //arrange
      const compiled = fixture.nativeElement as HTMLElement;

      //act
      const loginLink = compiled.querySelector('.login-link');

      //assert
      expect(loginLink).toBeNull();
    });

    it('should NOT render the register CTA link', () => {
      //arrange
      const compiled = fixture.nativeElement as HTMLElement;

      //act
      const registerLink = compiled.querySelector('.register-cta');

      //assert
      expect(registerLink).toBeNull();
    });

    it('should still render the brand name', () => {
      //arrange
      const compiled = fixture.nativeElement as HTMLElement;

      //act
      const brandEl = compiled.querySelector('.brand-name');

      //assert
      expect(brandEl).toBeTruthy();
      expect(brandEl?.textContent).toContain('LendingLoop');
    });
  });
});
