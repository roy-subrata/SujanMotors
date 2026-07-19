import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from 'src/environments/environment';
import { PdfDownloadService } from '@/shared/services/pdf-download.service';

export interface CustomerCreditNoteResponse {
  id: string;
  creditNoteNumber: string;
  customerId: string;
  customerName: string;
  salesReturnId?: string;
  returnNumber?: string;
  invoiceId?: string;
  invoiceNumber?: string;
  salesOrderId?: string;
  salesOrderNumber?: string;
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

export interface ApplyCustomerCreditNoteRequest {
  creditNoteId: string;
  invoiceId: string;
  salesOrderId: string;
  amountToApply: number;
}

export interface CustomerCreditNoteListQuery {
  customerId?: string;
  status?: string;
  pageNumber: number;
  pageSize: number;
}

export interface PaginatedCustomerCreditNotesResponse {
  data: CustomerCreditNoteResponse[];
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
export class CustomerCreditNoteService {
  private readonly http = inject(HttpClient);
  private readonly pdfDownload = inject(PdfDownloadService);
  private readonly apiUrl = `${environment.apiUrl}/v1/customer-credit-notes`;

  /**
   * Get all credit notes for a customer
   */
  getByCustomer(customerId: string): Observable<CustomerCreditNoteResponse[]> {
    return this.http.get<CustomerCreditNoteResponse[]>(`${this.apiUrl}/customer/${customerId}`);
  }

  /**
   * Get available (usable) credit notes for a customer
   */
  getAvailableCredits(customerId: string): Observable<CustomerCreditNoteResponse[]> {
    return this.http.get<CustomerCreditNoteResponse[]>(`${this.apiUrl}/customer/${customerId}/available`);
  }

  /**
   * Get total available credit for a customer
   */
  getTotalAvailableCredit(customerId: string): Observable<{ totalAvailableCredit: number }> {
    return this.http.get<{ totalAvailableCredit: number }>(`${this.apiUrl}/customer/${customerId}/total-available`);
  }

  /**
   * Get paginated list of credit notes
   */
  getList(query: CustomerCreditNoteListQuery): Observable<PaginatedCustomerCreditNotesResponse> {
    let params = new HttpParams()
      .set('pageNumber', query.pageNumber.toString())
      .set('pageSize', query.pageSize.toString());

    if (query.customerId) {
      params = params.set('customerId', query.customerId);
    }
    if (query.status) {
      params = params.set('status', query.status);
    }

    return this.http.get<PaginatedCustomerCreditNotesResponse>(`${this.apiUrl}/list`, { params });
  }

  /**
   * Get credit note by ID
   */
  getById(id: string): Observable<CustomerCreditNoteResponse> {
    return this.http.get<CustomerCreditNoteResponse>(`${this.apiUrl}/${id}`);
  }

  /**
   * Apply credit note to a sales order/invoice
   */
  applyCredit(request: ApplyCustomerCreditNoteRequest): Observable<CustomerCreditNoteResponse> {
    return this.http.post<CustomerCreditNoteResponse>(`${this.apiUrl}/apply`, request);
  }

  /**
   * Cancel a credit note
   */
  cancel(id: string, reason?: string): Observable<void> {
    const params = reason ? new HttpParams().set('reason', reason) : undefined;
    return this.http.patch<void>(`${this.apiUrl}/${id}/cancel`, null, { params });
  }

  /** Download the server-rendered Credit Note PDF and trigger the browser save dialog. */
  downloadPdf(id: string, creditNoteNumber: string): Observable<void> {
    return this.pdfDownload.downloadGet(`${this.apiUrl}/${id}/pdf`, `credit-note-${creditNoteNumber}.pdf`);
  }
}
