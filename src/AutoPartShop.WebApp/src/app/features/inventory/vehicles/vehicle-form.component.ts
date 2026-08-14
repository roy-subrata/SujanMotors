import { Component, OnInit, inject, DestroyRef } from '@angular/core';
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
import { I18nService } from '@/shared/services/i18n.service';
import { TranslatePipe } from '@/shared/pipes/translate.pipe';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

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
    ToastModule,
    TranslatePipe
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
  private readonly i18n = inject(I18nService);
  private readonly destroyRef = inject(DestroyRef);

  form: FormGroup;
  isEditMode = false;
  isViewMode = false;
  isSubmitting = false;
  vehicleId: string | null = null;
  pageTitle = '';

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
    this.updatePageTitle();
    this.i18n.translationsLoaded$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
      this.updatePageTitle();
    });

    // Check if we're in edit or view mode
    this.route.queryParams.subscribe(params => {
      if (params['id']) {
        this.vehicleId = params['id'];
        this.isEditMode = this.router.url.includes('/edit');
        this.isViewMode = this.router.url.includes('/view');
        this.updatePageTitle();

        if (this.vehicleId) {
          this.loadVehicle(this.vehicleId);
        }

        if (this.isViewMode) {
          this.form.disable();
        }
      }
    });
  }

  private updatePageTitle(): void {
    this.pageTitle = this.isViewMode
      ? this.i18n.t('vehicles.viewVehicle')
      : this.isEditMode
        ? this.i18n.t('vehicles.editVehicle')
        : this.i18n.t('vehicles.createVehicle');
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
          summary: this.i18n.t('common.messages.error'),
          detail: this.i18n.t('vehicles.messages.loadDetailsFailed')
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
        summary: this.i18n.t('common.messages.error'),
        detail: this.i18n.t('common.messages.fillRequiredFields')
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
            summary: this.i18n.t('common.messages.success'),
            detail: this.i18n.t('vehicles.messages.updateSuccess')
          });
          this.router.navigate(['/inventory/vehicles']);
        },
        error: (error: any) => {
          this.messageService.add({
            severity: 'error',
            summary: this.i18n.t('common.messages.error'),
            detail: error?.error?.message || this.i18n.t('vehicles.messages.updateFailed')
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
            summary: this.i18n.t('common.messages.success'),
            detail: this.i18n.t('vehicles.messages.createSuccess')
          });
          this.router.navigate(['/inventory/vehicles']);
        },
        error: (error: any) => {
          this.messageService.add({
            severity: 'error',
            summary: this.i18n.t('common.messages.error'),
            detail: error?.error?.message || this.i18n.t('vehicles.messages.createFailed')
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
      return this.i18n.t('common.messages.fieldRequired', { field: this.formatFieldName(fieldName) });
    }
    if (field?.hasError('minlength')) {
      const minLength = field.errors?.['minlength'].requiredLength;
      return this.i18n.t('common.messages.fieldMinLength', { field: this.formatFieldName(fieldName), min: String(minLength) });
    }
    if (field?.hasError('maxlength')) {
      const maxLength = field.errors?.['maxlength'].requiredLength;
      return this.i18n.t('common.messages.fieldMaxLength', { field: this.formatFieldName(fieldName), max: String(maxLength) });
    }
    if (field?.hasError('min')) {
      const min = field.errors?.['min'].min;
      return this.i18n.t('common.messages.fieldMinValue', { field: this.formatFieldName(fieldName), min: String(min) });
    }
    if (field?.hasError('max')) {
      const max = field.errors?.['max'].max;
      return this.i18n.t('common.messages.fieldMaxValue', { field: this.formatFieldName(fieldName), max: String(max) });
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
