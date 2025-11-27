import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { ItemsService } from '../../services/items.service';
import { LoopService } from '../../services/loop.service';
import { SharedItem } from '../../models/shared-item.interface';
import { Loop } from '../../models/loop.interface';
import { VisibilitySelectorComponent } from '../visibility-selector/visibility-selector.component';
import { ToolbarComponent } from '../toolbar/toolbar.component';
import { TagSelectorComponent } from '../tag-selector/tag-selector.component';

@Component({
  selector: 'app-item-add',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    VisibilitySelectorComponent,
    ToolbarComponent,
    TagSelectorComponent
  ],
  templateUrl: './item-add.component.html',
  styleUrls: ['./item-add.component.css']
})
export class ItemAddComponent implements OnInit {
  newItemName: string = '';
  newItemDescription: string = '';
  selectedImageFile: File | null = null;
  loading: boolean = false;
  error: string = '';
  success: string = '';

  // Loop visibility properties
  loops: Loop[] = [];
  selectedLoopIds: string[] = [];
  visibleToAllLoops: boolean = false;
  visibleToFutureLoops: boolean = false;

  // Tag selection
  selectedTags: string[] = [];

  constructor(
    private itemsService: ItemsService,
    private loopService: LoopService,
    private router: Router
  ) { }

  ngOnInit(): void {
    this.loadLoops();
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

  onTagsChange(tags: string[]): void {
    this.selectedTags = tags;
  }

  addItem(): void {
    if (!this.newItemName.trim()) {
      this.error = 'Item name is required';
      return;
    }

    if (this.selectedTags.length > 10) {
      this.error = 'You can select up to 10 tags per item';
      return;
    }

    const newItem: Partial<SharedItem> = {
      name: this.newItemName.trim(),
      description: this.newItemDescription.trim(),
      isAvailable: true,
      visibleToLoopIds: this.selectedLoopIds,
      visibleToAllLoops: this.visibleToAllLoops,
      visibleToFutureLoops: this.visibleToFutureLoops,
      tags: this.selectedTags
    };

    this.loading = true;
    this.error = '';
    this.success = '';

    this.itemsService.createItem(newItem).subscribe({
      next: (createdItem) => {
        // If an image was selected, upload it
        if (this.selectedImageFile && createdItem.id) {
          this.itemsService.uploadItemImage(createdItem.id, this.selectedImageFile).subscribe({
            next: () => {
              this.success = 'Item added successfully with tags!';
              this.loading = false;
              setTimeout(() => {
                this.router.navigate(['/items']);
              }, 1500);
            },
            error: (err) => {
              console.error('Error uploading image:', err);
              this.success = 'Item added but image upload failed.';
              this.loading = false;
              setTimeout(() => {
                this.router.navigate(['/items']);
              }, 2000);
            }
          });
        } else {
          this.success = this.selectedTags.length > 0 
            ? 'Item added successfully with tags!' 
            : 'Item added successfully!';
          this.loading = false;
          setTimeout(() => {
            this.router.navigate(['/items']);
          }, 1500);
        }
      },
      error: (err) => {
        console.error('Error creating item:', err);
        if (err.message && err.message.includes('tag')) {
          this.error = err.message;
        } else if (err.message && err.message.includes('10 tags')) {
          this.error = 'You can select up to 10 tags per item';
        } else {
          this.error = 'Failed to add item. Please try again.';
        }
        this.loading = false;
      }
    });
  }

  onCancel(): void {
    this.router.navigate(['/items']);
  }
}
