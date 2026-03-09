import { Component, Input, OnInit, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LoopScoreService, ScoreRules } from '../../services/loop-score.service';
import { AuthService } from '../../services/auth.service';
import { ScoreHistoryEntry } from '../../models/auth.interface';

@Component({
    selector: 'app-score-history',
    standalone: true,
    imports: [CommonModule],
    templateUrl: './score-history.component.html',
    styleUrls: ['./score-history.component.css']
})
export class ScoreHistoryComponent implements OnInit, OnChanges {
    @Input() userId: string | null = null;
    @Input() scoreHistory: ScoreHistoryEntry[] | null = null;
    
    displayScoreHistory: ScoreHistoryEntry[] = [];
    scoreRules: ScoreRules;
    loading: boolean = true;
    error: string | null = null;

    constructor(
        private loopScoreService: LoopScoreService,
        private authService: AuthService
    ) {
        this.scoreRules = this.loopScoreService.getScoreExplanation();
    }

    ngOnInit(): void {
        this.loadHistory();
    }

    ngOnChanges(changes: SimpleChanges): void {
        if (changes['userId'] || changes['scoreHistory']) {
            this.loadHistory();
        }
    }

    private loadHistory(): void {
        // If scoreHistory is provided as input, use it directly
        if (this.scoreHistory) {
            this.displayScoreHistory = this.scoreHistory;
            this.loading = false;
            return;
        }

        // Otherwise, load from API using userId
        const targetUserId = this.userId;
        
        if (targetUserId) {
            this.loadScoreHistory(targetUserId);
        } else {
            // Fall back to current user if no userId provided
            this.authService.getCurrentUser().subscribe(user => {
                if (user && user.id) {
                    this.loadScoreHistory(user.id);
                } else {
                    this.loading = false;
                    this.error = 'User not found';
                }
            });
        }
    }

    private loadScoreHistory(userId: string): void {
        this.loading = true;
        this.loopScoreService.getScoreHistory(userId).subscribe({
            next: (history) => {
                this.displayScoreHistory = history;
                this.loading = false;
            },
            error: (err) => {
                console.error('Error loading score history:', err);
                this.error = 'Failed to load score history';
                this.loading = false;
            }
        });
    }

    getActionTypeLabel(actionType: string): string {
        switch (actionType) {
            case 'BorrowCompleted':
                return 'Borrowed Item';
            case 'OnTimeReturn':
                return 'On-Time Return';
            case 'LendApproved':
                return 'Lent Item';
            case 'LendCancelled':
                return 'Lending Cancelled';
            default:
                return actionType;
        }
    }

    getActionTypeIcon(actionType: string): string {
        switch (actionType) {
            case 'BorrowCompleted':
                return '📦';
            case 'OnTimeReturn':
                return '✅';
            case 'LendApproved':
                return '🤝';
            case 'LendCancelled':
                return '❌';
            default:
                return '•';
        }
    }
}
