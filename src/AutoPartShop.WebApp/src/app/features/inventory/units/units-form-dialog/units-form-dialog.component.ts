import { Component, EventEmitter, inject, Input, Output, OnChanges, SimpleChanges, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { DialogModule } from 'primeng/dialog';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { MessageService } from 'primeng/api';
import { UnitService, UnitResponse } from '../../services/unit.service';
import { CodeGenerationService } from '@/shared/services/CodeGenerationService';

@Component({
  selector: 'app-units-form-dialog',
  standalone: true,
  imports: [CommonModule, DialogModule, ButtonModule, InputTextModule, ReactiveFormsModule],
  templateUrl: './units-form-dialog.component.html',
  styleUrls: ['./units-form-dialog.component.css']
})
export class UnitsFormDialogComponent implements OnChanges {
  @Input() displayCreateDialog = false;
  @Input() displayUpdateDialog = false;
  @Input() selectedUnit: UnitResponse | null = null;

  @Output() displayCreateDialogChange = new EventEmitter<boolean>();
  @Output() displayUpdateDialogChange = new EventEmitter<boolean>();
  @Output() createSuccess = new EventEmitter<void>();
  @Output() updateSuccess = new EventEmitter<void>();

  private readonly unitService = inject(UnitService);
  private readonly messageService = inject(MessageService);
  private readonly formBuilder = inject(FormBuilder);
  private readonly codeGenerationService = inject(CodeGenerationService);

  createForm!: FormGroup;
  updateForm!: FormGroup;
  isSubmitting = false;
  generatingCode = false;

  constructor() {
    this.initializeForms();
  }

  /**
   * Initialize forms
   */
  private initializeForms(): void {
    this.createForm = this.formBuilder.group({
      name: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(100)]],
      code: ['', [Validators.required, Validators.minLength(1), Validators.maxLength(50)]],
      symbol: ['', [Validators.required, Validators.minLength(1), Validators.maxLength(20)]],
      description: ['', [Validators.maxLength(500)]]
    });

    this.updateForm = this.formBuilder.group({
      id: [''],
      name: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(100)]],
      code: ['', [Validators.required, Validators.minLength(1), Validators.maxLength(50)]],
      symbol: ['', [Validators.required, Validators.minLength(1), Validators.maxLength(20)]],
      description: ['', [Validators.maxLength(500)]],
      isActive: [true],
      displayOrder: [0]
    });
  }

  /**
   * Handle input changes
   */
  ngOnChanges(changes: SimpleChanges): void {
    if (changes['selectedUnit'] && this.selectedUnit && this.displayUpdateDialog) {
      this.updateForm.patchValue({
        id: this.selectedUnit.id,
        name: this.selectedUnit.name,
        code: this.selectedUnit.code,
        symbol: this.selectedUnit.symbol,
        description: this.selectedUnit.description,
        isActive: this.selectedUnit.isActive,
        displayOrder: this.selectedUnit.displayOrder
      });
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
   * Handle create dialog show
   */
  onCreateDialogShow(): void {
    this.createForm.reset();
    this.generateUnitCode();
  }

  /**
   * Generate unit code automatically
   */
  private generateUnitCode(): void {
    this.generatingCode = true;
    this.codeGenerationService.generateUnitCode().subscribe({
      next: (code) => {
        this.createForm.patchValue({ code });
        this.generatingCode = false;
      },
      error: (error) => {
        console.error('Error generating unit code:', error);
        this.messageService.add({
          severity: 'warn',
          summary: 'Warning',
          detail: 'Failed to generate unit code. Please enter manually.'
        });
        this.generatingCode = false;
      }
    });
  }

  /**
   * Handle update dialog close
   */
  onUpdateDialogHide(): void {
    this.displayUpdateDialogChange.emit(false);
    this.updateForm.reset();
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

    this.isSubmitting = true;
    const request = this.createForm.value;

    this.unitService.createUnit(request).subscribe({
      next: (response) => {
        this.messageService.add({
          severity: 'success',
          summary: 'Success',
          detail: `Unit "${response.name}" created successfully`
        });
        this.createSuccess.emit();
        this.onCreateDialogHide();
        this.isSubmitting = false;
      },
      error: (error) => {
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: error?.error?.message || 'Failed to create unit'
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
    const unitId = this.updateForm.get('id')?.value;
    const request = {
      name: this.updateForm.get('name')?.value,
      code: this.updateForm.get('code')?.value,
      symbol: this.updateForm.get('symbol')?.value,
      description: this.updateForm.get('description')?.value,
      isActive: this.updateForm.get('isActive')?.value,
      displayOrder: this.updateForm.get('displayOrder')?.value
    };

    this.unitService.updateUnit(unitId, request).subscribe({
      next: (response) => {
        this.messageService.add({
          severity: 'success',
          summary: 'Success',
          detail: `Unit "${response.name}" updated successfully`
        });
        this.updateSuccess.emit();
        this.onUpdateDialogHide();
        this.isSubmitting = false;
      },
      error: (error) => {
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: error?.error?.message || 'Failed to update unit'
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
    if (control.errors['minlength']) {
      return `${this.formatFieldName(fieldName)} must be at least ${control.errors['minlength'].requiredLength} characters`;
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
    return fieldName.charAt(0).toUpperCase() + fieldName.slice(1).replace(/([A-Z])/g, ' $1');
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
