import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { Router } from '@angular/router';
import { UserService } from './user.service';
import { AuthService } from './auth.service';
import { UserProfile, PublicProfile } from '../models/auth.interface';

// Mock environment
jest.mock('../../environments/environment', () => ({
  environment: {
    production: false,
    apiUrl: 'http://localhost:8080'
  }
}));

describe('UserService', () => {
  let service: UserService;
  let httpMock: HttpTestingController;
  let authService: jest.Mocked<AuthService>;
  let router: jest.Mocked<Router>;
  const API_URL = 'http://localhost:8080/api/auth';

  const mockUserProfile: UserProfile = {
    id: '123',
    email: 'test@example.com',
    firstName: 'John',
    lastName: 'Doe',
    streetAddress: '123 Main St',
    isEmailVerified: true,
    loopScore: 100,
    badges: []
  };

  const mockPublicProfile: PublicProfile = {
    userId: '456',
    firstName: 'Jane',
    lastName: 'Smith',
    loopScore: 150,
    badges: [],
    scoreHistory: []
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
        UserService,
        { provide: AuthService, useValue: authServiceMock },
        { provide: Router, useValue: routerMock }
      ]
    });

    service = TestBed.inject(UserService);
    httpMock = TestBed.inject(HttpTestingController);
    authService = TestBed.inject(AuthService) as jest.Mocked<AuthService>;
    router = TestBed.inject(Router) as jest.Mocked<Router>;

    // Suppress console.error during tests
    jest.spyOn(console, 'error').mockImplementation(() => {});
  });

  afterEach(() => {
    httpMock.verify();
    jest.restoreAllMocks();
  });

  it('should be created', () => {
    //assert
    expect(service).toBeTruthy();
  });

  describe('getCurrentUser', () => {
    it('should fetch current user successfully', (done) => {
      //arrange
      // (no additional setup needed)

      //act
      service.getCurrentUser().subscribe(user => {
        //assert
        expect(user).toEqual(mockUserProfile);
        done();
      });

      const req = httpMock.expectOne(`${API_URL}/me`);
      expect(req.request.method).toBe('GET');
      req.flush(mockUserProfile);
    });

    it('should handle 401 error and redirect to login', (done) => {
      //arrange
      const errorResponse = { message: 'Unauthorized' };

      //act
      service.getCurrentUser().subscribe({
        next: () => fail('should have failed'),
        error: (error) => {
          //assert
          expect(error.message).toBe('Authentication required. Please log in again.');
          expect(authService.logout).toHaveBeenCalled();
          expect(router.navigate).toHaveBeenCalledWith(['/login']);
          done();
        }
      });

      const req = httpMock.expectOne(`${API_URL}/me`);
      req.flush(errorResponse, { status: 401, statusText: 'Unauthorized' });
    });

    it('should handle 403 error', (done) => {
      //arrange
      const errorResponse = { message: 'Forbidden' };

      //act
      service.getCurrentUser().subscribe({
        next: () => fail('should have failed'),
        error: (error) => {
          //assert
          expect(error.message).toBe('You do not have permission to perform this action.');
          done();
        }
      });

      const req = httpMock.expectOne(`${API_URL}/me`);
      req.flush(errorResponse, { status: 403, statusText: 'Forbidden' });
    });

    it('should handle generic error', (done) => {
      //arrange
      const errorResponse = { message: 'Server error' };

      //act
      service.getCurrentUser().subscribe({
        next: () => fail('should have failed'),
        error: (error) => {
          //assert
          expect(error.message).toBe('Server error');
          done();
        }
      });

      const req = httpMock.expectOne(`${API_URL}/me`);
      req.flush(errorResponse, { status: 500, statusText: 'Internal Server Error' });
    });
  });

  describe('getUserById', () => {
    it('should fetch user by id successfully', (done) => {
      //arrange
      const userId = '123';

      //act
      service.getUserById(userId).subscribe(user => {
        //assert
        expect(user).toEqual(mockUserProfile);
        done();
      });

      const req = httpMock.expectOne(`${API_URL}/users/${userId}`);
      expect(req.request.method).toBe('GET');
      req.flush(mockUserProfile);
    });

    it('should handle 404 error', (done) => {
      //arrange
      const userId = 'nonexistent';
      const errorResponse = { message: 'User not found' };

      //act
      service.getUserById(userId).subscribe({
        next: () => fail('should have failed'),
        error: (error) => {
          //assert
          expect(error.message).toBe('User not found.');
          done();
        }
      });

      const req = httpMock.expectOne(`${API_URL}/users/${userId}`);
      req.flush(errorResponse, { status: 404, statusText: 'Not Found' });
    });

    it('should handle 401 error and redirect to login', (done) => {
      //arrange
      const userId = '123';
      const errorResponse = { message: 'Unauthorized' };

      //act
      service.getUserById(userId).subscribe({
        next: () => fail('should have failed'),
        error: (error) => {
          //assert
          expect(error.message).toBe('Authentication required. Please log in again.');
          expect(authService.logout).toHaveBeenCalled();
          expect(router.navigate).toHaveBeenCalledWith(['/login']);
          done();
        }
      });

      const req = httpMock.expectOne(`${API_URL}/users/${userId}`);
      req.flush(errorResponse, { status: 401, statusText: 'Unauthorized' });
    });

    it('should handle error without message property', (done) => {
      //arrange
      const userId = '123';

      //act
      service.getUserById(userId).subscribe({
        next: () => fail('should have failed'),
        error: (error) => {
          //assert
          expect(error.message).toContain('500 Internal Server Error');
          done();
        }
      });

      const req = httpMock.expectOne(`${API_URL}/users/${userId}`);
      req.flush(null, { status: 500, statusText: 'Internal Server Error' });
    });
  });

  describe('getPublicProfile', () => {
    it('should fetch public profile successfully', (done) => {
      //arrange
      const userId = '456';

      //act
      service.getPublicProfile(userId).subscribe(profile => {
        //assert
        expect(profile).toEqual(mockPublicProfile);
        expect(profile.userId).toBe('456');
        expect(profile.firstName).toBe('Jane');
        expect(profile.lastName).toBe('Smith');
        done();
      });

      const req = httpMock.expectOne(`http://localhost:8080/api/users/${userId}/public-profile`);
      expect(req.request.method).toBe('GET');
      req.flush(mockPublicProfile);
    });

    it('should handle 404 error when user not found', (done) => {
      //arrange
      const userId = 'nonexistent';
      const errorResponse = { message: 'User not found' };

      //act
      service.getPublicProfile(userId).subscribe({
        next: () => fail('should have failed'),
        error: (error) => {
          //assert
          expect(error.message).toBe('User not found.');
          done();
        }
      });

      const req = httpMock.expectOne(`http://localhost:8080/api/users/${userId}/public-profile`);
      req.flush(errorResponse, { status: 404, statusText: 'Not Found' });
    });

    it('should handle 403 error when users do not share loops', (done) => {
      //arrange
      const userId = '789';
      const errorResponse = { message: 'You can only view profiles of users in your loops' };

      //act
      service.getPublicProfile(userId).subscribe({
        next: () => fail('should have failed'),
        error: (error) => {
          //assert
          expect(error.message).toBe('You do not have permission to perform this action.');
          done();
        }
      });

      const req = httpMock.expectOne(`http://localhost:8080/api/users/${userId}/public-profile`);
      req.flush(errorResponse, { status: 403, statusText: 'Forbidden' });
    });

    it('should handle 401 error and redirect to login', (done) => {
      //arrange
      const userId = '456';
      const errorResponse = { message: 'Unauthorized' };

      //act
      service.getPublicProfile(userId).subscribe({
        next: () => fail('should have failed'),
        error: (error) => {
          //assert
          expect(error.message).toBe('Authentication required. Please log in again.');
          expect(authService.logout).toHaveBeenCalled();
          expect(router.navigate).toHaveBeenCalledWith(['/login']);
          done();
        }
      });

      const req = httpMock.expectOne(`http://localhost:8080/api/users/${userId}/public-profile`);
      req.flush(errorResponse, { status: 401, statusText: 'Unauthorized' });
    });

    it('should handle generic server error', (done) => {
      //arrange
      const userId = '456';
      const errorResponse = { message: 'Internal server error' };

      //act
      service.getPublicProfile(userId).subscribe({
        next: () => fail('should have failed'),
        error: (error) => {
          //assert
          expect(error.message).toBe('Internal server error');
          done();
        }
      });

      const req = httpMock.expectOne(`http://localhost:8080/api/users/${userId}/public-profile`);
      req.flush(errorResponse, { status: 500, statusText: 'Internal Server Error' });
    });
  });
});
