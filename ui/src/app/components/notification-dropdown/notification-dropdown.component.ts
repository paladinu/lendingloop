import { Component, OnInit, OnChanges, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatDividerModule } from '@angular/material/divider';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { NotificationService } from '../../services/notification.service';
import { Notification, NotificationType } from '../../models/notification.interface';

@Component({
  selector: 'app-notification-dropdown',
  standalone: true,
  imports: [
    CommonModule,
    MatIconModule,
    MatButtonModule,
    MatDividerModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './notification-dropdown.component.html',
  styleUrls: ['./notification-dropdown.component.css']
})
export class NotificationDropdownComponent implements OnInit, OnChanges {
  @Input() isOpen = false;
  @Output() notificationRead = new EventEmitter<number>();
  @Output() closeDropdown = new EventEmitter<void>();

  notifications: Notification[] = [];
  isLoading = false;
  error: string | null = null;

  constructor(
    private notificationService: NotificationService,
    private router: Router
  ) { }

  ngOnInit(): void {
    if (this.isOpen) {
      this.loadNotifications();
    }
  }

  ngOnChanges(): void {
    if (this.isOpen) {
      this.loadNotifications();
    }
  }

  loadNotifications(): void {
    this.isLoading = true;
    this.error = null;

    this.notificationService.getNotifications(10).subscribe({
      next: (notifications) => {
        this.notifications = notifications;
        this.isLoading = false;
      },
      error: (err) => {
        console.error('Error loading notifications:', err);
        this.error = 'Failed to load notifications';
        this.isLoading = false;
      }
    });
  }

  onNotificationClick(notification: Notification): void {
    // Mark as read if not already read
    if (!notification.isRead) {
      this.notificationService.markAsRead(notification.id).subscribe({
        next: () => {
          notification.isRead = true;
          const unreadCount = this.notifications.filter(n => !n.isRead).length;
          this.notificationRead.emit(unreadCount);
        },
        error: (err) => {
          console.error('Error marking notification as read:', err);
        }
      });
    }

    // Navigate to related item or request
    this.navigateToRelatedItem(notification);
    this.closeDropdown.emit();
  }

  navigateToRelatedItem(notification: Notification): void {
    if (notification.itemRequestId) {
      // Navigate to requests page - the user can see their requests there
      this.router.navigate(['/requests']);
    } else if (notification.itemId) {
      // Navigate to the item detail (via loop detail)
      // Since we don't have a direct item detail page, navigate to requests
      this.router.navigate(['/requests']);
    }
  }

  getNotificationIcon(type: NotificationType): string {
    switch (type) {
      case NotificationType.ItemRequestCreated:
        return 'add_circle';
      case NotificationType.ItemRequestApproved:
        return 'check_circle';
      case NotificationType.ItemRequestRejected:
        return 'cancel';
      case NotificationType.ItemRequestCompleted:
        return 'done_all';
      case NotificationType.ItemRequestCancelled:
        return 'remove_circle';
      default:
        return 'notifications';
    }
  }

  getNotificationIconColor(type: NotificationType): string {
    switch (type) {
      case NotificationType.ItemRequestCreated:
        return 'primary';
      case NotificationType.ItemRequestApproved:
        return 'success';
      case NotificationType.ItemRequestRejected:
        return 'warn';
      case NotificationType.ItemRequestCompleted:
        return 'success';
      case NotificationType.ItemRequestCancelled:
        return 'warn';
      default:
        return 'default';
    }
  }

  getTimeAgo(date: Date): string {
    const now = new Date();
    const notificationDate = new Date(date);
    const diffMs = now.getTime() - notificationDate.getTime();
    const diffMins = Math.floor(diffMs / 60000);
    const diffHours = Math.floor(diffMs / 3600000);
    const diffDays = Math.floor(diffMs / 86400000);

    if (diffMins < 1) {
      return 'Just now';
    } else if (diffMins < 60) {
      return `${diffMins} minute${diffMins > 1 ? 's' : ''} ago`;
    } else if (diffHours < 24) {
      return `${diffHours} hour${diffHours > 1 ? 's' : ''} ago`;
    } else if (diffDays < 7) {
      return `${diffDays} day${diffDays > 1 ? 's' : ''} ago`;
    } else {
      return notificationDate.toLocaleDateString();
    }
  }

  viewAllNotifications(): void {
    this.router.navigate(['/notifications']);
    this.closeDropdown.emit();
  }
}
