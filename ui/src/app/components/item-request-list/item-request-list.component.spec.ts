import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { ItemRequestListComponent } from './item-request-list.component';
import { ItemRequestService } from '../../services/item-request.service';
import { AuthService } from '../../services/auth.service';
import { Router, ActivatedRoute } from '@angular/router';
import { of, throwError } from 'rxjs';
import { ItemRequest, RequestStatus } from '../../models/item-request.interface';
import { NO_ERRORS_SCHEMA } from '@angular/core';

describe('ItemRequestListComponent', () => {
    let component: ItemRequestListComponent;
    let fixture: ComponentFixture<ItemRequestListComponent>;
    let itemRequestService: any;
    let mockAuthService: any;
    let mockRouter: any;
    let mockActivatedRoute: any;

    beforeEach(async () => {
        const itemRequestServiceSpy = {
            getPendingRequests: jest.fn().mockReturnValue(of([])),
            approveRequest: jest.fn(),
            rejectRequest: jest.fn(),
            completeRequest: jest.fn()
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
            imports: [ItemRequestListComponent, HttpClientTestingModule],
            providers: [
                { provide: ItemRequestService, useValue: itemRequestServiceSpy },
                { provide: AuthService, useValue: mockAuthService },
                { provide: Router, useValue: mockRouter },
                { provide: ActivatedRoute, useValue: mockActivatedRoute }
            ],
            schemas: [NO_ERRORS_SCHEMA]
        }).compileComponents();

        itemRequestService = TestBed.inject(ItemRequestService);

        fixture = TestBed.createComponent(ItemRequestListComponent);
        component = fixture.componentInstance;
    });

    it('should create', () => {
        //assert
        expect(component).toBeTruthy();
    });

    it('should load pending and approved requests on init', () => {
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
                requesterId: 'user2',
                ownerId: 'owner1',
                status: RequestStatus.Approved,
                requestedAt: new Date()
            }
        ];

        (itemRequestService.getPendingRequests as jest.Mock).mockReturnValue(of(mockRequests));

        //act
        component.ngOnInit();

        //assert
        expect(itemRequestService.getPendingRequests).toHaveBeenCalled();
        expect(component.pendingRequests.length).toBe(1);
        expect(component.approvedRequests.length).toBe(1);
        expect(component.pendingRequests[0].status).toBe(RequestStatus.Pending);
        expect(component.approvedRequests[0].status).toBe(RequestStatus.Approved);
    });

    it('should handle error when loading requests', () => {
        //arrange
        const errorMessage = 'Failed to load requests';
        (itemRequestService.getPendingRequests as jest.Mock).mockReturnValue(
            throwError(() => new Error(errorMessage))
        );

        //act
        component.ngOnInit();

        //assert
        expect(component.errorMessage).toBe(errorMessage);
        expect(component.isLoading).toBe(false);
    });

    it('should approve request successfully', () => {
        //arrange
        const mockRequest: ItemRequest = {
            id: 'req1',
            itemId: 'item1',
            requesterId: 'user1',
            ownerId: 'owner1',
            status: RequestStatus.Pending,
            requestedAt: new Date()
        };

        const approvedRequest: ItemRequest = {
            ...mockRequest,
            status: RequestStatus.Approved,
            respondedAt: new Date()
        };

        (itemRequestService.getPendingRequests as jest.Mock).mockReturnValue(of([mockRequest]));
        (itemRequestService.approveRequest as jest.Mock).mockReturnValue(of(approvedRequest));

        component.ngOnInit();

        //act
        component.onApprove(mockRequest);

        //assert
        expect(itemRequestService.approveRequest).toHaveBeenCalledWith('req1');
        expect(component.successMessage).toBe('Request approved successfully');
    });

    it('should handle error when approving request', () => {
        //arrange
        const mockRequest: ItemRequest = {
            id: 'req1',
            itemId: 'item1',
            requesterId: 'user1',
            ownerId: 'owner1',
            status: RequestStatus.Pending,
            requestedAt: new Date()
        };

        const errorMessage = 'Failed to approve request';
        (itemRequestService.getPendingRequests as jest.Mock).mockReturnValue(of([mockRequest]));
        (itemRequestService.approveRequest as jest.Mock).mockReturnValue(
            throwError(() => new Error(errorMessage))
        );

        component.ngOnInit();

        //act
        component.onApprove(mockRequest);

        //assert
        expect(component.errorMessage).toBe(errorMessage);
    });

    it('should reject request successfully', () => {
        //arrange
        const mockRequest: ItemRequest = {
            id: 'req1',
            itemId: 'item1',
            requesterId: 'user1',
            ownerId: 'owner1',
            status: RequestStatus.Pending,
            requestedAt: new Date()
        };

        const rejectedRequest: ItemRequest = {
            ...mockRequest,
            status: RequestStatus.Rejected,
            respondedAt: new Date()
        };

        (itemRequestService.getPendingRequests as jest.Mock).mockReturnValue(of([mockRequest]));
        (itemRequestService.rejectRequest as jest.Mock).mockReturnValue(of(rejectedRequest));

        component.ngOnInit();

        //act
        component.onReject(mockRequest);

        //assert
        expect(itemRequestService.rejectRequest).toHaveBeenCalledWith('req1');
        expect(component.successMessage).toBe('Request rejected successfully');
    });

    it('should complete request successfully', () => {
        //arrange
        const mockRequest: ItemRequest = {
            id: 'req1',
            itemId: 'item1',
            requesterId: 'user1',
            ownerId: 'owner1',
            status: RequestStatus.Approved,
            requestedAt: new Date()
        };

        const completedRequest: ItemRequest = {
            ...mockRequest,
            status: RequestStatus.Completed,
            completedAt: new Date()
        };

        (itemRequestService.getPendingRequests as jest.Mock).mockReturnValue(of([mockRequest]));
        (itemRequestService.completeRequest as jest.Mock).mockReturnValue(of(completedRequest));

        component.ngOnInit();

        //act
        component.onComplete(mockRequest);

        //assert
        expect(itemRequestService.completeRequest).toHaveBeenCalledWith('req1');
        expect(component.successMessage).toBe('Request completed successfully');
    });

    it('should show empty state when no requests', () => {
        //arrange
        (itemRequestService.getPendingRequests as jest.Mock).mockReturnValue(of([]));

        //act
        component.ngOnInit();

        //assert
        expect(component.hasAnyRequests).toBe(false);
        expect(component.hasPendingRequests).toBe(false);
        expect(component.hasApprovedRequests).toBe(false);
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

        (itemRequestService.getPendingRequests as jest.Mock).mockReturnValue(of([mockRequest]));
        component.ngOnInit();

        //act
        component.onApprove(mockRequest);

        //assert
        expect(itemRequestService.approveRequest).not.toHaveBeenCalled();
    });
});
