import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { ToastModule } from 'primeng/toast';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { MessageService, ConfirmationService } from 'primeng/api';
import { VehiclesListComponent } from './vehicles-list.component';
import { VehicleService, VehicleResponse } from '../services/vehicle.service';

@Component({
  selector: 'app-vehicles',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ButtonModule,
    ToastModule,
    ConfirmDialogModule,
    VehiclesListComponent
  ],
  providers: [MessageService, ConfirmationService],
  templateUrl: './vehicles.component.html',
  styleUrls: ['./vehicles.component.css']
})
export class VehiclesComponent implements OnInit {
  private readonly vehicleService = inject(VehicleService);
  private readonly messageService = inject(MessageService);
  private readonly router = inject(Router);

  vehicles: VehicleResponse[] = [];
  loading = false;
  totalRecords = 0;
  rows = 10;
  currentPage = 1;
  searchTerm = '';

  constructor() {}

  ngOnInit(): void {
    this.loadVehicles();
  }

  /**
   * Load vehicles from API
   */
  loadVehicles(pageNumber: number = 1, pageSize: number = 10, searchTerm: string = ''): void {
    if (!pageNumber || isNaN(pageNumber) || pageNumber < 1) {
      pageNumber = 1;
    }
    if (!pageSize || isNaN(pageSize) || pageSize < 1) {
      pageSize = 10;
    }

    this.loading = true;
    this.vehicleService.getAllVehicles().subscribe({
      next: (vehicles: VehicleResponse[]) => {
        // Apply search filter if provided
        let filtered = vehicles;
        if (searchTerm) {
          const lowerSearch = searchTerm.toLowerCase();
          filtered = vehicles.filter(v =>
            v.make.toLowerCase().includes(lowerSearch) ||
            v.model.toLowerCase().includes(lowerSearch) ||
            v.year.toString().includes(lowerSearch) ||
            v.engineType.toLowerCase().includes(lowerSearch)
          );
        }

        this.totalRecords = filtered.length;

        // Apply pagination
        const startIndex = (pageNumber - 1) * pageSize;
        this.vehicles = filtered.slice(startIndex, startIndex + pageSize);
        this.rows = pageSize;
        this.currentPage = pageNumber;
        this.loading = false;
      },
      error: (error: any) => {
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: 'Failed to load vehicles'
        });
        console.error('Error loading vehicles:', error);
        this.loading = false;
      }
    });
  }

  /**
   * Handle create button click
   */
  onCreateClick(): void {
    this.router.navigate(['/inventory/vehicles/create']);
  }

  /**
   * Handle search
   */
  onSearch(): void {
    this.loadVehicles(1, this.rows, this.searchTerm);
  }

  /**
   * Handle search clear
   */
  onSearchClear(): void {
    this.searchTerm = '';
    this.loadVehicles(1, this.rows);
  }

  refreshData(): void {
    this.loadVehicles(this.currentPage, this.rows, this.searchTerm);
  }

  createVehicle(): void {
    this.onCreateClick();
  }

  clearFilters(): void {
    this.onSearchClear();
  }

  hasActiveFilters(): boolean {
    return !!this.searchTerm;
  }

  /**
   * Handle view click
   */
  onViewClick(vehicle: VehicleResponse): void {
    // Navigate to view details page
  }

  /**
   * Handle edit click
   */
  onEditClick(vehicle: VehicleResponse): void {
    // Navigate to edit page
  }

  /**
   * Handle page change
   */
  onPageChange(event: { page: number; rows: number }): void {
    this.loadVehicles(event.page, event.rows, this.searchTerm);
  }

  /**
   * Handle vehicle deleted
   */
  onVehicleDeleted(): void {
    this.loadVehicles(this.currentPage, this.rows, this.searchTerm);
  }
}
