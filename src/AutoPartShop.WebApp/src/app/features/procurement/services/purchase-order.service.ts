import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { PaginatedResponse } from '@/features/sales/services/customer.service';
import { environment } from 'src/environments/environment';

export interface PurchaseOrderLineResponse {
    id: string;
    partId: string;
    partName: string;
    variantId?: string | null;
    variantName?: string | null;
    variantCode?: string | null;
    displayName: string;
    partBaseUnitId?: string;
    unitId?: string;
    unitName?: string;
    unitSymbol?: string;
    quantity: number;
    quantityInBaseUnit: number;
    receivedQuantity: number;
    receivedQuantityInBaseUnit: number;
    remainingQuantity: number;
    unitPrice: number;
    lineTotal: number;
    partDefaultSellingPrice: number;
    partMinMarginPercent: number;
}

export interface PurchaseOrderResponse {
    id: string;
    poNumber: string;
    supplierId: string;
    supplierName?: string;
    supplierCode?: string;
    orderDate: string;
    deliveryDate: string;
    paymentTerms?: string;
    pymentStatus:string;
    status: string; // DRAFT, SUBMITTED, CONFIRMED, PARTIAL, DELIVERED, CANCELLED
    subTotal: number;
    taxAmount: number;
    taxPercentage: number;
    discount: number;
    discountPercentage: number;
    discountAmount?: number;
    discountType?: string; // 'BULK' or 'TOTAL'
    grandTotal: number;
    amountPaid: number;
    outstandingAmount: number;
    isOverdue: boolean;
    notes: string;
    lines: PurchaseOrderLineResponse[];
    createdAt: string;
}

export interface CreatePurchaseOrderRequest {
    supplierId: string;
    deliveryDate: string;
    taxPercentage: number;
    discountPercentage: number;
    discountAmount?: number;
    discountType?: string;
    notes: string;
    lineItems: CreatePurchaseOrderLineRequest[];
}

export interface CreatePurchaseOrderLineRequest {
    partId: string;
    variantId?: string | null;
    unitId?: string;
    quantity: number;
    unitPrice: number;
}

export interface UpdatePurchaseOrderRequest {
    id: string;
    supplierId: string;
    deliveryDate: string;
    taxPercentage: number;
    discountPercentage: number;
    discountAmount?: number;
    discountType?: string;
    notes: string;
    lineItems: CreatePurchaseOrderLineRequest[];
}

export interface PaginatedPurchaseOrderResponse {
    items: PurchaseOrderResponse[];
    pageNumber: number;
    pageSize: number;
    totalCount: number;
    totalPages: number;
    hasPreviousPage: boolean;
    hasNextPage: boolean;
}
export interface PurchaeOrderQuery {
    search: string;
    pageSize: number;
    pageNumber: number;
    status?:string,
    supplierId?: string;
    fromDate?:string;
    toDate?:string
}

@Injectable({
    providedIn: 'root'
})
export class PurchaseOrderService {
    private readonly http = inject(HttpClient);
    private readonly apiUrl = `${environment.apiUrl}/purchaseorder`;

    /**
     * Get all purchase orders
     */
    getAllPurchaseOrders(): Observable<PurchaseOrderResponse[]> {
        return this.http.get<PurchaseOrderResponse[]>(this.apiUrl);
    }

    /**
     * Get paginated purchase orders with optional search
     */
    getPurchaseOrders(query:PurchaeOrderQuery): Observable<PaginatedResponse<PurchaseOrderResponse>> {
        return this.http.post<PaginatedResponse<PurchaseOrderResponse>>(`${this.apiUrl}/list`, query);
    }

    /**
     * Get purchase order by ID
     */
    getPurchaseOrderById(id: string): Observable<PurchaseOrderResponse> {
        return this.http.get<PurchaseOrderResponse>(`${this.apiUrl}/${id}`);
    }

    /**
     * Get purchase order by PO Number
     */
    getPurchaseOrderByNumber(poNumber: string): Observable<PurchaseOrderResponse> {
        return this.http.get<PurchaseOrderResponse>(`${this.apiUrl}/number/${poNumber}`);
    }

    /**
     * Get purchase orders by supplier
     */
    getPurchaseOrdersBySupplier(supplierId: string): Observable<PurchaseOrderResponse[]> {
        return this.http.get<PurchaseOrderResponse[]>(`${this.apiUrl}/supplier/${supplierId}`);
    }

    /**
     * Get purchase orders by status
     */
    getPurchaseOrdersByStatus(status: string): Observable<PurchaseOrderResponse[]> {
        return this.http.get<PurchaseOrderResponse[]>(`${this.apiUrl}/status/${status}`);
    }

    /**
     * Get overdue purchase orders
     */
    getOverduePurchaseOrders(): Observable<PurchaseOrderResponse[]> {
        return this.http.get<PurchaseOrderResponse[]>(`${this.apiUrl}/overdue`);
    }

    /**
     * Create new purchase order
     */
    createPurchaseOrder(request: CreatePurchaseOrderRequest): Observable<PurchaseOrderResponse> {
        return this.http.post<PurchaseOrderResponse>(this.apiUrl, request);
    }

    /**
     * Update purchase order
     */
    updatePurchaseOrder(id: string, request: UpdatePurchaseOrderRequest): Observable<PurchaseOrderResponse> {
        return this.http.put<PurchaseOrderResponse>(`${this.apiUrl}/${id}`, request);
    }

    /**
     * Submit purchase order
     */
    submitPurchaseOrder(id: string): Observable<PurchaseOrderResponse> {
        return this.http.patch<PurchaseOrderResponse>(`${this.apiUrl}/${id}/submit`, {});
    }

    /**
     * Confirm purchase order
     */
    confirmPurchaseOrder(id: string): Observable<PurchaseOrderResponse> {
        return this.http.patch<PurchaseOrderResponse>(`${this.apiUrl}/${id}/confirm`, {});
    }

    /**
     * Cancel purchase order
     */
    cancelPurchaseOrder(id: string): Observable<PurchaseOrderResponse> {
        return this.http.patch<PurchaseOrderResponse>(`${this.apiUrl}/${id}/cancel`, {});
    }

    /**
     * Delete purchase order
     */
    deletePurchaseOrder(id: string): Observable<void> {
        return this.http.delete<void>(`${this.apiUrl}/${id}`);
    }
}
