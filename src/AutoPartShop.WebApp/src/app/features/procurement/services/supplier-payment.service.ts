import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';

export interface CreateSupplierPaymentRequest {
    supplierId: string;
    purchaseOrderId?: string;
    paymentProviderId: string;
    amount: number;
    paymentMethod: string;
    transactionNumber: string;
    referenceNumber: string;
    invoiceNumber: string;
    paymentDate?: string;
    notes: string;
}

export interface MarkPaymentAsAdvanceRequest {
    description: string;
}
export interface MarkPaymentAsRegularRequest {
    description: string;
}
export interface UpdateSupplierPaymentRequest {
    status: string;
    referenceNumber: string;
    authorizationCode: string;
    notes: string;
}

export interface SupplierPaymentResponse {
    id: string;
    supplierId: string;
    supplierName: string;
    purchaseOrderId?: string;
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
    invoiceNumber: string;
    notes: string;
    processedDate?: string;
    processedBy: string;
    confirmedDate?: string;
    confirmedBy: string;
    isReconciled: boolean;
    reconciledDate?: string;
    createdAt: string;
    paymentType: string;
    description: string;
}

export interface SupplierPaymentHistorySummary {
    supplierId: string;
    supplierName: string;
    supplierCode: string;
    creditLimit: number;
    creditUtilization: number;
    totalPaid: number;
    totalAdvanceAmount: number;
    totalDue: number;
    paymentBalance: number;
    totalFees: number;
    outstandingInvoiceCount: number;
    completedPayments: number;
    pendingPayments: number;
    failedPayments: number;
    processingPayments: number;
    cancelledPayments: number;
    lastPaymentDate?: string;
    lastPaymentAmount: number;
    statusBreakdown?: PaymentStatusBreakdown;
    paymentHistory: PaymentHistoryItem[];
}

export interface PaymentStatusBreakdown {
    pending: number;
    completed: number;
    failed: number;
    processing: number;
    cancelled: number;
    reconciled: number;
}

export interface PaymentHistoryItem {
    id: string;
    amount: number;
    paymentDate: string;
    status: string;
    paymentMethod: string;
    paymentType: string;
    invoiceNumber: string;
    transactionNumber: string;
    providerName: string;
}

export interface PaginatedSupplierPaymentResponse {
    items: SupplierPaymentResponse[];
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
export class SupplierPaymentService {
    private readonly http = inject(HttpClient);
    private readonly apiUrl = 'http://localhost:5292/api/supplier-payment';

    /**
     * Get all supplier payments
     */
    getAllSupplierPayments(): Observable<SupplierPaymentResponse[]> {
        return this.http.get<SupplierPaymentResponse[]>(this.apiUrl);
    }

    /**
     * Get paginated supplier payments
     */
    getSupplierPayments(pageNumber: number, pageSize: number, searchTerm?: string): Observable<PaginatedSupplierPaymentResponse> {
        let params = new HttpParams().set('pageNumber', pageNumber.toString()).set('pageSize', pageSize.toString());

        if (searchTerm) {
            params = params.set('searchTerm', searchTerm);
        }

        return this.http.get<any>(`${this.apiUrl}/list`, { params }).pipe(
            map((response) => ({
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
     * Get supplier payment by ID
     */
    getSupplierPaymentById(id: string): Observable<SupplierPaymentResponse> {
        return this.http.get<SupplierPaymentResponse>(`${this.apiUrl}/${id}`);
    }

    /**
     * Get payments for a specific supplier
     */
    getPaymentsBySupplier(supplierId: string): Observable<SupplierPaymentResponse[]> {
        return this.http.get<SupplierPaymentResponse[]>(`${this.apiUrl}/supplier/${supplierId}`);
    }

    /**
     * Get comprehensive payment summary for a supplier
     */
    getSupplierPaymentSummary(supplierId: string): Observable<SupplierPaymentHistorySummary> {
        return this.http.get<SupplierPaymentHistorySummary>(`${this.apiUrl}/supplier/${supplierId}/summary`);
    }

    /**
     * Get payment history for a supplier
     */
    getSupplierPaymentHistory(supplierId: string, limit: number = 10): Observable<PaymentHistoryItem[]> {
        let params = new HttpParams().set('limit', limit.toString());
        return this.http.get<PaymentHistoryItem[]>(`${this.apiUrl}/supplier/${supplierId}/history`, { params });
    }

    /**
     * Get payment status breakdown for a supplier
     */
    getPaymentStatusBreakdown(supplierId: string): Observable<PaymentStatusBreakdown> {
        return this.http.get<PaymentStatusBreakdown>(`${this.apiUrl}/supplier/${supplierId}/status-breakdown`);
    }

    /**
     * Get advance payments for a supplier
     */
    getAdvancePayments(supplierId: string): Observable<SupplierPaymentResponse[]> {
        return this.http.get<SupplierPaymentResponse[]>(`${this.apiUrl}/supplier/${supplierId}/advance`);
    }

    /**
     * Get payments by status
     */
    getPaymentsByStatus(status: string): Observable<SupplierPaymentResponse[]> {
        return this.http.get<SupplierPaymentResponse[]>(`${this.apiUrl}/status/${status}`);
    }

    /**
     * Create new supplier payment
     */
    createSupplierPayment(request: CreateSupplierPaymentRequest): Observable<SupplierPaymentResponse> {
        return this.http.post<SupplierPaymentResponse>(this.apiUrl, request);
    }

    /**
     * Update supplier payment
     */
    updateSupplierPayment(id: string, request: UpdateSupplierPaymentRequest): Observable<SupplierPaymentResponse> {
        return this.http.put<SupplierPaymentResponse>(`${this.apiUrl}/${id}`, request);
    }

    /**
     * Confirm supplier payment
     */
    confirmPayment(id: string): Observable<SupplierPaymentResponse> {
        return this.http.patch<SupplierPaymentResponse>(`${this.apiUrl}/${id}/confirm`, {});
    }

    /**
     * Reconcile supplier payment
     */
    reconcilePayment(id: string): Observable<SupplierPaymentResponse> {
        return this.http.patch<SupplierPaymentResponse>(`${this.apiUrl}/${id}/reconcile`, {});
    }

    /**
     * Cancel supplier payment
     */
    cancelPayment(id: string): Observable<SupplierPaymentResponse> {
        return this.http.patch<SupplierPaymentResponse>(`${this.apiUrl}/${id}/cancel`, {});
    }

    /**
     * Mark supplier payment as advance
     */
    markPaymentAsAdvance(id: string, request?: MarkPaymentAsAdvanceRequest): Observable<SupplierPaymentResponse> {
        return this.http.patch<SupplierPaymentResponse>(`${this.apiUrl}/${id}/mark-advance`, request || { description: 'Advance Payment' });
    }

    /**
     * Mark supplier payment as regular
     */
    markPaymentAsRegular(id: string, request?: MarkPaymentAsRegularRequest): Observable<SupplierPaymentResponse> {
        return this.http.patch<SupplierPaymentResponse>(`${this.apiUrl}/${id}/mark-regular`, request || { description: 'Regular Payment' });
    }

    /**
     * Delete supplier payment
     */
    deleteSupplierPayment(id: string): Observable<void> {
        return this.http.delete<void>(`${this.apiUrl}/${id}`);
    }

    /**
     * Download payment summary report as CSV
     */
    downloadPaymentSummaryReport(supplierId: string): Observable<Blob> {
        return this.http.get(`${this.apiUrl}/supplier/${supplierId}/report`, {
            responseType: 'blob'
        });
    }
}
