import { TagSelectorComponent } from './tag-selector.component';
import { TagsService } from '../../services/tags.service';
import { SystemTag } from '../../models/system-tag.interface';
import { of } from 'rxjs';

/**
 * Property-based tests for TagSelectorComponent
 * These tests verify invariants that should hold for any input
 */
describe('TagSelectorComponent - Property Tests', () => {
  let component: TagSelectorComponent;
  let mockTagsService: any;

  const createMockTag = (id: string, name: string, displayName: string): SystemTag => ({
    id,
    name,
    displayName,
    category: 'Test Category',
    usageCount: 0,
    isActive: true
  });

  const generateRandomTags = (count: number): SystemTag[] => {
    const tagNames = [
      'power-tools', 'hand-tools', 'garden-tools', 'camping-gear', 'sports-equipment',
      'kitchen-appliances', 'books', 'electronics', 'furniture', 'toys',
      'bikes', 'cameras', 'audio-equipment', 'cleaning-equipment', 'ladders',
      'painting-supplies', 'party-supplies', 'baby-gear', 'car-seats', 'strollers'
    ];
    
    return Array.from({ length: Math.min(count, tagNames.length) }, (_, i) => 
      createMockTag(`${i}`, tagNames[i], tagNames[i].replace(/-/g, ' ').replace(/\b\w/g, l => l.toUpperCase()))
    );
  };

  beforeEach(() => {
    //arrange
    const mockTags = generateRandomTags(20);
    
    mockTagsService = {
      getAllTags: jest.fn().mockReturnValue(of(mockTags)),
      searchTags: jest.fn((query: string, tags: SystemTag[]) => 
        tags.filter(t => 
          t.displayName.toLowerCase().includes(query.toLowerCase()) ||
          t.name.toLowerCase().includes(query.toLowerCase())
        )
      )
    };

    component = new TagSelectorComponent(mockTagsService);
    component.ngOnInit();
  });

  describe('Tag Selection Limit Enforcement Properties', () => {
    /**
     * Property 1: Tag selection limit enforcement (frontend)
     * For any item and any set of tags, selecting up to 10 tags should be accepted, 
     * and attempting to select more than 10 tags should be rejected with a validation error
     * 
     * **Feature: item-search-and-categorization, Property 1: Tag selection limit enforcement (frontend)**
     * **Validates: Requirements 1.3, 1.4**
     */
    it('should accept selection of up to 10 tags and reject more than 10', () => {
      //arrange
      const availableTags = generateRandomTags(20);
      component.availableTags = availableTags;
      component.filteredTags = availableTags;

      // Test cases with different numbers of tags
      const testCases = [
        { initialCount: 0, addCount: 5, shouldSucceed: true },
        { initialCount: 0, addCount: 10, shouldSucceed: true },
        { initialCount: 5, addCount: 5, shouldSucceed: true },
        { initialCount: 9, addCount: 1, shouldSucceed: true },
        { initialCount: 10, addCount: 1, shouldSucceed: false },
        { initialCount: 10, addCount: 5, shouldSucceed: false },
        { initialCount: 8, addCount: 3, shouldSucceed: false }
      ];

      testCases.forEach(({ initialCount, addCount, shouldSucceed }) => {
        //arrange
        component.selectedTags = availableTags.slice(0, initialCount).map(t => t.name);
        component.validationMessage = '';
        const initialLength = component.selectedTags.length;
        const emitSpy = jest.fn();
        component.selectedTagsChange.emit = emitSpy;

        //act
        for (let i = 0; i < addCount; i++) {
          const tagToAdd = availableTags[initialCount + i];
          if (tagToAdd && !component.selectedTags.includes(tagToAdd.name)) {
            component.onTagSelect(tagToAdd.name);
          }
        }

        //assert
        if (shouldSucceed) {
          // Invariant: Should accept all tags up to 10
          expect(component.selectedTags.length).toBe(Math.min(initialLength + addCount, 10));
          expect(component.selectedTags.length).toBeLessThanOrEqual(10);
          expect(component.validationMessage).toBe('');
        } else {
          // Invariant: Should reject tags beyond 10 and show validation message
          expect(component.selectedTags.length).toBe(10);
          expect(component.validationMessage).toContain('10 tags');
        }
      });
    });

    /**
     * Property: Maximum tag count is always 10
     * Invariant: selectedTags.length <= 10 at all times
     */
    it('should never exceed 10 tags regardless of selection attempts', () => {
      //arrange
      const availableTags = generateRandomTags(20);
      component.availableTags = availableTags;
      component.filteredTags = availableTags;
      component.selectedTags = [];

      //act
      // Attempt to select all 20 tags
      availableTags.forEach(tag => {
        component.onTagSelect(tag.name);
      });

      //assert
      // Invariant: Should never exceed 10 tags
      expect(component.selectedTags.length).toBe(10);
      expect(component.selectedTags.length).toBeLessThanOrEqual(10);
    });

    /**
     * Property: Validation message appears when limit is reached
     * Invariant: If selectedTags.length === 10 and user tries to add, validationMessage is set
     */
    it('should show validation message when attempting to exceed limit', () => {
      //arrange
      const availableTags = generateRandomTags(15);
      component.availableTags = availableTags;
      component.filteredTags = availableTags;
      component.selectedTags = availableTags.slice(0, 10).map(t => t.name);

      //act
      component.onTagSelect(availableTags[10].name);

      //assert
      // Invariant: Validation message should be set
      expect(component.validationMessage).toBeTruthy();
      expect(component.validationMessage).toContain('10 tags');
      expect(component.selectedTags.length).toBe(10);
    });

    /**
     * Property: Selection events are only emitted for successful additions
     * Invariant: selectedTagsChange.emit() is called only when tag is actually added
     */
    it('should only emit selection events for successful tag additions', () => {
      //arrange
      const availableTags = generateRandomTags(15);
      component.availableTags = availableTags;
      component.filteredTags = availableTags;
      
      const testCases = [
        { initialCount: 0, tagToAdd: 0, shouldEmit: true },
        { initialCount: 5, tagToAdd: 5, shouldEmit: true },
        { initialCount: 9, tagToAdd: 9, shouldEmit: true },
        { initialCount: 10, tagToAdd: 10, shouldEmit: false },
        { initialCount: 10, tagToAdd: 11, shouldEmit: false }
      ];

      testCases.forEach(({ initialCount, tagToAdd, shouldEmit }) => {
        //arrange
        component.selectedTags = availableTags.slice(0, initialCount).map(t => t.name);
        const emitSpy = jest.fn();
        component.selectedTagsChange.emit = emitSpy;

        //act
        component.onTagSelect(availableTags[tagToAdd].name);

        //assert
        if (shouldEmit) {
          // Invariant: Should emit when tag is successfully added
          expect(emitSpy).toHaveBeenCalled();
        } else {
          // Invariant: Should not emit when tag addition is rejected
          expect(emitSpy).not.toHaveBeenCalled();
        }
      });
    });

    /**
     * Property: Removing tags always succeeds and clears validation
     * Invariant: Removing a tag always reduces count and clears validation message
     */
    it('should always allow tag removal and clear validation message', () => {
      //arrange
      const availableTags = generateRandomTags(15);
      component.availableTags = availableTags;
      component.filteredTags = availableTags;
      
      const testCases = [
        { initialCount: 10, removeIndex: 0 },
        { initialCount: 10, removeIndex: 5 },
        { initialCount: 10, removeIndex: 9 },
        { initialCount: 5, removeIndex: 2 },
        { initialCount: 1, removeIndex: 0 }
      ];

      testCases.forEach(({ initialCount, removeIndex }) => {
        //arrange
        component.selectedTags = availableTags.slice(0, initialCount).map(t => t.name);
        component.validationMessage = 'Some validation message';
        const initialLength = component.selectedTags.length;
        const tagToRemove = component.selectedTags[removeIndex];
        const emitSpy = jest.fn();
        component.selectedTagsChange.emit = emitSpy;

        //act
        component.onTagRemove(tagToRemove);

        //assert
        // Invariant: Count should decrease by 1
        expect(component.selectedTags.length).toBe(initialLength - 1);
        // Invariant: Removed tag should not be in selected tags
        expect(component.selectedTags).not.toContain(tagToRemove);
        // Invariant: Validation message should be cleared
        expect(component.validationMessage).toBe('');
        // Invariant: Should emit the new selection
        expect(emitSpy).toHaveBeenCalled();
      });
    });

    /**
     * Property: Cannot select the same tag twice
     * Invariant: Attempting to select an already selected tag has no effect
     */
    it('should not allow duplicate tag selection', () => {
      //arrange
      const availableTags = generateRandomTags(15);
      component.availableTags = availableTags;
      component.filteredTags = availableTags;
      component.selectedTags = [availableTags[0].name, availableTags[1].name];
      const initialLength = component.selectedTags.length;
      const emitSpy = jest.fn();
      component.selectedTagsChange.emit = emitSpy;

      //act
      component.onTagSelect(availableTags[0].name); // Try to select already selected tag

      //assert
      // Invariant: Length should not change
      expect(component.selectedTags.length).toBe(initialLength);
      // Invariant: Should not emit
      expect(emitSpy).not.toHaveBeenCalled();
      // Invariant: Tag should still be in selected tags exactly once
      const count = component.selectedTags.filter(t => t === availableTags[0].name).length;
      expect(count).toBe(1);
    });

    /**
     * Property: Tag selection is order-preserving
     * Invariant: Tags appear in selectedTags in the order they were selected
     */
    it('should preserve selection order', () => {
      //arrange
      const availableTags = generateRandomTags(15);
      component.availableTags = availableTags;
      component.filteredTags = availableTags;
      component.selectedTags = [];
      const selectionOrder = [3, 1, 7, 2, 9, 0, 5];

      //act
      selectionOrder.forEach(index => {
        component.onTagSelect(availableTags[index].name);
      });

      //assert
      // Invariant: Selected tags should be in the order they were selected
      selectionOrder.forEach((tagIndex, orderIndex) => {
        expect(component.selectedTags[orderIndex]).toBe(availableTags[tagIndex].name);
      });
    });

    /**
     * Property: Limit enforcement is consistent across different starting states
     * Invariant: Regardless of how we reach 10 tags, the limit is enforced
     */
    it('should enforce limit consistently from any starting state', () => {
      //arrange
      const availableTags = generateRandomTags(20);
      component.availableTags = availableTags;
      component.filteredTags = availableTags;

      const scenarios = [
        // Add 10 tags one by one
        { setup: () => [], addSequence: [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10] },
        // Start with 5, add 5 more, then try to add more
        { setup: () => [0, 1, 2, 3, 4], addSequence: [5, 6, 7, 8, 9, 10, 11] },
        // Start with 8, add 2 more, then try to add more
        { setup: () => [0, 1, 2, 3, 4, 5, 6, 7], addSequence: [8, 9, 10, 11, 12] },
        // Start with 9, add 1 more, then try to add more
        { setup: () => [0, 1, 2, 3, 4, 5, 6, 7, 8], addSequence: [9, 10, 11] }
      ];

      scenarios.forEach(({ setup, addSequence }) => {
        //arrange
        const initialTags = setup();
        component.selectedTags = initialTags.map(i => availableTags[i].name);

        //act
        addSequence.forEach(index => {
          if (availableTags[index]) {
            component.onTagSelect(availableTags[index].name);
          }
        });

        //assert
        // Invariant: Should always end up with exactly 10 tags
        expect(component.selectedTags.length).toBe(10);
        expect(component.selectedTags.length).toBeLessThanOrEqual(10);
      });
    });

    /**
     * Property: Validation message clears after successful removal
     * Invariant: After showing validation and removing a tag, validation clears
     */
    it('should clear validation message after removing a tag from full selection', () => {
      //arrange
      const availableTags = generateRandomTags(15);
      component.availableTags = availableTags;
      component.filteredTags = availableTags;
      component.selectedTags = availableTags.slice(0, 10).map(t => t.name);

      //act
      // Try to add 11th tag (should show validation)
      component.onTagSelect(availableTags[10].name);
      expect(component.validationMessage).toBeTruthy();

      // Remove one tag
      component.onTagRemove(component.selectedTags[0]);

      //assert
      // Invariant: Validation message should be cleared
      expect(component.validationMessage).toBe('');
      expect(component.selectedTags.length).toBe(9);
    });

    /**
     * Property: Empty selection allows adding up to 10 tags
     * Invariant: Starting from empty, can add exactly 10 tags
     */
    it('should allow adding exactly 10 tags from empty state', () => {
      //arrange
      const availableTags = generateRandomTags(15);
      component.availableTags = availableTags;
      component.filteredTags = availableTags;
      component.selectedTags = [];

      //act
      for (let i = 0; i < 10; i++) {
        component.onTagSelect(availableTags[i].name);
      }

      //assert
      // Invariant: Should have exactly 10 tags
      expect(component.selectedTags.length).toBe(10);
      // Invariant: All 10 tags should be the first 10 from available tags
      for (let i = 0; i < 10; i++) {
        expect(component.selectedTags[i]).toBe(availableTags[i].name);
      }
    });

    /**
     * Property: Removing and re-adding maintains limit
     * Invariant: Remove-add cycles maintain the 10-tag limit
     */
    it('should maintain limit through remove-add cycles', () => {
      //arrange
      const availableTags = generateRandomTags(15);
      component.availableTags = availableTags;
      component.filteredTags = availableTags;
      component.selectedTags = availableTags.slice(0, 10).map(t => t.name);

      //act & assert
      // Cycle 1: Remove one, add one
      component.onTagRemove(component.selectedTags[0]);
      expect(component.selectedTags.length).toBe(9);
      component.onTagSelect(availableTags[10].name);
      expect(component.selectedTags.length).toBe(10);

      // Cycle 2: Remove two, add two
      component.onTagRemove(component.selectedTags[0]);
      component.onTagRemove(component.selectedTags[0]);
      expect(component.selectedTags.length).toBe(8);
      component.onTagSelect(availableTags[11].name);
      component.onTagSelect(availableTags[12].name);
      expect(component.selectedTags.length).toBe(10);

      // Try to add 11th
      component.onTagSelect(availableTags[13].name);
      
      //assert
      // Invariant: Should still be at 10 tags
      expect(component.selectedTags.length).toBe(10);
      expect(component.validationMessage).toContain('10 tags');
    });
  });
});
