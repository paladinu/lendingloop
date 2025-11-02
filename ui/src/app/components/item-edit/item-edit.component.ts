import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { forkJoin } from 'rxjs';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { ItemsService } from '../../services/items.service';
import { LoopService } from '../../services/loop.service';
import { SharedItem } from '../../models/shared-item.interface';
import { Loop } from '../../models/loop.interface';
import { VisibilitySelectorComponent } from '../visibility-selector/visibility-selector.component';
import { ToolbarComponent } from '../toolbar/toolbar.component';

@Component({
  selector: 'app-item-edit',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatCheckboxModule,
    VisibilitySelectorComponent,
    ToolbarComponent
  ],
  templateUrl: './item-edit.component.html',
  styleUrls: ['./item-edit.component.css']
})
export class ItemEditComponent implements OnInit {
  itemId: string | null = null;
  item: SharedItem | null = null;
  itemName: string = '';
  itemDescription: string = '';
  isAvailable: boolean = true;
  selectedImageFile: File | null = null;
  currentImageUrl: string | null = null;
  loops: Loop[] = [];
  selectedLoopIds: string[] = [];
  visibleToAllLoops: boolean = false;
  visibleToFutureLoops: boolean = false;
  loading: boolean = false;
  error: string = '';
  success: string = '';

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private itemsService: ItemsService,
    private loopService: LoopService
  ) { }

  ngOnInit(): void {
    this.itemId = this.route.snapshot.paramMap.get('id');
    if (this.itemId) {
      this.loadItemData();
      this.loadLoops();
    }
  }

  loadItemData(): void {
    if (!this.itemId) {
      this.error = 'No item ID provided';
      return;
    }

    this.loading = true;
    this.error = '';

    this.itemsService.getItemById(this.itemId).subscribe({
      next: (item) => {
        this.item = item;
        this.itemName = item.name;
        this.itemDescription = item.description;
        this.isAvailable = item.isAvailable;
        this.currentImageUrl = item.imageUrl || null;
        this.selectedLoopIds = item.visibleToLoopIds || [];
        this.visibleToAllLoops = item.visibleToAllLoops || false;
        this.visibleToFutureLoops = item.visibleToFutureLoops || false;
        this.loading = false;
      },
      error: (err) => {
        console.error('Error loading item:', err);
        if (err.message.includes('403') || err.message.includes('permission')) {
          this.error = 'You do not have permission to edit this item';
        } else if (err.message.includes('404') || err.message.includes('not found')) {
          this.error = 'Item not found';
        } else {
          this.error = 'Failed to load item. Please try again.';
        }
        this.loading = false;
        setTimeout(() => {
          this.router.navigate(['/']);
        }, 2000);
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
        this.loops = [];
      }
    });
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length > 0) {
      this.selectedImageFile = input.files[0];
    }
  }

  onVisibilitySelectionChange(selection: { selectedLoopIds: string[], visibleToAllLoops: boolean, visibleToFutureLoops: boolean }): void {
    this.selectedLoopIds = selection.selectedLoopIds;
    this.visibleToAllLoops = selection.visibleToAllLoops;
    this.visibleToFutureLoops = selection.visibleToFutureLoops;
  }

  updateItem(): void {
    if (!this.itemName.trim()) {
      this.error = 'Item name is required';
      return;
    }

    if (!this.itemId) {
      this.error = 'No item ID provided';
      return;
    }

    const updates: Partial<SharedItem> = {
      name: this.itemName.trim(),
      description: this.itemDescription.trim(),
      isAvailable: this.isAvailable,
      visibleToLoopIds: this.selectedLoopIds,
      visibleToAllLoops: this.visibleToAllLoops,
      visibleToFutureLoops: this.visibleToFutureLoops
    };

    this.loading = true;
    this.error = '';
    this.success = '';

    this.itemsService.updateItem(this.itemId, updates).subscribe({
      next: (updatedItem) => {
        // If an image was selected, upload it
        if (this.selectedImageFile && this.itemId) {
          this.itemsService.uploadItemImage(this.itemId, this.selectedImageFile).subscribe({
            next: () => {
              this.success = 'Item updated successfully!';
              this.loading = false;
              setTimeout(() => {
                this.router.navigate(['/']);
              }, 1500);
            },
            error: (err) => {
              console.error('Error uploading image:', err);
              this.success = 'Item updated but image upload failed.';
              this.loading = false;
              setTimeout(() => {
                this.router.navigate(['/']);
              }, 2000);
            }
          });
        } else {
          this.success = 'Item updated successfully!';
          this.loading = false;
          setTimeout(() => {
            this.router.navigate(['/']);
          }, 1500);
        }
      },
      error: (err) => {
        console.error('Error updating item:', err);
        if (err.message.includes('403') || err.message.includes('permission')) {
          this.error = 'You do not have permission to update this item';
        } else if (err.message.includes('404') || err.message.includes('not found')) {
          this.error = 'Item not found';
        } else if (err.message.includes('name')) {
          this.error = 'Item name is required';
        } else {
          this.error = 'Failed to update item. Please try again.';
        }
        this.loading = false;
      }
    });
  }

  onCancel(): void {
    this.router.navigate(['/']);
  }
}
