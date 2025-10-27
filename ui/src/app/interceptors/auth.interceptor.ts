import { Injectable } from '@angular/core';
import { HttpInterceptor, HttpRequest, HttpHandler, HttpEvent, HttpErrorResponse } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

@Injectable()
export class AuthInterceptor implements HttpInterceptor {

    constructor(
        private authService: AuthService,
        private router: Router
    ) { }

    intercept(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
        // Get the auth token from the service
        const authToken = this.authService.getToken();

        // Clone the request and add the authorization header if token exists
        let authReq = req;
        if (authToken && this.shouldAddToken(req.url)) {
            authReq = req.clone({
                headers: req.headers.set('Authorization', `Bearer ${authToken}`)
            });
        }

        // Handle the request and catch authentication errors
        return next.handle(authReq).pipe(
            catchError((error: HttpErrorResponse) => {
                if (error.status === 401) {
                    // Token is invalid or expired
                    this.handleUnauthorized();
                }
                return throwError(() => error);
            })
        );
    }

    private shouldAddToken(url: string): boolean {
        // Don't add token to login and register requests
        const excludedEndpoints = ['/auth/login', '/auth/register', '/auth/verify-email'];
        return !excludedEndpoints.some(endpoint => url.includes(endpoint));
    }

    private handleUnauthorized(): void {
        // Clear stored authentication data
        this.authService.logout();

        // Redirect to login page
        this.router.navigate(['/login']);
    }
}