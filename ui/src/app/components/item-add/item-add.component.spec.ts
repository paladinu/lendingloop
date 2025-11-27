import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormsModule } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { provideRouter } from '@angular/router';
import { provideHttpClient } from '@angular/common/http';
import { of, throwError } from 'rxjs';
import { ItemAddComponent } from './item-add.component';
import { ItemsService } from '../../services/items.service';
import { LoopService } from '../../services/loop.service';
import { TagsService } from '../../services/tags.service';
import { NotificationService } from '../../services/notification.service';
import { ItemRequestService } from '../../services/item-request.service';
import { SharedItem } from '../../models/shared-item.interface';
import { ToolbarComponent } from '../toolbar/toolbar.component';
import { getMockToolbarServices } from '../../testing/mock-services';

describe('ItemAddComponent', () => {
  let component: ItemAddComponent;
  let fixture: ComponentFixture<ItemAddComponent>;
  let mockItemsService: jest.Mocked<ItemsService>;
  let mockLoopService: jest.Mocked<LoopService>;
  let mockTagsService: jest.Mocked<TagsService>;
  let mockRouter: jest.Mocked<Router>;

  beforeEach(async () => {
    const toolbarMocks = getMockToolbarServices();

    mockItemsService = {
      createItem: jest.fn(),
      uploadItemImage: jest.fn(),
    } as any;

    mockLoopService = {
      getUserLoops: jest.fn().mockReturnValue(of([])),
    } as any;

    mockTagsService = {
      getAllTags: jest.fn().mockReturnValue(of([])),
      searchTags: jest.fn().mockReturnValue([]),
      getTagStatistics: jest.fn().mockReturnValue(of(new Map())),
      clearCache: jest.fn(),
    } as any;

    mockRouter = {
      navigate: jest.fn(),
    } as any;

    const activatedRouteMock = {
      snapshot: { params: {} },
      params: of({})
    };

    await TestBed.configureTestingModule({
      imports: [ItemAddComponent, FormsModule, ToolbarComponent],
      providers: [
        provideHttpClient(),
        provideRouter([
          { path: 'loops/create', component: ItemAddComponent },
          { path: 'loops/invitations', component: ItemAddComponent }
        ]),
        { provide: ItemsService, useValue: mockItemsService },
        { provide: LoopService, useValue: mockLoopService },
        { provide: TagsService, useValue: mockTagsService },
        { provide: NotificationService, useValue: toolbarMocks.mockNotificationService },
        { provide: ItemRequestService, useValue: toolbarMocks.mockItemRequestService },
        { provide: ActivatedRoute, useValue: activatedRouteMock }
      ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ItemAddComponent);
    component = fixture.componentInstance;
  });

  it('should create', () => {
    //assert
    expect(component).toBeTruthy();
  });

  it('should create item successfully', (done) => {
    //arrange
    const mockItem: SharedItem = {
      id: 'item1',
      name: 'Test Item',
      description: 'Test Description',
      userId: 'user1',
      isAvailable: true,
      visibleToLoopIds: [],
      visibleToAllLoops: true,
      visibleToFutureLoops: true,
      createdAt: new Date(),
      updatedAt: new Date()
    };
    mockItemsService.createItem.mockReturnValue(of(mockItem));
    mockLoopService.getUserLoops.mockReturnValue(of([]));
    component.newItemName = 'Test Item';
    component.newItemDescription = 'Test Description';

    fixture.detectChanges();

    //act
    component.addItem();

    //assert
    setTimeout(() => {
      expect(mockItemsService.createItem).toHaveBeenCalled();
      expect(component.success).toBeTruthy();
      done();
    }, 100);
  });

  describe('Tag Selection Integration', () => {
    it('should update selectedTags when onTagsChange is called', () => {
      //arrange
      const tags = ['power-tools', 'hand-tools', 'garden-tools'];

      //act
      component.onTagsChange(tags);

      //assert
      expect(component.selectedTags).toEqual(tags);
    });

    it('should include tags in item creation API call', (done) => {
      //arrange
      const mockItem: SharedItem = {
        id: 'item1',
        name: 'Test Item',
        description: 'Test Description',
        userId: 'user1',
        isAvailable: true,
        visibleToLoopIds: [],
        visibleToAllLoops: true,
        visibleToFutureLoops: true,
        tags: ['power-tools', 'hand-tools'],
        createdAt: new Date(),
        updatedAt: new Date()
      };
      mockItemsService.createItem.mockReturnValue(of(mockItem));
      mockLoopService.getUserLoops.mockReturnValue(of([]));
      
      component.newItemName = 'Test Item';
      component.newItemDescription = 'Test Description';
      component.selectedTags = ['power-tools', 'hand-tools'];

      fixture.detectChanges();

      //act
      component.addItem();

      //assert
      setTimeout(() => {
        expect(mockItemsService.createItem).toHaveBeenCalledWith(
          expect.objectContaining({
            name: 'Test Item',
            description: 'Test Description',
            tags: ['power-tools', 'hand-tools']
          })
        );
        expect(component.success).toBeTruthy();
        done();
      }, 100);
    });

    it('should create item with empty tags array when no tags selected', (done) => {
      //arrange
      const mockItem: SharedItem = {
        id: 'item1',
        name: 'Test Item',
        description: 'Test Description',
        userId: 'user1',
        isAvailable: true,
        visibleToLoopIds: [],
        visibleToAllLoops: true,
        visibleToFutureLoops: true,
        tags: [],
        createdAt: new Date(),
        updatedAt: new Date()
      };
      mockItemsService.createItem.mockReturnValue(of(mockItem));
      mockLoopService.getUserLoops.mockReturnValue(of([]));
      
      component.newItemName = 'Test Item';
      component.newItemDescription = 'Test Description';
      component.selectedTags = [];

      fixture.detectChanges();

      //act
      component.addItem();

      //assert
      setTimeout(() => {
        expect(mockItemsService.createItem).toHaveBeenCalledWith(
          expect.objectContaining({
            name: 'Test Item',
            tags: []
          })
        );
        expect(component.success).toBeTruthy();
        done();
      }, 100);
    });

    it('should handle tag selection with maximum 10 tags', () => {
      //arrange
      const maxTags = [
        'power-tools', 'hand-tools', 'garden-tools', 'automotive-tools', 'measuring-tools',
        'lawn-care', 'cleaning-equipment', 'ladders', 'pressure-washers', 'painting-supplies'
      ];

      //act
      component.onTagsChange(maxTags);

      //assert
      expect(component.selectedTags).toEqual(maxTags);
      expect(component.selectedTags.length).toBe(10);
    });

    it('should preserve tags when item creation fails', (done) => {
      //arrange
      const selectedTags = ['power-tools', 'hand-tools'];
      mockItemsService.createItem.mockReturnValue(
        throwError(() => new Error('API Error'))
      );
      mockLoopService.getUserLoops.mockReturnValue(of([]));
      
      component.newItemName = 'Test Item';
      component.selectedTags = selectedTags;

      fixture.detectChanges();

      //act
      component.addItem();

      //assert
      setTimeout(() => {
        expect(component.selectedTags).toEqual(selectedTags);
        expect(component.error).toBeTruthy();
        done();
      }, 100);
    });
  });

  describe('Tag Validation', () => {
    it('should initialize with empty selectedTags array', () => {
      //arrange & act
      fixture.detectChanges();

      //assert
      expect(component.selectedTags).toEqual([]);
    });

    it('should allow item creation without tags', (done) => {
      //arrange
      const mockItem: SharedItem = {
        id: 'item1',
        name: 'Test Item',
        description: 'Test Description',
        userId: 'user1',
        isAvailable: true,
        visibleToLoopIds: [],
        visibleToAllLoops: true,
        visibleToFutureLoops: true,
        tags: [],
        createdAt: new Date(),
        updatedAt: new Date()
      };
      mockItemsService.createItem.mockReturnValue(of(mockItem));
      mockLoopService.getUserLoops.mockReturnValue(of([]));
      
      component.newItemName = 'Test Item';
      // No tags selected

      fixture.detectChanges();

      //act
      component.addItem();

      //assert
      setTimeout(() => {
        expect(mockItemsService.createItem).toHaveBeenCalled();
        expect(component.success).toBeTruthy();
        done();
      }, 100);
    });

    it('should include tags in the item object passed to createItem', (done) => {
      //arrange
      const tags = ['cameras', 'audio-equipment'];
      const mockItem: SharedItem = {
        id: 'item1',
        name: 'Camera',
        description: 'Professional camera',
        userId: 'user1',
        isAvailable: true,
        visibleToLoopIds: ['loop1'],
        visibleToAllLoops: false,
        visibleToFutureLoops: false,
        tags: tags,
        createdAt: new Date(),
        updatedAt: new Date()
      };
      mockItemsService.createItem.mockReturnValue(of(mockItem));
      mockLoopService.getUserLoops.mockReturnValue(of([]));
      
      component.newItemName = 'Camera';
      component.newItemDescription = 'Professional camera';
      component.selectedTags = tags;
      component.selectedLoopIds = ['loop1'];

      fixture.detectChanges();

      //act
      component.addItem();

      //assert
      setTimeout(() => {
        const createItemCall = mockItemsService.createItem.mock.calls[0][0];
        expect(createItemCall.tags).toEqual(tags);
        expect(createItemCall.name).toBe('Camera');
        expect(createItemCall.description).toBe('Professional camera');
        done();
      }, 100);
    });
  });
});
