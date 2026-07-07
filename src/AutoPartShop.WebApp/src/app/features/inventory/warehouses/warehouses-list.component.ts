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
import { ConfirmationService, MessageService, MenuItem } from 'primeng/api';
import { WarehouseService, WarehouseResponse } from '../services/warehouse.service';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { I18nService } from '@/shared/services/i18n.service';
import { DataPaginationComponent } from '@/shared/components/data-pagination/data-pagination.component';

@Component({
  selector: 'app-warehouses-list',
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
    DataPaginationComponent
  ],
  providers: [ConfirmationService, MessageService],
  templateUrl: './warehouses-list.component.html',
  styleUrls: ['./warehouses-list.component.css']
})
export class WarehousesListComponent implements OnInit {
  @ViewChild('contextMenu') contextMenu: ContextMenu | undefined;

  @Input() warehouses: WarehouseResponse[] = [];
  @Input() loading = false;
  @Input() totalRecords = 0;
  @Input() rows = 10;
  @Input() currentPage = 1;

  @Output() viewClick = new EventEmitter<WarehouseResponse>();
  @Output() editClick = new EventEmitter<WarehouseResponse>();
  @Output() pageChange = new EventEmitter<{ page: number; rows: number }>();
  @Output() warehouseDeleted = new EventEmitter<void>();

  contextMenuItems: MenuItem[] = [];
  selectedWarehouse: WarehouseResponse | null = null;
  pageSizeOptions = [10, 20, 50];

  private readonly warehouseService = inject(WarehouseService);
  private readonly confirmationService = inject(ConfirmationService);
  private readonly messageService = inject(MessageService);
  private readonly router = inject(Router);
  private readonly i18n = inject(I18nService);
  private readonly destroyRef = inject(DestroyRef);

  ngOnInit(): void {
    this.rebuildContextMenu();
    this.i18n.translationsLoaded$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
      if (this.selectedWarehouse) this.rebuildContextMenu();
    });
  }

  private rebuildContextMenu(): void {
    this.contextMenuItems = [
      {
        label: this.i18n.t('common.actions.viewDetails'),
        icon: 'pi pi-eye',
        command: () => {
          if (this.selectedWarehouse) this.onViewClick(this.selectedWarehouse);
        }
      },
      {
        label: this.i18n.t('common.actions.edit'),
        icon: 'pi pi-pencil',
        command: () => {
          if (this.selectedWarehouse) this.onEditClick(this.selectedWarehouse);
        }
      },
      { separator: true },
      {
        label: this.i18n.t('common.actions.delete'),
        icon: 'pi pi-trash',
        command: () => {
          if (this.selectedWarehouse) this.onDeleteClick(this.selectedWarehouse);
        },
        styleClass: 'p-menuitem-danger'
      }
    ];
  }

  showContextMenu(event: MouseEvent, warehouse: WarehouseResponse): void {
    this.selectedWarehouse = warehouse;
    this.rebuildContextMenu();
    if (this.contextMenu) {
      this.contextMenu.show(event);
    }
  }

  onViewClick(warehouse: WarehouseResponse): void {
    this.viewClick.emit(warehouse);
    this.router.navigate(['/inventory/warehouses/view'], { queryParams: { id: warehouse.id } });
  }

  onEditClick(warehouse: WarehouseResponse): void {
    this.editClick.emit(warehouse);
    this.router.navigate(['/inventory/warehouses/edit'], { queryParams: { id: warehouse.id } });
  }

  onDeleteClick(warehouse: WarehouseResponse): void {
    this.confirmationService.confirm({
      message: this.i18n.t('warehouses.messages.deleteConfirm', { name: warehouse.name }),
      header: this.i18n.t('common.messages.confirmDeletion'),
      icon: 'pi pi-exclamation-triangle',
      acceptButtonStyleClass: 'p-button-danger',
      accept: () => {
        this.deleteWarehouse(warehouse.id);
      }
    });
  }

  private deleteWarehouse(id: string): void {
    this.warehouseService.deleteWarehouse(id).subscribe({
      next: () => {
        this.messageService.add({
          severity: 'success',
          summary: this.i18n.t('common.messages.success'),
          detail: this.i18n.t('warehouses.messages.deleteSuccess')
        });
        this.warehouseDeleted.emit();
      },
      error: (error: any) => {
        this.messageService.add({
          severity: 'error',
          summary: this.i18n.t('common.messages.error'),
          detail: error?.error?.message || this.i18n.t('warehouses.messages.deleteFailed')
        });
        console.error('Error deleting warehouse:', error);
      }
    });
  }

  onPageChange(event: any): void {
    if (!event || typeof event.first !== 'number' || typeof event.rows !== 'number') {
      return;
    }
    const pageNumber = Math.floor(event.first / event.rows) + 1;
    this.pageChange.emit({ page: pageNumber, rows: event.rows });
  }

  goToPage(page: number): void {
    this.onPageChange({ first: (page - 1) * this.rows, rows: this.rows });
  }

  onPageSizeChange(size: number): void {
    this.rows = size;
    this.onPageChange({ first: 0, rows: size });
  }

  getUtilizationPercentage(warehouse: WarehouseResponse): number {
    if (!warehouse.capacity || warehouse.capacity === 0) return 0;
    return Math.round((warehouse.currentStock / warehouse.capacity) * 100);
  }

  getUtilizationSeverity(percentage: number): string {
    if (percentage >= 90) return 'danger';
    if (percentage >= 75) return 'warning';
    if (percentage >= 50) return 'info';
    return 'success';
  }

  formatDate(date: string): string {
    return new Date(date).toLocaleDateString('en-IN');
  }

  get first(): number {
    return Math.max(0, (this.currentPage - 1) * this.rows);
  }

  get pageNumber(): number {
    if (!this.totalRecords) return 0;
    return Math.floor(this.first / this.rows) + 1;
  }

  get totalPages(): number {
    if (!this.totalRecords) return 0;
    return Math.ceil(this.totalRecords / this.rows);
  }

  get pageSize(): number { return this.rows; }
  set pageSize(value: number) { this.rows = value; }
}
