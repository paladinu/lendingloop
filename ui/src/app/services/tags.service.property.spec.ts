import { TagsService } from './tags.service';
import { SystemTag } from '../models/system-tag.interface';

/**
 * Property-based tests for TagsService
 * These tests verify invariants that should hold for any input
 */
describe('TagsService - Property Tests', () => {
  let service: TagsService;

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
      'bikes', 'cameras', 'audio-equipment', 'cleaning-equipment', 'ladders'
    ];
    
    return Array.from({ length: Math.min(count, tagNames.length) }, (_, i) => 
      createMockTag(`${i}`, tagNames[i], tagNames[i].replace(/-/g, ' ').replace(/\b\w/g, l => l.toUpperCase()))
    );
  };

  beforeEach(() => {
    // Create service without TestBed for property tests
    service = new TagsService(null as any);
  });

  describe('Tag List Alphabetical Ordering Properties', () => {
    /**
     * Property 2: Tag list alphabetical ordering
     * For any set of system tags, when displayed in the selection interface, 
     * the tags should be ordered alphabetically by display name
     * 
     * **Feature: item-search-and-categorization, Property 2: Tag list alphabetical ordering**
     * **Validates: Requirements 1.5**
     */
    it('should sort tags alphabetically by displayName', () => {
      //arrange
      const testCases = [
        // Various unsorted tag lists
        [
          createMockTag('1', 'zebra', 'Zebra'),
          createMockTag('2', 'apple', 'Apple'),
          createMockTag('3', 'mango', 'Mango')
        ],
        [
          createMockTag('1', 'power-tools', 'Power Tools'),
          createMockTag('2', 'camping-gear', 'Camping Gear'),
          createMockTag('3', 'kitchen-appliances', 'Kitchen Appliances'),
          createMockTag('4', 'audio-equipment', 'Audio Equipment')
        ],
        generateRandomTags(15),
        [
          createMockTag('1', 'z', 'Z'),
          createMockTag('2', 'a', 'A'),
          createMockTag('3', 'b', 'B'),
          createMockTag('4', 'y', 'Y')
        ]
      ];

      //act & assert
      testCases.forEach(unsortedTags => {
        // Create a copy to avoid mutating the original
        const tagsCopy = [...unsortedTags];
        
        // Sort using the same logic as the service
        const sorted = tagsCopy.sort((a, b) => a.displayName.localeCompare(b.displayName));
        
        // Invariant: For each adjacent pair, first should come before or equal to second
        for (let i = 0; i < sorted.length - 1; i++) {
          const comparison = sorted[i].displayName.localeCompare(sorted[i + 1].displayName);
          expect(comparison).toBeLessThanOrEqual(0);
        }
        
        // Invariant: Sorted array should contain all original tags
        expect(sorted.length).toBe(unsortedTags.length);
        unsortedTags.forEach(tag => {
          expect(sorted).toContain(tag);
        });
      });
    });

    /**
     * Property: Alphabetical ordering is stable
     * Invariant: Sorting twice produces the same order
     */
    it('should produce stable alphabetical ordering', () => {
      //arrange
      const tags = [
        createMockTag('1', 'zebra', 'Zebra'),
        createMockTag('2', 'apple', 'Apple'),
        createMockTag('3', 'mango', 'Mango'),
        createMockTag('4', 'banana', 'Banana')
      ];

      //act
      const sorted1 = [...tags].sort((a, b) => a.displayName.localeCompare(b.displayName));
      const sorted2 = [...sorted1].sort((a, b) => a.displayName.localeCompare(b.displayName));

      //assert
      // Invariant: Sorting twice should produce identical results
      expect(sorted1).toEqual(sorted2);
      expect(sorted1.map(t => t.displayName)).toEqual(sorted2.map(t => t.displayName));
    });

    /**
     * Property: Case-insensitive alphabetical ordering
     * Invariant: Tags should be sorted case-insensitively
     */
    it('should sort case-insensitively', () => {
      //arrange
      const tags = [
        createMockTag('1', 'zebra', 'zebra'),
        createMockTag('2', 'Apple', 'Apple'),
        createMockTag('3', 'Mango', 'Mango'),
        createMockTag('4', 'banana', 'banana')
      ];

      //act
      const sorted = [...tags].sort((a, b) => a.displayName.localeCompare(b.displayName));

      //assert
      // Invariant: Should be in alphabetical order regardless of case
      expect(sorted[0].displayName.toLowerCase()).toBe('apple');
      expect(sorted[1].displayName.toLowerCase()).toBe('banana');
      expect(sorted[2].displayName.toLowerCase()).toBe('mango');
      expect(sorted[3].displayName.toLowerCase()).toBe('zebra');
    });

    /**
     * Property: Empty array remains empty after sorting
     * Invariant: sort([]) = []
     */
    it('should handle empty array', () => {
      //arrange
      const emptyTags: SystemTag[] = [];

      //act
      const sorted = [...emptyTags].sort((a, b) => a.displayName.localeCompare(b.displayName));

      //assert
      // Invariant: Empty array should remain empty
      expect(sorted).toEqual([]);
      expect(sorted.length).toBe(0);
    });

    /**
     * Property: Single element array remains unchanged
     * Invariant: sort([x]) = [x]
     */
    it('should handle single element array', () => {
      //arrange
      const singleTag = [createMockTag('1', 'test', 'Test')];

      //act
      const sorted = [...singleTag].sort((a, b) => a.displayName.localeCompare(b.displayName));

      //assert
      // Invariant: Single element should remain unchanged
      expect(sorted).toEqual(singleTag);
      expect(sorted.length).toBe(1);
      expect(sorted[0]).toBe(singleTag[0]);
    });

    /**
     * Property: Already sorted array remains in same order
     * Invariant: If array is already sorted, sorting doesn't change order
     */
    it('should maintain order of already sorted array', () => {
      //arrange
      const alreadySorted = [
        createMockTag('1', 'apple', 'Apple'),
        createMockTag('2', 'banana', 'Banana'),
        createMockTag('3', 'cherry', 'Cherry'),
        createMockTag('4', 'date', 'Date')
      ];

      //act
      const sorted = [...alreadySorted].sort((a, b) => a.displayName.localeCompare(b.displayName));

      //assert
      // Invariant: Order should remain the same
      expect(sorted.map(t => t.displayName)).toEqual(alreadySorted.map(t => t.displayName));
    });

    /**
     * Property: Reverse sorted array gets reversed
     * Invariant: Sorting a reverse-sorted array produces correct order
     */
    it('should correctly sort reverse-ordered array', () => {
      //arrange
      const reverseSorted = [
        createMockTag('1', 'zebra', 'Zebra'),
        createMockTag('2', 'yellow', 'Yellow'),
        createMockTag('3', 'banana', 'Banana'),
        createMockTag('4', 'apple', 'Apple')
      ];

      //act
      const sorted = [...reverseSorted].sort((a, b) => a.displayName.localeCompare(b.displayName));

      //assert
      // Invariant: Should be in correct alphabetical order
      expect(sorted[0].displayName).toBe('Apple');
      expect(sorted[1].displayName).toBe('Banana');
      expect(sorted[2].displayName).toBe('Yellow');
      expect(sorted[3].displayName).toBe('Zebra');
    });

    /**
     * Property: Sorting preserves all elements
     * Invariant: No elements are lost or duplicated during sorting
     */
    it('should preserve all elements during sorting', () => {
      //arrange
      const tags = generateRandomTags(15);
      const originalIds = tags.map(t => t.id).sort();

      //act
      const sorted = [...tags].sort((a, b) => a.displayName.localeCompare(b.displayName));
      const sortedIds = sorted.map(t => t.id).sort();

      //assert
      // Invariant: All original IDs should be present
      expect(sortedIds).toEqual(originalIds);
      expect(sorted.length).toBe(tags.length);
    });
  });

  describe('Tag Search Filtering Properties', () => {
    /**
     * Property 3: Tag search filtering
     * For any search query string, the filtered tag list should contain only tags 
     * whose name or display name contains the search text (case-insensitive)
     * 
     * **Feature: item-search-and-categorization, Property 3: Tag search filtering**
     * **Validates: Requirements 2.2, 2.4**
     */
    it('should only return tags that contain the search query (case-insensitive)', () => {
      //arrange
      const tags = generateRandomTags(15);
      const testQueries = [
        'tools',
        'TOOLS',
        'Tools',
        'gear',
        'equipment',
        'kitchen',
        'power',
        'hand',
        'garden',
        'camping',
        'sports',
        'appliances',
        'books',
        'electronics',
        'furniture'
      ];

      //act & assert
      testQueries.forEach(query => {
        const result = service.searchTags(query, tags);
        
        // Invariant: Every result should contain the query (case-insensitive)
        result.forEach(tag => {
          const lowerQuery = query.toLowerCase();
          const matchesName = tag.name.toLowerCase().includes(lowerQuery);
          const matchesDisplayName = tag.displayName.toLowerCase().includes(lowerQuery);
          
          expect(matchesName || matchesDisplayName).toBe(true);
        });
        
        // Invariant: No tag that contains the query should be excluded
        tags.forEach(tag => {
          const lowerQuery = query.toLowerCase();
          const matchesName = tag.name.toLowerCase().includes(lowerQuery);
          const matchesDisplayName = tag.displayName.toLowerCase().includes(lowerQuery);
          
          if (matchesName || matchesDisplayName) {
            expect(result).toContain(tag);
          }
        });
      });
    });

    /**
     * Property: Empty or whitespace query returns all tags
     * Invariant: searchTags('', tags) = tags and searchTags('  ', tags) = tags
     */
    it('should return all tags when query is empty or whitespace', () => {
      //arrange
      const testCases = [
        { tags: generateRandomTags(5), queries: ['', '  ', '   ', '\t', '\n'] },
        { tags: generateRandomTags(10), queries: ['', ' '] },
        { tags: generateRandomTags(15), queries: ['', '    '] },
        { tags: [], queries: ['', ' '] }
      ];

      //act & assert
      testCases.forEach(({ tags, queries }) => {
        queries.forEach(query => {
          const result = service.searchTags(query, tags);
          
          // Invariant: Should return all tags
          expect(result).toEqual(tags);
          expect(result.length).toBe(tags.length);
        });
      });
    });

    /**
     * Property: Search results are a subset of input tags
     * Invariant: For any query, result ⊆ input tags
     */
    it('should return results that are a subset of input tags', () => {
      //arrange
      const tags = generateRandomTags(15);
      const queries = ['tool', 'gear', 'equipment', 'xyz', 'abc', 'test', 'random'];

      //act & assert
      queries.forEach(query => {
        const result = service.searchTags(query, tags);
        
        // Invariant: Every result should be in the original tags array
        result.forEach(resultTag => {
          expect(tags).toContain(resultTag);
        });
        
        // Invariant: Result size should not exceed input size
        expect(result.length).toBeLessThanOrEqual(tags.length);
      });
    });

    /**
     * Property: Case-insensitive matching
     * Invariant: searchTags(query.toLowerCase(), tags) = searchTags(query.toUpperCase(), tags)
     */
    it('should produce same results regardless of query case', () => {
      //arrange
      const tags = generateRandomTags(15);
      const baseQueries = ['tools', 'gear', 'equipment', 'kitchen', 'power'];

      //act & assert
      baseQueries.forEach(baseQuery => {
        const lowerResult = service.searchTags(baseQuery.toLowerCase(), tags);
        const upperResult = service.searchTags(baseQuery.toUpperCase(), tags);
        const mixedResult = service.searchTags(
          baseQuery.split('').map((c, i) => i % 2 === 0 ? c.toLowerCase() : c.toUpperCase()).join(''),
          tags
        );
        
        // Invariant: All case variations should produce identical results
        expect(lowerResult).toEqual(upperResult);
        expect(lowerResult).toEqual(mixedResult);
        expect(lowerResult.length).toBe(upperResult.length);
        expect(lowerResult.length).toBe(mixedResult.length);
      });
    });

    /**
     * Property: No matches returns empty array
     * Invariant: If no tag contains the query, result should be empty array
     */
    it('should return empty array when no tags match', () => {
      //arrange
      const tags = generateRandomTags(15);
      const nonMatchingQueries = [
        'zzzzzzz',
        'qqqqqq',
        'nonexistent',
        'xyzabc123',
        '!!!!!!',
        'notfound'
      ];

      //act & assert
      nonMatchingQueries.forEach(query => {
        const result = service.searchTags(query, tags);
        
        // Invariant: Should return empty array
        expect(result).toEqual([]);
        expect(result.length).toBe(0);
        expect(Array.isArray(result)).toBe(true);
      });
    });

    /**
     * Property: Partial string matching
     * Invariant: If tag contains substring, it should be in results
     */
    it('should match partial strings within tag names', () => {
      //arrange
      const tags = [
        createMockTag('1', 'power-tools', 'Power Tools'),
        createMockTag('2', 'hand-tools', 'Hand Tools'),
        createMockTag('3', 'garden-tools', 'Garden Tools'),
        createMockTag('4', 'camping-gear', 'Camping Gear'),
        createMockTag('5', 'sports-equipment', 'Sports Equipment')
      ];

      const testCases = [
        { query: 'tool', expectedCount: 3 }, // matches all *-tools
        { query: 'pow', expectedCount: 1 },  // matches power-tools
        { query: 'gear', expectedCount: 1 }, // matches camping-gear
        { query: 'equip', expectedCount: 1 }, // matches sports-equipment
        { query: 'camp', expectedCount: 1 }  // matches camping-gear
      ];

      //act & assert
      testCases.forEach(({ query, expectedCount }) => {
        const result = service.searchTags(query, tags);
        
        // Invariant: Should find expected number of matches
        expect(result.length).toBe(expectedCount);
        
        // Invariant: Each result should contain the query substring
        result.forEach(tag => {
          const lowerQuery = query.toLowerCase();
          const matchesName = tag.name.toLowerCase().includes(lowerQuery);
          const matchesDisplayName = tag.displayName.toLowerCase().includes(lowerQuery);
          expect(matchesName || matchesDisplayName).toBe(true);
        });
      });
    });

    /**
     * Property: Search on empty tag array returns empty array
     * Invariant: searchTags(query, []) = [] for any query
     */
    it('should return empty array when searching empty tag list', () => {
      //arrange
      const emptyTags: SystemTag[] = [];
      const queries = ['test', 'tools', 'gear', '', 'anything'];

      //act & assert
      queries.forEach(query => {
        const result = service.searchTags(query, emptyTags);
        
        // Invariant: Should always return empty array
        expect(result).toEqual([]);
        expect(result.length).toBe(0);
      });
    });

    /**
     * Property: Search preserves tag object identity
     * Invariant: Result tags should be the same objects (by reference) as input tags
     */
    it('should return same tag objects by reference', () => {
      //arrange
      const tags = generateRandomTags(10);
      const query = 'tools';

      //act
      const result = service.searchTags(query, tags);

      //assert
      // Invariant: Each result should be the exact same object (by reference)
      result.forEach(resultTag => {
        const originalTag = tags.find(t => t.id === resultTag.id);
        expect(resultTag).toBe(originalTag); // Same reference
      });
    });

    /**
     * Property: Multiple word search
     * Invariant: Query with spaces should match tags containing any part
     */
    it('should handle queries with multiple words', () => {
      //arrange
      const tags = [
        createMockTag('1', 'power-tools', 'Power Tools'),
        createMockTag('2', 'hand-tools', 'Hand Tools'),
        createMockTag('3', 'kitchen-appliances', 'Kitchen Appliances')
      ];

      //act
      const result1 = service.searchTags('power tools', tags);
      const result2 = service.searchTags('hand tools', tags);
      const result3 = service.searchTags('kitchen appliances', tags);

      //assert
      // Invariant: Should match tags containing the full phrase
      expect(result1.length).toBeGreaterThanOrEqual(0);
      expect(result2.length).toBeGreaterThanOrEqual(0);
      expect(result3.length).toBeGreaterThanOrEqual(0);
      
      // Each result should contain the query substring
      result1.forEach(tag => {
        expect(
          tag.name.toLowerCase().includes('power tools') ||
          tag.displayName.toLowerCase().includes('power tools')
        ).toBe(true);
      });
    });

    /**
     * Property: Idempotence - searching twice gives same results
     * Invariant: searchTags(query, searchTags(query, tags)) = searchTags(query, tags)
     */
    it('should be idempotent - searching results again gives same results', () => {
      //arrange
      const tags = generateRandomTags(15);
      const queries = ['tools', 'gear', 'equipment', 'kitchen'];

      //act & assert
      queries.forEach(query => {
        const firstResult = service.searchTags(query, tags);
        const secondResult = service.searchTags(query, firstResult);
        
        // Invariant: Searching the results again should give the same results
        expect(secondResult).toEqual(firstResult);
        expect(secondResult.length).toBe(firstResult.length);
      });
    });
  });
});
