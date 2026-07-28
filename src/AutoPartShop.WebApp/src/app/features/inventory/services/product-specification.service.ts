import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { environment } from 'src/environments/environment';

/** Mirrors the API's product specification projection. */
export interface ProductSpecification {
    id: string;
    label: string;
    /** Normalized slug of the label ("Front Axle" -> "front-axle"); assigned server-side. */
    key: string;
    value: string;
    displayOrder: number;
}

export interface SaveSpecificationItem {
    label: string;
    value: string;
}

/**
 * Simple product-level Label/Value specs (backed by /api/v1/products/{partId}/specifications).
 * These are product-scoped free text — not the variant attribute EAV.
 */
@Injectable({ providedIn: 'root' })
export class ProductSpecificationService {
    private readonly http = inject(HttpClient);
    private readonly baseUrl = `${environment.apiUrl}/v1/products`;

    getByPart(partId: string): Observable<ProductSpecification[]> {
        return this.http
            .get<{ data: ProductSpecification[] }>(`${this.baseUrl}/${partId}/specifications`)
            .pipe(map((r) => r.data));
    }

    /** Full replace — display order follows array position. Returns the saved list. */
    save(partId: string, specifications: SaveSpecificationItem[]): Observable<ProductSpecification[]> {
        return this.http
            .put<{ data: ProductSpecification[] }>(`${this.baseUrl}/${partId}/specifications`, { specifications })
            .pipe(map((r) => r.data));
    }

    /**
     * Typeahead over specs already used across the catalog, so staff converge on
     * consistent terms. `labelKey` scopes value suggestions to one label.
     */
    suggestions(field: 'label' | 'value', query: string, labelKey?: string): Observable<string[]> {
        let params = new HttpParams().set('field', field).set('query', query ?? '');
        if (labelKey) params = params.set('labelKey', labelKey);
        return this.http
            .get<{ data: string[] }>(`${this.baseUrl}/specifications/suggestions`, { params })
            .pipe(map((r) => r.data));
    }
}
