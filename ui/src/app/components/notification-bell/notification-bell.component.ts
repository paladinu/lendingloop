import { Component, OnInit, OnDestroy, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatBadgeModule } from '@angular/material/badge';
import { NotificationService } from '../../services/notification.service';
import { NotificationDropdownComponent } from '../notification-dropdown/notification-dropdown.component';
import { interval, Subscription } from 'rxjs';
import { switchMap } from 'rxjs/operators';

@Component({
  selector: 'app-notification-bell',
  standalone: true,
  imports: [
    CommonModule,
    MatIconModule,
    MatButtonModule,
    MatBadgeModule,
    NotificationDropdownComponent
  ],
  templateUrl: './notification-bell.component.html',
  styleUrls: ['./notification-bell.component.css']
})
export class NotificationBellComponent implements OnInit, OnDestroy {
  unreadCount = 0;
  isDropdownOpen = false;
  private pollSubscription?: Subscription;

  constructor(private notificationService: NotificationService) { }

  ngOnInit(): void {
    this.loadUnreadCount();
    this.startPolling();
  }

  ngOnDestroy(): void {
    this.stopPolling();
  }

  loadUnreadCount(): void {
    this.notificationService.getUnreadCount().subscribe({
      next: (count) => {
        this.unreadCount = count;
      },
      error: (err) => {
        console.error('Error loading unread count:', err);
      }
    });
  }

  startPolling(): void {
    // Poll every 30 seconds
    this.pollSubscription = interval(30000)
      .pipe(
        switchMap(() => this.notificationService.getUnreadCount())
      )
      .subscribe({
        next: (count) => {
          this.unreadCount = count;
        },
        error: (err) => {
          console.error('Error polling unread count:', err);
        }
      });
  }

  stopPolling(): void {
    if (this.pollSubscription) {
      this.pollSubscription.unsubscribe();
    }
  }

  toggleDropdown(): void {
    this.isDropdownOpen = !this.isDropdownOpen;
  }

  closeDropdown(): void {
    this.isDropdownOpen = false;
  }

  updateUnreadCount(count: number): void {
    this.unreadCount = count;
  }

  onNotificationRead(unreadCount: number): void {
    this.unreadCount = unreadCount;
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    const target = event.target as HTMLElement;
    const clickedInside = target.closest('.notification-bell-container');
    if (!clickedInside && this.isDropdownOpen) {
      this.closeDropdown();
    }
  }
}
