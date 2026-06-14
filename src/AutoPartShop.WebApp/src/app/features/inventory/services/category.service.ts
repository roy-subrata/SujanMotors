import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { environment } from 'src/environments/environment';

// ── Models ────────────────────────────────────────────────────────────────────

export interface CategoryResponse {
    id: string;
    name: string;
    description: string | null;
    parentCategoryId: string | null;
    isActive: boolean;
    displayOrder: number;
    createdBy: string | null;
    modifiedBy: string | null;
    breadcrumbPath: string;
    depthLevel: number;
    childCount: number;
    subCategories: CategoryResponse[];
}

export interface CreateCategoryRequest {
    name: string;
    description?: string | null;
    displayOrder?: number;
    parentCategoryId?: string | null;
}

export interface UpdateCategoryRequest {
    name: string;
    description?: string | null;
    displayOrder?: number;
    isActive?: boolean;
}

export interface CategoryQuery {
    search?: string;
    isActive?: boolean | null;
    page?: number;
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

export interface PagedCategoryResponse {
    data: CategoryResponse[];
    pagination: PaginationMeta;
}

// ── Service ───────────────────────────────────────────────────────────────────

@Injectable({ providedIn: 'root' })
export class CategoryService {
    private readonly http = inject(HttpClient);
    private readonly apiUrl = `${environment.apiUrl}/v1/categories`;

    /** List categories — all filters via query params. */
    getCategories(query: CategoryQuery): Observable<PagedCategoryResponse> {
        let params = new HttpParams()
            .set('page', String(query.page ?? 1))
            .set('pageSize', String(query.pageSize ?? 10));

        if (query.search) params = params.set('search', query.search);
        if (query.isActive !== null && query.isActive !== undefined)
            params = params.set('isActive', String(query.isActive));

        return this.http.get<PagedCategoryResponse>(this.apiUrl, { params });
    }

    /** All active categories as a flat array — convenience for dropdowns. */
    getActiveCategories(): Observable<CategoryResponse[]> {
        return this.getCategories({ isActive: true, pageSize: 500 }).pipe(map(r => r.data));
    }

    /** All categories (active + inactive) as a flat array — convenience for dropdowns. */
    getAllCategories(): Observable<CategoryResponse[]> {
        return this.getCategories({ pageSize: 500 }).pipe(map(r => r.data));
    }

    /** Get a single category by ID. */
    getCategoryById(id: string): Observable<{ data: CategoryResponse }> {
        return this.http.get<{ data: CategoryResponse }>(`${this.apiUrl}/${id}`);
    }

    /** Direct children of a category. */
    getSubcategories(parentId: string): Observable<{ data: CategoryResponse[] }> {
        return this.http.get<{ data: CategoryResponse[] }>(`${this.apiUrl}/${parentId}/subcategories`);
    }

    /** Ancestors from immediate parent to root. */
    getAncestors(id: string): Observable<{ data: CategoryResponse[] }> {
        return this.http.get<{ data: CategoryResponse[] }>(`${this.apiUrl}/${id}/ancestors`);
    }

    /** All descendants at every level. */
    getDescendants(id: string): Observable<{ data: CategoryResponse[] }> {
        return this.http.get<{ data: CategoryResponse[] }>(`${this.apiUrl}/${id}/descendants`);
    }

    /** Breadcrumb path string. */
    getBreadcrumb(id: string): Observable<{ data: { categoryId: string; breadcrumbPath: string } }> {
        return this.http.get<{ data: { categoryId: string; breadcrumbPath: string } }>(`${this.apiUrl}/${id}/breadcrumb`);
    }

    /** Check if re-parenting would create a circular reference. */
    checkCircularReference(id: string, newParentId: string | null): Observable<{ data: { wouldCreateCircularReference: boolean; message: string } }> {
        let params = new HttpParams();
        if (newParentId) params = params.set('newParentId', newParentId);
        return this.http.post<{ data: { wouldCreateCircularReference: boolean; message: string } }>(
            `${this.apiUrl}/${id}/check-circular-reference`, {}, { params });
    }

    /** Create a new category. */
    createCategory(request: CreateCategoryRequest): Observable<{ data: CategoryResponse }> {
        return this.http.post<{ data: CategoryResponse }>(this.apiUrl, request);
    }

    /** Full update. ID is in the URL only. */
    updateCategory(id: string, request: UpdateCategoryRequest): Observable<{ data: CategoryResponse }> {
        return this.http.put<{ data: CategoryResponse }>(`${this.apiUrl}/${id}`, request);
    }

    /** Activate or deactivate. */
    setStatus(id: string, isActive: boolean): Observable<{ data: CategoryResponse }> {
        return this.http.patch<{ data: CategoryResponse }>(`${this.apiUrl}/${id}/status`, { isActive });
    }

    /** Soft delete. */
    deleteCategory(id: string): Observable<void> {
        return this.http.delete<void>(`${this.apiUrl}/${id}`);
    }
}
