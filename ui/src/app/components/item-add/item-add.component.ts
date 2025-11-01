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
    ToolbarComponent
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

  addItem(): void {
    if (!this.newItemName.trim()) {
      return;
    }

    const newItem: Partial<SharedItem> = {
      name: this.newItemName.trim(),
      description: this.newItemDescription.trim(),
      isAvailable: true,
      visibleToLoopIds: this.selectedLoopIds,
      visibleToAllLoops: this.visibleToAllLoops,
      visibleToFutureLoops: this.visibleToFutureLoops
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
              this.success = 'Item added successfully!';
              this.loading = false;
              setTimeout(() => {
                this.router.navigate(['/']);
              }, 1500);
            },
            error: (err) => {
              console.error('Error uploading image:', err);
              this.success = 'Item added but image upload failed.';
              this.loading = false;
              setTimeout(() => {
                this.router.navigate(['/']);
              }, 2000);
            }
          });
        } else {
          this.success = 'Item added successfully!';
          this.loading = false;
          setTimeout(() => {
            this.router.navigate(['/']);
          }, 1500);
        }
      },
      error: (err) => {
        console.error('Error creating item:', err);
        this.error = 'Failed to add item. Please try again.';
        this.loading = false;
      }
    });
  }

  onCancel(): void {
    this.router.navigate(['/']);
  }
}
