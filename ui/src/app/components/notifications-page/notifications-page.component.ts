import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDividerModule } from '@angular/material/divider';
import { ToolbarComponent } from '../toolbar/toolbar.component';
import { NotificationService } from '../../services/notification.service';
import { Notification, NotificationType } from '../../models/notification.interface';

@Component({
  selector: 'app-notifications-page',
  standalone: true,
  imports: [
    CommonModule,
    MatIconModule,
    MatButtonModule,
    MatProgressSpinnerModule,
    MatDividerModule,
    ToolbarComponent
  ],
  templateUrl: './notifications-page.component.html',
  styleUrls: ['./notifications-page.component.css']
})
export class NotificationsPageComponent implements OnInit {
  notifications: Notification[] = [];
  filteredNotifications: Notification[] = [];
  isLoading = false;
  error = '';
  filterStatus: 'all' | 'unread' | 'read' = 'all';

  constructor(
    private notificationService: NotificationService,
    private router: Router
  ) { }

  ngOnInit(): void {
    this.loadNotifications();
  }

  loadNotifications(): void {
    this.isLoading = true;
    this.error = '';

    this.notificationService.getNotifications().subscribe({
      next: (notifications) => {
        this.notifications = notifications;
        this.applyFilter();
        this.isLoading = false;
      },
      error: (err) => {
        this.error = 'Failed to load notifications';
        console.error('Error loading notifications:', err);
        this.isLoading = false;
      }
    });
  }

  applyFilter(): void {
    if (this.filterStatus === 'all') {
      this.filteredNotifications = this.notifications;
    } else if (this.filterStatus === 'unread') {
      this.filteredNotifications = this.notifications.filter(n => !n.isRead);
    } else {
      this.filteredNotifications = this.notifications.filter(n => n.isRead);
    }
  }

  setFilter(status: 'all' | 'unread' | 'read'): void {
    this.filterStatus = status;
    this.applyFilter();
  }

  markAllAsRead(): void {
    this.notificationService.markAllAsRead().subscribe({
      next: () => {
        this.notifications.forEach(n => n.isRead = true);
        this.applyFilter();
      },
      error: (err) => {
        this.error = 'Failed to mark all as read';
        console.error('Error marking all as read:', err);
      }
    });
  }

  deleteNotification(notificationId: string): void {
    this.notificationService.deleteNotification(notificationId).subscribe({
      next: () => {
        this.notifications = this.notifications.filter(n => n.id !== notificationId);
        this.applyFilter();
      },
      error: (err) => {
        this.error = 'Failed to delete notification';
        console.error('Error deleting notification:', err);
      }
    });
  }

  onNotificationClick(notification: Notification): void {
    // Mark as read if unread
    if (!notification.isRead) {
      this.notificationService.markAsRead(notification.id).subscribe({
        next: (updatedNotification) => {
          const index = this.notifications.findIndex(n => n.id === notification.id);
          if (index !== -1) {
            this.notifications[index] = updatedNotification;
            this.applyFilter();
          }
        },
        error: (err) => {
          console.error('Error marking notification as read:', err);
        }
      });
    }

    // Navigate to related item or request
    if (notification.itemRequestId) {
      this.router.navigate(['/requests']);
    } else if (notification.itemId) {
      this.router.navigate(['/loops']);
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
        return 'task_alt';
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
        return 'accent';
      case NotificationType.ItemRequestRejected:
        return 'warn';
      case NotificationType.ItemRequestCompleted:
        return 'accent';
      case NotificationType.ItemRequestCancelled:
        return '';
      default:
        return '';
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

  get unreadCount(): number {
    return this.notifications.filter(n => !n.isRead).length;
  }

  get hasNotifications(): boolean {
    return this.filteredNotifications.length > 0;
  }
}
