import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from 'src/environments/environment';

export interface SalesOrderLineRequest {
    partId: string;
    unitId?: string;
    quantity: number;
    unitPrice: number;
    discount: number;
}

export interface CreateSalesOrderRequest {
    customerId: string;
    customerName: string;
    customerEmail: string;
    customerPhone: string;
    customerCity: string;
    technicianId?: string;
    technicianName?: string;
    deliveryDate: string;
    notes: string;
    currency: string;
    discount: number;
    lines: SalesOrderLineRequest[];
}

export interface SalesOrderLineResponse {
    id: string;
    partId: string;
    partName?: string;
    partSku?: string;
    unitId?: string;
    unitName?: string;
    unitSymbol?: string;
    quantity: number;
    quantityInBaseUnit: number;
    shippedQuantity: number;
    shippedQuantityInBaseUnit: number;
    unitPrice: number;
    discount: number;
    lineTotal: number;
}

export interface SalesOrderResponse {
    id: string;
    soNumber: string;
    customerId: string;
    customerName: string;
    customerEmail: string;
    customerPhone: string;
    customerCity: string;
    warehouseId?: string;
    technicianId?: string;
    technicianName?: string;
    orderDate: string;
    deliveryDate: string;
    status: string;
    subTotal: number;
    taxAmount: number;
    discount: number;
    grandTotal: number;
    currency: string;
    amountPaid: number;
    outstandingAmount: number;
    isOverdue: boolean;
    notes: string;
    lines: SalesOrderLineResponse[];
    createdAt: string;
}

export interface SaleOrderQuery {
    search: string;
    pageSize: number;
    pageNumber: number;
    status?: string;
    fromDate?:string,
    toDate?:string
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
export class SalesOrderService {
    private readonly http = inject(HttpClient);
    private readonly apiUrl = `${environment.apiUrl}/salesorder`;

    getAllSalesOrders(): Observable<SalesOrderResponse[]> {
        return this.http.get<SalesOrderResponse[]>(this.apiUrl);
    }

    getSalesOrders(query:SaleOrderQuery): Observable<PaginatedResponse<SalesOrderResponse>> {
        return this.http.post<PaginatedResponse<SalesOrderResponse>>(`${this.apiUrl}/list`, query);
    }

    getSalesOrderById(id: string): Observable<SalesOrderResponse> {
        return this.http.get<SalesOrderResponse>(`${this.apiUrl}/${id}`);
    }

    getSalesOrderByNumber(soNumber: string): Observable<SalesOrderResponse> {
        return this.http.get<SalesOrderResponse>(`${this.apiUrl}/number/${soNumber}`);
    }

    getSalesOrdersByCustomer(customerId: string): Observable<SalesOrderResponse[]> {
        return this.http.get<SalesOrderResponse[]>(`${this.apiUrl}/customer/${customerId}`);
    }

    getSalesOrdersByStatus(status: string): Observable<SalesOrderResponse[]> {
        return this.http.get<SalesOrderResponse[]>(`${this.apiUrl}/status/${status}`);
    }

    getOverdueSalesOrders(): Observable<SalesOrderResponse[]> {
        return this.http.get<SalesOrderResponse[]>(`${this.apiUrl}/overdue`);
    }

    createSalesOrder(request: CreateSalesOrderRequest): Observable<SalesOrderResponse> {
        return this.http.post<SalesOrderResponse>(this.apiUrl, request);
    }

    updateSalesOrder(id: string, request: CreateSalesOrderRequest): Observable<SalesOrderResponse> {
        return this.http.put<SalesOrderResponse>(`${this.apiUrl}/${id}`, request);
    }

    confirmSalesOrder(id: string): Observable<SalesOrderResponse> {
        return this.http.patch<SalesOrderResponse>(`${this.apiUrl}/${id}/confirm`, {});
    }

    deleteSalesOrder(id: string): Observable<void> {
        return this.http.delete<void>(`${this.apiUrl}/${id}`);
    }
}
