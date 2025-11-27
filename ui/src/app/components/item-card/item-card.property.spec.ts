import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { ItemCardComponent } from './item-card.component';
import { SharedItem } from '../../models/shared-item.interface';

/**
 * Property-based tests for ItemCardComponent tag display
 * These tests verify invariants that should hold for any number of tags
 */
describe('ItemCardComponent - Property Tests', () => {
  let component: ItemCardComponent;
  let fixture: ComponentFixture<ItemCardComponent>;

  const createMockItem = (tags?: string[]): SharedItem => ({
    id: '1',
    name: 'Test Item',
    description: 'Test Description',
    userId: 'user1',
    isAvailable: true,
    visibleToLoopIds: ['loop1'],
    visibleToAllLoops: false,
    visibleToFutureLoops: false,
    tags: tags,
    createdAt: new Date(),
    updatedAt: new Date()
  });

  const generateRandomTags = (count: number): string[] => {
    return Array.from({ length: count }, (_, i) => `tag${i}`);
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ItemCardComponent, HttpClientTestingModule]
    }).compileComponents();

    fixture = TestBed.createComponent(ItemCardComponent);
    component = fixture.componentInstance;
  });

  describe('Tag Display Limit Properties', () => {
    /**
     * Property: Visible tags should never exceed the maximum limit
     * Invariant: getVisibleTags().length <= MAX_VISIBLE_TAGS for any input
     * 
     * **Feature: item-search-and-categorization, Property 4: Tag display limit**
     * **Validates: Requirements 3.4**
     */
    it('should never show more than maximum visible tags', () => {
      const MAX_VISIBLE_TAGS = 5;
      
      // Test with various tag counts from 0 to 20
      for (let tagCount = 0; tagCount <= 20; tagCount++) {
        const tags = generateRandomTags(tagCount);
        component.item = createMockItem(tags);
        
        const visibleTags = component.getVisibleTags();
        
        // Invariant: Should never exceed maximum
        expect(visibleTags.length).toBeLessThanOrEqual(MAX_VISIBLE_TAGS);
        
        // Invariant: Should show all tags if count is within limit
        if (tagCount <= MAX_VISIBLE_TAGS) {
          expect(visibleTags.length).toBe(tagCount);
        } else {
          expect(visibleTags.length).toBe(MAX_VISIBLE_TAGS);
        }
      }
    });

    /**
     * Property: Hidden tag count should be accurate
     * Invariant: getHiddenTagCount() = max(0, totalTags - MAX_VISIBLE_TAGS)
     */
    it('should calculate hidden tag count correctly for any number of tags', () => {
      const MAX_VISIBLE_TAGS = 5;
      
      // Test with various tag counts
      for (let tagCount = 0; tagCount <= 25; tagCount++) {
        const tags = generateRandomTags(tagCount);
        component.item = createMockItem(tags);
        
        const hiddenCount = component.getHiddenTagCount();
        const expectedHiddenCount = Math.max(0, tagCount - MAX_VISIBLE_TAGS);
        
        // Invariant: Hidden count should match expected calculation
        expect(hiddenCount).toBe(expectedHiddenCount);
        
        // Invariant: Hidden count should never be negative
        expect(hiddenCount).toBeGreaterThanOrEqual(0);
      }
    });

    /**
     * Property: Visible tags should be a subset of all tags
     * Invariant: Every visible tag should exist in the original tags array
     */
    it('should show visible tags that are subset of all tags', () => {
      // Test with various tag configurations
      const testCases = [
        generateRandomTags(3),
        generateRandomTags(5),
        generateRandomTags(7),
        generateRandomTags(10),
        generateRandomTags(15),
        ['a', 'b', 'c', 'd', 'e', 'f', 'g', 'h'],
        ['tag1', 'tag2', 'tag3', 'tag4', 'tag5', 'tag6']
      ];
      
      testCases.forEach(tags => {
        component.item = createMockItem(tags);
        
        const visibleTags = component.getVisibleTags();
        
        // Invariant: Every visible tag should exist in original tags
        visibleTags.forEach(visibleTag => {
          expect(tags).toContain(visibleTag);
        });
        
        // Invariant: Visible tags should maintain original order
        const expectedVisibleTags = tags.slice(0, Math.min(tags.length, 5));
        expect(visibleTags).toEqual(expectedVisibleTags);
      });
    });

    /**
     * Property: hasTags() should be consistent with tag array state
     * Invariant: hasTags() = (tags exists and tags.length > 0)
     */
    it('should correctly identify presence of tags', () => {
      const testCases = [
        { tags: undefined, expected: false },
        { tags: [], expected: false },
        { tags: ['tag1'], expected: true },
        { tags: generateRandomTags(0), expected: false },
        { tags: generateRandomTags(1), expected: true },
        { tags: generateRandomTags(10), expected: true },
        { tags: generateRandomTags(100), expected: true }
      ];
      
      testCases.forEach(({ tags, expected }) => {
        component.item = createMockItem(tags);
        
        // Invariant: hasTags should match expected result
        expect(component.hasTags()).toBe(expected);
      });
    });

    /**
     * Property: Tag display functions should handle edge cases gracefully
     * Invariant: Functions should not throw errors for any valid input
     */
    it('should handle edge cases without errors', () => {
      const edgeCases = [
        undefined,
        [],
        [''],
        [' '],
        ['a'.repeat(1000)], // Very long tag name
        Array(1000).fill('tag'), // Many duplicate tags
        generateRandomTags(1000) // Very many tags
      ];
      
      edgeCases.forEach(tags => {
        component.item = createMockItem(tags);
        
        // Should not throw errors
        expect(() => component.getVisibleTags()).not.toThrow();
        expect(() => component.getHiddenTagCount()).not.toThrow();
        expect(() => component.hasTags()).not.toThrow();
        
        // Results should be valid
        const visibleTags = component.getVisibleTags();
        const hiddenCount = component.getHiddenTagCount();
        const hasTagsResult = component.hasTags();
        
        expect(Array.isArray(visibleTags)).toBe(true);
        expect(typeof hiddenCount).toBe('number');
        expect(typeof hasTagsResult).toBe('boolean');
        expect(hiddenCount).toBeGreaterThanOrEqual(0);
      });
    });

    /**
     * Property: Total tags = visible tags + hidden tags
     * Invariant: For any item, visibleTags.length + hiddenCount = totalTags
     */
    it('should maintain tag count consistency', () => {
      // Test with various tag counts
      for (let tagCount = 0; tagCount <= 30; tagCount++) {
        const tags = generateRandomTags(tagCount);
        component.item = createMockItem(tags);
        
        const visibleTags = component.getVisibleTags();
        const hiddenCount = component.getHiddenTagCount();
        const totalTags = tags.length;
        
        // Invariant: Visible + hidden should equal total
        expect(visibleTags.length + hiddenCount).toBe(totalTags);
      }
    });
  });
});
