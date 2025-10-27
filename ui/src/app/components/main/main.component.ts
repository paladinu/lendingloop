import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatChipsModule } from '@angular/material/chips';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { MatDividerModule } from '@angular/material/divider';
import { ItemsService } from '../../services/items.service';
import { AuthService } from '../../services/auth.service';
import { SharedItem } from '../../models/shared-item.interface';
import { UserProfile } from '../../models/auth.interface';

@Component({
  selector: 'app-main',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatChipsModule,
    MatToolbarModule,
    MatIconModule,
    MatMenuModule,
    MatDividerModule
  ],
  templateUrl: './main.component.html',
  styleUrl: './main.component.css'
})
export class MainComponent implements OnInit {
  title = 'Shared Items Manager';
  items: SharedItem[] = [];
  newItemName: string = '';
  selectedImageFile: File | null = null;
  loading: boolean = false;
  error: string = '';
  currentUser: UserProfile | null = null;

  constructor(
    private itemsService: ItemsService,
    private authService: AuthService,
    private router: Router
  ) { }

  ngOnInit(): void {
    this.loadCurrentUser();
    this.loadItems();
  }

  loadCurrentUser(): void {
    this.authService.getCurrentUser().subscribe({
      next: (user) => {
        this.currentUser = user;
      },
      error: (err) => {
        console.error('Error loading current user:', err);
        // If we can't get the current user, redirect to login
        this.logout();
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

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length > 0) {
      this.selectedImageFile = input.files[0];
    }
  }

  addItem(): void {
    if (!this.newItemName.trim()) {
      return;
    }

    const newItem: Partial<SharedItem> = {
      name: this.newItemName.trim(),
      isAvailable: true
    };

    this.loading = true;
    this.error = '';

    this.itemsService.createItem(newItem).subscribe({
      next: (createdItem) => {
        // If an image was selected, upload it
        if (this.selectedImageFile && createdItem.id) {
          this.itemsService.uploadItemImage(createdItem.id, this.selectedImageFile).subscribe({
            next: (updatedItem) => {
              this.items.push(updatedItem);
              this.newItemName = '';
              this.selectedImageFile = null;
              this.loading = false;
            },
            error: (err) => {
              console.error('Error uploading image:', err);
              // Still add the item without the image
              this.items.push(createdItem);
              this.newItemName = '';
              this.selectedImageFile = null;
              this.error = 'Item added but image upload failed.';
              this.loading = false;
            }
          });
        } else {
          this.items.push(createdItem);
          this.newItemName = '';
          this.selectedImageFile = null;
          this.loading = false;
        }
      },
      error: (err) => {
        console.error('Error creating item:', err);
        this.error = 'Failed to add item. Please try again.';
        this.loading = false;
      }
    });
  }

  logout(): void {
    this.authService.logout();
    this.router.navigate(['/login']);
  }

  getUserDisplayName(): string {
    if (this.currentUser) {
      return `${this.currentUser.firstName} ${this.currentUser.lastName}`;
    }
    return 'User';
  }


}