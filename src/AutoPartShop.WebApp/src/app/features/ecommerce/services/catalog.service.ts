import { inject, Injectable } from '@angular/core';
import { CatalogSearchRequest } from '../models/catalog.model';
import { MockCatalogService } from './mock-catalog.service';

/**
 * Catalog service - currently delegates to MockCatalogService for in-memory data.
 * TODO: Replace MockCatalogService calls with real HTTP calls when backend is ready.
 *
 * To switch to real API:
 *   1. Import HttpClient
 *   2. Replace mock.xxx() calls with this.http.get/post(...)
 *   3. Remove MockCatalogService import
 */
@Injectable({ providedIn: 'root' })
export class CatalogService {
  private readonly mock = inject(MockCatalogService);

  getLanding() {
    return this.mock.getLanding();
    // return this.http.get<CatalogLandingResponse>(`${this.baseUrl}/landing`);
  }

  getCategories() {
    return this.mock.getCategories();
    // return this.http.get<CatalogCategory[]>(`${this.baseUrl}/categories`);
  }

  getSaleProducts() {
    return this.mock.getSaleProducts();
    // return this.http.get<CatalogProductListItem[]>(`${this.baseUrl}/products/sale`);
  }

  getFilters(categoryId: string, _includeDescendants = true) {
    return this.mock.getFilters(categoryId);
    // return this.http.get<CatalogFilterResponse>(`${this.baseUrl}/categories/${categoryId}/filters?includeDescendants=${includeDescendants}`);
  }

  searchProducts(request: CatalogSearchRequest) {
    return this.mock.searchProducts(request);
    // return this.http.post<{...}>(`${this.baseUrl}/products/search`, request);
  }

  getProductDetail(partId: string) {
    return this.mock.getProductDetail(partId);
    // return this.http.get<CatalogProductDetail>(`${this.baseUrl}/products/${partId}`);
  }
}
