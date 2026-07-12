import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from 'src/environments/environment';

export interface PayslipResponse {
    id: string;
    employeeId: string;
    employeeCode: string;
    employeeName: string;
    designation: string;
    department: string;
    monthlySalary: number;
    daysInMonth: number;
    presentDays: number;
    lateDays: number;
    halfDays: number;
    absentDays: number;
    leaveDays: number;
    holidayDays: number;
    overtimeAmount: number;
    bonusAmount: number;
    otherAllowance: number;
    commissionAmount: number;
    monthlySalesTotal: number;
    advanceDeduction: number;
    taxDeduction: number;
    otherDeduction: number;
    adjustmentNotes: string;
    absenceDeduction: number;
    grossPay: number;
    totalDeduction: number;
    netPay: number;
}

export interface PayrollRunResponse {
    id: string;
    runCode: string;
    year: number;
    month: number;
    status: string;
    currency: string;
    totalGross: number;
    totalDeductions: number;
    totalNet: number;
    employeeCount: number;
    approvedBy: string;
    approvedAt: string | null;
    paidBy: string;
    paidAt: string | null;
    paymentMethod: string;
    expenseId: string | null;
    notes: string;
    createdAt: string;
    payslips: PayslipResponse[];
}

export interface UpdatePayslipRequest {
    overtimeAmount: number;
    bonusAmount: number;
    otherAllowance: number;
    commissionAmount: number;
    advanceDeduction: number;
    taxDeduction: number;
    otherDeduction: number;
    adjustmentNotes: string;
}

export interface SendPayslipsResponse {
    emailsSent: number;
    smsSent: number;
    skipped: number;
}

@Injectable({ providedIn: 'root' })
export class PayrollService {
    private readonly http = inject(HttpClient);
    private readonly apiUrl = `${environment.apiUrl}/v1/payroll`;

    getRuns(): Observable<PayrollRunResponse[]> {
        return this.http.get<PayrollRunResponse[]>(this.apiUrl);
    }

    getRun(id: string): Observable<PayrollRunResponse> {
        return this.http.get<PayrollRunResponse>(`${this.apiUrl}/${id}`);
    }

    generate(year: number, month: number, notes = ''): Observable<PayrollRunResponse> {
        return this.http.post<PayrollRunResponse>(`${this.apiUrl}/generate`, { year, month, notes });
    }

    updatePayslip(runId: string, payslipId: string, request: UpdatePayslipRequest): Observable<PayrollRunResponse> {
        return this.http.put<PayrollRunResponse>(`${this.apiUrl}/${runId}/payslips/${payslipId}`, request);
    }

    approve(id: string): Observable<PayrollRunResponse> {
        return this.http.patch<PayrollRunResponse>(`${this.apiUrl}/${id}/approve`, {});
    }

    pay(id: string, paymentMethod: string): Observable<PayrollRunResponse> {
        return this.http.patch<PayrollRunResponse>(`${this.apiUrl}/${id}/pay`, { paymentMethod });
    }

    sendPayslips(id: string): Observable<SendPayslipsResponse> {
        return this.http.post<SendPayslipsResponse>(`${this.apiUrl}/${id}/send-payslips`, {});
    }

    deleteRun(id: string): Observable<void> {
        return this.http.delete<void>(`${this.apiUrl}/${id}`);
    }
}
