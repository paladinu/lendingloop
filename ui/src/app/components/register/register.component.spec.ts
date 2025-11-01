import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ReactiveFormsModule } from '@angular/forms';
import { Router, ActivatedRoute, provideRouter } from '@angular/router';
import { of, throwError, delay } from 'rxjs';
import { RegisterComponent } from './register.component';
import { AuthService } from '../../services/auth.service';
import { RegisterResponse, UserProfile } from '../../models/auth.interface';

describe('RegisterComponent', () => {
  let component: RegisterComponent;
  let fixture: ComponentFixture<RegisterComponent>;
  let authService: jest.Mocked<AuthService>;
  let router: jest.Mocked<Router>;

  const mockUserProfile: UserProfile = {
    id: '123',
    email: 'newuser@example.com',
    firstName: 'Jane',
    lastName: 'Smith',
    streetAddress: '456 Oak Ave',
    isEmailVerified: false
  };

  const mockRegisterResponse: RegisterResponse = {
    message: 'Registration successful',
    user: mockUserProfile
  };

  beforeEach(async () => {
    const authServiceMock = {
      register: jest.fn()
    } as unknown as jest.Mocked<AuthService>;

    await TestBed.configureTestingModule({
      imports: [ReactiveFormsModule, RegisterComponent],
      providers: [
        provideRouter([]),
        { provide: AuthService, useValue: authServiceMock }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(RegisterComponent);
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
    expect(component.registerForm.value).toEqual({
      email: '',
      password: '',
      firstName: '',
      lastName: '',
      streetAddress: ''
    });
  });

  it('should mark form as invalid when empty', () => {
    //assert
    expect(component.registerForm.valid).toBe(false);
  });

  it('should mark form as valid when filled correctly', () => {
    //arrange & Act
    component.registerForm.patchValue({
      email: 'test@example.com',
      password: 'ValidPass123!',
      firstName: 'John',
      lastName: 'Doe',
      streetAddress: '123 Main St'
    });

    //assert
    expect(component.registerForm.valid).toBe(true);
  });

  it('should require all fields', () => {
    //arrange
    const controls = component.registerForm.controls;

    //act & Assert
    expect(controls['email'].hasError('required')).toBe(true);
    expect(controls['password'].hasError('required')).toBe(true);
    expect(controls['firstName'].hasError('required')).toBe(true);
    expect(controls['lastName'].hasError('required')).toBe(true);
    expect(controls['streetAddress'].hasError('required')).toBe(true);
  });

  it('should validate email format', () => {
    //arrange
    const emailControl = component.registerForm.get('email');

    //act
    emailControl?.setValue('invalid-email');

    //assert
    expect(emailControl?.hasError('email')).toBe(true);
  });

  it('should validate password minimum length', () => {
    //arrange
    const passwordControl = component.registerForm.get('password');

    //act
    passwordControl?.setValue('short');

    //assert
    expect(passwordControl?.hasError('minLength')).toBe(true);
  });

  it('should validate password contains lowercase', () => {
    //arrange
    const passwordControl = component.registerForm.get('password');

    //act
    passwordControl?.setValue('UPPERCASE123!');
    passwordControl?.markAsTouched();

    //assert
    expect(component.passwordMissingLowercase).toBe(true);
  });

  it('should validate password contains uppercase', () => {
    //arrange
    const passwordControl = component.registerForm.get('password');

    //act
    passwordControl?.setValue('lowercase123!');
    passwordControl?.markAsTouched();

    //assert
    expect(component.passwordMissingUppercase).toBe(true);
  });

  it('should validate password contains special character', () => {
    //arrange
    const passwordControl = component.registerForm.get('password');

    //act
    passwordControl?.setValue('NoSpecial123');
    passwordControl?.markAsTouched();

    //assert
    expect(component.passwordMissingSpecialChar).toBe(true);
  });

  it('should pass all password validations for valid password', () => {
    //arrange
    const passwordControl = component.registerForm.get('password');

    //act
    passwordControl?.setValue('ValidPass123!');
    passwordControl?.markAsTouched();
    passwordControl?.updateValueAndValidity();
    fixture.detectChanges();

    //assert
    expect(component.passwordTooShort).toBeFalsy();
    expect(component.passwordMissingLowercase).toBeFalsy();
    expect(component.passwordMissingUppercase).toBeFalsy();
    expect(component.passwordMissingSpecialChar).toBeFalsy();
  });

  it('should call authService.register on form submission', () => {
    //arrange
    authService.register.mockReturnValue(of(mockRegisterResponse));
    component.registerForm.patchValue({
      email: 'newuser@example.com',
      password: 'ValidPass123!',
      firstName: 'Jane',
      lastName: 'Smith',
      streetAddress: '456 Oak Ave'
    });

    //act
    component.onSubmit();

    //assert
    expect(authService.register).toHaveBeenCalledWith({
      email: 'newuser@example.com',
      password: 'ValidPass123!',
      firstName: 'Jane',
      lastName: 'Smith',
      streetAddress: '456 Oak Ave'
    });
  });

  it('should show success message on successful registration', async () => {
    //arrange
    authService.register.mockReturnValue(of(mockRegisterResponse));
    component.registerForm.patchValue({
      email: 'newuser@example.com',
      password: 'ValidPass123!',
      firstName: 'Jane',
      lastName: 'Smith',
      streetAddress: '456 Oak Ave'
    });

    //act
    component.onSubmit();

    //assert
    await new Promise(resolve => setTimeout(resolve, 100));
    expect(component.successMessage).toBeTruthy();
    expect(component.isLoading).toBe(false);
  });

  it('should display error message on registration failure', async () => {
    //arrange
    const errorResponse = { status: 409, error: { message: 'Email already exists' } };
    authService.register.mockReturnValue(throwError(() => errorResponse));
    component.registerForm.patchValue({
      email: 'existing@example.com',
      password: 'ValidPass123!',
      firstName: 'Jane',
      lastName: 'Smith',
      streetAddress: '456 Oak Ave'
    });

    //act
    component.onSubmit();

    //assert
    await new Promise(resolve => setTimeout(resolve, 100));
    expect(component.errorMessage).toBeTruthy();
    expect(component.isLoading).toBe(false);
  });

  it('should set isLoading to true during registration', () => {
    //arrange
    authService.register.mockReturnValue(of(mockRegisterResponse).pipe(delay(100)));
    component.registerForm.patchValue({
      email: 'newuser@example.com',
      password: 'ValidPass123!',
      firstName: 'Jane',
      lastName: 'Smith',
      streetAddress: '456 Oak Ave'
    });

    //act
    component.onSubmit();

    //assert
    expect(component.isLoading).toBe(true);
  });

  it('should not submit form when invalid', () => {
    //arrange
    component.registerForm.patchValue({
      email: '',
      password: '',
      firstName: '',
      lastName: '',
      streetAddress: ''
    });

    //act
    component.onSubmit();

    //assert
    expect(authService.register).not.toHaveBeenCalled();
  });

  it('should update password validation on password change', () => {
    //arrange
    const passwordControl = component.registerForm.get('password');

    //act
    passwordControl?.setValue('ValidPass123!');
    passwordControl?.markAsTouched();
    passwordControl?.updateValueAndValidity();
    fixture.detectChanges();

    //assert
    expect(component.passwordTooShort).toBeFalsy();
    expect(component.passwordMissingLowercase).toBeFalsy();
    expect(component.passwordMissingUppercase).toBeFalsy();
    expect(component.passwordMissingSpecialChar).toBeFalsy();
  });
});
