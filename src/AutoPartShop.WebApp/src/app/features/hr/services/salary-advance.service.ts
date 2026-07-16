import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from 'src/environments/environment';
import { PaginatedResponse } from './employee.service';

export interface SalaryAdvanceResponse {
    id: string;
    employeeId: string;
    employeeCode: string;
    employeeName: string;
    advanceDate: string;
    amount: number;
    paymentMethod: string;
    notes: string;
    status: string;
    settledAt: string | null;
    settledRunCode: string | null;
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
    status?: string;
    employeeId?: string;
    pageSize: number;
    pageNumber: number;
}

@Injectable({ providedIn: 'root' })
export class SalaryAdvanceService {
    private readonly http = inject(HttpClient);
    private readonly apiUrl = `${environment.apiUrl}/v1/salaryadvances`;

    getAdvances(query: SalaryAdvanceQuery): Observable<PaginatedResponse<SalaryAdvanceResponse>> {
        return this.http.post<PaginatedResponse<SalaryAdvanceResponse>>(`${this.apiUrl}/list`, query);
    }

    giveAdvance(request: GiveAdvanceRequest): Observable<{ id: string }> {
        return this.http.post<{ id: string }>(this.apiUrl, request);
    }

    cancelAdvance(id: string): Observable<void> {
        return this.http.delete<void>(`${this.apiUrl}/${id}`);
    }
}
