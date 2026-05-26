import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { from, Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { PaginatedResponse } from './customer.service';
import { environment } from 'src/environments/environment';

export interface CreateCustomerPaymentRequest {
    customerId: string;
    invoiceId?: string;
    paymentProviderId?: string;
    amount: number;
    paymentMethod: string;
    transactionNumber: string;
    referenceNumber: string;
    paymentDate?: string;
    notes: string;
}

export interface UpdateCustomerPaymentRequest {
    status?: string;
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
    paymentType: string;
    remainingAmount: number;
    sourceAdvancePaymentId?: string;
    sourceAdvanceTransactionNumber?: string;
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
    paymentType: string;
    invoiceNumber: string;
    transactionNumber: string;
    providerName: string;
    sourceAdvancePaymentId?: string;
    sourceAdvanceTransactionNumber?: string;
    isReconciled:boolean
}


export interface AvailableCustomerAdvancePayment {
    id: string;
    transactionNumber: string;
    amount: number;
    remainingAmount: number;
    usedAmount: number;
    paymentDate: string;
    description: string;
}

export interface ApplyCustomerAdvanceCreditRequest {
    invoiceId: string;
    sourceAdvancePaymentId: string;
    amount: number;
    description?: string;
}

export interface ApplyCustomerAdvanceCreditResponse {
    paymentId: string;
    transactionNumber: string;
    amountApplied: number;
    remainingAdvanceBalance: number;
    message: string;
}

export interface CustomerPaymentQuery {
    pageNumber: number;
    pageSize: number;
    search?: string;
    customerId?: string;
    status?: string;
    fromDate?: string;
    toDate?: string;
    isReconciled?: boolean;
}

@Injectable({
    providedIn: 'root'
})
export class CustomerPaymentService {
    private readonly http = inject(HttpClient);
    private readonly apiUrl = `${environment.apiUrl}/customer-payments`;

    /**
     * Get all customer payments
     */
    getAllCustomerPayments(): Observable<CustomerPaymentResponse[]> {
        return this.http.get<CustomerPaymentResponse[]>(this.apiUrl);
    }

    /**
     * Get paginated customer payments
     */
    getCustomerPayments(customerQuery: CustomerPaymentQuery): Observable<PaginatedResponse<CustomerPaymentResponse>> {

        return this.http.post<PaginatedResponse<CustomerPaymentResponse>>(`${this.apiUrl}/list`, customerQuery);
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
        return this.http.get<{ data: CustomerPaymentResponse[] }>(`${this.apiUrl}/customer/${customerId}`).pipe(map((response) => response.data));
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
     * Mark customer payment as advance
     */
    markPaymentAsAdvance(id: string, description?: string): Observable<CustomerPaymentResponse> {
        return this.http.patch<CustomerPaymentResponse>(`${this.apiUrl}/${id}/mark-advance`, { description: description || 'Advance Payment' });
    }

    /**
     * Mark customer payment as regular
     */
    markPaymentAsRegular(id: string, description?: string): Observable<CustomerPaymentResponse> {
        return this.http.patch<CustomerPaymentResponse>(`${this.apiUrl}/${id}/mark-regular`, { description: description || 'Regular Payment' });
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

    /**
     * Get available advance payments for a customer
     */
    getAvailableAdvances(customerId: string): Observable<AvailableCustomerAdvancePayment[]> {
        return this.http.get<AvailableCustomerAdvancePayment[]>(`${this.apiUrl}/customer/${customerId}/available-advances`);
    }

    /**
     * Apply advance credit to an invoice
     */
    applyAdvanceCredit(request: ApplyCustomerAdvanceCreditRequest): Observable<ApplyCustomerAdvanceCreditResponse> {
        return this.http.post<ApplyCustomerAdvanceCreditResponse>(`${this.apiUrl}/apply-advance-credit`, request);
    }
}
