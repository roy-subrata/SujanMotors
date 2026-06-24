import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from 'src/environments/environment';

export interface CustomerAccountSummaryQuery {
    customerId?: string;
    fromDate?: string;
    toDate?: string;
    customerVehicleId?: string;
    pageNumber: number;
    pageSize: number;
}

export interface CustomerPurchaseItem {
    invoiceId: string;
    invoiceDate: string;
    invoiceNumber: string;
    invoiceStatus: string;
    customerVehicleId?: string | null;
    vehicleLabel: string;
    salesOrderLineId: string;
    itemName: string;
    partNumber: string;
    sku: string;
    quantity: number;
    unitPrice: number;
    discount: number;
    lineTotal: number;
}

export interface CustomerAccountSummary {
    customerId: string;
    customerName: string;
    customerCode: string;
    customerPhone: string;
    customerType: string;
    reportDate: string;
    fromDate?: string;
    toDate?: string;
    totalPurchaseAmount: number;
    totalPaidAmount: number;
    currentDue: number;
    lastPaymentDate?: string | null;
    lastPaymentAmount: number;
    totalInvoices: number;
    totalLineItems: number;
    purchaseItems: CustomerPurchaseItem[];
    purchaseItemsTotalCount: number;
    purchaseItemsPageNumber: number;
    purchaseItemsPageSize: number;
    purchaseItemsTotalPages: number;
}

@Injectable({ providedIn: 'root' })
export class CustomerAccountSummaryService {
    private readonly http = inject(HttpClient);
    private readonly apiUrl = `${environment.apiUrl}/v1/customer-account-summary`;

    getAccountSummary(customerId: string, query: CustomerAccountSummaryQuery): Observable<CustomerAccountSummary> {
        return this.http.post<CustomerAccountSummary>(`${this.apiUrl}/${customerId}`, query);
    }
}
