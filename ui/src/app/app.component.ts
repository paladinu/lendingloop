import { Component, OnInit } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ItemsService } from './services/items.service';
import { SharedItem } from './models/shared-item.interface';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, CommonModule, FormsModule],
  templateUrl: './app.component.html',
  styleUrl: './app.component.css'
})
export class AppComponent implements OnInit {
  title = 'Shared Items Manager';
  items: SharedItem[] = [];
  newItemName: string = '';
  loading: boolean = false;
  error: string = '';

  constructor(private itemsService: ItemsService) { }

  ngOnInit(): void {
    this.loadItems();
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

  addItem(): void {
    if (!this.newItemName.trim()) {
      return;
    }

    const newItem: Partial<SharedItem> = {
      name: this.newItemName.trim(),
      ownerId: 'user1', // Default owner for demo
      isAvailable: true
    };

    this.loading = true;
    this.error = '';

    this.itemsService.createItem(newItem).subscribe({
      next: (createdItem) => {
        this.items.push(createdItem);
        this.newItemName = '';
        this.loading = false;
      },
      error: (err) => {
        console.error('Error creating item:', err);
        this.error = 'Failed to add item. Please try again.';
        this.loading = false;
      }
    });
  }
}
