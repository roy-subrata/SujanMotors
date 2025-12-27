import { Component, EventEmitter, Input, Output, ViewChild, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
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

@Component({
  selector: 'app-warehouses-list',
  standalone: true,
  imports: [
    CommonModule,
    TableModule,
    ButtonModule,
    ConfirmDialogModule,
    ContextMenuModule,
    RippleModule,
    TagModule,
    TooltipModule
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

  private readonly warehouseService = inject(WarehouseService);
  private readonly confirmationService = inject(ConfirmationService);
  private readonly messageService = inject(MessageService);
  private readonly router = inject(Router);

  ngOnInit(): void {
    this.initializeContextMenu();
  }

  /**
   * Initialize context menu items
   */
  private initializeContextMenu(): void {
    this.contextMenuItems = [
      {
        label: 'View',
        icon: 'pi pi-eye',
        command: () => {
          if (this.selectedWarehouse) {
            this.onViewClick(this.selectedWarehouse);
          }
        }
      },
      {
        label: 'Edit',
        icon: 'pi pi-pencil',
        command: () => {
          if (this.selectedWarehouse) {
            this.onEditClick(this.selectedWarehouse);
          }
        }
      },
      { separator: true },
      {
        label: 'Delete',
        icon: 'pi pi-trash',
        command: () => {
          if (this.selectedWarehouse) {
            this.onDeleteClick(this.selectedWarehouse);
          }
        },
        styleClass: 'p-menuitem-danger'
      }
    ];
  }

  /**
   * Show context menu
   */
  showContextMenu(event: MouseEvent, warehouse: WarehouseResponse): void {
    this.selectedWarehouse = warehouse;
    this.initializeContextMenu();
    if (this.contextMenu) {
      this.contextMenu.show(event);
    }
  }

  /**
   * Handle view click
   */
  onViewClick(warehouse: WarehouseResponse): void {
    this.viewClick.emit(warehouse);
    this.router.navigate(['/inventory/warehouses/view'], { queryParams: { id: warehouse.id } });
  }

  /**
   * Handle edit click
   */
  onEditClick(warehouse: WarehouseResponse): void {
    this.editClick.emit(warehouse);
    this.router.navigate(['/inventory/warehouses/edit'], { queryParams: { id: warehouse.id } });
  }

  /**
   * Delete warehouse
   */
  onDeleteClick(warehouse: WarehouseResponse): void {
    this.confirmationService.confirm({
      message: `Are you sure you want to delete warehouse "${warehouse.name}"?`,
      header: 'Confirm Deletion',
      icon: 'pi pi-exclamation-triangle',
      acceptButtonStyleClass: 'p-button-danger',
      accept: () => {
        this.deleteWarehouse(warehouse.id);
      }
    });
  }

  /**
   * Delete warehouse via API
   */
  private deleteWarehouse(id: string): void {
    this.warehouseService.deleteWarehouse(id).subscribe({
      next: () => {
        this.messageService.add({
          severity: 'success',
          summary: 'Success',
          detail: 'Warehouse deleted successfully'
        });
        this.warehouseDeleted.emit();
      },
      error: (error: any) => {
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: error?.error?.message || 'Failed to delete warehouse'
        });
        console.error('Error deleting warehouse:', error);
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
   * Calculate utilization percentage
   */
  getUtilizationPercentage(warehouse: WarehouseResponse): number {
    if (!warehouse.capacity || warehouse.capacity === 0) return 0;
    return Math.round((warehouse.currentStock / warehouse.capacity) * 100);
  }

  /**
   * Get utilization severity
   */
  getUtilizationSeverity(percentage: number): string {
    if (percentage >= 90) return 'danger';
    if (percentage >= 75) return 'warning';
    if (percentage >= 50) return 'info';
    return 'success';
  }

  /**
   * Format date
   */
  formatDate(date: string): string {
    return new Date(date).toLocaleDateString('en-IN');
  }
}
