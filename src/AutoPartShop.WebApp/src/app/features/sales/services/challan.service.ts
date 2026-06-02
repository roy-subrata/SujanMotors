import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { environment } from 'src/environments/environment';

export interface ChallanLineResponse {
  id: string;
  partId: string;
  productVariantId?: string | null;
  partName: string;
  partSku: string;
  displayName: string;
  unitName: string;
  quantity: number;
  lineNumber: number;
}

export interface ChallanResponse {
  id: string;
  challanNumber: string;
  salesOrderId: string;
  salesOrderNumber?: string | null;
  invoiceId?: string | null;
  status: 'DRAFT' | 'ISSUED' | 'DELIVERED';
  issuedAt?: string | null;
  deliveredAt?: string | null;
  deliveryAddress: string;
  receiverName: string;
  receiverPhone: string;
  notes: string;
  transportCompany: string;
  vehicleNumber: string;
  driverName: string;
  driverPhone: string;
  createdAt: string;
  createdBy: string;
  lines: ChallanLineResponse[];
}

export interface GenerateChallanRequest {
  deliveryAddress?: string;
  receiverName?: string;
  receiverPhone?: string;
  notes?: string;
  transportCompany?: string;
  vehicleNumber?: string;
  driverName?: string;
  driverPhone?: string;
}

export interface DeliverChallanRequest {
  receiverName?: string;
  receiverPhone?: string;
}

@Injectable({ providedIn: 'root' })
export class ChallanService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/v1/challans`;

  generate(salesOrderId: string, req: GenerateChallanRequest = {}): Observable<ChallanResponse> {
    return this.http.post<{ data: ChallanResponse }>(`${this.apiUrl}/sales-order/${salesOrderId}`, req)
      .pipe(map(r => r.data));
  }

  getBySalesOrder(salesOrderId: string): Observable<ChallanResponse[]> {
    return this.http.get<{ data: ChallanResponse[] }>(`${this.apiUrl}/sales-order/${salesOrderId}`)
      .pipe(map(r => r.data));
  }

  getById(id: string): Observable<ChallanResponse> {
    return this.http.get<{ data: ChallanResponse }>(`${this.apiUrl}/${id}`)
      .pipe(map(r => r.data));
  }

  getPending(): Observable<ChallanResponse[]> {
    return this.http.get<{ data: ChallanResponse[] }>(`${this.apiUrl}/pending`)
      .pipe(map(r => r.data));
  }

  issue(id: string): Observable<ChallanResponse> {
    return this.http.patch<{ data: ChallanResponse }>(`${this.apiUrl}/${id}/issue`, {})
      .pipe(map(r => r.data));
  }

  markDelivered(id: string, req: DeliverChallanRequest = {}): Observable<ChallanResponse> {
    return this.http.patch<{ data: ChallanResponse }>(`${this.apiUrl}/${id}/deliver`, req)
      .pipe(map(r => r.data));
  }
}
