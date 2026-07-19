import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from 'src/environments/environment';
import { PdfDownloadService } from '@/shared/services/pdf-download.service';

export interface CreateProformaInvoiceRequest {
    salesOrderId: string;
    validUntil?: string | null;
    notes: string;
}

/**
 * A Proforma Invoice stores no pricing data of its own — it's a thin wrapper generated FROM an
 * existing SalesOrder. Line items and totals are always read live from the linked order, so
 * `grandTotal`/`customerName`/`soNumber` here simply mirror what the API resolved from that order
 * at read time.
 */
export interface ProformaInvoiceResponse {
    id: string;
    proformaNumber: string;
    salesOrderId: string;
    soNumber: string;
    customerName: string;
    grandTotal: number;
    issueDate: string;
    validUntil: string;
    status: string; // ISSUED | EXPIRED | SUPERSEDED
    isExpired: boolean;
    notes: string;
    createdAt: string;
}

export interface ProformaInvoiceListResponse {
    data: ProformaInvoiceResponse[];
    totalCount: number;
    pageNumber: number;
    pageSize: number;
}

@Injectable({ providedIn: 'root' })
export class ProformaInvoiceService {
    private readonly http = inject(HttpClient);
    private readonly pdfDownload = inject(PdfDownloadService);
    private readonly apiUrl = `${environment.apiUrl}/v1/proforma-invoices`;

    create(request: CreateProformaInvoiceRequest): Observable<ProformaInvoiceResponse> {
        return this.http.post<ProformaInvoiceResponse>(this.apiUrl, request);
    }

    getById(id: string): Observable<ProformaInvoiceResponse> {
        return this.http.get<ProformaInvoiceResponse>(`${this.apiUrl}/${id}`);
    }

    getBySalesOrder(salesOrderId: string): Observable<ProformaInvoiceResponse[]> {
        return this.http.get<ProformaInvoiceResponse[]>(`${this.apiUrl}/sales-order/${salesOrderId}`);
    }

    /** Plain query-string paging — GET /list?pageNumber=&pageSize= (no search/status filters on the API). */
    list(pageNumber: number, pageSize: number): Observable<ProformaInvoiceListResponse> {
        const params = new HttpParams()
            .set('pageNumber', pageNumber.toString())
            .set('pageSize', pageSize.toString());
        return this.http.get<ProformaInvoiceListResponse>(`${this.apiUrl}/list`, { params });
    }

    /** Download the server-rendered Proforma Invoice PDF and trigger the browser save dialog. */
    downloadPdf(id: string, proformaNumber: string): Observable<void> {
        return this.pdfDownload.downloadGet(`${this.apiUrl}/${id}/pdf`, `proforma-invoice-${proformaNumber}.pdf`);
    }
}
