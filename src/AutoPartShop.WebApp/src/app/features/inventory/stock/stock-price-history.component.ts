import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { TableLazyLoadEvent, TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { ToastModule } from 'primeng/toast';
import { TooltipModule } from 'primeng/tooltip';
import { MessageService } from 'primeng/api';
import { StockLotService, StockLotPriceHistoryResponse, StockLotHistoryItem } from '../services/stock-lot.service';
import { PartService, PartResponse } from '../services/part.service';
import { CurrencyService } from '../../../shared/services/currency.service';
import { PriceCodeService } from '../../../shared/services/price-code.service';
import { LazyAutocompleteComponent, LazyRequest, LazyResponse } from '../../../shared/components/lazy-autocomplete';
import { map } from 'rxjs';

@Component({
  selector: 'app-stock-price-history',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ButtonModule,
    CardModule,
    TableModule,
    TagModule,
    ToastModule,
    TooltipModule,
    LazyAutocompleteComponent
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
  readonly priceCodeService = inject(PriceCodeService);

  priceHistory: StockLotPriceHistoryResponse | null = null;
  selectedPart: PartResponse | null = null;

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

  // Lazy autocomplete fetch function for parts
  fetchPartsLazy = (req: LazyRequest) =>
    this.partService.getParts({
      search: req.search || '',
      pageNumber: req.pageNumber,
      pageSize: req.pageSize,
      isActive: true,
      flattenVariants: true
    }).pipe(
      map((res) => ({
        items: res.data ?? [],
        totalCount: res.pagination?.totalCount ?? 0
      }) as LazyResponse<PartResponse>)
    );

  ngOnInit(): void {
    // Intentionally no timer-based polling. Data loads on user action.
  }

  onPartSelected(part: PartResponse | null): void {
    if (part) {
      this.selectedPart = part;
    } else {
      this.selectedPart = null;
    }
    this.priceHistory = null;
    this.resetPagination();
  }

  loadPriceHistory(): void {
    if (!this.selectedPart) {
      return;
    }

    this.loading = true;
    this.stockLotService.getPriceHistory(this.selectedPart.id, this.pageNumber, this.pageSize, this.selectedPart.variantId).subscribe({
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
