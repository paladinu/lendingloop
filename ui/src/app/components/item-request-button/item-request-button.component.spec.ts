import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ItemRequestButtonComponent } from './item-request-button.component';
import { ItemRequestService } from '../../services/item-request.service';
import { AuthService } from '../../services/auth.service';
import { of, throwError } from 'rxjs';
import { ItemRequest, RequestStatus } from '../../models/item-request.interface';
import { NO_ERRORS_SCHEMA } from '@angular/core';

describe('ItemRequestButtonComponent', () => {
    let component: ItemRequestButtonComponent;
    let fixture: ComponentFixture<ItemRequestButtonComponent>;
    let itemRequestService: any;
    let authService: any;

    beforeEach(async () => {
        const itemRequestServiceSpy = {
            getRequestsForItem: jest.fn().mockReturnValue(of([])),
            createRequest: jest.fn()
        };
        const authServiceSpy = {
            getCurrentUser: jest.fn().mockReturnValue(of({ id: 'user123', email: 'test@test.com', firstName: 'Test', lastName: 'User' }))
        };

        await TestBed.configureTestingModule({
            imports: [ItemRequestButtonComponent],
            providers: [
                { provide: ItemRequestService, useValue: itemRequestServiceSpy },
                { provide: AuthService, useValue: authServiceSpy }
            ],
            schemas: [NO_ERRORS_SCHEMA]
        }).compileComponents();

        itemRequestService = TestBed.inject(ItemRequestService);
        authService = TestBed.inject(AuthService);

        fixture = TestBed.createComponent(ItemRequestButtonComponent);
        component = fixture.componentInstance;
    });

    it('should create', () => {
        //assert
        expect(component).toBeTruthy();
    });

    it('should load existing request on init', () => {
        //arrange
        const mockRequests: ItemRequest[] = [
            {
                id: 'req1',
                itemId: 'item123',
                requesterId: 'user123',
                ownerId: 'owner123',
                status: RequestStatus.Pending,
                requestedAt: new Date()
            }
        ];

        (authService.getCurrentUser as jest.Mock).mockReturnValue(of({ id: 'user123', email: 'test@test.com', firstName: 'Test', lastName: 'User' }));
        (itemRequestService.getRequestsForItem as jest.Mock).mockReturnValue(of(mockRequests));
        component.itemId = 'item123';
        component.ownerId = 'owner123';

        //act
        component.ngOnInit();

        //assert
        expect(itemRequestService.getRequestsForItem).toHaveBeenCalledWith('item123');
        expect(component.existingRequest).toEqual(mockRequests[0]);
    });

    it('should show "Request Item" when no existing request', () => {
        //arrange
        (authService.getCurrentUser as jest.Mock).mockReturnValue(of({ id: 'user123', email: 'test@test.com', firstName: 'Test', lastName: 'User' }));
        (itemRequestService.getRequestsForItem as jest.Mock).mockReturnValue(of([]));
        component.itemId = 'item123';
        component.ownerId = 'owner123';

        //act
        component.ngOnInit();

        //assert
        expect(component.buttonText).toBe('Request Item');
        expect(component.isButtonDisabled).toBe(false);
    });

    it('should show "Pending Request" when pending request exists', () => {
        //arrange
        const mockRequest: ItemRequest = {
            id: 'req1',
            itemId: 'item123',
            requesterId: 'user123',
            ownerId: 'owner123',
            status: RequestStatus.Pending,
            requestedAt: new Date()
        };

        (authService.getCurrentUser as jest.Mock).mockReturnValue(of({ id: 'user123', email: 'test@test.com', firstName: 'Test', lastName: 'User' }));
        (itemRequestService.getRequestsForItem as jest.Mock).mockReturnValue(of([mockRequest]));
        component.itemId = 'item123';
        component.ownerId = 'owner123';

        //act
        component.ngOnInit();

        //assert
        expect(component.buttonText).toBe('Pending Request');
        expect(component.isButtonDisabled).toBe(true);
    });

    it('should show "Currently Borrowed" when approved request exists', () => {
        //arrange
        const mockRequest: ItemRequest = {
            id: 'req1',
            itemId: 'item123',
            requesterId: 'user123',
            ownerId: 'owner123',
            status: RequestStatus.Approved,
            requestedAt: new Date()
        };

        (authService.getCurrentUser as jest.Mock).mockReturnValue(of({ id: 'user123', email: 'test@test.com', firstName: 'Test', lastName: 'User' }));
        (itemRequestService.getRequestsForItem as jest.Mock).mockReturnValue(of([mockRequest]));
        component.itemId = 'item123';
        component.ownerId = 'owner123';

        //act
        component.ngOnInit();

        //assert
        expect(component.buttonText).toBe('Currently Borrowed');
        expect(component.isButtonDisabled).toBe(true);
    });

    it('should create request when button clicked', () => {
        //arrange
        const mockRequest: ItemRequest = {
            id: 'req1',
            itemId: 'item123',
            requesterId: 'user123',
            ownerId: 'owner123',
            status: RequestStatus.Pending,
            requestedAt: new Date()
        };

        (authService.getCurrentUser as jest.Mock).mockReturnValue(of({ id: 'user123', email: 'test@test.com', firstName: 'Test', lastName: 'User' }));
        (itemRequestService.getRequestsForItem as jest.Mock).mockReturnValue(of([]));
        (itemRequestService.createRequest as jest.Mock).mockReturnValue(of(mockRequest));
        component.itemId = 'item123';
        component.ownerId = 'owner123';

        jest.spyOn(component.requestCreated, 'emit');

        component.ngOnInit();

        //act
        component.onRequestItem();

        //assert
        expect(itemRequestService.createRequest).toHaveBeenCalledWith('item123');
        expect(component.existingRequest).toEqual(mockRequest);
        expect(component.requestCreated.emit).toHaveBeenCalledWith(mockRequest);
    });

    it('should handle error when creating request', () => {
        //arrange
        const errorMessage = 'Cannot request your own item';

        (authService.getCurrentUser as jest.Mock).mockReturnValue(of({ id: 'user123', email: 'test@test.com', firstName: 'Test', lastName: 'User' }));
        (itemRequestService.getRequestsForItem as jest.Mock).mockReturnValue(of([]));
        (itemRequestService.createRequest as jest.Mock).mockReturnValue(
            throwError(() => new Error(errorMessage))
        );
        component.itemId = 'item123';
        component.ownerId = 'owner123';

        component.ngOnInit();

        //act
        component.onRequestItem();

        //assert
        expect(component.errorMessage).toBe(errorMessage);
        expect(component.isLoading).toBe(false);
    });

    it('should not show button when user is owner', () => {
        //arrange
        (authService.getCurrentUser as jest.Mock).mockReturnValue(of({ id: 'owner123', email: 'test@test.com', firstName: 'Test', lastName: 'User' }));
        (itemRequestService.getRequestsForItem as jest.Mock).mockReturnValue(of([]));
        component.itemId = 'item123';
        component.ownerId = 'owner123';

        //act
        component.ngOnInit();

        //assert
        expect(component.showButton).toBe(false);
        expect(component.isButtonDisabled).toBe(true);
    });

    it('should disable button when loading', () => {
        //arrange
        (authService.getCurrentUser as jest.Mock).mockReturnValue(of({ id: 'user123', email: 'test@test.com', firstName: 'Test', lastName: 'User' }));
        (itemRequestService.getRequestsForItem as jest.Mock).mockReturnValue(of([]));
        component.itemId = 'item123';
        component.ownerId = 'owner123';
        component.isLoading = true;

        //act
        component.ngOnInit();

        //assert
        expect(component.isButtonDisabled).toBe(true);
    });
});
