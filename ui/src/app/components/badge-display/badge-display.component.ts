import { Component, Input, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { BadgeAward, BadgeType, BadgeMetadata } from '../../models/auth.interface';
import { LoopScoreService } from '../../services/loop-score.service';
import { FilterByCategoryPipe } from '../../pipes/filter-by-category.pipe';

export interface DisplayBadge {
    metadata: BadgeMetadata;
    earned: boolean;
    awardedAt?: string;
}

@Component({
    selector: 'app-badge-display',
    standalone: true,
    imports: [CommonModule, FilterByCategoryPipe],
    templateUrl: './badge-display.component.html',
    styleUrls: ['./badge-display.component.css']
})
export class BadgeDisplayComponent implements OnInit {
    @Input() earnedBadges: BadgeAward[] = [];
    @Input() showAllBadges: boolean = true;
    
    allBadgeMetadata: BadgeMetadata[] = [];
    displayBadges: DisplayBadge[] = [];
    milestoneBadges: BadgeAward[] = [];
    achievementBadges: BadgeAward[] = [];

    constructor(private loopScoreService: LoopScoreService) {}

    ngOnInit(): void {
        if (this.showAllBadges) {
            this.allBadgeMetadata = this.loopScoreService.getAllBadgeMetadata();
            this.prepareDisplayBadges();
        } else {
            this.categorizeBadges();
        }
    }

    prepareDisplayBadges(): void {
        this.displayBadges = this.allBadgeMetadata.map(metadata => {
            const earnedBadge = this.earnedBadges.find(b => b.badgeType === metadata.badgeType);
            return {
                metadata: metadata,
                earned: !!earnedBadge,
                awardedAt: earnedBadge?.awardedAt
            };
        });
    }

    categorizeBadges(): void {
        this.milestoneBadges = this.earnedBadges.filter(b => 
            ['Bronze', 'Silver', 'Gold'].includes(b.badgeType)
        );
        this.achievementBadges = this.earnedBadges.filter(b => 
            ['FirstLend', 'ReliableBorrower', 'GenerousLender', 'PerfectRecord', 'CommunityBuilder'].includes(b.badgeType)
        );
    }

    getBadgeIcon(badgeType: BadgeType): string {
        const icons: Record<string, string> = {
            'FirstLend': '🎁',
            'ReliableBorrower': '⭐',
            'GenerousLender': '🤝',
            'PerfectRecord': '💯',
            'CommunityBuilder': '🌟'
        };
        return icons[badgeType] || '🏅';
    }

    getBadgeLabel(badgeType: BadgeType): string {
        const labels: Record<string, string> = {
            'FirstLend': 'First Lend',
            'ReliableBorrower': 'Reliable Borrower',
            'GenerousLender': 'Generous Lender',
            'PerfectRecord': 'Perfect Record',
            'CommunityBuilder': 'Community Builder'
        };
        return labels[badgeType] || badgeType;
    }

    getBadgeDescription(badgeType: BadgeType): string {
        switch (badgeType) {
            case 'Bronze':
                return 'Bronze Badge - Reached 10 LoopScore points';
            case 'Silver':
                return 'Silver Badge - Reached 50 LoopScore points';
            case 'Gold':
                return 'Gold Badge - Reached 100 LoopScore points';
            case 'FirstLend':
                return 'First Lend - Completed your first lending transaction';
            case 'ReliableBorrower':
                return 'Reliable Borrower - Completed 10 on-time returns';
            case 'GenerousLender':
                return 'Generous Lender - Completed 50 lending transactions';
            case 'PerfectRecord':
                return 'Perfect Record - 25 consecutive on-time returns';
            case 'CommunityBuilder':
                return 'Community Builder - 10 invited users became active';
            default:
                return 'Badge';
        }
    }

    getBadgeRequirement(badgeType: BadgeType): string {
        switch (badgeType) {
            case 'Bronze':
                return 'Earned by reaching 10 points';
            case 'Silver':
                return 'Earned by reaching 50 points';
            case 'Gold':
                return 'Earned by reaching 100 points';
            case 'FirstLend':
                return 'Earned by completing your first lend';
            case 'ReliableBorrower':
                return 'Earned by completing 10 on-time returns';
            case 'GenerousLender':
                return 'Earned by completing 50 lending transactions';
            case 'PerfectRecord':
                return 'Earned by achieving 25 consecutive on-time returns';
            case 'CommunityBuilder':
                return 'Earned when 10 invited users become active';
            default:
                return '';
        }
    }
}
