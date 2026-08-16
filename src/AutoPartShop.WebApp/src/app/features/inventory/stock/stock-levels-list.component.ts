import { Component, EventEmitter, Input, Output, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { TagModule } from 'primeng/tag';
import { TooltipModule } from 'primeng/tooltip';
import { StockLevelResponse } from '../services/stock.service';
import { I18nService } from '@/shared/services/i18n.service';
import { TranslatePipe } from '@/shared/pipes/translate.pipe';
import { DataPaginationComponent } from '@/shared/components/data-pagination/data-pagination.component';

@Component({
  selector: 'app-stock-levels-list',
  standalone: true,
  imports: [
    CommonModule,
    TableModule,
    ButtonModule,
    TagModule,
    TooltipModule,
    TranslatePipe,
    DataPaginationComponent
  ],
  templateUrl: './stock-levels-list.component.html',
  styleUrls: ['./stock-levels-list.component.css']
})
export class StockLevelsListComponent {
  @Input() stockLevels: StockLevelResponse[] = [];
  @Input() loading = false;
  @Input() totalRecords = 0;
  @Input() rows = 10;
  @Input() currentPage = 1;
  @Input() getPartInfo: ((partId: string) => string) | null = null;
  @Input() getWarehouseName: ((warehouseId: string) => string) | null = null;

  private readonly i18n = inject(I18nService);

  @Output() adjustClick = new EventEmitter<StockLevelResponse>();
  @Output() pageChange = new EventEmitter<{ page: number; rows: number }>();

  get first(): number {
    return Math.max(0, (this.currentPage - 1) * this.rows);
  }

  get totalPages(): number {
    if (!this.totalRecords || !this.rows) return 0;
    return Math.ceil(this.totalRecords / this.rows);
  }

  goToPage(page: number): void {
    if (page < 1 || page > this.totalPages) return;
    this.pageChange.emit({ page, rows: this.rows });
  }

  onPageSizeChange(newRows: number): void {
    this.pageChange.emit({ page: 1, rows: newRows });
  }

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
    if (stock.availableQuantityInBaseUnit < stock.reorderLevel * 1.5) return 'warning';
    return 'success';
  }

  getStockStatus(stock: StockLevelResponse): string {
    if (stock.needsReorder) return this.i18n.t('stockLevels.status.lowStock');
    if (stock.availableQuantityInBaseUnit < stock.reorderLevel * 1.5) return this.i18n.t('stockLevels.status.warning');
    return this.i18n.t('stockLevels.status.inStock');
  }
}
