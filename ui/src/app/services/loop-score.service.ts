import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { environment } from '../../environments/environment';
import { ScoreHistoryEntry, BadgeAward, BadgeMetadata, BadgeProgress, BadgeType } from '../models/auth.interface';

export interface ScoreRules {
    borrowCompleted: number;
    onTimeReturn: number;
    lendApproved: number;
}

export interface BadgeMilestones {
    bronze: number;
    silver: number;
    gold: number;
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

    getUserBadges(userId: string): Observable<BadgeAward[]> {
        return this.http.get<BadgeAward[]>(`${this.API_URL}/${userId}/badges`);
    }

    getScoreExplanation(): ScoreRules {
        return {
            borrowCompleted: 1,
            onTimeReturn: 1,
            lendApproved: 4
        };
    }

    getBadgeMilestones(): BadgeMilestones {
        return {
            bronze: 10,
            silver: 50,
            gold: 100
        };
    }

    getAllBadgeMetadata(): BadgeMetadata[] {
        return [
            // Milestone badges
            {
                badgeType: 'Bronze',
                name: 'Bronze Badge',
                description: 'Awarded for reaching 10 points',
                category: 'milestone',
                requirement: 'Reach 10 points',
                icon: '🏆',
                hasProgress: true
            },
            {
                badgeType: 'Silver',
                name: 'Silver Badge',
                description: 'Awarded for reaching 50 points',
                category: 'milestone',
                requirement: 'Reach 50 points',
                icon: '🏆',
                hasProgress: true
            },
            {
                badgeType: 'Gold',
                name: 'Gold Badge',
                description: 'Awarded for reaching 100 points',
                category: 'milestone',
                requirement: 'Reach 100 points',
                icon: '🏆',
                hasProgress: true
            },
            // Achievement badges
            {
                badgeType: 'FirstLend',
                name: 'First Lend',
                description: 'Complete your first lending transaction',
                category: 'achievement',
                requirement: 'Lend an item for the first time',
                icon: '🎁',
                hasProgress: false
            },
            {
                badgeType: 'ReliableBorrower',
                name: 'Reliable Borrower',
                description: 'Return items on time consistently',
                category: 'achievement',
                requirement: 'Complete 10 on-time returns',
                icon: '⭐',
                hasProgress: true
            },
            {
                badgeType: 'GenerousLender',
                name: 'Generous Lender',
                description: 'Share your items frequently',
                category: 'achievement',
                requirement: 'Complete 50 lending transactions',
                icon: '🤝',
                hasProgress: true
            },
            {
                badgeType: 'PerfectRecord',
                name: 'Perfect Record',
                description: 'Maintain a perfect return streak',
                category: 'achievement',
                requirement: 'Complete 25 consecutive on-time returns',
                icon: '💯',
                hasProgress: true
            },
            {
                badgeType: 'CommunityBuilder',
                name: 'Community Builder',
                description: 'Grow the LendingLoop community',
                category: 'achievement',
                requirement: 'Invite 10 users who become active',
                icon: '🌟',
                hasProgress: true
            }
        ];
    }

    getBadgeProgress(userId: string): Observable<Map<BadgeType, BadgeProgress>> {
        return this.http.get<Record<string, BadgeProgress>>(`${this.API_URL}/${userId}/badge-progress`)
            .pipe(
                map(response => {
                    const progressMap = new Map<BadgeType, BadgeProgress>();
                    Object.entries(response).forEach(([key, value]) => {
                        progressMap.set(key as BadgeType, value);
                    });
                    return progressMap;
                })
            );
    }
}
