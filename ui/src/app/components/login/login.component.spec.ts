import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ReactiveFormsModule } from '@angular/forms';
import { Router, ActivatedRoute, provideRouter } from '@angular/router';
import { of, throwError, delay } from 'rxjs';
import { LoginComponent } from './login.component';
import { AuthService } from '../../services/auth.service';
import { AuthResponse, UserProfile } from '../../models/auth.interface';

describe('LoginComponent', () => {
  let component: LoginComponent;
  let fixture: ComponentFixture<LoginComponent>;
  let authService: jest.Mocked<AuthService>;
  let router: jest.Mocked<Router>;

  const mockUserProfile: UserProfile = {
    id: '123',
    email: 'test@example.com',
    firstName: 'John',
    lastName: 'Doe',
    streetAddress: '123 Main St',
    isEmailVerified: true
  };

  const mockAuthResponse: AuthResponse = {
    token: 'test-token',
    user: mockUserProfile,
    expiresAt: '2099-12-31T23:59:59Z'
  };

  beforeEach(async () => {
    const authServiceMock = {
      login: jest.fn(),
      getToken: jest.fn().mockReturnValue('test-token'),
      isAuthenticated: jest.fn().mockReturnValue(true)
    } as unknown as jest.Mocked<AuthService>;

    await TestBed.configureTestingModule({
      imports: [ReactiveFormsModule, LoginComponent],
      providers: [
        provideRouter([]),
        { provide: AuthService, useValue: authServiceMock }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(LoginComponent);
    component = fixture.componentInstance;
    authService = TestBed.inject(AuthService) as jest.Mocked<AuthService>;
    router = TestBed.inject(Router) as jest.Mocked<Router>;
    jest.spyOn(router, 'navigate');
    fixture.detectChanges();
  });

  it('should create', () => {
    //assert
    expect(component).toBeTruthy();
  });

  it('should initialize with empty form', () => {
    //assert
    expect(component.loginForm.value).toEqual({
      email: '',
      password: ''
    });
  });

  it('should mark form as invalid when empty', () => {
    //assert
    expect(component.loginForm.valid).toBe(false);
  });

  it('should mark form as valid when filled correctly', () => {
    //arrange & Act
    component.loginForm.patchValue({
      email: 'test@example.com',
      password: 'password123'
    });

    //assert
    expect(component.loginForm.valid).toBe(true);
  });

  it('should require email field', () => {
    //arrange
    const emailControl = component.loginForm.get('email');

    //act
    emailControl?.setValue('');

    //assert
    expect(emailControl?.hasError('required')).toBe(true);
  });

  it('should validate email format', () => {
    //arrange
    const emailControl = component.loginForm.get('email');

    //act
    emailControl?.setValue('invalid-email');

    //assert
    expect(emailControl?.hasError('email')).toBe(true);
  });

  it('should require password field', () => {
    //arrange
    const passwordControl = component.loginForm.get('password');

    //act
    passwordControl?.setValue('');

    //assert
    expect(passwordControl?.hasError('required')).toBe(true);
  });

  it('should call authService.login on form submission', () => {
    //arrange
    authService.login.mockReturnValue(of(mockAuthResponse));
    component.loginForm.patchValue({
      email: 'test@example.com',
      password: 'password123'
    });

    //act
    component.onSubmit();

    //assert
    expect(authService.login).toHaveBeenCalledWith('test@example.com', 'password123');
  });

  it('should navigate to main page on successful login', async () => {
    //arrange
    authService.login.mockReturnValue(of(mockAuthResponse));
    component.loginForm.patchValue({
      email: 'test@example.com',
      password: 'password123'
    });

    //act
    component.onSubmit();

    //assert
    await new Promise(resolve => setTimeout(resolve, 150));
    expect(router.navigate).toHaveBeenCalledWith(['/']);
  });

  it('should display error message on login failure', async () => {
    //arrange
    const errorResponse = { status: 401, error: { message: 'Invalid credentials' } };
    authService.login.mockReturnValue(throwError(() => errorResponse));
    component.loginForm.patchValue({
      email: 'test@example.com',
      password: 'wrongpassword'
    });

    //act
    component.onSubmit();

    //assert
    await new Promise(resolve => setTimeout(resolve, 100));
    expect(component.errorMessage).toBeTruthy();
    expect(component.isLoading).toBe(false);
  });

  it('should set isLoading to true during login', () => {
    //arrange
    authService.login.mockReturnValue(of(mockAuthResponse).pipe(delay(100)));
    component.loginForm.patchValue({
      email: 'test@example.com',
      password: 'password123'
    });

    //act
    component.onSubmit();

    //assert
    expect(component.isLoading).toBe(true);
  });

  it('should not submit form when invalid', () => {
    //arrange
    component.loginForm.patchValue({
      email: '',
      password: ''
    });

    //act
    component.onSubmit();

    //assert
    expect(authService.login).not.toHaveBeenCalled();
  });

  it('should handle email verification required error', async () => {
    //arrange
    const errorResponse = { status: 403, error: { message: 'Please verify your email' } };
    authService.login.mockReturnValue(throwError(() => errorResponse));
    component.loginForm.patchValue({
      email: 'test@example.com',
      password: 'password123'
    });

    //act
    component.onSubmit();

    //assert
    await new Promise(resolve => setTimeout(resolve, 100));
    expect(component.errorMessage).toContain('verify');
  });
});
