import { SharedItem } from './shared-item.interface';

export interface ItemSearchResult {
    items: SharedItem[];
    totalCount: number;
    pageNumber: number;
    pageSize: number;
    totalPages: number;
}
