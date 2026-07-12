import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from 'src/environments/environment';

export interface CreateCustomerVehicleRequest {
    registrationNo: string;
    vin: string;
    make: string;
    model: string;
    year?: number | null;
    engineType: string;
    color: string;
    mileage?: number | null;
    notes: string;
    catalogVehicleId?: string | null;
}

export interface UpdateCustomerVehicleRequest extends CreateCustomerVehicleRequest {}

export interface CustomerVehicleResponse {
    id: string;
    customerId: string;
    registrationNo: string;
    vin: string;
    make: string;
    model: string;
    year?: number | null;
    engineType: string;
    color: string;
    mileage?: number | null;
    notes: string;
    catalogVehicleId?: string | null;
    label: string;
    isActive: boolean;
    createdAt: string;
}

@Injectable({ providedIn: 'root' })
export class CustomerVehicleService {
    private readonly http = inject(HttpClient);
    private readonly baseUrl = `${environment.apiUrl}/v1/customers`;

    private vehiclesUrl(customerId: string): string {
        return `${this.baseUrl}/${customerId}/vehicles`;
    }

    getByCustomer(customerId: string, activeOnly = false): Observable<CustomerVehicleResponse[]> {
        const params = new HttpParams().set('activeOnly', activeOnly);
        return this.http.get<CustomerVehicleResponse[]>(this.vehiclesUrl(customerId), { params });
    }

    getById(customerId: string, vehicleId: string): Observable<CustomerVehicleResponse> {
        return this.http.get<CustomerVehicleResponse>(`${this.vehiclesUrl(customerId)}/${vehicleId}`);
    }

    create(customerId: string, request: CreateCustomerVehicleRequest): Observable<CustomerVehicleResponse> {
        return this.http.post<CustomerVehicleResponse>(this.vehiclesUrl(customerId), request);
    }

    update(customerId: string, vehicleId: string, request: UpdateCustomerVehicleRequest): Observable<CustomerVehicleResponse> {
        return this.http.put<CustomerVehicleResponse>(`${this.vehiclesUrl(customerId)}/${vehicleId}`, request);
    }

    delete(customerId: string, vehicleId: string): Observable<void> {
        return this.http.delete<void>(`${this.vehiclesUrl(customerId)}/${vehicleId}`);
    }
}
