import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { environment } from 'src/environments/environment';

/** Mirrors the API ProductMediaDto. */
export interface ProductMedia {
    id: string;
    url: string;
    mediaType: 'image' | 'video';
    altText: string | null;
    fileName: string | null;
    sortOrder: number;
    isPrimary: boolean;
    variantId: string | null;
}

export interface SaveProductMediaRequest {
    url: string;
    mediaType: 'image' | 'video';
    altText?: string | null;
    fileName?: string | null;
    isPrimary?: boolean;
    variantId?: string | null;
}

/** Gallery CRUD for a part's media (backed by /api/v1/products/{partId}/media). */
@Injectable({ providedIn: 'root' })
export class ProductMediaService {
    private readonly http = inject(HttpClient);

    private baseUrl(partId: string): string {
        return `${environment.apiUrl}/v1/products/${partId}/media`;
    }

    getByPart(partId: string): Observable<ProductMedia[]> {
        return this.http.get<{ data: ProductMedia[] }>(this.baseUrl(partId)).pipe(map((r) => r.data));
    }

    add(partId: string, request: SaveProductMediaRequest): Observable<ProductMedia> {
        return this.http.post<{ data: ProductMedia }>(this.baseUrl(partId), request).pipe(map((r) => r.data));
    }

    update(partId: string, mediaId: string, request: SaveProductMediaRequest): Observable<ProductMedia> {
        return this.http.put<{ data: ProductMedia }>(`${this.baseUrl(partId)}/${mediaId}`, request).pipe(map((r) => r.data));
    }

    setPrimary(partId: string, mediaId: string): Observable<ProductMedia> {
        return this.http.patch<{ data: ProductMedia }>(`${this.baseUrl(partId)}/${mediaId}/primary`, {}).pipe(map((r) => r.data));
    }

    /** SortOrder is assigned by position in orderedIds. Returns the reordered gallery. */
    reorder(partId: string, orderedIds: string[]): Observable<ProductMedia[]> {
        return this.http.put<{ data: ProductMedia[] }>(`${this.baseUrl(partId)}/order`, { orderedIds }).pipe(map((r) => r.data));
    }

    /** Removes the row and, when the URL is an uploaded file, the blob behind it. */
    delete(partId: string, mediaId: string): Observable<void> {
        return this.http.delete<void>(`${this.baseUrl(partId)}/${mediaId}`);
    }
}
