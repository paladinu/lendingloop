import { Injectable } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Observable, catchError, throwError } from 'rxjs';
import { Router } from '@angular/router';
import { ItemRequest } from '../models/item-request.interface';
import { AuthService } from './auth.service';
import { environment } from '../../environments/environment';

@Injectable({
    providedIn: 'root'
})
export class ItemRequestService {
    private apiUrl = `${environment.apiUrl}/api/itemrequests`;

    constructor(
        private http: HttpClient,
        private authService: AuthService,
        private router: Router
    ) { }

    createRequest(itemId: string, message?: string): Observable<ItemRequest> {
        const body: any = { itemId };
        if (message !== undefined && message !== null) {
            body.message = message;
        }
        return this.http.post<ItemRequest>(this.apiUrl, body)
            .pipe(
                catchError(error => this.handleError(error))
            );
    }

    getMyRequests(): Observable<ItemRequest[]> {
        return this.http.get<ItemRequest[]>(`${this.apiUrl}/my-requests`)
            .pipe(
                catchError(error => this.handleError(error))
            );
    }

    getPendingRequests(): Observable<ItemRequest[]> {
        return this.http.get<ItemRequest[]>(`${this.apiUrl}/pending`)
            .pipe(
                catchError(error => this.handleError(error))
            );
    }

    getRequestsForItem(itemId: string): Observable<ItemRequest[]> {
        return this.http.get<ItemRequest[]>(`${this.apiUrl}/item/${itemId}`)
            .pipe(
                catchError(error => this.handleError(error))
            );
    }

    approveRequest(requestId: string): Observable<ItemRequest> {
        return this.http.put<ItemRequest>(`${this.apiUrl}/${requestId}/approve`, {})
            .pipe(
                catchError(error => this.handleError(error))
            );
    }

    rejectRequest(requestId: string): Observable<ItemRequest> {
        return this.http.put<ItemRequest>(`${this.apiUrl}/${requestId}/reject`, {})
            .pipe(
                catchError(error => this.handleError(error))
            );
    }

    cancelRequest(requestId: string): Observable<ItemRequest> {
        return this.http.put<ItemRequest>(`${this.apiUrl}/${requestId}/cancel`, {})
            .pipe(
                catchError(error => this.handleError(error))
            );
    }

    completeRequest(requestId: string): Observable<ItemRequest> {
        return this.http.put<ItemRequest>(`${this.apiUrl}/${requestId}/complete`, {})
            .pipe(
                catchError(error => this.handleError(error))
            );
    }

    private handleError(error: HttpErrorResponse): Observable<never> {
        console.error('ItemRequestService error:', error);

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
