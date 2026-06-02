import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from 'src/environments/environment';

export interface CreditNoteResponse {
  id: string;
  creditNoteNumber: string;
  supplierId: string;
  supplierName: string;
  purchaseReturnId?: string;
  returnNumber?: string;
  purchaseOrderId?: string;
  purchaseOrderNumber?: string;
  totalAmount: number;
  usedAmount: number;
  availableAmount: number;
  currency: string;
  issueDate: string;
  expiryDate?: string;
  status: string;
  notes: string;
  issuedBy: string;
  createdBy: string;
  createdAt: string;
}

export interface ApplyCreditNoteRequest {
  creditNoteId: string;
  purchaseOrderId: string;
  amountToApply: number;
}

export interface CreditNoteListQuery {
  supplierId?: string;
  status?: string;
  pageNumber: number;
  pageSize: number;
}

export interface PaginatedCreditNotesResponse {
  data: CreditNoteResponse[];
  pagination: {
    pageNumber: number;
    pageSize: number;
    totalCount: number;
    totalPages: number;
  };
}

@Injectable({
  providedIn: 'root'
})
export class CreditNoteService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/v1/creditnote`;

  /**
   * Get all credit notes for a supplier
   */
  getBySupplier(supplierId: string): Observable<CreditNoteResponse[]> {
    return this.http.get<CreditNoteResponse[]>(`${this.apiUrl}/supplier/${supplierId}`);
  }

  /**
   * Get available (usable) credit notes for a supplier
   */
  getAvailableCredits(supplierId: string): Observable<CreditNoteResponse[]> {
    return this.http.get<CreditNoteResponse[]>(`${this.apiUrl}/supplier/${supplierId}/available`);
  }

  /**
   * Get total available credit for a supplier
   */
  getTotalAvailableCredit(supplierId: string): Observable<{ totalAvailableCredit: number }> {
    return this.http.get<{ totalAvailableCredit: number }>(`${this.apiUrl}/supplier/${supplierId}/total-available`);
  }

  /**
   * Get paginated list of credit notes
   */
  getList(query: CreditNoteListQuery): Observable<PaginatedCreditNotesResponse> {
    let params = new HttpParams()
      .set('pageNumber', query.pageNumber.toString())
      .set('pageSize', query.pageSize.toString());

    if (query.supplierId) {
      params = params.set('supplierId', query.supplierId);
    }
    if (query.status) {
      params = params.set('status', query.status);
    }

    return this.http.get<PaginatedCreditNotesResponse>(`${this.apiUrl}/list`, { params });
  }

  /**
   * Get credit note by ID
   */
  getById(id: string): Observable<CreditNoteResponse> {
    return this.http.get<CreditNoteResponse>(`${this.apiUrl}/${id}`);
  }

  /**
   * Apply credit note to a purchase order
   */
  applyCredit(request: ApplyCreditNoteRequest): Observable<CreditNoteResponse> {
    return this.http.post<CreditNoteResponse>(`${this.apiUrl}/apply`, request);
  }

  /**
   * Cancel a credit note
   */
  cancel(id: string, reason?: string): Observable<void> {
    const params = reason ? new HttpParams().set('reason', reason) : undefined;
    return this.http.patch<void>(`${this.apiUrl}/${id}/cancel`, null, { params });
  }
}
