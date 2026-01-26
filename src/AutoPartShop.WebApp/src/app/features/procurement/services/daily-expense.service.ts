import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface CreateDailyExpenseRequest {
    expenseDate: string;
    category: string;
    amount: number;
    description: string;
    paymentMethod: string;
    vendorName: string;
    referenceNumber: string;
    notes: string;
    isRecurring: boolean;
    recurrencePattern: string;
}

export interface UpdateDailyExpenseRequest {
    expenseDate: string;
    category: string;
    amount: number;
    description: string;
    paymentMethod: string;
    vendorName: string;
    referenceNumber: string;
    notes: string;
    isRecurring: boolean;
    recurrencePattern: string;
}

export interface DailyExpenseResponse {
    id: string;
    expenseDate: string;
    category: string;
    amount: number;
    description: string;
    paymentMethod: string;
    vendorName: string;
    referenceNumber: string;
    notes: string;
    isRecurring: boolean;
    recurrencePattern: string;
    createdDate: string;
    modifiedDate: string;
    createdBy: string;
    modifiedBy: string;
}

export interface ExpenseSummaryByCategory {
    category: string;
    totalAmount: number;
    expenseCount: number;
    averageAmount: number;
    minAmount: number;
    maxAmount: number;
}

export interface ExpenseSummaryByPeriod {
    startDate: string;
    endDate: string;
    totalExpenses: number;
    expenseCount: number;
    averageDailyExpense: number;
    byCategory: ExpenseSummaryByCategory[];
    recentExpenses: DailyExpenseResponse[];
}

export interface ExpenseCategory {
    value: string;
    label: string;
    icon: string;
}

@Injectable({
    providedIn: 'root'
})
export class DailyExpenseService {
    private readonly http = inject(HttpClient);
    private readonly apiUrl = 'http://localhost:5292/api/daily-expense';

    getAll(): Observable<DailyExpenseResponse[]> {
        return this.http.get<DailyExpenseResponse[]>(this.apiUrl);
    }

    getById(id: string): Observable<DailyExpenseResponse> {
        return this.http.get<DailyExpenseResponse>(`${this.apiUrl}/${id}`);
    }

    getByDateRange(startDate: string, endDate: string): Observable<DailyExpenseResponse[]> {
        const params = new HttpParams()
            .set('startDate', startDate)
            .set('endDate', endDate);
        return this.http.get<DailyExpenseResponse[]>(`${this.apiUrl}/by-date-range`, { params });
    }

    getByCategory(category: string): Observable<DailyExpenseResponse[]> {
        return this.http.get<DailyExpenseResponse[]>(`${this.apiUrl}/by-category/${category}`);
    }

    getSummary(startDate: string, endDate: string): Observable<ExpenseSummaryByPeriod> {
        const params = new HttpParams()
            .set('startDate', startDate)
            .set('endDate', endDate);
        return this.http.get<ExpenseSummaryByPeriod>(`${this.apiUrl}/summary`, { params });
    }

    getCategories(): Observable<ExpenseCategory[]> {
        return this.http.get<ExpenseCategory[]>(`${this.apiUrl}/categories`);
    }

    create(request: CreateDailyExpenseRequest): Observable<DailyExpenseResponse> {
        return this.http.post<DailyExpenseResponse>(this.apiUrl, request);
    }

    update(id: string, request: UpdateDailyExpenseRequest): Observable<DailyExpenseResponse> {
        return this.http.put<DailyExpenseResponse>(`${this.apiUrl}/${id}`, request);
    }

    delete(id: string): Observable<void> {
        return this.http.delete<void>(`${this.apiUrl}/${id}`);
    }
}
