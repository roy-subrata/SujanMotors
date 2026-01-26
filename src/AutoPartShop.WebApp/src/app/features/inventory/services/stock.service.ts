import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface StockLevelResponse {
  id: string;
  partId: string;
  warehouseId: string;
  quantity: number;
  reservedQuantity: number;
  availableQuantity: number;
  reorderLevel: number;
  reorderQuantity: number;
  needsReorder: boolean;
  createdAt: string;
}

export interface CreateStockLevelRequest {
  partId: string;
  warehouseId: string;
  reorderLevel: number;
  reorderQuantity: number;
}

export interface UpdateStockLevelRequest {
  reorderLevel: number;
  reorderQuantity: number;
}

export interface StockMovementResponse {
  id: string;
  partId: string;
  partName: string;
  partCode: string;
  warehouseId: string;
  warehouseName: string;
  warehouseCode: string;
  type: string;
  quantity: number;
  reason: string;
  reference: string;
  status: string;
  notes: string;
  approvedBy: string;
  approvedAt: string | null;
  createdAt: string;
}

export interface CreateStockMovementRequest {
  partId: string;
  type: string;
  quantity: number;
  reference: string;
}

export interface StockTransferRequest {
  partId: string;
  fromWarehouseId: string;
  toWarehouseId: string;
  quantity: number;
  reference: string;
  notes: string;
}

export interface StockTransferResponse {
  id: string;
  partId: string;
  partName: string;
  partCode: string;
  fromWarehouseId: string;
  fromWarehouseName: string;
  fromWarehouseCode: string;
  toWarehouseId: string;
  toWarehouseName: string;
  toWarehouseCode: string;
  quantity: number;
  reference: string;
  notes: string;
  status: string;
  createdBy: string;
  createdAt: string;
}

export interface StockAdjustmentRequest {
  partId: string;
  warehouseId: string;
  quantity: number;
  reason: string;
  reference: string;
  notes: string;
}

export interface StockAdjustmentResponse {
  id: string;
  partId: string;
  partName: string;
  partCode: string;
  warehouseId: string;
  warehouseName: string;
  warehouseCode: string;
  quantity: number;
  previousQuantity: number;
  newQuantity: number;
  reason: string;
  reference: string;
  notes: string;
  createdBy: string;
  createdAt: string;
}

@Injectable({
  providedIn: 'root'
})
export class StockService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = 'http://localhost:5292/api/stock';

  /**
   * Get all stock levels
   */
  getAllStockLevels(): Observable<StockLevelResponse[]> {
    return this.http.get<StockLevelResponse[]>(`${this.apiUrl}/levels`);
  }

  /**
   * Get stock level by ID
   */
  getStockLevelById(id: string): Observable<StockLevelResponse> {
    return this.http.get<StockLevelResponse>(`${this.apiUrl}/levels/${id}`);
  }

  /**
   * Get stock levels by part
   */
  getStockLevelsByPart(partId: string): Observable<StockLevelResponse[]> {
    return this.http.get<StockLevelResponse[]>(`${this.apiUrl}/levels/part/${partId}`);
  }

  /**
   * Get stock levels by warehouse
   */
  getStockLevelsByWarehouse(warehouseId: string): Observable<StockLevelResponse[]> {
    return this.http.get<StockLevelResponse[]>(`${this.apiUrl}/levels/warehouse/${warehouseId}`);
  }

  /**
   * Get low stock items
   */
  getLowStock(): Observable<StockLevelResponse[]> {
    return this.http.get<StockLevelResponse[]>(`${this.apiUrl}/levels/low-stock`);
  }

  /**
   * Get stock level by part and warehouse
   */
  getStockLevelByPartAndWarehouse(partId: string, warehouseId: string): Observable<StockLevelResponse> {
    return this.http.get<StockLevelResponse>(`${this.apiUrl}/levels/part/${partId}/warehouse/${warehouseId}`);
  }

  /**
   * Create stock level
   */
  createStockLevel(request: CreateStockLevelRequest): Observable<StockLevelResponse> {
    return this.http.post<StockLevelResponse>(`${this.apiUrl}/levels`, request);
  }

  /**
   * Update stock level
   */
  updateStockLevel(id: string, request: UpdateStockLevelRequest): Observable<StockLevelResponse> {
    return this.http.put<StockLevelResponse>(`${this.apiUrl}/levels/${id}`, request);
  }

  /**
   * Get all stock movements
   */
  getAllMovements(): Observable<StockMovementResponse[]> {
    return this.http.get<StockMovementResponse[]>(`${this.apiUrl}/movements`);
  }

  /**
   * Get movement by ID
   */
  getMovementById(id: string): Observable<StockMovementResponse> {
    return this.http.get<StockMovementResponse>(`${this.apiUrl}/movements/${id}`);
  }

  /**
   * Create stock movement
   */
  createMovement(request: CreateStockMovementRequest): Observable<StockMovementResponse> {
    return this.http.post<StockMovementResponse>(`${this.apiUrl}/movements`, request);
  }

  /**
   * Transfer stock between warehouses
   */
  transferStock(request: StockTransferRequest): Observable<StockTransferResponse> {
    return this.http.post<StockTransferResponse>(`${this.apiUrl}/transfer`, request);
  }

  /**
   * Adjust stock quantity
   */
  adjustStock(request: StockAdjustmentRequest): Observable<StockAdjustmentResponse> {
    return this.http.post<StockAdjustmentResponse>(`${this.apiUrl}/adjust`, request);
  }
}
