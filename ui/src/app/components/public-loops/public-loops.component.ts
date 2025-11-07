import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { LoopService } from '../../services/loop.service';
import { Loop } from '../../models/loop.interface';
import { ToolbarComponent } from '../toolbar/toolbar.component';

@Component({
  selector: 'app-public-loops',
  standalone: true,
  imports: [CommonModule, FormsModule, ToolbarComponent],
  templateUrl: './public-loops.component.html',
  styleUrl: './public-loops.component.css'
})
export class PublicLoopsComponent implements OnInit {
  loops: Loop[] = [];
  loading: boolean = false;
  searchTerm: string = '';
  skip: number = 0;
  limit: number = 20;
  hasMore: boolean = true;
  message: string = '';
  messageType: 'success' | 'error' = 'success';
  pendingRequests: Set<string> = new Set();

  constructor(
    private loopService: LoopService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.loadPublicLoops();
    this.checkPendingRequests();
  }

  loadPublicLoops(): void {
    this.loading = true;
    
    const request = this.searchTerm
      ? this.loopService.searchPublicLoops(this.searchTerm, this.skip, this.limit)
      : this.loopService.getPublicLoops(this.skip, this.limit);

    request.subscribe({
      next: (loops) => {
        if (this.skip === 0) {
          this.loops = loops;
        } else {
          this.loops = [...this.loops, ...loops];
        }
        this.hasMore = loops.length === this.limit;
        this.loading = false;
      },
      error: (error) => {
        console.error('Error loading public loops:', error);
        this.showMessage('Failed to load public loops', 'error');
        this.loading = false;
      }
    });
  }

  onSearch(): void {
    this.skip = 0;
    this.loops = [];
    this.loadPublicLoops();
  }

  clearSearch(): void {
    this.searchTerm = '';
    this.skip = 0;
    this.loops = [];
    this.loadPublicLoops();
  }

  loadMore(): void {
    if (!this.loading && this.hasMore) {
      this.skip += this.limit;
      this.loadPublicLoops();
    }
  }

  checkPendingRequests(): void {
    this.loopService.getMyJoinRequests().subscribe({
      next: (requests) => {
        this.pendingRequests = new Set(
          requests
            .filter(r => r.status === 'Pending')
            .map(r => r.loopId)
        );
      },
      error: (error) => {
        console.error('Error checking pending requests:', error);
      }
    });
  }

  hasPendingRequest(loopId: string): boolean {
    return this.pendingRequests.has(loopId);
  }

  requestToJoin(loopId: string, loopName: string): void {
    const message = prompt(`Request to join "${loopName}". Add an optional message:`);
    
    if (message === null) {
      return; // User cancelled
    }

    this.loopService.createJoinRequest(loopId, message || '').subscribe({
      next: () => {
        this.pendingRequests.add(loopId);
        this.showMessage('Join request sent successfully', 'success');
      },
      error: (error) => {
        console.error('Error creating join request:', error);
        const errorMessage = error.error?.message || 'Failed to send join request';
        this.showMessage(errorMessage, 'error');
      }
    });
  }

  viewLoop(loopId: string): void {
    this.router.navigate(['/loops', loopId]);
  }

  private showMessage(message: string, type: 'success' | 'error'): void {
    this.message = message;
    this.messageType = type;
    setTimeout(() => {
      this.message = '';
    }, 3000);
  }
}
