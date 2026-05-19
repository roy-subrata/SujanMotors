import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { environment } from 'src/environments/environment';

export interface ProductLocationResponse {
  id: string;
  partId: string;
  partName: string;
  partSKU: string;
  warehouseId: string;
  warehouseName: string;
  warehouseCode: string;
  section: string;
  shelf: string;
  fullLocation: string;
  notes: string;
  isPrimary: boolean;
  createdBy: string;
  createdAt: string;
}

export interface CreateProductLocationRequest {
  partId: string;
  warehouseId: string;
  section: string;
  shelf: string;
  isPrimary: boolean;
  notes?: string;
}

export interface UpdateProductLocationRequest {
  section: string;
  shelf: string;
  isPrimary: boolean;
  notes?: string;
}

@Injectable({ providedIn: 'root' })
export class ProductLocationService {
  private readonly http = inject(HttpClient);

  private baseUrl(productId: string): string {
    return `${environment.apiUrl}/v1/products/${productId}/locations`;
  }

  getLocationsByPart(partId: string): Observable<ProductLocationResponse[]> {
    return this.http.get<{ data: ProductLocationResponse[] }>(this.baseUrl(partId))
      .pipe(map(r => r.data));
  }

  getPrimaryLocation(partId: string): Observable<ProductLocationResponse> {
    return this.http.get<{ data: ProductLocationResponse }>(`${this.baseUrl(partId)}/primary`)
      .pipe(map(r => r.data));
  }

  getLocationById(partId: string, id: string): Observable<ProductLocationResponse> {
    return this.http.get<{ data: ProductLocationResponse }>(`${this.baseUrl(partId)}/${id}`)
      .pipe(map(r => r.data));
  }

  createLocation(request: CreateProductLocationRequest): Observable<ProductLocationResponse> {
    const { partId, ...body } = request;
    return this.http.post<{ data: ProductLocationResponse }>(this.baseUrl(partId), body)
      .pipe(map(r => r.data));
  }

  updateLocation(partId: string, id: string, request: UpdateProductLocationRequest): Observable<ProductLocationResponse> {
    return this.http.put<{ data: ProductLocationResponse }>(`${this.baseUrl(partId)}/${id}`, request)
      .pipe(map(r => r.data));
  }

  setPrimaryLocation(partId: string, locationId: string): Observable<void> {
    return this.http.patch<void>(`${this.baseUrl(partId)}/${locationId}/set-primary`, {});
  }

  deleteLocation(partId: string, id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl(partId)}/${id}`);
  }
}
