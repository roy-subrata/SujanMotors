import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ButtonModule } from 'primeng/button';
import { TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { TooltipModule } from 'primeng/tooltip';
import { CardModule } from 'primeng/card';
import { ToastModule } from 'primeng/toast';
import { MessageService } from 'primeng/api';
import { forkJoin } from 'rxjs';
import { StockService, StockMovementResponse } from '../services/stock.service';
import { PartService, PartResponse } from '../services/part.service';
import { WarehouseService, WarehouseResponse } from '../services/warehouse.service';

@Component({
  selector: 'app-stock-movement-history',
  standalone: true,
  imports: [
    CommonModule,
    ButtonModule,
    TableModule,
    TagModule,
    TooltipModule,
    CardModule,
    ToastModule
  ],
  providers: [MessageService],
  template: `
    <p-toast></p-toast>

    <p-card>

      <p-table
        [value]="movements"
        [rows]="10"
        [paginator]="true"
        [loading]="loading"
        responsiveLayout="scroll"
        styleClass="p-datatable-striped">

        <!-- Header Template -->
        <ng-template pTemplate="header">
          <tr>
            <th style="width: 15%">Date & Time</th>
            <th style="width: 15%">Part</th>
            <th style="width: 15%">Warehouse</th>
            <th style="width: 10%" class="text-right">Quantity</th>
            <th style="width: 12%">Type</th>
            <th style="width: 12%">Status</th>
            <th style="width: 20%">Notes / Reference</th>
          </tr>
        </ng-template>

        <!-- Data Rows -->
        <ng-template pTemplate="body" let-movement>
          <tr>
            <td>
              <span class="text-sm">{{ movement.createdAt | date:'dd-MMM-yyyy HH:mm' }}</span>
            </td>
            <td>
              <span class="font-semibold">{{ getPartDisplay(movement) }}</span>
            </td>
            <td>
              <span class="text-gray-600">{{ getWarehouseDisplay(movement) }}</span>
            </td>
            <td class="text-right">
              <span
                class="font-semibold"
                [ngClass]="getQuantityClass(movement.type)">
                {{ getQuantityDisplay(movement.type, movement.quantity) }}
              </span>
            </td>
            <td>
              <p-tag
                [value]="getMovementTypeLabel(movement.type)"
                [severity]="getMovementTypeSeverity(movement.type)">
              </p-tag>
            </td>
            <td>
              <p-tag
                [value]="movement.status"
                [severity]="getStatusSeverity(movement.status)">
              </p-tag>
            </td>
            <td>
              <div class="flex flex-col gap-1">
                <span class="text-sm font-semibold">{{ movement.reference }}</span>
                <span class="text-xs text-gray-500" *ngIf="movement.notes" [pTooltip]="movement.notes" tooltipPosition="left">
                  {{ getMovementDescription(movement) }}
                </span>
              </div>
            </td>
          </tr>
        </ng-template>

        <!-- Empty Message -->
        <ng-template pTemplate="emptymessage">
          <tr>
            <td colspan="7" class="text-center py-4">
              <i class="pi pi-inbox mr-2"></i>
              No stock movements found
            </td>
          </tr>
        </ng-template>
      </p-table>
    </p-card>
  `,
  styles: [`
    .history-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      width: 100%;
      padding: 0;
    }

    .history-header h3 {
      margin: 0;
      font-size: 1.125rem;
      font-weight: 600;
    }

    .text-sm {
      font-size: 0.875rem;
    }

    .text-gray-600 {
      color: #4b5563;
    }

    .font-semibold {
      font-weight: 600;
    }

    .text-right {
      text-align: right;
    }

    .text-green {
      color: #059669;
    }

    .text-red {
      color: #dc2626;
    }

    .text-orange {
      color: #ea580c;
    }
  `]
})
export class StockMovementHistoryComponent implements OnInit {
  private readonly stockService = inject(StockService);
  private readonly partService = inject(PartService);
  private readonly warehouseService = inject(WarehouseService);
  private readonly messageService = inject(MessageService);

  movements: StockMovementResponse[] = [];
  parts: PartResponse[] = [];
  warehouses: WarehouseResponse[] = [];
  loading = false;

  ngOnInit(): void {
    this.loadAllData();

    // Auto-refresh movement history every 30 seconds
    setInterval(() => {
      this.refreshMovements();
    }, 30000);
  }

  private refreshMovements(): void {
    // Only refresh if not currently loading
    if (!this.loading) {
      this.loadMovements();
    }
  }

  private loadAllData(): void {
    this.loading = true;
    // Load parts and warehouses in parallel, then load movements
    forkJoin({
      parts: this.partService.getAllParts(),
      warehouses: this.warehouseService.getAllWarehouses()
    }).subscribe({
      next: (result) => {
        this.parts = Array.isArray(result.parts) ? result.parts : [];
        this.warehouses = Array.isArray(result.warehouses) ? result.warehouses : [];
        console.log('Loaded parts:', this.parts.length, 'warehouses:', this.warehouses.length);
        // Now load movements after parts and warehouses are ready
        this.loadMovements();
      },
      error: (error) => {
        console.error('Error loading reference data:', error);
        this.loading = false;
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: 'Failed to load reference data'
        });
      }
    });
  }

  loadMovements(): void {
    this.loading = true;
    this.stockService.getAllMovements().subscribe({
      next: (movements) => {
        // Sort by created date descending (newest first)
        this.movements = Array.isArray(movements)
          ? movements.sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime())
          : [];
        this.loading = false;
      },
      error: (_error) => {
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: 'Failed to load stock movements'
        });
        this.loading = false;
      }
    });
  }

  getPartDisplay(movement: StockMovementResponse): string {
    // Use partName and partCode directly from the API response
    if (movement.partName) {
      return movement.partCode ? `${movement.partName} (${movement.partCode})` : movement.partName;
    }
    // Fallback: try to find in local parts array
    const part = this.parts.find(p => p.id === movement.partId);
    if (part) {
      const code = part.partNumber || part.sku || '';
      return code ? `${part.name} (${code})` : part.name;
    }
    return movement.partId || 'Unknown Part';
  }

  getWarehouseDisplay(movement: StockMovementResponse): string {
    // Use warehouseName and warehouseCode directly from the API response
    if (movement.warehouseName) {
      return movement.warehouseCode ? `${movement.warehouseName} (${movement.warehouseCode})` : movement.warehouseName;
    }
    // Fallback: try to find in local warehouses array
    const warehouse = this.warehouses.find(w => w.id === movement.warehouseId);
    if (warehouse) {
      return warehouse.code ? `${warehouse.name} (${warehouse.code})` : warehouse.name;
    }
    return movement.warehouseId || 'Unknown Warehouse';
  }

  // Keep old methods for backward compatibility
  getPartInfo(partId: string): string {
    const part = this.parts.find(p => p.id === partId);
    if (part) {
      // Show name with part number/SKU
      const code = part.partNumber || part.sku || '';
      return code ? `${part.name} (${code})` : part.name;
    }
    return partId; // Fallback to ID if part not found
  }

  getWarehouseName(warehouseId: string): string {
    const warehouse = this.warehouses.find(w => w.id === warehouseId);
    if (warehouse) {
      // Show name with code
      return warehouse.code ? `${warehouse.name} (${warehouse.code})` : warehouse.name;
    }
    return warehouseId; // Fallback to ID if warehouse not found
  }

  getMovementTypeLabel(type: string): string {
    const types: { [key: string]: string } = {
      'IN': 'Stock In',
      'OUT': 'Stock Out',
      'RETURN': 'Return',
      'ADJUST': 'Adjustment',
      'ADJUSTMENT': 'Adjustment',
      'TRANSFER': 'Transfer',
      'DAMAGE': 'Damage',
      'SHRINKAGE': 'Shrinkage',
      'COUNT_CORRECTION': 'Count Correction'
    };
    return types[type] || type;
  }

  getMovementTypeSeverity(type: string): string {
    switch (type) {
      case 'IN':
      case 'COUNT_CORRECTION':
        return 'success';
      case 'OUT':
      case 'DAMAGE':
      case 'SHRINKAGE':
        return 'danger';
      case 'RETURN':
      case 'TRANSFER':
        return 'info';
      case 'ADJUST':
      case 'ADJUSTMENT':
        return 'warning';
      default:
        return 'info';
    }
  }

  getStatusSeverity(status: string): string {
    switch (status) {
      case 'PENDING':
        return 'warning';
      case 'APPROVED':
        return 'success';
      case 'REJECTED':
        return 'danger';
      default:
        return 'info';
    }
  }

  getQuantityDisplay(type: string, quantity: number): string {
    if (type === 'IN' || type === 'COUNT_CORRECTION') {
      return `+${quantity}`;
    } else if (type === 'OUT' || type === 'DAMAGE' || type === 'SHRINKAGE') {
      return `-${quantity}`;
    } else if (type === 'RETURN' || type === 'ADJUST' || type === 'ADJUSTMENT') {
      // RETURN can be IN or OUT depending on context, show neutral
      return `${quantity}`;
    } else if (type === 'TRANSFER') {
      return `${quantity}`;
    } else {
      return `-${quantity}`;
    }
  }

  getQuantityClass(type: string): string {
    if (type === 'IN' || type === 'COUNT_CORRECTION') {
      return 'text-green';
    } else if (type === 'OUT' || type === 'DAMAGE' || type === 'SHRINKAGE') {
      return 'text-red';
    } else if (type === 'RETURN' || type === 'TRANSFER' || type === 'ADJUST' || type === 'ADJUSTMENT') {
      return 'text-orange';
    }
    return 'text-orange';
  }

  getMovementDescription(movement: StockMovementResponse): string {
    // Provide context based on reference number prefix
    const ref = movement.reference || '';
    const reason = movement.reason || '';

    if (ref.startsWith('PR-')) {
      return 'Returned to supplier';
    } else if (ref.startsWith('INV-') || reason.includes('Quick Sale')) {
      return 'Quick sale to customer';
    } else if (ref.startsWith('SO-') || ref.startsWith('Sales Order')) {
      return 'Sold to customer';
    } else if (ref.startsWith('SR-') || ref.includes('Sales Return')) {
      return 'Returned by customer';
    } else if (ref.startsWith('GRN-') || ref.includes('GRN')) {
      return 'Received from supplier';
    } else if (ref.startsWith('ADJ-')) {
      return 'Stock adjustment';
    } else if (ref.startsWith('TRF-')) {
      return 'Warehouse transfer';
    }

    return movement.notes || '';
  }
}
