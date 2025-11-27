import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { TagsService } from './tags.service';
import { SystemTag } from '../models/system-tag.interface';

// Mock environment
jest.mock('../../environments/environment', () => ({
  environment: {
    production: false,
    apiUrl: 'http://localhost:8080'
  }
}));

describe('TagsService', () => {
  let service: TagsService;
  let httpMock: HttpTestingController;
  const API_URL = 'http://localhost:8080/api/tags';

  const mockTags: SystemTag[] = [
    {
      id: '1',
      name: 'power-tools',
      displayName: 'Power Tools',
      category: 'Tools & Equipment',
      usageCount: 5,
      isActive: true
    },
    {
      id: '2',
      name: 'camping-gear',
      displayName: 'Camping Gear',
      category: 'Sports & Recreation',
      usageCount: 3,
      isActive: true
    },
    {
      id: '3',
      name: 'kitchen-appliances',
      displayName: 'Kitchen Appliances',
      category: 'Kitchen & Appliances',
      usageCount: 8,
      isActive: true
    }
  ];

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [TagsService]
    });

    service = TestBed.inject(TagsService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
    service.clearCache(); // Clear cache between tests
  });

  it('should be created', () => {
    //assert
    expect(service).toBeTruthy();
  });

  describe('getAllTags', () => {
    it('should fetch tags successfully and sort alphabetically', (done) => {
      //arrange
      const unsortedTags: SystemTag[] = [
        { ...mockTags[2] }, // Kitchen Appliances
        { ...mockTags[0] }, // Power Tools
        { ...mockTags[1] }  // Camping Gear
      ];

      //act
      service.getAllTags().subscribe(tags => {
        //assert
        expect(tags).toBeDefined();
        expect(tags.length).toBe(3);
        // Should be sorted alphabetically by displayName
        expect(tags[0].displayName).toBe('Camping Gear');
        expect(tags[1].displayName).toBe('Kitchen Appliances');
        expect(tags[2].displayName).toBe('Power Tools');
        done();
      });

      const req = httpMock.expectOne(API_URL);
      expect(req.request.method).toBe('GET');
      req.flush(unsortedTags);
    });

    it('should cache tags after first fetch', (done) => {
      //arrange
      const tags = [...mockTags];

      //act - first call
      service.getAllTags().subscribe(firstResult => {
        //assert - first call
        expect(firstResult).toEqual(tags.sort((a, b) => a.displayName.localeCompare(b.displayName)));

        // Second call should use cache (no HTTP request)
        service.getAllTags().subscribe(secondResult => {
          //assert - second call
          expect(secondResult).toEqual(firstResult);
          done();
        });
      });

      const req = httpMock.expectOne(API_URL);
      req.flush(tags);
      // No second HTTP request should be made
    });

    it('should handle empty tag list', (done) => {
      //arrange
      const emptyTags: SystemTag[] = [];

      //act
      service.getAllTags().subscribe(tags => {
        //assert
        expect(tags).toEqual([]);
        expect(tags.length).toBe(0);
        done();
      });

      const req = httpMock.expectOne(API_URL);
      expect(req.request.method).toBe('GET');
      req.flush(emptyTags);
    });

    it('should handle error and throw error', (done) => {
      //arrange
      const consoleErrorSpy = jest.spyOn(console, 'error').mockImplementation(() => {});

      //act
      service.getAllTags().subscribe({
        next: () => {
          fail('Should have thrown an error');
          done();
        },
        error: (error) => {
          //assert
          expect(error).toBeDefined();
          expect(consoleErrorSpy).toHaveBeenCalled();
          consoleErrorSpy.mockRestore();
          done();
        }
      });

      const req = httpMock.expectOne(API_URL);
      req.flush({ message: 'Server error' }, { status: 500, statusText: 'Internal Server Error' });
    });

    it('should handle network error gracefully', (done) => {
      //arrange
      const consoleErrorSpy = jest.spyOn(console, 'error').mockImplementation(() => {});

      //act
      service.getAllTags().subscribe({
        next: () => {
          fail('Should have thrown an error');
          done();
        },
        error: (error) => {
          //assert
          expect(error).toBeDefined();
          expect(consoleErrorSpy).toHaveBeenCalled();
          consoleErrorSpy.mockRestore();
          done();
        }
      });

      const req = httpMock.expectOne(API_URL);
      req.error(new ProgressEvent('error'));
    });
  });

  describe('getTagStatistics', () => {
    it('should fetch tag statistics successfully', (done) => {
      //arrange
      const mockStats = {
        'power-tools': 5,
        'camping-gear': 3,
        'kitchen-appliances': 8
      };

      //act
      service.getTagStatistics().subscribe(stats => {
        //assert
        expect(stats).toBeInstanceOf(Map);
        expect(stats.size).toBe(3);
        expect(stats.get('power-tools')).toBe(5);
        expect(stats.get('camping-gear')).toBe(3);
        expect(stats.get('kitchen-appliances')).toBe(8);
        done();
      });

      const req = httpMock.expectOne(`${API_URL}/statistics`);
      expect(req.request.method).toBe('GET');
      req.flush(mockStats);
    });

    it('should handle empty statistics', (done) => {
      //arrange
      const emptyStats = {};

      //act
      service.getTagStatistics().subscribe(stats => {
        //assert
        expect(stats).toBeInstanceOf(Map);
        expect(stats.size).toBe(0);
        done();
      });

      const req = httpMock.expectOne(`${API_URL}/statistics`);
      req.flush(emptyStats);
    });

    it('should handle error and throw error', (done) => {
      //arrange
      const consoleErrorSpy = jest.spyOn(console, 'error').mockImplementation(() => {});

      //act
      service.getTagStatistics().subscribe({
        next: () => {
          fail('Should have thrown an error');
          done();
        },
        error: (error) => {
          //assert
          expect(error).toBeDefined();
          expect(consoleErrorSpy).toHaveBeenCalled();
          consoleErrorSpy.mockRestore();
          done();
        }
      });

      const req = httpMock.expectOne(`${API_URL}/statistics`);
      req.flush({ message: 'Server error' }, { status: 500, statusText: 'Internal Server Error' });
    });
  });

  describe('searchTags', () => {
    it('should filter tags by name (case-insensitive)', () => {
      //arrange
      const tags = [...mockTags];
      const query = 'power';

      //act
      const result = service.searchTags(query, tags);

      //assert
      expect(result.length).toBe(1);
      expect(result[0].name).toBe('power-tools');
    });

    it('should filter tags by displayName (case-insensitive)', () => {
      //arrange
      const tags = [...mockTags];
      const query = 'camping';

      //act
      const result = service.searchTags(query, tags);

      //assert
      expect(result.length).toBe(1);
      expect(result[0].displayName).toBe('Camping Gear');
    });

    it('should be case-insensitive', () => {
      //arrange
      const tags = [...mockTags];
      const queries = ['POWER', 'Power', 'power', 'PoWeR'];

      //act & assert
      queries.forEach(query => {
        const result = service.searchTags(query, tags);
        expect(result.length).toBe(1);
        expect(result[0].name).toBe('power-tools');
      });
    });

    it('should return all tags when query is empty', () => {
      //arrange
      const tags = [...mockTags];
      const query = '';

      //act
      const result = service.searchTags(query, tags);

      //assert
      expect(result).toEqual(tags);
      expect(result.length).toBe(3);
    });

    it('should return all tags when query is whitespace', () => {
      //arrange
      const tags = [...mockTags];
      const query = '   ';

      //act
      const result = service.searchTags(query, tags);

      //assert
      expect(result).toEqual(tags);
      expect(result.length).toBe(3);
    });

    it('should return empty array when no tags match', () => {
      //arrange
      const tags = [...mockTags];
      const query = 'nonexistent';

      //act
      const result = service.searchTags(query, tags);

      //assert
      expect(result).toEqual([]);
      expect(result.length).toBe(0);
    });

    it('should match partial strings', () => {
      //arrange
      const tags = [...mockTags];
      const query = 'gear';

      //act
      const result = service.searchTags(query, tags);

      //assert
      expect(result.length).toBe(1);
      expect(result[0].name).toBe('camping-gear');
    });

    it('should return multiple matches', () => {
      //arrange
      const tags = [
        ...mockTags,
        {
          id: '4',
          name: 'hand-tools',
          displayName: 'Hand Tools',
          category: 'Tools & Equipment',
          usageCount: 2,
          isActive: true
        }
      ];
      const query = 'tools';

      //act
      const result = service.searchTags(query, tags);

      //assert
      expect(result.length).toBe(2);
      expect(result.some(t => t.name === 'power-tools')).toBe(true);
      expect(result.some(t => t.name === 'hand-tools')).toBe(true);
    });

    it('should handle empty tag array', () => {
      //arrange
      const tags: SystemTag[] = [];
      const query = 'test';

      //act
      const result = service.searchTags(query, tags);

      //assert
      expect(result).toEqual([]);
    });
  });

  describe('clearCache', () => {
    it('should clear cached tags', (done) => {
      //arrange
      const tags = [...mockTags];

      //act - first call to populate cache
      service.getAllTags().subscribe(() => {
        // Clear the cache
        service.clearCache();

        // Second call should make a new HTTP request
        service.getAllTags().subscribe(result => {
          //assert
          expect(result).toBeDefined();
          done();
        });

        const req = httpMock.expectOne(API_URL);
        req.flush(tags);
      });

      const firstReq = httpMock.expectOne(API_URL);
      firstReq.flush(tags);
    });
  });
});
