import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from 'src/environments/environment';
import { ReportPageConfig } from '../report-configs';

/**
 * Uniform query posted to every report endpoint; each report reads only the
 * filters that apply to it (mirrors the API's shared ReportQuery DTO).
 */
export interface ReportQuery {
    search?: string;
    pageNumber?: number;
    pageSize?: number;
    fromDate?: string;
    toDate?: string;
    warehouseId?: string;
    categoryId?: string;
    brandId?: string;
    supplierId?: string;
    customerId?: string;
    partId?: string;
    groupBy?: string;
    channel?: string;
    movementType?: string;
    paymentMethod?: string;
    customerType?: string;
    daysAhead?: number;
    noSaleDays?: number;
    asOfDate?: string;
    includeZeroStock?: boolean;
    includeExpired?: boolean;
}

export interface PaginationMeta {
    pageNumber: number;
    pageSize: number;
    totalCount: number;
    totalPages: number;
}

/** Paged report response; `totals` is present only for reports with hasTotals. */
export interface ReportPageResponse<TRow = Record<string, unknown>, TTotals = Record<string, unknown>> {
    data: TRow[];
    pagination: PaginationMeta;
    totals?: TTotals;
}

export type ReportExportFormat = 'xlsx' | 'pdf';

@Injectable({ providedIn: 'root' })
export class ReportsService {
    private readonly http = inject(HttpClient);
    private readonly baseUrl = environment.apiUrl;

    /** Run a paged report ({ data, pagination, totals? }). */
    runPaged(config: ReportPageConfig, query: ReportQuery): Observable<ReportPageResponse> {
        return this.http.post<ReportPageResponse>(`${this.baseUrl}/${config.endpoint}`, query);
    }

    /** Run a non-paged (grouped/summary) report returning a plain row array. */
    runList(config: ReportPageConfig, query: ReportQuery): Observable<Record<string, unknown>[]> {
        return this.http.post<Record<string, unknown>[]>(`${this.baseUrl}/${config.endpoint}`, query);
    }

    /** Download the report as a file; requires the reports.export permission. */
    export(config: ReportPageConfig, query: ReportQuery, format: ReportExportFormat): Observable<Blob> {
        return this.http.post(`${this.baseUrl}/${config.endpoint}/export?format=${format}`, query, {
            responseType: 'blob'
        });
    }
}
