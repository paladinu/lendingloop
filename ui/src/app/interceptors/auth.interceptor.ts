import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { AuthService } from '../services/auth.service';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
    const authService = inject(AuthService);
    const router = inject(Router);

    // Get the auth token from the service
    const authToken = authService.getToken();

    // Clone the request and add the authorization header if token exists
    let authReq = req;
    if (authToken && shouldAddToken(req.url)) {
        console.log('AuthInterceptor - Adding token to request:', req.url);
        authReq = req.clone({
            headers: req.headers.set('Authorization', `Bearer ${authToken}`)
        });
    } else {
        console.log('AuthInterceptor - No token added to request:', req.url, 'Has token:', !!authToken);
    }

    // Handle the request and catch authentication errors
    return next(authReq).pipe(
        catchError((error: HttpErrorResponse) => {
            if (error.status === 401 && shouldHandleUnauthorized(req.url)) {
                console.log('AuthInterceptor - 401 error, redirecting to login');
                // Token is invalid or expired
                handleUnauthorized(authService, router);
            }
            return throwError(() => error);
        })
    );
};

function shouldAddToken(url: string): boolean {
    // Don't add token to login and register requests
    const excludedEndpoints = ['/auth/login', '/auth/register', '/auth/verify-email', '/auth/resend-verification'];
    return !excludedEndpoints.some(endpoint => url.includes(endpoint));
}

function shouldHandleUnauthorized(url: string): boolean {
    // Don't handle 401 errors for logout endpoint to prevent infinite loops
    return !url.includes('/auth/logout');
}

function handleUnauthorized(authService: AuthService, router: Router): void {
    // Clear stored authentication data
    authService.logout();

    // Redirect to login page
    router.navigate(['/login']);
}