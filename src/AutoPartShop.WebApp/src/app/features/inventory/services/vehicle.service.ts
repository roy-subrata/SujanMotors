import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from 'src/environments/environment';

export interface VehicleResponse {
  id: string;
  make: string;
  model: string;
  trim?: string;
  year: number;
  engineType: string;
  description: string;
  isActive: boolean;
  createdBy: string;
  modifiedBy: string;
}

export interface CreateVehicleRequest {
  make: string;
  model: string;
  year: number;
  engineType: string;
  description?: string;
}

export interface UpdateVehicleRequest {
  make: string;
  model: string;
  year: number;
  engineType: string;
  description: string;
  isActive: boolean;
}

export interface PartCompatibilityResponse {
  id: string;
  partId: string;
  partName: string;
  partSKU: string;
  vehicleId: string;
  vehicleInfo: string;
  isCompatible: boolean;
  notes: string;
  createdBy: string;
}

export interface CreatePartCompatibilityRequest {
  isCompatible: boolean;
  notes?: string;
}

@Injectable({
  providedIn: 'root'
})
export class VehicleService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/vehicles`;

  /**
   * Get all vehicles
   */
  getAllVehicles(): Observable<VehicleResponse[]> {
    return this.http.get<VehicleResponse[]>(this.apiUrl);
  }

  /**
   * Get active vehicles
   */
  getActiveVehicles(): Observable<VehicleResponse[]> {
    return this.http.get<VehicleResponse[]>(`${this.apiUrl}/active`);
  }

  /**
   * Get vehicle by ID
   */
  getVehicleById(id: string): Observable<VehicleResponse> {
    return this.http.get<VehicleResponse>(`${this.apiUrl}/${id}`);
  }

  /**
   * Create vehicle
   */
  createVehicle(vehicle: CreateVehicleRequest): Observable<VehicleResponse> {
    return this.http.post<VehicleResponse>(this.apiUrl, vehicle);
  }

  /**
   * Update vehicle
   */
  updateVehicle(id: string, vehicle: UpdateVehicleRequest): Observable<VehicleResponse> {
    return this.http.put<VehicleResponse>(`${this.apiUrl}/${id}`, vehicle);
  }

  /**
   * Activate vehicle
   */
  activateVehicle(id: string): Observable<VehicleResponse> {
    return this.http.patch<VehicleResponse>(`${this.apiUrl}/${id}/activate`, {});
  }

  /**
   * Deactivate vehicle
   */
  deactivateVehicle(id: string): Observable<VehicleResponse> {
    return this.http.patch<VehicleResponse>(`${this.apiUrl}/${id}/deactivate`, {});
  }

  /**
   * Delete vehicle
   */
  deleteVehicle(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }

  /**
   * Add part compatibility
   */
  addPartCompatibility(vehicleId: string, partId: string, request: CreatePartCompatibilityRequest): Observable<PartCompatibilityResponse> {
    return this.http.post<PartCompatibilityResponse>(`${this.apiUrl}/${vehicleId}/parts/${partId}/compatibility`, request);
  }

  /**
   * Get vehicle compatibilities
   */
  getVehicleCompatibilities(vehicleId: string): Observable<PartCompatibilityResponse[]> {
    return this.http.get<PartCompatibilityResponse[]>(`${this.apiUrl}/${vehicleId}/compatibilities`);
  }

  /**
   * Remove compatibility
   */
  removeCompatibility(compatibilityId: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/compatibilities/${compatibilityId}`);
  }
}
