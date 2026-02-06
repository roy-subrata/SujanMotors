import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { SelectModule } from 'primeng/select';
import { TableLazyLoadEvent, TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { ToastModule } from 'primeng/toast';
import { MessageService } from 'primeng/api';
import { StockLotService, StockLotPriceHistoryResponse, StockLotHistoryItem } from '../services/stock-lot.service';
import { PartService, PartResponse } from '../services/part.service';
import { CurrencyService } from '../../../shared/services/currency.service';

@Component({
  selector: 'app-stock-price-history',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ButtonModule,
    CardModule,
    SelectModule,
    TableModule,
    TagModule,
    ToastModule
  ],
  providers: [MessageService],
  templateUrl: './stock-price-history.component.html',
  styleUrls: ['./stock-price-history.component.css']
})
export class StockPriceHistoryComponent implements OnInit {
  private readonly stockLotService = inject(StockLotService);
  private readonly partService = inject(PartService);
  private readonly messageService = inject(MessageService);
  private readonly currencyService = inject(CurrencyService);

  parts: PartResponse[] = [];
  priceHistory: StockLotPriceHistoryResponse | null = null;
  selectedPartId: string | null = null;

  loading = false;
  Math = Math;
  totalRecords = 0;
  pageNumber = 1;
  pageSize = 10;
  first = 0;
  pageSizeOptions = [10, 25, 50];

  get currencyCode(): string {
    return this.currencyService.selectedCurrency();
  }

  ngOnInit(): void {
    this.loadParts();

    // Auto-refresh price history every 30 seconds when a part is selected
    setInterval(() => {
      this.refreshPriceHistory();
    }, 30000);
  }

  private refreshPriceHistory(): void {
    // Only refresh if a part is selected and not currently loading
    if (this.selectedPartId && !this.loading) {
      this.loadPriceHistory();
    }
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

  onPartSelected(): void {
    this.priceHistory = null;
    this.resetPagination();
  }

  loadPriceHistory(): void {
    if (!this.selectedPartId) {
      return;
    }

    this.loading = true;
    this.stockLotService.getPriceHistory(this.selectedPartId, this.pageNumber, this.pageSize).subscribe({
      next: (history) => {
        this.priceHistory = history;
        this.totalRecords = history.pagination?.totalCount ?? 0;
        this.loading = false;
      },
      error: (_error) => {
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: 'Failed to load price history'
        });
        this.loading = false;
      }
    });
  }

  onLazyLoad(event: TableLazyLoadEvent): void {
    this.first = event.first ?? 0;
    this.pageSize = event.rows ?? this.pageSize;
    this.pageNumber = Math.floor(this.first / this.pageSize) + 1;
    this.loadPriceHistory();
  }

  private resetPagination(): void {
    this.first = 0;
    this.pageNumber = 1;
  }

  getPriceVariation(): number {
    if (!this.priceHistory || this.priceHistory.lots.length < 2) {
      return 0;
    }
    const latest = this.priceHistory.lots[0].costPrice;
    const oldest = this.priceHistory.lots[this.priceHistory.lots.length - 1].costPrice;
    return latest - oldest;
  }

  getPriceVariationPercent(): number {
    if (!this.priceHistory || this.priceHistory.lots.length < 2) {
      return 0;
    }
    const oldest = this.priceHistory.lots[this.priceHistory.lots.length - 1].costPrice;
    if (oldest === 0) return 0;
    return ((this.getPriceVariation() / oldest) * 100);
  }

  getPriceClass(price: number, history: StockLotPriceHistoryResponse): string {
    if (price === history.minPrice) {
      return 'text-green-600';
    } else if (price === history.maxPrice) {
      return 'text-red-600';
    }
    return '';
  }

  getExpiryDisplay(lot: StockLotHistoryItem): string {
    if (!lot.expiryDate) return 'N/A';
    if (lot.isExpired) return 'Expired';
    return new Date(lot.expiryDate).toLocaleDateString('en-IN', { day: '2-digit', month: 'short', year: 'numeric' });
  }
}
