import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from 'src/environments/environment';

export interface AppSetting {
  key: string;
  value: string;
  dataType: string;
  category: string;
  description: string;
  isSystemSetting: boolean;
}

export interface UpdateSettingRequest {
  value: string;
  dataType?: string;
  category?: string;
  description?: string;
  isSystemSetting?: boolean;
}

export interface NotificationSettings {
  smsEnabled: boolean;
  whatsAppEnabled: boolean;
  signalRRoles: string[];
}

export interface ShopProfile {
  name: string;
  address: string;
  phone: string;
  email: string;
  taxNo: string;
  logoUrl: string;
  tagline: string;
  invoiceFooterText: string;
  challanFooterText: string;
}

@Injectable({ providedIn: 'root' })
export class AppSettingsService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/v1/applicationsettings`;
  private readonly notifUrl = `${environment.apiUrl}/v1/notifications`;

  getByCategory(category: string): Observable<AppSetting[]> {
    return this.http.get<AppSetting[]>(`${this.baseUrl}/category/${category}`);
  }

  getShopProfile(): Observable<ShopProfile> {
    return this.http.get<ShopProfile>(`${this.baseUrl}/public/shop`);
  }

  update(key: string, request: UpdateSettingRequest): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/${key}`, request);
  }

  getNotificationSettings(): Observable<NotificationSettings> {
    return this.http.get<NotificationSettings>(`${this.notifUrl}/settings`);
  }

  updateNotificationSettings(settings: NotificationSettings): Observable<NotificationSettings> {
    return this.http.put<NotificationSettings>(`${this.notifUrl}/settings`, settings);
  }
}
