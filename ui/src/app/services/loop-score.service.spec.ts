import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { LoopScoreService } from './loop-score.service';
import { ScoreHistoryEntry, BadgeAward } from '../models/auth.interface';
import { environment } from '../../environments/environment';

describe('LoopScoreService', () => {
    let service: LoopScoreService;
    let httpMock: HttpTestingController;

    beforeEach(() => {
        TestBed.configureTestingModule({
            imports: [HttpClientTestingModule],
            providers: [LoopScoreService]
        });
        service = TestBed.inject(LoopScoreService);
        httpMock = TestBed.inject(HttpTestingController);
    });

    afterEach(() => {
        httpMock.verify();
    });

    it('should be created', () => {
        expect(service).toBeTruthy();
    });

    it('getUserScore() should fetch user score from API', () => {
        //arrange
        const userId = 'user123';
        const expectedScore = 15;

        //act
        service.getUserScore(userId).subscribe(score => {
            //assert
            expect(score).toBe(expectedScore);
        });

        const req = httpMock.expectOne(`${environment.apiUrl}/api/users/${userId}/score`);
        expect(req.request.method).toBe('GET');
        req.flush(expectedScore);
    });

    it('getScoreHistory() should fetch score history with limit', () => {
        //arrange
        const userId = 'user123';
        const limit = 10;
        const mockHistory: ScoreHistoryEntry[] = [
            {
                timestamp: new Date().toISOString(),
                points: 1,
                actionType: 'BorrowCompleted',
                itemRequestId: 'req1',
                itemName: 'Test Item 1'
            },
            {
                timestamp: new Date().toISOString(),
                points: 4,
                actionType: 'LendApproved',
                itemRequestId: 'req2',
                itemName: 'Test Item 2'
            }
        ];

        //act
        service.getScoreHistory(userId, limit).subscribe(history => {
            //assert
            expect(history).toEqual(mockHistory);
            expect(history.length).toBe(2);
        });

        const req = httpMock.expectOne(`${environment.apiUrl}/api/users/${userId}/score-history?limit=${limit}`);
        expect(req.request.method).toBe('GET');
        req.flush(mockHistory);
    });

    it('getScoreHistory() should use default limit when not specified', () => {
        //arrange
        const userId = 'user123';
        const mockHistory: ScoreHistoryEntry[] = [];

        //act
        service.getScoreHistory(userId).subscribe(history => {
            //assert
            expect(history).toEqual(mockHistory);
        });

        const req = httpMock.expectOne(`${environment.apiUrl}/api/users/${userId}/score-history?limit=50`);
        expect(req.request.method).toBe('GET');
        req.flush(mockHistory);
    });

    it('getScoreExplanation() should return score rules object', () => {
        //act
        const rules = service.getScoreExplanation();

        //assert
        expect(rules).toBeDefined();
        expect(rules.borrowCompleted).toBe(1);
        expect(rules.onTimeReturn).toBe(1);
        expect(rules.lendApproved).toBe(4);
    });

    it('getUserBadges() should fetch user badges from API', () => {
        //arrange
        const userId = 'user123';
        const mockBadges: BadgeAward[] = [
            {
                badgeType: 'Bronze',
                awardedAt: new Date().toISOString()
            },
            {
                badgeType: 'Silver',
                awardedAt: new Date().toISOString()
            }
        ];

        //act
        service.getUserBadges(userId).subscribe(badges => {
            //assert
            expect(badges).toEqual(mockBadges);
            expect(badges.length).toBe(2);
        });

        const req = httpMock.expectOne(`${environment.apiUrl}/api/users/${userId}/badges`);
        expect(req.request.method).toBe('GET');
        req.flush(mockBadges);
    });

    it('getBadgeMilestones() should return badge milestone values', () => {
        //act
        const milestones = service.getBadgeMilestones();

        //assert
        expect(milestones).toBeDefined();
        expect(milestones.bronze).toBe(10);
        expect(milestones.silver).toBe(50);
        expect(milestones.gold).toBe(100);
    });

    it('getAllBadgeMetadata() should return metadata for all 8 badge types', () => {
        //act
        const metadata = service.getAllBadgeMetadata();

        //assert
        expect(metadata).toBeDefined();
        expect(metadata.length).toBe(8);
    });

    it('getAllBadgeMetadata() should include all required properties for each badge', () => {
        //act
        const metadata = service.getAllBadgeMetadata();

        //assert
        metadata.forEach(badge => {
            expect(badge.badgeType).toBeDefined();
            expect(badge.name).toBeDefined();
            expect(badge.description).toBeDefined();
            expect(badge.category).toBeDefined();
            expect(badge.requirement).toBeDefined();
            expect(badge.icon).toBeDefined();
        });
    });

    it('getAllBadgeMetadata() should categorize milestone badges correctly', () => {
        //act
        const metadata = service.getAllBadgeMetadata();

        //assert
        const milestoneBadges = metadata.filter(b => b.category === 'milestone');
        expect(milestoneBadges.length).toBe(3);
        expect(milestoneBadges.map(b => b.badgeType)).toContain('Bronze');
        expect(milestoneBadges.map(b => b.badgeType)).toContain('Silver');
        expect(milestoneBadges.map(b => b.badgeType)).toContain('Gold');
    });

    it('getAllBadgeMetadata() should categorize achievement badges correctly', () => {
        //act
        const metadata = service.getAllBadgeMetadata();

        //assert
        const achievementBadges = metadata.filter(b => b.category === 'achievement');
        expect(achievementBadges.length).toBe(5);
        expect(achievementBadges.map(b => b.badgeType)).toContain('FirstLend');
        expect(achievementBadges.map(b => b.badgeType)).toContain('ReliableBorrower');
        expect(achievementBadges.map(b => b.badgeType)).toContain('GenerousLender');
        expect(achievementBadges.map(b => b.badgeType)).toContain('PerfectRecord');
        expect(achievementBadges.map(b => b.badgeType)).toContain('CommunityBuilder');
    });
});
