import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { Subject, debounceTime, distinctUntilChanged, takeUntil } from 'rxjs';
import { LoopService } from '../../services/loop.service';
import { AuthService } from '../../services/auth.service';
import { ItemsService } from '../../services/items.service';
import { Loop } from '../../models/loop.interface';
import { SharedItem } from '../../models/shared-item.interface';
import { ItemSearchFilter } from '../../models/item-search-filter.interface';
import { ToolbarComponent } from '../toolbar/toolbar.component';
import { ItemFilterComponent } from '../item-filter/item-filter.component';
import { ItemCardComponent } from '../item-card/item-card.component';
import { PaginationComponent } from '../pagination/pagination.component';
import { ItemRequest } from '../../models/item-request.interface';

@Component({
  selector: 'app-loop-detail',
  standalone: true,
  imports: [
    CommonModule, 
    FormsModule, 
    ToolbarComponent, 
    ItemFilterComponent,
    ItemCardComponent,
    PaginationComponent
  ],
  templateUrl: './loop-detail.component.html',
  styleUrls: ['./loop-detail.component.css']
})
export class LoopDetailComponent implements OnInit, OnDestroy {
  loopId: string | null = null;
  loop: Loop | null = null;
  items: SharedItem[] = [];
  searchQuery = '';
  loading = false;
  searching = false;
  error: string | null = null;
  currentUserId: string | null = null;
  isOwner: boolean = false;
  
  // Search and filter state
  currentFilter: ItemSearchFilter = {
    tags: [],
    isAvailable: undefined,
    ownerIds: [],
    pageNumber: 1,
    pageSize: 50
  };
  totalResults = 0;
  
  private searchSubject = new Subject<string>();
  private destroy$ = new Subject<void>();

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private loopService: LoopService,
    private authService: AuthService,
    private itemsService: ItemsService
  ) {}

  ngOnInit(): void {
    this.loopId = this.route.snapshot.paramMap.get('id');
    
    // Set up search debouncing
    this.searchSubject
      .pipe(
        debounceTime(300),
        distinctUntilChanged(),
        takeUntil(this.destroy$)
      )
      .subscribe(query => {
        this.currentFilter.searchText = query || undefined;
        this.performSearch();
      });
    
    // Get current user
    this.authService.getCurrentUser().subscribe(user => {
      this.currentUserId = user?.id || null;
      
      if (this.loopId) {
        this.loadLoopDetails();
        this.performSearch(); // Use search instead of loadLoopItems
      }
    });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  loadLoopDetails(): void {
    if (!this.loopId) return;

    this.loopService.getLoopById(this.loopId).subscribe({
      next: (loop) => {
        this.loop = loop;
        this.isOwner = this.currentUserId === loop.creatorId;
      },
      error: (err) => {
        this.error = 'Failed to load loop details';
        console.error('Error loading loop:', err);
      }
    });
  }

  performSearch(): void {
    if (!this.loopId) return;

    this.searching = true;
    this.error = null;

    this.itemsService.searchItems(this.loopId, this.currentFilter).subscribe({
      next: (response) => {
        this.items = response.items;
        this.totalResults = response.totalCount;
        this.searching = false;
      },
      error: (err) => {
        this.error = 'Failed to search items';
        this.searching = false;
        console.error('Error searching items:', err);
      }
    });
  }

  onSearchChange(): void {
    this.searchSubject.next(this.searchQuery);
  }

  onFilterChange(filter: ItemSearchFilter): void {
    this.currentFilter = { ...filter, searchText: this.searchQuery || undefined };
    this.performSearch();
  }

  onTagClick(tag: string): void {
    // Add tag to filter if not already present
    if (!this.currentFilter.tags.includes(tag)) {
      this.currentFilter.tags = [...this.currentFilter.tags, tag];
      this.performSearch();
    }
  }

  onPageChange(pageNumber: number): void {
    this.currentFilter.pageNumber = pageNumber;
    this.performSearch();
  }

  navigateToMembers(): void {
    if (this.loopId) {
      this.router.navigate(['/loops', this.loopId, 'members']);
    }
  }

  navigateToInvite(): void {
    if (this.loopId) {
      this.router.navigate(['/loops', this.loopId, 'invite']);
    }
  }

  navigateToSettings(): void {
    if (this.loopId) {
      this.router.navigate(['/loops', this.loopId, 'settings']);
    }
  }

  navigateBack(): void {
    this.router.navigate(['/loops']);
  }

  onRequestCreated(request: ItemRequest): void {
    // Optionally refresh the items list to show updated availability
    console.log('Request created:', request);
    // Could show a success message or refresh items
  }

  onEditItem(itemId: string): void {
    this.router.navigate(['/items', itemId, 'edit']);
  }

  onEditVisibility(itemId: string): void {
    this.router.navigate(['/items', itemId, 'visibility']);
  }
}
