import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from 'src/environments/environment';

export interface CreateCustomerRequest {
    customerCode: string;
    firstName: string;
    lastName: string;
    companyName: string;
    email: string;
    phone: string;
    alternatePhone?: string;
    billingAddress: string;
    shippingAddress: string;
    city: string;
    state: string;
    postalCode: string;
    country: string;
    customerType: string;
    primaryContactPerson?: string;
    notes?: string;
}

export interface UpdateCustomerRequest extends CreateCustomerRequest {}

export interface CustomerResponse {
    id: string;
    customerCode: string;
    firstName: string;
    lastName: string;
    fullName: string;
    companyName: string;
    email: string;
    phone: string;
    alternatePhone: string;
    billingAddress: string;
    shippingAddress: string;
    city: string;
    state: string;
    postalCode: string;
    country: string;
    customerType: string;
    status: string;
    currentBalance: number;
    advanceAmount: number;
    dueAmount: number;
    canPlaceOrder: boolean;
    primaryContactPerson: string;
    lastPurchaseDate?: string;
    totalPurchaseAmount: number;
    notes: string;
    createdAt: string;
}

export interface CustomerQuery {
    search: string;
    pageSize: number;
    pageNumber: number;
    customerType?: string;
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

@Injectable({ providedIn: 'root' })
export class CustomerService {
    private readonly http = inject(HttpClient);
    private readonly apiUrl = `${environment.apiUrl}/v1/customers`;

    getAllCustomers(): Observable<CustomerResponse[]> {
        return this.http.get<CustomerResponse[]>(this.apiUrl);
    }

    getCustomers(rQuery: CustomerQuery): Observable<PaginatedResponse<CustomerResponse>> {
        return this.http.post<PaginatedResponse<CustomerResponse>>(`${this.apiUrl}/list`, rQuery);
    }

    getCustomerById(id: string): Observable<CustomerResponse> {
        return this.http.get<CustomerResponse>(`${this.apiUrl}/${id}`);
    }

    getCustomerByCode(code: string): Observable<CustomerResponse> {
        return this.http.get<CustomerResponse>(`${this.apiUrl}/code/${code}`);
    }

    getCustomerByEmail(email: string): Observable<CustomerResponse> {
        return this.http.get<CustomerResponse>(`${this.apiUrl}/email/${email}`);
    }

    getCustomersByStatus(status: string): Observable<CustomerResponse[]> {
        return this.http.get<CustomerResponse[]>(`${this.apiUrl}/status/${status}`);
    }

    getActiveCustomers(): Observable<CustomerResponse[]> {
        return this.http.get<CustomerResponse[]>(`${this.apiUrl}/active`);
    }

    getCustomersWithCreditExceeded(): Observable<CustomerResponse[]> {
        return this.http.get<CustomerResponse[]>(`${this.apiUrl}/credit-exceeded`);
    }

    createCustomer(request: CreateCustomerRequest): Observable<CustomerResponse> {
        return this.http.post<CustomerResponse>(this.apiUrl, request);
    }

    updateCustomer(id: string, request: UpdateCustomerRequest): Observable<CustomerResponse> {
        return this.http.put<CustomerResponse>(`${this.apiUrl}/${id}`, request);
    }

    activateCustomer(id: string): Observable<CustomerResponse> {
        return this.http.patch<CustomerResponse>(`${this.apiUrl}/${id}/activate`, {});
    }

    deactivateCustomer(id: string): Observable<CustomerResponse> {
        return this.http.patch<CustomerResponse>(`${this.apiUrl}/${id}/deactivate`, {});
    }

    suspendCustomer(id: string): Observable<CustomerResponse> {
        return this.http.patch<CustomerResponse>(`${this.apiUrl}/${id}/suspend`, {});
    }

    blacklistCustomer(id: string): Observable<CustomerResponse> {
        return this.http.patch<CustomerResponse>(`${this.apiUrl}/${id}/blacklist`, {});
    }

    deleteCustomer(id: string): Observable<void> {
        return this.http.delete<void>(`${this.apiUrl}/${id}`);
    }
}
