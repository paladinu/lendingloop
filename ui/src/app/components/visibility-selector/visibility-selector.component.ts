import { Component, Input, Output, EventEmitter, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { Loop } from '../../models/loop.interface';

export interface VisibilitySelection {
  selectedLoopIds: string[];
  visibleToAllLoops: boolean;
  visibleToFutureLoops: boolean;
}

@Component({
  selector: 'app-visibility-selector',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './visibility-selector.component.html',
  styleUrls: ['./visibility-selector.component.css']
})
export class VisibilitySelectorComponent implements OnInit {
  @Input() loops: Loop[] = [];
  @Input() selectedLoopIds: string[] = [];
  @Input() visibleToAllLoops: boolean = false;
  @Input() visibleToFutureLoops: boolean = false;
  
  @Output() selectionChange = new EventEmitter<VisibilitySelection>();

  private selectedLoopIdsSet: Set<string> = new Set();

  ngOnInit(): void {
    this.selectedLoopIdsSet = new Set(this.selectedLoopIds);
  }

  isLoopSelected(loopId: string | undefined): boolean {
    if (!loopId) return false;
    return this.selectedLoopIdsSet.has(loopId);
  }

  toggleLoop(loopId: string | undefined): void {
    if (!loopId) return;
    
    if (this.selectedLoopIdsSet.has(loopId)) {
      this.selectedLoopIdsSet.delete(loopId);
    } else {
      this.selectedLoopIdsSet.add(loopId);
    }
    
    this.emitSelection();
  }

  onAllLoopsChange(): void {
    if (this.visibleToAllLoops) {
      // Clear specific selections when "all loops" is enabled
      this.selectedLoopIdsSet.clear();
    }
    this.emitSelection();
  }

  onFutureLoopsChange(): void {
    this.emitSelection();
  }

  private emitSelection(): void {
    const selection: VisibilitySelection = {
      selectedLoopIds: Array.from(this.selectedLoopIdsSet),
      visibleToAllLoops: this.visibleToAllLoops,
      visibleToFutureLoops: this.visibleToFutureLoops
    };
    this.selectionChange.emit(selection);
  }
}
