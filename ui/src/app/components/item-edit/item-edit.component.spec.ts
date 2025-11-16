import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router } from '@angular/router';
import { provideHttpClient } from '@angular/common/http';
import { of, throwError } from 'rxjs';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { ItemEditComponent } from './item-edit.component';
import { ItemsService } from '../../services/items.service';
import { LoopService } from '../../services/loop.service';
import { AuthService } from '../../services/auth.service';
import { ItemRequestService } from '../../services/item-request.service';
import { NotificationService } from '../../services/notification.service';
import { SharedItem } from '../../models/shared-item.interface';
import { Loop } from '../../models/loop.interface';

describe('ItemEditComponent', () => {
  let component: ItemEditComponent;
  let fixture: ComponentFixture<ItemEditComponent>;
  let itemsService: jest.Mocked<ItemsService>;
  let loopService: jest.Mocked<LoopService>;
  let router: jest.Mocked<Router>;
  let activatedRoute: any;
  let authService: jest.Mocked<AuthService>;
  let itemRequestService: jest.Mocked<ItemRequestService>;
  let notificationService: jest.Mocked<NotificationService>;

  const mockItem: SharedItem = {
    id: '1',
    name: 'Test Item',
    description: 'Test Description',
    userId: 'user123',
    isAvailable: true,
    imageUrl: 'http://example.com/image.jpg',
    visibleToLoopIds: ['loop1'],
    visibleToAllLoops: false,
    visibleToFutureLoops: false,
    createdAt: new Date(),
    updatedAt: new Date(),
    ownerName: 'John Doe'
  };

  const mockLoops: Loop[] = [
    { 
      id: 'loop1', 
      name: 'Loop 1', 
      description: 'Test Loop',
      creatorId: 'user123', 
      memberIds: ['user123'], 
      createdAt: new Date(), 
      updatedAt: new Date(), 
      memberCount: 2,
      isPublic: false,
      isArchived: false,
      ownershipHistory: []
    }
  ];

  beforeEach(async () => {
    const itemsServiceMock = {
      getItemById: jest.fn(),
      updateItem: jest.fn(),
      uploadItemImage: jest.fn()
    } as unknown as jest.Mocked<ItemsService>;

    const loopServiceMock = {
      getUserLoops: jest.fn()
    } as unknown as jest.Mocked<LoopService>;

    const routerMock = {
      navigate: jest.fn()
    } as unknown as jest.Mocked<Router>;

    const authServiceMock = {
      getCurrentUser: jest.fn().mockReturnValue(of({ 
        id: 'user1', 
        email: 'test@example.com',
        firstName: 'Test',
        lastName: 'User'
      })),
      logout: jest.fn()
    } as unknown as jest.Mocked<AuthService>;

    const itemRequestServiceMock = {
      getPendingRequests: jest.fn().mockReturnValue(of([]))
    } as unknown as jest.Mocked<ItemRequestService>;

    const notificationServiceMock = {
      getUnreadCount: jest.fn().mockReturnValue(of(0)),
      getNotifications: jest.fn().mockReturnValue(of([]))
    } as unknown as jest.Mocked<NotificationService>;

    activatedRoute = {
      snapshot: {
        paramMap: {
          get: jest.fn().mockReturnValue('1')
        }
      }
    };

    await TestBed.configureTestingModule({
      imports: [ItemEditComponent],
      providers: [
        provideHttpClient(),
        { provide: ItemsService, useValue: itemsServiceMock },
        { provide: LoopService, useValue: loopServiceMock },
        { provide: Router, useValue: routerMock },
        { provide: ActivatedRoute, useValue: activatedRoute },
        { provide: AuthService, useValue: authServiceMock },
        { provide: ItemRequestService, useValue: itemRequestServiceMock },
        { provide: NotificationService, useValue: notificationServiceMock }
      ],
      schemas: [NO_ERRORS_SCHEMA]
    }).compileComponents();

    fixture = TestBed.createComponent(ItemEditComponent);
    component = fixture.componentInstance;
    itemsService = TestBed.inject(ItemsService) as jest.Mocked<ItemsService>;
    loopService = TestBed.inject(LoopService) as jest.Mocked<LoopService>;
    router = TestBed.inject(Router) as jest.Mocked<Router>;
    authService = TestBed.inject(AuthService) as jest.Mocked<AuthService>;
    itemRequestService = TestBed.inject(ItemRequestService) as jest.Mocked<ItemRequestService>;
    notificationService = TestBed.inject(NotificationService) as jest.Mocked<NotificationService>;
  });

  it('should create', () => {
    //assert
    expect(component).toBeTruthy();
  });

  it('should load item data on initialization', () => {
    //arrange
    itemsService.getItemById.mockReturnValue(of(mockItem));
    loopService.getUserLoops.mockReturnValue(of(mockLoops));

    //act
    component.ngOnInit();

    //assert
    expect(itemsService.getItemById).toHaveBeenCalledWith('1');
    expect(component.itemName).toBe('Test Item');
    expect(component.itemDescription).toBe('Test Description');
    expect(component.isAvailable).toBe(true);
  });

  it('should pre-populate form fields with item data', () => {
    //arrange
    itemsService.getItemById.mockReturnValue(of(mockItem));
    loopService.getUserLoops.mockReturnValue(of(mockLoops));

    //act
    component.ngOnInit();

    //assert
    expect(component.itemName).toBe(mockItem.name);
    expect(component.itemDescription).toBe(mockItem.description);
    expect(component.isAvailable).toBe(mockItem.isAvailable);
    expect(component.selectedLoopIds).toEqual(mockItem.visibleToLoopIds);
    expect(component.visibleToAllLoops).toBe(mockItem.visibleToAllLoops);
    expect(component.visibleToFutureLoops).toBe(mockItem.visibleToFutureLoops);
  });

  it('should validate required name field', () => {
    //arrange
    component.itemName = '';
    component.itemId = '1';

    //act
    component.updateItem();

    //assert
    expect(component.error).toBe('Item name is required');
    expect(itemsService.updateItem).not.toHaveBeenCalled();
  });

  it('should call ItemsService.updateItem with correct parameters', () => {
    //arrange
    component.itemId = '1';
    component.itemName = 'Updated Name';
    component.itemDescription = 'Updated Description';
    component.isAvailable = false;
    component.selectedLoopIds = ['loop1', 'loop2'];
    component.visibleToAllLoops = true;
    component.visibleToFutureLoops = true;

    itemsService.updateItem.mockReturnValue(of(mockItem));

    //act
    component.updateItem();

    //assert
    expect(itemsService.updateItem).toHaveBeenCalledWith('1', {
      name: 'Updated Name',
      description: 'Updated Description',
      isAvailable: false,
      visibleToLoopIds: ['loop1', 'loop2'],
      visibleToAllLoops: true,
      visibleToFutureLoops: true
    });
  });

  it('should navigate to main page on successful update', (done) => {
    //arrange
    component.itemId = '1';
    component.itemName = 'Updated Name';
    itemsService.updateItem.mockReturnValue(of(mockItem));

    //act
    component.updateItem();

    //assert
    setTimeout(() => {
      expect(router.navigate).toHaveBeenCalledWith(['/items']);
      done();
    }, 1600);
  });

  it('should display error message on update failure', () => {
    //arrange
    const consoleErrorSpy = jest.spyOn(console, 'error').mockImplementation();
    component.itemId = '1';
    component.itemName = 'Updated Name';
    itemsService.updateItem.mockReturnValue(throwError(() => new Error('Update failed')));

    //act
    component.updateItem();

    //assert
    expect(component.error).toBe('Failed to update item. Please try again.');
    expect(component.loading).toBe(false);
    consoleErrorSpy.mockRestore();
  });

  it('should navigate to main page on cancel', () => {
    //arrange
    //act
    component.onCancel();

    //assert
    expect(router.navigate).toHaveBeenCalledWith(['/items']);
  });

  it('should handle 403 forbidden error appropriately', () => {
    //arrange
    const consoleErrorSpy = jest.spyOn(console, 'error').mockImplementation();
    component.itemId = '1';
    component.itemName = 'Updated Name';
    const error = new Error('403 Forbidden');
    itemsService.updateItem.mockReturnValue(throwError(() => error));

    //act
    component.updateItem();

    //assert
    expect(component.error).toBe('You do not have permission to update this item');
    consoleErrorSpy.mockRestore();
  });

  it('should handle 404 not found error appropriately', () => {
    //arrange
    const consoleErrorSpy = jest.spyOn(console, 'error').mockImplementation();
    component.itemId = '1';
    component.itemName = 'Updated Name';
    const error = new Error('404 Not Found');
    itemsService.updateItem.mockReturnValue(throwError(() => error));

    //act
    component.updateItem();

    //assert
    expect(component.error).toBe('Item not found');
    consoleErrorSpy.mockRestore();
  });

  it('should handle file selection for image upload', () => {
    //arrange
    const mockFile = new File(['image content'], 'test.jpg', { type: 'image/jpeg' });
    const event = {
      target: {
        files: [mockFile]
      }
    } as any;

    //act
    component.onFileSelected(event);

    //assert
    expect(component.selectedImageFile).toBe(mockFile);
  });

  it('should update visibility settings when selector changes', () => {
    //arrange
    const selection = {
      selectedLoopIds: ['loop1', 'loop2'],
      visibleToAllLoops: true,
      visibleToFutureLoops: true
    };

    //act
    component.onVisibilitySelectionChange(selection);

    //assert
    expect(component.selectedLoopIds).toEqual(['loop1', 'loop2']);
    expect(component.visibleToAllLoops).toBe(true);
    expect(component.visibleToFutureLoops).toBe(true);
  });
});
