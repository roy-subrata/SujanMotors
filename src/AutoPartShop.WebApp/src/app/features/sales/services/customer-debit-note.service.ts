import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from 'src/environments/environment';
import { PdfDownloadService } from '@/shared/services/pdf-download.service';

export interface CreateCustomerDebitNoteRequest {
    customerId: string;
    invoiceId?: string | null;
    amount: number;
    reason: string;
    currency: string;
    notes: string;
}

export interface CustomerDebitNoteResponse {
    id: string;
    debitNoteNumber: string;
    customerId: string;
    customerName: string;
    invoiceId?: string | null;
    invoiceNumber?: string | null;
    totalAmount: number;
    currency: string;
    issueDate: string;
    reason: string;
    status: string; // ISSUED | SETTLED | CANCELLED
    notes: string;
    issuedBy: string;
    createdAt: string;
}

export interface CustomerDebitNoteQuery {
    customerId?: string;
    status?: string;
    pageNumber: number;
    pageSize: number;
}

export interface CustomerDebitNoteListResponse {
    data: CustomerDebitNoteResponse[];
    totalCount: number;
    pageNumber: number;
    pageSize: number;
}

@Injectable({ providedIn: 'root' })
export class CustomerDebitNoteService {
    private readonly http = inject(HttpClient);
    private readonly pdfDownload = inject(PdfDownloadService);
    private readonly apiUrl = `${environment.apiUrl}/v1/customer-debit-notes`;

    create(request: CreateCustomerDebitNoteRequest): Observable<CustomerDebitNoteResponse> {
        return this.http.post<CustomerDebitNoteResponse>(this.apiUrl, request);
    }

    getById(id: string): Observable<CustomerDebitNoteResponse> {
        return this.http.get<CustomerDebitNoteResponse>(`${this.apiUrl}/${id}`);
    }

    getByCustomer(customerId: string): Observable<CustomerDebitNoteResponse[]> {
        return this.http.get<CustomerDebitNoteResponse[]>(`${this.apiUrl}/customer/${customerId}`);
    }

    search(query: CustomerDebitNoteQuery): Observable<CustomerDebitNoteListResponse> {
        return this.http.post<CustomerDebitNoteListResponse>(`${this.apiUrl}/list`, query);
    }

    /** ISSUED → SETTLED */
    settle(id: string): Observable<CustomerDebitNoteResponse> {
        return this.http.patch<CustomerDebitNoteResponse>(`${this.apiUrl}/${id}/settle`, {});
    }

    /**
     * ISSUED → CANCELLED. The API's [FromBody] parameter is a bare string (`string? reason`), not
     * an object — send the JSON-encoded string itself as the body, not `{ reason }`.
     */
    cancel(id: string, reason: string): Observable<CustomerDebitNoteResponse> {
        return this.http.patch<CustomerDebitNoteResponse>(`${this.apiUrl}/${id}/cancel`, JSON.stringify(reason ?? ''), {
            headers: { 'Content-Type': 'application/json' }
        });
    }

    /** Download the server-rendered Debit Note PDF and trigger the browser save dialog. */
    downloadPdf(id: string, debitNoteNumber: string): Observable<void> {
        return this.pdfDownload.downloadGet(`${this.apiUrl}/${id}/pdf`, `debit-note-${debitNoteNumber}.pdf`);
    }
}
