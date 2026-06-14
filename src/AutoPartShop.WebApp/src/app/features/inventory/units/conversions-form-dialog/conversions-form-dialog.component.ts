import { Component, EventEmitter, inject, Input, Output, OnChanges, SimpleChanges, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { DialogModule } from 'primeng/dialog';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { InputNumberModule } from 'primeng/inputnumber';
import { MessageService } from 'primeng/api';
import { UnitService, UnitResponse } from '../../services/unit.service';
import { UnitConversionService } from '../../services/unit-conversion.service';
import { AutoCompleteModule } from 'primeng/autocomplete';

@Component({
  selector: 'app-conversions-form-dialog',
  standalone: true,
  imports: [
    CommonModule,
    DialogModule,
    ButtonModule,
    InputTextModule,
    InputNumberModule,
    AutoCompleteModule,
    ReactiveFormsModule
  ],
  templateUrl: './conversions-form-dialog.component.html',
  styleUrls: ['./conversions-form-dialog.component.css']
})
export class ConversionsFormDialogComponent implements OnInit, OnChanges {
  @Input() displayCreateDialog = false;
  @Input() displayUpdateDialog = false;
  @Input() selectedConversion: any = null;
  @Input() units: UnitResponse[] = [];

  @Output() displayCreateDialogChange = new EventEmitter<boolean>();
  @Output() displayUpdateDialogChange = new EventEmitter<boolean>();
  @Output() createSuccess = new EventEmitter<void>();
  @Output() updateSuccess = new EventEmitter<void>();

  private readonly conversionService = inject(UnitConversionService);
  private readonly unitService = inject(UnitService);
  private readonly messageService = inject(MessageService);
  private readonly formBuilder = inject(FormBuilder);

  createForm!: FormGroup;
  updateForm!: FormGroup;
  isSubmitting = false;
  unitsDropdown: any[] = [];

  // Autocomplete properties for Create dialog
  filteredFromUnits: UnitResponse[] = [];
  filteredToUnits: UnitResponse[] = [];
  selectedFromUnit: UnitResponse | null = null;
  selectedToUnit: UnitResponse | null = null;
  allUnits: UnitResponse[] = [];

  // Autocomplete properties for Update dialog
  selectedUpdateFromUnit: UnitResponse | null = null;
  selectedUpdateToUnit: UnitResponse | null = null;

  constructor() {
    this.initializeForms();
  }

  ngOnInit(): void {
    this.loadUnits();
  }

  /**
   * Load all units for autocomplete
   */
  private loadUnits(): void {
    this.unitService.getAllUnits().subscribe({
      next: (response) => {
        this.allUnits = response;
        this.filteredFromUnits = response;
        this.filteredToUnits = response;
        this.unitsDropdown = response.map((unit) => ({
          label: `${unit.name} (${unit.symbol})`,
          value: unit.id,
          icon: 'pi pi-fw pi-check'
        }));
      },
      error: (error) => {
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: 'Failed to load units'
        });
      }
    });
  }

  /**
   * Initialize forms
   */
  private initializeForms(): void {
    this.createForm = this.formBuilder.group({
      fromUnit: [null as UnitResponse | null, [Validators.required]],
      toUnit: [null as UnitResponse | null, [Validators.required]],
      conversionFactor: ['', [Validators.required, Validators.min(0.000001)]],
      description: ['', [Validators.maxLength(500)]]
    });

    this.updateForm = this.formBuilder.group({
      id: [''],
      fromUnitId: ['', [Validators.required]],
      toUnitId: ['', [Validators.required]],
      conversionFactor: ['', [Validators.required, Validators.min(0.000001)]],
      description: ['', [Validators.maxLength(500)]],
      isActive: [true]
    });
  }

  /**
   * Handle input changes
   */
  ngOnChanges(changes: SimpleChanges): void {
    if (changes['selectedConversion'] && this.selectedConversion && this.displayUpdateDialog) {
      this.updateForm.patchValue({
        id: this.selectedConversion.id,
        fromUnitId: this.selectedConversion.fromUnitId,
        toUnitId: this.selectedConversion.toUnitId,
        conversionFactor: this.selectedConversion.conversionFactor,
        description: this.selectedConversion.description,
        isActive: this.selectedConversion.isActive
      });

      // Set the selected units for display
      this.selectedUpdateFromUnit = this.allUnits.find(u => u.id === this.selectedConversion.fromUnitId) || null;
      this.selectedUpdateToUnit = this.allUnits.find(u => u.id === this.selectedConversion.toUnitId) || null;
    }
  }

  /**
   * Handle create dialog close
   */
  onCreateDialogHide(): void {
    this.displayCreateDialogChange.emit(false);
    this.createForm.reset();
  }

  /**
   * Handle update dialog close
   */
  onUpdateDialogHide(): void {
    this.displayUpdateDialogChange.emit(false);
    this.updateForm.reset();
  }

  /**
   * Validate that from and to units are different
   */
  validateUnitSelection(): boolean {
    const fromUnit = this.selectedFromUnit;
    const toUnit = this.selectedToUnit;

    if (!fromUnit || !toUnit) {
      return false;
    }

    if (fromUnit.id === toUnit.id) {
      this.messageService.add({
        severity: 'warn',
        summary: 'Validation Error',
        detail: 'From Unit and To Unit cannot be the same'
      });
      return false;
    }
    return true;
  }

  /**
   * Get unit name by ID
   */
  getUnitName(unitId: string): string {
    const unit = this.allUnits.find((u) => u.id === unitId);
    return unit ? `${unit.name} (${unit.symbol})` : unitId;
  }

  /**
   * Handle from unit autocomplete event
   */
  onFromUnitEvent(event: any): void {
    const query = event.query || '';
    this.filteredFromUnits = this.allUnits.filter((unit) =>
      unit.name.toLowerCase().includes(query.toLowerCase()) ||
      unit.symbol.toLowerCase().includes(query.toLowerCase())
    );
  }

  /**
   * Handle to unit autocomplete event
   */
  onToUnitEvent(event: any): void {
    const query = event.query || '';
    this.filteredToUnits = this.allUnits.filter((unit) =>
      unit.name.toLowerCase().includes(query.toLowerCase()) ||
      unit.symbol.toLowerCase().includes(query.toLowerCase())
    );
  }

  /**
   * Handle from unit selection
   */
  onFromUnitSelect(unit: UnitResponse): void {
    this.selectedFromUnit = unit;
    this.createForm.get('fromUnit')?.setValue(unit);
  }

  /**
   * Handle to unit selection
   */
  onToUnitSelect(unit: UnitResponse): void {
    this.selectedToUnit = unit;
    this.createForm.get('toUnit')?.setValue(unit);
  }

  /**
   * Handle from unit cleared
   */
  onFromUnitCleared(): void {
    this.selectedFromUnit = null;
    this.createForm.get('fromUnit')?.setValue(null);
  }

  /**
   * Handle to unit cleared
   */
  onToUnitCleared(): void {
    this.selectedToUnit = null;
    this.createForm.get('toUnit')?.setValue(null);
  }

  /**
   * Submit create form
   */
  onCreateSubmit(): void {
    if (this.createForm.invalid) {
      this.messageService.add({
        severity: 'warn',
        summary: 'Validation Error',
        detail: 'Please fill in all required fields correctly'
      });
      return;
    }

    if (!this.validateUnitSelection()) {
      return;
    }

    this.isSubmitting = true;
    const request = {
      fromUnitId: this.selectedFromUnit?.id ?? '',
      toUnitId: this.selectedToUnit?.id ?? '',
      conversionFactor: this.createForm.get('conversionFactor')?.value ?? '',
      description: this.createForm.get('description')?.value ?? ''
    };

    this.conversionService.createConversion(request).subscribe({
      next: (response) => {
        this.messageService.add({
          severity: 'success',
          summary: 'Success',
          detail: `Conversion created successfully: 1 ${response.fromUnitCode} = ${response.conversionFactor} ${response.toUnitCode}`
        });
        this.createSuccess.emit();
        this.onCreateDialogHide();
        this.isSubmitting = false;
      },
      error: (error) => {
        const errorMessage = error?.error?.message || 'Failed to create conversion';
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: errorMessage
        });
        this.isSubmitting = false;
      }
    });
  }

  /**
   * Submit update form
   */
  onUpdateSubmit(): void {
    if (this.updateForm.invalid) {
      this.messageService.add({
        severity: 'warn',
        summary: 'Validation Error',
        detail: 'Please fill in all required fields correctly'
      });
      return;
    }

    this.isSubmitting = true;
    const conversionId = this.updateForm.get('id')?.value;
    const request = {
      conversionFactor: this.updateForm.get('conversionFactor')?.value,
      description: this.updateForm.get('description')?.value,
      isActive: this.updateForm.get('isActive')?.value
    };

    this.conversionService.updateConversion(conversionId, request).subscribe({
      next: (response) => {
        this.messageService.add({
          severity: 'success',
          summary: 'Success',
          detail: `Conversion updated successfully: 1 ${response.fromUnitCode} = ${response.conversionFactor} ${response.toUnitCode}`
        });
        this.updateSuccess.emit();
        this.onUpdateDialogHide();
        this.isSubmitting = false;
      },
      error: (error) => {
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: error?.error?.message || 'Failed to update conversion'
        });
        this.isSubmitting = false;
      }
    });
  }

  /**
   * Get form control error message
   */
  getErrorMessage(formGroup: FormGroup, fieldName: string): string {
    const control = formGroup.get(fieldName);
    if (!control || !control.errors || !control.touched) {
      return '';
    }

    if (control.errors['required']) {
      return `${this.formatFieldName(fieldName)} is required`;
    }
    if (control.errors['min']) {
      return `${this.formatFieldName(fieldName)} must be greater than 0`;
    }
    if (control.errors['maxlength']) {
      return `${this.formatFieldName(fieldName)} cannot exceed ${control.errors['maxlength'].requiredLength} characters`;
    }
    return 'Invalid input';
  }

  /**
   * Format field name for display
   */
  private formatFieldName(fieldName: string): string {
    return fieldName
      .charAt(0)
      .toUpperCase()
      .concat(fieldName.slice(1).replace(/([A-Z])/g, ' $1'));
  }

  /**
   * Check if field has error
   */
  hasError(formGroup: FormGroup, fieldName: string): boolean {
    const control = formGroup.get(fieldName);
    return control ? control.invalid && control.touched : false;
  }

  /**
   * Check if field is valid
   */
  isFieldValid(formGroup: FormGroup, fieldName: string): boolean {
    const control = formGroup.get(fieldName);
    return control ? control.valid && control.touched : false;
  }
}
