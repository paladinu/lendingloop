import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
    selector: 'app-loop-score-display',
    standalone: true,
    imports: [CommonModule],
    templateUrl: './loop-score-display.component.html',
    styleUrls: ['./loop-score-display.component.css']
})
export class LoopScoreDisplayComponent {
    @Input() score: number = 0;
    @Input() size: 'small' | 'medium' | 'large' = 'medium';
}
