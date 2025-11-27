import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatTooltipModule } from '@angular/material/tooltip';
import { SharedItem } from '../../models/shared-item.interface';
import { Loop } from '../../models/loop.interface';
import { ItemRequest } from '../../models/item-request.interface';
import { LoopScoreDisplayComponent } from '../loop-score-display/loop-score-display.component';
import { ItemRequestButtonComponent } from '../item-request-button/item-request-button.component';

@Component({
  selector: 'app-item-card',
  standalone: true,
  imports: [
    CommonModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatChipsModule,
    MatTooltipModule,
    LoopScoreDisplayComponent,
    ItemRequestButtonComponent
  ],
  templateUrl: './item-card.component.html',
  styleUrls: ['./item-card.component.css']
})
export class ItemCardComponent {
  @Input() item!: SharedItem;
  @Input() isOwner: boolean = false;
  @Input() loops: Loop[] = [];
  
  @Output() editVisibility = new EventEmitter<string>();
  @Output() editItem = new EventEmitter<string>();
  @Output() tagClick = new EventEmitter<string>();
  @Output() requestCreated = new EventEmitter<ItemRequest>();

  private readonly MAX_VISIBLE_TAGS = 5;

  getVisibilityText(): string {
    if (this.item.visibleToAllLoops) {
      return 'All loops';
    }
    
    const loopCount = this.item.visibleToLoopIds?.length || 0;
    if (loopCount === 0) {
      return 'No loops';
    }
    
    return `${loopCount} loop${loopCount !== 1 ? 's' : ''}`;
  }

  getLoopNames(): string[] {
    if (!this.item.visibleToLoopIds || this.item.visibleToLoopIds.length === 0) {
      return [];
    }
    
    return this.item.visibleToLoopIds
      .map(loopId => {
        const loop = this.loops.find(l => l.id === loopId);
        return loop ? loop.name : null;
      })
      .filter(name => name !== null) as string[];
  }

  onEditVisibility(): void {
    if (this.item.id) {
      this.editVisibility.emit(this.item.id);
    }
  }

  onEditItem(): void {
    if (this.item.id) {
      this.editItem.emit(this.item.id);
    }
  }

  getVisibleTags(): string[] {
    if (!this.item.tags || this.item.tags.length === 0) {
      return [];
    }
    return this.item.tags.slice(0, this.MAX_VISIBLE_TAGS);
  }

  getHiddenTagCount(): number {
    if (!this.item.tags || this.item.tags.length <= this.MAX_VISIBLE_TAGS) {
      return 0;
    }
    return this.item.tags.length - this.MAX_VISIBLE_TAGS;
  }

  hasTags(): boolean {
    return !!(this.item.tags && this.item.tags.length > 0);
  }

  onTagClick(tagName: string): void {
    this.tagClick.emit(tagName);
  }

  onRequestCreated(request: ItemRequest): void {
    this.requestCreated.emit(request);
  }
}
