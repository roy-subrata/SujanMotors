import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from 'src/environments/environment';

export interface EmployeeResponse {
    id: string;
    employeeCode: string;
    name: string;
    phone: string;
    email: string;
    nidNumber: string;
    dateOfBirth: string | null;
    gender: string;
    address: string;
    city: string;
    designation: string;
    department: string;
    joinDate: string;
    endDate: string | null;
    employmentType: string;
    monthlySalary: number;
    currency: string;
    shiftId: string | null;
    shiftName: string | null;
    monthlyTaxDeduction: number;
    commissionRate: number;
    emergencyContactName: string;
    emergencyContactPhone: string;
    status: string;
    notes: string;
    userId: string | null;
    userName: string | null;
    createdAt: string;
}

export interface EmployeeRequest {
    name: string;
    phone: string;
    email: string;
    nidNumber: string;
    dateOfBirth: string | null;
    gender: string;
    address: string;
    city: string;
    designation: string;
    department: string;
    joinDate: string;
    employmentType: string;
    monthlySalary: number;
    shiftId: string | null;
    monthlyTaxDeduction: number;
    commissionRate: number;
    emergencyContactName: string;
    emergencyContactPhone: string;
    notes: string;
    userId: string | null;
}

export interface LinkableUser {
    id: string;
    userName: string;
    fullName: string;
    email: string;
}

export interface PaginatedResponse<T> {
    data: T[];
    pagination: {
        pageNumber: number;
        pageSize: number;
        totalCount: number;
        totalPages: number;
    };
}

export interface EmployeeQuery {
    search?: string;
    status?: string;
    department?: string;
    pageSize: number;
    pageNumber: number;
}

@Injectable({ providedIn: 'root' })
export class EmployeeService {
    private readonly http = inject(HttpClient);
    private readonly apiUrl = `${environment.apiUrl}/v1/employees`;

    getAllEmployees(): Observable<EmployeeResponse[]> {
        return this.http.get<EmployeeResponse[]>(this.apiUrl);
    }

    getEmployees(query: EmployeeQuery): Observable<PaginatedResponse<EmployeeResponse>> {
        return this.http.post<PaginatedResponse<EmployeeResponse>>(`${this.apiUrl}/list`, query);
    }

    getEmployeeById(id: string): Observable<EmployeeResponse> {
        return this.http.get<EmployeeResponse>(`${this.apiUrl}/${id}`);
    }

    getLinkableUsers(employeeId?: string): Observable<LinkableUser[]> {
        let params = new HttpParams();
        if (employeeId) {
            params = params.set('employeeId', employeeId);
        }
        return this.http.get<LinkableUser[]>(`${this.apiUrl}/linkable-users`, { params });
    }

    createEmployee(request: EmployeeRequest): Observable<EmployeeResponse> {
        return this.http.post<EmployeeResponse>(this.apiUrl, request);
    }

    updateEmployee(id: string, request: EmployeeRequest): Observable<EmployeeResponse> {
        return this.http.put<EmployeeResponse>(`${this.apiUrl}/${id}`, request);
    }

    activateEmployee(id: string): Observable<EmployeeResponse> {
        return this.http.patch<EmployeeResponse>(`${this.apiUrl}/${id}/activate`, {});
    }

    deactivateEmployee(id: string): Observable<EmployeeResponse> {
        return this.http.patch<EmployeeResponse>(`${this.apiUrl}/${id}/deactivate`, {});
    }

    deleteEmployee(id: string): Observable<void> {
        return this.http.delete<void>(`${this.apiUrl}/${id}`);
    }
}
