import { FilterByCategoryPipe } from './filter-by-category.pipe';
import { DisplayBadge } from '../components/badge-display/badge-display.component';
import { BadgeMetadata } from '../models/auth.interface';

describe('FilterByCategoryPipe', () => {
    let pipe: FilterByCategoryPipe;

    beforeEach(() => {
        pipe = new FilterByCategoryPipe();
    });

    it('should create an instance', () => {
        expect(pipe).toBeTruthy();
    });

    it('should filter badges by milestone category correctly', () => {
        //arrange
        const badges: DisplayBadge[] = [
            {
                metadata: { badgeType: 'Bronze', name: 'Bronze', description: '', category: 'milestone', requirement: '', icon: '🏆' },
                earned: true,
                awardedAt: new Date().toISOString()
            },
            {
                metadata: { badgeType: 'FirstLend', name: 'First Lend', description: '', category: 'achievement', requirement: '', icon: '🎁' },
                earned: false
            },
            {
                metadata: { badgeType: 'Silver', name: 'Silver', description: '', category: 'milestone', requirement: '', icon: '🏆' },
                earned: false
            }
        ];

        //act
        const result = pipe.transform(badges, 'milestone');

        //assert
        expect(result.length).toBe(2);
        expect(result[0].metadata.badgeType).toBe('Bronze');
        expect(result[1].metadata.badgeType).toBe('Silver');
    });

    it('should filter badges by achievement category correctly', () => {
        //arrange
        const badges: DisplayBadge[] = [
            {
                metadata: { badgeType: 'Bronze', name: 'Bronze', description: '', category: 'milestone', requirement: '', icon: '🏆' },
                earned: true,
                awardedAt: new Date().toISOString()
            },
            {
                metadata: { badgeType: 'FirstLend', name: 'First Lend', description: '', category: 'achievement', requirement: '', icon: '🎁' },
                earned: false
            },
            {
                metadata: { badgeType: 'ReliableBorrower', name: 'Reliable Borrower', description: '', category: 'achievement', requirement: '', icon: '⭐' },
                earned: true,
                awardedAt: new Date().toISOString()
            }
        ];

        //act
        const result = pipe.transform(badges, 'achievement');

        //assert
        expect(result.length).toBe(2);
        expect(result[0].metadata.badgeType).toBe('FirstLend');
        expect(result[1].metadata.badgeType).toBe('ReliableBorrower');
    });

    it('should return empty array when no badges match category', () => {
        //arrange
        const badges: DisplayBadge[] = [
            {
                metadata: { badgeType: 'Bronze', name: 'Bronze', description: '', category: 'milestone', requirement: '', icon: '🏆' },
                earned: true,
                awardedAt: new Date().toISOString()
            }
        ];

        //act
        const result = pipe.transform(badges, 'achievement');

        //assert
        expect(result.length).toBe(0);
    });

    it('should return empty array when badges is null', () => {
        //arrange
        const badges: any = null;

        //act
        const result = pipe.transform(badges, 'milestone');

        //assert
        expect(result).toEqual([]);
    });

    it('should return empty array when category is null', () => {
        //arrange
        const badges: DisplayBadge[] = [
            {
                metadata: { badgeType: 'Bronze', name: 'Bronze', description: '', category: 'milestone', requirement: '', icon: '🏆' },
                earned: true,
                awardedAt: new Date().toISOString()
            }
        ];

        //act
        const result = pipe.transform(badges, null as any);

        //assert
        expect(result).toEqual([]);
    });
});
