import { TestBed } from '@angular/core/testing';
import { ItemFilterComponent } from './item-filter.component';
import { TagsService } from '../../services/tags.service';
import { ItemsService } from '../../services/items.service';
import { UserService } from '../../services/user.service';
import { of } from 'rxjs';
import { SystemTag } from '../../models/system-tag.interface';
import { ItemSearchFilter } from '../../models/item-search-filter.interface';
import * as fc from 'fast-check';

/**
 * Feature: item-search-and-categorization, Property 11: Filter clear round-trip
 * Validates: Requirements 6.4, 10.2, 10.4
 * 
 * Property: For any loop with items, applying filters then clearing all filters 
 * should return the same set of items as the initial unfiltered state
 */
describe('ItemFilterComponent Property Tests', () => {
  let component: ItemFilterComponent;
  let mockTagsService: any;
  let mockItemsService: any;
  let mockUserService: any;

  const mockTags: SystemTag[] = [
    { id: '1', name: 'tools', displayName: 'Tools', category: 'equipment', usageCount: 10, isActive: true },
    { id: '2', name: 'electronics', displayName: 'Electronics', category: 'tech', usageCount: 5, isActive: true },
    { id: '3', name: 'books', displayName: 'Books', category: 'media', usageCount: 8, isActive: true },
    { id: '4', name: 'sports', displayName: 'Sports', category: 'recreation', usageCount: 3, isActive: true }
  ];

  beforeEach(() => {
    //arrange
    mockTagsService = {
      getAllTags: jest.fn().mockReturnValue(of(mockTags))
    };

    mockItemsService = {
      getDistinctOwners: jest.fn().mockReturnValue(of(['owner1', 'owner2', 'owner3']))
    };

    mockUserService = {};

    TestBed.configureTestingModule({
      imports: [ItemFilterComponent],
      providers: [
        { provide: TagsService, useValue: mockTagsService },
        { provide: ItemsService, useValue: mockItemsService },
        { provide: UserService, useValue: mockUserService }
      ]
    });

    const fixture = TestBed.createComponent(ItemFilterComponent);
    component = fixture.componentInstance;
    component.loopId = 'test-loop';
    fixture.detectChanges();
  });

  /**
   * Property 11: Filter clear round-trip
   * 
   * For any combination of filters (tags, availability, owners), applying those filters
   * and then clearing all filters should result in a filter state that matches the
   * initial unfiltered state (all empty/default values).
   */
  it('should return to initial state after applying and clearing filters', () => {
    fc.assert(
      fc.property(
        // Generate arbitrary filter states
        fc.array(fc.constantFrom('tools', 'electronics', 'books', 'sports'), { minLength: 0, maxLength: 4 }),
        fc.constantFrom('all', 'available', 'unavailable'),
        fc.array(fc.constantFrom('owner1', 'owner2', 'owner3'), { minLength: 0, maxLength: 3 }),
        (tags, availability, owners) => {
          //arrange - capture initial state
          const initialFilter = captureFilterState(component);

          // Apply filters
          component.selectedTags = [...tags];
          component.availabilityFilter = availability as 'all' | 'available' | 'unavailable';
          component.selectedOwners = [...owners];

          //act - clear all filters
          component.clearAllFilters();

          //assert - should match initial state
          const finalFilter = captureFilterState(component);
          
          expect(finalFilter.selectedTags).toEqual(initialFilter.selectedTags);
          expect(finalFilter.availabilityFilter).toEqual(initialFilter.availabilityFilter);
          expect(finalFilter.selectedOwners).toEqual(initialFilter.selectedOwners);
          
          // Verify the emitted filter matches the cleared state
          expect(finalFilter.selectedTags).toEqual([]);
          expect(finalFilter.availabilityFilter).toBe('all');
          expect(finalFilter.selectedOwners).toEqual([]);
        }
      ),
      { numRuns: 100 }
    );
  });

  /**
   * Property: Clearing filters should emit a filter object with default values
   * 
   * After clearing filters, the emitted filter object should have:
   * - Empty tags array
   * - undefined isAvailable (representing 'all')
   * - Empty ownerIds array
   * - Default pagination values
   */
  it('should emit default filter object after clearing any filter combination', () => {
    fc.assert(
      fc.property(
        fc.array(fc.constantFrom('tools', 'electronics', 'books', 'sports'), { minLength: 1, maxLength: 4 }),
        fc.constantFrom('available', 'unavailable'),
        fc.array(fc.constantFrom('owner1', 'owner2', 'owner3'), { minLength: 1, maxLength: 3 }),
        (tags, availability, owners) => {
          //arrange - apply some filters
          component.selectedTags = [...tags];
          component.availabilityFilter = availability as 'available' | 'unavailable';
          component.selectedOwners = [...owners];

          let emittedFilter: ItemSearchFilter | undefined;
          component.filterChange.subscribe((filter) => {
            emittedFilter = filter;
          });

          //act - clear all filters
          component.clearAllFilters();

          //assert - emitted filter should have default values
          expect(emittedFilter).toBeDefined();
          expect(emittedFilter!.tags).toEqual([]);
          expect(emittedFilter!.isAvailable).toBeUndefined();
          expect(emittedFilter!.ownerIds).toEqual([]);
          expect(emittedFilter!.pageNumber).toBe(1);
          expect(emittedFilter!.pageSize).toBe(50);
        }
      ),
      { numRuns: 100 }
    );
  });

  /**
   * Property: Active filter count should be zero after clearing
   * 
   * For any filter combination, after clearing all filters,
   * the active filter count should be 0 and hasActiveFilters should be false.
   */
  it('should have zero active filters after clearing any filter combination', () => {
    fc.assert(
      fc.property(
        fc.array(fc.constantFrom('tools', 'electronics', 'books', 'sports'), { minLength: 0, maxLength: 4 }),
        fc.constantFrom('all', 'available', 'unavailable'),
        fc.array(fc.constantFrom('owner1', 'owner2', 'owner3'), { minLength: 0, maxLength: 3 }),
        (tags, availability, owners) => {
          //arrange - apply filters
          component.selectedTags = [...tags];
          component.availabilityFilter = availability as 'all' | 'available' | 'unavailable';
          component.selectedOwners = [...owners];

          //act - clear all filters
          component.clearAllFilters();

          //assert
          expect(component.getActiveFilterCount()).toBe(0);
          expect(component.hasActiveFilters()).toBe(false);
        }
      ),
      { numRuns: 100 }
    );
  });

  /**
   * Property: Multiple clear operations are idempotent
   * 
   * Clearing filters multiple times should have the same effect as clearing once.
   * The state after clearing twice should be identical to the state after clearing once.
   */
  it('should be idempotent when clearing filters multiple times', () => {
    fc.assert(
      fc.property(
        fc.array(fc.constantFrom('tools', 'electronics', 'books', 'sports'), { minLength: 1, maxLength: 4 }),
        fc.constantFrom('available', 'unavailable'),
        fc.array(fc.constantFrom('owner1', 'owner2', 'owner3'), { minLength: 1, maxLength: 3 }),
        (tags, availability, owners) => {
          //arrange - apply filters
          component.selectedTags = [...tags];
          component.availabilityFilter = availability as 'available' | 'unavailable';
          component.selectedOwners = [...owners];

          //act - clear filters twice
          component.clearAllFilters();
          const stateAfterFirstClear = captureFilterState(component);
          
          component.clearAllFilters();
          const stateAfterSecondClear = captureFilterState(component);

          //assert - both states should be identical
          expect(stateAfterSecondClear.selectedTags).toEqual(stateAfterFirstClear.selectedTags);
          expect(stateAfterSecondClear.availabilityFilter).toEqual(stateAfterFirstClear.availabilityFilter);
          expect(stateAfterSecondClear.selectedOwners).toEqual(stateAfterFirstClear.selectedOwners);
        }
      ),
      { numRuns: 100 }
    );
  });
});

/**
 * Helper function to capture the current filter state
 */
function captureFilterState(component: ItemFilterComponent) {
  return {
    selectedTags: [...component.selectedTags],
    availabilityFilter: component.availabilityFilter,
    selectedOwners: [...component.selectedOwners]
  };
}
