import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { LoginRequest } from '../../models/auth.interface';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './login.component.html',
  styleUrl: './login.component.css'
})
export class LoginComponent {
  loginForm: FormGroup;
  isLoading = false;
  errorMessage = '';

  constructor(
    private fb: FormBuilder,
    private authService: AuthService,
    private router: Router
  ) {
    this.loginForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required]]
    });
  }

  get email() { return this.loginForm.get('email'); }
  get password() { return this.loginForm.get('password'); }

  onSubmit() {
    if (this.loginForm.valid) {
      this.isLoading = true;
      this.errorMessage = '';

      const loginRequest: LoginRequest = {
        email: this.loginForm.value.email,
        password: this.loginForm.value.password
      };

      this.authService.login(loginRequest.email, loginRequest.password).subscribe({
        next: (response) => {
          this.isLoading = false;
          console.log('Login successful:', response);
          console.log('Token stored:', this.authService.getToken());
          console.log('Is authenticated:', this.authService.isAuthenticated());

          // Check if there's a return URL stored by the AuthGuard
          const returnUrl = localStorage.getItem('returnUrl') || '/';
          localStorage.removeItem('returnUrl');

          console.log('Navigating to:', returnUrl);

          // Add a small delay to ensure authentication state is properly set
          setTimeout(() => {
            this.router.navigate([returnUrl]);
          }, 100);
        },
        error: (error) => {
          this.isLoading = false;
          console.error('Login error:', error);
          if (error.status === 401) {
            this.errorMessage = 'Invalid email or password';
          } else if (error.status === 403) {
            this.errorMessage = 'Please verify your email address before logging in';
          } else {
            this.errorMessage = 'An error occurred during login. Please try again.';
          }
        }
      });
    } else {
      // Mark all fields as touched to show validation errors
      this.loginForm.markAllAsTouched();
    }
  }
}