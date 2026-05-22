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
    private readonly apiUrl = `${environment.apiUrl}/v1/products`;

    getAllParts(): Observable<PartResponse[]> {
        return this.http.get<{ data: PartResponse[] }>(this.apiUrl, { params: new HttpParams().set('pageSize', '500') })
            .pipe(map(r => r.data));
    }

    getActiveParts(): Observable<PartResponse[]> {
        return this.http.get<{ data: PartResponse[] }>(this.apiUrl, {
            params: new HttpParams().set('isActive', 'true').set('pageSize', '500')
        }).pipe(map(r => r.data));
    }

    getParts(rQuery: PartsQuery): Observable<PaginatedResponse<PartResponse>> {
        let params = new HttpParams()
            .set('search', rQuery.search ?? '')
            .set('page', rQuery.pageNumber.toString())
            .set('pageSize', rQuery.pageSize.toString())
            .set('flattenVariants', (rQuery.flattenVariants ?? false).toString());
        if (rQuery.isActive != null) params = params.set('isActive', rQuery.isActive.toString());
        return this.http.get<{ data: PartResponse[]; pagination: any }>(this.apiUrl, { params })
            .pipe(map(r => ({
                data: r.data,
                pagination: { ...r.pagination, pageNumber: r.pagination.page }
            })));
    }

    getPartById(id: string): Observable<PartResponse> {
        return this.http.get<{ data: any }>(`${this.apiUrl}/${id}`)
            .pipe(map(r => {
                const p = r.data;
                return {
                    id: p.id,
                    name: p.name,
                    displayName: p.name,
                    description: p.description ?? '',
                    richDescription: p.richDescription ?? null,
                    partNumber: p.partNumber,
                    sku: p.sku,
                    barcode: p.barcode ?? null,
                    categoryId: p.category?.id ?? '',
                    categoryName: p.category?.name ?? '',
                    brandId: p.brand?.id ?? null,
                    brandName: p.brand?.name ?? null,
                    brandCode: p.brand?.code ?? null,
                    baseUnitId: p.baseUnit?.id ?? null,
                    baseUnitName: p.baseUnit?.name ?? null,
                    baseUnitCode: p.baseUnit?.code ?? null,
                    unitId: p.unit?.id ?? null,
                    unitName: p.unit?.name ?? null,
                    unitCode: p.unit?.code ?? null,
                    costPrice: p.pricing?.costPrice ?? 0,
                    sellingPrice: p.pricing?.sellingPrice ?? 0,
                    effectiveCostPrice: p.pricing?.costPrice ?? 0,
                    effectiveSellingPrice: p.pricing?.sellingPrice ?? 0,
                    hasVariants: p.hasVariants ?? false,
                    variantCount: p.variants?.filter((v: any) => !v.isDefault)?.length ?? 0,
                    isVariant: false,
                    variantId: null,
                    variantName: null,
                    variantCode: null,
                    variantSKU: null,
                    variantBarcode: null,
                    pricingMode: null,
                    minimumStock: p.minimumStock ?? 0,
                    isActive: p.isActive ?? true,
                    tags: p.tags ?? null,
                    productType: p.productType ?? 'PHYSICAL',
                    isPerishable: p.isPerishable ?? false,
                    weightKg: p.dimensions?.weightKg ?? null,
                    widthCm: p.dimensions?.widthCm ?? null,
                    heightCm: p.dimensions?.heightCm ?? null,
                    depthCm: p.dimensions?.depthCm ?? null,
                    taxCode: p.taxCode ?? null,
                    hasWarranty: p.warranty?.hasWarranty ?? false,
                    warrantyPeriodMonths: p.warranty?.periodMonths ?? null,
                    warrantyType: p.warranty?.type ?? null,
                    warrantyTerms: p.warranty?.terms ?? null,
                    warrantyCertificateTemplate: p.warranty?.certificateTemplate ?? null,
                    createdBy: p.createdBy ?? '',
                    modifiedBy: p.modifiedBy ?? '',
                } as PartResponse;
            }));
    }

    createPart(request: CreatePartRequest): Observable<PartResponse> {
        return this.http.post<{ data: PartResponse }>(this.apiUrl, request)
            .pipe(map(r => r.data));
    }

    updatePart(id: string, request: UpdatePartRequest): Observable<PartResponse> {
        return this.http.put<{ data: PartResponse }>(`${this.apiUrl}/${id}`, request)
            .pipe(map(r => r.data));
    }

    activatePart(id: string): Observable<PartResponse> {
        return this.http.patch<{ data: PartResponse }>(`${this.apiUrl}/${id}/status`, { isActive: true })
            .pipe(map(r => r.data));
    }

    deactivatePart(id: string): Observable<PartResponse> {
        return this.http.patch<{ data: PartResponse }>(`${this.apiUrl}/${id}/status`, { isActive: false })
            .pipe(map(r => r.data));
    }

    deletePart(id: string): Observable<void> {
        return this.http.delete<void>(`${this.apiUrl}/${id}`);
    }

    getPartCompatibleVehicles(partId: string): Observable<VehicleCompatibilityResponse[]> {
        return this.http.get<{ data: VehicleCompatibilityResponse[] }>(`${this.apiUrl}/${partId}/compatible-vehicles`)
            .pipe(map(r => r.data));
    }
}
