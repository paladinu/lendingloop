import { ComponentFixture, TestBed } from '@angular/core/testing';
import { NotificationDropdownComponent } from './notification-dropdown.component';
import { NotificationService } from '../../services/notification.service';
import { Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { Notification, NotificationType } from '../../models/notification.interface';
import { NO_ERRORS_SCHEMA } from '@angular/core';

describe('NotificationDropdownComponent', () => {
  let component: NotificationDropdownComponent;
  let fixture: ComponentFixture<NotificationDropdownComponent>;
  let mockNotificationService: any;
  let mockRouter: any;

  const mockNotifications: Notification[] = [
    {
      id: '1',
      userId: 'user1',
      type: NotificationType.ItemRequestCreated,
      message: 'John requested to borrow your Drill',
      itemId: 'item1',
      itemRequestId: 'request1',
      relatedUserId: 'user2',
      isRead: false,
      createdAt: new Date('2024-01-01T10:00:00Z')
    },
    {
      id: '2',
      userId: 'user1',
      type: NotificationType.ItemRequestApproved,
      message: 'Jane approved your request for Ladder',
      itemId: 'item2',
      itemRequestId: 'request2',
      relatedUserId: 'user3',
      isRead: true,
      createdAt: new Date('2024-01-01T09:00:00Z')
    }
  ];

  beforeEach(async () => {
    mockNotificationService = {
      getNotifications: jest.fn().mockReturnValue(of([])),
      markAsRead: jest.fn().mockReturnValue(of(mockNotifications[0]))
    };

    mockRouter = {
      navigate: jest.fn().mockResolvedValue(true)
    };

    await TestBed.configureTestingModule({
      imports: [NotificationDropdownComponent],
      providers: [
        { provide: NotificationService, useValue: mockNotificationService },
        { provide: Router, useValue: mockRouter }
      ],
      schemas: [NO_ERRORS_SCHEMA]
    }).compileComponents();

    fixture = TestBed.createComponent(NotificationDropdownComponent);
    component = fixture.componentInstance;
    
    // Prevent automatic change detection
    fixture.autoDetectChanges(false);
  });

  afterEach(() => {
    if (fixture) {
      fixture.destroy();
    }
    jest.clearAllMocks();
  });

  it('should create', () => {
    //arrange
    mockNotificationService.getNotifications.mockReturnValue(of([]));
    
    //act
    fixture.detectChanges();
    
    //assert
    expect(component).toBeTruthy();
  });

  describe('notification list display', () => {
    it('should load notifications when dropdown opens', () => {
      //arrange
      mockNotificationService.getNotifications.mockReturnValue(of(mockNotifications));
      component.isOpen = true;

      //act
      component.ngOnInit();

      //assert
      expect(mockNotificationService.getNotifications).toHaveBeenCalledWith(10);
      expect(component.notifications).toEqual(mockNotifications);
    });

    it('should not load notifications when dropdown is closed', () => {
      //arrange
      component.isOpen = false;

      //act
      component.ngOnInit();

      //assert
      expect(mockNotificationService.getNotifications).not.toHaveBeenCalled();
    });

    it('should display notifications in the list', () => {
      //arrange
      mockNotificationService.getNotifications.mockReturnValue(of(mockNotifications));
      component.isOpen = true;

      //act
      component.loadNotifications();

      //assert
      expect(component.notifications.length).toBe(2);
      expect(component.isLoading).toBe(false);
    });

    it('should handle error when loading notifications', (done) => {
      //arrange
      const consoleErrorSpy = jest.spyOn(console, 'error').mockImplementation(() => {});
      const error = new Error('API error');
      mockNotificationService.getNotifications.mockReturnValue(throwError(() => error));
      component.isOpen = true;

      //act
      component.loadNotifications();

      //assert
      setTimeout(() => {
        expect(component.error).toBe('Failed to load notifications');
        expect(component.isLoading).toBe(false);
        expect(consoleErrorSpy).toHaveBeenCalledWith('Error loading notifications:', error);
        consoleErrorSpy.mockRestore();
        done();
      }, 0);
    });
  });

  describe('empty state', () => {
    it('should show empty state when no notifications', () => {
      //arrange
      mockNotificationService.getNotifications.mockReturnValue(of([]));
      component.isOpen = true;

      //act
      component.loadNotifications();

      //assert
      expect(component.notifications.length).toBe(0);
      expect(component.isLoading).toBe(false);
      expect(component.error).toBeNull();
    });
  });

  describe('mark as read on click', () => {
    it('should mark unread notification as read when clicked', () => {
      //arrange
      const unreadNotification = { ...mockNotifications[0], isRead: false };
      component.notifications = [unreadNotification];
      mockNotificationService.markAsRead.mockReturnValue(of({ ...unreadNotification, isRead: true }));

      //act
      component.onNotificationClick(unreadNotification);

      //assert
      expect(mockNotificationService.markAsRead).toHaveBeenCalledWith('1');
      expect(unreadNotification.isRead).toBe(true);
    });

    it('should not mark already read notification as read', () => {
      //arrange
      const readNotification = { ...mockNotifications[1], isRead: true };
      component.notifications = [readNotification];

      //act
      component.onNotificationClick(readNotification);

      //assert
      expect(mockNotificationService.markAsRead).not.toHaveBeenCalled();
    });

    it('should emit notification read event with updated count', () => {
      //arrange
      const unreadNotification1 = { ...mockNotifications[0], isRead: false };
      const unreadNotification2 = { ...mockNotifications[1], isRead: false };
      component.notifications = [unreadNotification1, unreadNotification2];
      mockNotificationService.markAsRead.mockReturnValue(of({ ...unreadNotification1, isRead: true }));
      const emitSpy = jest.spyOn(component.notificationRead, 'emit');

      //act
      component.onNotificationClick(unreadNotification1);

      //assert
      expect(emitSpy).toHaveBeenCalledWith(1);
    });

    it('should handle error when marking as read', (done) => {
      //arrange
      const consoleErrorSpy = jest.spyOn(console, 'error').mockImplementation(() => {});
      const unreadNotification = { ...mockNotifications[0], isRead: false };
      component.notifications = [unreadNotification];
      const error = new Error('API error');
      mockNotificationService.markAsRead.mockReturnValue(throwError(() => error));

      //act
      component.onNotificationClick(unreadNotification);

      //assert
      setTimeout(() => {
        expect(consoleErrorSpy).toHaveBeenCalledWith('Error marking notification as read:', error);
        consoleErrorSpy.mockRestore();
        done();
      }, 0);
    });
  });

  describe('navigation on notification click', () => {
    it('should navigate to requests page when notification has itemRequestId', () => {
      //arrange
      const notification = mockNotifications[0];
      const closeEmitSpy = jest.spyOn(component.closeDropdown, 'emit');

      //act
      component.onNotificationClick(notification);

      //assert
      expect(mockRouter.navigate).toHaveBeenCalledWith(['/requests']);
      expect(closeEmitSpy).toHaveBeenCalled();
    });

    it('should navigate to requests page when notification has itemId', () => {
      //arrange
      const notification = { ...mockNotifications[0], itemRequestId: undefined };
      const closeEmitSpy = jest.spyOn(component.closeDropdown, 'emit');

      //act
      component.onNotificationClick(notification);

      //assert
      expect(mockRouter.navigate).toHaveBeenCalledWith(['/requests']);
      expect(closeEmitSpy).toHaveBeenCalled();
    });
  });

  describe('notification type icons', () => {
    it('should return correct icon for ItemRequestCreated', () => {
      //arrange & act
      const icon = component.getNotificationIcon(NotificationType.ItemRequestCreated);

      //assert
      expect(icon).toBe('add_circle');
    });

    it('should return correct icon for ItemRequestApproved', () => {
      //arrange & act
      const icon = component.getNotificationIcon(NotificationType.ItemRequestApproved);

      //assert
      expect(icon).toBe('check_circle');
    });

    it('should return correct icon for ItemRequestRejected', () => {
      //arrange & act
      const icon = component.getNotificationIcon(NotificationType.ItemRequestRejected);

      //assert
      expect(icon).toBe('cancel');
    });

    it('should return correct icon for ItemRequestCompleted', () => {
      //arrange & act
      const icon = component.getNotificationIcon(NotificationType.ItemRequestCompleted);

      //assert
      expect(icon).toBe('done_all');
    });

    it('should return correct icon for ItemRequestCancelled', () => {
      //arrange & act
      const icon = component.getNotificationIcon(NotificationType.ItemRequestCancelled);

      //assert
      expect(icon).toBe('remove_circle');
    });
  });

  describe('notification icon colors', () => {
    it('should return correct color for ItemRequestCreated', () => {
      //arrange & act
      const color = component.getNotificationIconColor(NotificationType.ItemRequestCreated);

      //assert
      expect(color).toBe('primary');
    });

    it('should return correct color for ItemRequestApproved', () => {
      //arrange & act
      const color = component.getNotificationIconColor(NotificationType.ItemRequestApproved);

      //assert
      expect(color).toBe('success');
    });

    it('should return correct color for ItemRequestRejected', () => {
      //arrange & act
      const color = component.getNotificationIconColor(NotificationType.ItemRequestRejected);

      //assert
      expect(color).toBe('warn');
    });
  });

  describe('time ago formatting', () => {
    it('should return "Just now" for very recent notifications', () => {
      //arrange
      const now = new Date();

      //act
      const timeAgo = component.getTimeAgo(now);

      //assert
      expect(timeAgo).toBe('Just now');
    });

    it('should return minutes ago for recent notifications', () => {
      //arrange
      const fiveMinutesAgo = new Date(Date.now() - 5 * 60 * 1000);

      //act
      const timeAgo = component.getTimeAgo(fiveMinutesAgo);

      //assert
      expect(timeAgo).toBe('5 minutes ago');
    });

    it('should return hours ago for notifications within 24 hours', () => {
      //arrange
      const twoHoursAgo = new Date(Date.now() - 2 * 60 * 60 * 1000);

      //act
      const timeAgo = component.getTimeAgo(twoHoursAgo);

      //assert
      expect(timeAgo).toBe('2 hours ago');
    });

    it('should return days ago for notifications within a week', () => {
      //arrange
      const threeDaysAgo = new Date(Date.now() - 3 * 24 * 60 * 60 * 1000);

      //act
      const timeAgo = component.getTimeAgo(threeDaysAgo);

      //assert
      expect(timeAgo).toBe('3 days ago');
    });

    it('should return date for older notifications', () => {
      //arrange
      const tenDaysAgo = new Date(Date.now() - 10 * 24 * 60 * 60 * 1000);

      //act
      const timeAgo = component.getTimeAgo(tenDaysAgo);

      //assert
      expect(timeAgo).toContain('/');
    });
  });

  describe('view all notifications', () => {
    it('should navigate to notifications page', () => {
      //arrange
      const closeEmitSpy = jest.spyOn(component.closeDropdown, 'emit');

      //act
      component.viewAllNotifications();

      //assert
      expect(mockRouter.navigate).toHaveBeenCalledWith(['/notifications']);
      expect(closeEmitSpy).toHaveBeenCalled();
    });
  });
});
