import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from 'src/environments/environment';
import { PaginatedResponse } from './stock-lot.service';

export interface StockTakeResponse {
  id: string;
  stockTakeNumber: string;
  warehouseId: string;
  warehouseName: string;
  categoryId?: string | null;
  categoryName: string;
  status: 'COUNTING' | 'REVIEW' | 'COMPLETED' | 'CANCELLED';
  snapshotDate: string;
  submittedDate?: string | null;
  completedDate?: string | null;
  completedBy: string;
  notes: string;
  totalLines: number;
  countedLines: number;
  varianceLines: number;
  totalVarianceValue: number;
  createdBy: string;
  createdDate: string;
}

export interface StockTakeLineResponse {
  id: string;
  partId: string;
  variantId?: string | null;
  partName: string;
  partCode: string;
  variantName: string;
  location: string;
  expectedQuantity: number;
  countedQuantity: number | null;
  variance: number | null;
  unitCost: number;
  varianceValue: number | null;
  countedBy: string;
  countedAt?: string | null;
  notes: string;
}

export interface StockTakeDetailResponse extends StockTakeResponse {
  lines: StockTakeLineResponse[];
}

export interface CreateStockTakeRequest {
  warehouseId: string;
  categoryId?: string | null;
  notes?: string;
}

export interface StockTakeCountEntry {
  lineId: string;
  countedQuantity: number | null; // null clears a recorded count
  notes?: string;
}

export interface ApproveStockTakeResponse {
  id: string;
  stockTakeNumber: string;
  adjustmentsApplied: number;
  linesUnchanged: number;
  linesSkippedUncounted: number;
  totalVarianceValue: number;
  lotSyncWarnings: string[];
}

export interface StockTakeListQuery {
  pageNumber: number;
  pageSize: number;
  status?: string | null;
  warehouseId?: string | null;
  search?: string;
}

@Injectable({ providedIn: 'root' })
export class StockTakeService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/v1/stocktake`;

  getStockTakes(query: StockTakeListQuery): Observable<PaginatedResponse<StockTakeResponse>> {
    let params = new HttpParams()
      .set('pageNumber', query.pageNumber)
      .set('pageSize', query.pageSize);
    if (query.status) params = params.set('status', query.status);
    if (query.warehouseId) params = params.set('warehouseId', query.warehouseId);
    if (query.search) params = params.set('search', query.search);
    return this.http.get<PaginatedResponse<StockTakeResponse>>(this.apiUrl, { params });
  }

  getById(id: string): Observable<StockTakeDetailResponse> {
    return this.http.get<StockTakeDetailResponse>(`${this.apiUrl}/${id}`);
  }

  create(request: CreateStockTakeRequest): Observable<StockTakeResponse> {
    return this.http.post<StockTakeResponse>(this.apiUrl, request);
  }

  recordCounts(id: string, counts: StockTakeCountEntry[]): Observable<{ message: string }> {
    return this.http.put<{ message: string }>(`${this.apiUrl}/${id}/counts`, { counts });
  }

  submit(id: string): Observable<{ id: string; status: string }> {
    return this.http.post<{ id: string; status: string }>(`${this.apiUrl}/${id}/submit`, {});
  }

  reopen(id: string): Observable<{ id: string; status: string }> {
    return this.http.post<{ id: string; status: string }>(`${this.apiUrl}/${id}/reopen`, {});
  }

  approve(id: string): Observable<ApproveStockTakeResponse> {
    return this.http.post<ApproveStockTakeResponse>(`${this.apiUrl}/${id}/approve`, {});
  }

  cancel(id: string): Observable<{ id: string; status: string }> {
    return this.http.post<{ id: string; status: string }>(`${this.apiUrl}/${id}/cancel`, {});
  }
}
