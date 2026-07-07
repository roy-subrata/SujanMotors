import { Injectable, inject, OnDestroy } from '@angular/core';
import { Subject, Subscription } from 'rxjs';
import { distinctUntilChanged } from 'rxjs/operators';
import * as signalR from '@microsoft/signalr';
import { environment } from 'src/environments/environment';
import { AuthService } from './auth.service';

export interface SaleNotificationEvent {
  salesOrderId: string;
  soNumber: string;
  customerName: string;
  grandTotal: number;
  currency: string;
  saleChannel: string;
  saleType: string;
  occurredAt: string;
  createdBy: string;
}

export interface ReorderAlertItem {
  stockLevelId: string;
  partId: string;
  variantId: string | null;
  partName: string;
  sku: string | null;
  warehouseName: string;
  quantityAvailable: number;
  reorderLevel: number;
  reorderQuantity: number;
}

export interface ReorderAlertEvent {
  /** Total items needing reorder; items[] is capped server-side. */
  itemCount: number;
  items: ReorderAlertItem[];
  occurredAt: string;
}

@Injectable({ providedIn: 'root' })
export class NotificationHubService implements OnDestroy {
  private readonly auth = inject(AuthService);

  private connection: signalR.HubConnection | null = null;
  private readonly _saleNotification$ = new Subject<SaleNotificationEvent>();
  private readonly _reorderAlert$ = new Subject<ReorderAlertEvent>();
  private authSub?: Subscription;

  /** Emits every time a sale notification arrives from the hub. */
  readonly saleNotification$ = this._saleNotification$.asObservable();

  /** Emits when the daily (or manually triggered) low-stock reorder alert arrives. */
  readonly reorderAlert$ = this._reorderAlert$.asObservable();

  constructor() {
    this.authSub = this.auth.isAuthenticated$
      .pipe(distinctUntilChanged())
      .subscribe(authenticated => {
        if (authenticated) {
          this.connect();
        } else {
          this.disconnect();
        }
      });
  }

  private connect(): void {
    if (this.connection) return;

    const hubUrl = `${environment.apiUrl.replace('/api', '')}/hubs/sale-notifications`;

    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(hubUrl, { accessTokenFactory: () => localStorage.getItem('auth_token') ?? '' })
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.Warning)
      .build();

    this.connection.on('ReceiveSaleNotification', (evt: SaleNotificationEvent) => {
      this._saleNotification$.next(evt);
    });

    this.connection.on('ReceiveReorderAlert', (evt: ReorderAlertEvent) => {
      this._reorderAlert$.next(evt);
    });

    this.connection
      .start()
      .catch(err => console.warn('[NotificationHub] Connection failed:', err));
  }

  private disconnect(): void {
    this.connection?.stop();
    this.connection = null;
  }

  /** @deprecated Use auth-reactive lifecycle instead. Kept for backwards compatibility. */
  start(): void { this.connect(); }
  stop(): void { this.disconnect(); }

  ngOnDestroy(): void {
    this.authSub?.unsubscribe();
    this.disconnect();
  }
}
