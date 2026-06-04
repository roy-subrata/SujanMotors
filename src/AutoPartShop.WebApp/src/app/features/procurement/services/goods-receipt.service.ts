import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { environment } from 'src/environments/environment';

export interface GoodsReceiptLineResponse {
  id: string;
  purchaseOrderLineId: string;
  partId: string;
  partName: string;
  partSKU: string;
  orderedQuantity: number;
  receivedQuantity: number;
  rejectedQuantity: number;
  acceptedQuantity: number;
  condition: string;
  notes: string;
  hasDiscrepancy: boolean;
  // Batch / lot identification
  batchNumber?: string | null;
  expiryDate?: string | null;
  // Cost Information
  unitCost: number;
  currency: string;
  unitId?: string;
  totalCost: number;
  acceptedTotalCost: number;
  // Lot-level selling price & warranty overrides
  sellingPrice?: number | null;
  hasWarranty?: boolean | null;
  warrantyPeriodMonths?: number | null;
  warrantyType?: string | null;
  warrantyTerms?: string | null;
}

export interface GoodsReceiptResponse {
  id: string;
  grnNumber: string;
  purchaseOrderId: string;
  poNumber?: string;
  warehouseId: string;
  warehouseName?: string;
  receivedDate: string;
  status: string; // PENDING, VERIFIED, ACCEPTED, REJECTED
  notes: string;
  totalItemsReceived: number;
  discrepancyCount: number;
  verifiedBy: string;
  verificationDate?: string;
  lines: GoodsReceiptLineResponse[];
  createdAt: string;
  // Supplier Invoice
  supplierInvoiceNumber: string;
  supplierInvoiceDate?: string;
  invoiceNotProvided: boolean;
  // Delivery Information
  deliveryDate?: string;
  deliveryReference: string;
  carrierName: string;
  driverName: string;
  deliveryNotes: string;
}

export interface CreateGoodsReceiptRequest {
  purchaseOrderId: string;
  warehouseId: string;
  receivedDate: string;
  lines: CreateGoodsReceiptLineRequest[];
  // Supplier Invoice
  supplierInvoiceNumber?: string | null;
  supplierInvoiceDate?: string | null;
  invoiceNotProvided?: boolean;
  // Delivery Information
  deliveryDate?: string;
  deliveryReference?: string;
  carrierName?: string;
  driverName?: string;
  deliveryNotes?: string;
}

export interface CreateGoodsReceiptLineRequest {
  partId: string;
  purchaseOrderLineId?: string | null;
  receivedQuantity: number;
  condition: string;
  notes?: string;
  hasDiscrepancy?: boolean;
  // Batch / lot identification
  batchNumber?: string | null;
  expiryDate?: string | null;
  // Cost Information
  unitCost: number;
  currency: string;
  unitId?: string | null;
  // Lot-level selling price & warranty (optional overrides — defaults to Part master if omitted)
  sellingPrice?: number | null;
  hasWarranty?: boolean | null;
  warrantyPeriodMonths?: number | null;
  warrantyType?: string | null;
  warrantyTerms?: string | null;
}

export interface PaginatedGoodsReceiptResponse {
  items: GoodsReceiptResponse[];
  pageNumber: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

@Injectable({
  providedIn: 'root'
})
export class GoodsReceiptService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/v1/purchaseorder/grn`;

  /**
   * Get all goods receipts
   */
  getAllGoodsReceipts(): Observable<GoodsReceiptResponse[]> {
    return this.http.get<GoodsReceiptResponse[]>(this.apiUrl);
  }

  /**
   * Get paginated goods receipts with optional search
   */
  getGoodsReceipts(pageNumber: number, pageSize: number, searchTerm?: string): Observable<PaginatedGoodsReceiptResponse> {
    let params = new HttpParams()
      .set('pageNumber', pageNumber.toString())
      .set('pageSize', pageSize.toString());

    if (searchTerm) {
      params = params.set('searchTerm', searchTerm);
    }

    return this.http.get<any>(`${this.apiUrl}/list`, { params }).pipe(
      map(response => ({
        items: response.data,
        pageNumber: response.pagination.pageNumber,
        pageSize: response.pagination.pageSize,
        totalCount: response.pagination.totalCount,
        totalPages: response.pagination.totalPages,
        hasPreviousPage: response.pagination.pageNumber > 1,
        hasNextPage: response.pagination.pageNumber < response.pagination.totalPages
      }))
    );
  }

  /**
   * Get goods receipt by ID
   */
  getGoodsReceiptById(id: string): Observable<GoodsReceiptResponse> {
    return this.http.get<GoodsReceiptResponse>(`${this.apiUrl}/${id}`);
  }

  /**
   * Get goods receipt by GRN Number
   */
  getGoodsReceiptByNumber(grnNumber: string): Observable<GoodsReceiptResponse> {
    return this.http.get<GoodsReceiptResponse>(`${this.apiUrl}/number/${grnNumber}`);
  }

  /**
   * Get goods receipts by purchase order
   */
  getGoodsReceiptsByPurchaseOrder(purchaseOrderId: string): Observable<GoodsReceiptResponse[]> {
    return this.http.get<GoodsReceiptResponse[]>(`${this.apiUrl}/purchase-order/${purchaseOrderId}`);
  }

  /**
   * Get goods receipts by warehouse
   */
  getGoodsReceiptsByWarehouse(warehouseId: string): Observable<GoodsReceiptResponse[]> {
    return this.http.get<GoodsReceiptResponse[]>(`${this.apiUrl}/warehouse/${warehouseId}`);
  }

  /**
   * Get goods receipts by status
   */
  getGoodsReceiptsByStatus(status: string): Observable<GoodsReceiptResponse[]> {
    return this.http.get<GoodsReceiptResponse[]>(`${this.apiUrl}/status/${status}`);
  }

  /**
   * Create new goods receipt
   */
  createGoodsReceipt(request: CreateGoodsReceiptRequest): Observable<GoodsReceiptResponse> {
    return this.http.post<GoodsReceiptResponse>(this.apiUrl, request);
  }

  /**
   * Update goods receipt
   */
  updateGoodsReceipt(id: string, request: CreateGoodsReceiptRequest): Observable<GoodsReceiptResponse> {
    return this.http.put<GoodsReceiptResponse>(`${this.apiUrl}/${id}`, request);
  }

  /**
   * Verify goods receipt
   */
  verifyGoodsReceipt(id: string, verifiedBy: string): Observable<GoodsReceiptResponse> {
    return this.http.patch<GoodsReceiptResponse>(`${this.apiUrl}/${id}/verify?verifiedBy=${verifiedBy}`, {});
  }

  /**
   * Accept goods receipt
   */
  acceptGoodsReceipt(id: string): Observable<GoodsReceiptResponse> {
    return this.http.patch<GoodsReceiptResponse>(`${this.apiUrl}/${id}/accept`, {});
  }

  /**
   * Reject goods receipt
   */
  rejectGoodsReceipt(id: string, reason: string = ''): Observable<GoodsReceiptResponse> {
    let url = `${this.apiUrl}/${id}/reject`;
    if (reason) {
      url += `?reason=${encodeURIComponent(reason)}`;
    }
    return this.http.patch<GoodsReceiptResponse>(url, {});
  }

  /**
   * Delete goods receipt
   */
  deleteGoodsReceipt(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }
}
