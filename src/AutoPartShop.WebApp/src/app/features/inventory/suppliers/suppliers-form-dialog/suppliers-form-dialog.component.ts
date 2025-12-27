import { Component, EventEmitter, Input, Output, ViewChild, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { InputNumberModule } from 'primeng/inputnumber';
import { ButtonModule } from 'primeng/button';
import { AutoCompleteModule } from 'primeng/autocomplete';
import { MessageService } from 'primeng/api';
import { SupplierService, SupplierResponse, CreateSupplierRequest, UpdateSupplierRequest } from '../../services/supplier.service';
import { CodeGenerationService } from '@/shared/services/CodeGenerationService';

@Component({
  selector: 'app-suppliers-form-dialog',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    DialogModule,
    InputTextModule,
    InputNumberModule,
    AutoCompleteModule,
    ButtonModule
  ],
  templateUrl: './suppliers-form-dialog.component.html',
  styleUrls: ['./suppliers-form-dialog.component.css']
})
export class SuppliersFormDialogComponent implements OnInit {
  @Input() visible = false;
  @Input() supplier: SupplierResponse | null = null;

  @Output() visibleChange = new EventEmitter<boolean>();
  @Output() submitted = new EventEmitter<SupplierResponse>();

  @ViewChild('dialog') dialog: any;

  form: FormGroup;
  isEditing = false;
  isSubmitting = false;

  paymentTermsOptions = [
    { label: 'Net 15', value: 'NET15' },
    { label: 'Net 30', value: 'NET30' },
    { label: 'Net 45', value: 'NET45' },
    { label: 'Net 60', value: 'NET60' },
    { label: 'COD (Cash on Delivery)', value: 'COD' },
    { label: 'Prepaid', value: 'PREPAID' }
  ];

  countryOptions = [
    { label: 'India', value: 'India' },
    { label: 'United States', value: 'USA' },
    { label: 'United Kingdom', value: 'UK' },
    { label: 'Canada', value: 'Canada' },
    { label: 'Australia', value: 'Australia' },
    { label: 'Germany', value: 'Germany' },
    { label: 'France', value: 'France' },
    { label: 'Japan', value: 'Japan' },
    { label: 'China', value: 'China' },
    { label: 'Other', value: 'Other' }
  ];

  filteredCountries: any[] = [];
  filteredPaymentTerms: any[] = [];

  private readonly supplierService = inject(SupplierService);
  private readonly messageService = inject(MessageService);
  private readonly fb = inject(FormBuilder);
  private readonly codeGenerationService = inject(CodeGenerationService);

  generatingCode = false;

  constructor() {
    this.form = this.createForm();
  }

  ngOnInit(): void {
    // Form is initialized in constructor
  }

