import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HowItWorksSectionComponent } from './how-it-works-section.component';

describe('HowItWorksSectionComponent', () => {
  let fixture: ComponentFixture<HowItWorksSectionComponent>;

  beforeEach(async () => {
    //arrange
    await TestBed.configureTestingModule({
      imports: [HowItWorksSectionComponent]
    }).compileComponents();

    fixture = TestBed.createComponent(HowItWorksSectionComponent);
    fixture.detectChanges();
  });

  it('should render at least 3 steps', () => {
    //arrange
    const compiled = fixture.nativeElement as HTMLElement;

    //act
    const steps = compiled.querySelectorAll('.how-it-works-step');

    //assert
    expect(steps.length).toBeGreaterThanOrEqual(3);
  });

  it('should render a step number, title, and description in each step', () => {
    //arrange
    const compiled = fixture.nativeElement as HTMLElement;

    //act
    const steps = compiled.querySelectorAll('.how-it-works-step');

    //assert
    steps.forEach(step => {
      const stepNumber = step.querySelector('.step-number');
      const stepTitle = step.querySelector('.step-title');
      const stepDescription = step.querySelector('.step-description');

      expect(stepNumber).not.toBeNull();
      expect(stepNumber?.textContent?.trim().length).toBeGreaterThan(0);

      expect(stepTitle).not.toBeNull();
      expect(stepTitle?.textContent?.trim().length).toBeGreaterThan(0);

      expect(stepDescription).not.toBeNull();
      expect(stepDescription?.textContent?.trim().length).toBeGreaterThan(0);
    });
  });
});
