import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ItemFilterComponent } from './item-filter.component';
import { TagsService } from '../../services/tags.service';
import { ItemsService } from '../../services/items.service';
import { UserService } from '../../services/user.service';
import { of, throwError } from 'rxjs';
import { SystemTag } from '../../models/system-tag.interface';

describe('ItemFilterComponent', () => {
  let component: ItemFilterComponent;
  let fixture: ComponentFixture<ItemFilterComponent>;
  let mockTagsService: any;
  let mockItemsService: any;
  let mockUserService: any;

  const mockTags: SystemTag[] = [
    { name: 'tools', displayName: 'Tools', usageCount: 10, isActive: true },
    { name: 'electronics', displayName: 'Electronics', usageCount: 5, isActive: true }
  ];

  beforeEach(async () => {
    //arrange
    mockTagsService = {
      getAllTags: jest.fn().mockReturnValue(of(mockTags))
    };

    mockItemsService = {
      getDistinctOwners: jest.fn().mockReturnValue(of(['owner1', 'owner2']))
    };

    mockUserService = {};

    await TestBed.configureTestingModule({
      imports: [ItemFilterComponent],
      providers: [
        { provide: TagsService, useValue: mockTagsService },
        { provide: ItemsService, useValue: mockItemsService },
        { provide: UserService, useValue: mockUserService }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(ItemFilterComponent);
    component = fixture.componentInstance;
  });

  it('should create', () => {
    //act
    fixture.detectChanges();

    //assert
    expect(component).toBeTruthy();
  });

  it('should load tags on init', () => {
    //act
    fixture.detectChanges();

    //assert
    expect(mockTagsService.getAllTags).toHaveBeenCalled();
    expect(component.availableTags).toEqual(mockTags);
  });

  it('should load owners when loopId is provided', () => {
    //arrange
    component.loopId = 'loop1';

    //act
    fixture.detectChanges();

    //assert
    expect(mockItemsService.getDistinctOwners).toHaveBeenCalledWith('loop1');
  });

  it('should toggle tag selection', () => {
    //arrange
    fixture.detectChanges();
    const emitSpy = jest.spyOn(component.filterChange, 'emit');

    //act
    component.onTagToggle('tools');

    //assert
    expect(component.selectedTags).toContain('tools');
    expect(emitSpy).toHaveBeenCalled();
  });

  it('should remove tag when toggled again', () => {
    //arrange
    fixture.detectChanges();
    component.selectedTags = ['tools'];
    const emitSpy = jest.spyOn(component.filterChange, 'emit');

    //act
    component.onTagToggle('tools');

    //assert
    expect(component.selectedTags).not.toContain('tools');
    expect(emitSpy).toHaveBeenCalled();
  });

  it('should emit filter change on availability change', () => {
    //arrange
    fixture.detectChanges();
    const emitSpy = jest.spyOn(component.filterChange, 'emit');
    component.availabilityFilter = 'available';

    //act
    component.onAvailabilityChange();

    //assert
    expect(emitSpy).toHaveBeenCalled();
  });

  it('should toggle owner selection', () => {
    //arrange
    fixture.detectChanges();
    const emitSpy = jest.spyOn(component.filterChange, 'emit');

    //act
    component.onOwnerToggle('owner1');

    //assert
    expect(component.selectedOwners).toContain('owner1');
    expect(emitSpy).toHaveBeenCalled();
  });

  it('should clear all filters', () => {
    //arrange
    fixture.detectChanges();
    component.selectedTags = ['tools'];
    component.availabilityFilter = 'available';
    component.selectedOwners = ['owner1'];
    const emitSpy = jest.spyOn(component.filterChange, 'emit');

    //act
    component.clearAllFilters();

    //assert
    expect(component.selectedTags).toEqual([]);
    expect(component.availabilityFilter).toBe('all');
    expect(component.selectedOwners).toEqual([]);
    expect(emitSpy).toHaveBeenCalled();
  });

  it('should calculate active filter count', () => {
    //arrange
    fixture.detectChanges();
    component.selectedTags = ['tools'];
    component.availabilityFilter = 'available';
    component.selectedOwners = ['owner1'];

    //act
    const count = component.getActiveFilterCount();

    //assert
    expect(count).toBe(3);
  });

  it('should check if has active filters', () => {
    //arrange
    fixture.detectChanges();
    component.selectedTags = ['tools'];

    //act
    const hasFilters = component.hasActiveFilters();

    //assert
    expect(hasFilters).toBe(true);
  });

  it('should return false when no filters are active', () => {
    //arrange
    fixture.detectChanges();

    //act
    const hasFilters = component.hasActiveFilters();

    //assert
    expect(hasFilters).toBe(false);
  });

  it('should calculate zero active filter count when no filters applied', () => {
    //arrange
    fixture.detectChanges();

    //act
    const count = component.getActiveFilterCount();

    //assert
    expect(count).toBe(0);
  });

  it('should emit correct filter object with tags', () => {
    //arrange
    fixture.detectChanges();
    let emittedFilter: any;
    component.filterChange.subscribe((filter) => {
      emittedFilter = filter;
    });

    //act
    component.onTagToggle('tools');

    //assert
    expect(emittedFilter).toBeDefined();
    expect(emittedFilter.tags).toEqual(['tools']);
    expect(emittedFilter.isAvailable).toBeUndefined();
    expect(emittedFilter.ownerIds).toEqual([]);
    expect(emittedFilter.pageNumber).toBe(1);
    expect(emittedFilter.pageSize).toBe(50);
  });

  it('should emit correct filter object with availability', () => {
    //arrange
    fixture.detectChanges();
    let emittedFilter: any;
    component.filterChange.subscribe((filter) => {
      emittedFilter = filter;
    });
    component.availabilityFilter = 'available';

    //act
    component.onAvailabilityChange();

    //assert
    expect(emittedFilter).toBeDefined();
    expect(emittedFilter.isAvailable).toBe(true);
  });

  it('should emit correct filter object with unavailable status', () => {
    //arrange
    fixture.detectChanges();
    let emittedFilter: any;
    component.filterChange.subscribe((filter) => {
      emittedFilter = filter;
    });
    component.availabilityFilter = 'unavailable';

    //act
    component.onAvailabilityChange();

    //assert
    expect(emittedFilter).toBeDefined();
    expect(emittedFilter.isAvailable).toBe(false);
  });

  it('should emit correct filter object with owners', () => {
    //arrange
    fixture.detectChanges();
    let emittedFilter: any;
    component.filterChange.subscribe((filter) => {
      emittedFilter = filter;
    });

    //act
    component.onOwnerToggle('owner1');

    //assert
    expect(emittedFilter).toBeDefined();
    expect(emittedFilter.ownerIds).toEqual(['owner1']);
  });

  it('should emit filter with all criteria combined', () => {
    //arrange
    fixture.detectChanges();
    let emittedFilter: any;
    component.filterChange.subscribe((filter) => {
      emittedFilter = filter;
    });
    component.selectedTags = ['tools'];
    component.availabilityFilter = 'available';
    component.selectedOwners = ['owner1'];

    //act
    component.onAvailabilityChange();

    //assert
    expect(emittedFilter).toBeDefined();
    expect(emittedFilter.tags).toEqual(['tools']);
    expect(emittedFilter.isAvailable).toBe(true);
    expect(emittedFilter.ownerIds).toEqual(['owner1']);
  });

  it('should display active filter count badge when filters are active', () => {
    //arrange
    fixture.detectChanges();
    component.selectedTags = ['tools', 'electronics'];
    component.availabilityFilter = 'available';

    //act
    const count = component.getActiveFilterCount();

    //assert
    expect(count).toBe(2);
    expect(component.hasActiveFilters()).toBe(true);
  });

  it('should handle error when loading tags', () => {
    //arrange
    const consoleErrorSpy = jest.spyOn(console, 'error').mockImplementation();
    mockTagsService.getAllTags.mockReturnValue(throwError(() => new Error('Failed to load tags')));

    //act
    fixture.detectChanges();

    //assert
    expect(consoleErrorSpy).toHaveBeenCalledWith('Error loading tags:', expect.any(Error));
    expect(component.isLoadingTags).toBe(false);
    consoleErrorSpy.mockRestore();
  });

  it('should handle error when loading owners', () => {
    //arrange
    const consoleErrorSpy = jest.spyOn(console, 'error').mockImplementation();
    mockItemsService.getDistinctOwners.mockReturnValue(throwError(() => new Error('Failed to load owners')));
    component.loopId = 'loop1';

    //act
    fixture.detectChanges();

    //assert
    expect(consoleErrorSpy).toHaveBeenCalledWith('Error loading owners:', expect.any(Error));
    expect(component.isLoadingOwners).toBe(false);
    consoleErrorSpy.mockRestore();
  });

  it('should get tag display name from available tags', () => {
    //arrange
    fixture.detectChanges();

    //act
    const displayName = component.getTagDisplayName('tools');

    //assert
    expect(displayName).toBe('Tools');
  });

  it('should return tag name if not found in available tags', () => {
    //arrange
    fixture.detectChanges();

    //act
    const displayName = component.getTagDisplayName('unknown-tag');

    //assert
    expect(displayName).toBe('unknown-tag');
  });

  it('should get owner name from available owners', () => {
    //arrange
    component.loopId = 'loop1';
    fixture.detectChanges();

    //act
    const ownerName = component.getOwnerName('owner1');

    //assert
    expect(ownerName).toContain('User');
  });

  it('should return owner id if not found in available owners', () => {
    //arrange
    fixture.detectChanges();

    //act
    const ownerName = component.getOwnerName('unknown-owner');

    //assert
    expect(ownerName).toBe('unknown-owner');
  });

  it('should toggle tag dropdown', () => {
    //arrange
    fixture.detectChanges();
    component.showTagDropdown = false;

    //act
    component.toggleTagDropdown();

    //assert
    expect(component.showTagDropdown).toBe(true);
  });

  it('should close owner dropdown when opening tag dropdown', () => {
    //arrange
    fixture.detectChanges();
    component.showOwnerDropdown = true;

    //act
    component.toggleTagDropdown();

    //assert
    expect(component.showTagDropdown).toBe(true);
    expect(component.showOwnerDropdown).toBe(false);
  });

  it('should toggle owner dropdown', () => {
    //arrange
    fixture.detectChanges();
    component.showOwnerDropdown = false;

    //act
    component.toggleOwnerDropdown();

    //assert
    expect(component.showOwnerDropdown).toBe(true);
  });

  it('should close tag dropdown when opening owner dropdown', () => {
    //arrange
    fixture.detectChanges();
    component.showTagDropdown = true;

    //act
    component.toggleOwnerDropdown();

    //assert
    expect(component.showOwnerDropdown).toBe(true);
    expect(component.showTagDropdown).toBe(false);
  });

  it('should close all dropdowns', () => {
    //arrange
    fixture.detectChanges();
    component.showTagDropdown = true;
    component.showOwnerDropdown = true;

    //act
    component.closeDropdowns();

    //assert
    expect(component.showTagDropdown).toBe(false);
    expect(component.showOwnerDropdown).toBe(false);
  });

  it('should not load owners if loopId is not provided', () => {
    //arrange
    component.loopId = '';

    //act
    fixture.detectChanges();

    //assert
    expect(mockItemsService.getDistinctOwners).not.toHaveBeenCalled();
  });

  it('should sort owners alphabetically', () => {
    //arrange
    mockItemsService.getDistinctOwners.mockReturnValue(of(['owner3', 'owner1', 'owner2']));
    component.loopId = 'loop1';

    //act
    fixture.detectChanges();

    //assert
    const ownerNames = component.availableOwners.map(o => o.name);
    const sortedNames = [...ownerNames].sort();
    expect(ownerNames).toEqual(sortedNames);
  });

  it('should remove owner when toggled again', () => {
    //arrange
    fixture.detectChanges();
    component.selectedOwners = ['owner1'];
    const emitSpy = jest.spyOn(component.filterChange, 'emit');

    //act
    component.onOwnerToggle('owner1');

    //assert
    expect(component.selectedOwners).not.toContain('owner1');
    expect(emitSpy).toHaveBeenCalled();
  });

  it('should emit filter change when clearing all filters', () => {
    //arrange
    fixture.detectChanges();
    component.selectedTags = ['tools'];
    component.availabilityFilter = 'available';
    component.selectedOwners = ['owner1'];
    let emittedFilter: any;
    component.filterChange.subscribe((filter) => {
      emittedFilter = filter;
    });

    //act
    component.clearAllFilters();

    //assert
    expect(emittedFilter).toBeDefined();
    expect(emittedFilter.tags).toEqual([]);
    expect(emittedFilter.isAvailable).toBeUndefined();
    expect(emittedFilter.ownerIds).toEqual([]);
  });
});
