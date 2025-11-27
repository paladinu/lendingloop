import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute } from '@angular/router';
import { provideHttpClient } from '@angular/common/http';
import { of } from 'rxjs';
import { LoopDetailComponent } from './loop-detail.component';
import { LoopService } from '../../services/loop.service';
import { ItemsService } from '../../services/items.service';
import { UserService } from '../../services/user.service';
import { TagsService } from '../../services/tags.service';
import { Loop } from '../../models/loop.interface';
import { SharedItem } from '../../models/shared-item.interface';
import { AuthService } from '../../services/auth.service';

describe('LoopDetailComponent', () => {
  let component: LoopDetailComponent;
  let fixture: ComponentFixture<LoopDetailComponent>;
  let mockLoopService: jest.Mocked<LoopService>;
  let mockAuthService: jest.Mocked<AuthService>;
  let mockActivatedRoute: any;

  const mockLoop: Loop = {
    id: 'loop1',
    name: 'Test Loop',
    description: 'Test Description',
    creatorId: 'user1',
    memberIds: ['user1', 'user2'],
    isPublic: false,
    isArchived: false,
    ownershipHistory: [],
    createdAt: new Date(),
    updatedAt: new Date()
  };

  const mockItems: SharedItem[] = [
    {
      id: 'item1',
      name: 'Test Item',
      description: 'Test Description',
      userId: 'user1',
      isAvailable: true,
      visibleToLoopIds: ['loop1'],
      visibleToAllLoops: false,
      visibleToFutureLoops: false,
      tags: [],
      createdAt: new Date(),
      updatedAt: new Date()
    }
  ];

  let mockItemsService: any;
  let mockUserService: any;
  let mockTagsService: any;

  beforeEach(() => {
    jest.useFakeTimers();
    
    mockLoopService = {
      getLoopById: jest.fn(),
    } as any;

    mockItemsService = {
      searchItems: jest.fn(),
      getDistinctOwners: jest.fn().mockReturnValue(of([]))
    };

    mockUserService = {
      getUsersByIds: jest.fn().mockReturnValue(of([]))
    };

    mockTagsService = {
      getAllTags: jest.fn().mockReturnValue(of([]))
    };

    mockAuthService = {
      getCurrentUserId: jest.fn().mockReturnValue('user1'),
      getCurrentUser: jest.fn().mockReturnValue(of({ id: 'user1', email: 'test@example.com', firstName: 'Test', lastName: 'User', streetAddress: '123 Test St', isEmailVerified: true })),
      refreshCurrentUser: jest.fn().mockReturnValue(of({ id: 'user1', email: 'test@example.com', firstName: 'Test', lastName: 'User', streetAddress: '123 Test St', isEmailVerified: true })),
    } as any;

    mockActivatedRoute = {
      snapshot: {
        paramMap: {
          get: jest.fn().mockReturnValue('loop1')
        }
      },
      paramMap: of({
        get: jest.fn().mockReturnValue('loop1')
      })
    };

    TestBed.configureTestingModule({
      imports: [LoopDetailComponent],
      providers: [
        provideHttpClient(),
        { provide: LoopService, useValue: mockLoopService },
        { provide: ItemsService, useValue: mockItemsService },
        { provide: UserService, useValue: mockUserService },
        { provide: TagsService, useValue: mockTagsService },
        { provide: AuthService, useValue: mockAuthService },
        { provide: ActivatedRoute, useValue: mockActivatedRoute }
      ]
    });

    fixture = TestBed.createComponent(LoopDetailComponent);
    component = fixture.componentInstance;
  });

  afterEach(() => {
    jest.useRealTimers();
  });

  it('should create', () => {
    //assert
    expect(component).toBeTruthy();
  });

  it('should load loop and search items on init', () => {
    //arrange
    mockLoopService.getLoopById.mockReturnValue(of(mockLoop));
    mockItemsService.searchItems.mockReturnValue(of({ 
      items: mockItems, 
      totalCount: 1,
      pageNumber: 1,
      pageSize: 50,
      totalPages: 1
    }));

    //act
    fixture.detectChanges();
    jest.advanceTimersByTime(50);

    //assert
    expect(mockLoopService.getLoopById).toHaveBeenCalledWith('loop1');
    expect(mockItemsService.searchItems).toHaveBeenCalled();
    expect(component.loop).toEqual(mockLoop);
    expect(component.items).toEqual(mockItems);
  });

  describe('Filter Integration', () => {
    beforeEach(() => {
      mockLoopService.getLoopById.mockReturnValue(of(mockLoop));
      mockItemsService.searchItems.mockReturnValue(of({ 
        items: mockItems, 
        totalCount: 1,
        pageNumber: 1,
        pageSize: 50,
        totalPages: 1
      }));
      fixture.detectChanges();
    });

    it('should handle filter changes from ItemFilterComponent', () => {
      //arrange
      const newFilter = {
        tags: ['power-tools'],
        isAvailable: true,
        ownerIds: ['user1'],
        pageNumber: 1,
        pageSize: 50
      };
      const expectedItems = [mockItems[0]];
      mockItemsService.searchItems.mockReturnValue(of({ 
        items: expectedItems, 
        totalCount: 1,
        pageNumber: 1,
        pageSize: 50,
        totalPages: 1
      }));

      //act
      component.onFilterChange(newFilter);
      jest.advanceTimersByTime(100);

      //assert
      expect(mockItemsService.searchItems).toHaveBeenCalledWith('loop1', expect.objectContaining({
        tags: ['power-tools'],
        isAvailable: true,
        ownerIds: ['user1']
      }));
      expect(component.items).toEqual(expectedItems);
      expect(component.totalResults).toBe(1);
    });

    it('should preserve search text when filter changes', () => {
      //arrange
      component.searchQuery = 'drill';
      const newFilter = {
        tags: ['power-tools'],
        isAvailable: undefined,
        ownerIds: [],
        pageNumber: 1,
        pageSize: 50
      };

      //act
      component.onFilterChange(newFilter);
      jest.advanceTimersByTime(100);

      //assert
      expect(mockItemsService.searchItems).toHaveBeenCalledWith('loop1', expect.objectContaining({
        searchText: 'drill',
        tags: ['power-tools']
      }));
    });

    it('should update current filter state when filter changes', () => {
      //arrange
      const newFilter = {
        tags: ['hand-tools', 'garden-tools'],
        isAvailable: false,
        ownerIds: ['user2'],
        pageNumber: 1,
        pageSize: 50
      };

      //act
      component.onFilterChange(newFilter);

      //assert
      expect(component.currentFilter.tags).toEqual(['hand-tools', 'garden-tools']);
      expect(component.currentFilter.isAvailable).toBe(false);
      expect(component.currentFilter.ownerIds).toEqual(['user2']);
    });
  });

  describe('Search with Debouncing', () => {
    beforeEach(() => {
      mockLoopService.getLoopById.mockReturnValue(of(mockLoop));
      mockItemsService.searchItems.mockReturnValue(of({ 
        items: mockItems, 
        totalCount: 1,
        pageNumber: 1,
        pageSize: 50,
        totalPages: 1
      }));
      fixture.detectChanges();
    });

    it('should debounce search text changes', () => {
      //arrange
      mockItemsService.searchItems.mockClear();
      const searchResults = [mockItems[0]];
      mockItemsService.searchItems.mockReturnValue(of({ 
        items: searchResults, 
        totalCount: 1,
        pageNumber: 1,
        pageSize: 50,
        totalPages: 1
      }));

      //act
      component.searchQuery = 'd';
      component.onSearchChange();
      component.searchQuery = 'dr';
      component.onSearchChange();
      component.searchQuery = 'dri';
      component.onSearchChange();
      component.searchQuery = 'drill';
      component.onSearchChange();
      jest.advanceTimersByTime(350);

      //assert
      // Should only call search once after debounce period
      expect(mockItemsService.searchItems).toHaveBeenCalledTimes(1);
      expect(mockItemsService.searchItems).toHaveBeenCalledWith('loop1', expect.objectContaining({
        searchText: 'drill'
      }));
    });

    it('should update search filter with search text', () => {
      //arrange
      mockItemsService.searchItems.mockClear();

      //act
      component.searchQuery = 'hammer';
      component.onSearchChange();
      jest.advanceTimersByTime(350);

      //assert
      expect(mockItemsService.searchItems).toHaveBeenCalledWith('loop1', expect.objectContaining({
        searchText: 'hammer'
      }));
    });

    it('should handle empty search text', () => {
      //arrange
      mockItemsService.searchItems.mockClear();
      component.searchQuery = 'test';
      component.onSearchChange();
      jest.advanceTimersByTime(350);

      mockItemsService.searchItems.mockClear();
      
      //act
      component.searchQuery = '';
      component.onSearchChange();
      jest.advanceTimersByTime(350);

      //assert
      expect(mockItemsService.searchItems).toHaveBeenCalledWith('loop1', expect.objectContaining({
        searchText: undefined
      }));
    });
  });

  describe('Result Display', () => {
    beforeEach(() => {
      mockLoopService.getLoopById.mockReturnValue(of(mockLoop));
      mockItemsService.searchItems.mockReturnValue(of({ 
        items: mockItems, 
        totalCount: 1,
        pageNumber: 1,
        pageSize: 50,
        totalPages: 1
      }));
      fixture.detectChanges();
    });

    it('should display loading indicator during search', () => {
      //arrange
      component.searching = false;
      let searchingDuringCall = false;
      mockItemsService.searchItems.mockImplementation(() => {
        searchingDuringCall = component.searching;
        return of({ 
          items: mockItems, 
          totalCount: 1,
          pageNumber: 1,
          pageSize: 50,
          totalPages: 1
        });
      });

      //act
      component.performSearch();

      //assert
      expect(searchingDuringCall).toBe(true);
    });

    it('should hide loading indicator after search completes', () => {
      //arrange
      mockItemsService.searchItems.mockClear();
      mockItemsService.searchItems.mockReturnValue(of({ 
        items: mockItems, 
        totalCount: 1,
        pageNumber: 1,
        pageSize: 50,
        totalPages: 1
      }));

      //act
      component.performSearch();
      jest.advanceTimersByTime(100);

      //assert
      expect(component.searching).toBe(false);
    });

    it('should display result count', () => {
      //arrange
      const multipleItems = [mockItems[0], { ...mockItems[0], id: 'item2' }, { ...mockItems[0], id: 'item3' }];
      mockItemsService.searchItems.mockClear();
      mockItemsService.searchItems.mockReturnValue(of({ 
        items: multipleItems, 
        totalCount: 3,
        pageNumber: 1,
        pageSize: 50,
        totalPages: 1
      }));

      //act
      component.performSearch();
      jest.advanceTimersByTime(100);

      //assert
      expect(component.totalResults).toBe(3);
    });

    it('should handle empty results', () => {
      //arrange
      mockItemsService.searchItems.mockClear();
      mockItemsService.searchItems.mockReturnValue(of({ 
        items: [], 
        totalCount: 0,
        pageNumber: 1,
        pageSize: 50,
        totalPages: 0
      }));

      //act
      component.performSearch();
      jest.advanceTimersByTime(100);

      //assert
      expect(component.items).toEqual([]);
      expect(component.totalResults).toBe(0);
    });

    it('should display error message on search failure', () => {
      //arrange
      const error = new Error('Search failed');
      mockItemsService.searchItems.mockClear();
      mockItemsService.searchItems.mockReturnValue(
        new (require('rxjs').Observable)((observer: any) => {
          observer.error(error);
        })
      );

      //act
      component.performSearch();
      jest.advanceTimersByTime(100);

      //assert
      expect(component.error).toBe('Failed to search items');
      expect(component.searching).toBe(false);
    });
  });

  describe('Pagination', () => {
    beforeEach(() => {
      mockLoopService.getLoopById.mockReturnValue(of(mockLoop));
      mockItemsService.searchItems.mockReturnValue(of({ 
        items: mockItems, 
        totalCount: 100,
        pageNumber: 1,
        pageSize: 50,
        totalPages: 2
      }));
      fixture.detectChanges();
    });

    it('should handle page change events', () => {
      //arrange
      mockItemsService.searchItems.mockClear();
      const page2Items = [{ ...mockItems[0], id: 'item51' }];
      mockItemsService.searchItems.mockReturnValue(of({ 
        items: page2Items, 
        totalCount: 100,
        pageNumber: 2,
        pageSize: 50,
        totalPages: 2
      }));

      //act
      component.onPageChange(2);
      jest.advanceTimersByTime(100);

      //assert
      expect(component.currentFilter.pageNumber).toBe(2);
      expect(mockItemsService.searchItems).toHaveBeenCalledWith('loop1', expect.objectContaining({
        pageNumber: 2
      }));
      expect(component.items).toEqual(page2Items);
    });

    it('should preserve filters when changing pages', () => {
      //arrange
      component.currentFilter = {
        tags: ['power-tools'],
        isAvailable: true,
        ownerIds: ['user1'],
        pageNumber: 1,
        pageSize: 50,
        searchText: 'drill'
      };
      mockItemsService.searchItems.mockClear();

      //act
      component.onPageChange(2);
      jest.advanceTimersByTime(100);

      //assert
      expect(mockItemsService.searchItems).toHaveBeenCalledWith('loop1', expect.objectContaining({
        tags: ['power-tools'],
        isAvailable: true,
        ownerIds: ['user1'],
        searchText: 'drill',
        pageNumber: 2
      }));
    });
  });

  describe('Item Navigation', () => {
    beforeEach(() => {
      mockLoopService.getLoopById.mockReturnValue(of(mockLoop));
      mockItemsService.searchItems.mockReturnValue(of({ 
        items: mockItems, 
        totalCount: 1,
        pageNumber: 1,
        pageSize: 50,
        totalPages: 1
      }));
      fixture.detectChanges();
    });

    it('should navigate to edit item page when onEditItem is called', () => {
      //arrange
      const itemId = 'item1';
      const navigateSpy = jest.spyOn(component['router'], 'navigate');

      //act
      component.onEditItem(itemId);

      //assert
      expect(navigateSpy).toHaveBeenCalledWith(['/items', itemId, 'edit']);
    });

    it('should navigate to edit visibility page when onEditVisibility is called', () => {
      //arrange
      const itemId = 'item1';
      const navigateSpy = jest.spyOn(component['router'], 'navigate');

      //act
      component.onEditVisibility(itemId);

      //assert
      expect(navigateSpy).toHaveBeenCalledWith(['/items', itemId, 'visibility']);
    });
  });

  describe('Tag Click Handling', () => {
    beforeEach(() => {
      mockLoopService.getLoopById.mockReturnValue(of(mockLoop));
      mockItemsService.searchItems.mockReturnValue(of({ 
        items: mockItems, 
        totalCount: 1,
        pageNumber: 1,
        pageSize: 50,
        totalPages: 1
      }));
      fixture.detectChanges();
    });

    it('should add clicked tag to active filters', () => {
      //arrange
      component.currentFilter.tags = [];
      mockItemsService.searchItems.mockClear();

      //act
      component.onTagClick('power-tools');
      jest.advanceTimersByTime(100);

      //assert
      expect(component.currentFilter.tags).toContain('power-tools');
      expect(mockItemsService.searchItems).toHaveBeenCalledWith('loop1', expect.objectContaining({
        tags: ['power-tools']
      }));
    });

    it('should not add duplicate tags', () => {
      //arrange
      component.currentFilter.tags = ['power-tools'];
      mockItemsService.searchItems.mockClear();

      //act
      component.onTagClick('power-tools');
      jest.advanceTimersByTime(100);

      //assert
      expect(component.currentFilter.tags).toEqual(['power-tools']);
      expect(mockItemsService.searchItems).not.toHaveBeenCalled();
    });

    it('should trigger search with updated filters after tag click', () => {
      //arrange
      component.currentFilter.tags = ['hand-tools'];
      mockItemsService.searchItems.mockClear();
      const filteredItems = [mockItems[0]];
      mockItemsService.searchItems.mockReturnValue(of({ 
        items: filteredItems, 
        totalCount: 1,
        pageNumber: 1,
        pageSize: 50,
        totalPages: 1
      }));

      //act
      component.onTagClick('garden-tools');
      jest.advanceTimersByTime(100);

      //assert
      expect(component.currentFilter.tags).toEqual(['hand-tools', 'garden-tools']);
      expect(mockItemsService.searchItems).toHaveBeenCalledWith('loop1', expect.objectContaining({
        tags: ['hand-tools', 'garden-tools']
      }));
      expect(component.items).toEqual(filteredItems);
    });

    it('should preserve existing filters when adding tag', () => {
      //arrange
      component.currentFilter = {
        tags: [],
        isAvailable: true,
        ownerIds: ['user1'],
        pageNumber: 1,
        pageSize: 50,
        searchText: 'drill'
      };
      mockItemsService.searchItems.mockClear();

      //act
      component.onTagClick('power-tools');
      jest.advanceTimersByTime(100);

      //assert
      expect(mockItemsService.searchItems).toHaveBeenCalledWith('loop1', expect.objectContaining({
        tags: ['power-tools'],
        isAvailable: true,
        ownerIds: ['user1'],
        searchText: 'drill'
      }));
    });
  });
});
