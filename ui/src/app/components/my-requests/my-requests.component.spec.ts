import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MyRequestsComponent } from './my-requests.component';
import { ItemRequestService } from '../../services/item-request.service';
import { AuthService } from '../../services/auth.service';
import { Router, ActivatedRoute } from '@angular/router';
import { of, throwError } from 'rxjs';
import { ItemRequest, RequestStatus } from '../../models/item-request.interface';
import { NO_ERRORS_SCHEMA } from '@angular/core';

describe('MyRequestsComponent', () => {
    let component: MyRequestsComponent;
    let fixture: ComponentFixture<MyRequestsComponent>;
    let itemRequestService: any;
    let mockAuthService: any;
    let mockRouter: any;
    let mockActivatedRoute: any;

    beforeEach(async () => {
        const itemRequestServiceSpy = {
            getMyRequests: jest.fn().mockReturnValue(of([])),
            cancelRequest: jest.fn(),
            getPendingRequests: jest.fn().mockReturnValue(of([]))
        };

        mockAuthService = {
            getCurrentUser: jest.fn().mockReturnValue(of(null)),
            logout: jest.fn()
        };

        mockRouter = {
            navigate: jest.fn()
        };

        mockActivatedRoute = {
            snapshot: { params: {} }
        };

        await TestBed.configureTestingModule({
            imports: [MyRequestsComponent],
            providers: [
                { provide: ItemRequestService, useValue: itemRequestServiceSpy },
                { provide: AuthService, useValue: mockAuthService },
                { provide: Router, useValue: mockRouter },
                { provide: ActivatedRoute, useValue: mockActivatedRoute }
            ],
            schemas: [NO_ERRORS_SCHEMA]
        }).compileComponents();

        itemRequestService = TestBed.inject(ItemRequestService);

        fixture = TestBed.createComponent(MyRequestsComponent);
        component = fixture.componentInstance;
    });

    it('should create', () => {
        //assert
        expect(component).toBeTruthy();
    });

    it('should load all requests on init', () => {
        //arrange
        const mockRequests: ItemRequest[] = [
            {
                id: 'req1',
                itemId: 'item1',
                requesterId: 'user1',
                ownerId: 'owner1',
                status: RequestStatus.Pending,
                requestedAt: new Date()
            },
            {
                id: 'req2',
                itemId: 'item2',
                requesterId: 'user1',
                ownerId: 'owner2',
                status: RequestStatus.Approved,
                requestedAt: new Date()
            },
            {
                id: 'req3',
                itemId: 'item3',
                requesterId: 'user1',
                ownerId: 'owner3',
                status: RequestStatus.Completed,
                requestedAt: new Date()
            }
        ];

        (itemRequestService.getMyRequests as jest.Mock).mockReturnValue(of(mockRequests));

        //act
        component.ngOnInit();

        //assert
        expect(itemRequestService.getMyRequests).toHaveBeenCalled();
        expect(component.allRequests.length).toBe(3);
        expect(component.pendingRequests.length).toBe(1);
        expect(component.approvedRequests.length).toBe(1);
        expect(component.completedRequests.length).toBe(1);
    });

    it('should handle error when loading requests', () => {
        //arrange
        const errorMessage = 'Failed to load requests';
        (itemRequestService.getMyRequests as jest.Mock).mockReturnValue(
            throwError(() => new Error(errorMessage))
        );

        //act
        component.ngOnInit();

        //assert
        expect(component.errorMessage).toBe(errorMessage);
        expect(component.isLoading).toBe(false);
    });

    it('should filter requests by status correctly', () => {
        //arrange
        const mockRequests: ItemRequest[] = [
            {
                id: 'req1',
                itemId: 'item1',
                requesterId: 'user1',
                ownerId: 'owner1',
                status: RequestStatus.Pending,
                requestedAt: new Date()
            },
            {
                id: 'req2',
                itemId: 'item2',
                requesterId: 'user1',
                ownerId: 'owner2',
                status: RequestStatus.Approved,
                requestedAt: new Date()
            },
            {
                id: 'req3',
                itemId: 'item3',
                requesterId: 'user1',
                ownerId: 'owner3',
                status: RequestStatus.Rejected,
                requestedAt: new Date()
            },
            {
                id: 'req4',
                itemId: 'item4',
                requesterId: 'user1',
                ownerId: 'owner4',
                status: RequestStatus.Cancelled,
                requestedAt: new Date()
            },
            {
                id: 'req5',
                itemId: 'item5',
                requesterId: 'user1',
                ownerId: 'owner5',
                status: RequestStatus.Completed,
                requestedAt: new Date()
            }
        ];

        (itemRequestService.getMyRequests as jest.Mock).mockReturnValue(of(mockRequests));

        //act
        component.ngOnInit();

        //assert
        expect(component.pendingRequests.length).toBe(1);
        expect(component.approvedRequests.length).toBe(1);
        expect(component.rejectedRequests.length).toBe(1);
        expect(component.cancelledRequests.length).toBe(1);
        expect(component.completedRequests.length).toBe(1);
    });

    it('should cancel request successfully', () => {
        //arrange
        const mockRequest: ItemRequest = {
            id: 'req1',
            itemId: 'item1',
            requesterId: 'user1',
            ownerId: 'owner1',
            status: RequestStatus.Pending,
            requestedAt: new Date()
        };

        const cancelledRequest: ItemRequest = {
            ...mockRequest,
            status: RequestStatus.Cancelled,
            respondedAt: new Date()
        };

        (itemRequestService.getMyRequests as jest.Mock).mockReturnValue(of([mockRequest]));
        (itemRequestService.cancelRequest as jest.Mock).mockReturnValue(of(cancelledRequest));

        component.ngOnInit();

        //act
        component.onCancel(mockRequest);

        //assert
        expect(itemRequestService.cancelRequest).toHaveBeenCalledWith('req1');
        expect(component.successMessage).toBe('Request cancelled successfully');
    });

    it('should handle error when cancelling request', () => {
        //arrange
        const mockRequest: ItemRequest = {
            id: 'req1',
            itemId: 'item1',
            requesterId: 'user1',
            ownerId: 'owner1',
            status: RequestStatus.Pending,
            requestedAt: new Date()
        };

        const errorMessage = 'Failed to cancel request';
        (itemRequestService.getMyRequests as jest.Mock).mockReturnValue(of([mockRequest]));
        (itemRequestService.cancelRequest as jest.Mock).mockReturnValue(
            throwError(() => new Error(errorMessage))
        );

        component.ngOnInit();

        //act
        component.onCancel(mockRequest);

        //assert
        expect(component.errorMessage).toBe(errorMessage);
    });

    it('should show empty state when no requests', () => {
        //arrange
        (itemRequestService.getMyRequests as jest.Mock).mockReturnValue(of([]));

        //act
        component.ngOnInit();

        //assert
        expect(component.hasRequests).toBe(false);
    });

    it('should not call service when request has no id', () => {
        //arrange
        const mockRequest: ItemRequest = {
            itemId: 'item1',
            requesterId: 'user1',
            ownerId: 'owner1',
            status: RequestStatus.Pending,
            requestedAt: new Date()
        };

        (itemRequestService.getMyRequests as jest.Mock).mockReturnValue(of([mockRequest]));
        component.ngOnInit();

        //act
        component.onCancel(mockRequest);

        //assert
        expect(itemRequestService.cancelRequest).not.toHaveBeenCalled();
    });

    it('should return correct badge class for each status', () => {
        //arrange & act & assert
        expect(component.getStatusBadgeClass(RequestStatus.Pending)).toBe('bg-warning');
        expect(component.getStatusBadgeClass(RequestStatus.Approved)).toBe('bg-success');
        expect(component.getStatusBadgeClass(RequestStatus.Rejected)).toBe('bg-danger');
        expect(component.getStatusBadgeClass(RequestStatus.Cancelled)).toBe('bg-secondary');
        expect(component.getStatusBadgeClass(RequestStatus.Completed)).toBe('bg-info');
    });
});
