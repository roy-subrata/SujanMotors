import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, BehaviorSubject } from 'rxjs';
import { tap } from 'rxjs/operators';
import { environment } from 'src/environments/environment';

// Payment method types
export type PaymentMethod = 'CASH' | 'MOBILE_BANKING' | 'CARD' | 'DUE' | 'PART_PAY';
export type PaymentResponsibility = 'CUSTOMER' | 'TECHNICIAN_TEMPORARY';

export interface QuickSaleLineItem {
  partId: string;
  productVariantId?: string;
  partName?: string;
  partNumber?: string;
  sku?: string;
  unitId?: string;
  quantity: number;
  unitPrice: number;
  discount: number;
  stockAvailable?: number;
  warehouseLocation?: string;
  supplierName?: string;
}

export interface PaymentDetail {
  method: PaymentMethod;
  amount: number;
  reference?: string;
  notes?: string;
}

export interface QuickSaleRequest {
  customerId?: string;
  customerName: string;
  customerPhone: string;
  customerEmail?: string;
  technicianId?: string;
  technicianName?: string;
  technicianNotes?: string;
  paymentResponsibility: PaymentResponsibility;
  purchaseOrderId?: string;
  autoCreatePO: boolean;
  items: QuickSaleLineItem[];
  payments: PaymentDetail[];
  subtotal: number;
  discountAmount: number;
  discountType?: string;    // 'NONE' | 'PERCENTAGE' | 'FIXED'
  discountReason?: string;  // required for audit trail when discount > 0
  vatAmount: number;
  vatPercentage: number;
  grandTotal: number;
  paidAmount: number;
  dueAmount: number;
  notes?: string;
  // Advance Payment Support
  useAdvanceBalance?: boolean;
  advanceAmountToApply?: number;
  // Quotation Support
  saveAsQuotation?: boolean;
}

export interface QuickSaleResponse {
  id: string;
  invoiceNumber: string;
  salesOrderId: string;
  salesOrderNumber: string;
  customerId: string;
  customerName: string;
  technicianId?: string;
  technicianName?: string;
  paymentResponsibility: PaymentResponsibility;
  subtotal: number;
  discountAmount: number;
  vatAmount: number;
  grandTotal: number;
  paidAmount: number;
  dueAmount: number;
  status: string;
  isQuotation?: boolean;
  createdAt: string;
}

export interface StockCheckRequest {
  partId: string;
  quantity: number;
}

export interface StockCheckResponse {
  partId: string;
  available: boolean;
  stockAvailable: number;
  warehouseLocation?: string;
  supplierName?: string;
  message?: string;
}

export interface QuickSaleDraft {
  draftId: string;
  customerId?: string;
  customerName?: string;
  customerPhone?: string;
  items: QuickSaleLineItem[];
  payments: PaymentDetail[];
  technicianId?: string;
  notes?: string;
  timestamp: Date;
}

