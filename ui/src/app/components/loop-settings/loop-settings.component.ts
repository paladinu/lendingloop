import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { LoopService } from '../../services/loop.service';
import { LoopSettings } from '../../models/loop.interface';
import { ToolbarComponent } from '../toolbar/toolbar.component';

@Component({
  selector: 'app-loop-settings',
  standalone: true,
  imports: [CommonModule, FormsModule, ToolbarComponent],
  templateUrl: './loop-settings.component.html',
  styleUrl: './loop-settings.component.css'
})
export class LoopSettingsComponent implements OnInit {
  loopId: string = '';
  settings: LoopSettings = {
    name: '',
    description: '',
    isPublic: false
  };
  isArchived: boolean = false;
  loading: boolean = false;
  saving: boolean = false;
  message: string = '';
  messageType: 'success' | 'error' = 'success';

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private loopService: LoopService
  ) { }

  ngOnInit(): void {
    this.loopId = this.route.snapshot.paramMap.get('id') || '';
    if (this.loopId) {
      this.loadSettings();
      this.loadLoopDetails();
    }
  }

  loadSettings(): void {
    this.loading = true;
    this.loopService.getLoopSettings(this.loopId).subscribe({
      next: (settings) => {
        this.settings = settings;
        this.loading = false;
      },
      error: (error) => {
        console.error('Error loading settings:', error);
        this.showMessage('Failed to load settings', 'error');
        this.loading = false;
      }
    });
  }

  loadLoopDetails(): void {
    this.loopService.getLoopById(this.loopId).subscribe({
      next: (loop) => {
        this.isArchived = loop.isArchived;
      },
      error: (error) => {
        console.error('Error loading loop details:', error);
      }
    });
  }

  saveSettings(): void {
    if (!this.settings.name.trim()) {
      this.showMessage('Loop name is required', 'error');
      return;
    }

    if (this.settings.description.length > 500) {
      this.showMessage('Description must be 500 characters or less', 'error');
      return;
    }

    this.saving = true;
    this.loopService.updateLoopSettings(this.loopId, this.settings).subscribe({
      next: () => {
        this.showMessage('Settings saved successfully', 'success');
        this.saving = false;
      },
      error: (error) => {
        console.error('Error saving settings:', error);
        this.showMessage('Failed to save settings', 'error');
        this.saving = false;
      }
    });
  }

  archiveLoop(): void {
    if (!confirm('Archive this loop? Members won\'t be able to view items.')) {
      return;
    }

    this.loopService.archiveLoop(this.loopId).subscribe({
      next: () => {
        this.showMessage('Loop archived successfully', 'success');
        this.router.navigate(['/loops']);
      },
      error: (error) => {
        console.error('Error archiving loop:', error);
        this.showMessage('Failed to archive loop', 'error');
      }
    });
  }

  restoreLoop(): void {
    this.loopService.restoreLoop(this.loopId).subscribe({
      next: () => {
        this.showMessage('Loop restored successfully', 'success');
        this.isArchived = false;
      },
      error: (error) => {
        console.error('Error restoring loop:', error);
        this.showMessage('Failed to restore loop', 'error');
      }
    });
  }

  deleteLoop(): void {
    if (!confirm('Permanently delete this loop? This cannot be undone.')) {
      return;
    }

    this.loopService.deleteLoop(this.loopId).subscribe({
      next: () => {
        this.showMessage('Loop deleted successfully', 'success');
        this.router.navigate(['/loops']);
      },
      error: (error) => {
        console.error('Error deleting loop:', error);
        this.showMessage('Failed to delete loop', 'error');
      }
    });
  }

  goToTransferOwnership(): void {
    this.router.navigate(['/loops', this.loopId, 'transfer-ownership']);
  }

  goToMembers(): void {
    this.router.navigate(['/loops', this.loopId, 'members']);
  }

  private showMessage(message: string, type: 'success' | 'error'): void {
    this.message = message;
    this.messageType = type;
    setTimeout(() => {
      this.message = '';
    }, 3000);
  }
}
