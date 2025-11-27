import { ComponentFixture, TestBed } from '@angular/core/testing';
import { TagSelectorComponent } from './tag-selector.component';
import { TagsService } from '../../services/tags.service';
import { of, throwError } from 'rxjs';
import { SystemTag } from '../../models/system-tag.interface';

describe('TagSelectorComponent', () => {
  let component: TagSelectorComponent;
  let fixture: ComponentFixture<TagSelectorComponent>;
  let mockTagsService: any;

  const mockTags: SystemTag[] = [
    { name: 'tools', displayName: 'Tools', usageCount: 10, isActive: true },
    { name: 'electronics', displayName: 'Electronics', usageCount: 5, isActive: true },
    { name: 'books', displayName: 'Books', usageCount: 3, isActive: true }
  ];

  beforeEach(async () => {
    //arrange
    mockTagsService = {
      getAllTags: jest.fn().mockReturnValue(of(mockTags)),
      searchTags: jest.fn((query: string, tags: SystemTag[]) => 
        tags.filter(t => t.displayName.toLowerCase().includes(query.toLowerCase()))
      )
    };

    await TestBed.configureTestingModule({
      imports: [TagSelectorComponent],
      providers: [
        { provide: TagsService, useValue: mockTagsService }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(TagSelectorComponent);
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
    expect(component.filteredTags).toEqual(mockTags);
  });

  it('should handle tag selection', () => {
    //arrange
    fixture.detectChanges();
    const emitSpy = jest.spyOn(component.selectedTagsChange, 'emit');

    //act
    component.onTagSelect('tools');

    //assert
    expect(component.selectedTags).toContain('tools');
    expect(emitSpy).toHaveBeenCalledWith(['tools']);
  });

  it('should not select already selected tag', () => {
    //arrange
    fixture.detectChanges();
    component.selectedTags = ['tools'];
    const emitSpy = jest.spyOn(component.selectedTagsChange, 'emit');

    //act
    component.onTagSelect('tools');

    //assert
    expect(emitSpy).not.toHaveBeenCalled();
  });

  it('should enforce 10-tag limit', () => {
    //arrange
    fixture.detectChanges();
    component.selectedTags = ['tag1', 'tag2', 'tag3', 'tag4', 'tag5', 'tag6', 'tag7', 'tag8', 'tag9', 'tag10'];
    const emitSpy = jest.spyOn(component.selectedTagsChange, 'emit');

    //act
    component.onTagSelect('tools');

    //assert
    expect(component.selectedTags.length).toBe(10);
    expect(component.validationMessage).toContain('10 tags');
    expect(emitSpy).not.toHaveBeenCalled();
  });

  it('should remove tag', () => {
    //arrange
    fixture.detectChanges();
    component.selectedTags = ['tools', 'electronics'];
    const emitSpy = jest.spyOn(component.selectedTagsChange, 'emit');

    //act
    component.onTagRemove('tools');

    //assert
    expect(component.selectedTags).toEqual(['electronics']);
    expect(emitSpy).toHaveBeenCalledWith(['electronics']);
  });

  it('should filter tags based on search', () => {
    //arrange
    fixture.detectChanges();
    component.searchQuery = 'tool';

    //act
    component.onSearchChange();

    //assert
    expect(mockTagsService.searchTags).toHaveBeenCalledWith('tool', mockTags);
  });

  it('should clear search', () => {
    //arrange
    fixture.detectChanges();
    component.searchQuery = 'test';
    component.filteredTags = [];

    //act
    component.clearSearch();

    //assert
    expect(component.searchQuery).toBe('');
    expect(component.filteredTags).toEqual(mockTags);
  });

  it('should handle error loading tags', () => {
    //arrange
    mockTagsService.getAllTags.mockReturnValue(throwError(() => new Error('Load error')));

    //act
    fixture.detectChanges();

    //assert
    expect(component.errorMessage).toBe('Failed to load tags. Please try again.');
    expect(component.isLoading).toBe(false);
  });
});
