import { query } from '@angular/animations';
import { Injectable } from '@angular/core';
import { delay, map, Observable, of } from 'rxjs';

export interface PagedRequest {
    query?: string;
    page: number; // 1-based
    pageSize: number;
}

export interface PagedResponse<T> {
    items: T[];
    total: number;
    page: number;
    pageSize: number;
}
export type ItemResponse = {
    label: string;
    value: string;
};

@Injectable({
    providedIn: 'root'
})
export class CountryService {
    private readonly countries: ItemResponse[] = [
        { label: 'China', value: 'China' },
        { label: 'Bangladesh', value: 'Bangladesh' },
        { label: 'India', value: 'India' },
        { label: 'USA', value: 'USA' },
        { label: 'Canada', value: 'Canada' },
        { label: 'UK', value: 'UK' },
        { label: 'Germany', value: 'Germany' },
        { label: 'France', value: 'France' },
        { label: 'Japan', value: 'Japan' },
        { label: 'Australia', value: 'Australia' },
        { label: 'Mexico', value: 'Mexico' }
    ];

    findAll(request: PagedRequest): Observable<PagedResponse<ItemResponse>> {
        const search = request.query?.toLowerCase() ?? '';
        return of(this.countries).pipe(
            map((list) => {
                // 1️⃣ filter
                const filtered = query != null ? list.filter((c) => c.label.toLowerCase().includes(search)) : list;

                // 2️⃣ paginate
                const start = (request.page - 1) * request.pageSize;
                const end = start + request.pageSize;

                return {
                    items: filtered.slice(start, end),
                    total: filtered.length,
                    page: request.page,
                    pageSize: request.pageSize
                };
            }),
            delay(100) // simulate API latency
        );
    }
}

@Injectable({
    providedIn:'root'
})

export class CustomerTypeService {
    private readonly customerTypes: ItemResponse[] = [
        { label: 'Retail', value: 'RETAIL' },
        { label: 'Wholesale', value: 'WHOLESALE' },
        { label: 'Corporate', value: 'CORPORATE' },
        { label: 'Distributor', value: 'DISTRIBUTOR' }
    ];

    findAll(request: PagedRequest): Observable<PagedResponse<ItemResponse>> {
        const search = request.query?.toLowerCase() ?? '';
        return of(this.customerTypes).pipe(
            map((list) => {
                // 1️⃣ filter
                const filtered = query != null ? list.filter((c) => c.label.toLowerCase().includes(search)) : list;

                // 2️⃣ paginate
                const start = (request.page - 1) * request.pageSize;
                const end = start + request.pageSize;

                return {
                    items: filtered.slice(start, end),
                    total: filtered.length,
                    page: request.page,
                    pageSize: request.pageSize
                };
            }),
            delay(100) // simulate API latency
        );
    }
}
