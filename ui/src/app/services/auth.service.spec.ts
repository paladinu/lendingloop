import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { AuthService } from './auth.service';
import { AuthResponse, RegisterRequest, RegisterResponse, UserProfile, VerificationResponse } from '../models/auth.interface';

describe('AuthService', () => {
  let service: AuthService;
  let httpMock: HttpTestingController;
  const API_URL = 'http://localhost:8080/api/auth';

  const mockUserProfile: UserProfile = {
    id: '123',
    email: 'test@example.com',
    firstName: 'John',
    lastName: 'Doe',
    streetAddress: '123 Main St',
    isEmailVerified: true
  };

  const mockAuthResponse: AuthResponse = {
    token: 'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiZXhwIjo5OTk5OTk5OTk5fQ.test',
    user: mockUserProfile,
    expiresAt: '2099-12-31T23:59:59Z'
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [AuthService]
    });
    service = TestBed.inject(AuthService);
    httpMock = TestBed.inject(HttpTestingController);
    localStorage.clear();
  });

  afterEach(() => {
    httpMock.verify();
    localStorage.clear();
  });

  it('should be created', () => {
    // Assert
    expect(service).toBeTruthy();
  });

  describe('login', () => {
    it('should login successfully and store auth data', (done) => {
      // Arrange
      const email = 'test@example.com';
      const password = 'password123';

      // Act
      service.login(email, password).subscribe(response => {
        // Assert
        expect(response).toEqual(mockAuthResponse);
        expect(localStorage.getItem('auth_token')).toBe(mockAuthResponse.token);
        expect(localStorage.getItem('current_user')).toBe(JSON.stringify(mockUserProfile));
        done();
      });

      const req = httpMock.expectOne(`${API_URL}/login`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual({ email, password });
      req.flush(mockAuthResponse);
    });

    it('should handle login error', (done) => {
      // Arrange
      const email = 'test@example.com';
      const password = 'wrongpassword';
      const errorResponse = { message: 'Invalid credentials' };

      // Act
      service.login(email, password).subscribe({
        next: () => fail('should have failed'),
        error: (error) => {
          // Assert
          expect(error.status).toBe(401);
          done();
        }
      });

      const req = httpMock.expectOne(`${API_URL}/login`);
      req.flush(errorResponse, { status: 401, statusText: 'Unauthorized' });
    });
  });

  describe('register', () => {
    it('should register successfully', (done) => {
      // Arrange
      const registerRequest: RegisterRequest = {
        email: 'newuser@example.com',
        password: 'password123',
        firstName: 'Jane',
        lastName: 'Smith',
        streetAddress: '456 Oak Ave'
      };

      const mockRegisterResponse: RegisterResponse = {
        message: 'Registration successful',
        user: { ...mockUserProfile, email: registerRequest.email }
      };

      // Act
      service.register(registerRequest).subscribe(response => {
        // Assert
        expect(response).toEqual(mockRegisterResponse);
        done();
      });

      const req = httpMock.expectOne(`${API_URL}/register`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual(registerRequest);
      req.flush(mockRegisterResponse);
    });

    it('should handle registration error', (done) => {
      // Arrange
      const registerRequest: RegisterRequest = {
        email: 'existing@example.com',
        password: 'password123',
        firstName: 'Jane',
        lastName: 'Smith',
        streetAddress: '456 Oak Ave'
      };

      // Act
      service.register(registerRequest).subscribe({
        next: () => fail('should have failed'),
        error: (error) => {
          // Assert
          expect(error.status).toBe(400);
          done();
        }
      });

      const req = httpMock.expectOne(`${API_URL}/register`);
      req.flush({ message: 'Email already exists' }, { status: 400, statusText: 'Bad Request' });
    });
  });

  describe('logout', () => {
    it('should logout successfully and clear auth data', () => {
      // Arrange
      localStorage.setItem('auth_token', mockAuthResponse.token);
      localStorage.setItem('current_user', JSON.stringify(mockUserProfile));

      // Act
      service.logout();

      const req = httpMock.expectOne(`${API_URL}/logout`);
      expect(req.request.method).toBe('POST');
      req.flush({});

      // Assert
      expect(localStorage.getItem('auth_token')).toBeNull();
      expect(localStorage.getItem('current_user')).toBeNull();
    });

    it('should clear auth data even if logout request fails', () => {
      // Arrange
      localStorage.setItem('auth_token', mockAuthResponse.token);
      localStorage.setItem('current_user', JSON.stringify(mockUserProfile));

      // Act
      service.logout();

      const req = httpMock.expectOne(`${API_URL}/logout`);
      req.flush({}, { status: 500, statusText: 'Server Error' });

      // Assert
      expect(localStorage.getItem('auth_token')).toBeNull();
      expect(localStorage.getItem('current_user')).toBeNull();
    });
  });

  describe('getCurrentUser', () => {
    it('should return current user observable', (done) => {
      // Arrange
      service['currentUserSubject'].next(mockUserProfile);

      // Act
      service.getCurrentUser().subscribe(user => {
        // Assert
        expect(user).toEqual(mockUserProfile);
        done();
      });
    });
  });

  describe('isAuthenticated', () => {
    it('should return true for valid token', () => {
      // Arrange
      const futureExp = Math.floor(Date.now() / 1000) + 3600;
      const validToken = `header.${btoa(JSON.stringify({ exp: futureExp }))}.signature`;
      localStorage.setItem('auth_token', validToken);

      // Act
      const result = service.isAuthenticated();

      // Assert
      expect(result).toBe(true);
    });

    it('should return false for expired token', () => {
      // Arrange
      const pastExp = Math.floor(Date.now() / 1000) - 3600;
      const expiredToken = `header.${btoa(JSON.stringify({ exp: pastExp }))}.signature`;
      localStorage.setItem('auth_token', expiredToken);

      // Act
      const result = service.isAuthenticated();

      // Assert
      expect(result).toBe(false);
    });

    it('should return false when no token exists', () => {
      // Arrange
      localStorage.removeItem('auth_token');

      // Act
      const result = service.isAuthenticated();

      // Assert
      expect(result).toBe(false);
    });

    it('should return false for invalid token format', () => {
      // Arrange
      localStorage.setItem('auth_token', 'invalid-token');

      // Act
      const result = service.isAuthenticated();

      // Assert
      expect(result).toBe(false);
    });
  });

  describe('verifyEmail', () => {
    it('should verify email successfully', (done) => {
      // Arrange
      const token = 'verification-token-123';
      const mockResponse: VerificationResponse = {
        message: 'Email verified successfully',
        success: true
      };

      // Act
      service.verifyEmail(token).subscribe(response => {
        // Assert
        expect(response).toEqual(mockResponse);
        done();
      });

      const req = httpMock.expectOne(`${API_URL}/verify-email`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual({ token });
      req.flush(mockResponse);
    });

    it('should handle verification error', (done) => {
      // Arrange
      const token = 'invalid-token';

      // Act
      service.verifyEmail(token).subscribe({
        next: () => fail('should have failed'),
        error: (error) => {
          // Assert
          expect(error.status).toBe(400);
          done();
        }
      });

      const req = httpMock.expectOne(`${API_URL}/verify-email`);
      req.flush({ message: 'Invalid token' }, { status: 400, statusText: 'Bad Request' });
    });
  });

  describe('resendVerificationEmail', () => {
    it('should resend verification email successfully', (done) => {
      // Arrange
      const email = 'test@example.com';
      const mockResponse = { message: 'Verification email sent' };

      // Act
      service.resendVerificationEmail(email).subscribe(response => {
        // Assert
        expect(response).toEqual(mockResponse);
        done();
      });

      const req = httpMock.expectOne(`${API_URL}/resend-verification`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual({ email });
      req.flush(mockResponse);
    });

    it('should handle resend error', (done) => {
      // Arrange
      const email = 'nonexistent@example.com';

      // Act
      service.resendVerificationEmail(email).subscribe({
        next: () => fail('should have failed'),
        error: (error) => {
          // Assert
          expect(error.status).toBe(404);
          done();
        }
      });

      const req = httpMock.expectOne(`${API_URL}/resend-verification`);
      req.flush({ message: 'User not found' }, { status: 404, statusText: 'Not Found' });
    });
  });

  describe('getToken', () => {
    it('should return token from localStorage', () => {
      // Arrange
      const token = 'test-token-123';
      localStorage.setItem('auth_token', token);

      // Act
      const result = service.getToken();

      // Assert
      expect(result).toBe(token);
    });

    it('should return null when no token exists', () => {
      // Arrange
      localStorage.removeItem('auth_token');

      // Act
      const result = service.getToken();

      // Assert
      expect(result).toBeNull();
    });
  });
});
