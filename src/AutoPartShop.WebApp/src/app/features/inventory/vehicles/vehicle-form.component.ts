import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, FormsModule, Validators } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { InputTextModule } from 'primeng/inputtext';
import { InputNumberModule } from 'primeng/inputnumber';
import { TextareaModule } from 'primeng/textarea';
import { AutoCompleteModule } from 'primeng/autocomplete';
import { CheckboxModule } from 'primeng/checkbox';
import { MessageService } from 'primeng/api';
import { ToastModule } from 'primeng/toast';
import { VehicleService, VehicleResponse } from '../services/vehicle.service';

@Component({
  selector: 'app-vehicle-form',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    FormsModule,
    ButtonModule,
    CardModule,
    InputTextModule,
    InputNumberModule,
    TextareaModule,
    AutoCompleteModule,
    CheckboxModule,
    ToastModule
  ],
  providers: [MessageService],
  templateUrl: './vehicle-form.component.html',
  styleUrls: ['./vehicle-form.component.css']
})
export class VehicleFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly vehicleService = inject(VehicleService);
  private readonly messageService = inject(MessageService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  form: FormGroup;
  isEditMode = false;
  isViewMode = false;
  isSubmitting = false;
  vehicleId: string | null = null;
  pageTitle = 'Create Vehicle';

  // Engine type options
  engineTypes = [
    { label: 'Petrol', value: 'Petrol' },
    { label: 'Diesel', value: 'Diesel' },
    { label: 'Hybrid', value: 'Hybrid' },
    { label: 'Electric', value: 'Electric' },
    { label: 'CNG', value: 'CNG' },
    { label: 'LPG', value: 'LPG' }
  ];

  selectedEngineType: any = null;
  filteredEngineTypes: any[] = [];

  constructor() {
    this.form = this.createForm();
  }

  ngOnInit(): void {
    // Check if we're in edit or view mode
    this.route.queryParams.subscribe(params => {
      if (params['id']) {
        this.vehicleId = params['id'];
        this.isEditMode = this.router.url.includes('/edit');
        this.isViewMode = this.router.url.includes('/view');
        this.pageTitle = this.isViewMode ? 'View Vehicle' : 'Edit Vehicle';

        if (this.vehicleId) {
          this.loadVehicle(this.vehicleId);
        }

        if (this.isViewMode) {
          this.form.disable();
        }
      }
    });
  }

  /**
   * Create form group
   */
  private createForm(): FormGroup {
    const currentYear = new Date().getFullYear();
    return this.fb.group({
      make: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(50)]],
      model: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(100)]],
      year: [currentYear, [Validators.required, Validators.min(1900), Validators.max(currentYear + 1)]],
      engineType: ['', Validators.required],
      description: ['', Validators.maxLength(500)],
      isActive: [true]
    });
  }

  /**
   * Load vehicle data for editing
   */
  private loadVehicle(id: string): void {
    this.vehicleService.getVehicleById(id).subscribe({
      next: (vehicle: VehicleResponse) => {
        // Set selected engine type for autocomplete
        this.selectedEngineType = this.engineTypes.find(e => e.value === vehicle.engineType);

        this.form.patchValue({
          make: vehicle.make,
          model: vehicle.model,
          year: vehicle.year,
          engineType: vehicle.engineType,
          description: vehicle.description,
          isActive: vehicle.isActive
        });
      },
      error: (error: any) => {
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: 'Failed to load vehicle details'
        });
        console.error('Error loading vehicle:', error);
      }
    });
  }

  /**
   * Filter engine types for autocomplete
   */
  filterEngineTypes(event: { query: string }): void {
    const filtered = this.engineTypes.filter(type =>
      type.label.toLowerCase().includes(event.query.toLowerCase())
    );
    this.filteredEngineTypes = filtered;
  }

  /**
   * Handle engine type selection
   */
  onEngineTypeSelected(event: any): void {
    const engineType = event.value as any;
    this.selectedEngineType = engineType;
    this.form.patchValue({
      engineType: engineType.value
    });
  }

  /**
   * Submit form
   */
  onSubmit(): void {
    if (!this.form.valid) {
      this.messageService.add({
        severity: 'error',
        summary: 'Error',
        detail: 'Please fill all required fields'
      });
      return;
    }

    this.isSubmitting = true;

    const vehicleData = {
      make: this.form.value.make,
      model: this.form.value.model,
      year: this.form.value.year,
      engineType: this.form.value.engineType,
      description: this.form.value.description || '',
      isActive: this.form.value.isActive
    };

    if (this.isEditMode && this.vehicleId) {
      // Update existing vehicle
      this.vehicleService.updateVehicle(this.vehicleId, vehicleData).subscribe({
        next: () => {
          this.messageService.add({
            severity: 'success',
            summary: 'Success',
            detail: `Vehicle '${vehicleData.make} ${vehicleData.model}' updated successfully`
          });
          this.router.navigate(['/inventory/vehicles']);
        },
        error: (error: any) => {
          this.messageService.add({
            severity: 'error',
            summary: 'Error',
            detail: error?.error?.message || 'Failed to update vehicle'
          });
          console.error('Error updating vehicle:', error);
          this.isSubmitting = false;
        }
      });
    } else {
      // Create new vehicle
      this.vehicleService.createVehicle(vehicleData).subscribe({
        next: () => {
          this.messageService.add({
            severity: 'success',
            summary: 'Success',
            detail: `Vehicle '${vehicleData.make} ${vehicleData.model}' created successfully`
          });
          this.router.navigate(['/inventory/vehicles']);
        },
        error: (error: any) => {
          this.messageService.add({
            severity: 'error',
            summary: 'Error',
            detail: error?.error?.message || 'Failed to create vehicle'
          });
          console.error('Error creating vehicle:', error);
          this.isSubmitting = false;
        }
      });
    }
  }

  /**
   * Go back to list
   */
  goBack(): void {
    this.router.navigate(['/inventory/vehicles']);
  }

  /**
   * Check if field has error
   */
  hasError(fieldName: string): boolean {
    const field = this.form.get(fieldName);
    return !!(field && field.invalid && field.touched);
  }

  /**
   * Get error message
   */
  getErrorMessage(fieldName: string): string {
    const field = this.form.get(fieldName);
    if (field?.hasError('required')) {
      return `${this.formatFieldName(fieldName)} is required`;
    }
    if (field?.hasError('minlength')) {
      const minLength = field.errors?.['minlength'].requiredLength;
      return `${this.formatFieldName(fieldName)} must be at least ${minLength} characters`;
    }
    if (field?.hasError('maxlength')) {
      const maxLength = field.errors?.['maxlength'].requiredLength;
      return `${this.formatFieldName(fieldName)} cannot exceed ${maxLength} characters`;
    }
    if (field?.hasError('min')) {
      const min = field.errors?.['min'].min;
      return `${this.formatFieldName(fieldName)} must be at least ${min}`;
    }
    if (field?.hasError('max')) {
      const max = field.errors?.['max'].max;
      return `${this.formatFieldName(fieldName)} cannot exceed ${max}`;
    }
    return '';
  }

  /**
   * Format field name for display
   */
  private formatFieldName(fieldName: string): string {
    return fieldName.charAt(0).toUpperCase() + fieldName.slice(1).replace(/([A-Z])/g, ' $1');
  }
}
