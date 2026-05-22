import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { environment } from 'src/environments/environment';

export interface CashBookEntry {
  id: string;
  time: string;
  type: 'CUSTOMER_PAYMENT' | 'EXPENSE' | 'SUPPLIER_PAYMENT';
  description: string;
  reference: string;
  paymentMethod: string;
  amount: number;
  currency: string;
  status: string;
  notes?: string | null;
  category?: string | null;
  vendor?: string | null;
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
  cashIn: CashBookEntry[];
  cashOut: CashBookEntry[];
  ledger: LedgerRow[];
  totalCashIn: number;
  totalCashOut: number;
  netCash: number;
  closingBalance: number;
  entryCount: number;
  paymentMethodBreakdown: PaymentMethodBreakdown[];
}

function toLocalDateStr(d: Date): string {
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
}

@Injectable({ providedIn: 'root' })
export class CashBookService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/cash-book`;

  getDaily(date: Date): Observable<DailyCashBook> {
    const params = new HttpParams().set('date', toLocalDateStr(date));
    return this.http.get<{ data: DailyCashBook }>(`${this.apiUrl}/daily`, { params })
      .pipe(map(r => r.data));
  }

  getRange(from: Date, to: Date): Observable<DailyCashBook> {
    const params = new HttpParams()
      .set('from', toLocalDateStr(from))
      .set('to', toLocalDateStr(to));
    return this.http.get<{ data: DailyCashBook }>(`${this.apiUrl}/daily`, { params })
      .pipe(map(r => r.data));
  }
}
