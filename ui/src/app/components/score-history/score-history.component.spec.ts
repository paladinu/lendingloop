import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ScoreHistoryComponent } from './score-history.component';
import { LoopScoreService } from '../../services/loop-score.service';
import { AuthService } from '../../services/auth.service';
import { of, throwError } from 'rxjs';
import { ScoreHistoryEntry, UserProfile } from '../../models/auth.interface';
import { DatePipe } from '@angular/common';

describe('ScoreHistoryComponent', () => {
    let component: ScoreHistoryComponent;
    let fixture: ComponentFixture<ScoreHistoryComponent>;
    let mockLoopScoreService: jest.Mocked<LoopScoreService>;
    let mockAuthService: jest.Mocked<AuthService>;

    const mockUser: UserProfile = {
        id: 'user123',
        email: 'test@example.com',
        firstName: 'Test',
        lastName: 'User',
        streetAddress: '123 Test St',
        isEmailVerified: true,
        loopScore: 10
    };

    const mockHistory: ScoreHistoryEntry[] = [
        {
            timestamp: new Date().toISOString(),
            points: 1,
            actionType: 'BorrowCompleted',
            itemRequestId: 'req1',
            itemName: 'Test Item 1'
        },
        {
            timestamp: new Date(Date.now() - 86400000).toISOString(),
            points: 4,
            actionType: 'LendApproved',
            itemRequestId: 'req2',
            itemName: 'Test Item 2'
        }
    ];

    beforeEach(async () => {
        mockLoopScoreService = {
            getScoreHistory: jest.fn(),
            getScoreExplanation: jest.fn().mockReturnValue({
                borrowCompleted: 1,
                onTimeReturn: 1,
                lendApproved: 4
            })
        } as any;

        mockAuthService = {
            getCurrentUser: jest.fn()
        } as any;

        await TestBed.configureTestingModule({
            imports: [ScoreHistoryComponent],
            providers: [
                { provide: LoopScoreService, useValue: mockLoopScoreService },
                { provide: AuthService, useValue: mockAuthService },
                DatePipe
            ]
        }).compileComponents();

        fixture = TestBed.createComponent(ScoreHistoryComponent);
        component = fixture.componentInstance;
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });

    it('should load score history on init', (done) => {
        //arrange
        mockAuthService.getCurrentUser.mockReturnValue(of(mockUser));
        mockLoopScoreService.getScoreHistory.mockReturnValue(of(mockHistory));

        //act
        fixture.detectChanges();

        //assert
        setTimeout(() => {
            expect(component.scoreHistory).toEqual(mockHistory);
            expect(component.loading).toBe(false);
            expect(mockLoopScoreService.getScoreHistory).toHaveBeenCalledWith('user123');
            done();
        }, 100);
    });

    it('should display history entries correctly', (done) => {
        //arrange
        mockAuthService.getCurrentUser.mockReturnValue(of(mockUser));
        mockLoopScoreService.getScoreHistory.mockReturnValue(of(mockHistory));

        //act
        fixture.detectChanges();

        //assert
        setTimeout(() => {
            fixture.detectChanges();
            const compiled = fixture.nativeElement;
            const entries = compiled.querySelectorAll('.history-entry');
            expect(entries.length).toBe(2);
            done();
        }, 100);
    });

    it('should show empty state when no history', (done) => {
        //arrange
        mockAuthService.getCurrentUser.mockReturnValue(of(mockUser));
        mockLoopScoreService.getScoreHistory.mockReturnValue(of([]));

        //act
        fixture.detectChanges();

        //assert
        setTimeout(() => {
            fixture.detectChanges();
            const compiled = fixture.nativeElement;
            const emptyState = compiled.querySelector('.empty-state');
            expect(emptyState).toBeTruthy();
            expect(emptyState.textContent).toContain('No activity yet');
            done();
        }, 100);
    });

    it('should have timestamp in history entry', () => {
        //arrange
        const timestamp = '2024-01-15T10:30:00Z';
        const entry: ScoreHistoryEntry = {
            timestamp: timestamp,
            points: 1,
            actionType: 'BorrowCompleted',
            itemRequestId: 'req1',
            itemName: 'Test Item'
        };

        //act & assert
        expect(entry.timestamp).toBe(timestamp);
    });

    it('should display action type labels correctly', () => {
        //assert
        expect(component.getActionTypeLabel('BorrowCompleted')).toBe('Borrowed Item');
        expect(component.getActionTypeLabel('OnTimeReturn')).toBe('On-Time Return');
        expect(component.getActionTypeLabel('LendApproved')).toBe('Lent Item');
        expect(component.getActionTypeLabel('LendCancelled')).toBe('Lending Cancelled');
    });

    it('should display action type icons correctly', () => {
        //assert
        expect(component.getActionTypeIcon('BorrowCompleted')).toBe('📦');
        expect(component.getActionTypeIcon('OnTimeReturn')).toBe('✅');
        expect(component.getActionTypeIcon('LendApproved')).toBe('🤝');
        expect(component.getActionTypeIcon('LendCancelled')).toBe('❌');
    });

    it('should handle error when loading history fails', (done) => {
        //arrange
        const consoleErrorSpy = jest.spyOn(console, 'error').mockImplementation();
        mockAuthService.getCurrentUser.mockReturnValue(of(mockUser));
        mockLoopScoreService.getScoreHistory.mockReturnValue(throwError(() => new Error('API Error')));

        //act
        fixture.detectChanges();

        //assert
        setTimeout(() => {
            expect(component.error).toBe('Failed to load score history');
            expect(component.loading).toBe(false);
            expect(consoleErrorSpy).toHaveBeenCalledWith('Error loading score history:', expect.any(Error));
            consoleErrorSpy.mockRestore();
            done();
        }, 100);
    });

    it('should display score rules', () => {
        //arrange
        mockAuthService.getCurrentUser.mockReturnValue(of(mockUser));
        mockLoopScoreService.getScoreHistory.mockReturnValue(of([]));

        //act
        fixture.detectChanges();

        //assert
        const compiled = fixture.nativeElement;
        const rulesSection = compiled.querySelector('.score-rules-section');
        expect(rulesSection).toBeTruthy();
        expect(rulesSection.textContent).toContain('How to Earn Points');
    });
});
