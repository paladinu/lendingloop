import { ComponentFixture, TestBed } from '@angular/core/testing';
import { BadgeDisplayComponent } from './badge-display.component';
import { BadgeAward } from '../../models/auth.interface';
import { LoopScoreService } from '../../services/loop-score.service';
import { provideHttpClient } from '@angular/common/http';

describe('BadgeDisplayComponent', () => {
    let component: BadgeDisplayComponent;
    let fixture: ComponentFixture<BadgeDisplayComponent>;
    let mockLoopScoreService: any;

    beforeEach(async () => {
        mockLoopScoreService = {
            getAllBadgeMetadata: jest.fn().mockReturnValue([
                { badgeType: 'Bronze', name: 'Bronze Badge', description: 'Awarded for reaching 10 points', category: 'milestone', requirement: 'Reach 10 points', icon: '🏆' },
                { badgeType: 'Silver', name: 'Silver Badge', description: 'Awarded for reaching 50 points', category: 'milestone', requirement: 'Reach 50 points', icon: '🏆' },
                { badgeType: 'Gold', name: 'Gold Badge', description: 'Awarded for reaching 100 points', category: 'milestone', requirement: 'Reach 100 points', icon: '🏆' },
                { badgeType: 'FirstLend', name: 'First Lend', description: 'Complete your first lending transaction', category: 'achievement', requirement: 'Lend an item for the first time', icon: '🎁' },
                { badgeType: 'ReliableBorrower', name: 'Reliable Borrower', description: 'Return items on time consistently', category: 'achievement', requirement: 'Complete 10 on-time returns', icon: '⭐' },
                { badgeType: 'GenerousLender', name: 'Generous Lender', description: 'Share your items frequently', category: 'achievement', requirement: 'Complete 50 lending transactions', icon: '🤝' },
                { badgeType: 'PerfectRecord', name: 'Perfect Record', description: 'Maintain a perfect return streak', category: 'achievement', requirement: 'Complete 25 consecutive on-time returns', icon: '💯' },
                { badgeType: 'CommunityBuilder', name: 'Community Builder', description: 'Grow the LendingLoop community', category: 'achievement', requirement: 'Invite 10 users who become active', icon: '🌟' }
            ])
        };

        await TestBed.configureTestingModule({
            imports: [BadgeDisplayComponent],
            providers: [
                provideHttpClient(),
                { provide: LoopScoreService, useValue: mockLoopScoreService }
            ]
        }).compileComponents();

        fixture = TestBed.createComponent(BadgeDisplayComponent);
        component = fixture.componentInstance;
        component.showAllBadges = false; // Default to false to avoid calling getAllBadgeMetadata in most tests
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
        component.earnedBadges = mockBadges;
        component.showAllBadges = false;

        //act
        component.ngOnInit();
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
        component.earnedBadges = mockBadges;
        component.showAllBadges = false;

        //act
        component.ngOnInit();
        fixture.detectChanges();

        //assert
        const badgeElements = fixture.nativeElement.querySelectorAll('.badge-icon');
        expect(badgeElements[0].classList.contains('bronze')).toBe(true);
        expect(badgeElements[1].classList.contains('silver')).toBe(true);
        expect(badgeElements[2].classList.contains('gold')).toBe(true);
    });

    it('should show empty state when no badges', () => {
        //arrange
        component.earnedBadges = [];
        component.showAllBadges = false;

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
        component.earnedBadges = mockBadges;
        component.showAllBadges = false;

        //act
        component.ngOnInit();
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
        component.earnedBadges = mockBadges;
        component.showAllBadges = false;

        //act
        component.ngOnInit();
        fixture.detectChanges();

        //assert
        const badgeTitle = fixture.nativeElement.querySelector('.badge-title');
        const badgeDescription = fixture.nativeElement.querySelector('.badge-description');
        const badgeDate = fixture.nativeElement.querySelector('.badge-date');
        
        expect(badgeTitle.textContent).toContain('Bronze Badge');
        expect(badgeDescription.textContent).toContain('Earned by reaching 10 points');
        expect(badgeDate.textContent).toContain('Earned');
    });

    it('should categorize badges into milestone and achievement sections correctly', () => {
        //arrange
        const mockBadges: BadgeAward[] = [
            { badgeType: 'Bronze', awardedAt: new Date().toISOString() },
            { badgeType: 'FirstLend', awardedAt: new Date().toISOString() },
            { badgeType: 'Silver', awardedAt: new Date().toISOString() },
            { badgeType: 'ReliableBorrower', awardedAt: new Date().toISOString() }
        ];
        component.earnedBadges = mockBadges;
        component.showAllBadges = false;

        //act
        component.ngOnInit();

        //assert
        expect(component.milestoneBadges.length).toBe(2);
        expect(component.achievementBadges.length).toBe(2);
        expect(component.milestoneBadges[0].badgeType).toBe('Bronze');
        expect(component.milestoneBadges[1].badgeType).toBe('Silver');
        expect(component.achievementBadges[0].badgeType).toBe('FirstLend');
        expect(component.achievementBadges[1].badgeType).toBe('ReliableBorrower');
    });

    it('should return correct icons for achievement badges', () => {
        //arrange & act
        const firstLendIcon = component.getBadgeIcon('FirstLend');
        const reliableBorrowerIcon = component.getBadgeIcon('ReliableBorrower');

        //assert
        expect(firstLendIcon).toBe('🎁');
        expect(reliableBorrowerIcon).toBe('⭐');
    });

    it('should return correct labels for achievement badges', () => {
        //arrange & act
        const firstLendLabel = component.getBadgeLabel('FirstLend');
        const reliableBorrowerLabel = component.getBadgeLabel('ReliableBorrower');

        //assert
        expect(firstLendLabel).toBe('First Lend');
        expect(reliableBorrowerLabel).toBe('Reliable Borrower');
    });

    it('should display achievement badges with correct CSS classes', () => {
        //arrange
        const mockBadges: BadgeAward[] = [
            { badgeType: 'FirstLend', awardedAt: new Date().toISOString() },
            { badgeType: 'ReliableBorrower', awardedAt: new Date().toISOString() }
        ];
        component.earnedBadges = mockBadges;
        component.showAllBadges = false;

        //act
        component.ngOnInit();
        fixture.detectChanges();

        //assert
        const achievementBadges = fixture.nativeElement.querySelectorAll('.badge-icon.achievement');
        expect(achievementBadges.length).toBe(2);
        expect(achievementBadges[0].classList.contains('first-lend')).toBe(true);
        expect(achievementBadges[1].classList.contains('reliable-borrower')).toBe(true);
    });

    it('should show achievement badge section only when achievement badges exist', () => {
        //arrange
        const mockBadges: BadgeAward[] = [
            { badgeType: 'Bronze', awardedAt: new Date().toISOString() },
            { badgeType: 'FirstLend', awardedAt: new Date().toISOString() }
        ];
        component.earnedBadges = mockBadges;
        component.showAllBadges = false;

        //act
        component.ngOnInit();
        fixture.detectChanges();

        //assert
        const achievementSection = fixture.nativeElement.querySelector('.achievement-badges');
        expect(achievementSection).toBeTruthy();
        
        const sectionTitle = achievementSection.querySelector('.section-title');
        expect(sectionTitle.textContent).toContain('Achievement Badges');
    });

    it('should not show achievement badge section when no achievement badges exist', () => {
        //arrange
        const mockBadges: BadgeAward[] = [
            { badgeType: 'Bronze', awardedAt: new Date().toISOString() },
            { badgeType: 'Silver', awardedAt: new Date().toISOString() }
        ];
        component.earnedBadges = mockBadges;
        component.showAllBadges = false;

        //act
        component.ngOnInit();
        fixture.detectChanges();

        //assert
        const achievementSection = fixture.nativeElement.querySelector('.achievement-badges');
        expect(achievementSection).toBeFalsy();
    });

    it('should return correct badge description for FirstLend', () => {
        //arrange & act
        const description = component.getBadgeDescription('FirstLend');

        //assert
        expect(description).toBe('First Lend - Completed your first lending transaction');
    });

    it('should return correct badge description for ReliableBorrower', () => {
        //arrange & act
        const description = component.getBadgeDescription('ReliableBorrower');

        //assert
        expect(description).toBe('Reliable Borrower - Completed 10 on-time returns');
    });

    it('should return correct badge requirement for FirstLend', () => {
        //arrange & act
        const requirement = component.getBadgeRequirement('FirstLend');

        //assert
        expect(requirement).toBe('Earned by completing your first lend');
    });

    it('should return correct badge requirement for ReliableBorrower', () => {
        //arrange & act
        const requirement = component.getBadgeRequirement('ReliableBorrower');

        //assert
        expect(requirement).toBe('Earned by completing 10 on-time returns');
    });

    it('should return correct icons for new achievement badges', () => {
        //arrange & act
        const generousLenderIcon = component.getBadgeIcon('GenerousLender');
        const perfectRecordIcon = component.getBadgeIcon('PerfectRecord');
        const communityBuilderIcon = component.getBadgeIcon('CommunityBuilder');

        //assert
        expect(generousLenderIcon).toBe('🤝');
        expect(perfectRecordIcon).toBe('💯');
        expect(communityBuilderIcon).toBe('🌟');
    });

    it('should return correct labels for new achievement badges', () => {
        //arrange & act
        const generousLenderLabel = component.getBadgeLabel('GenerousLender');
        const perfectRecordLabel = component.getBadgeLabel('PerfectRecord');
        const communityBuilderLabel = component.getBadgeLabel('CommunityBuilder');

        //assert
        expect(generousLenderLabel).toBe('Generous Lender');
        expect(perfectRecordLabel).toBe('Perfect Record');
        expect(communityBuilderLabel).toBe('Community Builder');
    });

    it('should categorize new achievement badges correctly', () => {
        //arrange
        const mockBadges: BadgeAward[] = [
            { badgeType: 'Bronze', awardedAt: new Date().toISOString() },
            { badgeType: 'GenerousLender', awardedAt: new Date().toISOString() },
            { badgeType: 'PerfectRecord', awardedAt: new Date().toISOString() },
            { badgeType: 'CommunityBuilder', awardedAt: new Date().toISOString() }
        ];
        component.earnedBadges = mockBadges;
        component.showAllBadges = false;

        //act
        component.ngOnInit();

        //assert
        expect(component.milestoneBadges.length).toBe(1);
        expect(component.achievementBadges.length).toBe(3);
        expect(component.achievementBadges.map(b => b.badgeType)).toContain('GenerousLender');
        expect(component.achievementBadges.map(b => b.badgeType)).toContain('PerfectRecord');
        expect(component.achievementBadges.map(b => b.badgeType)).toContain('CommunityBuilder');
    });

    it('should display new achievement badges with correct CSS classes', () => {
        //arrange
        const mockBadges: BadgeAward[] = [
            { badgeType: 'GenerousLender', awardedAt: new Date().toISOString() },
            { badgeType: 'PerfectRecord', awardedAt: new Date().toISOString() },
            { badgeType: 'CommunityBuilder', awardedAt: new Date().toISOString() }
        ];
        component.earnedBadges = mockBadges;
        component.showAllBadges = false;

        //act
        component.ngOnInit();
        fixture.detectChanges();

        //assert
        const achievementBadges = fixture.nativeElement.querySelectorAll('.badge-icon.achievement');
        expect(achievementBadges.length).toBe(3);
        expect(achievementBadges[0].classList.contains('generous-lender')).toBe(true);
        expect(achievementBadges[1].classList.contains('perfect-record')).toBe(true);
        expect(achievementBadges[2].classList.contains('community-builder')).toBe(true);
    });

    it('should return correct badge description for GenerousLender', () => {
        //arrange & act
        const description = component.getBadgeDescription('GenerousLender');

        //assert
        expect(description).toBe('Generous Lender - Completed 50 lending transactions');
    });

    it('should return correct badge description for PerfectRecord', () => {
        //arrange & act
        const description = component.getBadgeDescription('PerfectRecord');

        //assert
        expect(description).toBe('Perfect Record - 25 consecutive on-time returns');
    });

    it('should return correct badge description for CommunityBuilder', () => {
        //arrange & act
        const description = component.getBadgeDescription('CommunityBuilder');

        //assert
        expect(description).toBe('Community Builder - 10 invited users became active');
    });

    it('should return correct badge requirement for GenerousLender', () => {
        //arrange & act
        const requirement = component.getBadgeRequirement('GenerousLender');

        //assert
        expect(requirement).toBe('Earned by completing 50 lending transactions');
    });

    it('should return correct badge requirement for PerfectRecord', () => {
        //arrange & act
        const requirement = component.getBadgeRequirement('PerfectRecord');

        //assert
        expect(requirement).toBe('Earned by achieving 25 consecutive on-time returns');
    });

    it('should return correct badge requirement for CommunityBuilder', () => {
        //arrange & act
        const requirement = component.getBadgeRequirement('CommunityBuilder');

        //assert
        expect(requirement).toBe('Earned when 10 invited users become active');
    });

    // Tests for displaying all badges (earned and unearned) - Requirement 12
    it('should call loopScoreService.getAllBadgeMetadata() in ngOnInit when showAllBadges is true', () => {
        //arrange
        component.showAllBadges = true;
        const spy = jest.spyOn(component['loopScoreService'], 'getAllBadgeMetadata');

        //act
        component.ngOnInit();

        //assert
        expect(spy).toHaveBeenCalled();
        expect(component.allBadgeMetadata.length).toBe(8);
    });

    it('should populate allBadgeMetadata with badge data', () => {
        //arrange
        component.showAllBadges = true;

        //act
        component.ngOnInit();

        //assert
        expect(component.allBadgeMetadata).toBeDefined();
        expect(component.allBadgeMetadata.length).toBe(8);
        expect(component.allBadgeMetadata[0].badgeType).toBe('Bronze');
    });

    it('should create DisplayBadge for each badge metadata in prepareDisplayBadges', () => {
        //arrange
        component.earnedBadges = [
            { badgeType: 'Bronze', awardedAt: new Date().toISOString() }
        ];
        component.allBadgeMetadata = component['loopScoreService'].getAllBadgeMetadata();

        //act
        component.prepareDisplayBadges();

        //assert
        expect(component.displayBadges.length).toBe(8);
    });

    it('should set earned flag to true when badge is in earnedBadges array', () => {
        //arrange
        const awardedDate = new Date().toISOString();
        component.earnedBadges = [
            { badgeType: 'Bronze', awardedAt: awardedDate }
        ];
        component.allBadgeMetadata = component['loopScoreService'].getAllBadgeMetadata();

        //act
        component.prepareDisplayBadges();

        //assert
        const bronzeBadge = component.displayBadges.find(b => b.metadata.badgeType === 'Bronze');
        expect(bronzeBadge?.earned).toBe(true);
        expect(bronzeBadge?.awardedAt).toBe(awardedDate);
    });

    it('should set earned flag to false when badge is not in earnedBadges array', () => {
        //arrange
        component.earnedBadges = [
            { badgeType: 'Bronze', awardedAt: new Date().toISOString() }
        ];
        component.allBadgeMetadata = component['loopScoreService'].getAllBadgeMetadata();

        //act
        component.prepareDisplayBadges();

        //assert
        const silverBadge = component.displayBadges.find(b => b.metadata.badgeType === 'Silver');
        expect(silverBadge?.earned).toBe(false);
        expect(silverBadge?.awardedAt).toBeUndefined();
    });

    it('should set awardedAt correctly for earned badges', () => {
        //arrange
        const awardedDate = new Date('2024-01-15T10:00:00Z').toISOString();
        component.earnedBadges = [
            { badgeType: 'Bronze', awardedAt: awardedDate },
            { badgeType: 'FirstLend', awardedAt: awardedDate }
        ];
        component.allBadgeMetadata = component['loopScoreService'].getAllBadgeMetadata();

        //act
        component.prepareDisplayBadges();

        //assert
        const bronzeBadge = component.displayBadges.find(b => b.metadata.badgeType === 'Bronze');
        const firstLendBadge = component.displayBadges.find(b => b.metadata.badgeType === 'FirstLend');
        expect(bronzeBadge?.awardedAt).toBe(awardedDate);
        expect(firstLendBadge?.awardedAt).toBe(awardedDate);
    });

    it('should set awardedAt to undefined for unearned badges', () => {
        //arrange
        component.earnedBadges = [];
        component.allBadgeMetadata = component['loopScoreService'].getAllBadgeMetadata();

        //act
        component.prepareDisplayBadges();

        //assert
        component.displayBadges.forEach(badge => {
            expect(badge.awardedAt).toBeUndefined();
        });
    });

    it('should display all 8 badges when showAllBadges is true', () => {
        //arrange
        component.showAllBadges = true;
        component.earnedBadges = [
            { badgeType: 'Bronze', awardedAt: new Date().toISOString() }
        ];

        //act
        component.ngOnInit();
        fixture.detectChanges();

        //assert
        const badgeItems = fixture.nativeElement.querySelectorAll('.badge-item');
        expect(badgeItems.length).toBe(8);
    });

    it('should apply earned CSS class to earned badges', () => {
        //arrange
        component.showAllBadges = true;
        component.earnedBadges = [
            { badgeType: 'Bronze', awardedAt: new Date().toISOString() }
        ];

        //act
        component.ngOnInit();
        fixture.detectChanges();

        //assert
        const badgeItems = fixture.nativeElement.querySelectorAll('.badge-item');
        const earnedBadges = Array.from(badgeItems).filter((item: any) => item.classList.contains('earned'));
        expect(earnedBadges.length).toBe(1);
    });

    it('should apply unearned CSS class to unearned badges', () => {
        //arrange
        component.showAllBadges = true;
        component.earnedBadges = [
            { badgeType: 'Bronze', awardedAt: new Date().toISOString() }
        ];

        //act
        component.ngOnInit();
        fixture.detectChanges();

        //assert
        const badgeItems = fixture.nativeElement.querySelectorAll('.badge-item');
        const unearnedBadges = Array.from(badgeItems).filter((item: any) => item.classList.contains('unearned'));
        expect(unearnedBadges.length).toBe(7);
    });

    it('should show requirement text for unearned badges', () => {
        //arrange
        component.showAllBadges = true;
        component.earnedBadges = [];

        //act
        component.ngOnInit();
        fixture.detectChanges();

        //assert
        const requirements = fixture.nativeElement.querySelectorAll('.badge-requirement');
        expect(requirements.length).toBe(8);
    });

    it('should show earned date for earned badges', () => {
        //arrange
        component.showAllBadges = true;
        component.earnedBadges = [
            { badgeType: 'Bronze', awardedAt: new Date().toISOString() }
        ];

        //act
        component.ngOnInit();
        fixture.detectChanges();

        //assert
        const earnedDates = fixture.nativeElement.querySelectorAll('.badge-earned-date');
        expect(earnedDates.length).toBe(1);
    });

    it('should not show requirement text for earned badges', () => {
        //arrange
        component.showAllBadges = true;
        component.earnedBadges = [
            { badgeType: 'Bronze', awardedAt: new Date().toISOString() }
        ];

        //act
        component.ngOnInit();
        fixture.detectChanges();

        //assert
        const requirements = fixture.nativeElement.querySelectorAll('.badge-requirement');
        expect(requirements.length).toBe(7); // 7 unearned badges
    });

    it('should not show earned date for unearned badges', () => {
        //arrange
        component.showAllBadges = true;
        component.earnedBadges = [];

        //act
        component.ngOnInit();
        fixture.detectChanges();

        //assert
        const earnedDates = fixture.nativeElement.querySelectorAll('.badge-earned-date');
        expect(earnedDates.length).toBe(0);
    });

    it('should include badge name and earned status in aria-label for earned badges', () => {
        //arrange
        component.showAllBadges = true;
        const awardedDate = new Date('2024-01-15T10:00:00Z').toISOString();
        component.earnedBadges = [
            { badgeType: 'Bronze', awardedAt: awardedDate }
        ];

        //act
        component.ngOnInit();
        fixture.detectChanges();

        //assert
        const badgeItems = fixture.nativeElement.querySelectorAll('.badge-item.earned');
        expect(badgeItems.length).toBe(1);
        const ariaLabel = badgeItems[0].getAttribute('aria-label');
        expect(ariaLabel).toContain('Bronze Badge');
        expect(ariaLabel).toContain('Earned on');
    });

    it('should include badge name and requirement in aria-label for unearned badges', () => {
        //arrange
        component.showAllBadges = true;
        component.earnedBadges = [];

        //act
        component.ngOnInit();
        fixture.detectChanges();

        //assert
        const badgeItems = fixture.nativeElement.querySelectorAll('.badge-item.unearned');
        expect(badgeItems.length).toBe(8);
        const ariaLabel = badgeItems[0].getAttribute('aria-label');
        expect(ariaLabel).toContain('Bronze Badge');
        expect(ariaLabel).toContain('Reach 10 points');
    });
});
