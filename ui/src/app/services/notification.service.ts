import { Injectable } from '@angular/core';
import { HttpClient, HttpErrorResponse, HttpParams } from '@angular/common/http';
import { Observable, catchError, throwError } from 'rxjs';
import { Router } from '@angular/router';
import { Notification } from '../models/notification.interface';
import { AuthService } from './auth.service';
import { environment } from '../../environments/environment';

@Injectable({
    providedIn: 'root'
})
export class NotificationService {
    private apiUrl = `${environment.apiUrl}/api/notifications`;

    constructor(
        private http: HttpClient,
        private authService: AuthService,
        private router: Router
    ) { }

    getNotifications(limit?: number): Observable<Notification[]> {
        let params = new HttpParams();
        if (limit !== undefined) {
            params = params.set('limit', limit.toString());
        }

        return this.http.get<Notification[]>(this.apiUrl, { params })
            .pipe(
                catchError(error => this.handleError(error))
            );
    }

    getUnreadCount(): Observable<number> {
        return this.http.get<number>(`${this.apiUrl}/unread-count`)
            .pipe(
                catchError(error => this.handleError(error))
            );
    }

    markAsRead(notificationId: string): Observable<Notification> {
        return this.http.put<Notification>(`${this.apiUrl}/${notificationId}/read`, {})
            .pipe(
                catchError(error => this.handleError(error))
            );
    }

    markAllAsRead(): Observable<boolean> {
        return this.http.put<boolean>(`${this.apiUrl}/mark-all-read`, {})
            .pipe(
                catchError(error => this.handleError(error))
            );
    }

    deleteNotification(notificationId: string): Observable<boolean> {
        return this.http.delete<boolean>(`${this.apiUrl}/${notificationId}`)
            .pipe(
                catchError(error => this.handleError(error))
            );
    }

    private handleError(error: HttpErrorResponse): Observable<never> {
        console.error('NotificationService error:', error);

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
