import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { SharedItem } from '../shared-item.interface';

@Injectable({
    providedIn: 'root'
})
export class ItemsService {
    private apiUrl = '/api/items';

    constructor(private http: HttpClient) { }

    getItems(): Observable<SharedItem[]> {
        return this.http.get<SharedItem[]>(this.apiUrl);
    }

    createItem(item: Partial<SharedItem>): Observable<SharedItem> {
        return this.http.post<SharedItem>(this.apiUrl, item);
    }
}