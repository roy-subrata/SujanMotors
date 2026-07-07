import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from 'src/environments/environment';

export interface ShiftResponse {
    id: string;
    name: string;
    startTime: string;  // "HH:mm:ss"
    endTime: string;
    graceMinutes: number;
    notes: string;
}

export interface SaveShiftRequest {
    name: string;
    startTime: string;
    endTime: string;
    graceMinutes: number;
    notes: string;
}

@Injectable({ providedIn: 'root' })
export class ShiftService {
    private readonly http = inject(HttpClient);
    private readonly apiUrl = `${environment.apiUrl}/v1/shifts`;

    getShifts(): Observable<ShiftResponse[]> {
        return this.http.get<ShiftResponse[]>(this.apiUrl);
    }

    createShift(request: SaveShiftRequest): Observable<ShiftResponse> {
        return this.http.post<ShiftResponse>(this.apiUrl, request);
    }

    updateShift(id: string, request: SaveShiftRequest): Observable<ShiftResponse> {
        return this.http.put<ShiftResponse>(`${this.apiUrl}/${id}`, request);
    }

    deleteShift(id: string): Observable<void> {
        return this.http.delete<void>(`${this.apiUrl}/${id}`);
    }
}
