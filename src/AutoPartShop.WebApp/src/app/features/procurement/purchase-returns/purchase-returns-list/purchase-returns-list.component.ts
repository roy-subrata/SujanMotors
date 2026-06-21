import { Component, EventEmitter, Input, Output, ViewChild, inject, OnInit, DestroyRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { ContextMenuModule, ContextMenu } from 'primeng/contextmenu';
import { RippleModule } from 'primeng/ripple';
import { TagModule } from 'primeng/tag';
import { TooltipModule } from 'primeng/tooltip';
import { PaginatorModule } from 'primeng/paginator';
import { InputGroupModule } from 'primeng/inputgroup';
import { InputGroupAddonModule } from 'primeng/inputgroupaddon';
import { InputTextModule } from 'primeng/inputtext';
import { ConfirmationService, MessageService, MenuItem } from 'primeng/api';
import { PurchaseReturnService, PurchaseReturnResponse } from '../../services/purchase-return.service';
import { CurrencyService } from '../../../../shared/services/currency.service';
import { I18nService } from '@/shared/services/i18n.service';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { AuthService } from '../../../../shared/services/auth.service';

@Component({
  selector: 'app-purchase-returns-list',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    TableModule,
    ButtonModule,
    ConfirmDialogModule,
    ContextMenuModule,
    RippleModule,
    TagModule,
    TooltipModule,
    PaginatorModule,
    InputGroupModule,
    InputGroupAddonModule,
    InputTextModule
  ],
  templateUrl: './purchase-returns-list.component.html',
  styleUrls: ['./purchase-returns-list.component.css']
})
export class PurchaseReturnsListComponent implements OnInit {
  @ViewChild('contextMenu') contextMenu: ContextMenu | undefined;

  @Input() purchaseReturns: PurchaseReturnResponse[] = [];
  @Input() loading = false;
  @Input() totalRecords = 0;
  @Input() rows = 10;
  @Input() currentPage = 1;
  @Input() searchTerm = '';

  @Output() pageChange = new EventEmitter<{ page: number; rows: number }>();
  @Output() onSearch = new EventEmitter<string>();
  @Output() onSearchClear = new EventEmitter<void>();
  @Output() onReturnCreated = new EventEmitter<void>();
  @Output() onReturnDeleted = new EventEmitter<void>();

  contextMenuItems: MenuItem[] = [];
  selectedPurchaseReturn: PurchaseReturnResponse | null = null;
  searchValue = '';
  pageSizeOptions = [10, 25, 50, 100];
  Math = Math;

  private readonly prService = inject(PurchaseReturnService);
  private readonly confirmationService = inject(ConfirmationService);
  private readonly messageService = inject(MessageService);
  private readonly router = inject(Router);
  private readonly currencyService = inject(CurrencyService);
  private readonly i18n = inject(I18nService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly auth = inject(AuthService);

  /** Procurement mutations (create/edit/delete) are restricted to back-office roles. */
  get canManage(): boolean {
    return this.auth.hasAnyRole(['Admin', 'Manager']);
  }

  ngOnInit(): void {
    this.rebuildContextMenu();
    this.i18n.translationsLoaded$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
      if (this.selectedPurchaseReturn) this.rebuildContextMenu();
    });
  }

  private rebuildContextMenu(): void {
    const pr = this.selectedPurchaseReturn;
    const status = pr?.status;

    this.contextMenuItems = [
      {
        label: this.i18n.t('common.actions.viewDetails'),
        icon: 'pi pi-eye',
        command: () => { if (pr) this.viewDetails(pr); }
      },
      {
        label: this.i18n.t('common.actions.edit'),
        icon: 'pi pi-pencil',
        command: () => { if (pr) this.onEditClick(pr); },
        visible: status === 'PENDING' && this.canManage
      },
      { separator: true },
      {
        label: this.i18n.t('common.actions.delete'),
        icon: 'pi pi-trash',
        command: () => { if (pr) this.onDeleteClick(pr); },
        visible: status === 'PENDING' && this.canManage,
        styleClass: 'p-menuitem-danger'
      }
    ];
  }

  showContextMenu(event: any, pr: PurchaseReturnResponse): void {
    this.selectedPurchaseReturn = pr;
    this.rebuildContextMenu();
    if (this.contextMenu) {
      this.contextMenu.show(event);
    }
  }

  viewPurchaseReturn(pr: PurchaseReturnResponse): void {
    this.router.navigate(['/procurement/purchase-returns/view'], { queryParams: { id: pr.id } });
  }

  editPurchaseReturn(pr: PurchaseReturnResponse): void {
    this.router.navigate(['/procurement/purchase-returns/edit'], { queryParams: { id: pr.id } });
  }

  onEditClick(pr: PurchaseReturnResponse): void {
    this.editPurchaseReturn(pr);
  }

  viewDetails(pr: PurchaseReturnResponse): void {
    this.viewPurchaseReturn(pr);
  }

  deletePurchaseReturn(pr: PurchaseReturnResponse): void {
    this.confirmationService.confirm({
      message: this.i18n.t('purchaseReturns.messages.deleteConfirm'),
      header: this.i18n.t('common.messages.confirmDeletion'),
      icon: 'pi pi-exclamation-triangle',
      acceptButtonStyleClass: 'p-button-danger',
      accept: () => {
        this.deletePurchaseReturnById(pr.id);
      }
    });
  }

  onDeleteClick(pr: PurchaseReturnResponse): void {
    this.deletePurchaseReturn(pr);
  }

  private deletePurchaseReturnById(id: string): void {
    this.prService.deletePurchaseReturn(id).subscribe({
      next: () => {
        this.messageService.add({
          severity: 'success',
          summary: this.i18n.t('common.messages.success'),
          detail: this.i18n.t('purchaseReturns.messages.deleteSuccess')
        });
        this.onReturnDeleted.emit();
      },
      error: (error) => {
        this.messageService.add({
          severity: 'error',
          summary: this.i18n.t('common.messages.error'),
          detail: error?.error?.message || this.i18n.t('purchaseReturns.messages.deleteFailed')
        });
        console.error('Error deleting purchase return:', error);
      }
    });
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

  onSearchInput(event: Event): void {
    const value = (event.target as HTMLInputElement).value;
    this.searchValue = value;
    if (value && value.trim()) {
      this.onSearch.emit(value.trim());
    }
  }

  clearSearch(): void {
    this.searchValue = '';
    this.onSearchClear.emit();
  }

  getStatusSeverity(status: string): string {
    switch (status?.toUpperCase()) {
      case 'PENDING': return 'warning';
      case 'APPROVED': return 'info';
      case 'RETURNED': return 'primary';
      case 'RECEIVED': return 'success';
      case 'CREDITED': return 'success';
      case 'REJECTED': return 'danger';
      default: return 'secondary';
    }
  }

  formatCurrency(value: number): string {
    const currency = this.currencyService.selectedCurrency();
    return this.currencyService.formatCurrency(value, currency);
  }

  formatDate(date: string): string {
    return new Date(date).toLocaleDateString('en-IN');
  }
}
