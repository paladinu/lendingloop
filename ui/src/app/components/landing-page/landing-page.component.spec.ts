import { ComponentFixture, TestBed } from '@angular/core/testing';
import { RouterTestingModule } from '@angular/router/testing';
import { LandingPageComponent } from './landing-page.component';
import { AuthService } from '../../services/auth.service';

describe('LandingPageComponent', () => {
  let component: LandingPageComponent;
  let fixture: ComponentFixture<LandingPageComponent>;
  let mockAuthService: { isAuthenticated: jest.Mock };

  beforeEach(async () => {
    //arrange
    mockAuthService = {
      isAuthenticated: jest.fn().mockReturnValue(false)
    };

    await TestBed.configureTestingModule({
      imports: [LandingPageComponent, RouterTestingModule],
      providers: [
        { provide: AuthService, useValue: mockAuthService }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(LandingPageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create the component', () => {
    //act & assert
    expect(component).toBeTruthy();
  });

  it('should render the landing navbar', () => {
    //arrange
    const compiled = fixture.nativeElement as HTMLElement;

    //act
    const navbar = compiled.querySelector('app-landing-navbar');

    //assert
    expect(navbar).not.toBeNull();
  });

  it('should render the hero section', () => {
    //arrange
    const compiled = fixture.nativeElement as HTMLElement;

    //act
    const hero = compiled.querySelector('app-hero-section');

    //assert
    expect(hero).not.toBeNull();
  });

  it('should render the features section', () => {
    //arrange
    const compiled = fixture.nativeElement as HTMLElement;

    //act
    const features = compiled.querySelector('app-features-section');

    //assert
    expect(features).not.toBeNull();
  });

  it('should render the how-it-works section', () => {
    //arrange
    const compiled = fixture.nativeElement as HTMLElement;

    //act
    const howItWorks = compiled.querySelector('app-how-it-works-section');

    //assert
    expect(howItWorks).not.toBeNull();
  });

  it('should render the cta section', () => {
    //arrange
    const compiled = fixture.nativeElement as HTMLElement;

    //act
    const cta = compiled.querySelector('app-cta-section');

    //assert
    expect(cta).not.toBeNull();
  });

  it('should render all five child sections in order', () => {
    //arrange
    const compiled = fixture.nativeElement as HTMLElement;

    //act
    const sections = compiled.querySelectorAll(
      'app-landing-navbar, app-hero-section, app-features-section, app-how-it-works-section, app-cta-section'
    );

    //assert
    expect(sections.length).toBe(5);
    expect(sections[0].tagName.toLowerCase()).toBe('app-landing-navbar');
    expect(sections[1].tagName.toLowerCase()).toBe('app-hero-section');
    expect(sections[2].tagName.toLowerCase()).toBe('app-features-section');
    expect(sections[3].tagName.toLowerCase()).toBe('app-how-it-works-section');
    expect(sections[4].tagName.toLowerCase()).toBe('app-cta-section');
  });
});
