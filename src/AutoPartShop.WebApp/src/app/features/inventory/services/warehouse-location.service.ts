import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { environment } from 'src/environments/environment';

// ── Models ────────────────────────────────────────────────────────────────────

/**
 * Standalone physical bin/shelf location (Zone-Aisle-Rack-Bin) inside a warehouse.
 * Independent of any specific product — printed as a barcode/QR label and stuck
 * on an empty shelf before any stock is assigned there. `ProductLocation`
 * (a product's own location) now references one of these by id rather than
 * carrying free-text Section/Shelf — see `product-location.service.ts`.
 */
export interface WarehouseLocationResponse {
    id: string;
    warehouseId: string;
    warehouseName: string;
    warehouseCode: string;
    zone: string;
    aisle: string;
    rack: string;
    bin: string;
    /** Computed "Zone-Aisle-Rack-Bin", e.g. "A-04-B-12". */
    locationCode: string;
    categoryId: string | null;
    categoryName: string | null;
    notes: string | null;
    isActive: boolean;
    createdBy: string;
    createdAt: string;
}

export interface CreateWarehouseLocationRequest {
    warehouseId: string;
    zone: string;
    aisle: string;
    rack: string;
    bin: string;
    categoryId?: string | null;
    notes?: string | null;
}

export type UpdateWarehouseLocationRequest = CreateWarehouseLocationRequest;

export interface WarehouseLocationQuery {
    warehouseId?: string | null;
    categoryId?: string | null;
    search?: string;
    pageNumber?: number;
    pageSize?: number;
}

export interface PaginationMeta {
    page: number;
    pageSize: number;
    totalCount: number;
    totalPages: number;
    hasNextPage: boolean;
    hasPreviousPage: boolean;
}

export interface PagedWarehouseLocationResponse {
    data: WarehouseLocationResponse[];
    pagination: PaginationMeta;
}

// ── Service ───────────────────────────────────────────────────────────────────

@Injectable({ providedIn: 'root' })
export class WarehouseLocationService {
    private readonly http = inject(HttpClient);
    private readonly apiUrl = `${environment.apiUrl}/v1/warehouse-locations`;

    /** Paged, filterable list — GET /api/v1/warehouse-locations */
    getList(query: WarehouseLocationQuery): Observable<PagedWarehouseLocationResponse> {
        let params = new HttpParams()
            .set('pageNumber', String(query.pageNumber ?? 1))
            .set('pageSize', String(query.pageSize ?? 20));

        if (query.warehouseId) params = params.set('warehouseId', query.warehouseId);
        if (query.categoryId) params = params.set('categoryId', query.categoryId);
        if (query.search) params = params.set('search', query.search);

        return this.http.get<PagedWarehouseLocationResponse>(this.apiUrl, { params });
    }

    /** Alias mirroring other feature services' naming (e.g. CategoryService.getCategories). */
    search(query: WarehouseLocationQuery): Observable<PagedWarehouseLocationResponse> {
        return this.getList(query);
    }

    /** All (unpaged) locations in a single warehouse — for populating a picker. */
    getByWarehouse(warehouseId: string): Observable<WarehouseLocationResponse[]> {
        return this.http
            .get<{ data: WarehouseLocationResponse[] }>(`${this.apiUrl}/warehouse/${warehouseId}`)
            .pipe(map((res) => res.data ?? []));
    }

    /** Get a single location by ID. */
    getById(id: string): Observable<WarehouseLocationResponse> {
        return this.http
            .get<{ data: WarehouseLocationResponse }>(`${this.apiUrl}/${id}`)
            .pipe(map((res) => res.data));
    }

    /** Create a new location. 409 on a duplicate Zone/Aisle/Rack/Bin combo in the same warehouse. */
    create(request: CreateWarehouseLocationRequest): Observable<WarehouseLocationResponse> {
        return this.http
            .post<{ data: WarehouseLocationResponse }>(this.apiUrl, request)
            .pipe(map((res) => res.data));
    }

    /** Full update. 409 on a duplicate Zone/Aisle/Rack/Bin combo in the same warehouse. */
    update(id: string, request: UpdateWarehouseLocationRequest): Observable<WarehouseLocationResponse> {
        return this.http
            .put<{ data: WarehouseLocationResponse }>(`${this.apiUrl}/${id}`, request)
            .pipe(map((res) => res.data));
    }

    /** Soft delete. */
    delete(id: string): Observable<void> {
        return this.http.delete<void>(`${this.apiUrl}/${id}`);
    }
}
