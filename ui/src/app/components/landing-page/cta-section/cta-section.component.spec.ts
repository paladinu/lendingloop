import { ComponentFixture, TestBed } from '@angular/core/testing';
import { RouterModule } from '@angular/router';
import { CtaSectionComponent } from './cta-section.component';

describe('CtaSectionComponent', () => {
  let component: CtaSectionComponent;
  let fixture: ComponentFixture<CtaSectionComponent>;

  beforeEach(async () => {
    //arrange
    await TestBed.configureTestingModule({
      imports: [CtaSectionComponent, RouterModule.forRoot([])]
    }).compileComponents();

    fixture = TestBed.createComponent(CtaSectionComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should render the motivational message', () => {
    //arrange
    const compiled = fixture.nativeElement as HTMLElement;

    //act
    const message = compiled.querySelector('.cta-message');

    //assert
    expect(message).not.toBeNull();
    expect(message?.textContent?.trim()).toBe('Ready to start sharing with your community?');
  });

  it('should render the CTA button with correct text linking to /register', () => {
    //arrange
    const compiled = fixture.nativeElement as HTMLElement;

    //act
    const ctaButton = compiled.querySelector('.cta-button');

    //assert
    expect(ctaButton).not.toBeNull();
    expect(ctaButton?.textContent?.trim()).toBe('Join LendingLoop Today');
    expect(ctaButton?.getAttribute('href')).toBe('/register');
  });
});
