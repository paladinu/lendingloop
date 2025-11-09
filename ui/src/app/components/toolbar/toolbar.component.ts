import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { MatDividerModule } from '@angular/material/divider';
import { AuthService } from '../../services/auth.service';
import { UserProfile } from '../../models/auth.interface';
import { ItemRequestService } from '../../services/item-request.service';
import { NotificationBellComponent } from '../notification-bell/notification-bell.component';

@Component({
  selector: 'app-toolbar',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    MatToolbarModule,
    MatButtonModule,
    MatIconModule,
    MatMenuModule,
    MatDividerModule,
    NotificationBellComponent
  ],
  templateUrl: './toolbar.component.html',
  styleUrls: ['./toolbar.component.css']
})
export class ToolbarComponent implements OnInit {
  title = 'Lending Loop';
  currentUser: UserProfile | null = null;
  pendingRequestCount = 0;

  constructor(
    private authService: AuthService,
    private router: Router,
    private itemRequestService: ItemRequestService
  ) { }

  ngOnInit(): void {
    this.loadCurrentUser();
    this.loadPendingRequestCount();
  }

  loadCurrentUser(): void {
    this.authService.getCurrentUser().subscribe({
      next: (user) => {
        this.currentUser = user;
      },
      error: (err) => {
        console.error('Error loading current user:', err);
        // If we can't get the current user, redirect to login
        this.logout();
      }
    });
  }

  loadPendingRequestCount(): void {
    this.itemRequestService.getPendingRequests().subscribe({
      next: (requests) => {
        this.pendingRequestCount = requests.length;
      },
      error: (err) => {
        console.error('Error loading pending request count:', err);
      }
    });
  }

  logout(): void {
    this.authService.logout();
    this.router.navigate(['/login']);
  }

  getUserDisplayName(): string {
    if (this.currentUser) {
      return `${this.currentUser.firstName} ${this.currentUser.lastName}`;
    }
    return 'User';
  }
}
