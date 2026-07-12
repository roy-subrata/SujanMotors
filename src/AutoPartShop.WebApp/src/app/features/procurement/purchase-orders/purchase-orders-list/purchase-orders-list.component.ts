import { Component, EventEmitter, Input, Output, ViewChild, inject, OnInit, DestroyRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';
import { ContextMenuModule, ContextMenu } from 'primeng/contextmenu';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { TooltipModule } from 'primeng/tooltip';
import { ConfirmationService, MessageService, MenuItem } from 'primeng/api';
import { DialogService, DynamicDialogRef } from 'primeng/dynamicdialog';
import { PurchaseOrderService, PurchaseOrderResponse } from '../../services/purchase-order.service';
import { CurrencyService } from '../../../../shared/services/currency.service';
import { ApplyAdvanceCreditDialogComponent } from '../apply-advance-credit/apply-advance-credit-dialog.component';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { I18nService } from '@/shared/services/i18n.service';
import { AuthService } from '../../../../shared/services/auth.service';
import { DataPaginationComponent } from '@/shared/components/data-pagination/data-pagination.component';

@Component({
  selector: 'app-purchase-orders-list',
  standalone: true,
  imports: [
    CommonModule,
    ContextMenuModule,
    ConfirmDialogModule,
    TooltipModule,
    RouterModule,
    DataPaginationComponent
  ],
  providers: [ConfirmationService, MessageService, DialogService],
  templateUrl: './purchase-orders-list.component.html',
  styleUrls: ['./purchase-orders-list.component.css']
})
export class PurchaseOrdersListComponent implements OnInit {
  @ViewChild('contextMenu') contextMenu: ContextMenu | undefined;

  @Input() purchaseOrders: PurchaseOrderResponse[] = [];
  @Input() loading = false;
  @Input() totalRecords = 0;
  @Input() rows = 10;
  @Input() currentPage = 1;
  @Input() searchTerm = '';

  @Output() editClick = new EventEmitter<PurchaseOrderResponse>();
  @Output() pageChange = new EventEmitter<{ page: number; rows: number }>();
  @Output() poDeleted = new EventEmitter<void>();

  contextMenuItems: MenuItem[] = [];
  selectedPurchaseOrder: PurchaseOrderResponse | null = null;
  pageSizeOptions = [10, 25, 50, 100];

  Math = Math;

  private readonly poService = inject(PurchaseOrderService);
  private readonly currencyService = inject(CurrencyService);
  private readonly confirmationService = inject(ConfirmationService);
  private readonly messageService = inject(MessageService);
  private readonly dialogService = inject(DialogService);
  private readonly router = inject(Router);
  private readonly i18n = inject(I18nService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly auth = inject(AuthService);

  /** Procurement mutations (create/edit/delete/payment) are restricted to back-office roles. */
  get canManage(): boolean {
    return this.auth.hasAnyRole(['Admin', 'Manager']);
  }

  private dialogRef: DynamicDialogRef | null | undefined;

  get totalPages(): number {
    return Math.ceil(this.totalRecords / this.rows) || 1;
  }

  ngOnInit(): void {
    this.rebuildContextMenu();
    this.i18n.translationsLoaded$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
      if (this.selectedPurchaseOrder) this.rebuildContextMenu();
    });
  }

  private rebuildContextMenu(): void {
    const po = this.selectedPurchaseOrder;
    this.contextMenuItems = [
      {
        label: this.i18n.t('common.actions.viewDetails'),
        icon: 'pi pi-eye',
        command: () => {
          if (po) this.viewDetails(po);
        }
      },
      {
        label: this.i18n.t('common.actions.edit'),
        icon: 'pi pi-pencil',
        command: () => {
          if (po) this.onEditClick(po);
        },
        visible: po ? po.status === 'DRAFT' && this.canManage : false
      },
      { separator: true },
      {
        label: this.i18n.t('common.actions.applyAdvanceCredit'),
        icon: 'pi pi-credit-card',
        command: () => {
          if (po) this.openApplyCreditDialog(po);
        },
        visible: po
          ? ['CONFIRMED', 'PARTIAL', 'DELIVERED'].includes(po.status) && po.outstandingAmount > 0 && this.canManage
          : false
      },
      {
        label: this.i18n.t('common.actions.recordPayment'),
        icon: 'pi pi-wallet',
        command: () => {
          if (po) this.recordPayment(po);
        },
        visible: po
          ? ['CONFIRMED', 'PARTIAL', 'DELIVERED'].includes(po.status) && po.outstandingAmount > 0 && this.canManage
          : false
      },
      { separator: true },
      {
        label: this.i18n.t('common.actions.delete'),
        icon: 'pi pi-trash',
        command: () => {
          if (po) this.onDeleteClick(po);
        },
        visible: po ? po.status === 'DRAFT' && this.canManage : false,
        styleClass: 'p-menuitem-danger'
      }
    ];
  }

  showContextMenu(event: MouseEvent, po: PurchaseOrderResponse): void {
    event.preventDefault();
    event.stopPropagation();
    this.selectedPurchaseOrder = po;
    this.rebuildContextMenu();
    this.contextMenu?.show(event);
  }

  getStatusClass(status: string): string {
    const statusLower = status?.toLowerCase() || '';
    switch (statusLower) {
      case 'draft':       return 'status-draft';
      case 'pending':     return 'status-pending';
      case 'partial':     return 'status-partial';
      case 'submitted':   return 'status-submitted';
      case 'processing':  return 'status-processing';
      case 'confirmed':   return 'status-confirmed';
      case 'delivered':   return 'status-delivered';
      case 'paid':        return 'status-paid';
      case 'cancelled':   return 'status-cancelled';
      case 'closed':      return 'status-closed';
      case 'active':      return 'status-active';
      case 'offline':     return 'status-offline';
      default:            return 'status-default';
    }
  }

  getStatusSeverity(status: string): string {
    switch (status?.toUpperCase()) {
      case 'DRAFT':     return 'warning';
      case 'SUBMITTED': return 'info';
      case 'CONFIRMED': return 'success';
      case 'PARTIAL':   return 'warning';
      case 'DELIVERED': return 'success';
      case 'CANCELLED': return 'danger';
      default:          return 'secondary';
    }
  }

  goToPage(page: number): void {
    if (page < 1 || page > this.totalPages) return;
    this.pageChange.emit({ page, rows: this.rows });
  }

  onPageSizeChange(size: number): void {
    this.pageChange.emit({ page: 1, rows: size });
  }

  onPageChange(event: any): void {
    if (!event || typeof event.first !== 'number' || typeof event.rows !== 'number') return;
    const pageNumber = Math.floor(event.first / event.rows) + 1;
    this.pageChange.emit({ page: pageNumber, rows: event.rows });
  }

  formatCurrency(value: number): string {
    const currency = this.currencyService.selectedCurrency();
    return this.currencyService.formatCurrency(value, currency);
  }

  formatDate(date: string): string {
    return new Date(date).toLocaleDateString('en-IN');
  }

  viewPurchaseOrder(po: PurchaseOrderResponse): void {
    this.router.navigate(['/procurement/purchase-orders/view'], { queryParams: { id: po.id } });
  }

  editPurchaseOrder(po: PurchaseOrderResponse): void {
    this.editClick.emit(po);
    this.router.navigate(['/procurement/purchase-orders/edit'], { queryParams: { id: po.id } });
  }

  onEditClick(po: PurchaseOrderResponse): void {
    this.editClick.emit(po);
    this.router.navigate(['/procurement/purchase-orders/edit'], { queryParams: { id: po.id } });
  }

  viewDetails(po: PurchaseOrderResponse): void {
    this.router.navigate(['/procurement/purchase-orders/view'], { queryParams: { id: po.id } });
  }

  submitPurchaseOrder(po: PurchaseOrderResponse): void {
    this.confirmationService.confirm({
      message: this.i18n.t('purchaseOrders.messages.submitConfirm', { number: po.poNumber }),
      header: this.i18n.t('common.actions.confirm'),
      icon: 'pi pi-exclamation-triangle',
      accept: () => {
        this.poService.submitPurchaseOrder(po.id).subscribe({
          next: () => {
            this.messageService.add({
              severity: 'success',
              summary: this.i18n.t('common.messages.success'),
              detail: this.i18n.t('purchaseOrders.messages.submitSuccess', { number: po.poNumber })
            });
            this.poDeleted.emit();
          },
          error: (error) => {
            this.messageService.add({
              severity: 'error',
              summary: this.i18n.t('common.messages.error'),
              detail: error?.error?.message || this.i18n.t('purchaseOrders.messages.submitFailed')
            });
            console.error('Error submitting purchase order:', error);
          }
        });
      }
    });
  }

  confirmPurchaseOrder(po: PurchaseOrderResponse): void {
    this.confirmationService.confirm({
      message: this.i18n.t('purchaseOrders.messages.confirmConfirm', { number: po.poNumber }),
      header: this.i18n.t('common.actions.confirm'),
      icon: 'pi pi-exclamation-triangle',
      accept: () => {
        this.poService.confirmPurchaseOrder(po.id).subscribe({
          next: () => {
            this.messageService.add({
              severity: 'success',
              summary: this.i18n.t('common.messages.success'),
              detail: this.i18n.t('purchaseOrders.messages.confirmSuccess', { number: po.poNumber })
            });
            this.poDeleted.emit();
          },
          error: (error) => {
            this.messageService.add({
              severity: 'error',
              summary: this.i18n.t('common.messages.error'),
              detail: error?.error?.message || this.i18n.t('purchaseOrders.messages.confirmFailed')
            });
            console.error('Error confirming purchase order:', error);
          }
        });
      }
    });
  }

  cancelPurchaseOrder(po: PurchaseOrderResponse): void {
    this.confirmationService.confirm({
      message: this.i18n.t('purchaseOrders.messages.cancelConfirm', { number: po.poNumber }),
      header: this.i18n.t('common.actions.confirm'),
      icon: 'pi pi-exclamation-triangle',
      accept: () => {
        this.poService.cancelPurchaseOrder(po.id).subscribe({
          next: () => {
            this.messageService.add({
              severity: 'success',
              summary: this.i18n.t('common.messages.success'),
              detail: this.i18n.t('purchaseOrders.messages.cancelSuccess', { number: po.poNumber })
            });
            this.poDeleted.emit();
          },
          error: (error) => {
            this.messageService.add({
              severity: 'error',
              summary: this.i18n.t('common.messages.error'),
              detail: error?.error?.message || this.i18n.t('purchaseOrders.messages.cancelFailed')
            });
            console.error('Error cancelling purchase order:', error);
          }
        });
      }
    });
  }

  deletePurchaseOrder(po: PurchaseOrderResponse): void {
    this.confirmationService.confirm({
      message: this.i18n.t('purchaseOrders.messages.deleteConfirm', { number: po.poNumber }),
      header: this.i18n.t('common.messages.confirmDeletion'),
      icon: 'pi pi-exclamation-triangle',
      acceptButtonStyleClass: 'p-button-danger',
      accept: () => {
        this.deletePurchaseOrderById(po.id);
      }
    });
  }

  onDeleteClick(po: PurchaseOrderResponse): void {
    this.deletePurchaseOrder(po);
  }

  private deletePurchaseOrderById(id: string): void {
    this.poService.deletePurchaseOrder(id).subscribe({
      next: () => {
        this.messageService.add({
          severity: 'success',
          summary: this.i18n.t('common.messages.success'),
          detail: this.i18n.t('purchaseOrders.messages.deleteSuccess')
        });
        this.poDeleted.emit();
      },
      error: (error) => {
        this.messageService.add({
          severity: 'error',
          summary: this.i18n.t('common.messages.error'),
          detail: error?.error?.message || this.i18n.t('purchaseOrders.messages.deleteFailed')
        });
        console.error('Error deleting purchase order:', error);
      }
    });
  }

  openApplyCreditDialog(po: PurchaseOrderResponse): void {
    this.dialogRef = this.dialogService.open(ApplyAdvanceCreditDialogComponent, {
      header: this.i18n.t('common.actions.applyAdvanceCredit'),
      width: '850px',
      data: {
        supplierId: po.supplierId,
        purchaseOrderId: po.id,
        purchaseOrderAmount: po.outstandingAmount
      }
    });

    if (this.dialogRef) {
      this.dialogRef.onClose.subscribe((result) => {
        if (result) {
          this.messageService.add({
            severity: 'success',
            summary: this.i18n.t('common.messages.success'),
            detail: this.i18n.t('purchaseOrders.messages.advanceCreditSuccess')
          });
          this.poDeleted.emit();
        }
      });
    }
  }

  recordPayment(po: PurchaseOrderResponse): void {
    this.router.navigate(['/procurement/supplier-payments/new'], {
      queryParams: { supplierId: po.supplierId, purchaseOrderId: po.id }
    });
  }
}
