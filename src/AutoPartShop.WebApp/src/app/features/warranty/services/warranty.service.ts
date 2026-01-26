import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from 'src/environments/environment';

// Warranty Registration Interfaces
export interface WarrantyRegistrationResponse {
    id: string;
    warrantyNumber: string;
    partId: string;
    partName: string;
    partSKU: string;
    salesOrderId: string;
    salesOrderNumber: string;
    salesOrderLineId: string;
    customerId: string;
    customerName: string;
    customerPhone: string;
    saleDate: Date;
    warrantyStartDate: Date;
    warrantyExpiryDate: Date;
    warrantyType: string;
    warrantyPeriodMonths: number;
    warrantyTerms: string;
    certificateNumber: string;
    status: string;
    voidReason?: string;
    voidedDate?: Date;
    isValid: boolean;
    daysUntilExpiry: number;
    createdDate: Date;
    createdBy: string;
    modifiedDate: Date;
    modifiedBy: string;
}

export interface CreateWarrantyRegistrationRequest {
    partId: string;
    salesOrderId: string;
    salesOrderLineId: string;
    customerId: string;
    saleDate: Date;
    warrantyStartDate: Date;
    warrantyPeriodMonths: number;
    warrantyType: string;
    warrantyTerms: string;
    certificateNumber?: string;
}

export interface VoidWarrantyRequest {
    reason: string;
}

export interface WarrantySearchParams {
    searchTerm?: string;
    status?: string;
    customerId?: string;
    partId?: string;
    expiryDateFrom?: Date;
    expiryDateTo?: Date;
    pageNumber?: number;
    pageSize?: number;
}

// Warranty Claim Interfaces
export interface WarrantyClaimResponse {
    id: string;
    claimNumber: string;
    warrantyRegistrationId: string;
    warrantyNumber: string;
    salesOrderNumber: string;
    partName: string;
    partSKU: string;
    customerId: string;
    customerName: string;
    customerPhone: string;
    assignedToTechnicianId?: string;
    technicianId?: string;
    technicianName?: string;
    claimDate: Date;
    issueDescription: string;
    serviceType: string;
    priority: string;
    status: string;
    resolutionType?: string;
    rejectionReason?: string;
    rejectedDate?: Date;
    approvedDate?: Date;
    approvedBy?: string;
    serviceStartDate?: Date;
    serviceCompletedDate?: Date;
    serviceCost: number;
    serviceCostCurrency: string;
    serviceNotes?: string;
    resolutionDetails?: string;
    isOpen: boolean;
    canBeModified: boolean;
    daysOpen: number;
    createdDate: Date;
    createdBy: string;
    modifiedDate: Date;
    modifiedBy: string;
}

export interface CreateWarrantyClaimRequest {
    warrantyRegistrationId: string;
    customerId: string;
    claimDate: Date;
    issueDescription: string;
    serviceType: string;
    serviceCostCurrency?: string;
}

export interface ApproveClaimRequest {
    approvedBy: string;
    approvalNotes?: string;
    approvalType?: 'REPAIR' | 'REPLACEMENT' | 'REFUND';
    estimatedCost?: number;
}

export interface RejectClaimRequest {
    rejectionReason: string;
    rejectedBy: string;
}

export interface AssignTechnicianRequest {
    technicianId: string;
    notes?: string;
}

export interface CloseClaimRequest {
    closureNotes?: string;
}

export interface UpdateServiceCostRequest {
    serviceCost: number;
    serviceNotes?: string;
}

export interface CompleteClaimRequest {
    resolutionDetails: string;
}

export interface ClaimSearchParams {
    searchTerm?: string;
    status?: string;
    serviceType?: string;
    customerId?: string;
    technicianId?: string;
    warrantyRegistrationId?: string;
    claimDateFrom?: Date;
    claimDateTo?: Date;
    pageNumber?: number;
    pageSize?: number;
}

export interface PagedResponse<T> {
    data: T[];
    totalCount: number;
    pageNumber: number;
    pageSize: number;
    totalPages: number;
}

@Injectable({
    providedIn: 'root'
})
export class WarrantyService {
    private readonly http = inject(HttpClient);
    private readonly apiUrl = `${environment.apiUrl}/warrantyregistrations`;
    private readonly claimsApiUrl = `${environment.apiUrl}/warrantyclaims`;

    // ==================== WARRANTY REGISTRATIONS ====================

    getAllWarranties(): Observable<WarrantyRegistrationResponse[]> {
        return this.http.get<WarrantyRegistrationResponse[]>(this.apiUrl);
    }

    getWarrantyById(id: string): Observable<WarrantyRegistrationResponse> {
        return this.http.get<WarrantyRegistrationResponse>(`${this.apiUrl}/${id}`);
    }

