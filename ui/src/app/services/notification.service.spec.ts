import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { Router } from '@angular/router';
import { NotificationService } from './notification.service';
import { AuthService } from './auth.service';
import { Notification, NotificationType } from '../models/notification.interface';

// Mock environment
jest.mock('../../environments/environment', () => ({
  environment: {
    production: false,
    apiUrl: 'http://localhost:8080'
  }
}));

describe('NotificationService', () => {
    let service: NotificationService;
    let httpMock: HttpTestingController;
    let authService: any;
    let router: any;
    let consoleErrorSpy: jest.SpyInstance;

    const apiUrl = 'http://localhost:8080/api/notifications';

    beforeEach(() => {
        // Suppress console.error during tests
        consoleErrorSpy = jest.spyOn(console, 'error').mockImplementation(() => {});

        const authServiceSpy = {
            logout: jest.fn()
        };
        const routerSpy = {
            navigate: jest.fn()
        };

        TestBed.configureTestingModule({
            imports: [HttpClientTestingModule],
            providers: [
                NotificationService,
                { provide: AuthService, useValue: authServiceSpy },
                { provide: Router, useValue: routerSpy }
            ]
        });

        service = TestBed.inject(NotificationService);
        httpMock = TestBed.inject(HttpTestingController);
        authService = TestBed.inject(AuthService);
        router = TestBed.inject(Router);
    });

    afterEach(() => {
        httpMock.verify();
        consoleErrorSpy.mockRestore();
    });

    it('should be created', () => {
        //assert
        expect(service).toBeTruthy();
    });

    describe('getNotifications', () => {
        it('should fetch notifications without limit', () => {
            //arrange
            const mockNotifications: Notification[] = [
                {
                    id: 'notif1',
                    userId: 'user123',
                    type: NotificationType.ItemRequestCreated,
                    message: 'John requested to borrow your Drill',
                    itemId: 'item123',
                    itemRequestId: 'req123',
                    relatedUserId: 'user456',
                    isRead: false,
                    createdAt: new Date()
                },
                {
                    id: 'notif2',
                    userId: 'user123',
                    type: NotificationType.ItemRequestApproved,
                    message: 'Jane approved your request for Hammer',
                    itemId: 'item456',
                    itemRequestId: 'req456',
                    relatedUserId: 'user789',
                    isRead: true,
                    createdAt: new Date()
                }
            ];

            //act
            service.getNotifications().subscribe(notifications => {
                //assert
                expect(notifications.length).toBe(2);
                expect(notifications).toEqual(mockNotifications);
            });

            const req = httpMock.expectOne(apiUrl);
            expect(req.request.method).toBe('GET');
            expect(req.request.params.has('limit')).toBe(false);
            req.flush(mockNotifications);
        });

        it('should fetch notifications with limit parameter', () => {
            //arrange
            const limit = 10;
            const mockNotifications: Notification[] = [
                {
                    id: 'notif1',
                    userId: 'user123',
                    type: NotificationType.ItemRequestCreated,
                    message: 'John requested to borrow your Drill',
                    itemId: 'item123',
                    itemRequestId: 'req123',
                    relatedUserId: 'user456',
                    isRead: false,
                    createdAt: new Date()
                }
            ];

            //act
            service.getNotifications(limit).subscribe(notifications => {
                //assert
                expect(notifications.length).toBe(1);
                expect(notifications).toEqual(mockNotifications);
            });

            const req = httpMock.expectOne(`${apiUrl}?limit=${limit}`);
            expect(req.request.method).toBe('GET');
            expect(req.request.params.get('limit')).toBe(limit.toString());
            req.flush(mockNotifications);
        });

        it('should handle error when fetching notifications', () => {
            //arrange
            const errorMessage = 'Failed to fetch notifications';

            //act
            service.getNotifications().subscribe({
                next: () => fail('should have failed'),
                error: (error) => {
                    //assert
                    expect(error.message).toContain('An unexpected error occurred');
                }
            });

            const req = httpMock.expectOne(apiUrl);
            req.flush({ message: errorMessage }, { status: 500, statusText: 'Internal Server Error' });
        });
    });

    describe('getUnreadCount', () => {
        it('should fetch unread count successfully', () => {
            //arrange
            const mockCount = 5;

            //act
            service.getUnreadCount().subscribe(count => {
                //assert
                expect(count).toBe(mockCount);
            });

            const req = httpMock.expectOne(`${apiUrl}/unread-count`);
            expect(req.request.method).toBe('GET');
            req.flush(mockCount);
        });

        it('should return zero when no unread notifications', () => {
            //arrange
            const mockCount = 0;

            //act
            service.getUnreadCount().subscribe(count => {
                //assert
                expect(count).toBe(0);
            });

            const req = httpMock.expectOne(`${apiUrl}/unread-count`);
            expect(req.request.method).toBe('GET');
            req.flush(mockCount);
        });
    });

    describe('markAsRead', () => {
        it('should mark notification as read successfully', () => {
            //arrange
            const notificationId = 'notif123';
            const mockNotification: Notification = {
                id: notificationId,
                userId: 'user123',
                type: NotificationType.ItemRequestCreated,
                message: 'John requested to borrow your Drill',
                itemId: 'item123',
                itemRequestId: 'req123',
                relatedUserId: 'user456',
                isRead: true,
                createdAt: new Date()
            };

            //act
            service.markAsRead(notificationId).subscribe(notification => {
                //assert
                expect(notification.isRead).toBe(true);
                expect(notification.id).toBe(notificationId);
            });

            const req = httpMock.expectOne(`${apiUrl}/${notificationId}/read`);
            expect(req.request.method).toBe('PUT');
            req.flush(mockNotification);
        });

        it('should handle error when marking as read', () => {
            //arrange
            const notificationId = 'notif123';

            //act
            service.markAsRead(notificationId).subscribe({
                next: () => fail('should have failed'),
                error: (error) => {
                    //assert
                    expect(error.message).toContain('An unexpected error occurred');
                }
            });

            const req = httpMock.expectOne(`${apiUrl}/${notificationId}/read`);
            req.flush({}, { status: 404, statusText: 'Not Found' });
        });
    });

    describe('markAllAsRead', () => {
        it('should mark all notifications as read successfully', () => {
            //arrange
            const mockResponse = true;

            //act
            service.markAllAsRead().subscribe(result => {
                //assert
                expect(result).toBe(true);
            });

            const req = httpMock.expectOne(`${apiUrl}/mark-all-read`);
            expect(req.request.method).toBe('PUT');
            req.flush(mockResponse);
        });
    });

    describe('deleteNotification', () => {
        it('should delete notification successfully', () => {
            //arrange
            const notificationId = 'notif123';
            const mockResponse = true;

            //act
            service.deleteNotification(notificationId).subscribe(result => {
                //assert
                expect(result).toBe(true);
            });

            const req = httpMock.expectOne(`${apiUrl}/${notificationId}`);
            expect(req.request.method).toBe('DELETE');
            req.flush(mockResponse);
        });

        it('should handle error when deleting notification', () => {
            //arrange
            const notificationId = 'notif123';

            //act
            service.deleteNotification(notificationId).subscribe({
                next: () => fail('should have failed'),
                error: (error) => {
                    //assert
                    expect(error.message).toContain('An unexpected error occurred');
                }
            });

            const req = httpMock.expectOne(`${apiUrl}/${notificationId}`);
            req.flush({}, { status: 404, statusText: 'Not Found' });
        });
    });

    describe('error handling', () => {
        it('should handle 401 error and redirect to login', () => {
            //act
            service.getNotifications().subscribe({
                next: () => fail('should have failed'),
                error: (error) => {
                    //assert
                    expect(error.message).toContain('Authentication required');
                    expect(authService.logout).toHaveBeenCalled();
                    expect(router.navigate).toHaveBeenCalledWith(['/login']);
                }
            });

            const req = httpMock.expectOne(apiUrl);
            req.flush({}, { status: 401, statusText: 'Unauthorized' });
        });

        it('should handle 403 error', (done) => {
            //arrange
            const notificationId = 'notif123';

            //act
            service.markAsRead(notificationId).subscribe({
                next: () => {
                    fail('should have failed');
                    done();
                },
                error: (error) => {
                    //assert
                    expect(error.message).toContain('permission');
                    done();
                }
            });

            const req = httpMock.expectOne(`${apiUrl}/${notificationId}/read`);
            req.flush({}, { status: 403, statusText: 'Forbidden' });
        });
    });
});
