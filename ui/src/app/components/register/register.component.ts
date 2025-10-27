import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule, AbstractControl, ValidationErrors } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { RegisterRequest } from '../../models/auth.interface';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './register.component.html',
  styleUrl: './register.component.css'
})
export class RegisterComponent {
  registerForm: FormGroup;
  isLoading = false;
  errorMessage = '';
  successMessage = '';

  constructor(
    private fb: FormBuilder,
    private authService: AuthService,
    private router: Router
  ) {
    this.registerForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      firstName: ['', [Validators.required, Validators.minLength(2)]],
      lastName: ['', [Validators.required, Validators.minLength(2)]],
      streetAddress: ['', [Validators.required, Validators.minLength(5)]],
      password: ['', [Validators.required, this.passwordValidator]]
    });
  }

  // Custom password validator that implements the password policy
  passwordValidator(control: AbstractControl): ValidationErrors | null {
    const value = control.value;
    if (!value) {
      return null; // Let required validator handle empty values
    }

    const errors: ValidationErrors = {};

    if (value.length < 8) {
      errors['minLength'] = true;
    }

    if (!/[a-z]/.test(value)) {
      errors['lowercase'] = true;
    }

    if (!/[A-Z]/.test(value)) {
      errors['uppercase'] = true;
    }

    if (!/[!@#$%^&*()_+\-=\[\]{};':"\\|,.<>\/?]/.test(value)) {
      errors['specialChar'] = true;
    }

    return Object.keys(errors).length > 0 ? errors : null;
  }

  get email() { return this.registerForm.get('email'); }
  get firstName() { return this.registerForm.get('firstName'); }
  get lastName() { return this.registerForm.get('lastName'); }
  get streetAddress() { return this.registerForm.get('streetAddress'); }
  get password() { return this.registerForm.get('password'); }

  // Helper methods for password validation feedback
  get passwordTooShort() { return this.password?.errors?.['minLength'] && this.password?.touched; }
  get passwordMissingLowercase() { return this.password?.errors?.['lowercase'] && this.password?.touched; }
  get passwordMissingUppercase() { return this.password?.errors?.['uppercase'] && this.password?.touched; }
  get passwordMissingSpecialChar() { return this.password?.errors?.['specialChar'] && this.password?.touched; }

  onSubmit() {
    if (this.registerForm.valid) {
      this.isLoading = true;
      this.errorMessage = '';
      this.successMessage = '';

      const registerRequest: RegisterRequest = {
        email: this.registerForm.value.email,
        firstName: this.registerForm.value.firstName,
        lastName: this.registerForm.value.lastName,
        streetAddress: this.registerForm.value.streetAddress,
        password: this.registerForm.value.password
      };

      this.authService.register(registerRequest).subscribe({
        next: (response) => {
          this.isLoading = false;
          this.successMessage = 'Registration successful! Please check your email to verify your account.';
          // Clear the form
          this.registerForm.reset();
        },
        error: (error) => {
          this.isLoading = false;
          if (error.status === 409) {
            this.errorMessage = 'An account with this email address already exists';
          } else if (error.status === 400) {
            this.errorMessage = 'Please check your information and try again';
          } else {
            this.errorMessage = 'An error occurred during registration. Please try again.';
          }
        }
      });
    } else {
      // Mark all fields as touched to show validation errors
      this.registerForm.markAllAsTouched();
    }
  }
}