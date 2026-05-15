import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, catchError, map, of } from 'rxjs';
import { environment } from 'src/environments/environment';
import {
  CatalogLandingResponse,
  CatalogFilterResponse,
  CatalogProductListItem,
  CatalogProductDetail,
  CatalogSearchRequest,
  CatalogCategory,
} from '../models/catalog.model';

@Injectable({ providedIn: 'root' })
export class CatalogService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/catalog`;

  getLanding(): Observable<CatalogLandingResponse> {
    return this.http.get<CatalogLandingResponse>(`${this.baseUrl}/landing`).pipe(
      catchError(() => of({ categories: [], featured: [], popular: [], latest: [] }))
    );
  }

  getCategories(): Observable<CatalogCategory[]> {
    return this.getLanding().pipe(
      map(r => r.categories),
      catchError(() => of([]))
    );
  }

  getSaleProducts(pageNumber = 1, pageSize = 12): Observable<{
    items: CatalogProductListItem[];
    pageNumber: number;
    pageSize: number;
    totalCount: number;
  }> {
    return this.searchProducts({
      search: '',
      pageNumber,
      pageSize,
      includeDescendants: true,
      inStockOnly: false,
      onSaleOnly: true,
      attributeFilters: [],
    }).pipe(
      catchError(() => of({ items: [], pageNumber, pageSize, totalCount: 0 }))
    );
  }

  getFilters(categoryId: string, includeDescendants = true): Observable<CatalogFilterResponse> {
    return this.http
      .get<CatalogFilterResponse>(
        `${this.baseUrl}/categories/${categoryId}/filters?includeDescendants=${includeDescendants}`
      )
      .pipe(
        catchError(() =>
          of({
            categoryId,
            filters: [],
            priceRange: { min: null, max: null, currency: 'BDT' },
            availability: { inStockAvailable: true },
          })
        )
      );
  }

  searchProducts(request: CatalogSearchRequest): Observable<{
    items: CatalogProductListItem[];
    pageNumber: number;
    pageSize: number;
    totalCount: number;
  }> {
    return this.http
      .post<{ items: CatalogProductListItem[]; pageNumber: number; pageSize: number; totalCount: number }>(
        `${this.baseUrl}/products/search`,
        request
      )
      .pipe(
        catchError(() =>
          of({ items: [], pageNumber: request.pageNumber, pageSize: request.pageSize, totalCount: 0 })
        )
      );
  }

  getProductDetail(partId: string): Observable<CatalogProductDetail | null> {
    return this.http.get<CatalogProductDetail>(`${this.baseUrl}/products/${partId}`).pipe(
      catchError(() => of(null))
    );
  }
}
