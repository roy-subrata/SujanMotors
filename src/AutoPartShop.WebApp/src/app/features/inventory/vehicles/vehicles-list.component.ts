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
import { VehicleService, VehicleResponse } from '../services/vehicle.service';

@Component({
  selector: 'app-vehicles-list',
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

  private readonly vehicleService = inject(VehicleService);
  private readonly confirmationService = inject(ConfirmationService);
  private readonly messageService = inject(MessageService);
  private readonly router = inject(Router);

  ngOnInit(): void {
    this.initializeContextMenu();
  }

  private initializeContextMenu(): void {
    this.contextMenuItems = [
      {
        label: 'View',
        icon: 'pi pi-eye',
        command: () => {
          if (this.selectedVehicle) {
            this.onViewClick(this.selectedVehicle);
          }
        }
      },
      {
        label: 'Edit',
        icon: 'pi pi-pencil',
        command: () => {
          if (this.selectedVehicle) {
            this.onEditClick(this.selectedVehicle);
          }
        }
      },
      {
        label: 'Manage Compatibility',
        icon: 'pi pi-sitemap',
        command: () => {
          if (this.selectedVehicle) {
            this.onManageCompatibility(this.selectedVehicle);
          }
        }
      },
      { separator: true },
      {
        label: this.selectedVehicle?.isActive ? 'Deactivate' : 'Activate',
        icon: this.selectedVehicle?.isActive ? 'pi pi-times-circle' : 'pi pi-check-circle',
        command: () => {
          if (this.selectedVehicle) {
            this.toggleActive(this.selectedVehicle);
          }
        }
      },
      { separator: true },
      {
        label: 'Delete',
        icon: 'pi pi-trash',
        command: () => {
          if (this.selectedVehicle) {
            this.onDeleteClick(this.selectedVehicle);
          }
        },
        styleClass: 'p-menuitem-danger'
      }
    ];
  }

  showContextMenu(event: MouseEvent, vehicle: VehicleResponse): void {
    this.selectedVehicle = vehicle;
    this.initializeContextMenu();
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
    const action = vehicle.isActive ? 'deactivate' : 'activate';
    this.confirmationService.confirm({
      message: `Are you sure you want to ${action} "${vehicle.make} ${vehicle.model} ${vehicle.year}"?`,
      header: `Confirm ${action.charAt(0).toUpperCase() + action.slice(1)}`,
      icon: 'pi pi-exclamation-triangle',
      accept: () => {
        const request$ = vehicle.isActive
          ? this.vehicleService.deactivateVehicle(vehicle.id)
          : this.vehicleService.activateVehicle(vehicle.id);

        request$.subscribe({
          next: () => {
            this.messageService.add({
              severity: 'success',
              summary: 'Success',
              detail: `Vehicle ${action}d successfully`
            });
            this.vehicleDeleted.emit();
          },
          error: (error: any) => {
            this.messageService.add({
              severity: 'error',
              summary: 'Error',
              detail: error?.error?.message || `Failed to ${action} vehicle`
            });
          }
        });
      }
    });
  }

  onDeleteClick(vehicle: VehicleResponse): void {
    this.confirmationService.confirm({
      message: `Are you sure you want to delete "${vehicle.make} ${vehicle.model} ${vehicle.year}"?`,
      header: 'Confirm Deletion',
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
          summary: 'Success',
          detail: 'Vehicle deleted successfully'
        });
        this.vehicleDeleted.emit();
      },
      error: (error: any) => {
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: error?.error?.message || 'Failed to delete vehicle'
        });
        console.error('Error deleting vehicle:', error);
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

  getStatusSeverity(isActive: boolean): string {
    return isActive ? 'success' : 'danger';
  }
}
