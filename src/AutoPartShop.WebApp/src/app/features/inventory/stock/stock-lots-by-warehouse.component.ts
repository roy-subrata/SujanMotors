import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { InputTextModule } from 'primeng/inputtext';
import { SelectModule } from 'primeng/select';
import { TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { ToastModule } from 'primeng/toast';
import { TooltipModule } from 'primeng/tooltip';
import { MessageService } from 'primeng/api';
import { StockLotService, StockLotResponse } from '../services/stock-lot.service';
import { PartService, PartResponse } from '../services/part.service';
import { WarehouseService, WarehouseResponse } from '../services/warehouse.service';

@Component({
  selector: 'app-stock-lots-by-warehouse',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ButtonModule,
    CardModule,
    InputTextModule,
    SelectModule,
    TableModule,
    TagModule,
    ToastModule,
    TooltipModule
  ],
  providers: [MessageService],
  templateUrl: './stock-lots-by-warehouse.component.html',
  styleUrls: ['./stock-lots-by-warehouse.component.css']
})
export class StockLotsByWarehouseComponent implements OnInit {
  private readonly stockLotService = inject(StockLotService);
  private readonly partService = inject(PartService);
  private readonly warehouseService = inject(WarehouseService);
  private readonly messageService = inject(MessageService);

  parts: PartResponse[] = [];
  warehouses: WarehouseResponse[] = [];
  stockLots: StockLotResponse[] = [];

  selectedPartId: string | null = null;
  selectedWarehouseId: string | null = null;
  selectedPartName: string = '';
  selectedWarehouseName: string = '';

  loading = false;

  ngOnInit(): void {
    this.loadParts();
    this.loadWarehouses();
  }

  private loadParts(): void {
    this.partService.getAllParts().subscribe({
      next: (parts) => {
        this.parts = Array.isArray(parts) ? parts : [];
      },
      error: (_error) => {
        console.error('Error loading parts:', _error);
      }
    });
  }

  private loadWarehouses(): void {
    this.warehouseService.getAllWarehouses().subscribe({
      next: (warehouses) => {
        this.warehouses = Array.isArray(warehouses) ? warehouses : [];
      },
      error: (_error) => {
        console.error('Error loading warehouses:', _error);
      }
    });
  }

  onPartSelected(): void {
    if (this.selectedPartId) {
      const part = this.parts.find(p => p.id === this.selectedPartId);
      this.selectedPartName = part ? `${part.name} (${part.sku})` : '';
    }
  }

  onWarehouseSelected(): void {
    if (this.selectedWarehouseId) {
      const warehouse = this.warehouses.find(w => w.id === this.selectedWarehouseId);
      this.selectedWarehouseName = warehouse?.name || '';
    }
  }

  loadStockLots(): void {
    if (!this.selectedPartId || !this.selectedWarehouseId) {
      return;
    }

    this.loading = true;
    this.stockLotService.getByPartAndWarehouse(this.selectedPartId, this.selectedWarehouseId).subscribe({
      next: (lots) => {
        this.stockLots = Array.isArray(lots) ? lots : [];
        this.loading = false;
      },
      error: (_error) => {
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: 'Failed to load stock lots'
        });
        this.loading = false;
      }
    });
  }

  getTotalCost(): number {
    return this.stockLots.reduce((sum, lot) => sum + lot.totalCost, 0);
  }

  getTotalAvailableCost(): number {
    return this.stockLots.reduce((sum, lot) => sum + lot.availableCost, 0);
  }

  getAverageCostPerUnit(): number {
    const totalQuantity = this.stockLots.reduce((sum, lot) => sum + lot.quantityAvailable, 0);
    if (totalQuantity === 0) return 0;
    return this.getTotalAvailableCost() / totalQuantity;
  }

  getExpiryDisplay(lot: StockLotResponse): string {
    if (!lot.expiryDate) return 'N/A';
    if (lot.isExpired) return 'Expired';
    return new Date(lot.expiryDate).toLocaleDateString('en-IN', { day: '2-digit', month: 'short', year: 'numeric' });
  }
}
