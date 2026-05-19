import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { PaginatedResponse } from '../../sales/services/customer.service';
import { SupplierQuery } from './supplier.service';
import { environment } from 'src/environments/environment';

export interface PartResponse {
    id: string;
    name: string;
    displayName: string;
    description: string;
    richDescription?: string | null;
    partNumber: string;
    sku: string;
    barcode?: string | null;
    categoryId: string;
    categoryName: string;
    brandId: string | null;
    brandName: string | null;
    brandCode: string | null;
    baseUnitId: string | null;
    baseUnitName: string | null;
    baseUnitCode: string | null;
    unitId: string | null;
    unitName: string | null;
    unitCode: string | null;
    costPrice: number;
    sellingPrice: number;
    // Effective prices: OVERRIDE = variant price, ADDITIVE = base + delta
    effectiveCostPrice: number;
    effectiveSellingPrice: number;
    // Variant fields (populated when flattenVariants = true)
    hasVariants: boolean;
    variantCount: number;
    isVariant: boolean;
    variantId?: string | null;
    variantName?: string | null;
    variantCode?: string | null;
    variantSKU?: string | null;
    variantBarcode?: string | null;
    pricingMode?: string | null;
    minimumStock: number;
    isActive: boolean;
    // Universal product fields
    tags?: string | null;
    productType: string;
    isPerishable: boolean;
    weightKg?: number | null;
    widthCm?: number | null;
    heightCm?: number | null;
    depthCm?: number | null;
    taxCode?: string | null;
    // Warranty
    hasWarranty: boolean;
    warrantyPeriodMonths: number | null;
    warrantyType: string | null;
    warrantyTerms: string | null;
    warrantyCertificateTemplate: string | null;
    createdBy: string;
    modifiedBy: string;
}

export interface CreatePartRequest {
    name: string;
    description: string;
    richDescription?: string | null;
    partNumber: string;
    sku: string;
    barcode?: string | null;
    categoryId: string;
    brandId: string | null;
    baseUnitId: string | null;
    unitId: string | null;
    costPrice: number;
    sellingPrice: number;
    minimumStock: number;
    // Universal product fields
    tags?: string | null;
    productType: string;
    isPerishable: boolean;
    weightKg?: number | null;
    widthCm?: number | null;
    heightCm?: number | null;
    depthCm?: number | null;
    taxCode?: string | null;
    // Warranty
    hasWarranty: boolean;
    warrantyPeriodMonths: number | null;
    warrantyType: string | null;
    warrantyTerms: string | null;
    warrantyCertificateTemplate: string | null;
}

export interface UpdatePartRequest {
    id: string;
    name: string;
    description: string;
    richDescription?: string | null;
    sku: string;
    barcode?: string | null;
    categoryId: string;
    brandId: string | null;
    baseUnitId: string | null;
    unitId: string | null;
    costPrice: number;
    sellingPrice: number;
    minimumStock: number;
    isActive: boolean;
    // Universal product fields
    tags?: string | null;
    productType: string;
    isPerishable: boolean;
    weightKg?: number | null;
    widthCm?: number | null;
    heightCm?: number | null;
    depthCm?: number | null;
    taxCode?: string | null;
    // Warranty
    hasWarranty: boolean;
    warrantyPeriodMonths: number | null;
    warrantyType: string | null;
    warrantyTerms: string | null;
    warrantyCertificateTemplate: string | null;
}

export interface PaginatedPartResponse {
    items: PartResponse[];
    pageNumber: number;
    pageSize: number;
    totalCount: number;
    totalPages: number;
    hasPreviousPage: boolean;
    hasNextPage: boolean;
}

export interface VehicleCompatibilityResponse {
    id: string;
    partId: string;
    vehicleId: string;
    vehicleMake: string;
    vehicleModel: string;
    vehicleYear: number;
    vehicleEngineType: string;
    isCompatible: boolean;
    notes: string;
}

export interface PartsQuery {
    search: string;
    pageSize: number;
    pageNumber: number;
    isActive?: boolean;
    flattenVariants?: boolean;
}

@Injectable({
    providedIn: 'root'
})
export class PartService {
    private readonly http = inject(HttpClient);
    private readonly apiUrl = `${environment.apiUrl}/parts`;

    /**
     * Get all parts
     */
    getAllParts(): Observable<PartResponse[]> {
        return this.http.get<PartResponse[]>(this.apiUrl);
    }

    /**
     * Get all active parts
     */

    getActiveParts(): Observable<PartResponse[]> {
        return this.http.get<PartResponse[]>(`${this.apiUrl}/active`);
    }


    /**
  * Get paginated suppliers with optional search
  */
    getParts(rQuery: PartsQuery): Observable<PaginatedResponse<PartResponse>> {
        return this.http.post<PaginatedResponse<PartResponse>>(`${this.apiUrl}/list`, rQuery);
    }

    /**
     * Get part by ID
     */
    getPartById(id: string): Observable<PartResponse> {
        return this.http.get<PartResponse>(`${this.apiUrl}/${id}`);
    }

    /**
     * Create new part
     */
    createPart(request: CreatePartRequest): Observable<PartResponse> {
        return this.http.post<PartResponse>(this.apiUrl, request);
    }

    /**
     * Update existing part
     */
    updatePart(id: string, request: UpdatePartRequest): Observable<PartResponse> {
        return this.http.put<PartResponse>(`${this.apiUrl}/${id}`, request);
    }

    /**
     * Activate part
     */
    activatePart(id: string): Observable<PartResponse> {
        return this.http.patch<PartResponse>(`${this.apiUrl}/${id}/activate`, {});
    }

    /**
     * Deactivate part
     */
    deactivatePart(id: string): Observable<PartResponse> {
        return this.http.patch<PartResponse>(`${this.apiUrl}/${id}/deactivate`, {});
    }

    /**
     * Delete part
     */
    deletePart(id: string): Observable<void> {
        return this.http.delete<void>(`${this.apiUrl}/${id}`);
    }

    /**
     * Get compatible vehicles for a part
     */
    getPartCompatibleVehicles(partId: string): Observable<VehicleCompatibilityResponse[]> {
        return this.http.get<VehicleCompatibilityResponse[]>(`${this.apiUrl}/${partId}/compatible-vehicles`);
    }
}
