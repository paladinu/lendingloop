import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { LoopService } from '../../services/loop.service';
import { Loop } from '../../models/loop.interface';
import { ToolbarComponent } from '../toolbar/toolbar.component';

@Component({
  selector: 'app-archived-loops',
  standalone: true,
  imports: [CommonModule, ToolbarComponent],
  templateUrl: './archived-loops.component.html',
  styleUrl: './archived-loops.component.css'
})
export class ArchivedLoopsComponent implements OnInit {
  loops: Loop[] = [];
  loading: boolean = false;

  constructor(
    private loopService: LoopService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.loadArchivedLoops();
  }

  loadArchivedLoops(): void {
    this.loading = true;
    this.loopService.getArchivedLoops().subscribe({
      next: (loops) => {
        this.loops = loops;
        this.loading = false;
      },
      error: (error) => {
        console.error('Error loading archived loops:', error);
        this.loading = false;
      }
    });
  }

  restoreLoop(loopId: string): void {
    this.loopService.restoreLoop(loopId).subscribe({
      next: () => {
        this.router.navigate(['/loops', loopId]);
      },
      error: (error) => {
        console.error('Error restoring loop:', error);
      }
    });
  }

  deleteLoop(loopId: string): void {
    if (!confirm('Permanently delete this loop? This cannot be undone.')) {
      return;
    }

    this.loopService.deleteLoop(loopId).subscribe({
      next: () => {
        this.loops = this.loops.filter(l => l.id !== loopId);
      },
      error: (error) => {
        console.error('Error deleting loop:', error);
      }
    });
  }
}
