import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from 'src/environments/environment';
import { PdfDownloadService } from '@/shared/services/pdf-download.service';

export interface CreateQuotationLineRequest {
    partId: string;
    productVariantId?: string | null;
    unitId?: string | null;
    quantity: number;
    unitPrice: number;
    discount: number;
}

export interface CreateQuotationRequest {
    customerId: string;
    customerName: string;
    customerEmail: string;
    customerPhone: string;
    validUntil?: string | null;
    notes: string;
    currency: string;
    discount: number; // percentage
    taxAmount: number;
    lines: CreateQuotationLineRequest[];
}

export interface QuotationLineResponse {
    id: string;
    partId: string;
    partName: string;
    variantName?: string | null;
    sku: string;
    quantity: number;
    unitPrice: number;
    discount: number;
    totalPrice: number;
    unitSymbol: string;
}

export interface QuotationResponse {
    id: string;
    quotationNumber: string;
    customerId: string;
    customerName: string;
    customerEmail: string;
    customerPhone: string;
    quoteDate: string;
    validUntil: string;
    status: string; // DRAFT | SENT | ACCEPTED | REJECTED | CONVERTED | EXPIRED
    isExpired: boolean;
    subTotal: number;
    discountPercentage: number;
    discountAmount: number;
    totalAmount: number;
    taxAmount: number;
    grandTotal: number;
    currency: string;
    notes: string;
    convertedToSalesOrderId?: string | null;
    lines: QuotationLineResponse[];
    createdAt: string;
}

export interface QuotationQuery {
    customerId?: string;
    status?: string;
    search?: string;
    pageNumber: number;
    pageSize: number;
}

export interface QuotationSearchResponse {
    data: QuotationResponse[];
    totalCount: number;
    pageNumber: number;
    pageSize: number;
}

export interface ConvertQuotationResponse {
    quotationId: string;
    salesOrderId: string;
    soNumber: string;
}

@Injectable({ providedIn: 'root' })
export class QuotationService {
    private readonly http = inject(HttpClient);
    private readonly pdfDownload = inject(PdfDownloadService);
    private readonly apiUrl = `${environment.apiUrl}/v1/quotations`;

    create(request: CreateQuotationRequest): Observable<QuotationResponse> {
        return this.http.post<QuotationResponse>(this.apiUrl, request);
    }

    getById(id: string): Observable<QuotationResponse> {
        return this.http.get<QuotationResponse>(`${this.apiUrl}/${id}`);
    }

    getByNumber(quotationNumber: string): Observable<QuotationResponse> {
        return this.http.get<QuotationResponse>(`${this.apiUrl}/number/${quotationNumber}`);
    }

    getByCustomer(customerId: string): Observable<QuotationResponse[]> {
        return this.http.get<QuotationResponse[]>(`${this.apiUrl}/customer/${customerId}`);
    }

    search(query: QuotationQuery): Observable<QuotationSearchResponse> {
        return this.http.post<QuotationSearchResponse>(`${this.apiUrl}/list`, query);
    }

    /** DRAFT → SENT */
    send(id: string): Observable<QuotationResponse> {
        return this.http.patch<QuotationResponse>(`${this.apiUrl}/${id}/send`, {});
    }

    /** SENT → ACCEPTED */
    accept(id: string): Observable<QuotationResponse> {
        return this.http.patch<QuotationResponse>(`${this.apiUrl}/${id}/accept`, {});
    }

    /** SENT → REJECTED */
    reject(id: string, reason: string): Observable<QuotationResponse> {
        return this.http.patch<QuotationResponse>(`${this.apiUrl}/${id}/reject`, { reason });
    }

    /** ACCEPTED → CONVERTED, creates a new SalesOrder */
    convertToSalesOrder(id: string): Observable<ConvertQuotationResponse> {
        return this.http.post<ConvertQuotationResponse>(`${this.apiUrl}/${id}/convert`, {});
    }

    /** Download the server-rendered Quotation PDF and trigger the browser save dialog. */
    downloadPdf(id: string, quotationNumber: string): Observable<void> {
        return this.pdfDownload.downloadGet(`${this.apiUrl}/${id}/pdf`, `quotation-${quotationNumber}.pdf`);
    }
}
