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
import { I18nService } from '@/shared/services/i18n.service';
import { TranslatePipe } from '@/shared/pipes/translate.pipe';

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
    TooltipModule,
    TranslatePipe
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
  private readonly i18n = inject(I18nService);

  supplierForm!: FormGroup;
  loading = signal(false);
  saving = signal(false);
  error = signal<string | null>(null);
  mode = signal<'create' | 'edit' | 'view'>('create');
  supplierId = signal<string | null>(null);
  generatingCode = signal(false);
  private loadedIsActive = true;

  paymentTermsOptions = [
    { label: 'Net 15', value: 'NET15' },
    { label: 'Net 30', value: 'NET30' },
    { label: 'Net 45', value: 'NET45' },
    { label: 'Net 60', value: 'NET60' },
    { label: 'COD (Cash on Delivery)', value: 'COD' },
    { label: 'Prepaid', value: 'PREPAID' }
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
        if (mode === 'view') {
          this.supplierForm.disable();
        }
      } else {
        this.generateSupplierCode();
      }
    });
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
      email: ['', [Validators.email]],
      phone: ['', [Validators.required, Validators.pattern(/^[0-9\s\-\+\(\)]{7,}$/)]],
      address: ['', [Validators.required, Validators.minLength(5)]],
      country: ['', Validators.required],
      paymentTerms: ['NET30'],
      creditLimit: [0, [Validators.min(0)]]
    });
  }

  private loadSupplier(id: string): void {
    this.loading.set(true);
    this.supplierService.getSupplierById(id).subscribe({
      next: (supplier) => {
        this.loadedIsActive = supplier.isActive;
        this.supplierForm.patchValue({
          code: supplier.code,
          name: supplier.name,
          contactPerson: supplier.contactPerson,
          email: supplier.email,
          phone: supplier.phone,
          address: supplier.address,
          country: supplier.country,
          paymentTerms: supplier.paymentTerms || 'NET30',
          creditLimit: supplier.creditLimit ?? 0
        });
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set(this.i18n.t('suppliers.messages.loadDetailsFailed'));
        this.messageService.add({
          severity: 'error',
          summary: this.i18n.t('common.messages.error'),
          detail: this.i18n.t('suppliers.messages.loadDetailsFailed')
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
        summary: this.i18n.t('common.messages.validationError'),
        detail: this.i18n.t('common.messages.fillRequiredFields')
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
      // `code` here is only a *preview* of the next code (see CodeGenerateController's
      // peek endpoints) — it hasn't reserved that number. Submitting it made every
      // supplier creation after the first race the same unconsumed code and 409/500.
      // Omit it so the backend always takes its atomic generate-on-create path.
      code: undefined,
      contactPerson: formValue.contactPerson,
      email: formValue.email,
      phone: formValue.phone,
      address: formValue.address,
      country: formValue.country,
      paymentTerms: formValue.paymentTerms,
      creditLimit: formValue.creditLimit
    };

    this.supplierService.createSupplier(request).subscribe({
      next: (supplier) => {
        this.messageService.add({
          severity: 'success',
          summary: this.i18n.t('common.messages.success'),
          detail: this.i18n.t('suppliers.messages.createSuccess')
        });
        this.saving.set(false);
        this.router.navigate(['/inventory/suppliers']);
      },
      error: (err) => {
        this.messageService.add({
          severity: 'error',
          summary: this.i18n.t('common.messages.error'),
          detail: err?.error?.message || this.i18n.t('suppliers.messages.createFailed')
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
      country: formValue.country,
      paymentTerms: formValue.paymentTerms,
      creditLimit: formValue.creditLimit,
      isActive: this.loadedIsActive
    };

    this.supplierService.updateSupplier(this.supplierId()!, request).subscribe({
      next: (supplier) => {
        this.messageService.add({
          severity: 'success',
          summary: this.i18n.t('common.messages.success'),
          detail: this.i18n.t('suppliers.messages.updateSuccess')
        });
        this.saving.set(false);
        this.router.navigate(['/inventory/suppliers']);
      },
      error: (err) => {
        this.messageService.add({
          severity: 'error',
          summary: this.i18n.t('common.messages.error'),
          detail: err?.error?.message || this.i18n.t('suppliers.messages.updateFailed')
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
        return this.i18n.t('suppliers.createNewSupplier');
      case 'edit':
        return this.i18n.t('suppliers.editSupplier');
      case 'view':
        return this.i18n.t('suppliers.supplierDetails');
      default:
        return this.i18n.t('suppliers.supplierLabel');
    }
  }

  getPageSubtitle(): string {
    switch (this.mode()) {
      case 'create':
        return this.i18n.t('suppliers.addSupplierSubtitle');
      case 'edit':
        return this.i18n.t('suppliers.updateSupplierSubtitle');
      case 'view':
        return this.i18n.t('suppliers.viewSupplierSubtitle');
      default:
        return '';
    }
  }
}
