import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from 'src/environments/environment';

export interface PricingValidationResponse {
    effectivePrice: number;
}

export interface PricingCalculationResponse {
    effectivePrice: number;
    mrp: number;
    isValid: boolean;
    validationMessage?: string | null;
}

export interface LineInfoResponse {
    costPrice: number;
    discountId?: string;
    discountName?: string;
    discountType?: string;
    discountValue: number;
    discountAmount: number;
    appliedLevel: 'VARIANT' | 'PRODUCT' | 'CART' | 'NONE';
    finalPrice: number;
}

export interface PriceOverrideApprovalResponse {
    token: string;
    expiresAt: string;
}

@Injectable({ providedIn: 'root' })
export class PricingValidationService {
    private readonly http = inject(HttpClient);
    private readonly apiUrl = `${environment.apiUrl}/v1/pricing`;

    validateLine(partId: string, unitPrice: number, discountPercent: number, unitId?: string | null): Observable<PricingValidationResponse> {
        return this.http.post<PricingValidationResponse>(`${this.apiUrl}/validate-line`, {
            partId,
            unitPrice,
            discountPercent,
            unitId: unitId ?? null
        });
    }

    calculateLine(partId: string, unitPrice: number, discountPercent: number, unitId?: string | null): Observable<PricingCalculationResponse> {
        return this.http.post<PricingCalculationResponse>(`${this.apiUrl}/calculate-line`, {
            partId,
            unitPrice,
            discountPercent,
            unitId: unitId ?? null
        });
    }

    /**
     * Combines getUnitCost + DiscountService.resolveItemDiscount into one round trip — use this
     * instead of calling both separately when a cart line needs both pieces of data at once
     * (e.g. adding a part to the POS cart).
     */
    getLineInfo(partId: string, unitPrice: number, variantId?: string | null): Observable<LineInfoResponse> {
        return this.http.get<LineInfoResponse>(`${this.apiUrl}/line-info`, {
            params: { partId, unitPrice: unitPrice.toString(), ...(variantId ? { variantId } : {}) }
        });
    }

    /**
     * The industry-standard "manager approves this specific sale" flow — verifies the given
     * credentials belong to someone allowed to approve price overrides and, on success, returns a
     * short-lived single-use token to attach to the checkout request.
     */
    requestPriceOverride(username: string, password: string): Observable<PriceOverrideApprovalResponse> {
        return this.http.post<PriceOverrideApprovalResponse>(`${this.apiUrl}/request-override`, { username, password });
    }
}