  /**
   * Create form group with validators
   */
  private createForm(): FormGroup {
    return this.fb.group({
      name: ['', [Validators.required, Validators.minLength(2)]],
      code: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(20)]],
      contactPerson: ['', [Validators.required, Validators.minLength(2)]],
      email: ['', [Validators.required, Validators.email]],
      phone: ['', [Validators.required, Validators.pattern(/^[0-9\s\-\+\(\)]{7,}$/)]],
      address: ['', [Validators.required, Validators.minLength(5)]],
      city: ['', [Validators.required, Validators.minLength(2)]],
      state: ['', [Validators.required, Validators.minLength(2)]],
      country: ['India', Validators.required],
      postalCode: ['', [Validators.required, Validators.pattern(/^[0-9\-\s]{3,}$/)]],
      paymentTerms: ['NET30'],
      creditLimit: [0, [Validators.required, Validators.min(0)]]
    });
  }

  /**
   * Handle dialog show event
   */
  onDialogShow(): void {
    if (this.supplier) {
      this.isEditing = true;
      this.form.patchValue({
        name: this.supplier.name,
        code: this.supplier.code,
        contactPerson: this.supplier.contactPerson,
        email: this.supplier.email,
        phone: this.supplier.phone,
        address: this.supplier.address,
        city: this.supplier.city,
        state: this.supplier.state,
        country: this.supplier.country,
        postalCode: this.supplier.postalCode,
        paymentTerms: this.supplier.paymentTerms,
        creditLimit: this.supplier.creditLimit
      });
    } else {
      this.isEditing = false;
      this.form.reset({
        country: 'India',
        paymentTerms: 'NET30',
        creditLimit: 0
      });
      this.generateSupplierCode();
    }
  }

  /**
   * Generate supplier code automatically
   */
  private generateSupplierCode(): void {
    this.generatingCode = true;
    this.codeGenerationService.generateSupplierCode().subscribe({
      next: (code) => {
        this.form.patchValue({ code });
        this.generatingCode = false;
      },
      error: (error) => {
        console.error('Error generating supplier code:', error);
        this.messageService.add({
          severity: 'warn',
          summary: 'Warning',
          detail: 'Failed to generate supplier code. Please enter manually.'
        });
        this.generatingCode = false;
      }
    });
  }

  /**
   * Handle form submission
   */
  onSubmit(): void {
    if (this.form.invalid) {
      this.markFormGroupTouched(this.form);
      return;
    }

    this.isSubmitting = true;

    if (this.isEditing && this.supplier) {
      this.updateSupplier();
    } else {
      this.createSupplier();
    }
  }

  /**
   * Extract value from autocomplete field (handles both string and object values)
   */
  private extractAutoCompleteValue(value: any, valueKey: string = 'value'): string {
    if (!value) return '';
    if (typeof value === 'string') return value;
    return value[valueKey] || '';
  }

  /**
   * Create new supplier
   */
  private createSupplier(): void {
    const formValue = this.form.value;
    const request: CreateSupplierRequest = {
      ...formValue,
      country: this.extractAutoCompleteValue(formValue.country, 'value'),
      paymentTerms: this.extractAutoCompleteValue(formValue.paymentTerms, 'value')
    };

    this.supplierService.createSupplier(request).subscribe({
      next: (supplier) => {
        this.messageService.add({
          severity: 'success',
          summary: 'Success',
          detail: `Supplier '${supplier.name}' created successfully`
        });
        this.submitted.emit(supplier);
        this.closeDialog();
        this.isSubmitting = false;
      },
      error: (error) => {
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: error?.error?.message || 'Failed to create supplier'
        });
        console.error('Error creating supplier:', error);
        this.isSubmitting = false;
      }
    });
  }

  /**
   * Update existing supplier
   */
  private updateSupplier(): void {
    if (!this.supplier) return;

    const formValue = this.form.value;
    const request: UpdateSupplierRequest = {
      id: this.supplier.id,
      ...formValue,
      country: this.extractAutoCompleteValue(formValue.country, 'value'),
      paymentTerms: this.extractAutoCompleteValue(formValue.paymentTerms, 'value'),
      isActive: this.supplier.isActive
    };

    this.supplierService.updateSupplier(this.supplier.id, request).subscribe({
      next: (supplier) => {
        this.messageService.add({
          severity: 'success',
          summary: 'Success',
          detail: `Supplier '${supplier.name}' updated successfully`
        });
        this.submitted.emit(supplier);
        this.closeDialog();
        this.isSubmitting = false;
      },
      error: (error) => {
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: error?.error?.message || 'Failed to update supplier'
        });
        console.error('Error updating supplier:', error);
        this.isSubmitting = false;
      }
    });
  }

  /**
   * Close dialog
   */
  closeDialog(): void {
    this.visibleChange.emit(false);
  }

  /**
   * Mark all form fields as touched to show validation errors
   */
  private markFormGroupTouched(formGroup: FormGroup): void {
    Object.keys(formGroup.controls).forEach(key => {
      const control = formGroup.get(key);
      control?.markAsTouched();
    });
  }

  /**
   * Get form field error message
   */
  getErrorMessage(fieldName: string): string {
    const control = this.form.get(fieldName);

    if (!control || !control.errors || !control.touched) {
      return '';
    }

    const errors = control.errors;

    if (errors['required']) {
      return `${this.formatFieldName(fieldName)} is required`;
    }
    if (errors['email']) {
      return 'Please enter a valid email address';
    }
    if (errors['minlength']) {
      return `${this.formatFieldName(fieldName)} must be at least ${errors['minlength'].requiredLength} characters`;
    }
    if (errors['maxlength']) {
      return `${this.formatFieldName(fieldName)} cannot exceed ${errors['maxlength'].requiredLength} characters`;
    }
    if (errors['pattern']) {
      if (fieldName === 'phone') {
        return 'Please enter a valid phone number';
      }
      if (fieldName === 'postalCode') {
        return 'Please enter a valid postal code';
      }
      return `${this.formatFieldName(fieldName)} format is invalid`;
    }
    if (errors['min']) {
      return `${this.formatFieldName(fieldName)} must be at least ${errors['min'].min}`;
    }

    return 'Invalid input';
  }

  /**
   * Format field name for display
   */
  private formatFieldName(fieldName: string): string {
    return fieldName
      .replace(/([A-Z])/g, ' $1')
      .replace(/^./, (str) => str.toUpperCase())
      .trim();
  }

  /**
   * Check if field has error
   */
  hasError(fieldName: string): boolean {
    const control = this.form.get(fieldName);
    return !!(control && control.invalid && control.touched);
  }

  /**
   * Get dialog title
   */
  getDialogTitle(): string {
    return this.isEditing ? 'Edit Supplier' : 'New Supplier';
  }

  /**
   * Get submit button label
   */
  getSubmitButtonLabel(): string {
    return this.isEditing ? 'Update' : 'Create';
  }

  /**
   * Filter countries based on search input
   */
  onCountryFilter(event: any): void {
    const query = event.query.toLowerCase();
    this.filteredCountries = this.countryOptions.filter(country =>
      country.label.toLowerCase().includes(query) || country.value.toLowerCase().includes(query)
    );
  }

  /**
   * Filter payment terms based on search input
   */
  onPaymentTermsFilter(event: any): void {
    const query = event.query.toLowerCase();
    this.filteredPaymentTerms = this.paymentTermsOptions.filter(term =>
      term.label.toLowerCase().includes(query) || term.value.toLowerCase().includes(query)
    );
  }
}
