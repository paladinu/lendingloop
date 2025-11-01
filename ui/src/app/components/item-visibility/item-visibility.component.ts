import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { forkJoin } from 'rxjs';
import { LoopService } from '../../services/loop.service';
import { ItemsService } from '../../services/items.service';
import { Loop } from '../../models/loop.interface';
import { SharedItem } from '../../models/shared-item.interface';

@Component({
  selector: 'app-item-visibility',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './item-visibility.component.html',
  styleUrls: ['./item-visibility.component.css']
})
export class ItemVisibilityComponent implements OnInit {
  itemId: string | null = null;
  item: SharedItem | null = null;
  loops: Loop[] = [];
  selectedLoopIds: Set<string> = new Set();
  visibleToAllLoops = false;
  visibleToFutureLoops = false;
  loading = false;
  error: string | null = null;
  success: string | null = null;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private loopService: LoopService,
    private itemsService: ItemsService
  ) {}

  ngOnInit(): void {
    this.itemId = this.route.snapshot.paramMap.get('id');
    if (this.itemId) {
      this.loadData();
    }
  }

  loadData(): void {
    this.loading = true;
    this.error = null;
    
    forkJoin({
      loops: this.loopService.getUserLoops(),
      item: this.itemsService.getItemById(this.itemId!)
    }).subscribe({
      next: ({ loops, item }) => {
        this.loops = loops || [];
        this.item = item || null;
        if (this.item) {
          this.selectedLoopIds = new Set(this.item.visibleToLoopIds);
          this.visibleToAllLoops = this.item.visibleToAllLoops;
          this.visibleToFutureLoops = this.item.visibleToFutureLoops;
        }
        this.loading = false;
      },
      error: (err) => {
        this.error = 'Failed to load data. Please try again.';
        this.loading = false;
        console.error('Error loading data:', err);
      }
    });
  }

  toggleLoopSelection(loopId: string): void {
    if (this.selectedLoopIds.has(loopId)) {
      this.selectedLoopIds.delete(loopId);
    } else {
      this.selectedLoopIds.add(loopId);
    }
  }

  onSubmit(): void {
    if (!this.itemId) return;

    this.loading = true;
    this.error = null;
    this.success = null;

    this.itemsService.updateItemVisibility(
      this.itemId,
      Array.from(this.selectedLoopIds),
      this.visibleToAllLoops,
      this.visibleToFutureLoops
    ).subscribe({
      next: () => {
        this.success = 'Visibility settings updated successfully';
        this.loading = false;
        setTimeout(() => {
          this.router.navigate(['/']);
        }, 1500);
      },
      error: (err) => {
        this.error = err.error?.message || 'Failed to update visibility settings';
        this.loading = false;
      }
    });
  }

  onCancel(): void {
    this.router.navigate(['/']);
  }

  get hasNoVisibility(): boolean {
    return !this.visibleToAllLoops && this.selectedLoopIds.size === 0;
  }

  validateAndSave(): void {
    if (this.hasNoVisibility) {
      const confirmed = confirm('This item will not be visible in any loops. Are you sure you want to continue?');
      if (!confirmed) {
        return;
      }
    }
    this.onSubmit();
  }

  getLoopName(loopId: string): string {
    const loop = this.loops.find(l => l.id === loopId);
    return loop ? loop.name : 'Unknown Loop';
  }

  getLoopMemberCount(loopId: string): number | undefined {
    const loop = this.loops.find(l => l.id === loopId);
    return loop?.memberCount;
  }

  onAllLoopsChange(): void {
    if (this.visibleToAllLoops) {
      // Clear specific selections when "all loops" is enabled
      this.selectedLoopIds.clear();
    }
  }
}
