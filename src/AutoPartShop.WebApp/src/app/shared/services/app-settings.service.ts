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

@Injectable({ providedIn: 'root' })
export class AppSettingsService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/applicationsettings`;
  private readonly notifUrl = `${environment.apiUrl}/notifications`;

  getByCategory(category: string): Observable<AppSetting[]> {
    return this.http.get<AppSetting[]>(`${this.baseUrl}/category/${category}`);
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
