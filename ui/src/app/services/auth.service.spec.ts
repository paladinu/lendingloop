import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { Router } from '@angular/router';
import { AuthService } from './auth.service';

// Mock environment
jest.mock('../../environments/environment', () => ({
  environment: {
    production: false,
    apiUrl: 'http://localhost:8080'
  }
}));

describe('AuthService', () => {
  let service: AuthService;
  let httpMock: HttpTestingController;
  let mockRouter: jest.Mocked<Router>;

  beforeEach(() => {
    // Clear localStorage and suppress console errors before each test
    localStorage.clear();
    jest.spyOn(console, 'error').mockImplementation(() => {});

    mockRouter = {
      navigate: jest.fn(),
    } as any;

    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        AuthService,
        { provide: Router, useValue: mockRouter }
      ]
    });

    service = TestBed.inject(AuthService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
    localStorage.clear();
    jest.restoreAllMocks();
  });

  it('should be created', () => {
    //assert
    expect(service).toBeTruthy();
  });

  it('should login successfully', (done) => {
    //arrange
    const mockResponse = { token: 'test-token', userId: 'user1' };
    const credentials = { email: 'test@example.com', password: 'password' };

    //act
    service.login(credentials.email, credentials.password).subscribe({
      next: (response) => {
        //assert
        expect(response).toEqual(mockResponse);
        expect(localStorage.getItem('auth_token')).toBe('test-token');
        done();
      }
    });

    const req = httpMock.expectOne('http://localhost:8080/api/auth/login');
    expect(req.request.method).toBe('POST');
    req.flush(mockResponse);
  });

  it('should register successfully', (done) => {
    //arrange
    const mockResponse = { message: 'Registration successful' };
    const userData = {
      email: 'test@example.com',
      password: 'password',
      firstName: 'John',
      lastName: 'Doe',
      streetAddress: '123 Main St'
    };

    //act
    service.register(userData).subscribe({
      next: (response) => {
        //assert
        expect(response).toEqual(mockResponse);
        done();
      }
    });

    const req = httpMock.expectOne('http://localhost:8080/api/auth/register');
    expect(req.request.method).toBe('POST');
    req.flush(mockResponse);
  });

  it('should logout and clear token', (done) => {
    //arrange
    localStorage.setItem('auth_token', 'test-token');

    //act
    service.logout();

    //assert
    const req = httpMock.expectOne('http://localhost:8080/api/auth/logout');
    expect(req.request.method).toBe('POST');
    req.flush({});
    
    setTimeout(() => {
      expect(localStorage.getItem('auth_token')).toBeNull();
      done();
    }, 0);
  });

  it('should return true when user is authenticated', () => {
    //arrange
    const futureTime = Math.floor(Date.now() / 1000) + 3600;
    const token = `header.${btoa(JSON.stringify({ exp: futureTime }))}.signature`;
    localStorage.setItem('auth_token', token);

    //act
    const result = service.isAuthenticated();

    //assert
    expect(result).toBe(true);
  });

  it('should return false when user is not authenticated', () => {
    //arrange
    localStorage.removeItem('auth_token');

    //act
    const result = service.isAuthenticated();

    //assert
    expect(result).toBe(false);
  });
});
