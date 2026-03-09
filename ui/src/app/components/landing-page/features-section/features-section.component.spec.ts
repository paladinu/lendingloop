import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FeaturesSectionComponent } from './features-section.component';

describe('FeaturesSectionComponent', () => {
  let component: FeaturesSectionComponent;
  let fixture: ComponentFixture<FeaturesSectionComponent>;

  beforeEach(async () => {
    //arrange
    await TestBed.configureTestingModule({
      imports: [FeaturesSectionComponent]
    }).compileComponents();

    fixture = TestBed.createComponent(FeaturesSectionComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should render at least 3 feature cards', () => {
    //arrange
    const compiled = fixture.nativeElement as HTMLElement;

    //act
    const cards = compiled.querySelectorAll('.feature-card');

    //assert
    expect(cards.length).toBeGreaterThanOrEqual(3);
  });

  it('should render a title and description in each card', () => {
    //arrange
    const compiled = fixture.nativeElement as HTMLElement;

    //act
    const cards = compiled.querySelectorAll('.feature-card');

    //assert
    cards.forEach(card => {
      const title = card.querySelector('.feature-title');
      const description = card.querySelector('.feature-description');
      expect(title).not.toBeNull();
      expect(title?.textContent?.trim().length).toBeGreaterThan(0);
      expect(description).not.toBeNull();
      expect(description?.textContent?.trim().length).toBeGreaterThan(0);
    });
  });

  it('should include the "Trusted Loops" feature', () => {
    //arrange
    const compiled = fixture.nativeElement as HTMLElement;

    //act
    const titles = Array.from(compiled.querySelectorAll('.feature-title')).map(el => el.textContent?.trim());

    //assert
    expect(titles).toContain('Trusted Loops');
  });

  it('should include the "Lend & Borrow Items" feature', () => {
    //arrange
    const compiled = fixture.nativeElement as HTMLElement;

    //act
    const titles = Array.from(compiled.querySelectorAll('.feature-title')).map(el => el.textContent?.trim());

    //assert
    expect(titles).toContain('Lend & Borrow Items');
  });

  it('should include the "Owner-Controlled Approvals" feature', () => {
    //arrange
    const compiled = fixture.nativeElement as HTMLElement;

    //act
    const titles = Array.from(compiled.querySelectorAll('.feature-title')).map(el => el.textContent?.trim());

    //assert
    expect(titles).toContain('Owner-Controlled Approvals');
  });
});
