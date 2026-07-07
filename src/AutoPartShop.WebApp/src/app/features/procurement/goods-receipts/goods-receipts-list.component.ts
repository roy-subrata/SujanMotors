import { Component, EventEmitter, Input, Output, ViewChild, inject, OnInit, DestroyRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { MenuModule, Menu } from 'primeng/menu';
import { TooltipModule } from 'primeng/tooltip';
import { ConfirmationService, MessageService, MenuItem } from 'primeng/api';
import { GoodsReceiptService, GoodsReceiptResponse } from '../services/goods-receipt.service';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { I18nService } from '@/shared/services/i18n.service';
import { AuthService } from '../../../shared/services/auth.service';
import { DataPaginationComponent } from '@/shared/components/data-pagination/data-pagination.component';

@Component({
  selector: 'app-goods-receipts-list',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    TableModule,
    ButtonModule,
    MenuModule,
    TooltipModule,
    DataPaginationComponent
  ],
  providers: [ConfirmationService, MessageService],
  templateUrl: './goods-receipts-list.component.html',
  styleUrls: ['./goods-receipts-list.component.css']
})
export class GoodsReceiptsListComponent implements OnInit {
  @ViewChild('actionMenu') actionMenu!: Menu;

  @Input() goodsReceipts: GoodsReceiptResponse[] = [];
  @Input() loading = false;
  @Input() totalRecords = 0;
  @Input() rows = 10;
  @Input() currentPage = 1;
  @Input() searchTerm = '';
  @Input() hasActiveFilters = false;

  @Output() pageChange = new EventEmitter<{ page: number; rows: number }>();
  @Output() grnDeleted = new EventEmitter<void>();

  actionMenuItems: MenuItem[] = [];
  selectedGrn: GoodsReceiptResponse | null = null;

  pageSizeOptions = [
    { label: '10', value: 10 },
    { label: '20', value: 20 },
    { label: '50', value: 50 }
  ];

  private readonly grnService = inject(GoodsReceiptService);
  private readonly confirmationService = inject(ConfirmationService);
  private readonly messageService = inject(MessageService);
  private readonly router = inject(Router);
  private readonly i18n = inject(I18nService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly auth = inject(AuthService);

  /** Procurement mutations (create/delete) are restricted to back-office roles. */
  get canManage(): boolean {
    return this.auth.hasAnyRole(['Admin', 'Manager']);
  }

  ngOnInit(): void {
    this.i18n.translationsLoaded$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
      if (this.selectedGrn) this.buildActionMenuItems(this.selectedGrn);
    });
  }

  get totalPages(): number {
    return Math.max(1, Math.ceil(this.totalRecords / this.rows));
  }

  get firstIndex(): number {
    return Math.max(0, (this.currentPage - 1) * this.rows);
  }

  private buildActionMenuItems(grn: GoodsReceiptResponse): void {
    this.actionMenuItems = [
      {
        label: this.i18n.t('common.actions.viewDetails'),
        icon: 'pi pi-eye',
        command: () => this.viewGoodsReceipt(grn)
      },
      {
        label: this.i18n.t('common.actions.delete'),
        icon: 'pi pi-trash',
        command: () => this.deleteGoodsReceipt(grn),
        visible: grn.status === 'PENDING' && this.canManage,
        styleClass: 'text-red-600'
      }
    ];
  }

  showActionMenu(event: Event, grn: GoodsReceiptResponse): void {
    this.selectedGrn = grn;
    this.buildActionMenuItems(grn);
    this.actionMenu.toggle(event);
  }

  viewGoodsReceipt(grn: GoodsReceiptResponse): void {
    this.router.navigate(['/procurement/goods-receipts/view'], { queryParams: { id: grn.id } });
  }

  deleteGoodsReceipt(grn: GoodsReceiptResponse): void {
    this.confirmationService.confirm({
      message: this.i18n.t('goodsReceipts.messages.deleteConfirm', { number: grn.grnNumber }),
      header: this.i18n.t('common.messages.confirmDeletion'),
      icon: 'pi pi-exclamation-triangle',
      acceptButtonStyleClass: 'p-button-danger',
      accept: () => {
        this.deleteGrnById(grn.id);
      }
    });
  }

  private deleteGrnById(id: string): void {
    this.grnService.deleteGoodsReceipt(id).subscribe({
      next: () => {
        this.messageService.add({
          severity: 'success',
          summary: this.i18n.t('common.messages.success'),
          detail: this.i18n.t('goodsReceipts.messages.deleteSuccess')
        });
        this.grnDeleted.emit();
      },
      error: (error) => {
        this.messageService.add({
          severity: 'error',
          summary: this.i18n.t('common.messages.error'),
          detail: error?.error?.message || this.i18n.t('goodsReceipts.messages.deleteFailed')
        });
        console.error('Error deleting GRN:', error);
      }
    });
  }

  goToPage(page: number): void {
    this.pageChange.emit({ page, rows: this.rows });
  }

  onPageSizeChange(size?: number): void {
    if (size !== undefined) this.rows = size;
    this.pageChange.emit({ page: 1, rows: this.rows });
  }

  goToFirstPage(): void {
    if (this.currentPage === 1) return;
    this.pageChange.emit({ page: 1, rows: this.rows });
  }

  goToPrevPage(): void {
    if (this.currentPage <= 1) return;
    this.pageChange.emit({ page: this.currentPage - 1, rows: this.rows });
  }

  goToNextPage(): void {
    if (this.currentPage >= this.totalPages) return;
    this.pageChange.emit({ page: this.currentPage + 1, rows: this.rows });
  }

  goToLastPage(): void {
    if (this.currentPage >= this.totalPages) return;
    this.pageChange.emit({ page: this.totalPages, rows: this.rows });
  }

  formatDate(date: string): string {
    if (!date) return '-';
    return new Date(date).toLocaleDateString('en-IN', {
      day: '2-digit',
      month: 'short',
      year: 'numeric'
    });
  }

  formatStatus(status: string): string {
    if (!status) return '-';
    return status
      .split('_')
      .map((word) => word.charAt(0) + word.slice(1).toLowerCase())
      .join(' ');
  }
}
