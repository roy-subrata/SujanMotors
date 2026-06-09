import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { environment } from 'src/environments/environment';

export interface PurchaseReturnLineResponse {
  id: string;
  partId: string;
  partName?: string;
  partSku?: string;
  variantName?: string | null;
  displayName?: string;
  stockLotId?: string;
  lotNumber?: string;
  quantity: number;
  rejectedQuantity: number;
  unitPrice: number;
  refundAmount: number;
  condition: string; // UNOPENED, OPENED, DAMAGED, DEFECTIVE
  notes?: string;
}

export interface AvailableLotForReturn {
  lotId: string;
  lotNumber: string;
  partId: string;
  partName: string;
  partSku: string;
  supplierId: string;
  supplierName: string;
  quantityAvailable: number;
  costPrice: number;
  receivingDate: string;
  expiryDate?: string;
  isFromSameSupplier: boolean;
  status: string; // AVAILABLE, DAMAGED, QUARANTINE - which inventory bucket the lot belongs to
}

/** Draft payload to pre-fill a new return from an accepted Goods Receipt's damaged/wrong lines. */
export interface ReturnPrefillFromGrn {
  goodsReceiptId: string;
  grnNumber: string;
  purchaseOrderId: string;
  purchaseOrderNumber?: string;
  supplierId: string;
  supplierName?: string;
  reason: string;
  lines: ReturnPrefillLine[];
}

export interface ReturnPrefillLine {
  purchaseOrderLineId: string;
  partId: string;
  displayName: string;
  partSku: string;
  stockLotId?: string;
  lotNumber?: string;
  bucket: string; // DAMAGED or QUARANTINE
  quantity: number;
  unitPrice: number;
  condition: string;
  notes: string;
}

export interface PurchaseReturnResponse {
  id: string;
  returnNumber: string;
  purchaseOrderId: string;
  purchaseOrderNumber?: string;
  supplierId: string;
  supplierName?: string;
  supplierCode?: string;
  returnDate: string;
  reason: string;
  status: string; // PENDING, APPROVED, RETURNED, RECEIVED, REJECTED, CREDITED
  refundAmount: number;
  creditNoteAmount: number;
  notes?: string;
  approvedBy?: string;
  approvedDate?: string;
  receivedDate?: string;
  receivedBy?: string;
  lines: PurchaseReturnLineResponse[];
  createdAt: string;
}

export interface CreatePurchaseReturnLineRequest {
  purchaseOrderLineId: string;
  partId: string;
  stockLotId?: string; // Optional: specific lot to return from
  quantity: number;
  rejectedQuantity: number;
  unitPrice: number;
  condition: string; // UNOPENED, OPENED, DAMAGED, DEFECTIVE
  notes?: string;
}

export interface CreatePurchaseReturnRequest {
  purchaseOrderId: string;
  supplierId: string;
  returnDate: string;
  reason: string;
  notes?: string;
  lines: CreatePurchaseReturnLineRequest[];
}

export interface UpdatePurchaseReturnRequest {
  id: string;
  purchaseOrderId: string;
  supplierId: string;
  returnDate: string;
  reason: string;
  notes?: string;
  lines: CreatePurchaseReturnLineRequest[];
}

