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
import { VehicleService, VehicleResponse } from '../services/vehicle.service';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { I18nService } from '@/shared/services/i18n.service';
import { DataPaginationComponent } from '@/shared/components/data-pagination/data-pagination.component';

@Component({
  selector: 'app-vehicles-list',
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
  templateUrl: './vehicles-list.component.html',
  styleUrls: ['./vehicles-list.component.css']
})
export class VehiclesListComponent implements OnInit {
  @ViewChild('contextMenu') contextMenu: ContextMenu | undefined;

  @Input() vehicles: VehicleResponse[] = [];
  @Input() loading = false;
  @Input() totalRecords = 0;
  @Input() rows = 10;
  @Input() currentPage = 1;

  @Output() viewClick = new EventEmitter<VehicleResponse>();
  @Output() editClick = new EventEmitter<VehicleResponse>();
  @Output() pageChange = new EventEmitter<{ page: number; rows: number }>();
  @Output() vehicleDeleted = new EventEmitter<void>();

  contextMenuItems: MenuItem[] = [];
  selectedVehicle: VehicleResponse | null = null;
  pageSizeOptions = [10, 20, 50];

  private readonly vehicleService = inject(VehicleService);
  private readonly confirmationService = inject(ConfirmationService);
  private readonly messageService = inject(MessageService);
  private readonly router = inject(Router);
  private readonly i18n = inject(I18nService);
  private readonly destroyRef = inject(DestroyRef);

  ngOnInit(): void {
    this.rebuildContextMenu();
    this.i18n.translationsLoaded$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
      if (this.selectedVehicle) this.rebuildContextMenu();
    });
  }

  private rebuildContextMenu(): void {
    const v = this.selectedVehicle;
    this.contextMenuItems = [
      {
        label: this.i18n.t('common.actions.view'),
        icon: 'pi pi-eye',
        command: () => { if (v) this.onViewClick(v); }
      },
      {
        label: this.i18n.t('common.actions.edit'),
        icon: 'pi pi-pencil',
        command: () => { if (v) this.onEditClick(v); }
      },
      {
        label: this.i18n.t('common.actions.manageCompatibility'),
        icon: 'pi pi-sitemap',
        command: () => { if (v) this.onManageCompatibility(v); }
      },
      { separator: true },
      {
        label: v?.isActive ? this.i18n.t('common.actions.deactivate') : this.i18n.t('common.actions.activate'),
        icon: v?.isActive ? 'pi pi-times-circle' : 'pi pi-check-circle',
        command: () => { if (v) this.toggleActive(v); }
      },
      { separator: true },
      {
        label: this.i18n.t('common.actions.delete'),
        icon: 'pi pi-trash',
        command: () => { if (v) this.onDeleteClick(v); },
        styleClass: 'p-menuitem-danger'
      }
    ];
  }

  showContextMenu(event: MouseEvent, vehicle: VehicleResponse): void {
    this.selectedVehicle = vehicle;
    this.rebuildContextMenu();
    if (this.contextMenu) {
      this.contextMenu.show(event);
    }
  }

  onViewClick(vehicle: VehicleResponse): void {
    this.viewClick.emit(vehicle);
    this.router.navigate(['/inventory/vehicles/view'], { queryParams: { id: vehicle.id } });
  }

  onEditClick(vehicle: VehicleResponse): void {
    this.editClick.emit(vehicle);
    this.router.navigate(['/inventory/vehicles/edit'], { queryParams: { id: vehicle.id } });
  }

  onManageCompatibility(vehicle: VehicleResponse): void {
    this.router.navigate(['/inventory/vehicles/compatibility'], { queryParams: { vehicleId: vehicle.id } });
  }

  toggleActive(vehicle: VehicleResponse): void {
    const isDeactivating = vehicle.isActive;
    const confirmKey = isDeactivating ? 'vehicles.messages.deactivateConfirm' : 'vehicles.messages.activateConfirm';
    const header = isDeactivating ? this.i18n.t('common.actions.deactivate') : this.i18n.t('common.actions.activate');

    this.confirmationService.confirm({
      message: this.i18n.t(confirmKey),
      header,
      icon: 'pi pi-exclamation-triangle',
      accept: () => {
        const request$ = vehicle.isActive
          ? this.vehicleService.deactivateVehicle(vehicle.id)
          : this.vehicleService.activateVehicle(vehicle.id);

        request$.subscribe({
          next: () => {
            const detailKey = isDeactivating ? 'vehicles.messages.deactivateSuccess' : 'vehicles.messages.activateSuccess';
            this.messageService.add({
              severity: 'success',
              summary: this.i18n.t('common.messages.success'),
              detail: this.i18n.t(detailKey)
            });
            this.vehicleDeleted.emit();
          },
          error: (error: any) => {
            const detailKey = isDeactivating ? 'vehicles.messages.deactivateFailed' : 'vehicles.messages.activateFailed';
            this.messageService.add({
              severity: 'error',
              summary: this.i18n.t('common.messages.error'),
              detail: error?.error?.message || this.i18n.t(detailKey)
            });
          }
        });
      }
    });
  }

  onDeleteClick(vehicle: VehicleResponse): void {
    const name = `${vehicle.make} ${vehicle.model} ${vehicle.year}`;
    this.confirmationService.confirm({
      message: this.i18n.t('vehicles.messages.deleteConfirm', { name }),
      header: this.i18n.t('common.messages.confirmDeletion'),
      icon: 'pi pi-exclamation-triangle',
      acceptButtonStyleClass: 'p-button-danger',
      accept: () => {
        this.deleteVehicle(vehicle.id);
      }
    });
  }

  private deleteVehicle(id: string): void {
    this.vehicleService.deleteVehicle(id).subscribe({
      next: () => {
        this.messageService.add({
          severity: 'success',
          summary: this.i18n.t('common.messages.success'),
          detail: this.i18n.t('vehicles.messages.deleteSuccess')
        });
        this.vehicleDeleted.emit();
      },
      error: (error: any) => {
        this.messageService.add({
          severity: 'error',
          summary: this.i18n.t('common.messages.error'),
          detail: error?.error?.message || this.i18n.t('vehicles.messages.deleteFailed')
        });
        console.error('Error deleting vehicle:', error);
      }
    });
  }

  onPageChange(event: any): void {
    if (!event || typeof event.first !== 'number' || typeof event.rows !== 'number') return;
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

  getStatusSeverity(isActive: boolean): string {
    return isActive ? 'success' : 'danger';
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
