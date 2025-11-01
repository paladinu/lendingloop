import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { ItemsService } from '../../services/items.service';
import { AuthService } from '../../services/auth.service';
import { LoopService } from '../../services/loop.service';
import { SharedItem } from '../../models/shared-item.interface';
import { UserProfile } from '../../models/auth.interface';
import { Loop } from '../../models/loop.interface';
import { ItemCardComponent } from '../item-card/item-card.component';
import { ToolbarComponent } from '../toolbar/toolbar.component';

@Component({
  selector: 'app-main',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    ItemCardComponent,
    ToolbarComponent
  ],
  templateUrl: './main.component.html',
  styleUrl: './main.component.css'
})
export class MainComponent implements OnInit {
  items: SharedItem[] = [];
  loading: boolean = false;
  error: string = '';
  currentUser: UserProfile | null = null;
  
  // Loop visibility properties
  loops: Loop[] = [];

  constructor(
    private itemsService: ItemsService,
    private authService: AuthService,
    private loopService: LoopService,
    private router: Router
  ) { }

  ngOnInit(): void {
    this.loadCurrentUser();
    this.loadItems();
    this.loadLoops();
  }

  loadCurrentUser(): void {
    this.authService.getCurrentUser().subscribe({
      next: (user) => {
        this.currentUser = user;
      },
      error: (err) => {
        console.error('Error loading current user:', err);
      }
    });
  }

  loadItems(): void {
    this.loading = true;
    this.error = '';

    this.itemsService.getItems().subscribe({
      next: (items) => {
        this.items = items;
        this.loading = false;
      },
      error: (err) => {
        console.error('Error loading items:', err);
        this.error = 'Failed to load items. Please try again.';
        this.loading = false;
      }
    });
  }

  loadLoops(): void {
    this.loopService.getUserLoops().subscribe({
      next: (loops) => {
        this.loops = loops;
      },
      error: (err) => {
        console.error('Error loading loops:', err);
        // Don't show error to user - having no loops is acceptable
        this.loops = [];
      }
    });
  }

  onEditVisibility(itemId: string): void {
    this.router.navigate(['/items', itemId, 'visibility']);
  }

  isItemOwner(item: SharedItem): boolean {
    return this.currentUser?.id === item.userId;
  }
}