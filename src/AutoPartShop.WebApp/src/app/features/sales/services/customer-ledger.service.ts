import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { environment } from 'src/environments/environment';
import { I18nService } from '@/shared/services/i18n.service';

export enum CustomerLedgerTransactionType {
    INVOICE = 'INVOICE',   // Invoice issued (debit - increases what the customer owes)
    PAYMENT = 'PAYMENT',   // Customer payment (credit - decreases what the customer owes)
    ADVANCE = 'ADVANCE',   // Advance payment (credit)
    REFUND = 'REFUND',     // Sales return refund (credit - decreases what the customer owes)
}

export interface CustomerLedgerEntryDto {
    id: string;
    transactionDate: string;
    transactionType: CustomerLedgerTransactionType;
    referenceNumber: string;
    referenceId?: string;
    debitAmount: number;
    creditAmount: number;
    runningBalance: number;
    description: string;
    status: string;
}

export interface CustomerLedgerSummaryDto {
    customerId: string;
    customerName: string;
    customerCode: string;
    totalInvoiced: number;
    totalPayments: number;
    totalRefunds: number;
    availableAdvanceCredit: number;
    currentBalance: number;
    transactionCount: number;
    lastTransactionDate?: string;
    entries: CustomerLedgerEntryDto[];
}

export interface CustomerLedgerQueryDto {
    customerId: string;
    pageNumber: number;
    pageSize: number;
    fromDate?: string;
    toDate?: string;
    transactionType?: CustomerLedgerTransactionType;
}

export interface PagedCustomerLedgerResult {
    entries: CustomerLedgerEntryDto[];
    totalCount: number;
    pageNumber: number;
    pageSize: number;
}

export interface ReceivablesAgingRow {
    customerId: string;
    customerCode: string;
    customerName: string;
    currentAmount: number;
    days1To30: number;
    days31To60: number;
    days61To90: number;
    days90Plus: number;
    total: number;
}

@Injectable({
    providedIn: 'root'
})
export class CustomerLedgerService {
    private readonly http = inject(HttpClient);
    private readonly i18n = inject(I18nService);
    private readonly apiUrl = `${environment.apiUrl}/v1/customer-ledger`;
    private readonly reportsUrl = `${environment.apiUrl}/v1/reports/financial`;

    /** Get full ledger summary for a customer including recent entries. */
    getLedgerSummary(customerId: string, entryLimit: number = 20): Observable<CustomerLedgerSummaryDto> {
        const params = new HttpParams().set('entryLimit', entryLimit.toString());
        return this.http.get<CustomerLedgerSummaryDto>(`${this.apiUrl}/${customerId}/summary`, { params });
    }

    /** Get paginated ledger entries with filters. */
    getLedgerEntries(query: CustomerLedgerQueryDto): Observable<PagedCustomerLedgerResult> {
        return this.http.post<PagedCustomerLedgerResult>(`${this.apiUrl}/${query.customerId}/entries`, query);
    }

    /**
     * Look up this customer's row on the Receivables Ageing report, narrowed server-side by the
     * report's existing text `Search` filter (no dedicated CustomerId parameter exists on the
     * stored procedure). Returns null if the customer has no outstanding receivables row.
     */
    getAgingForCustomer(customerId: string, customerCode: string): Observable<ReceivablesAgingRow | null> {
        return this.http.post<{ data: ReceivablesAgingRow[] }>(`${this.reportsUrl}/receivables-aging`, {
            search: customerCode,
            pageNumber: 1,
            pageSize: 10
        }).pipe(
            map(res => (res.data ?? []).find(row => row.customerId === customerId) ?? null)
        );
    }

    getTransactionTypeLabel(type: CustomerLedgerTransactionType): string {
        if (!type) return type;
        const key = `customerLedger.transactionTypes.${type}`;
        const label = this.i18n.t(key);
        return label === key ? type : label;
    }

    getTransactionTypeStatus(type: CustomerLedgerTransactionType): string {
        switch (type) {
            case CustomerLedgerTransactionType.INVOICE:
                return 'invoice';
            case CustomerLedgerTransactionType.PAYMENT:
                return 'payment';
            case CustomerLedgerTransactionType.ADVANCE:
                return 'advance';
            case CustomerLedgerTransactionType.REFUND:
                return 'refund';
            default:
                return 'default';
        }
    }
}
