import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { environment } from 'src/environments/environment';

export interface CashBookEntry {
  id: string;
  time: string;
  type: 'CUSTOMER_PAYMENT' | 'EXPENSE' | 'SUPPLIER_PAYMENT' | 'REFUND';
  description: string;
  reference: string;
  paymentMethod: string;
  amount: number;
  currency: string;
  status: string;
  notes?: string | null;
  category?: string | null;
  vendor?: string | null;
  isCreditSale?: boolean;
}

export interface LedgerRow {
  id: string;
  time: string;
  flow: 'IN' | 'OUT';
  type: string;
  description: string;
  reference: string;
  paymentMethod: string;
  cashIn?: number | null;
  cashOut?: number | null;
  balance: number;
  currency: string;
  status: string;
  notes?: string | null;
  category?: string | null;
  vendor?: string | null;
  isCreditSale?: boolean;
}

export interface PaymentMethodBreakdown {
  method: string;
  in: number;
  out: number;
  net: number;
}

export interface DailyCashBook {
  from: string;
  to: string;
  isSingleDay: boolean;
  openingBalance: number;
  cashIn: CashBookEntry[];
  cashOut: CashBookEntry[];
  ledger: LedgerRow[];
  totalCashIn: number;
  totalActualCashIn: number;
  totalCreditIn: number;
  totalCashOut: number;
  netCash: number;
  netActualCash: number;
  closingBalance: number;
  entryCount: number;
  paymentMethodBreakdown: PaymentMethodBreakdown[];
}

/** Returns the browser's local date as YYYY-MM-DD (no timezone conversion). */
function toLocalDateStr(d: Date): string {
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
}

/**
 * Returns the browser's UTC offset in minutes (positive = ahead of UTC).
 * E.g., UTC+6 → 360, UTC-5 → -300.
 * Note: Date.getTimezoneOffset() returns the NEGATED offset, so we negate it back.
 */
function tzOffsetMinutes(): number {
  return -new Date().getTimezoneOffset();
}

@Injectable({ providedIn: 'root' })
export class CashBookService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/cash-book`;

  getDaily(date: Date): Observable<DailyCashBook> {
    const params = new HttpParams()
      .set('date', toLocalDateStr(date))
      .set('tzOffsetMinutes', String(tzOffsetMinutes()));
    return this.http.get<{ data: DailyCashBook }>(`${this.apiUrl}/daily`, { params })
      .pipe(map(r => r.data));
  }

  getRange(from: Date, to: Date): Observable<DailyCashBook> {
    const params = new HttpParams()
      .set('from', toLocalDateStr(from))
      .set('to', toLocalDateStr(to))
      .set('tzOffsetMinutes', String(tzOffsetMinutes()));
    return this.http.get<{ data: DailyCashBook }>(`${this.apiUrl}/daily`, { params })
      .pipe(map(r => r.data));
  }
}
