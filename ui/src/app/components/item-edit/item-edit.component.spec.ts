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
    jest.useFakeTimers();
    
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

  afterEach(() => {
    jest.useRealTimers();
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
      visibleToFutureLoops: true,
      tags: []
    });
  });

  it('should navigate to main page on successful update', () => {
    //arrange
    component.itemId = '1';
    component.itemName = 'Updated Name';
    itemsService.updateItem.mockReturnValue(of(mockItem));

    //act
    component.updateItem();
    jest.advanceTimersByTime(1550);

    //assert
    expect(router.navigate).toHaveBeenCalledWith(['/items']);
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

  describe('Tag Selection Integration', () => {
    it('should load existing tags when editing item', () => {
      //arrange
      const itemWithTags: SharedItem = {
        ...mockItem,
        tags: ['power-tools', 'hand-tools', 'garden-tools']
      };
      itemsService.getItemById.mockReturnValue(of(itemWithTags));
      loopService.getUserLoops.mockReturnValue(of(mockLoops));

      //act
      component.ngOnInit();

      //assert
      expect(component.selectedTags).toEqual(['power-tools', 'hand-tools', 'garden-tools']);
    });

    it('should load empty tags array when item has no tags', () => {
      //arrange
      const itemWithoutTags: SharedItem = {
        ...mockItem,
        tags: []
      };
      itemsService.getItemById.mockReturnValue(of(itemWithoutTags));
      loopService.getUserLoops.mockReturnValue(of(mockLoops));

      //act
      component.ngOnInit();

      //assert
      expect(component.selectedTags).toEqual([]);
    });

    it('should handle item with undefined tags field', () => {
      //arrange
      const itemWithUndefinedTags: SharedItem = {
        ...mockItem
      };
      delete (itemWithUndefinedTags as any).tags;
      itemsService.getItemById.mockReturnValue(of(itemWithUndefinedTags));
      loopService.getUserLoops.mockReturnValue(of(mockLoops));

      //act
      component.ngOnInit();

      //assert
      expect(component.selectedTags).toEqual([]);
    });

    it('should update selectedTags when onTagsChange is called', () => {
      //arrange
      const tags = ['cameras', 'audio-equipment', 'projectors'];

      //act
      component.onTagsChange(tags);

      //assert
      expect(component.selectedTags).toEqual(tags);
    });

    it('should include tags in item update API call', () => {
      //arrange
      component.itemId = '1';
      component.itemName = 'Updated Item';
      component.itemDescription = 'Updated Description';
      component.selectedTags = ['power-tools', 'hand-tools'];
      itemsService.updateItem.mockReturnValue(of(mockItem));

      //act
      component.updateItem();

      //assert
      expect(itemsService.updateItem).toHaveBeenCalledWith('1', 
        expect.objectContaining({
          name: 'Updated Item',
          description: 'Updated Description',
          tags: ['power-tools', 'hand-tools']
        })
      );
    });

    it('should update item with empty tags array when no tags selected', () => {
      //arrange
      component.itemId = '1';
      component.itemName = 'Updated Item';
      component.selectedTags = [];
      itemsService.updateItem.mockReturnValue(of(mockItem));

      //act
      component.updateItem();

      //assert
      expect(itemsService.updateItem).toHaveBeenCalledWith('1',
        expect.objectContaining({
          tags: []
        })
      );
    });

    it('should handle tag updates with maximum 10 tags', () => {
      //arrange
      const maxTags = [
        'power-tools', 'hand-tools', 'garden-tools', 'automotive-tools', 'measuring-tools',
        'lawn-care', 'cleaning-equipment', 'ladders', 'pressure-washers', 'painting-supplies'
      ];
      component.itemId = '1';
      component.itemName = 'Updated Item';
      component.selectedTags = maxTags;
      itemsService.updateItem.mockReturnValue(of(mockItem));

      //act
      component.updateItem();

      //assert
      expect(itemsService.updateItem).toHaveBeenCalledWith('1',
        expect.objectContaining({
          tags: maxTags
        })
      );
      expect(component.selectedTags.length).toBe(10);
    });

    it('should preserve tags when item update fails', () => {
      //arrange
      const consoleErrorSpy = jest.spyOn(console, 'error').mockImplementation();
      const selectedTags = ['cameras', 'audio-equipment'];
      component.itemId = '1';
      component.itemName = 'Updated Item';
      component.selectedTags = selectedTags;
      itemsService.updateItem.mockReturnValue(
        throwError(() => new Error('API Error'))
      );

      //act
      component.updateItem();

      //assert
      expect(component.selectedTags).toEqual(selectedTags);
      expect(component.error).toBeTruthy();
      consoleErrorSpy.mockRestore();
    });

    it('should update tags independently of other fields', () => {
      //arrange
      component.itemId = '1';
      component.itemName = 'Original Name';
      component.itemDescription = 'Original Description';
      component.isAvailable = true;
      component.selectedTags = ['old-tag'];
      
      // Only change tags
      component.selectedTags = ['new-tag-1', 'new-tag-2'];
      itemsService.updateItem.mockReturnValue(of(mockItem));

      //act
      component.updateItem();

      //assert
      expect(itemsService.updateItem).toHaveBeenCalledWith('1',
        expect.objectContaining({
          name: 'Original Name',
          description: 'Original Description',
          isAvailable: true,
          tags: ['new-tag-1', 'new-tag-2']
        })
      );
    });

    it('should include tags in update call along with image upload', () => {
      //arrange
      component.itemId = '1';
      component.itemName = 'Updated Item';
      component.selectedTags = ['power-tools'];
      component.selectedImageFile = new File(['image'], 'test.jpg', { type: 'image/jpeg' });
      
      itemsService.updateItem.mockReturnValue(of(mockItem));
      itemsService.uploadItemImage.mockReturnValue(of(void 0));

      //act
      component.updateItem();

      //assert
      expect(itemsService.updateItem).toHaveBeenCalledWith('1',
        expect.objectContaining({
          tags: ['power-tools']
        })
      );
      
      jest.advanceTimersByTime(100);
      expect(itemsService.uploadItemImage).toHaveBeenCalledWith('1', component.selectedImageFile);
    });

    it('should maintain tag selection when switching between different tag sets', () => {
      //arrange
      const firstTags = ['power-tools', 'hand-tools'];
      const secondTags = ['cameras', 'audio-equipment', 'projectors'];

      //act
      component.onTagsChange(firstTags);
      expect(component.selectedTags).toEqual(firstTags);

      component.onTagsChange(secondTags);
      expect(component.selectedTags).toEqual(secondTags);

      //assert
      expect(component.selectedTags).toEqual(secondTags);
      expect(component.selectedTags.length).toBe(3);
    });
  });

  describe('Tag Validation', () => {
    it('should initialize with empty selectedTags array by default', () => {
      //arrange & act
      const newComponent = new ItemEditComponent(
        {} as ActivatedRoute,
        {} as Router,
        itemsService,
        loopService
      );

      //assert
      expect(newComponent.selectedTags).toEqual([]);
    });

    it('should allow item update without tags', () => {
      //arrange
      component.itemId = '1';
      component.itemName = 'Updated Item';
      component.selectedTags = [];
      itemsService.updateItem.mockReturnValue(of(mockItem));

      //act
      component.updateItem();

      //assert
      expect(itemsService.updateItem).toHaveBeenCalled();
      expect(component.success).toBeTruthy();
    });

    it('should include tags in the update object passed to updateItem', () => {
      //arrange
      const tags = ['books', 'educational'];
      component.itemId = '1';
      component.itemName = 'Textbook';
      component.itemDescription = 'Educational book';
      component.selectedTags = tags;
      component.selectedLoopIds = ['loop1'];
      component.visibleToAllLoops = false;
      component.visibleToFutureLoops = false;
      itemsService.updateItem.mockReturnValue(of(mockItem));

      //act
      component.updateItem();

      //assert
      const updateCall = itemsService.updateItem.mock.calls[0][1];
      expect(updateCall.tags).toEqual(tags);
      expect(updateCall.name).toBe('Textbook');
      expect(updateCall.description).toBe('Educational book');
      expect(updateCall.visibleToLoopIds).toEqual(['loop1']);
    });

    it('should handle rapid tag changes before update', () => {
      //arrange
      component.itemId = '1';
      component.itemName = 'Test Item';
      
      //act
      component.onTagsChange(['tag1']);
      component.onTagsChange(['tag1', 'tag2']);
      component.onTagsChange(['tag1', 'tag2', 'tag3']);
      
      itemsService.updateItem.mockReturnValue(of(mockItem));
      component.updateItem();

      //assert
      expect(itemsService.updateItem).toHaveBeenCalledWith('1',
        expect.objectContaining({
          tags: ['tag1', 'tag2', 'tag3']
        })
      );
    });
  });
});
