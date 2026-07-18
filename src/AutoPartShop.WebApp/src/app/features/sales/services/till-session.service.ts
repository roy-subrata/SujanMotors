import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from 'src/environments/environment';
import { PdfDownloadService } from '@/shared/services/pdf-download.service';

export interface OpenTillSessionRequest {
    terminalLabel: string;
    openingFloat: number;
    shiftLabel?: string | null;
    notes: string;
}

export interface RecordCashDropRequest {
    amount: number;
    notes: string;
}

export interface CloseTillSessionRequest {
    countedAmount: number;
    notes: string;
}

export interface TillCashDropResponse {
    id: string;
    amount: number;
    droppedAt: string;
    notes: string;
}

export interface TillSessionResponse {
    id: string;
    cashierId: string;
    cashierName: string;
    terminalLabel: string;
    shiftLabel?: string | null;
    openedAt: string;
    closedAt?: string | null;
    openingFloat: number;
    closingCountedAmount?: number | null;
    status: string; // OPEN | CLOSED
    cashSalesTotal: number;
    cashRefundsTotal: number;
    cashDropsTotal: number;
    expectedAmount: number;
    overShortAmount: number;
    notes: string;
    cashDrops: TillCashDropResponse[];
}

export interface TillSessionQuery {
    cashierId?: string;
    status?: string;
    fromDate?: string;
    toDate?: string;
    pageNumber: number;
    pageSize: number;
}

export interface TillSessionSearchResponse {
    data: TillSessionResponse[];
    totalCount: number;
    pageNumber: number;
    pageSize: number;
}

@Injectable({ providedIn: 'root' })
export class TillSessionService {
    private readonly http = inject(HttpClient);
    private readonly pdfDownload = inject(PdfDownloadService);
    private readonly apiUrl = `${environment.apiUrl}/v1/till-sessions`;

    /** Opens a new till session for the current logged-in user. */
    open(request: OpenTillSessionRequest): Observable<TillSessionResponse> {
        return this.http.post<TillSessionResponse>(`${this.apiUrl}/open`, request);
    }

    getById(id: string): Observable<TillSessionResponse> {
        return this.http.get<TillSessionResponse>(`${this.apiUrl}/${id}`);
    }

    /** The calling user's own currently-open session, or null if none. */
    getCurrent(): Observable<TillSessionResponse | null> {
        return this.http.get<TillSessionResponse | null>(`${this.apiUrl}/current`);
    }

    search(query: TillSessionQuery): Observable<TillSessionSearchResponse> {
        return this.http.post<TillSessionSearchResponse>(`${this.apiUrl}/list`, query);
    }

    recordCashDrop(id: string, request: RecordCashDropRequest): Observable<TillSessionResponse> {
        return this.http.post<TillSessionResponse>(`${this.apiUrl}/${id}/cash-drops`, request);
    }

    /** Closes the session and freezes its reconciliation. */
    close(id: string, request: CloseTillSessionRequest): Observable<TillSessionResponse> {
        return this.http.post<TillSessionResponse>(`${this.apiUrl}/${id}/close`, request);
    }

    /** Download the Shift Report PDF. Only available once the session is CLOSED. */
    downloadPdf(id: string, terminalLabel: string, openedAt: string): Observable<void> {
        const day = (openedAt || '').split('T')[0]?.replace(/-/g, '') || '';
        const terminal = (terminalLabel || '').replace(/\s+/g, '');
        return this.pdfDownload.downloadGet(`${this.apiUrl}/${id}/pdf`, `shift-report-${day}-${terminal}.pdf`);
    }
}
