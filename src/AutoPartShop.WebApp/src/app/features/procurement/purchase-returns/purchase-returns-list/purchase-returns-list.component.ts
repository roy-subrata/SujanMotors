import { Component, EventEmitter, Input, Output, ViewChild, inject, OnInit } from '@angular/core';
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

  ngOnInit(): void {
    this.initializeContextMenu();
  }

  /**
   * Initialize context menu items based on return status
   */
  private initializeContextMenu(): void {
    const status = this.selectedPurchaseReturn?.status;

    this.contextMenuItems = [
      {
        label: 'View Details',
        icon: 'pi pi-eye',
        command: () => {
          if (this.selectedPurchaseReturn) {
            this.viewDetails(this.selectedPurchaseReturn);
          }
        }
      },
      {
        label: 'Edit',
        icon: 'pi pi-pencil',
        command: () => {
          if (this.selectedPurchaseReturn) {
            this.onEditClick(this.selectedPurchaseReturn);
          }
        },
        visible: status === 'PENDING'
      },
      { separator: true },
      {
        label: 'Delete',
        icon: 'pi pi-trash',
        command: () => {
          if (this.selectedPurchaseReturn) {
            this.onDeleteClick(this.selectedPurchaseReturn);
          }
        },
        visible: status === 'PENDING',
        styleClass: 'p-menuitem-danger'
      }
    ];
  }

  /**
   * Show context menu
   */
  showContextMenu(event: any, pr: PurchaseReturnResponse): void {
    this.selectedPurchaseReturn = pr;
    this.initializeContextMenu();
    if (this.contextMenu) {
      this.contextMenu.show(event);
    }
  }

  /**
   * View purchase return details
   */
  viewPurchaseReturn(pr: PurchaseReturnResponse): void {
    this.router.navigate(['/procurement/purchase-returns/view'], { queryParams: { id: pr.id } });
  }

  /**
   * Edit purchase return
   */
  editPurchaseReturn(pr: PurchaseReturnResponse): void {
    this.router.navigate(['/procurement/purchase-returns/edit'], { queryParams: { id: pr.id } });
  }

  /**
   * Handle edit click (legacy for context menu)
   */
  onEditClick(pr: PurchaseReturnResponse): void {
    this.editPurchaseReturn(pr);
  }

  /**
   * View details (legacy for context menu)
   */
  viewDetails(pr: PurchaseReturnResponse): void {
    this.viewPurchaseReturn(pr);
  }

  /**
   * Delete purchase return
   */
  deletePurchaseReturn(pr: PurchaseReturnResponse): void {
    this.confirmationService.confirm({
      message: `Are you sure you want to delete return #${pr.returnNumber}?`,
      header: 'Confirm Deletion',
      icon: 'pi pi-exclamation-triangle',
      acceptButtonStyleClass: 'p-button-danger',
      accept: () => {
        this.deletePurchaseReturnById(pr.id);
      }
    });
  }

  /**
   * Delete purchase return (legacy for context menu)
   */
  onDeleteClick(pr: PurchaseReturnResponse): void {
    this.deletePurchaseReturn(pr);
  }

  /**
   * Delete purchase return via API
   */
  private deletePurchaseReturnById(id: string): void {
    this.prService.deletePurchaseReturn(id).subscribe({
      next: () => {
        this.messageService.add({
          severity: 'success',
          summary: 'Success',
          detail: 'Purchase Return deleted successfully'
        });
        this.onReturnDeleted.emit();
      },
      error: (error) => {
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: error?.error?.message || 'Failed to delete purchase return'
        });
        console.error('Error deleting purchase return:', error);
      }
    });
  }

  /**
   * Handle pagination change
   */
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

  /**
   * Handle search
   */
  onSearchInput(event: Event): void {
    const value = (event.target as HTMLInputElement).value;
    this.searchValue = value;
    if (value && value.trim()) {
      this.onSearch.emit(value.trim());
    }
  }

  /**
   * Clear search
   */
  clearSearch(): void {
    this.searchValue = '';
    this.onSearchClear.emit();
  }

  /**
   * Get status badge severity
   */
  getStatusSeverity(status: string): string {
    switch (status?.toUpperCase()) {
      case 'PENDING':
        return 'warning';
      case 'APPROVED':
        return 'info';
      case 'RETURNED':
        return 'primary';
      case 'RECEIVED':
        return 'success';
      case 'CREDITED':
        return 'success';
      case 'REJECTED':
        return 'danger';
      default:
        return 'secondary';
    }
  }

  /**
   * Format currency - uses default currency from settings
   */
  formatCurrency(value: number): string {
    const currency = this.currencyService.selectedCurrency();
    return this.currencyService.formatCurrency(value, currency);
  }

  /**
   * Format date
   */
  formatDate(date: string): string {
    return new Date(date).toLocaleDateString('en-IN');
  }
}
