import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { CardModule } from 'primeng/card';
import { ToastModule } from 'primeng/toast';
import { SelectModule } from 'primeng/select';
import { CheckboxModule } from 'primeng/checkbox';
import { TextareaModule } from 'primeng/textarea';
import { MessageService } from 'primeng/api';
import {
  SupplierPaymentAccountService,
  CreateSupplierPaymentAccountRequest,
  UpdateSupplierPaymentAccountRequest
} from '../../services/supplier-payment-account.service';
import { SupplierService } from '../../services/supplier.service';
import { ACCOUNT_TYPES, MOBILE_PROVIDERS, PaymentMethodOption } from '../../../../shared/constants/payment-methods.constants';

@Component({
  selector: 'app-supplier-payment-account-form',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    ButtonModule,
    InputTextModule,
    CardModule,
    ToastModule,
    SelectModule,
    CheckboxModule,
    TextareaModule
  ],
  providers: [MessageService],
  templateUrl: './supplier-payment-account-form.component.html',
  styleUrls: ['./supplier-payment-account-form.component.css']
})
export class SupplierPaymentAccountFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly messageService = inject(MessageService);
  private readonly service = inject(SupplierPaymentAccountService);
  private readonly supplierService = inject(SupplierService);

  form: FormGroup;
  loading = false;
  isEditing = false;
  accountId: string | null = null;
  supplierId: string | null = null;
  supplierName: string = '';
  selectedAccountType: string = '';

  // Use shared constants from centralized configuration
  accountTypes: PaymentMethodOption[] = ACCOUNT_TYPES;
  mobileProviders = MOBILE_PROVIDERS;

  // Field visibility based on account type
  get showBankFields(): boolean {
    return this.selectedAccountType === 'BANK_TRANSFER';
  }

  get showMobileBankingFields(): boolean {
    return this.selectedAccountType === 'MOBILE_BANKING';
  }

  get showNotesField(): boolean {
    return !!this.selectedAccountType;
  }

  constructor() {
    this.form = this.fb.group({
      accountType: ['', Validators.required],
      accountName: ['', Validators.required],
      isDefault: [false],
      // Bank Transfer fields
      bankName: [''],
      bankAccountNumber: [''],
      bankBranchName: [''],
      bankBranchCode: [''],
      beneficiaryName: [''],
      bankIBAN: [''],
      bankSWIFT: [''],
      // Mobile Banking fields
      mobileNumber: [''],
      mobileAccountHolderName: [''],
      mobileProvider: [''],
      // Common
      notes: [''],
      isActive: [true]
    });
  }

  ngOnInit(): void {
    // Subscribe to account type changes
    this.form.get('accountType')?.valueChanges.subscribe(value => {
      this.selectedAccountType = value || '';
      this.updateValidators();
    });

    this.route.queryParams.subscribe(params => {
      this.supplierId = params['supplierId'];
      this.accountId = params['id'];

      if (this.supplierId) {
        this.loadSupplierInfo();
      }

      if (this.accountId) {
        this.isEditing = true;
        this.loadAccount();
      }
    });
  }

  private loadSupplierInfo(): void {
    if (!this.supplierId) return;
    this.supplierService.getSupplierById(this.supplierId).subscribe({
      next: (supplier) => {
        this.supplierName = supplier.name;
      },
      error: () => {
        this.supplierName = 'Unknown Supplier';
      }
    });
  }

  private loadAccount(): void {
    if (!this.accountId) return;
    this.loading = true;
    this.service.getById(this.accountId).subscribe({
      next: (account) => {
        // Set account type first for field visibility
        this.selectedAccountType = account.accountType || '';

        this.form.patchValue({
          accountType: account.accountType,
          accountName: account.accountName,
          isDefault: account.isDefault,
          bankName: account.bankName,
          bankAccountNumber: account.bankAccountNumber,
          bankBranchName: account.bankBranchName,
          bankBranchCode: account.bankBranchCode,
          beneficiaryName: account.beneficiaryName,
          bankIBAN: account.bankIBAN,
          bankSWIFT: account.bankSWIFT,
          mobileNumber: account.mobileNumber,
          mobileAccountHolderName: account.mobileAccountHolderName,
          mobileProvider: account.mobileProvider,
          notes: account.notes,
          isActive: account.isActive
        });

        // Disable account type change in edit mode
        this.form.get('accountType')?.disable();

        this.loading = false;
      },
      error: () => {
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: 'Failed to load payment account'
        });
        this.loading = false;
      }
    });
  }

  private updateValidators(): void {
    // Reset validators
    this.form.get('bankName')?.clearValidators();
    this.form.get('bankAccountNumber')?.clearValidators();
    this.form.get('beneficiaryName')?.clearValidators();
    this.form.get('mobileNumber')?.clearValidators();
    this.form.get('mobileProvider')?.clearValidators();

    // Set validators based on account type
    if (this.selectedAccountType === 'BANK_TRANSFER') {
      this.form.get('bankName')?.setValidators([Validators.required]);
      this.form.get('bankAccountNumber')?.setValidators([Validators.required]);
      this.form.get('beneficiaryName')?.setValidators([Validators.required]);
    } else if (this.selectedAccountType === 'MOBILE_BANKING') {
      this.form.get('mobileNumber')?.setValidators([Validators.required]);
      this.form.get('mobileProvider')?.setValidators([Validators.required]);
    }

    // Update validity
    this.form.get('bankName')?.updateValueAndValidity();
    this.form.get('bankAccountNumber')?.updateValueAndValidity();
    this.form.get('beneficiaryName')?.updateValueAndValidity();
    this.form.get('mobileNumber')?.updateValueAndValidity();
    this.form.get('mobileProvider')?.updateValueAndValidity();
  }

  onSubmit(): void {
    if (!this.form.valid) {
      this.markFormGroupTouched(this.form);
      this.messageService.add({
        severity: 'error',
        summary: 'Validation Error',
        detail: 'Please fill in all required fields'
      });
      return;
    }

    if (!this.supplierId) {
      this.messageService.add({
        severity: 'error',
        summary: 'Error',
        detail: 'Supplier ID is required'
      });
      return;
    }

    this.loading = true;
    const formValue = this.form.getRawValue();

    if (this.isEditing && this.accountId) {
      const request: UpdateSupplierPaymentAccountRequest = {
        accountName: formValue.accountName,
        isActive: formValue.isActive,
        bankName: formValue.bankName,
        bankAccountNumber: formValue.bankAccountNumber,
        bankBranchName: formValue.bankBranchName,
        bankBranchCode: formValue.bankBranchCode,
        beneficiaryName: formValue.beneficiaryName,
        bankIBAN: formValue.bankIBAN,
        bankSWIFT: formValue.bankSWIFT,
        mobileNumber: formValue.mobileNumber,
        mobileAccountHolderName: formValue.mobileAccountHolderName,
        mobileProvider: formValue.mobileProvider,
        notes: formValue.notes
      };

      this.service.update(this.accountId, request).subscribe({
        next: () => {
          this.messageService.add({
            severity: 'success',
            summary: 'Success',
            detail: 'Payment account updated successfully'
          });
          this.navigateBack();
        },
        error: (error) => {
          this.messageService.add({
            severity: 'error',
            summary: 'Error',
            detail: error?.error?.message || 'Failed to update payment account'
          });
          this.loading = false;
        }
      });
    } else {
      const request: CreateSupplierPaymentAccountRequest = {
        supplierId: this.supplierId,
        accountType: formValue.accountType,
        accountName: formValue.accountName,
        isDefault: formValue.isDefault,
        bankName: formValue.bankName,
        bankAccountNumber: formValue.bankAccountNumber,
        bankBranchName: formValue.bankBranchName,
        bankBranchCode: formValue.bankBranchCode,
        beneficiaryName: formValue.beneficiaryName,
        bankIBAN: formValue.bankIBAN,
        bankSWIFT: formValue.bankSWIFT,
        mobileNumber: formValue.mobileNumber,
        mobileAccountHolderName: formValue.mobileAccountHolderName,
        mobileProvider: formValue.mobileProvider,
        notes: formValue.notes
      };

      this.service.create(request).subscribe({
        next: () => {
          this.messageService.add({
            severity: 'success',
            summary: 'Success',
            detail: 'Payment account created successfully'
          });
          this.navigateBack();
        },
        error: (error) => {
          this.messageService.add({
            severity: 'error',
            summary: 'Error',
            detail: error?.error?.message || 'Failed to create payment account'
          });
          this.loading = false;
        }
      });
    }
  }

  onCancel(): void {
    this.navigateBack();
  }

  private navigateBack(): void {
    this.router.navigate(['/inventory/suppliers/payment-accounts'], {
      queryParams: { supplierId: this.supplierId }
    });
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
}
