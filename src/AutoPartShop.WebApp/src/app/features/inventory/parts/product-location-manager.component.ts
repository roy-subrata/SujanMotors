import { Component, OnInit, Input, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { TableModule } from 'primeng/table';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { TextareaModule } from 'primeng/textarea';
import { SelectModule } from 'primeng/select';
import { CheckboxModule } from 'primeng/checkbox';
import { ToastModule } from 'primeng/toast';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { TagModule } from 'primeng/tag';
import { MessageService, ConfirmationService } from 'primeng/api';
import {
  ProductLocationService,
  ProductLocationResponse,
  CreateProductLocationRequest,
  UpdateProductLocationRequest
} from '../services/product-location.service';
import { WarehouseService, WarehouseResponse } from '../services/warehouse.service';

@Component({
  selector: 'app-product-location-manager',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    ButtonModule,
    TableModule,
    DialogModule,
    InputTextModule,
    TextareaModule,
    SelectModule,
    CheckboxModule,
    ToastModule,
    ConfirmDialogModule,
    TagModule
  ],
  providers: [MessageService, ConfirmationService],
  templateUrl: './product-location-manager.component.html',
  styleUrls: ['./product-location-manager.component.css']
})
export class ProductLocationManagerComponent implements OnInit {
  @Input() partId!: string;
  @Input() partName!: string;

  private readonly fb = inject(FormBuilder);
  private readonly locationService = inject(ProductLocationService);
  private readonly warehouseService = inject(WarehouseService);
  private readonly messageService = inject(MessageService);
  private readonly confirmationService = inject(ConfirmationService);

  locations: ProductLocationResponse[] = [];
  warehouses: WarehouseResponse[] = [];
  loading = false;
  displayDialog = false;
  isEditing = false;
  isSubmitting = false;

  locationForm!: FormGroup;
  selectedLocation: ProductLocationResponse | null = null;

  ngOnInit(): void {
    this.locationForm = this.createForm();
    this.loadWarehouses();
    this.loadLocations();
  }

  private createForm(): FormGroup {
    return this.fb.group({
      warehouseId: ['', Validators.required],
      section: ['', [Validators.required, Validators.maxLength(100)]],
      shelf: ['', [Validators.required, Validators.maxLength(100)]],
      isPrimary: [false],
      notes: ['', Validators.maxLength(500)]
    });
  }

  private loadWarehouses(): void {
    this.warehouseService.getAllWarehouses().subscribe({
      next: (warehouses) => {
        this.warehouses = warehouses;
      },
      error: (error) => {
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: 'Failed to load warehouses'
        });
        console.error('Error loading warehouses:', error);
      }
    });
  }

  private loadLocations(): void {
    if (!this.partId) return;

    this.loading = true;
    this.locationService.getLocationsByPart(this.partId).subscribe({
      next: (locations) => {
        this.locations = locations;
        this.loading = false;
      },
      error: (error) => {
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: 'Failed to load locations'
        });
        console.error('Error loading locations:', error);
        this.loading = false;
      }
    });
  }

  openCreateDialog(): void {
    this.isEditing = false;
    this.selectedLocation = null;
    this.locationForm.reset({ isPrimary: false });
    this.displayDialog = true;
  }

  openEditDialog(location: ProductLocationResponse): void {
    this.isEditing = true;
    this.selectedLocation = location;
    this.locationForm.patchValue({
      warehouseId: location.warehouseId,
      section: location.section,
      shelf: location.shelf,
      isPrimary: location.isPrimary,
      notes: location.notes || ''
    });
    this.displayDialog = true;
  }

  closeDialog(): void {
    this.displayDialog = false;
    this.locationForm.reset();
    this.selectedLocation = null;
  }

  onSubmit(): void {
    if (this.locationForm.invalid) {
      Object.keys(this.locationForm.controls).forEach(key => {
        this.locationForm.get(key)?.markAsTouched();
      });
      return;
    }

    this.isSubmitting = true;

    if (this.isEditing && this.selectedLocation) {
      this.updateLocation();
    } else {
      this.createLocation();
    }
  }

  private createLocation(): void {
    const request: CreateProductLocationRequest = {
      partId: this.partId,
      warehouseId: this.locationForm.value.warehouseId,
      section: this.locationForm.value.section,
      shelf: this.locationForm.value.shelf,
      isPrimary: this.locationForm.value.isPrimary,
      notes: this.locationForm.value.notes || undefined
    };

    this.locationService.createLocation(request).subscribe({
      next: () => {
        this.messageService.add({
          severity: 'success',
          summary: 'Success',
          detail: 'Location created successfully'
        });
        this.loadLocations();
        this.closeDialog();
        this.isSubmitting = false;
      },
      error: (error) => {
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: error?.error?.message || 'Failed to create location'
        });
        console.error('Error creating location:', error);
        this.isSubmitting = false;
      }
    });
  }

  private updateLocation(): void {
    if (!this.selectedLocation) return;

    const request: UpdateProductLocationRequest = {
      section: this.locationForm.value.section,
      shelf: this.locationForm.value.shelf,
      isPrimary: this.locationForm.value.isPrimary,
      notes: this.locationForm.value.notes || undefined
    };

    this.locationService.updateLocation(this.selectedLocation.id, request).subscribe({
      next: () => {
        this.messageService.add({
          severity: 'success',
          summary: 'Success',
          detail: 'Location updated successfully'
        });
        this.loadLocations();
        this.closeDialog();
        this.isSubmitting = false;
      },
      error: (error) => {
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: error?.error?.message || 'Failed to update location'
        });
        console.error('Error updating location:', error);
        this.isSubmitting = false;
      }
    });
  }

  setPrimary(location: ProductLocationResponse): void {
    this.locationService.setPrimaryLocation(this.partId, { locationId: location.id }).subscribe({
      next: () => {
        this.messageService.add({
          severity: 'success',
          summary: 'Success',
          detail: 'Primary location updated successfully'
        });
        this.loadLocations();
      },
      error: (error) => {
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: error?.error?.message || 'Failed to set primary location'
        });
        console.error('Error setting primary location:', error);
      }
    });
  }

  deleteLocation(location: ProductLocationResponse): void {
    this.confirmationService.confirm({
      message: `Are you sure you want to delete the location "${location.fullLocation}"?`,
      header: 'Confirm Delete',
      icon: 'pi pi-exclamation-triangle',
      acceptButtonStyleClass: 'p-button-danger',
      accept: () => {
        this.locationService.deleteLocation(location.id).subscribe({
          next: () => {
            this.messageService.add({
              severity: 'success',
              summary: 'Success',
              detail: 'Location deleted successfully'
            });
            this.loadLocations();
          },
          error: (error) => {
            this.messageService.add({
              severity: 'error',
              summary: 'Error',
              detail: error?.error?.message || 'Failed to delete location'
            });
            console.error('Error deleting location:', error);
          }
        });
      }
    });
  }

  hasError(fieldName: string): boolean {
    const field = this.locationForm.get(fieldName);
    return !!(field && field.invalid && field.touched);
  }

  getSeverity(isPrimary: boolean): string {
    return isPrimary ? 'success' : 'info';
  }
}
