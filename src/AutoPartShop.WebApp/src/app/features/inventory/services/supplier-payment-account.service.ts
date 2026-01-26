import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface SupplierPaymentAccountResponse {
  id: string;
  supplierId: string;
  supplierName: string;
  accountType: string;
  accountName: string;
  isDefault: boolean;
  isActive: boolean;
  // Bank Transfer fields
  bankName: string;
  bankAccountNumber: string;
  bankBranchName: string;
  bankBranchCode: string;
  beneficiaryName: string;
  bankIBAN: string;
  bankSWIFT: string;
  // Mobile Banking fields
  mobileNumber: string;
  mobileAccountHolderName: string;
  mobileProvider: string;
  notes: string;
  displayText: string;
  createdAt: string;
}

export interface CreateSupplierPaymentAccountRequest {
  supplierId: string;
  accountType: string;
  accountName: string;
  isDefault: boolean;
  // Bank Transfer fields
  bankName: string;
  bankAccountNumber: string;
  bankBranchName: string;
  bankBranchCode: string;
  beneficiaryName: string;
  bankIBAN: string;
  bankSWIFT: string;
  // Mobile Banking fields
  mobileNumber: string;
  mobileAccountHolderName: string;
  mobileProvider: string;
  notes: string;
}

export interface UpdateSupplierPaymentAccountRequest {
  accountName: string;
  isActive: boolean;
  // Bank Transfer fields
  bankName: string;
  bankAccountNumber: string;
  bankBranchName: string;
  bankBranchCode: string;
  beneficiaryName: string;
  bankIBAN: string;
  bankSWIFT: string;
  // Mobile Banking fields
  mobileNumber: string;
  mobileAccountHolderName: string;
  mobileProvider: string;
  notes: string;
}

@Injectable({
  providedIn: 'root'
})
export class SupplierPaymentAccountService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = 'http://localhost:5292/api/supplier-payment-accounts';

  /**
   * Get all supplier payment accounts
   */
  getAll(): Observable<SupplierPaymentAccountResponse[]> {
    return this.http.get<SupplierPaymentAccountResponse[]>(this.apiUrl);
  }

  /**
   * Get supplier payment account by ID
   */
  getById(id: string): Observable<SupplierPaymentAccountResponse> {
    return this.http.get<SupplierPaymentAccountResponse>(`${this.apiUrl}/${id}`);
  }

  /**
   * Get all payment accounts for a supplier
   */
  getBySupplier(supplierId: string): Observable<SupplierPaymentAccountResponse[]> {
    return this.http.get<SupplierPaymentAccountResponse[]>(`${this.apiUrl}/by-supplier/${supplierId}`);
  }

  /**
   * Get active payment accounts for a supplier (for dropdown selection)
   */
  getActiveBySupplier(supplierId: string): Observable<SupplierPaymentAccountResponse[]> {
    return this.http.get<SupplierPaymentAccountResponse[]>(`${this.apiUrl}/active/by-supplier/${supplierId}`);
  }

  /**
   * Get default payment account for a supplier
   */
  getDefaultBySupplier(supplierId: string): Observable<SupplierPaymentAccountResponse> {
    return this.http.get<SupplierPaymentAccountResponse>(`${this.apiUrl}/default/by-supplier/${supplierId}`);
  }

  /**
   * Create a new supplier payment account
   */
  create(request: CreateSupplierPaymentAccountRequest): Observable<SupplierPaymentAccountResponse> {
    return this.http.post<SupplierPaymentAccountResponse>(this.apiUrl, request);
  }

  /**
   * Update an existing supplier payment account
   */
  update(id: string, request: UpdateSupplierPaymentAccountRequest): Observable<SupplierPaymentAccountResponse> {
    return this.http.put<SupplierPaymentAccountResponse>(`${this.apiUrl}/${id}`, request);
  }

  /**
   * Set a payment account as default for the supplier
   */
  setDefault(id: string): Observable<SupplierPaymentAccountResponse> {
    return this.http.patch<SupplierPaymentAccountResponse>(`${this.apiUrl}/${id}/set-default`, {});
  }

  /**
   * Delete a payment account (soft delete)
   */
  delete(id: string): Observable<{ message: string }> {
    return this.http.delete<{ message: string }>(`${this.apiUrl}/${id}`);
  }
}
