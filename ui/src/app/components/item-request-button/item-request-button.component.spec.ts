import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { ItemRequestButtonComponent } from './item-request-button.component';
import { ItemRequestService } from '../../services/item-request.service';
import { AuthService } from '../../services/auth.service';
import { ItemsService } from '../../services/items.service';
import { MatDialog, MatDialogRef } from '@angular/material/dialog';
import { of, throwError } from 'rxjs';
import { ItemRequest, RequestStatus } from '../../models/item-request.interface';
import { SharedItem } from '../../models/shared-item.interface';
import { NO_ERRORS_SCHEMA } from '@angular/core';

describe('ItemRequestButtonComponent', () => {
    let component: ItemRequestButtonComponent;
    let fixture: ComponentFixture<ItemRequestButtonComponent>;
    let itemRequestService: any;
    let authService: any;
    let itemsService: any;
    let dialog: any;

    beforeEach(async () => {
        const itemRequestServiceSpy = {
            getRequestsForItem: jest.fn().mockReturnValue(of([])),
            createRequest: jest.fn()
        };
        const authServiceSpy = {
            getCurrentUser: jest.fn().mockReturnValue(of({ id: 'user123', email: 'test@test.com', firstName: 'Test', lastName: 'User' }))
        };
        const itemsServiceSpy = {
            getItemById: jest.fn()
        };
        const dialogSpy = {
            open: jest.fn()
        };

        await TestBed.configureTestingModule({
            imports: [ItemRequestButtonComponent, HttpClientTestingModule],
            providers: [
                { provide: ItemRequestService, useValue: itemRequestServiceSpy },
                { provide: AuthService, useValue: authServiceSpy },
                { provide: ItemsService, useValue: itemsServiceSpy },
                { provide: MatDialog, useValue: dialogSpy }
            ],
            schemas: [NO_ERRORS_SCHEMA]
        }).compileComponents();

        itemRequestService = TestBed.inject(ItemRequestService);
        authService = TestBed.inject(AuthService);
        itemsService = TestBed.inject(ItemsService);
        dialog = TestBed.inject(MatDialog);

        fixture = TestBed.createComponent(ItemRequestButtonComponent);
        component = fixture.componentInstance;
        
        // Prevent ngOnInit from running automatically
        fixture.autoDetectChanges(false);
    });

    it('should create', () => {
        //arrange
        component.itemId = 'item123';
        component.ownerId = 'owner123';
        
        //act
        fixture.detectChanges();
        
        //assert
        expect(component).toBeTruthy();
    });

    it('should load existing request on init', (done) => {
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
        setTimeout(() => {
            expect(itemRequestService.getRequestsForItem).toHaveBeenCalledWith('item123');
            expect(component.existingRequest).toEqual(mockRequests[0]);
            done();
        }, 0);
    });

    it('should show "Request Item" when no existing request', (done) => {
        //arrange
        (authService.getCurrentUser as jest.Mock).mockReturnValue(of({ id: 'user123', email: 'test@test.com', firstName: 'Test', lastName: 'User' }));
        (itemRequestService.getRequestsForItem as jest.Mock).mockReturnValue(of([]));
        component.itemId = 'item123';
        component.ownerId = 'owner123';

        //act
        component.ngOnInit();

        //assert
        setTimeout(() => {
            expect(component.buttonText).toBe('Request Item');
            expect(component.isButtonDisabled).toBe(false);
            done();
        }, 0);
    });

    it('should show "Pending Request" when pending request exists', (done) => {
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
        setTimeout(() => {
            expect(component.buttonText).toBe('Pending Request');
            expect(component.isButtonDisabled).toBe(true);
            done();
        }, 0);
    });

    it('should show "Currently Borrowed" when approved request exists', (done) => {
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
        setTimeout(() => {
            expect(component.buttonText).toBe('Currently Borrowed');
            expect(component.isButtonDisabled).toBe(true);
            done();
        }, 0);
    });

    it('should open dialog when button clicked', (done) => {
        //arrange
        const mockItem: SharedItem = {
            id: 'item123',
            name: 'Test Item',
            description: 'Test description',
            ownerId: 'owner123',
            isAvailable: true,
            visibleToLoopIds: [],
            visibleToAllLoops: false,
            visibleToFutureLoops: false
        };

        const mockDialogRef = {
            afterClosed: jest.fn().mockReturnValue(of({ message: 'Test message' }))
        };

        const mockRequest: ItemRequest = {
            id: 'req1',
            itemId: 'item123',
            requesterId: 'user123',
            ownerId: 'owner123',
            status: RequestStatus.Pending,
            message: 'Test message',
            requestedAt: new Date()
        };

        (authService.getCurrentUser as jest.Mock).mockReturnValue(of({ id: 'user123', email: 'test@test.com', firstName: 'Test', lastName: 'User' }));
        (itemRequestService.getRequestsForItem as jest.Mock).mockReturnValue(of([]));
        (itemsService.getItemById as jest.Mock).mockReturnValue(of(mockItem));
        (dialog.open as jest.Mock).mockReturnValue(mockDialogRef);
        (itemRequestService.createRequest as jest.Mock).mockReturnValue(of(mockRequest));
        
        component.itemId = 'item123';
        component.ownerId = 'owner123';

        jest.spyOn(component.requestCreated, 'emit');

        component.ngOnInit();

        //act
        setTimeout(() => {
            component.onRequestItem();
            
            //assert
            setTimeout(() => {
                expect(itemsService.getItemById).toHaveBeenCalledWith('item123');
                expect(dialog.open).toHaveBeenCalled();
                expect(itemRequestService.createRequest).toHaveBeenCalledWith('item123', 'Test message');
                expect(component.existingRequest).toEqual(mockRequest);
                expect(component.requestCreated.emit).toHaveBeenCalledWith(mockRequest);
                done();
            }, 0);
        }, 0);
    });

    it('should handle dialog cancellation', (done) => {
        //arrange
        const mockItem: SharedItem = {
            id: 'item123',
            name: 'Test Item',
            description: 'Test description',
            ownerId: 'owner123',
            isAvailable: true,
            visibleToLoopIds: [],
            visibleToAllLoops: false,
            visibleToFutureLoops: false
        };

        const mockDialogRef = {
            afterClosed: jest.fn().mockReturnValue(of(undefined))
        };

        (authService.getCurrentUser as jest.Mock).mockReturnValue(of({ id: 'user123', email: 'test@test.com', firstName: 'Test', lastName: 'User' }));
        (itemRequestService.getRequestsForItem as jest.Mock).mockReturnValue(of([]));
        (itemsService.getItemById as jest.Mock).mockReturnValue(of(mockItem));
        (dialog.open as jest.Mock).mockReturnValue(mockDialogRef);
        
        component.itemId = 'item123';
        component.ownerId = 'owner123';

        component.ngOnInit();

        //act
        setTimeout(() => {
            component.onRequestItem();
            
            //assert
            setTimeout(() => {
                expect(dialog.open).toHaveBeenCalled();
                expect(itemRequestService.createRequest).not.toHaveBeenCalled();
                done();
            }, 0);
        }, 0);
    });

    it('should handle error when creating request', (done) => {
        //arrange
        const mockItem: SharedItem = {
            id: 'item123',
            name: 'Test Item',
            description: 'Test description',
            ownerId: 'owner123',
            isAvailable: true,
            visibleToLoopIds: [],
            visibleToAllLoops: false,
            visibleToFutureLoops: false
        };

        const mockDialogRef = {
            afterClosed: jest.fn().mockReturnValue(of({ message: 'Test message' }))
        };

        const errorMessage = 'Cannot request your own item';

        (authService.getCurrentUser as jest.Mock).mockReturnValue(of({ id: 'user123', email: 'test@test.com', firstName: 'Test', lastName: 'User' }));
        (itemRequestService.getRequestsForItem as jest.Mock).mockReturnValue(of([]));
        (itemsService.getItemById as jest.Mock).mockReturnValue(of(mockItem));
        (dialog.open as jest.Mock).mockReturnValue(mockDialogRef);
        (itemRequestService.createRequest as jest.Mock).mockReturnValue(
            throwError(() => new Error(errorMessage))
        );
        component.itemId = 'item123';
        component.ownerId = 'owner123';

        component.ngOnInit();

        //act
        setTimeout(() => {
            component.onRequestItem();
            
            //assert
            setTimeout(() => {
                expect(component.errorMessage).toBe(errorMessage);
                expect(component.isLoading).toBe(false);
                done();
            }, 0);
        }, 0);
    });

    it('should not show button when user is owner', (done) => {
        //arrange
        (authService.getCurrentUser as jest.Mock).mockReturnValue(of({ id: 'owner123', email: 'test@test.com', firstName: 'Test', lastName: 'User' }));
        (itemRequestService.getRequestsForItem as jest.Mock).mockReturnValue(of([]));
        component.itemId = 'item123';
        component.ownerId = 'owner123';

        //act
        component.ngOnInit();

        //assert
        setTimeout(() => {
            expect(component.showButton).toBe(false);
            expect(component.isButtonDisabled).toBe(true);
            done();
        }, 0);
    });

    it('should disable button when loading', (done) => {
        //arrange
        (authService.getCurrentUser as jest.Mock).mockReturnValue(of({ id: 'user123', email: 'test@test.com', firstName: 'Test', lastName: 'User' }));
        (itemRequestService.getRequestsForItem as jest.Mock).mockReturnValue(of([]));
        component.itemId = 'item123';
        component.ownerId = 'owner123';
        component.isLoading = true;

        //act
        component.ngOnInit();

        //assert
        setTimeout(() => {
            expect(component.isButtonDisabled).toBe(true);
            done();
        }, 0);
    });
});
