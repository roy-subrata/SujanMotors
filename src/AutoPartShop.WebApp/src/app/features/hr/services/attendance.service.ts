import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from 'src/environments/environment';

export interface DailyAttendanceRow {
    employeeId: string;
    employeeCode: string;
    name: string;
    designation: string;
    department: string;
    isMarked: boolean;
    status: string;
    checkInTime: string | null;   // "HH:mm:ss"
    checkOutTime: string | null;
    notes: string;
}

export interface MarkAttendanceEntry {
    employeeId: string;
    status: string;
    checkInTime: string | null;
    checkOutTime: string | null;
    notes: string;
}

export interface MarkAttendanceRequest {
    date: string;  // yyyy-MM-dd
    entries: MarkAttendanceEntry[];
}

export interface MonthlyAttendanceSummaryRow {
    employeeId: string;
    employeeCode: string;
    name: string;
    department: string;
    presentDays: number;
    lateDays: number;
    halfDays: number;
    absentDays: number;
    leaveDays: number;
    holidayDays: number;
    markedDays: number;
}

@Injectable({ providedIn: 'root' })
export class AttendanceService {
    private readonly http = inject(HttpClient);
    private readonly apiUrl = `${environment.apiUrl}/v1/attendance`;

    getDailySheet(date: string): Observable<DailyAttendanceRow[]> {
        const params = new HttpParams().set('date', date);
        return this.http.get<DailyAttendanceRow[]>(`${this.apiUrl}/daily`, { params });
    }

    markDaily(request: MarkAttendanceRequest): Observable<DailyAttendanceRow[]> {
        return this.http.post<DailyAttendanceRow[]>(`${this.apiUrl}/daily`, request);
    }

    getMonthlySummary(year: number, month: number): Observable<MonthlyAttendanceSummaryRow[]> {
        const params = new HttpParams().set('year', year).set('month', month);
        return this.http.get<MonthlyAttendanceSummaryRow[]>(`${this.apiUrl}/summary`, { params });
    }
}
