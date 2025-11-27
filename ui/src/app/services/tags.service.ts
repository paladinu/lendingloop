import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { catchError, map } from 'rxjs/operators';
import { SystemTag } from '../models/system-tag.interface';
import { environment } from '../../environments/environment';

@Injectable({
    providedIn: 'root'
})
export class TagsService {
    private readonly API_URL = `${environment.apiUrl}/api/tags`;
    private cachedTags: SystemTag[] | null = null;

    constructor(private http: HttpClient) { }

    getAllTags(): Observable<SystemTag[]> {
        if (this.cachedTags) {
            return of(this.cachedTags);
        }

        return this.http.get<SystemTag[]>(this.API_URL).pipe(
            map(tags => {
                // Sort tags alphabetically by display name
                const sortedTags = tags.sort((a, b) => 
                    a.displayName.localeCompare(b.displayName)
                );
                this.cachedTags = sortedTags;
                return sortedTags;
            }),
            catchError(error => {
                console.error('Error fetching tags:', error);
                // Clear cache on error so retry will fetch fresh data
                this.cachedTags = null;
                // Re-throw the error so components can handle it
                throw error;
            })
        );
    }

    getTagStatistics(): Observable<Map<string, number>> {
        return this.http.get<{ [key: string]: number }>(`${this.API_URL}/statistics`).pipe(
            map(stats => new Map(Object.entries(stats))),
            catchError(error => {
                console.error('Error fetching tag statistics:', error);
                // Re-throw the error so components can handle it
                throw error;
            })
        );
    }

    searchTags(query: string, tags: SystemTag[]): SystemTag[] {
        if (!query || query.trim() === '') {
            return tags;
        }

        const lowerQuery = query.toLowerCase();
        return tags.filter(tag =>
            tag.name.toLowerCase().includes(lowerQuery) ||
            tag.displayName.toLowerCase().includes(lowerQuery)
        );
    }

    clearCache(): void {
        this.cachedTags = null;
    }
}
