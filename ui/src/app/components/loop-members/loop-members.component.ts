import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { LoopService } from '../../services/loop.service';
import { LoopMember } from '../../models/loop-member.interface';
import { Loop } from '../../models/loop.interface';
import { ToolbarComponent } from '../toolbar/toolbar.component';

@Component({
  selector: 'app-loop-members',
  standalone: true,
  imports: [CommonModule, ToolbarComponent],
  templateUrl: './loop-members.component.html',
  styleUrls: ['./loop-members.component.css']
})
export class LoopMembersComponent implements OnInit {
  loopId: string | null = null;
  loop: Loop | null = null;
  members: LoopMember[] = [];
  loading = false;
  error: string | null = null;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private loopService: LoopService
  ) {}

  ngOnInit(): void {
    this.loopId = this.route.snapshot.paramMap.get('id');
    if (this.loopId) {
      this.loadLoopDetails();
      this.loadMembers();
    }
  }

  loadLoopDetails(): void {
    if (!this.loopId) return;

    this.loopService.getLoopById(this.loopId).subscribe({
      next: (loop) => {
        this.loop = loop;
      },
      error: (err) => {
        console.error('Error loading loop:', err);
      }
    });
  }

  loadMembers(): void {
    if (!this.loopId) return;

    this.loading = true;
    this.error = null;

    this.loopService.getLoopMembers(this.loopId).subscribe({
      next: (members) => {
        this.members = members;
        this.loading = false;
      },
      error: (err) => {
        this.error = 'Failed to load members';
        this.loading = false;
        console.error('Error loading members:', err);
      }
    });
  }

  navigateBack(): void {
    if (this.loopId) {
      this.router.navigate(['/loops', this.loopId]);
    } else {
      this.router.navigate(['/loops']);
    }
  }

  getMemberDisplayName(member: LoopMember): string {
    const name = `${member.firstName} ${member.lastName}`.trim();
    return name || member.email;
  }
}

