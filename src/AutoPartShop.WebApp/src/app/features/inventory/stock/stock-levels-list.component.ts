import { Component, EventEmitter, Input, Output, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { TagModule } from 'primeng/tag';
import { TooltipModule } from 'primeng/tooltip';
import { StockLevelResponse } from '../services/stock.service';

@Component({
  selector: 'app-stock-levels-list',
  standalone: true,
  imports: [
    CommonModule,
    TableModule,
    ButtonModule,
    TagModule,
    TooltipModule
  ],
  templateUrl: './stock-levels-list.component.html',
  styleUrls: ['./stock-levels-list.component.css']
})
export class StockLevelsListComponent implements OnChanges {
  @Input() stockLevels: StockLevelResponse[] = [];
  @Input() loading = false;
  @Input() totalRecords = 0;
  @Input() rows = 10;
  @Input() currentPage = 1;
  @Input() getPartInfo: ((partId: string) => string) | null = null;
  @Input() getWarehouseName: ((warehouseId: string) => string) | null = null;

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['stockLevels']) {
      console.log('📋 TABLE received new stockLevels:', this.stockLevels.length, 'items');
      if (this.stockLevels.length > 0) {
        console.log('First item ID:', this.stockLevels[0].id.substring(0, 8) + '...');
      }
    }
  }

  @Output() adjustClick = new EventEmitter<StockLevelResponse>();
  @Output() pageChange = new EventEmitter<{ page: number; rows: number }>();

  onPageChange(event: any): void {
    if (!event || typeof event.first !== 'number' || typeof event.rows !== 'number') {
      return;
    }
    const pageNumber = Math.floor(event.first / event.rows) + 1;
    this.pageChange.emit({
      page: pageNumber,
      rows: event.rows
    });
  }

  onAdjustClick(stock: StockLevelResponse): void {
    this.adjustClick.emit(stock);
  }

  getStockSeverity(stock: StockLevelResponse): string {
    if (stock.needsReorder) return 'danger';
    if (stock.availableQuantity < stock.reorderLevel * 1.5) return 'warning';
    return 'success';
  }

  getStockStatus(stock: StockLevelResponse): string {
    if (stock.needsReorder) return 'Low Stock';
    if (stock.availableQuantity < stock.reorderLevel * 1.5) return 'Warning';
    return 'In Stock';
  }
}
