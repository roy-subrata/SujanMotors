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

@Injectable({ providedIn: 'root' })
export class PricingValidationService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/pricing`;

  validateLine(partId: string, unitPrice: number, discountPercent: number, unitId?: string | null): Observable<PricingValidationResponse> {
    return this.http.post<PricingValidationResponse>(`${this.apiUrl}/validate-line`, {
      partId, unitPrice, discountPercent, unitId: unitId ?? null
    });
  }

  calculateLine(partId: string, unitPrice: number, discountPercent: number, unitId?: string | null): Observable<PricingCalculationResponse> {
    return this.http.post<PricingCalculationResponse>(`${this.apiUrl}/calculate-line`, {
      partId, unitPrice, discountPercent, unitId: unitId ?? null
    });
  }
}
