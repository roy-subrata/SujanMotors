import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { environment } from 'src/environments/environment';

export interface StockLevelResponse {
  id: string;
  partId: string;
  partName?: string | null;
  partSku?: string | null;
  variantId?: string | null;
  variantName?: string | null;
  variantSku?: string | null;
  warehouseId: string;
  warehouseName?: string | null;
  unitId: string | null;
  unitName: string | null;
  unitSymbol: string | null;
  baseUnitName: string | null;
  baseUnitSymbol: string | null;
  quantity: number;
  quantityInBaseUnit: number;
  reservedQuantity: number;
  reservedQuantityInBaseUnit: number;
  availableQuantity: number;
  availableQuantityInBaseUnit: number;
  reorderLevel: number;
  reorderQuantity: number;
  needsReorder: boolean;
  createdAt: string;
}

export interface StockLevelQuery {
  search?: string;
  pageSize: number;
  pageNumber: number;
  partId?: string;
  variantId?: string;
  warehouseId?: string;
  status?: string;
  lowStockOnly?: boolean;
}

export interface CreateStockLevelRequest {
  partId: string;
  variantId?: string | null;
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
  quantityInBaseUnit: number;
  unitId: string | null;
  unitName: string | null;
  unitSymbol: string | null;
  baseUnitSymbol: string | null;
  reason: string;
  reference: string;
  status: string;
  notes: string;
  approvedBy: string;
  approvedAt: string | null;
  createdAt: string;
}

export interface StockMovementQuery {
  search?: string;
  pageSize: number;
  pageNumber: number;
  partId?: string;
  warehouseId?: string;
  type?: string;
  status?: string;
  fromDate?: string;
  toDate?: string;
}

export interface CreateStockMovementRequest {
  partId: string;
  type: string;
  quantity: number;
  quantityInBaseUnit?: number;
  unitId?: string;
  reference: string;
}

export interface StockTransferRequest {
  partId: string;
  fromWarehouseId: string;
  toWarehouseId: string;
  quantity: number;
  quantityInBaseUnit?: number;
  unitId?: string;
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
  variantId?: string | null;
  warehouseId: string;
  quantity: number;
  quantityInBaseUnit?: number;
  unitId?: string;
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

export interface PaginatedResponse<T> {
  data: T[];
  pagination: {
    pageNumber: number;
    pageSize: number;
    totalCount: number;
    totalPages: number;
  };
}

@Injectable({
  providedIn: 'root'
})
export class StockService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/v1/stock`;

  /**
   * Get all stock levels
   */
  getAllStockLevels(): Observable<StockLevelResponse[]> {
    return this.http.get<StockLevelResponse[]>(`${this.apiUrl}/levels`);
  }

  /**
   * Get stock levels with pagination & filters
   */
  getStockLevels(query: StockLevelQuery): Observable<PaginatedResponse<StockLevelResponse>> {
    return this.http.post<PaginatedResponse<StockLevelResponse>>(`${this.apiUrl}/levels/list`, query);
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
   * Get stock movements with pagination & filters
   */
  getStockMovements(query: StockMovementQuery): Observable<PaginatedResponse<StockMovementResponse>> {
    return this.http.post<PaginatedResponse<StockMovementResponse>>(`${this.apiUrl}/movements/list`, query);
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
