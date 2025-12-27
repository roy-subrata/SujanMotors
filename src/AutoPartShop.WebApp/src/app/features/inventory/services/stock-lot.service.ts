import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface StockLotResponse {
  id: string;
  lotNumber: string;
  partId: string;
  partName: string;
  partSKU: string;
  warehouseId: string;
  warehouseName: string;
  supplierId: string;
  supplierName: string;
  quantityReceived: number;
  quantityAvailable: number;
  costPrice: number;
  currency: string;
  totalCost: number;
  availableCost: number;
  receivingDate: string;
  expiryDate: string | null;
  isExpired: boolean;
  manufacturerLotNumber: string;
  notes: string;
  isActive: boolean;
  createdAt: string;
}

export interface StockLotHistoryItem {
  lotId: string;
  lotNumber: string;
  supplierId: string;
  supplierName: string;
  quantityReceived: number;
  quantityAvailable: number;
  costPrice: number;
  receivingDate: string;
  expiryDate: string | null;
  isExpired: boolean;
}

export interface StockLotPriceHistoryResponse {
  partId: string;
  partName: string;
  partSKU: string;
  lots: StockLotHistoryItem[];
  minPrice: number;
  maxPrice: number;
  averagePrice: number;
  latestPrice: number;
}

@Injectable({
  providedIn: 'root'
})
export class StockLotService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = 'http://localhost:5292/api/stocklot';

  /**
   * Get all lots for a specific part
   */
  getByPart(partId: string): Observable<StockLotResponse[]> {
    return this.http.get<StockLotResponse[]>(`${this.apiUrl}/part/${partId}`);
  }

  /**
   * Get price history for a part (all lots sorted by date)
   */
  getPriceHistory(partId: string): Observable<StockLotPriceHistoryResponse> {
    return this.http.get<StockLotPriceHistoryResponse>(`${this.apiUrl}/price-history/${partId}`);
  }

  /**
   * Get all lots for a part in a specific warehouse
   */
  getByPartAndWarehouse(partId: string, warehouseId: string): Observable<StockLotResponse[]> {
    return this.http.get<StockLotResponse[]>(`${this.apiUrl}/warehouse/${partId}/${warehouseId}`);
  }

  /**
   * Get available lots (quantity > 0) for a part and warehouse
   */
  getAvailableLots(partId: string, warehouseId: string): Observable<StockLotResponse[]> {
    return this.http.get<StockLotResponse[]>(`${this.apiUrl}/available/${partId}/${warehouseId}`);
  }

  /**
   * Get all expired lots
   */
  getExpiredLots(): Observable<StockLotResponse[]> {
    return this.http.get<StockLotResponse[]>(`${this.apiUrl}/expired`);
  }

  /**
   * Get low stock lots
   */
  getLowStockLots(): Observable<StockLotResponse[]> {
    return this.http.get<StockLotResponse[]>(`${this.apiUrl}/low-stock`);
  }

  /**
   * Get a specific stock lot by ID
   */
  getById(id: string): Observable<StockLotResponse> {
    return this.http.get<StockLotResponse>(`${this.apiUrl}/${id}`);
  }

  /**
   * Get a specific stock lot by lot number
   */
  getByLotNumber(lotNumber: string): Observable<StockLotResponse> {
    return this.http.get<StockLotResponse>(`${this.apiUrl}/by-lot/${lotNumber}`);
  }
}
