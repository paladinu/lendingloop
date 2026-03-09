import { ComponentFixture, TestBed } from '@angular/core/testing';
import { RouterModule } from '@angular/router';
import { HeroSectionComponent } from './hero-section.component';

describe('HeroSectionComponent', () => {
  let component: HeroSectionComponent;
  let fixture: ComponentFixture<HeroSectionComponent>;

  beforeEach(async () => {
    //arrange
    await TestBed.configureTestingModule({
      imports: [HeroSectionComponent, RouterModule.forRoot([])]
    }).compileComponents();

    fixture = TestBed.createComponent(HeroSectionComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should render the headline text', () => {
    //arrange
    const compiled = fixture.nativeElement as HTMLElement;

    //act
    const headline = compiled.querySelector('.hero-headline');

    //assert
    expect(headline).not.toBeNull();
    expect(headline?.textContent).toContain('Share More. Buy Less. Build Community.');
  });

  it('should render the subheadline text', () => {
    //arrange
    const compiled = fixture.nativeElement as HTMLElement;

    //act
    const subheadline = compiled.querySelector('.hero-subheadline');

    //assert
    expect(subheadline).not.toBeNull();
    expect(subheadline?.textContent).toContain('LendingLoop lets you borrow and lend tools and items within trusted groups called loops.');
  });

  it('should render the primary CTA linking to /register', () => {
    //arrange
    const compiled = fixture.nativeElement as HTMLElement;

    //act
    const ctaPrimary = compiled.querySelector('.cta-primary') as HTMLAnchorElement;

    //assert
    expect(ctaPrimary).not.toBeNull();
    expect(ctaPrimary?.textContent?.trim()).toBe('Get Started Free');
    expect(ctaPrimary?.getAttribute('href')).toBe('/register');
  });

  it('should render the secondary link to /login', () => {
    //arrange
    const compiled = fixture.nativeElement as HTMLElement;

    //act
    const ctaSecondary = compiled.querySelector('.cta-secondary') as HTMLAnchorElement;

    //assert
    expect(ctaSecondary).not.toBeNull();
    expect(ctaSecondary?.textContent?.trim()).toBe('Already have an account? Log in');
    expect(ctaSecondary?.getAttribute('href')).toBe('/login');
  });
});
