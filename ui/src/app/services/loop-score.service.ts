import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ScoreHistoryEntry } from '../models/auth.interface';

export interface ScoreRules {
    borrowCompleted: number;
    onTimeReturn: number;
    lendApproved: number;
}

@Injectable({
    providedIn: 'root'
})
export class LoopScoreService {
    private readonly API_URL = `${environment.apiUrl}/api/users`;

    constructor(private http: HttpClient) { }

    getUserScore(userId: string): Observable<number> {
        return this.http.get<number>(`${this.API_URL}/${userId}/score`);
    }

    getScoreHistory(userId: string, limit: number = 50): Observable<ScoreHistoryEntry[]> {
        return this.http.get<ScoreHistoryEntry[]>(`${this.API_URL}/${userId}/score-history?limit=${limit}`);
    }

    getScoreExplanation(): ScoreRules {
        return {
            borrowCompleted: 1,
            onTimeReturn: 1,
            lendApproved: 4
        };
    }
}
