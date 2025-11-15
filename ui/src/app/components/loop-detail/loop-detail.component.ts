import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { LoopService } from '../../services/loop.service';
import { AuthService } from '../../services/auth.service';
import { Loop } from '../../models/loop.interface';
import { SharedItem } from '../../models/shared-item.interface';
import { ToolbarComponent } from '../toolbar/toolbar.component';
import { ItemRequestButtonComponent } from '../item-request-button/item-request-button.component';
import { LoopScoreDisplayComponent } from '../loop-score-display/loop-score-display.component';
import { ItemRequest } from '../../models/item-request.interface';

@Component({
  selector: 'app-loop-detail',
  standalone: true,
  imports: [CommonModule, FormsModule, ToolbarComponent, ItemRequestButtonComponent, LoopScoreDisplayComponent],
  templateUrl: './loop-detail.component.html',
  styleUrls: ['./loop-detail.component.css']
})
export class LoopDetailComponent implements OnInit {
  loopId: string | null = null;
  loop: Loop | null = null;
  items: SharedItem[] = [];
  filteredItems: SharedItem[] = [];
  searchQuery = '';
  loading = false;
  error: string | null = null;
  currentUserId: string | null = null;
  isOwner: boolean = false;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private loopService: LoopService,
    private authService: AuthService
  ) {}

  ngOnInit(): void {
    this.loopId = this.route.snapshot.paramMap.get('id');
    
    // Get current user
    this.authService.getCurrentUser().subscribe(user => {
      this.currentUserId = user?.id || null;
      
      if (this.loopId) {
        this.loadLoopDetails();
        this.loadLoopItems();
      }
    });
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

  loadLoopItems(): void {
    if (!this.loopId) return;

    this.loading = true;
    this.error = null;

    this.loopService.getLoopItems(this.loopId).subscribe({
      next: (response) => {
        this.items = response.items;
        this.filteredItems = response.items;
        this.loading = false;
      },
      error: (err) => {
        this.error = 'Failed to load items';
        this.loading = false;
        console.error('Error loading items:', err);
      }
    });
  }

  onSearchChange(): void {
    if (!this.searchQuery.trim()) {
      this.filteredItems = this.items;
      return;
    }

    const query = this.searchQuery.toLowerCase();
    this.filteredItems = this.items.filter(item =>
      item.name.toLowerCase().includes(query) ||
      item.description.toLowerCase().includes(query)
    );
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
}
