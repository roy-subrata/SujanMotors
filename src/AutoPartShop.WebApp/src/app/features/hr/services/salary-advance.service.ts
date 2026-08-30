import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from 'src/environments/environment';
import { PaginatedResponse } from './employee.service';
import { SalaryAdvanceStatus } from 'src/app/shared/models/status.types';

export interface SalaryAdvanceResponse {
    id: string;
    employeeId: string;
    employeeCode: string;
    employeeName: string;
    advanceDate: string;
    amount: number;
    paymentMethod: string;
    notes: string;
    status: SalaryAdvanceStatus;
    settledAt: string | null;
    settledRunCode: string | null;
    approvedBy: string | null;
    approvedAt: string | null;
    rejectionReason: string | null;
    createdAt: string;
}

export interface GiveAdvanceRequest {
    employeeId: string;
    advanceDate: string;
    amount: number;
    paymentMethod: string;
    notes: string;
}

export interface SalaryAdvanceQuery {
    search?: string;
    status?: SalaryAdvanceStatus | '';
    employeeId?: string;
    pageSize: number;
    pageNumber: number;
}

@Injectable({ providedIn: 'root' })
export class SalaryAdvanceService {
    private readonly http = inject(HttpClient);
    private readonly apiUrl = `${environment.apiUrl}/v1/salaryadvances`;

    getAdvances(query: SalaryAdvanceQuery): Observable<PaginatedResponse<SalaryAdvanceResponse>> {
        // Backend Status is a nullable enum — '' (the "All Statuses" sentinel) fails
        // JSON deserialization, so drop it rather than send an empty string.
        const { status, ...rest } = query;
        const body: SalaryAdvanceQuery = status ? query : rest;
        return this.http.post<PaginatedResponse<SalaryAdvanceResponse>>(`${this.apiUrl}/list`, body);
    }

    /**
     * Raises a REQUESTED advance. Nothing is paid out and no cash-book expense is posted until
     * it is approved.
     */
    requestAdvance(request: GiveAdvanceRequest): Observable<{ id: string; status: string }> {
        return this.http.post<{ id: string; status: string }>(this.apiUrl, request);
    }

    /** Authorises the payout: posts the cash-book expense and moves the advance to OUTSTANDING. */
    approveAdvance(id: string): Observable<{ id: string; status: string }> {
        return this.http.patch<{ id: string; status: string }>(`${this.apiUrl}/${id}/approve`, {});
    }

    rejectAdvance(id: string, reason: string): Observable<{ id: string; status: string }> {
        return this.http.patch<{ id: string; status: string }>(`${this.apiUrl}/${id}/reject`, { reason });
    }

    cancelAdvance(id: string): Observable<void> {
        return this.http.delete<void>(`${this.apiUrl}/${id}`);
    }
}
