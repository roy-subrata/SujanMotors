import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from 'src/environments/environment';

export interface HolidayResponse {
    id: string;
    date: string;
    name: string;
}

export interface SaveHolidayRequest {
    date: string;  // yyyy-MM-dd
    name: string;
}

@Injectable({ providedIn: 'root' })
export class HolidayService {
    private readonly http = inject(HttpClient);
    private readonly apiUrl = `${environment.apiUrl}/v1/holidays`;

    getHolidays(year: number): Observable<HolidayResponse[]> {
        const params = new HttpParams().set('year', year);
        return this.http.get<HolidayResponse[]>(this.apiUrl, { params });
    }

    createHoliday(request: SaveHolidayRequest): Observable<HolidayResponse> {
        return this.http.post<HolidayResponse>(this.apiUrl, request);
    }

    updateHoliday(id: string, request: SaveHolidayRequest): Observable<HolidayResponse> {
        return this.http.put<HolidayResponse>(`${this.apiUrl}/${id}`, request);
    }

    deleteHoliday(id: string): Observable<void> {
        return this.http.delete<void>(`${this.apiUrl}/${id}`);
    }
}
