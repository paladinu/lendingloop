import { Component, Input, Output, EventEmitter, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subject, takeUntil } from 'rxjs';
import { SystemTag } from '../../models/system-tag.interface';
import { ItemSearchFilter } from '../../models/item-search-filter.interface';
import { TagsService } from '../../services/tags.service';
import { ItemsService } from '../../services/items.service';
import { UserService } from '../../services/user.service';

@Component({
  selector: 'app-item-filter',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './item-filter.component.html',
  styleUrl: './item-filter.component.css'
})
export class ItemFilterComponent implements OnInit, OnDestroy {
  @Input() loopId: string = '';
  @Output() filterChange = new EventEmitter<ItemSearchFilter>();

  // Filter state
  selectedTags: string[] = [];
  availabilityFilter: 'all' | 'available' | 'unavailable' = 'all';
  selectedOwners: string[] = [];

  // Data
  availableTags: SystemTag[] = [];
  availableOwners: { id: string; name: string }[] = [];

  // UI state
  isLoadingTags: boolean = false;
  isLoadingOwners: boolean = false;
  showTagDropdown: boolean = false;
  showOwnerDropdown: boolean = false;
  tagsError: string = '';
  ownersError: string = '';

  private destroy$ = new Subject<void>();

  constructor(
    private tagsService: TagsService,
    private itemsService: ItemsService,
    private userService: UserService
  ) {}

  ngOnInit(): void {
    this.loadTags();
    if (this.loopId) {
      this.loadOwners();
    }
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private loadTags(): void {
    this.isLoadingTags = true;
    this.tagsError = '';
    this.tagsService.getAllTags()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (tags) => {
          this.availableTags = tags;
          this.isLoadingTags = false;
          this.tagsError = '';
        },
        error: (error) => {
          console.error('Error loading tags:', error);
          this.tagsError = 'Failed to load tags. Please try again.';
          this.isLoadingTags = false;
        }
      });
  }

  retryLoadTags(): void {
    this.loadTags();
  }

  private loadOwners(): void {
    this.isLoadingOwners = true;
    this.ownersError = '';
    this.itemsService.getDistinctOwners(this.loopId)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (ownerIds) => {
          // Get user details for each owner ID
          this.loadOwnerDetails(ownerIds);
          this.ownersError = '';
        },
        error: (error) => {
          console.error('Error loading owners:', error);
          this.ownersError = 'Failed to load owners. Please try again.';
          this.isLoadingOwners = false;
        }
      });
  }

  retryLoadOwners(): void {
    if (this.loopId) {
      this.loadOwners();
    }
  }

  private loadOwnerDetails(ownerIds: string[]): void {
    // For now, just use the IDs as names. In a real app, you'd fetch user details
    this.availableOwners = ownerIds
      .map(id => ({ id, name: `User ${id.substring(0, 8)}` }))
      .sort((a, b) => a.name.localeCompare(b.name));
    this.isLoadingOwners = false;
  }

  onTagToggle(tagName: string): void {
    if (this.selectedTags.includes(tagName)) {
      this.selectedTags = this.selectedTags.filter(t => t !== tagName);
    } else {
      this.selectedTags = [...this.selectedTags, tagName];
    }
    this.emitFilterChange();
  }

  onAvailabilityChange(): void {
    this.emitFilterChange();
  }

  onOwnerToggle(ownerId: string): void {
    if (this.selectedOwners.includes(ownerId)) {
      this.selectedOwners = this.selectedOwners.filter(o => o !== ownerId);
    } else {
      this.selectedOwners = [...this.selectedOwners, ownerId];
    }
    this.emitFilterChange();
  }

  clearAllFilters(): void {
    this.selectedTags = [];
    this.availabilityFilter = 'all';
    this.selectedOwners = [];
    this.emitFilterChange();
  }

  private emitFilterChange(): void {
    const filter: ItemSearchFilter = {
      tags: this.selectedTags,
      isAvailable: this.availabilityFilter === 'all' ? undefined : this.availabilityFilter === 'available',
      ownerIds: this.selectedOwners,
      pageNumber: 1,
      pageSize: 50
    };
    this.filterChange.emit(filter);
  }

  getActiveFilterCount(): number {
    let count = 0;
    if (this.selectedTags.length > 0) count++;
    if (this.availabilityFilter !== 'all') count++;
    if (this.selectedOwners.length > 0) count++;
    return count;
  }

  hasActiveFilters(): boolean {
    return this.getActiveFilterCount() > 0;
  }

  getTagDisplayName(tagName: string): string {
    const tag = this.availableTags.find(t => t.name === tagName);
    return tag ? tag.displayName : tagName;
  }

  getOwnerName(ownerId: string): string {
    const owner = this.availableOwners.find(o => o.id === ownerId);
    return owner ? owner.name : ownerId;
  }

  toggleTagDropdown(): void {
    this.showTagDropdown = !this.showTagDropdown;
    if (this.showTagDropdown) {
      this.showOwnerDropdown = false;
    }
  }

  toggleOwnerDropdown(): void {
    this.showOwnerDropdown = !this.showOwnerDropdown;
    if (this.showOwnerDropdown) {
      this.showTagDropdown = false;
    }
  }

  closeDropdowns(): void {
    this.showTagDropdown = false;
    this.showOwnerDropdown = false;
  }
}
