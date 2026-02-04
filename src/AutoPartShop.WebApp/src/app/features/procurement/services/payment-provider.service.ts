import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { environment } from 'src/environments/environment';

export interface CreatePaymentProviderRequest {
  providerName: string;
  providerType: string;
  // Bank Transfer fields
  bankName: string;
  bankAccountNumber: string;
  bankRoutingNumber: string;
  bankIBAN: string;
  bankSWIFT: string;
  beneficiaryName: string;
  // Online Gateway fields
  apiKey: string;
  merchantId: string;
  webhookUrl: string;
  // Mobile Banking fields (bKash, Nagad, eZ Cash, etc.)
  mobileNumber: string;
  accountHolderName: string;
  agentNumber: string;
  // Fee Configuration
  transactionFeeType: string;
  transactionFeeAmount: number;
  minimumAmount: number;
  maximumAmount: number;
  settlementDays: number;
  supportedCurrencies: string;
  notes: string;
}

export interface UpdatePaymentProviderRequest {
  providerName: string;
  // Bank Transfer fields
  bankName: string;
  bankAccountNumber: string;
  bankRoutingNumber: string;
  bankIBAN: string;
  bankSWIFT: string;
  beneficiaryName: string;
  // Mobile Banking fields
  mobileNumber: string;
  accountHolderName: string;
  agentNumber: string;
  // Fee Configuration
  transactionFeeType: string;
  transactionFeeAmount: number;
  minimumAmount: number;
  maximumAmount: number;
  settlementDays: number;
  supportedCurrencies: string;
  webhookUrl: string;
  notes: string;
}

export interface PaymentProviderResponse {
  id: string;
  providerName: string;
  providerType: string;
  status: string;
  // Bank Transfer fields
  bankName: string;
  bankAccountNumber: string;
  bankRoutingNumber: string;
  bankIBAN: string;
  bankSWIFT: string;
  beneficiaryName: string;
  // Online Gateway fields
  apiKey: string;
  merchantId: string;
  webhookUrl: string;
  // Mobile Banking fields
  mobileNumber: string;
  accountHolderName: string;
  agentNumber: string;
  // Fee Configuration
  transactionFeeType: string;
  transactionFeeAmount: number;
  minimumAmount: number;
  maximumAmount: number;
  settlementDays: number;
  supportedCurrencies: string;
  isDefault: boolean;
  lastTestedDate?: string;
  notes: string;
  createdAt: string;
}

export interface PaginatedPaymentProviderResponse {
  items: PaymentProviderResponse[];
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
export class PaymentProviderService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/payment-provider`;

  /**
   * Get all payment providers
   */
  getAllPaymentProviders(): Observable<PaymentProviderResponse[]> {
    return this.http.get<PaymentProviderResponse[]>(this.apiUrl);
  }

  /**
   * Get paginated payment providers
   */
  getPaymentProviders(pageNumber: number, pageSize: number, searchTerm?: string): Observable<PaginatedPaymentProviderResponse> {
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
   * Get payment provider by ID
   */
  getPaymentProviderById(id: string): Observable<PaymentProviderResponse> {
    return this.http.get<PaymentProviderResponse>(`${this.apiUrl}/${id}`);
  }

  /**
   * Create new payment provider
   */
  createPaymentProvider(request: CreatePaymentProviderRequest): Observable<PaymentProviderResponse> {
    return this.http.post<PaymentProviderResponse>(this.apiUrl, request);
  }

  /**
   * Update payment provider
   */
  updatePaymentProvider(id: string, request: UpdatePaymentProviderRequest): Observable<PaymentProviderResponse> {
    return this.http.put<PaymentProviderResponse>(`${this.apiUrl}/${id}`, request);
  }

  /**
   * Delete payment provider
   */
  deletePaymentProvider(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }

  /**
   * Set default payment provider
   */
  setDefault(id: string): Observable<PaymentProviderResponse> {
    return this.http.patch<PaymentProviderResponse>(`${this.apiUrl}/${id}/set-default`, {});
  }

  /**
   * Test payment provider connection
   */
  testConnection(id: string): Observable<{ success: boolean; message: string }> {
    return this.http.post<{ success: boolean; message: string }>(`${this.apiUrl}/${id}/test-connection`, {});
  }
}
