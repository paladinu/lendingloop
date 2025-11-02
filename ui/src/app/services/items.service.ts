import { Injectable } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Observable, catchError, throwError } from 'rxjs';
import { Router } from '@angular/router';
import { SharedItem } from '../models/shared-item.interface';
import { AuthService } from './auth.service';

@Injectable({
    providedIn: 'root'
})
export class ItemsService {
    private apiUrl = 'http://localhost:8080/api/items';

    constructor(
        private http: HttpClient,
        private authService: AuthService,
        private router: Router
    ) { }

    getItems(): Observable<SharedItem[]> {
        return this.http.get<SharedItem[]>(this.apiUrl)
            .pipe(
                catchError(error => this.handleError(error))
            );
    }

    createItem(item: Partial<SharedItem>): Observable<SharedItem> {
        return this.http.post<SharedItem>(this.apiUrl, item)
            .pipe(
                catchError(error => this.handleError(error))
            );
    }

    uploadItemImage(itemId: string, imageFile: File): Observable<SharedItem> {
        const formData = new FormData();
        formData.append('file', imageFile);
        return this.http.post<SharedItem>(`${this.apiUrl}/${itemId}/image`, formData)
            .pipe(
                catchError(error => this.handleError(error))
            );
    }

    getItemById(itemId: string): Observable<SharedItem> {
        return this.http.get<SharedItem>(`${this.apiUrl}/${itemId}`)
            .pipe(
                catchError(error => this.handleError(error))
            );
    }

    updateItemVisibility(
        itemId: string,
        visibleToLoopIds: string[],
        visibleToAllLoops: boolean,
        visibleToFutureLoops: boolean
    ): Observable<SharedItem> {
        return this.http.put<SharedItem>(`${this.apiUrl}/${itemId}/visibility`, {
            visibleToLoopIds,
            visibleToAllLoops,
            visibleToFutureLoops
        }).pipe(
            catchError(error => this.handleError(error))
        );
    }

    updateItem(itemId: string, updates: Partial<SharedItem>): Observable<SharedItem> {
        return this.http.put<SharedItem>(`${this.apiUrl}/${itemId}`, updates)
            .pipe(
                catchError(error => this.handleError(error))
            );
    }

    private handleError(error: HttpErrorResponse): Observable<never> {
        console.error('ItemsService error:', error);

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