import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { environment } from '../../../../environments/environment';
import {
  CatalogFilterResponse,
  CatalogLandingResponse,
  CatalogProductDetail,
  CatalogProductListItem,
  CatalogSearchRequest
} from '../models/catalog.model';

@Injectable({ providedIn: 'root' })
export class CatalogService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/catalog`;

  getLanding() {
    return this.http.get<CatalogLandingResponse>(`${this.baseUrl}/landing`);
  }

  getFilters(categoryId: string, includeDescendants = true) {
    return this.http.get<CatalogFilterResponse>(
      `${this.baseUrl}/categories/${categoryId}/filters?includeDescendants=${includeDescendants}`
    );
  }

  searchProducts(request: CatalogSearchRequest) {
    return this.http.post<{ items: CatalogProductListItem[]; pageNumber: number; pageSize: number; totalCount: number }>(
      `${this.baseUrl}/products/search`,
      request
    );
  }

  getProductDetail(partId: string) {
    return this.http.get<CatalogProductDetail>(`${this.baseUrl}/products/${partId}`);
  }
}
