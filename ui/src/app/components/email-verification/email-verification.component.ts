import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-email-verification',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './email-verification.component.html',
  styleUrl: './email-verification.component.css'
})
export class EmailVerificationComponent implements OnInit {
  isLoading = true;
  isSuccess = false;
  errorMessage = '';
  successMessage = '';
  canResend = false;
  isResending = false;
  resendMessage = '';

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private authService: AuthService
  ) { }

  ngOnInit() {
    // Get the verification token from the URL query parameters
    this.route.queryParams.subscribe(params => {
      const token = params['token'];
      if (token) {
        this.verifyEmail(token);
      } else {
        this.isLoading = false;
        this.errorMessage = 'No verification token provided';
        this.canResend = true;
      }
    });
  }

  verifyEmail(token: string) {
    this.authService.verifyEmail(token).subscribe({
      next: (response) => {
        this.isLoading = false;
        if (response.success) {
          this.isSuccess = true;
          this.successMessage = response.message || 'Email verified successfully! You can now log in to your account.';
        } else {
          this.errorMessage = response.message || 'Email verification failed';
          this.canResend = true;
        }
      },
      error: (error) => {
        this.isLoading = false;
        if (error.status === 400) {
          this.errorMessage = 'Invalid or expired verification token';
        } else if (error.status === 404) {
          this.errorMessage = 'Verification token not found';
        } else {
          this.errorMessage = 'An error occurred during email verification';
        }
        this.canResend = true;
      }
    });
  }

  resendVerificationEmail() {
    // Get email from user input or stored data
    const email = prompt('Please enter your email address to resend verification:');
    if (!email) {
      return;
    }

    this.isResending = true;
    this.resendMessage = '';

    this.authService.resendVerificationEmail(email).subscribe({
      next: (response) => {
        this.isResending = false;
        this.resendMessage = response.message || 'Verification email sent successfully!';
      },
      error: (error) => {
        this.isResending = false;
        if (error.status === 400) {
          this.resendMessage = 'This email address is already verified or invalid.';
        } else if (error.status === 500) {
          this.resendMessage = 'Failed to send verification email. Please try again later.';
        } else {
          this.resendMessage = 'An error occurred while sending verification email.';
        }
      }
    });
  }

  goToLogin() {
    this.router.navigate(['/login']);
  }

  goToRegister() {
    this.router.navigate(['/register']);
  }
}