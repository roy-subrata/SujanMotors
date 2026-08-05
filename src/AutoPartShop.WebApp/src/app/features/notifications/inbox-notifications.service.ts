import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { BehaviorSubject, Observable } from 'rxjs';
import { environment } from 'src/environments/environment';

export interface InboxNotification {
  id: string;
  type: string;
  title: string;
  message: string;
  isRead: boolean;
  readAt: string | null;
  routerLink: string | null;
  queryParamsJson: string | null;
  payloadJson: string | null;
  createdDate: string;
}

export interface InboxNotificationPayload {
  itemCount: number;
  occurredAt: string;
  items: Array<{
    stockLevelId: string;
    partId: string;
    variantId: string | null;
    partName: string;
    sku: string | null;
    warehouseName: string;
    quantityAvailable: number;
    reorderLevel: number;
    reorderQuantity: number;
  }>;
}

export interface PaginatedInboxNotifications {
  data: InboxNotification[];
  pagination: {
    page: number;
    pageSize: number;
    totalCount: number;
    totalPages: number;
  };
}

export interface InboxNotificationQuery {
  type?: string;
  unreadOnly?: boolean;
  page?: number;
  pageSize?: number;
}

@Injectable({ providedIn: 'root' })
export class InboxNotificationService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/v1/inboxnotifications`;

  private readonly _unreadCount = new BehaviorSubject<number>(0);
  /** Emits the persisted unread inbox count — topbar badge subscribes to this. */
  readonly unreadCount$ = this._unreadCount.asObservable();

  getNotifications(query: InboxNotificationQuery = {}): Observable<PaginatedInboxNotifications> {
    let params = new HttpParams();
    if (query.type) params = params.set('type', query.type);
    if (query.unreadOnly !== undefined) params = params.set('unreadOnly', query.unreadOnly.toString());
    params = params.set('page', (query.page ?? 1).toString());
    params = params.set('pageSize', (query.pageSize ?? 20).toString());
    return this.http.get<PaginatedInboxNotifications>(this.apiUrl, { params });
  }

  getUnreadCount(type?: string): Observable<{ count: number }> {
    let params = new HttpParams();
    if (type) params = params.set('type', type);
    return this.http.get<{ count: number }>(`${this.apiUrl}/unread-count`, { params });
  }

  /** Re-fetch the unread count into the shared BehaviorSubject (badge source of truth). */
  refreshUnreadCount(type?: string): void {
    this.getUnreadCount(type).subscribe({
      next: res => this._unreadCount.next(res.count),
      error: () => { /* keep last known value */ }
    });
  }

  markRead(id: string, isRead = true): Observable<{ id: string; isRead: boolean }> {
    return this.http.patch<{ id: string; isRead: boolean }>(`${this.apiUrl}/${id}/read`, { isRead });
  }

  markAllRead(type?: string): Observable<{ updated: number }> {
    let params = new HttpParams();
    if (type) params = params.set('type', type);
    return this.http.post<{ updated: number }>(`${this.apiUrl}/mark-all-read`, null, { params });
  }
}
