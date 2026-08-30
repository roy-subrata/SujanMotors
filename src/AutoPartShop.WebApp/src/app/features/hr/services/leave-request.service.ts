import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from 'src/environments/environment';
import { PaginatedResponse } from './employee.service';
import { LeaveRequestStatus } from 'src/app/shared/models/status.types';

export interface LeaveRequestResponse {
    id: string;
    employeeId: string;
    employeeCode: string;
    employeeName: string;
    leaveType: string;
    fromDate: string;
    toDate: string;
    totalDays: number;
    reason: string;
    status: LeaveRequestStatus;
    decisionBy: string;
    decisionAt: string | null;
    decisionNotes: string;
    createdAt: string;
}

export interface CreateLeaveRequestRequest {
    employeeId: string;
    leaveType: string;
    fromDate: string;
    toDate: string;
    reason: string;
}

export interface UpdateLeaveRequestRequest {
    leaveType: string;
    fromDate: string;
    toDate: string;
    reason: string;
}

export interface LeaveRequestQuery {
    search?: string;
    status?: LeaveRequestStatus | '';
    employeeId?: string;
    pageSize: number;
    pageNumber: number;
}

@Injectable({ providedIn: 'root' })
export class LeaveRequestService {
    private readonly http = inject(HttpClient);
    private readonly apiUrl = `${environment.apiUrl}/v1/leaverequests`;

    getLeaveRequests(query: LeaveRequestQuery): Observable<PaginatedResponse<LeaveRequestResponse>> {
        // Backend Status is a nullable enum — '' (the "All Statuses" sentinel) fails
        // JSON deserialization, so drop it rather than send an empty string.
        const { status, ...rest } = query;
        const body: LeaveRequestQuery = status ? query : rest;
        return this.http.post<PaginatedResponse<LeaveRequestResponse>>(`${this.apiUrl}/list`, body);
    }

    createLeaveRequest(request: CreateLeaveRequestRequest): Observable<{ id: string }> {
        return this.http.post<{ id: string }>(this.apiUrl, request);
    }

    updateLeaveRequest(id: string, request: UpdateLeaveRequestRequest): Observable<{ id: string }> {
        return this.http.put<{ id: string }>(`${this.apiUrl}/${id}`, request);
    }

    approveLeaveRequest(id: string, notes = ''): Observable<{ id: string; status: LeaveRequestStatus }> {
        return this.http.patch<{ id: string; status: LeaveRequestStatus }>(`${this.apiUrl}/${id}/approve`, { notes });
    }

    rejectLeaveRequest(id: string, notes = ''): Observable<{ id: string; status: LeaveRequestStatus }> {
        return this.http.patch<{ id: string; status: LeaveRequestStatus }>(`${this.apiUrl}/${id}/reject`, { notes });
    }

    deleteLeaveRequest(id: string): Observable<void> {
        return this.http.delete<void>(`${this.apiUrl}/${id}`);
    }
}
