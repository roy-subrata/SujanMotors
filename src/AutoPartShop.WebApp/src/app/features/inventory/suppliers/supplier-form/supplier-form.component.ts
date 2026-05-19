import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { SupplierService, SupplierResponse, CreateSupplierRequest, UpdateSupplierRequest } from '../../services/supplier.service';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { InputNumberModule } from 'primeng/inputnumber';
import { SelectModule } from 'primeng/select';
import { CardModule } from 'primeng/card';
import { ToastModule } from 'primeng/toast';
import { TextareaModule } from 'primeng/textarea';
import { TooltipModule } from 'primeng/tooltip';
import { MessageService } from 'primeng/api';
import { CodeGenerationService } from '@/shared/services/CodeGenerationService';

@Component({
  selector: 'app-supplier-form',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    ButtonModule,
    InputTextModule,
    InputNumberModule,
    SelectModule,
    CardModule,
    ToastModule,
    TextareaModule,
    TooltipModule
  ],
  providers: [MessageService],
  templateUrl: './supplier-form.component.html',
  styleUrls: ['./supplier-form.component.css']
})
export class SupplierFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly supplierService = inject(SupplierService);
  private readonly messageService = inject(MessageService);
  private readonly codeGenerationService = inject(CodeGenerationService);

  supplierForm!: FormGroup;
  loading = signal(false);
  saving = signal(false);
  error = signal<string | null>(null);
  mode = signal<'create' | 'edit' | 'view'>('create');
  supplierId = signal<string | null>(null);
  generatingCode = signal(false);

  paymentTermsOptions = [
    { label: 'Net 15', value: 'NET15' },
    { label: 'Net 30', value: 'NET30' },
    { label: 'Net 45', value: 'NET45' },
    { label: 'Net 60', value: 'NET60' },
    { label: 'COD (Cash on Delivery)', value: 'COD' },
    { label: 'Prepaid', value: 'PREPAID' }
  ];

  countries = [
    { label: 'India', value: 'India' },
    { label: 'USA', value: 'USA' },
    { label: 'Canada', value: 'Canada' },
    { label: 'UK', value: 'UK' },
    { label: 'Germany', value: 'Germany' },
    { label: 'France', value: 'France' },
    { label: 'China', value: 'China' },
    { label: 'Japan', value: 'Japan' },
    { label: 'Australia', value: 'Australia' },
    { label: 'Other', value: 'Other' }
  ];

  ngOnInit(): void {
    this.initializeForm();

    this.route.queryParams.subscribe(params => {
      const id = params['id'];
      const mode = params['mode'];

      if (id) {
        this.supplierId.set(id);
        this.mode.set(mode === 'view' ? 'view' : 'edit');
        this.loadSupplier(id);
      } else {
        // Create mode - generate supplier code automatically
        this.generateSupplierCode();
      }
    });

    if (this.mode() === 'view') {
      this.supplierForm.disable();
    }
  }

  generateSupplierCode(): void {
    this.generatingCode.set(true);
    this.codeGenerationService.generateSupplierCode().subscribe({
      next: (code) => {
        this.supplierForm.patchValue({ code: code });
        this.generatingCode.set(false);
      },
      error: (err) => {
        console.error('Error generating supplier code:', err);
        this.generatingCode.set(false);
      }
    });
  }

  private initializeForm(): void {
    this.supplierForm = this.fb.group({
      code: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(20)]],
      name: ['', [Validators.required, Validators.minLength(2)]],
      contactPerson: ['', [Validators.required, Validators.minLength(2)]],
      email: ['', [Validators.required, Validators.email]],
      phone: ['', [Validators.required, Validators.pattern(/^[0-9\s\-\+\(\)]{7,}$/)]],
      address: ['', [Validators.required, Validators.minLength(5)]],
      city: ['', [Validators.required, Validators.minLength(2)]],
      state: ['', [Validators.required, Validators.minLength(2)]],
      country: ['India', Validators.required],
      postalCode: ['', [Validators.required, Validators.pattern(/^[0-9\-\s]{3,}$/)]],
      paymentTerms: ['NET30'],
      creditLimit: [0, [Validators.min(0)]]
    });
  }

  private loadSupplier(id: string): void {
    this.loading.set(true);
    this.supplierService.getSupplierById(id).subscribe({
      next: (supplier) => {
        this.supplierForm.patchValue({
          code: supplier.code,
          name: supplier.name,
          contactPerson: supplier.contactPerson,
          email: supplier.email,
          phone: supplier.phone,
          address: supplier.address,
          city: supplier.city,
          state: supplier.state,
          country: supplier.country,
          postalCode: supplier.postalCode,
          paymentTerms: supplier.paymentTerms || 'NET30',
          creditLimit: supplier.creditLimit ?? 0
        });
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set('Failed to load supplier details');
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: 'Failed to load supplier details'
        });
        this.loading.set(false);
      }
    });
  }

  onSubmit(): void {
    if (this.supplierForm.invalid) {
      this.markFormGroupTouched(this.supplierForm);
      this.messageService.add({
        severity: 'warn',
        summary: 'Validation Error',
        detail: 'Please fill in all required fields correctly'
      });
      return;
    }

    this.saving.set(true);
    const formValue = this.supplierForm.getRawValue();

    if (this.mode() === 'create') {
      this.createSupplier(formValue);
    } else {
      this.updateSupplier(formValue);
    }
  }

  private createSupplier(formValue: any): void {
    const request: CreateSupplierRequest = {
      name: formValue.name,
      code: formValue.code,
      contactPerson: formValue.contactPerson,
      email: formValue.email,
      phone: formValue.phone,
      address: formValue.address,
      city: formValue.city,
      state: formValue.state,
      country: formValue.country,
      postalCode: formValue.postalCode,
      paymentTerms: formValue.paymentTerms,
      creditLimit: formValue.creditLimit
    };

    this.supplierService.createSupplier(request).subscribe({
      next: (supplier) => {
        this.messageService.add({
          severity: 'success',
          summary: 'Success',
          detail: 'Supplier created successfully'
        });
        this.saving.set(false);
        this.router.navigate(['/inventory/suppliers']);
      },
      error: (err) => {
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: err?.error?.message || 'Failed to create supplier'
        });
        this.saving.set(false);
      }
    });
  }

  private updateSupplier(formValue: any): void {
    const request: UpdateSupplierRequest = {
      id: this.supplierId()!,
      name: formValue.name,
      contactPerson: formValue.contactPerson,
      email: formValue.email,
      phone: formValue.phone,
      address: formValue.address,
      city: formValue.city,
      state: formValue.state,
      country: formValue.country,
      postalCode: formValue.postalCode,
      paymentTerms: formValue.paymentTerms,
      creditLimit: formValue.creditLimit,
      isActive: true
    };

    this.supplierService.updateSupplier(this.supplierId()!, request).subscribe({
      next: (supplier) => {
        this.messageService.add({
          severity: 'success',
          summary: 'Success',
          detail: 'Supplier updated successfully'
        });
        this.saving.set(false);
        this.router.navigate(['/inventory/suppliers']);
      },
      error: (err) => {
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: err?.error?.message || 'Failed to update supplier'
        });
        this.saving.set(false);
      }
    });
  }

  onCancel(): void {
    this.router.navigate(['/inventory/suppliers']);
  }

  private markFormGroupTouched(formGroup: FormGroup): void {
    Object.keys(formGroup.controls).forEach(key => {
      const control = formGroup.get(key);
      control?.markAsTouched();
      if (control instanceof FormGroup) {
        this.markFormGroupTouched(control);
      }
    });
  }

  getPageTitle(): string {
    switch (this.mode()) {
      case 'create':
        return 'Create New Supplier';
      case 'edit':
        return 'Edit Supplier';
      case 'view':
        return 'Supplier Details';
      default:
        return 'Supplier';
    }
  }

  getPageSubtitle(): string {
    switch (this.mode()) {
      case 'create':
        return 'Add a new supplier to your inventory';
      case 'edit':
        return 'Update supplier information';
      case 'view':
        return 'View supplier details';
      default:
        return '';
    }
  }
}
