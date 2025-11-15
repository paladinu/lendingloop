import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { LoopScoreService } from './loop-score.service';
import { ScoreHistoryEntry } from '../models/auth.interface';
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
});
