import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, BehaviorSubject, tap, catchError, of, map } from 'rxjs';
import { environment } from 'src/environments/environment';

export interface Currency {
  id: string;
  code: string;
  name: string;
  symbol: string;
  decimalPlaces: number;
  isActive: boolean;
  isBaseCurrency: boolean;
  displayOrder: number;
  createdAt: Date;
  updatedAt: Date;
}

export interface ExchangeRate {
  id: string;
  fromCurrencyId: string;
  fromCurrencyCode: string;
  toCurrencyId: string;
  toCurrencyCode: string;
  rate: number;
  effectiveDate: Date;
  expiryDate?: Date;
  source: string;
  isActive: boolean;
  notes: string;
}

export interface ConversionRequest {
  amount: number;
  fromCurrency: string;
  toCurrency: string;
  effectiveDate?: Date;
}

export interface ConversionResponse {
  originalAmount: number;
  originalCurrency: string;
  convertedAmount: number;
  convertedCurrency: string;
  exchangeRate: number;
  effectiveDate: Date;
  conversionTimestamp: Date;
}

export interface CreateCurrencyRequest {
  code: string;
  name: string;
  symbol: string;
  decimalPlaces?: number;
  isActive?: boolean;
  displayOrder?: number;
}

export interface UpdateCurrencyRequest {
  name: string;
  symbol: string;
  decimalPlaces?: number;
  isActive?: boolean;
  displayOrder?: number;
}

export interface CreateExchangeRateRequest {
  fromCurrencyId: string;
  toCurrencyId: string;
  rate: number;
  effectiveDate: Date;
  expiryDate?: Date;
  notes?: string;
}

export interface UpdateExchangeRateRequest {
  rate: number;
  effectiveDate: Date;
  expiryDate?: Date;
  notes?: string;
  isActive?: boolean;
}

