import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { LoopService } from '../../services/loop.service';

@Component({
  selector: 'app-loop-create',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './loop-create.component.html',
  styleUrls: ['./loop-create.component.css']
})
export class LoopCreateComponent {
  loopName = '';
  loading = false;
  error: string | null = null;

  constructor(
    private loopService: LoopService,
    private router: Router
  ) {}

  onSubmit(): void {
    if (!this.loopName.trim()) {
      this.error = 'Loop name is required';
      return;
    }

    this.loading = true;
    this.error = null;

    this.loopService.createLoop(this.loopName.trim()).subscribe({
      next: (loop) => {
        this.loading = false;
        this.router.navigate(['/loops', loop.id]);
      },
      error: (err) => {
        this.loading = false;
        this.error = err.error?.message || 'Failed to create loop. Please try again.';
        console.error('Error creating loop:', err);
      }
    });
  }

  onCancel(): void {
    this.router.navigate(['/loops']);
  }
}
