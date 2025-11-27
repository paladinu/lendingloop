export interface ItemSearchFilter {
    searchText?: string;
    tags: string[];
    isAvailable?: boolean;
    ownerIds: string[];
    pageNumber: number;
    pageSize: number;
}