@Injectable({ providedIn: 'root' })
export class QuickSaleService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}`;

  // Draft management
  private readonly DRAFT_STORAGE_KEY = 'quickSaleDraft';
  private draftSubject = new BehaviorSubject<QuickSaleDraft | null>(this.loadDraftFromStorage());

  draft$ = this.draftSubject.asObservable();

  /**
   * Create a quick sale (sales order + invoice + payments in one transaction)
   */
  createQuickSale(request: QuickSaleRequest): Observable<QuickSaleResponse> {
    return this.http.post<QuickSaleResponse>(`${this.apiUrl}/salesorder/quick-sale`, request).pipe(
      tap(() => this.clearDraft()) // Clear draft on successful sale
    );
  }

  /**
   * Check stock availability for a part
   */
  checkStock(partId: string, quantity: number): Observable<StockCheckResponse> {
    return this.http.post<StockCheckResponse>(`${this.apiUrl}/stock/check`, { partId, quantity });
  }

  /**
   * Check multiple items stock availability
   */
  checkMultipleStock(items: StockCheckRequest[]): Observable<StockCheckResponse[]> {
    return this.http.post<StockCheckResponse[]>(`${this.apiUrl}/stock/check-multiple`, items);
  }

  /**
   * Generate next invoice number
   */
  generateInvoiceNumber(): Observable<{ invoiceNumber: string }> {
    return this.http.get<{ invoiceNumber: string }>(`${this.apiUrl}/code-generate/invoice`);
  }

  /**
   * Get recent customers (last 50)
   */
  getRecentCustomers(): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/customer/recent?limit=50`);
  }

  /**
   * Search customer by phone number
   */
  searchCustomerByPhone(phone: string): Observable<any | null> {
    return this.http.get<any>(`${this.apiUrl}/customer/search-by-phone?phone=${phone}`);
  }

  /**
   * Get VAT configuration
   */
  getVATConfig(): Observable<{ enabled: boolean; percentage: number }> {
    // For now, return default VAT config
    // In production, this would come from settings API
    return new Observable(observer => {
      observer.next({ enabled: false, percentage: 0 });
      observer.complete();
    });
  }

  // Draft Management Methods

  /**
   * Save draft to local storage
   */
  saveDraft(draft: Partial<QuickSaleDraft>): void {
    const fullDraft: QuickSaleDraft = {
      draftId: draft.draftId || this.generateDraftId(),
      customerId: draft.customerId,
      customerName: draft.customerName,
      customerPhone: draft.customerPhone,
      items: draft.items || [],
      payments: draft.payments || [],
      technicianId: draft.technicianId,
      notes: draft.notes,
      timestamp: new Date()
    };

    localStorage.setItem(this.DRAFT_STORAGE_KEY, JSON.stringify(fullDraft));
    this.draftSubject.next(fullDraft);
  }

  /**
   * Load draft from local storage
   */
  loadDraft(): QuickSaleDraft | null {
    return this.loadDraftFromStorage();
  }

  /**
   * Clear draft
   */
  clearDraft(): void {
    localStorage.removeItem(this.DRAFT_STORAGE_KEY);
    this.draftSubject.next(null);
  }

  /**
   * Check if draft exists
   */
  hasDraft(): boolean {
    return !!this.loadDraftFromStorage();
  }

  private loadDraftFromStorage(): QuickSaleDraft | null {
    const draftJson = localStorage.getItem(this.DRAFT_STORAGE_KEY);
    if (!draftJson) return null;

    try {
      const draft = JSON.parse(draftJson);
      // Check if draft is not too old (older than 24 hours)
      const draftTime = new Date(draft.timestamp);
      const now = new Date();
      const hoursDiff = (now.getTime() - draftTime.getTime()) / (1000 * 60 * 60);

      if (hoursDiff > 24) {
        this.clearDraft();
        return null;
      }

      return draft;
    } catch {
      return null;
    }
  }

  private generateDraftId(): string {
    return `DRAFT-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`;
  }

  // ===== HELD SALES MANAGEMENT =====
  private readonly HELD_SALES_KEY = 'quickSaleHeldSales';

  /**
   * Hold current sale for later
   */
  holdSale(sale: Partial<QuickSaleDraft>): string {
    const heldSales = this.getHeldSales();
    const holdId = `HOLD-${Date.now()}-${Math.random().toString(36).substr(2, 6)}`;
    
    const heldSale: QuickSaleDraft = {
      draftId: holdId,
      customerId: sale.customerId,
      customerName: sale.customerName,
      customerPhone: sale.customerPhone,
      items: sale.items || [],
      payments: sale.payments || [],
      technicianId: sale.technicianId,
      notes: sale.notes,
      timestamp: new Date()
    };
    
    heldSales.push(heldSale);
    localStorage.setItem(this.HELD_SALES_KEY, JSON.stringify(heldSales));
    return holdId;
  }

  /**
   * Get all held sales
   */
  getHeldSales(): QuickSaleDraft[] {
    const heldJson = localStorage.getItem(this.HELD_SALES_KEY);
    if (!heldJson) return [];
    try {
      return JSON.parse(heldJson);
    } catch {
      return [];
    }
  }

  /**
   * Recall a specific held sale
   */
  recallHeldSale(holdId: string): QuickSaleDraft | null {
    const heldSales = this.getHeldSales();
    const sale = heldSales.find(s => s.draftId === holdId);
    if (sale) {
      // Remove from held sales
      const updated = heldSales.filter(s => s.draftId !== holdId);
      localStorage.setItem(this.HELD_SALES_KEY, JSON.stringify(updated));
    }
    return sale || null;
  }

  /**
   * Remove a held sale
   */
  removeHeldSale(holdId: string): void {
    const heldSales = this.getHeldSales();
    const updated = heldSales.filter(s => s.draftId !== holdId);
    localStorage.setItem(this.HELD_SALES_KEY, JSON.stringify(updated));
  }

  /**
   * Clear all held sales
   */
  clearAllHeldSales(): void {
    localStorage.removeItem(this.HELD_SALES_KEY);
  }

  // ===== LAST SALE TRACKING =====
  private readonly LAST_SALE_KEY = 'quickSaleLastSale';

  /**
   * Save last completed sale info
   */
  saveLastSale(sale: QuickSaleResponse): void {
    localStorage.setItem(this.LAST_SALE_KEY, JSON.stringify({
      ...sale,
      savedAt: new Date().toISOString()
    }));
  }

  /**
   * Get last completed sale
   */
  getLastSale(): (QuickSaleResponse & { savedAt: string }) | null {
    const lastJson = localStorage.getItem(this.LAST_SALE_KEY);
    if (!lastJson) return null;
    try {
      return JSON.parse(lastJson);
    } catch {
      return null;
    }
  }

  // ===== PRICE CHECK =====
  /**
   * Search part by SKU, barcode, or part number. Returns current FIFO lot selling price
   * (falls back to Part.SellingPrice when no lot price is set).
   */
  getPriceByCode(code: string): Observable<{ partId: string; name: string; partNumber: string; sku: string; sellingPrice: number; fallbackSellingPrice: number; hasLotPrice: boolean; stockLevel: number; unitId: string | null } | null> {
    return this.http.get<any>(`${this.apiUrl}/parts/search-by-code?code=${encodeURIComponent(code)}`);
  }

  // ===== RETURNS =====
  /**
   * Process a return
   */
  processReturn(request: { originalInvoiceNumber: string; items: { partId: string; quantity: number; reason: string }[] }): Observable<any> {
    return this.http.post(`${this.apiUrl}/salesorder/return`, request);
  }

  /**
   * Lookup invoice for returns
   */
  lookupInvoice(invoiceNumber: string): Observable<QuickSaleResponse | null> {
    return this.http.get<QuickSaleResponse>(`${this.apiUrl}/salesorder/by-invoice/${encodeURIComponent(invoiceNumber)}`);
  }

  // ===== CUSTOMER CREDIT =====
  /**
   * Get customer credit info
   */
  getCustomerCredit(customerId: string): Observable<{ creditLimit: number; usedCredit: number; availableCredit: number; dueBalance: number }> {
    return this.http.get<any>(`${this.apiUrl}/customer/${customerId}/credit`);
  }

  /**
   * Get customer purchase history
   */
  getCustomerHistory(customerId: string, limit: number = 10): Observable<QuickSaleResponse[]> {
    return this.http.get<QuickSaleResponse[]>(`${this.apiUrl}/salesorder/customer/${customerId}`);
  }

  // ===== QUOTE GENERATION =====
  /**
   * Generate a quote
   */
  generateQuote(request: Partial<QuickSaleRequest>): Observable<{ quoteId: string; quoteNumber: string }> {
    return this.http.post<{ quoteId: string; quoteNumber: string }>(`${this.apiUrl}/quotes`, request);
  }
}
