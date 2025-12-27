import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';

export interface CreateCustomerPaymentRequest {
  customerId: string;
  invoiceId?: string;
  paymentProviderId: string;
  amount: number;
  paymentMethod: string;
  transactionNumber: string;
  referenceNumber: string;
  paymentDate?: string;
  notes: string;
}

export interface UpdateCustomerPaymentRequest {
  status: string;
  referenceNumber: string;
  authorizationCode: string;
  notes: string;
}

export interface CustomerPaymentResponse {
  id: string;
  customerId: string;
  customerName: string;
  invoiceId?: string;
  invoiceNumber?: string;
  paymentProviderId: string;
  providerName: string;
  transactionNumber: string;
  amount: number;
  paymentFee: number;
  netAmount: number;
  currency: string;
  paymentDate: string;
  paymentMethod: string;
  status: string;
  referenceNumber: string;
  authorizationCode: string;
  notes: string;
  settledDate?: string;
  settledBy: string;
  isReconciled: boolean;
  reconciledDate?: string;
  createdAt: string;
}

export interface CustomerPaymentHistorySummary {
  customerId: string;
  customerName: string;
  totalPaid: number;
  totalFees: number;
  completedPayments: number;
  pendingPayments: number;
  failedPayments: number;
  lastPaymentDate?: string;
  lastPaymentAmount: number;

  // Invoice and Outstanding Balance Information
  totalInvoiceAmount: number;
  totalOutstanding: number;
  amountDue: number;
  totalInvoices: number;
  unpaidInvoices: number;
  overdueInvoices: number;

  paymentHistory: PaymentHistoryItem[];
  availableAdvance: number;
}

export interface PaymentHistoryItem {
  id: string;
  amount: number;
  paymentDate: string;
  status: string;
  paymentMethod: string;
  invoiceNumber: string;
  transactionNumber: string;
  providerName: string;
}

export interface PaginatedCustomerPaymentResponse {
  items: CustomerPaymentResponse[];
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
export class CustomerPaymentService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = 'http://localhost:5292/api/customerpayment';

  /**
   * Get all customer payments
   */
  getAllCustomerPayments(): Observable<CustomerPaymentResponse[]> {
    return this.http.get<CustomerPaymentResponse[]>(this.apiUrl);
  }

  /**
   * Get paginated customer payments
   */
  getCustomerPayments(pageNumber: number, pageSize: number, searchTerm?: string): Observable<PaginatedCustomerPaymentResponse> {
    let params = new HttpParams()
      .set('pageNumber', pageNumber.toString())
      .set('pageSize', pageSize.toString());

    if (searchTerm) {
      params = params.set('searchTerm', searchTerm);
    }

    return this.http.get<any>(`${this.apiUrl}/list`, { params }).pipe(
      map(response => ({
        items: response.data || response.items || [],
        pageNumber: response.pagination?.pageNumber || response.pageNumber || pageNumber,
        pageSize: response.pagination?.pageSize || response.pageSize || pageSize,
        totalCount: response.pagination?.totalCount || response.totalCount || 0,
        totalPages: response.pagination?.totalPages || response.totalPages || 0,
        hasPreviousPage: response.pagination?.pageNumber > 1 || response.pageNumber > 1 || false,
        hasNextPage: response.pagination?.pageNumber < response.pagination?.totalPages || false
      }))
    );
  }

  /**
   * Get customer payment by ID
   */
  getCustomerPaymentById(id: string): Observable<CustomerPaymentResponse> {
    return this.http.get<CustomerPaymentResponse>(`${this.apiUrl}/${id}`);
  }

  /**
   * Get payments for a specific customer
   */
  getCustomerPaymentsByCustomer(customerId: string): Observable<CustomerPaymentResponse[]> {
    return this.http.get<{ data: CustomerPaymentResponse[] }>(`${this.apiUrl}/customer/${customerId}`).pipe(
      map(response => response.data)
    );
  }

  /**
   * Get comprehensive payment summary for a customer
   */
  getCustomerPaymentSummary(customerId: string): Observable<CustomerPaymentHistorySummary> {
    return this.http.get<CustomerPaymentHistorySummary>(`${this.apiUrl}/customer/${customerId}/summary`);
  }

  /**
   * Create new customer payment
   */
  createCustomerPayment(request: CreateCustomerPaymentRequest): Observable<CustomerPaymentResponse> {
    return this.http.post<CustomerPaymentResponse>(this.apiUrl, request);
  }

  /**
   * Update customer payment
   */
  updateCustomerPayment(id: string, request: UpdateCustomerPaymentRequest): Observable<CustomerPaymentResponse> {
    return this.http.put<CustomerPaymentResponse>(`${this.apiUrl}/${id}`, request);
  }

  /**
   * Confirm customer payment
   */
  confirmPayment(id: string): Observable<CustomerPaymentResponse> {
    return this.http.patch<CustomerPaymentResponse>(`${this.apiUrl}/${id}/mark-completed`, {});
  }

  /**
   * Cancel customer payment
   */
  cancelPayment(id: string): Observable<CustomerPaymentResponse> {
    return this.http.patch<CustomerPaymentResponse>(`${this.apiUrl}/${id}/cancel`, {});
  }

  /**
   * Mark payment as failed
   */
  failPayment(id: string): Observable<CustomerPaymentResponse> {
    return this.http.patch<CustomerPaymentResponse>(`${this.apiUrl}/${id}/fail`, {});
  }

  /**
   * Refund customer payment
   */
  refundPayment(id: string): Observable<CustomerPaymentResponse> {
    return this.http.patch<CustomerPaymentResponse>(`${this.apiUrl}/${id}/refund`, {});
  }

  /**
   * Reconcile customer payment
   */
  reconcilePayment(id: string): Observable<CustomerPaymentResponse> {
    return this.http.patch<CustomerPaymentResponse>(`${this.apiUrl}/${id}/reconcile`, {});
  }

  /**
   * Delete customer payment
   */
  deleteCustomerPayment(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }

  /**
   * Download payment summary report as CSV
   */
  downloadPaymentSummaryReport(customerId: string): Observable<Blob> {
    return this.http.get(`${this.apiUrl}/customer/${customerId}/report`, {
      responseType: 'blob'
    });
  }
}