    getWarrantyByNumber(warrantyNumber: string): Observable<WarrantyRegistrationResponse> {
        return this.http.get<WarrantyRegistrationResponse>(`${this.apiUrl}/warranty-number/${warrantyNumber}`);
    }

    getWarrantiesByCustomer(customerId: string): Observable<WarrantyRegistrationResponse[]> {
        return this.http.get<WarrantyRegistrationResponse[]>(`${this.apiUrl}/customer/${customerId}`);
    }

    getWarrantiesBySalesOrder(salesOrderId: string): Observable<WarrantyRegistrationResponse[]> {
        return this.http.get<WarrantyRegistrationResponse[]>(`${this.apiUrl}/sales-order/${salesOrderId}`);
    }

    getWarrantiesByPart(partId: string): Observable<WarrantyRegistrationResponse[]> {
        return this.http.get<WarrantyRegistrationResponse[]>(`${this.apiUrl}/part/${partId}`);
    }

    getActiveWarranties(): Observable<WarrantyRegistrationResponse[]> {
        return this.http.get<WarrantyRegistrationResponse[]>(`${this.apiUrl}/active`);
    }

    getExpiredWarranties(): Observable<WarrantyRegistrationResponse[]> {
        return this.http.get<WarrantyRegistrationResponse[]>(`${this.apiUrl}/expired`);
    }

    getExpiringWarranties(daysFromNow: number = 30): Observable<WarrantyRegistrationResponse[]> {
        return this.http.get<WarrantyRegistrationResponse[]>(`${this.apiUrl}/expiring?daysFromNow=${daysFromNow}`);
    }

    searchWarranties(params: WarrantySearchParams): Observable<PagedResponse<WarrantyRegistrationResponse>> {
        let httpParams = new HttpParams();

        if (params.searchTerm) httpParams = httpParams.set('searchTerm', params.searchTerm);
        if (params.status) httpParams = httpParams.set('status', params.status);
        if (params.customerId) httpParams = httpParams.set('customerId', params.customerId);
        if (params.partId) httpParams = httpParams.set('partId', params.partId);
        if (params.expiryDateFrom) httpParams = httpParams.set('expiryDateFrom', params.expiryDateFrom.toISOString());
        if (params.expiryDateTo) httpParams = httpParams.set('expiryDateTo', params.expiryDateTo.toISOString());
        if (params.pageNumber) httpParams = httpParams.set('pageNumber', params.pageNumber.toString());
        if (params.pageSize) httpParams = httpParams.set('pageSize', params.pageSize.toString());

        return this.http.get<PagedResponse<WarrantyRegistrationResponse>>(`${this.apiUrl}/search`, { params: httpParams });
    }

    createWarranty(request: CreateWarrantyRegistrationRequest): Observable<WarrantyRegistrationResponse> {
        return this.http.post<WarrantyRegistrationResponse>(this.apiUrl, request);
    }

    voidWarranty(id: string, request: VoidWarrantyRequest): Observable<WarrantyRegistrationResponse> {
        return this.http.patch<WarrantyRegistrationResponse>(`${this.apiUrl}/${id}/void`, request);
    }

    checkWarrantyExpiry(id: string): Observable<WarrantyRegistrationResponse> {
        return this.http.patch<WarrantyRegistrationResponse>(`${this.apiUrl}/${id}/check-expiry`, {});
    }

    deleteWarranty(id: string): Observable<void> {
        return this.http.delete<void>(`${this.apiUrl}/${id}`);
    }

    // ==================== WARRANTY CLAIMS ====================

    getAllClaims(): Observable<WarrantyClaimResponse[]> {
        return this.http.get<WarrantyClaimResponse[]>(this.claimsApiUrl);
    }

    getClaimById(id: string): Observable<WarrantyClaimResponse> {
        return this.http.get<WarrantyClaimResponse>(`${this.claimsApiUrl}/${id}`);
    }

    getClaimByNumber(claimNumber: string): Observable<WarrantyClaimResponse> {
        return this.http.get<WarrantyClaimResponse>(`${this.claimsApiUrl}/claim-number/${claimNumber}`);
    }

    getClaimsByWarranty(warrantyRegistrationId: string): Observable<WarrantyClaimResponse[]> {
        return this.http.get<WarrantyClaimResponse[]>(`${this.claimsApiUrl}/warranty/${warrantyRegistrationId}`);
    }

    getClaimsByCustomer(customerId: string): Observable<WarrantyClaimResponse[]> {
        return this.http.get<WarrantyClaimResponse[]>(`${this.claimsApiUrl}/customer/${customerId}`);
    }

    getClaimsByTechnician(technicianId: string): Observable<WarrantyClaimResponse[]> {
        return this.http.get<WarrantyClaimResponse[]>(`${this.claimsApiUrl}/technician/${technicianId}`);
    }

