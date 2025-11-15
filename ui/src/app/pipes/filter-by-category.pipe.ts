import { Pipe, PipeTransform } from '@angular/core';
import { DisplayBadge } from '../components/badge-display/badge-display.component';

@Pipe({
    name: 'filterByCategory',
    standalone: true
})
export class FilterByCategoryPipe implements PipeTransform {
    transform(badges: DisplayBadge[], category: 'milestone' | 'achievement'): DisplayBadge[] {
        if (!badges || !category) {
            return [];
        }
        return badges.filter(badge => badge.metadata.category === category);
    }
}