@Injectable({
  providedIn: 'root'
})
export class CurrencyService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl =`${environment.apiUrl}`; 

  // State management for currencies
  private currenciesSubject = new BehaviorSubject<Currency[]>([]);
  public currencies$ = this.currenciesSubject.asObservable();

  private baseCurrencySubject = new BehaviorSubject<Currency | null>(null);
  public baseCurrency$ = this.baseCurrencySubject.asObservable();

  private activeCurrenciesSubject = new BehaviorSubject<Currency[]>([]);
  public activeCurrencies$ = this.activeCurrenciesSubject.asObservable();

  private defaultCurrencySubject = new BehaviorSubject<Currency | null>(null);
  public defaultCurrency$ = this.defaultCurrencySubject.asObservable();

  // Signal for selected currency (used in forms) - will be updated when default currency loads
  public selectedCurrency = signal<string>('BDT');
  public defaultCurrencyId = signal<string | null>(null);

  // Track if default currency has been loaded from settings
  private defaultCurrencyLoaded = false;

  constructor() {
    // Load currencies on initialization
    this.loadActiveCurrencies();
    this.loadBaseCurrency();
    this.loadDefaultCurrency();

    // Sync selectedCurrency with base currency if default currency is not set
    this.baseCurrency$.subscribe(baseCurrency => {
      if (baseCurrency && baseCurrency.code && !this.defaultCurrencyLoaded) {
        this.selectedCurrency.set(baseCurrency.code);
      }
    });
  }

  // ============= Currency Management =============

  /**
   * Get all currencies
   */
  getAllCurrencies(): Observable<Currency[]> {
    return this.http.get<Currency[]>(`${this.apiUrl}/currencies`).pipe(
      tap(currencies => this.currenciesSubject.next(currencies)),
      catchError(error => {
        console.error('Error loading currencies:', error);
        return of([]);
      })
    );
  }

  /**
   * Get active currencies only
   */
  getActiveCurrencies(): Observable<Currency[]> {
    return this.http.get<Currency[]>(`${this.apiUrl}/currencies/active`).pipe(
      tap(currencies => this.activeCurrenciesSubject.next(currencies)),
      catchError(error => {
        console.error('Error loading active currencies:', error);
        return of([]);
      })
    );
  }

  /**
   * Load active currencies into state
   */
  loadActiveCurrencies(): void {
    this.getActiveCurrencies().subscribe();
  }

  /**
   * Get base currency
   */
  getBaseCurrency(): Observable<Currency> {
    return this.http.get<Currency>(`${this.apiUrl}/currencies/base`).pipe(
      tap(currency => this.baseCurrencySubject.next(currency)),
      catchError(error => {
        console.error('Error loading base currency:', error);
        return of({} as Currency);
      })
    );
  }

  /**
   * Load base currency into state
   */
  loadBaseCurrency(): void {
    this.getBaseCurrency().subscribe();
  }

  /**
   * Get currency by ID
   */
  getCurrencyById(id: string): Observable<Currency> {
    return this.http.get<Currency>(`${this.apiUrl}/currencies/${id}`);
  }

  /**
   * Get currency by code
   */
  getCurrencyByCode(code: string): Observable<Currency> {
    return this.http.get<Currency>(`${this.apiUrl}/currencies/code/${code}`);
  }

  /**
   * Create new currency (Admin only)
   */
  createCurrency(request: CreateCurrencyRequest): Observable<Currency> {
    return this.http.post<Currency>(`${this.apiUrl}/currencies`, request).pipe(
      tap(() => this.loadActiveCurrencies()) // Reload currencies after creation
    );
  }

  /**
   * Update currency (Admin only)
   */
  updateCurrency(id: string, request: UpdateCurrencyRequest): Observable<Currency> {
    return this.http.put<Currency>(`${this.apiUrl}/currencies/${id}`, request).pipe(
      tap(() => this.loadActiveCurrencies()) // Reload currencies after update
    );
  }

  /**
   * Set currency as base (Admin only)
   */
  setAsBaseCurrency(id: string): Observable<any> {
    return this.http.put(`${this.apiUrl}/currencies/${id}/set-base`, {}).pipe(
      tap(() => {
        this.loadBaseCurrency();
        this.loadActiveCurrencies();
      })
    );
  }

  /**
   * Get default currency ID from application settings
   */
  getDefaultCurrencyId(): Observable<string | null> {
    return this.http.get<{ defaultCurrencyId: string | null }>(`${this.apiUrl}/applicationsettings/default-currency`).pipe(
      map(response => response.defaultCurrencyId),
      tap(id => {
        this.defaultCurrencyId.set(id);
        if (id) {
          this.getCurrencyById(id).subscribe(currency => {
            this.defaultCurrencySubject.next(currency);
            this.selectedCurrency.set(currency.code);
            this.defaultCurrencyLoaded = true;
          });
        }
      }),
      catchError(error => {
        console.error('Error loading default currency ID:', error);
        return of(null);
      })
    );
  }

  /**
   * Load default currency into state
   */
  loadDefaultCurrency(): void {
    this.getDefaultCurrencyId().subscribe();
  }

  /**
   * Set default currency ID (Admin only)
   */
  setDefaultCurrency(currencyId: string): Observable<any> {
    return this.http.put(`${this.apiUrl}/applicationsettings/default-currency`, { currencyId }).pipe(
      tap(() => {
        this.loadDefaultCurrency();
      })
    );
  }

  /**
   * Delete currency (Admin only)
   */
  deleteCurrency(id: string): Observable<any> {
    return this.http.delete(`${this.apiUrl}/currencies/${id}`).pipe(
      tap(() => this.loadActiveCurrencies()) // Reload currencies after deletion
    );
  }

  // ============= Exchange Rate Management =============

  /**
   * Get all exchange rates
   */
  getAllExchangeRates(): Observable<ExchangeRate[]> {
    return this.http.get<ExchangeRate[]>(`${this.apiUrl}/exchange-rates`);
  }

  /**
   * Get current active exchange rates
   */
  getCurrentExchangeRates(): Observable<ExchangeRate[]> {
    return this.http.get<ExchangeRate[]>(`${this.apiUrl}/exchange-rates/current`);
  }

  /**
   * Get exchange rates for a specific date
   */
  getExchangeRatesForDate(date: Date): Observable<ExchangeRate[]> {
    const dateStr = date.toISOString().split('T')[0];
    return this.http.get<ExchangeRate[]>(`${this.apiUrl}/exchange-rates/date/${dateStr}`);
  }

  /**
   * Get exchange rate history for currency pair
   */
  getExchangeRateHistory(fromId: string, toId: string): Observable<ExchangeRate[]> {
    return this.http.get<ExchangeRate[]>(`${this.apiUrl}/exchange-rates/history/${fromId}/${toId}`);
  }

  /**
   * Convert amount between currencies
   */
  convertCurrency(request: ConversionRequest): Observable<ConversionResponse> {
    return this.http.post<ConversionResponse>(`${this.apiUrl}/exchange-rates/convert`, request);
  }

  /**
   * Create exchange rate (Admin only)
   */
  createExchangeRate(request: CreateExchangeRateRequest): Observable<ExchangeRate> {
    return this.http.post<ExchangeRate>(`${this.apiUrl}/exchange-rates`, request);
  }

  /**
   * Bulk create exchange rates (Admin only)
   */
  createExchangeRatesBulk(requests: CreateExchangeRateRequest[]): Observable<ExchangeRate[]> {
    return this.http.post<ExchangeRate[]>(`${this.apiUrl}/exchange-rates/bulk`, requests);
  }

  /**
   * Update exchange rate (Admin only)
   */
  updateExchangeRate(id: string, request: UpdateExchangeRateRequest): Observable<ExchangeRate> {
    return this.http.put<ExchangeRate>(`${this.apiUrl}/exchange-rates/${id}`, request);
  }

  /**
   * Delete exchange rate (Admin only)
   */
  deleteExchangeRate(id: string): Observable<any> {
    return this.http.delete(`${this.apiUrl}/exchange-rates/${id}`);
  }

  // ============= Utility Methods =============

  /**
   * Format amount with currency symbol
   */
  formatCurrency(amount: number, currencyCode: string): string {
    const currency = this.activeCurrenciesSubject.value.find(c => c.code === currencyCode);
    if (!currency) {
      return `${amount.toFixed(2)} ${currencyCode}`;
    }
    return `${currency.symbol} ${amount.toFixed(currency.decimalPlaces)}`;
  }

  /**
   * Get currency symbol by code
   */
  getCurrencySymbol(currencyCode: string): string {
    const currency = this.activeCurrenciesSubject.value.find(c => c.code === currencyCode);
    return currency?.symbol || currencyCode;
  }

  /**
   * Get currency decimal places by code
   */
  getCurrencyDecimalPlaces(currencyCode: string): number {
    const currency = this.activeCurrenciesSubject.value.find(c => c.code === currencyCode);
    return currency?.decimalPlaces || 2;
  }

  /**
   * Check if currency is base currency
   */
  isBaseCurrency(currencyCode: string): boolean {
    const baseCurrency = this.baseCurrencySubject.value;
    return baseCurrency?.code === currencyCode;
  }

  /**
   * Get current selected currency code
   */
  getSelectedCurrency(): string {
    return this.selectedCurrency();
  }

  /**
   * Set selected currency
   */
  setSelectedCurrency(currencyCode: string): void {
    this.selectedCurrency.set(currencyCode);
  }
}
