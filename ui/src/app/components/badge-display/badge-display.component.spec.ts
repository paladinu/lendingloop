import { ComponentFixture, TestBed } from '@angular/core/testing';
import { BadgeDisplayComponent } from './badge-display.component';
import { BadgeAward } from '../../models/auth.interface';

describe('BadgeDisplayComponent', () => {
    let component: BadgeDisplayComponent;
    let fixture: ComponentFixture<BadgeDisplayComponent>;

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            imports: [BadgeDisplayComponent]
        }).compileComponents();

        fixture = TestBed.createComponent(BadgeDisplayComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });

    it('should display all earned badges', () => {
        //arrange
        const mockBadges: BadgeAward[] = [
            { badgeType: 'Bronze', awardedAt: new Date().toISOString() },
            { badgeType: 'Silver', awardedAt: new Date().toISOString() },
            { badgeType: 'Gold', awardedAt: new Date().toISOString() }
        ];
        component.badges = mockBadges;

        //act
        fixture.detectChanges();

        //assert
        const badgeElements = fixture.nativeElement.querySelectorAll('.badge-icon');
        expect(badgeElements.length).toBe(3);
    });

    it('should apply correct CSS class for each badge type', () => {
        //arrange
        const mockBadges: BadgeAward[] = [
            { badgeType: 'Bronze', awardedAt: new Date().toISOString() },
            { badgeType: 'Silver', awardedAt: new Date().toISOString() },
            { badgeType: 'Gold', awardedAt: new Date().toISOString() }
        ];
        component.badges = mockBadges;

        //act
        fixture.detectChanges();

        //assert
        const badgeElements = fixture.nativeElement.querySelectorAll('.badge-icon');
        expect(badgeElements[0].classList.contains('bronze')).toBe(true);
        expect(badgeElements[1].classList.contains('silver')).toBe(true);
        expect(badgeElements[2].classList.contains('gold')).toBe(true);
    });

    it('should show empty state when no badges', () => {
        //arrange
        component.badges = [];

        //act
        fixture.detectChanges();

        //assert
        const emptyState = fixture.nativeElement.querySelector('.empty-state');
        expect(emptyState).toBeTruthy();
        expect(emptyState.textContent).toContain('No badges earned yet');
    });

    it('should set aria-label correctly with badge description', () => {
        //arrange
        const testDate = new Date('2024-01-15T10:00:00Z');
        const mockBadges: BadgeAward[] = [
            { badgeType: 'Bronze', awardedAt: testDate.toISOString() }
        ];
        component.badges = mockBadges;

        //act
        fixture.detectChanges();

        //assert
        const badgeElement = fixture.nativeElement.querySelector('.badge-icon');
        const ariaLabel = badgeElement.getAttribute('aria-label');
        expect(ariaLabel).toBe('Bronze Badge - Reached 10 LoopScore points');
    });

    it('should return correct badge description for Bronze', () => {
        //arrange & act
        const description = component.getBadgeDescription('Bronze');

        //assert
        expect(description).toBe('Bronze Badge - Reached 10 LoopScore points');
    });

    it('should return correct badge description for Silver', () => {
        //arrange & act
        const description = component.getBadgeDescription('Silver');

        //assert
        expect(description).toBe('Silver Badge - Reached 50 LoopScore points');
    });

    it('should return correct badge description for Gold', () => {
        //arrange & act
        const description = component.getBadgeDescription('Gold');

        //assert
        expect(description).toBe('Gold Badge - Reached 100 LoopScore points');
    });

    it('should return correct badge requirement for Bronze', () => {
        //arrange & act
        const requirement = component.getBadgeRequirement('Bronze');

        //assert
        expect(requirement).toBe('Earned by reaching 10 points');
    });

    it('should return correct badge requirement for Silver', () => {
        //arrange & act
        const requirement = component.getBadgeRequirement('Silver');

        //assert
        expect(requirement).toBe('Earned by reaching 50 points');
    });

    it('should return correct badge requirement for Gold', () => {
        //arrange & act
        const requirement = component.getBadgeRequirement('Gold');

        //assert
        expect(requirement).toBe('Earned by reaching 100 points');
    });

    it('should display badge details including title, description, and date', () => {
        //arrange
        const testDate = new Date('2024-01-15T10:00:00Z');
        const mockBadges: BadgeAward[] = [
            { badgeType: 'Bronze', awardedAt: testDate.toISOString() }
        ];
        component.badges = mockBadges;

        //act
        fixture.detectChanges();

        //assert
        const badgeTitle = fixture.nativeElement.querySelector('.badge-title');
        const badgeDescription = fixture.nativeElement.querySelector('.badge-description');
        const badgeDate = fixture.nativeElement.querySelector('.badge-date');
        
        expect(badgeTitle.textContent).toContain('Bronze Badge');
        expect(badgeDescription.textContent).toContain('Earned by reaching 10 points');
        expect(badgeDate.textContent).toContain('Earned');
    });
});
