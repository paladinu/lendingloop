import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { Router } from '@angular/router';
import { ItemsService } from './items.service';
import { AuthService } from './auth.service';
import { SharedItem } from '../models/shared-item.interface';

describe('ItemsService', () => {
  let service: ItemsService;
  let httpMock: HttpTestingController;
  let authService: jest.Mocked<AuthService>;
  let router: jest.Mocked<Router>;
  const API_URL = 'http://localhost:8080/api/items';

  const mockItem: SharedItem = {
    id: '1',
    name: 'Test Item',
    description: 'Test Description',
    userId: 'user123',
    isAvailable: true,
    imageUrl: 'http://example.com/image.jpg',
    visibleToLoopIds: ['loop1', 'loop2'],
    visibleToAllLoops: false,
    visibleToFutureLoops: false,
    createdAt: new Date('2024-01-01'),
    updatedAt: new Date('2024-01-02'),
    ownerName: 'John Doe'
  };

  beforeEach(() => {
    const authServiceMock = {
      logout: jest.fn()
    } as unknown as jest.Mocked<AuthService>;

    const routerMock = {
      navigate: jest.fn()
    } as unknown as jest.Mocked<Router>;

    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [
        ItemsService,
        { provide: AuthService, useValue: authServiceMock },
        { provide: Router, useValue: routerMock }
      ]
    });

    service = TestBed.inject(ItemsService);
    httpMock = TestBed.inject(HttpTestingController);
    authService = TestBed.inject(AuthService) as jest.Mocked<AuthService>;
    router = TestBed.inject(Router) as jest.Mocked<Router>;
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    //assert
    expect(service).toBeTruthy();
  });

  describe('getItems', () => {
    it('should fetch items successfully', (done) => {
      //arrange
      const mockItems: SharedItem[] = [mockItem];

      //act
      service.getItems().subscribe(items => {
        //assert
        expect(items).toEqual(mockItems);
        expect(items.length).toBe(1);
        done();
      });

      const req = httpMock.expectOne(API_URL);
      expect(req.request.method).toBe('GET');
      req.flush(mockItems);
    });

    it('should handle 401 error and redirect to login', (done) => {
      //arrange
      const errorResponse = { message: 'Unauthorized' };
      const consoleErrorSpy = jest.spyOn(console, 'error').mockImplementation(() => {});

      //act
      service.getItems().subscribe({
        next: () => fail('should have failed'),
        error: (error) => {
          //assert
          expect(error.message).toBe('Authentication required. Please log in again.');
          expect(authService.logout).toHaveBeenCalled();
          expect(router.navigate).toHaveBeenCalledWith(['/login']);
          consoleErrorSpy.mockRestore();
          done();
        }
      });

      const req = httpMock.expectOne(API_URL);
      req.flush(errorResponse, { status: 401, statusText: 'Unauthorized' });
    });

    it('should handle 403 error', (done) => {
      //arrange
      const errorResponse = { message: 'Forbidden' };
      const consoleErrorSpy = jest.spyOn(console, 'error').mockImplementation(() => {});

      //act
      service.getItems().subscribe({
        next: () => fail('should have failed'),
        error: (error) => {
          //assert
          expect(error.message).toBe('You do not have permission to perform this action.');
          consoleErrorSpy.mockRestore();
          done();
        }
      });

      const req = httpMock.expectOne(API_URL);
      req.flush(errorResponse, { status: 403, statusText: 'Forbidden' });
    });
  });

  describe('createItem', () => {
    it('should create item successfully', (done) => {
      //arrange
      const newItem: Partial<SharedItem> = {
        name: 'New Item',
        description: 'New Description',
        isAvailable: true,
        visibleToLoopIds: [],
        visibleToAllLoops: false,
        visibleToFutureLoops: false
      };

      //act
      service.createItem(newItem).subscribe(item => {
        //assert
        expect(item).toEqual(mockItem);
        done();
      });

      const req = httpMock.expectOne(API_URL);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual(newItem);
      req.flush(mockItem);
    });

    it('should handle create error', (done) => {
      //arrange
      const newItem: Partial<SharedItem> = {
        name: 'New Item',
        description: 'New Description'
      };
      const consoleErrorSpy = jest.spyOn(console, 'error').mockImplementation(() => {});

      //act
      service.createItem(newItem).subscribe({
        next: () => fail('should have failed'),
        error: (error) => {
          //assert
          expect(error.message).toBeTruthy();
          consoleErrorSpy.mockRestore();
          done();
        }
      });

      const req = httpMock.expectOne(API_URL);
      req.flush({ message: 'Validation error' }, { status: 400, statusText: 'Bad Request' });
    });
  });

  describe('uploadItemImage', () => {
    it('should upload image successfully', (done) => {
      //arrange
      const itemId = '1';
      const mockFile = new File(['image content'], 'test.jpg', { type: 'image/jpeg' });

      //act
      service.uploadItemImage(itemId, mockFile).subscribe(item => {
        //assert
        expect(item).toEqual(mockItem);
        done();
      });

      const req = httpMock.expectOne(`${API_URL}/${itemId}/image`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body instanceof FormData).toBe(true);
      req.flush(mockItem);
    });

    it('should handle upload error', (done) => {
      //arrange
      const itemId = '1';
      const mockFile = new File(['image content'], 'test.jpg', { type: 'image/jpeg' });
      const consoleErrorSpy = jest.spyOn(console, 'error').mockImplementation(() => {});

      //act
      service.uploadItemImage(itemId, mockFile).subscribe({
        next: () => fail('should have failed'),
        error: (error) => {
          //assert
          expect(error.message).toBeTruthy();
          consoleErrorSpy.mockRestore();
          done();
        }
      });

      const req = httpMock.expectOne(`${API_URL}/${itemId}/image`);
      req.flush({ message: 'Upload failed' }, { status: 500, statusText: 'Server Error' });
    });
  });

  describe('getItemById', () => {
    it('should fetch item by id successfully', (done) => {
      //arrange
      const itemId = '1';

      //act
      service.getItemById(itemId).subscribe(item => {
        //assert
        expect(item).toEqual(mockItem);
        done();
      });

      const req = httpMock.expectOne(`${API_URL}/${itemId}`);
      expect(req.request.method).toBe('GET');
      req.flush(mockItem);
    });

    it('should handle not found error', (done) => {
      //arrange
      const itemId = 'nonexistent';
      const consoleErrorSpy = jest.spyOn(console, 'error').mockImplementation(() => {});

      //act
      service.getItemById(itemId).subscribe({
        next: () => fail('should have failed'),
        error: (error) => {
          //assert
          expect(error.message).toBeTruthy();
          consoleErrorSpy.mockRestore();
          done();
        }
      });

      const req = httpMock.expectOne(`${API_URL}/${itemId}`);
      req.flush({ message: 'Item not found' }, { status: 404, statusText: 'Not Found' });
    });
  });

  describe('updateItemVisibility', () => {
    it('should update visibility successfully', (done) => {
      //arrange
      const itemId = '1';
      const visibleToLoopIds = ['loop1', 'loop2'];
      const visibleToAllLoops = true;
      const visibleToFutureLoops = true;

      //act
      service.updateItemVisibility(itemId, visibleToLoopIds, visibleToAllLoops, visibleToFutureLoops)
        .subscribe(item => {
          //assert
          expect(item).toEqual(mockItem);
          done();
        });

      const req = httpMock.expectOne(`${API_URL}/${itemId}/visibility`);
      expect(req.request.method).toBe('PUT');
      expect(req.request.body).toEqual({
        visibleToLoopIds,
        visibleToAllLoops,
        visibleToFutureLoops
      });
      req.flush(mockItem);
    });

    it('should handle update error', (done) => {
      //arrange
      const itemId = '1';
      const visibleToLoopIds = ['loop1'];
      const visibleToAllLoops = false;
      const visibleToFutureLoops = false;
      const consoleErrorSpy = jest.spyOn(console, 'error').mockImplementation(() => {});

      //act
      service.updateItemVisibility(itemId, visibleToLoopIds, visibleToAllLoops, visibleToFutureLoops)
        .subscribe({
          next: () => fail('should have failed'),
          error: (error) => {
            //assert
            expect(error.message).toBeTruthy();
            consoleErrorSpy.mockRestore();
            done();
          }
        });

      const req = httpMock.expectOne(`${API_URL}/${itemId}/visibility`);
      req.flush({ message: 'Update failed' }, { status: 400, statusText: 'Bad Request' });
    });
  });

  describe('updateItem', () => {
    it('should update item successfully', (done) => {
      //arrange
      const itemId = '1';
      const updates: Partial<SharedItem> = {
        name: 'Updated Name',
        description: 'Updated Description',
        isAvailable: false,
        visibleToLoopIds: ['loop1'],
        visibleToAllLoops: true,
        visibleToFutureLoops: true
      };

      //act
      service.updateItem(itemId, updates).subscribe(item => {
        //assert
        expect(item).toEqual(mockItem);
        done();
      });

      const req = httpMock.expectOne(`${API_URL}/${itemId}`);
      expect(req.request.method).toBe('PUT');
      expect(req.request.body).toEqual(updates);
      req.flush(mockItem);
    });

    it('should handle 401 error and redirect to login', (done) => {
      //arrange
      const itemId = '1';
      const updates: Partial<SharedItem> = { name: 'Updated Name' };
      const consoleErrorSpy = jest.spyOn(console, 'error').mockImplementation(() => {});

      //act
      service.updateItem(itemId, updates).subscribe({
        next: () => fail('should have failed'),
        error: (error) => {
          //assert
          expect(error.message).toBe('Authentication required. Please log in again.');
          expect(authService.logout).toHaveBeenCalled();
          expect(router.navigate).toHaveBeenCalledWith(['/login']);
          consoleErrorSpy.mockRestore();
          done();
        }
      });

      const req = httpMock.expectOne(`${API_URL}/${itemId}`);
      req.flush({ message: 'Unauthorized' }, { status: 401, statusText: 'Unauthorized' });
    });

    it('should handle 403 forbidden error', (done) => {
      //arrange
      const itemId = '1';
      const updates: Partial<SharedItem> = { name: 'Updated Name' };
      const consoleErrorSpy = jest.spyOn(console, 'error').mockImplementation(() => {});

      //act
      service.updateItem(itemId, updates).subscribe({
        next: () => fail('should have failed'),
        error: (error) => {
          //assert
          expect(error.message).toBe('You do not have permission to perform this action.');
          consoleErrorSpy.mockRestore();
          done();
        }
      });

      const req = httpMock.expectOne(`${API_URL}/${itemId}`);
      req.flush({ message: 'Forbidden' }, { status: 403, statusText: 'Forbidden' });
    });

    it('should handle 404 not found error', (done) => {
      //arrange
      const itemId = 'nonexistent';
      const updates: Partial<SharedItem> = { name: 'Updated Name' };
      const consoleErrorSpy = jest.spyOn(console, 'error').mockImplementation(() => {});

      //act
      service.updateItem(itemId, updates).subscribe({
        next: () => fail('should have failed'),
        error: (error) => {
          //assert
          expect(error.message).toBeTruthy();
          consoleErrorSpy.mockRestore();
          done();
        }
      });

      const req = httpMock.expectOne(`${API_URL}/${itemId}`);
      req.flush({ message: 'Item not found' }, { status: 404, statusText: 'Not Found' });
    });

    it('should handle network error', (done) => {
      //arrange
      const itemId = '1';
      const updates: Partial<SharedItem> = { name: 'Updated Name' };
      const consoleErrorSpy = jest.spyOn(console, 'error').mockImplementation(() => {});

      //act
      service.updateItem(itemId, updates).subscribe({
        next: () => fail('should have failed'),
        error: (error) => {
          //assert
          expect(error.message).toBeTruthy();
          consoleErrorSpy.mockRestore();
          done();
        }
      });

      const req = httpMock.expectOne(`${API_URL}/${itemId}`);
      req.flush({ message: 'Server error' }, { status: 500, statusText: 'Internal Server Error' });
    });
  });
});
