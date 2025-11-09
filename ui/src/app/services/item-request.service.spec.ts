import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { Router } from '@angular/router';
import { ItemRequestService } from './item-request.service';
import { AuthService } from './auth.service';
import { ItemRequest, RequestStatus } from '../models/item-request.interface';

// Mock environment
jest.mock('../../environments/environment', () => ({
  environment: {
    production: false,
    apiUrl: 'http://localhost:8080'
  }
}));

describe('ItemRequestService', () => {
    let service: ItemRequestService;
    let httpMock: HttpTestingController;
    let authService: any;
    let router: any;

    const apiUrl = 'http://localhost:8080/api/itemrequests';

    beforeEach(() => {
        const authServiceSpy = {
            logout: jest.fn()
        };
        const routerSpy = {
            navigate: jest.fn()
        };

        TestBed.configureTestingModule({
            imports: [HttpClientTestingModule],
            providers: [
                ItemRequestService,
                { provide: AuthService, useValue: authServiceSpy },
                { provide: Router, useValue: routerSpy }
            ]
        });

        service = TestBed.inject(ItemRequestService);
        httpMock = TestBed.inject(HttpTestingController);
        authService = TestBed.inject(AuthService);
        router = TestBed.inject(Router);
    });

    afterEach(() => {
        httpMock.verify();
    });

    it('should be created', () => {
        //assert
        expect(service).toBeTruthy();
    });

    describe('createRequest', () => {
        it('should create a request successfully', () => {
            //arrange
            const itemId = 'item123';
            const mockRequest: ItemRequest = {
                id: 'request123',
                itemId: itemId,
                requesterId: 'user123',
                ownerId: 'owner123',
                status: RequestStatus.Pending,
                requestedAt: new Date()
            };

            //act
            service.createRequest(itemId).subscribe(request => {
                //assert
                expect(request).toEqual(mockRequest);
            });

            const req = httpMock.expectOne(apiUrl);
            expect(req.request.method).toBe('POST');
            expect(req.request.body).toEqual({ itemId });
            req.flush(mockRequest);
        });

        it('should handle error when creating request', () => {
            //arrange
            const itemId = 'item123';
            const errorMessage = 'Cannot request your own item';

            //act
            service.createRequest(itemId).subscribe({
                next: () => fail('should have failed'),
                error: (error) => {
                    //assert
                    expect(error.message).toContain('An unexpected error occurred');
                }
            });

            const req = httpMock.expectOne(apiUrl);
            req.flush({ message: errorMessage }, { status: 400, statusText: 'Bad Request' });
        });
    });

    describe('getMyRequests', () => {
        it('should fetch user requests successfully', () => {
            //arrange
            const mockRequests: ItemRequest[] = [
                {
                    id: 'req1',
                    itemId: 'item1',
                    requesterId: 'user123',
                    ownerId: 'owner1',
                    status: RequestStatus.Pending,
                    requestedAt: new Date()
                },
                {
                    id: 'req2',
                    itemId: 'item2',
                    requesterId: 'user123',
                    ownerId: 'owner2',
                    status: RequestStatus.Approved,
                    requestedAt: new Date()
                }
            ];

            //act
            service.getMyRequests().subscribe(requests => {
                //assert
                expect(requests.length).toBe(2);
                expect(requests).toEqual(mockRequests);
            });

            const req = httpMock.expectOne(`${apiUrl}/my-requests`);
            expect(req.request.method).toBe('GET');
            req.flush(mockRequests);
        });
    });

    describe('getPendingRequests', () => {
        it('should fetch pending requests successfully', () => {
            //arrange
            const mockRequests: ItemRequest[] = [
                {
                    id: 'req1',
                    itemId: 'item1',
                    requesterId: 'user1',
                    ownerId: 'owner123',
                    status: RequestStatus.Pending,
                    requestedAt: new Date()
                }
            ];

            //act
            service.getPendingRequests().subscribe(requests => {
                //assert
                expect(requests.length).toBe(1);
                expect(requests[0].status).toBe(RequestStatus.Pending);
            });

            const req = httpMock.expectOne(`${apiUrl}/pending`);
            expect(req.request.method).toBe('GET');
            req.flush(mockRequests);
        });
    });

    describe('getRequestsForItem', () => {
        it('should fetch requests for specific item', () => {
            //arrange
            const itemId = 'item123';
            const mockRequests: ItemRequest[] = [
                {
                    id: 'req1',
                    itemId: itemId,
                    requesterId: 'user1',
                    ownerId: 'owner123',
                    status: RequestStatus.Pending,
                    requestedAt: new Date()
                }
            ];

            //act
            service.getRequestsForItem(itemId).subscribe(requests => {
                //assert
                expect(requests.length).toBe(1);
                expect(requests[0].itemId).toBe(itemId);
            });

            const req = httpMock.expectOne(`${apiUrl}/item/${itemId}`);
            expect(req.request.method).toBe('GET');
            req.flush(mockRequests);
        });
    });

    describe('approveRequest', () => {
        it('should approve request successfully', () => {
            //arrange
            const requestId = 'request123';
            const mockRequest: ItemRequest = {
                id: requestId,
                itemId: 'item123',
                requesterId: 'user123',
                ownerId: 'owner123',
                status: RequestStatus.Approved,
                requestedAt: new Date(),
                respondedAt: new Date()
            };

            //act
            service.approveRequest(requestId).subscribe(request => {
                //assert
                expect(request.status).toBe(RequestStatus.Approved);
                expect(request.respondedAt).toBeDefined();
            });

            const req = httpMock.expectOne(`${apiUrl}/${requestId}/approve`);
            expect(req.request.method).toBe('PUT');
            req.flush(mockRequest);
        });
    });

    describe('rejectRequest', () => {
        it('should reject request successfully', () => {
            //arrange
            const requestId = 'request123';
            const mockRequest: ItemRequest = {
                id: requestId,
                itemId: 'item123',
                requesterId: 'user123',
                ownerId: 'owner123',
                status: RequestStatus.Rejected,
                requestedAt: new Date(),
                respondedAt: new Date()
            };

            //act
            service.rejectRequest(requestId).subscribe(request => {
                //assert
                expect(request.status).toBe(RequestStatus.Rejected);
            });

            const req = httpMock.expectOne(`${apiUrl}/${requestId}/reject`);
            expect(req.request.method).toBe('PUT');
            req.flush(mockRequest);
        });
    });

    describe('cancelRequest', () => {
        it('should cancel request successfully', () => {
            //arrange
            const requestId = 'request123';
            const mockRequest: ItemRequest = {
                id: requestId,
                itemId: 'item123',
                requesterId: 'user123',
                ownerId: 'owner123',
                status: RequestStatus.Cancelled,
                requestedAt: new Date(),
                respondedAt: new Date()
            };

            //act
            service.cancelRequest(requestId).subscribe(request => {
                //assert
                expect(request.status).toBe(RequestStatus.Cancelled);
            });

            const req = httpMock.expectOne(`${apiUrl}/${requestId}/cancel`);
            expect(req.request.method).toBe('PUT');
            req.flush(mockRequest);
        });
    });

    describe('completeRequest', () => {
        it('should complete request successfully', () => {
            //arrange
            const requestId = 'request123';
            const mockRequest: ItemRequest = {
                id: requestId,
                itemId: 'item123',
                requesterId: 'user123',
                ownerId: 'owner123',
                status: RequestStatus.Completed,
                requestedAt: new Date(),
                respondedAt: new Date(),
                completedAt: new Date()
            };

            //act
            service.completeRequest(requestId).subscribe(request => {
                //assert
                expect(request.status).toBe(RequestStatus.Completed);
                expect(request.completedAt).toBeDefined();
            });

            const req = httpMock.expectOne(`${apiUrl}/${requestId}/complete`);
            expect(req.request.method).toBe('PUT');
            req.flush(mockRequest);
        });
    });

    describe('error handling', () => {
        it('should handle 401 error and redirect to login', () => {
            //arrange
            const itemId = 'item123';

            //act
            service.createRequest(itemId).subscribe({
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

        it('should handle 403 error', () => {
            //arrange
            const requestId = 'request123';

            //act
            service.approveRequest(requestId).subscribe({
                next: () => fail('should have failed'),
                error: (error) => {
                    //assert
                    expect(error.message).toContain('permission');
                }
            });

            const req = httpMock.expectOne(`${apiUrl}/${requestId}/approve`);
            req.flush({}, { status: 403, statusText: 'Forbidden' });
        });
    });
});
