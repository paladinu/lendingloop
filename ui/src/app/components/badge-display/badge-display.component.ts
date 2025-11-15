import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { BadgeAward, BadgeType } from '../../models/auth.interface';

@Component({
    selector: 'app-badge-display',
    standalone: true,
    imports: [CommonModule],
    templateUrl: './badge-display.component.html',
    styleUrls: ['./badge-display.component.css']
})
export class BadgeDisplayComponent {
    @Input() badges: BadgeAward[] = [];

    getBadgeDescription(badgeType: BadgeType): string {
        switch (badgeType) {
            case 'Bronze':
                return 'Bronze Badge - Reached 10 LoopScore points';
            case 'Silver':
                return 'Silver Badge - Reached 50 LoopScore points';
            case 'Gold':
                return 'Gold Badge - Reached 100 LoopScore points';
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
            default:
                return '';
        }
    }
}
