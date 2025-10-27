import { Injectable } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Observable, catchError, throwError } from 'rxjs';
import { Router } from '@angular/router';
import { UserProfile } from '../models/auth.interface';
import { AuthService } from './auth.service';

@Injectable({
    providedIn: 'root'
})
export class UserService {
    private readonly API_URL = 'http://localhost:8080/api/auth';

    constructor(
        private http: HttpClient,
        private authService: AuthService,
        private router: Router
    ) { }

    getCurrentUser(): Observable<UserProfile> {
        return this.http.get<UserProfile>(`${this.API_URL}/me`)
            .pipe(
                catchError(error => this.handleError(error))
            );
    }

    // Future method for getting user details by ID
    // This would require a new API endpoint on the backend
    getUserById(userId: string): Observable<UserProfile> {
        return this.http.get<UserProfile>(`${this.API_URL}/users/${userId}`)
            .pipe(
                catchError(error => this.handleError(error))
            );
    }

    private handleError(error: HttpErrorResponse): Observable<never> {
        console.error('UserService error:', error);

        // Handle authentication errors
        if (error.status === 401) {
            // Token expired or invalid - logout and redirect to login
            this.authService.logout();
            this.router.navigate(['/login']);
            return throwError(() => new Error('Authentication required. Please log in again.'));
        }

        if (error.status === 403) {
            // Forbidden - user doesn't have permission
            return throwError(() => new Error('You do not have permission to perform this action.'));
        }

        if (error.status === 404) {
            // User not found
            return throwError(() => new Error('User not found.'));
        }

        // Handle other HTTP errors
        let errorMessage = 'An unexpected error occurred.';
        if (error.error?.message) {
            errorMessage = error.error.message;
        } else if (error.message) {
            errorMessage = error.message;
        }

        return throwError(() => new Error(errorMessage));
    }
}