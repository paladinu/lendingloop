import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { LoopService } from '../../services/loop.service';
import { Loop } from '../../models/loop.interface';
import { ToolbarComponent } from '../toolbar/toolbar.component';

@Component({
  selector: 'app-loop-list',
  standalone: true,
  imports: [CommonModule, ToolbarComponent],
  templateUrl: './loop-list.component.html',
  styleUrls: ['./loop-list.component.css']
})
export class LoopListComponent implements OnInit {
  loops: Loop[] = [];
  loading = false;
  error: string | null = null;

  constructor(
    private loopService: LoopService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.loadLoops();
  }

  loadLoops(): void {
    this.loading = true;
    this.error = null;

    this.loopService.getUserLoops().subscribe({
      next: (loops) => {
        this.loops = loops;
        this.loading = false;
      },
      error: (err) => {
        this.error = 'Failed to load loops. Please try again.';
        this.loading = false;
        console.error('Error loading loops:', err);
      }
    });
  }

  navigateToLoop(loopId: string | undefined): void {
    if (loopId) {
      this.router.navigate(['/loops', loopId]);
    }
  }

  navigateToCreateLoop(): void {
    this.router.navigate(['/loops/create']);
  }

  navigateToHome(): void {
    this.router.navigate(['/']);
  }

  navigateToArchivedLoops(): void {
    this.router.navigate(['/loops/archived']);
  }

  navigateToPublicLoops(): void {
    this.router.navigate(['/loops/public']);
  }
}