export interface PaginatedPurchaseReturnResponse {
  items: PurchaseReturnResponse[];
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
export class PurchaseReturnService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/v1/purchasereturn`;

  /**
   * Get all purchase returns
   */
  getAllPurchaseReturns(): Observable<PurchaseReturnResponse[]> {
    return this.http.get<PurchaseReturnResponse[]>(this.apiUrl);
  }

  /**
   * Get paginated purchase returns with optional search
   */
  getPurchaseReturns(pageNumber: number, pageSize: number, searchTerm?: string): Observable<PaginatedPurchaseReturnResponse> {
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
   * Get purchase return by ID
   */
  getPurchaseReturnById(id: string): Observable<PurchaseReturnResponse> {
    return this.http.get<PurchaseReturnResponse>(`${this.apiUrl}/${id}`);
  }

  /**
   * Get purchase return by return number
   */
  getPurchaseReturnByNumber(returnNumber: string): Observable<PurchaseReturnResponse> {
    return this.http.get<PurchaseReturnResponse>(`${this.apiUrl}/number/${returnNumber}`);
  }

  /**
   * Get purchase returns by purchase order
   */
  getPurchaseReturnsByPurchaseOrder(purchaseOrderId: string): Observable<PurchaseReturnResponse[]> {
    return this.http.get<PurchaseReturnResponse[]>(`${this.apiUrl}/purchase-order/${purchaseOrderId}`);
  }

  /**
   * Get purchase returns by supplier
   */
  getPurchaseReturnsBySupplier(supplierId: string): Observable<PurchaseReturnResponse[]> {
    return this.http.get<PurchaseReturnResponse[]>(`${this.apiUrl}/supplier/${supplierId}`);
  }

  /**
   * Get purchase returns by status
   */
  getPurchaseReturnsByStatus(status: string): Observable<PurchaseReturnResponse[]> {
    return this.http.get<PurchaseReturnResponse[]>(`${this.apiUrl}/status/${status}`);
  }

  /**
   * Get pending approvals
   */
  getPendingApprovals(): Observable<PurchaseReturnResponse[]> {
    return this.http.get<PurchaseReturnResponse[]>(`${this.apiUrl}/pending-approvals`);
  }

  /**
   * Create new purchase return
   */
  createPurchaseReturn(request: CreatePurchaseReturnRequest): Observable<PurchaseReturnResponse> {
    return this.http.post<PurchaseReturnResponse>(this.apiUrl, request);
  }

  /**
   * Update purchase return
   */
  updatePurchaseReturn(id: string, request: UpdatePurchaseReturnRequest): Observable<PurchaseReturnResponse> {
    return this.http.put<PurchaseReturnResponse>(`${this.apiUrl}/${id}`, request);
  }

  /**
   * Approve purchase return
   */
  approvePurchaseReturn(id: string): Observable<PurchaseReturnResponse> {
    return this.http.patch<PurchaseReturnResponse>(`${this.apiUrl}/${id}/approve`, {});
  }

  /**
   * Mark purchase return as returned
   */
  markAsReturned(id: string): Observable<PurchaseReturnResponse> {
    return this.http.patch<PurchaseReturnResponse>(`${this.apiUrl}/${id}/mark-returned`, {});
  }

  /**
   * Mark purchase return as received
   */
  markAsReceived(id: string): Observable<PurchaseReturnResponse> {
    return this.http.patch<PurchaseReturnResponse>(`${this.apiUrl}/${id}/mark-received`, {});
  }

  /**
   * Issue credit note for purchase return
   */
  issueCreditNote(id: string, creditNoteAmount: number): Observable<PurchaseReturnResponse> {
    return this.http.patch<PurchaseReturnResponse>(`${this.apiUrl}/${id}/issue-credit-note?creditAmount=${creditNoteAmount}`, {});
  }

  /**
   * Reject purchase return
   */
  rejectPurchaseReturn(id: string, reason: string): Observable<PurchaseReturnResponse> {
    return this.http.patch<PurchaseReturnResponse>(`${this.apiUrl}/${id}/reject?reason=${encodeURIComponent(reason)}`, {});
  }

  /**
   * Delete purchase return
   */
  deletePurchaseReturn(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }

  /**
   * Get available stock lots for a part (for return selection)
   * @param partId The part ID
   * @param supplierId Optional supplier ID to prioritize lots from same supplier
   * @param bucket Optional inventory bucket filter (AVAILABLE, DAMAGED, QUARANTINE)
   */
  getAvailableLotsForReturn(partId: string, supplierId?: string, bucket?: string): Observable<AvailableLotForReturn[]> {
    let params = new HttpParams();
    if (supplierId) {
      params = params.set('supplierId', supplierId);
    }
    if (bucket) {
      params = params.set('bucket', bucket);
    }
    return this.http.get<AvailableLotForReturn[]>(`${this.apiUrl}/available-lots/${partId}`, { params });
  }

  /**
   * Build a draft return from an accepted Goods Receipt's remaining damaged/wrong units.
   */
  getReturnPrefillFromGrn(goodsReceiptId: string): Observable<ReturnPrefillFromGrn> {
    return this.http.get<ReturnPrefillFromGrn>(`${this.apiUrl}/from-goods-receipt/${goodsReceiptId}`);
  }
}