    getClaimsByStatus(status: string): Observable<WarrantyClaimResponse[]> {
        return this.http.get<WarrantyClaimResponse[]>(`${this.claimsApiUrl}/status/${status}`);
    }

    getPendingClaims(): Observable<WarrantyClaimResponse[]> {
        return this.http.get<WarrantyClaimResponse[]>(`${this.claimsApiUrl}/pending`);
    }

    getInProgressClaims(): Observable<WarrantyClaimResponse[]> {
        return this.http.get<WarrantyClaimResponse[]>(`${this.claimsApiUrl}/in-progress`);
    }

    getOpenClaims(): Observable<WarrantyClaimResponse[]> {
        return this.http.get<WarrantyClaimResponse[]>(`${this.claimsApiUrl}/open`);
    }

    searchClaims(params: ClaimSearchParams): Observable<PagedResponse<WarrantyClaimResponse>> {
        let httpParams = new HttpParams();

        if (params.searchTerm) httpParams = httpParams.set('searchTerm', params.searchTerm);
        if (params.status) httpParams = httpParams.set('status', params.status);
        if (params.serviceType) httpParams = httpParams.set('serviceType', params.serviceType);
        if (params.customerId) httpParams = httpParams.set('customerId', params.customerId);
        if (params.technicianId) httpParams = httpParams.set('technicianId', params.technicianId);
        if (params.warrantyRegistrationId) httpParams = httpParams.set('warrantyRegistrationId', params.warrantyRegistrationId);
        if (params.claimDateFrom) httpParams = httpParams.set('claimDateFrom', params.claimDateFrom.toISOString());
        if (params.claimDateTo) httpParams = httpParams.set('claimDateTo', params.claimDateTo.toISOString());
        if (params.pageNumber) httpParams = httpParams.set('pageNumber', params.pageNumber.toString());
        if (params.pageSize) httpParams = httpParams.set('pageSize', params.pageSize.toString());

        return this.http.get<PagedResponse<WarrantyClaimResponse>>(`${this.claimsApiUrl}/search`, { params: httpParams });
    }

    createClaim(request: CreateWarrantyClaimRequest): Observable<WarrantyClaimResponse> {
        return this.http.post<WarrantyClaimResponse>(this.claimsApiUrl, request);
    }

    submitClaimForReview(id: string): Observable<WarrantyClaimResponse> {
        return this.http.patch<WarrantyClaimResponse>(`${this.claimsApiUrl}/${id}/submit-for-review`, {});
    }

    approveClaim(id: string, request: ApproveClaimRequest): Observable<WarrantyClaimResponse> {
        return this.http.patch<WarrantyClaimResponse>(`${this.claimsApiUrl}/${id}/approve`, request);
    }

    rejectClaim(id: string, request: RejectClaimRequest): Observable<WarrantyClaimResponse> {
        return this.http.patch<WarrantyClaimResponse>(`${this.claimsApiUrl}/${id}/reject`, request);
    }

    assignTechnician(id: string, request: AssignTechnicianRequest): Observable<WarrantyClaimResponse> {
        return this.http.patch<WarrantyClaimResponse>(`${this.claimsApiUrl}/${id}/assign-technician`, request);
    }

    updateServiceCost(id: string, request: UpdateServiceCostRequest): Observable<WarrantyClaimResponse> {
        return this.http.patch<WarrantyClaimResponse>(`${this.claimsApiUrl}/${id}/update-service-cost`, request);
    }

    completeClaim(id: string, request: CompleteClaimRequest): Observable<WarrantyClaimResponse> {
        return this.http.patch<WarrantyClaimResponse>(`${this.claimsApiUrl}/${id}/complete`, request);
    }

    markAsInvestigating(id: string): Observable<WarrantyClaimResponse> {
        return this.http.patch<WarrantyClaimResponse>(`${this.claimsApiUrl}/${id}/mark-investigating`, {});
    }

    markAsInRepair(id: string): Observable<WarrantyClaimResponse> {
        return this.http.patch<WarrantyClaimResponse>(`${this.claimsApiUrl}/${id}/mark-in-repair`, {});
    }

    markAsRepaired(id: string): Observable<WarrantyClaimResponse> {
        return this.http.patch<WarrantyClaimResponse>(`${this.claimsApiUrl}/${id}/mark-repaired`, {});
    }

    markAsReplaced(id: string): Observable<WarrantyClaimResponse> {
        return this.http.patch<WarrantyClaimResponse>(`${this.claimsApiUrl}/${id}/mark-replaced`, {});
    }

    closeClaim(id: string, request?: CloseClaimRequest): Observable<WarrantyClaimResponse> {
        return this.http.patch<WarrantyClaimResponse>(`${this.claimsApiUrl}/${id}/close`, request || {});
    }

    deleteClaim(id: string): Observable<void> {
        return this.http.delete<void>(`${this.claimsApiUrl}/${id}`);
    }
}
