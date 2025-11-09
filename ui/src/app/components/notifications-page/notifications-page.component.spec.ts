import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router, ActivatedRoute } from '@angular/router';
import { NotificationsPageComponent } from './notifications-page.component';
import { NotificationService } from '../../services/notification.service';
import { Notification, NotificationType } from '../../models/notification.interface';
import { of, throwError } from 'rxjs';
import { NO_ERRORS_SCHEMA, Component } from '@angular/core';

// Mock ToolbarComponent
@Component({
  selector: 'app-toolbar',
  template: '',
  standalone: true
})
class MockToolbarComponent { }

describe('NotificationsPageComponent', () => {
  let component: NotificationsPageComponent;
  let fixture: ComponentFixture<NotificationsPageComponent>;
  let mockNotificationService: any;
  let mockRouter: any;
  let mockNotifications: Notification[];

  const getMockNotifications = (): Notification[] => [
    {
      id: '1',
      userId: 'user1',
      type: NotificationType.ItemRequestCreated,
      message: 'User requested your item',
      itemId: 'item1',
      itemRequestId: 'request1',
      isRead: false,
      createdAt: new Date('2024-01-01T10:00:00Z')
    },
    {
      id: '2',
      userId: 'user1',
      type: NotificationType.ItemRequestApproved,
      message: 'Your request was approved',
      itemId: 'item2',
      itemRequestId: 'request2',
      isRead: true,
      createdAt: new Date('2024-01-01T09:00:00Z')
    },
    {
      id: '3',
      userId: 'user1',
      type: NotificationType.ItemRequestRejected,
      message: 'Your request was rejected',
      itemId: 'item3',
      itemRequestId: 'request3',
      isRead: false,
      createdAt: new Date('2024-01-01T08:00:00Z')
    }
  ];

  beforeEach(async () => {
    mockNotifications = getMockNotifications();
    
    mockNotificationService = {
      getNotifications: jest.fn().mockImplementation(() => of(getMockNotifications())),
      getUnreadCount: jest.fn().mockReturnValue(of(2)),
      markAsRead: jest.fn().mockReturnValue(of({ ...mockNotifications[0], isRead: true })),
      markAllAsRead: jest.fn().mockReturnValue(of(true)),
      deleteNotification: jest.fn().mockReturnValue(of(true))
    };

    mockRouter = {
      navigate: jest.fn().mockResolvedValue(true)
    };

    await TestBed.configureTestingModule({
      imports: [NotificationsPageComponent],
      providers: [
        { provide: NotificationService, useValue: mockNotificationService },
        { provide: Router, useValue: mockRouter },
        { provide: ActivatedRoute, useValue: {} }
      ],
      schemas: [NO_ERRORS_SCHEMA]
    })
    .overrideComponent(NotificationsPageComponent, {
      remove: {
        imports: [require('../toolbar/toolbar.component').ToolbarComponent]
      },
      add: {
        imports: [MockToolbarComponent]
      }
    })
    .compileComponents();

    fixture = TestBed.createComponent(NotificationsPageComponent);
    component = fixture.componentInstance;
    
    // Prevent ngOnInit from being called automatically
    fixture.autoDetectChanges(false);
  });

  afterEach(() => {
    if (fixture) {
      fixture.destroy();
    }
    jest.clearAllMocks();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  describe('notification list display', () => {
    it('should load notifications on init', () => {
      //arrange
      mockNotificationService.getNotifications.mockReturnValue(of(mockNotifications));

      //act
      fixture.detectChanges();

      //assert
      expect(component.notifications).toEqual(mockNotifications);
      expect(mockNotificationService.getNotifications).toHaveBeenCalled();
    });

    it('should display all notifications by default', () => {
      //arrange
      mockNotificationService.getNotifications.mockReturnValue(of(mockNotifications));

      //act
      fixture.detectChanges();

      //assert
      expect(component.filteredNotifications.length).toBe(3);
    });

    it('should handle empty notification list', () => {
      //arrange
      component.notifications = [];
      mockNotificationService.getNotifications.mockReturnValue(of([]));

      //act
      component.loadNotifications();
      fixture.detectChanges();

      //assert
      expect(component.notifications.length).toBe(0);
      expect(component.hasNotifications).toBe(false);
    });

    it('should handle error when loading notifications', (done) => {
      //arrange
      const consoleErrorSpy = jest.spyOn(console, 'error').mockImplementation(() => {});
      const error = new Error('API error');
      mockNotificationService.getNotifications.mockReturnValue(throwError(() => error));

      //act
      component.loadNotifications();
      fixture.detectChanges();

      //assert
      setTimeout(() => {
        expect(component.error).toBe('Failed to load notifications');
        expect(consoleErrorSpy).toHaveBeenCalledWith('Error loading notifications:', error);
        consoleErrorSpy.mockRestore();
        done();
      }, 0);
    });
  });

  describe('filter functionality', () => {
    beforeEach(() => {
      fixture.detectChanges();
    });

    it('should filter to show only unread notifications', () => {
      //arrange
      component.notifications = mockNotifications;

      //act
      component.setFilter('unread');

      //assert
      expect(component.filteredNotifications.length).toBe(2);
      expect(component.filteredNotifications.every(n => !n.isRead)).toBe(true);
    });

    it('should filter to show only read notifications', () => {
      //arrange
      component.notifications = mockNotifications;

      //act
      component.setFilter('read');

      //assert
      expect(component.filteredNotifications.length).toBe(1);
      expect(component.filteredNotifications.every(n => n.isRead)).toBe(true);
    });

    it('should show all notifications when filter is set to all', () => {
      //arrange
      component.notifications = mockNotifications;
      component.setFilter('unread');

      //act
      component.setFilter('all');

      //assert
      expect(component.filteredNotifications.length).toBe(3);
    });

    it('should update filter status', () => {
      //arrange
      component.notifications = mockNotifications;

      //act
      component.setFilter('unread');

      //assert
      expect(component.filterStatus).toBe('unread');
    });
  });

  describe('mark all as read', () => {
    beforeEach(() => {
      fixture.detectChanges();
    });

    it('should mark all notifications as read', () => {
      //arrange
      component.notifications = [...mockNotifications];
      mockNotificationService.markAllAsRead.mockReturnValue(of(true));

      //act
      component.markAllAsRead();

      //assert
      expect(mockNotificationService.markAllAsRead).toHaveBeenCalled();
      expect(component.notifications.every(n => n.isRead)).toBe(true);
    });

    it('should handle error when marking all as read', (done) => {
      //arrange
      const consoleErrorSpy = jest.spyOn(console, 'error').mockImplementation(() => {});
      const error = new Error('API error');
      mockNotificationService.markAllAsRead.mockReturnValue(throwError(() => error));

      //act
      component.markAllAsRead();

      //assert
      setTimeout(() => {
        expect(component.error).toBe('Failed to mark all as read');
        expect(consoleErrorSpy).toHaveBeenCalledWith('Error marking all as read:', error);
        consoleErrorSpy.mockRestore();
        done();
      }, 0);
    });
  });

  describe('delete notification', () => {
    beforeEach(() => {
      fixture.detectChanges();
    });

    it('should delete notification successfully', () => {
      //arrange
      component.notifications = [...mockNotifications];
      mockNotificationService.deleteNotification.mockReturnValue(of(true));

      //act
      component.deleteNotification('1');

      //assert
      expect(mockNotificationService.deleteNotification).toHaveBeenCalledWith('1');
      expect(component.notifications.length).toBe(2);
      expect(component.notifications.find(n => n.id === '1')).toBeUndefined();
    });

    it('should handle error when deleting notification', (done) => {
      //arrange
      const consoleErrorSpy = jest.spyOn(console, 'error').mockImplementation(() => {});
      const error = new Error('API error');
      mockNotificationService.deleteNotification.mockReturnValue(throwError(() => error));

      //act
      component.deleteNotification('1');

      //assert
      setTimeout(() => {
        expect(component.error).toBe('Failed to delete notification');
        expect(consoleErrorSpy).toHaveBeenCalledWith('Error deleting notification:', error);
        consoleErrorSpy.mockRestore();
        done();
      }, 0);
    });
  });

  describe('navigation', () => {
    beforeEach(() => {
      fixture.detectChanges();
    });

    it('should navigate to requests when notification with itemRequestId is clicked', () => {
      //arrange
      const notification = mockNotifications[0];
      mockNotificationService.markAsRead.mockReturnValue(of({ ...notification, isRead: true }));

      //act
      component.onNotificationClick(notification);

      //assert
      expect(mockRouter.navigate).toHaveBeenCalledWith(['/requests']);
    });

    it('should navigate to loops when notification with only itemId is clicked', () => {
      //arrange
      const notification = { ...mockNotifications[0], itemRequestId: undefined };
      component.notifications = [notification];
      mockNotificationService.markAsRead.mockReturnValue(of({ ...notification, isRead: true }));

      //act
      component.onNotificationClick(notification);

      //assert
      expect(mockRouter.navigate).toHaveBeenCalledWith(['/loops']);
    });

    it('should mark notification as read when clicked', () => {
      //arrange
      const notification = { ...mockNotifications[0], isRead: false };
      component.notifications = [notification];
      mockNotificationService.markAsRead.mockReturnValue(of({ ...notification, isRead: true }));

      //act
      component.onNotificationClick(notification);

      //assert
      expect(mockNotificationService.markAsRead).toHaveBeenCalledWith(notification.id);
    });

    it('should not mark already read notification as read again', () => {
      //arrange
      const notification = mockNotifications[1]; // This one is already read

      //act
      component.onNotificationClick(notification);

      //assert
      expect(mockNotificationService.markAsRead).not.toHaveBeenCalled();
    });
  });

  describe('unread count', () => {
    it('should calculate unread count correctly', () => {
      //arrange
      component.notifications = [...mockNotifications];

      //act
      const unreadNotifications = component.notifications.filter(n => !n.isRead);
      const count = component.unreadCount;

      //assert
      expect(component.notifications.length).toBe(3);
      expect(unreadNotifications.length).toBe(2);
      expect(count).toBe(2);
    });

    it('should return zero when all notifications are read', () => {
      //arrange
      component.notifications = mockNotifications.map(n => ({ ...n, isRead: true }));

      //act
      const count = component.unreadCount;

      //assert
      expect(count).toBe(0);
    });
  });

  describe('helper methods', () => {
    it('should return correct icon for notification type', () => {
      //arrange & act & assert
      expect(component.getNotificationIcon(NotificationType.ItemRequestCreated)).toBe('add_circle');
      expect(component.getNotificationIcon(NotificationType.ItemRequestApproved)).toBe('check_circle');
      expect(component.getNotificationIcon(NotificationType.ItemRequestRejected)).toBe('cancel');
      expect(component.getNotificationIcon(NotificationType.ItemRequestCompleted)).toBe('task_alt');
      expect(component.getNotificationIcon(NotificationType.ItemRequestCancelled)).toBe('remove_circle');
    });

    it('should return correct icon color for notification type', () => {
      //arrange & act & assert
      expect(component.getNotificationIconColor(NotificationType.ItemRequestCreated)).toBe('primary');
      expect(component.getNotificationIconColor(NotificationType.ItemRequestApproved)).toBe('accent');
      expect(component.getNotificationIconColor(NotificationType.ItemRequestRejected)).toBe('warn');
      expect(component.getNotificationIconColor(NotificationType.ItemRequestCompleted)).toBe('accent');
      expect(component.getNotificationIconColor(NotificationType.ItemRequestCancelled)).toBe('');
    });

    it('should format time ago correctly', () => {
      //arrange
      const now = new Date();
      const oneMinuteAgo = new Date(now.getTime() - 60000);
      const oneHourAgo = new Date(now.getTime() - 3600000);
      const oneDayAgo = new Date(now.getTime() - 86400000);

      //act & assert
      expect(component.getTimeAgo(now)).toBe('Just now');
      expect(component.getTimeAgo(oneMinuteAgo)).toContain('minute');
      expect(component.getTimeAgo(oneHourAgo)).toContain('hour');
      expect(component.getTimeAgo(oneDayAgo)).toContain('day');
    });
  });
});
