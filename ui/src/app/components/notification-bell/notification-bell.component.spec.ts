import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { NotificationBellComponent } from './notification-bell.component';
import { NotificationService } from '../../services/notification.service';
import { of, throwError, NEVER } from 'rxjs';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import * as rxjs from 'rxjs';

describe('NotificationBellComponent', () => {
  let component: NotificationBellComponent;
  let fixture: ComponentFixture<NotificationBellComponent>;
  let mockNotificationService: any;
  let intervalSpy: jest.SpyInstance;
  let consoleErrorSpy: jest.SpyInstance;

  beforeEach(async () => {
    // Mock interval to prevent real polling
    intervalSpy = jest.spyOn(rxjs, 'interval').mockReturnValue(NEVER);

    // Suppress console.error during tests
    consoleErrorSpy = jest.spyOn(console, 'error').mockImplementation(() => {});

    mockNotificationService = {
      getUnreadCount: jest.fn().mockReturnValue(of(0))
    };

    await TestBed.configureTestingModule({
      imports: [NotificationBellComponent],
      providers: [
        { provide: NotificationService, useValue: mockNotificationService }
      ],
      schemas: [NO_ERRORS_SCHEMA]
    }).compileComponents();

    fixture = TestBed.createComponent(NotificationBellComponent);
    component = fixture.componentInstance;
    
    // Prevent automatic change detection
    fixture.autoDetectChanges(false);
  });

  afterEach(() => {
    // Ensure all subscriptions are cleaned up before destroying
    if (component) {
      component.ngOnDestroy();
    }
    if (fixture) {
      fixture.destroy();
    }
    jest.clearAllMocks();
    intervalSpy.mockRestore();
    consoleErrorSpy.mockRestore();
  });

  it('should create', () => {
    //arrange
    mockNotificationService.getUnreadCount.mockReturnValue(of(0));
    
    //act
    fixture.detectChanges();
    
    //assert
    expect(component).toBeTruthy();
  });

  describe('unread count display', () => {
    it('should load unread count on init', fakeAsync(() => {
      //arrange
      mockNotificationService.getUnreadCount.mockReturnValue(of(5));

      //act
      component.loadUnreadCount();
      tick();

      //assert
      expect(component.unreadCount).toBe(5);
      expect(mockNotificationService.getUnreadCount).toHaveBeenCalled();
    }));

    it('should display zero unread count initially', fakeAsync(() => {
      //arrange
      mockNotificationService.getUnreadCount.mockReturnValue(of(0));

      //act
      fixture.detectChanges();
      tick();

      //assert
      expect(component.unreadCount).toBe(0);
    }));

    it('should handle error when loading unread count', fakeAsync(() => {
      //arrange
      const error = new Error('API error');
      mockNotificationService.getUnreadCount.mockReturnValue(throwError(() => error));

      //act
      component.loadUnreadCount();
      tick();

      //assert
      expect(consoleErrorSpy).toHaveBeenCalledWith('Error loading unread count:', error);
      expect(component.unreadCount).toBe(0);
    }));
  });

  describe('badge visibility logic', () => {
    it('should hide badge when count is zero', fakeAsync(() => {
      //arrange
      mockNotificationService.getUnreadCount.mockReturnValue(of(0));

      //act
      component.loadUnreadCount();
      tick();

      //assert
      expect(component.unreadCount).toBe(0);
    }));

    it('should show badge when count is greater than zero', fakeAsync(() => {
      //arrange
      mockNotificationService.getUnreadCount.mockReturnValue(of(3));

      //act
      component.loadUnreadCount();
      tick();

      //assert
      expect(component.unreadCount).toBe(3);
    }));
  });

  describe('dropdown toggle', () => {
    it('should toggle dropdown state when clicked', fakeAsync(() => {
      //arrange
      fixture.detectChanges();
      tick();
      expect(component.isDropdownOpen).toBe(false);

      //act
      component.toggleDropdown();

      //assert
      expect(component.isDropdownOpen).toBe(true);
    }));

    it('should toggle dropdown back to closed', fakeAsync(() => {
      //arrange
      fixture.detectChanges();
      tick();
      component.isDropdownOpen = true;

      //act
      component.toggleDropdown();

      //assert
      expect(component.isDropdownOpen).toBe(false);
    }));
  });

  describe('polling mechanism', () => {
    it('should start polling on init', fakeAsync(() => {
      //arrange
      mockNotificationService.getUnreadCount.mockReturnValue(of(2));
      
      //act
      fixture.detectChanges();
      tick();

      //assert
      expect(component['pollSubscription']).toBeDefined();
      expect(mockNotificationService.getUnreadCount).toHaveBeenCalledTimes(1);
      
      // Cleanup
      component.ngOnDestroy();
      tick();
    }));

    it('should call loadUnreadCount method', fakeAsync(() => {
      //arrange
      const loadSpy = jest.spyOn(component, 'loadUnreadCount');
      mockNotificationService.getUnreadCount.mockReturnValue(of(5));

      //act
      component.loadUnreadCount();
      tick();

      //assert
      expect(loadSpy).toHaveBeenCalled();
      expect(component.unreadCount).toBe(5);
    }));

    it('should handle errors when loading unread count', fakeAsync(() => {
      //arrange
      const error = new Error('API error');
      mockNotificationService.getUnreadCount.mockReturnValue(throwError(() => error));

      //act
      component.loadUnreadCount();
      tick();

      //assert
      expect(consoleErrorSpy).toHaveBeenCalledWith('Error loading unread count:', error);
      expect(component.unreadCount).toBe(0);
    }));

    it('should stop polling on component destroy', fakeAsync(() => {
      //arrange
      fixture.detectChanges();
      tick();
      const unsubscribeSpy = jest.spyOn(component['pollSubscription']!, 'unsubscribe');

      //act
      component.ngOnDestroy();
      tick();

      //assert
      expect(unsubscribeSpy).toHaveBeenCalled();
    }));
  });

  describe('updateUnreadCount', () => {
    it('should update unread count when called', fakeAsync(() => {
      //arrange
      fixture.detectChanges();
      tick();
      component.unreadCount = 5;

      //act
      component.updateUnreadCount(10);

      //assert
      expect(component.unreadCount).toBe(10);
    }));

    it('should update unread count to zero', fakeAsync(() => {
      //arrange
      fixture.detectChanges();
      tick();
      component.unreadCount = 5;

      //act
      component.updateUnreadCount(0);

      //assert
      expect(component.unreadCount).toBe(0);
    }));
  });
});
