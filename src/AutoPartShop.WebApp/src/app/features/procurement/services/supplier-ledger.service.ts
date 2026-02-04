import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from 'src/environments/environment';

export enum SupplierLedgerTransactionType {
    PURCHASE = 'PURCHASE',       // PO confirmation (debit - increases debt)
    PAYMENT = 'PAYMENT',         // Supplier payment (credit - decreases debt)
    REFUND = 'REFUND',           // Purchase return settlement (credit - decreases debt)
    ADVANCE = 'ADVANCE',         // Advance payment (credit)
    CANCELLATION = 'CANCELLATION' // Purchase order cancellation (decreases debt/reversal)
}

export interface SupplierLedgerEntryDto {
    id: string;
    transactionDate: string;
    transactionType: SupplierLedgerTransactionType;
    referenceNumber: string;
    referenceId?: string;
    debitAmount: number;
    creditAmount: number;
    runningBalance: number;
    description: string;
    status: string;
}

export interface SupplierLedgerSummaryDto {
    supplierId: string;
    supplierName: string;
    supplierCode: string;
    totalPurchases: number;
    totalPayments: number;
    totalRefunds: number;
    availableAdvanceCredit: number;
    currentBalance: number;  // Calculated: TotalPurchases - TotalPayments - TotalRefunds
    transactionCount: number;
    lastTransactionDate?: string;
    entries: SupplierLedgerEntryDto[];
}

export interface SupplierLedgerQueryDto {
    supplierId: string;
    pageNumber: number;
    pageSize: number;
    fromDate?: string;
    toDate?: string;
    transactionType?: SupplierLedgerTransactionType;
}

export interface PagedLedgerResult {
    entries: SupplierLedgerEntryDto[];
    totalCount: number;
    pageNumber: number;
    pageSize: number;
}

export interface SettlePurchaseReturnRequest {
    amount: number;
    settlementMethod: string;  // CREDIT, CASH, BANK_TRANSFER
    notes?: string;
}

@Injectable({
    providedIn: 'root'
})
export class SupplierLedgerService {
    private readonly http = inject(HttpClient);
    private readonly apiUrl = `${environment.apiUrl}/supplier-ledger`;

    /**
     * Get full ledger summary for a supplier including recent entries
     */
    getLedgerSummary(supplierId: string, entryLimit: number = 20): Observable<SupplierLedgerSummaryDto> {
        let params = new HttpParams().set('entryLimit', entryLimit.toString());
        return this.http.get<SupplierLedgerSummaryDto>(`${this.apiUrl}/${supplierId}/summary`, { params });
    }

    /**
     * Get paginated ledger entries with filters
     */
    getLedgerEntries(query: SupplierLedgerQueryDto): Observable<PagedLedgerResult> {
        return this.http.post<PagedLedgerResult>(`${this.apiUrl}/${query.supplierId}/entries`, query);
    }

    /**
     * Get calculated current balance for a supplier
     */
    getCurrentBalance(supplierId: string): Observable<{ currentBalance: number }> {
        return this.http.get<{ currentBalance: number }>(`${this.apiUrl}/${supplierId}/balance`);
    }

    /**
     * Get ledger entries with date range filter
     */
    getLedgerEntriesWithDateRange(
        supplierId: string,
        fromDate?: string,
        toDate?: string
    ): Observable<SupplierLedgerEntryDto[]> {
        let params = new HttpParams();
        if (fromDate) params = params.set('fromDate', fromDate);
        if (toDate) params = params.set('toDate', toDate);
        return this.http.get<SupplierLedgerEntryDto[]>(`${this.apiUrl}/${supplierId}/entries`, { params });
    }

    /**
     * Get transaction type display label
     */
    getTransactionTypeLabel(type: SupplierLedgerTransactionType): string {
        switch (type) {
            case SupplierLedgerTransactionType.PURCHASE:
                return 'Purchase Order';
            case SupplierLedgerTransactionType.PAYMENT:
                return 'Payment';
            case SupplierLedgerTransactionType.REFUND:
                return 'Purchase Return';
            case SupplierLedgerTransactionType.ADVANCE:
                return 'Advance Payment';
            case SupplierLedgerTransactionType.CANCELLATION:
                return 'Cancellation';
            default:
                return type;
        }
    }

    /**
     * Get transaction type CSS class
     */
    getTransactionTypeClass(type: SupplierLedgerTransactionType): string {
        const baseClass = 'px-2 py-1 rounded text-xs font-medium';
        switch (type) {
            case SupplierLedgerTransactionType.PURCHASE:
                return `${baseClass} bg-red-100 text-red-800`;
            case SupplierLedgerTransactionType.PAYMENT:
                return `${baseClass} bg-green-100 text-green-800`;
            case SupplierLedgerTransactionType.REFUND:
                return `${baseClass} bg-purple-100 text-purple-800`;
            case SupplierLedgerTransactionType.ADVANCE:
                return `${baseClass} bg-blue-100 text-blue-800`;
            case SupplierLedgerTransactionType.CANCELLATION:
                return `${baseClass} bg-gray-100 text-gray-800`;
            default:
                return `${baseClass} bg-gray-100 text-gray-800`;
        }
    }

    /**
     * Determine if a transaction is a debit (increases debt)
     */
    isDebitTransaction(type: SupplierLedgerTransactionType): boolean {
        return type === SupplierLedgerTransactionType.PURCHASE;
    }

    /**
     * Determine if a transaction is a credit (decreases debt)
     */
    isCreditTransaction(type: SupplierLedgerTransactionType): boolean {
        return type === SupplierLedgerTransactionType.PAYMENT ||
               type === SupplierLedgerTransactionType.REFUND ||
               type === SupplierLedgerTransactionType.ADVANCE ||
               type === SupplierLedgerTransactionType.CANCELLATION;
    }
}
