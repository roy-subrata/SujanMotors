import { Component, inject, signal, output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { DialogModule } from 'primeng/dialog';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { SelectModule } from 'primeng/select';
import { CustomerService, CreateCustomerRequest } from '../services/customer.service';
import { I18nService } from '@/shared/services/i18n.service';
import { TranslatePipe } from '@/shared/pipes/translate.pipe';

@Component({
    selector: 'app-quick-customer-dialog',
    standalone: true,
    imports: [CommonModule, ReactiveFormsModule, DialogModule, ButtonModule, InputTextModule, SelectModule, TranslatePipe],
    template: `
        <p-dialog [(visible)]="visible" [modal]="true" [style]="{ width: '550px', maxWidth: '95vw' }" [draggable]="false" [resizable]="false" [closable]="true" styleClass="quick-customer-dialog">
            <ng-template pTemplate="header">
                <div class="dialog-header">
                    <div class="header-icon">
                        <i class="pi pi-user-plus"></i>
                    </div>
                    <div class="header-text">
                        <h3>{{ 'quickCustomerDialog.title' | translate }}</h3>
                        <p>{{ 'quickCustomerDialog.subtitle' | translate }}</p>
                    </div>
                </div>
            </ng-template>

            <form [formGroup]="customerForm" (ngSubmit)="onSubmit()" class="customer-form">
                <!-- Name Row -->
                <div class="form-row two-col">
                    <div class="form-field">
                        <label for="firstName"> {{ 'customers.form.firstNameLabel' | translate }} <span class="required">*</span> </label>
                        <input pInputText id="firstName" formControlName="firstName" [placeholder]="'quickCustomerDialog.firstNamePlaceholder' | translate" [class.invalid]="customerForm.get('firstName')?.invalid && customerForm.get('firstName')?.touched" />
                        <small class="error-text" *ngIf="customerForm.get('firstName')?.invalid && customerForm.get('firstName')?.touched"> {{ 'customers.form.firstNameRequired' | translate }} </small>
                    </div>

                    <div class="form-field">
                        <label for="lastName"> {{ 'customers.form.lastNameLabel' | translate }} </label>
                        <input pInputText id="lastName" formControlName="lastName" [placeholder]="'quickCustomerDialog.lastNamePlaceholder' | translate" [class.invalid]="customerForm.get('lastName')?.invalid && customerForm.get('lastName')?.touched" />
                    </div>
                </div>

                <!-- Phone & Email Row -->
                <div class="form-row two-col">
                    <div class="form-field">
                        <label for="phone">
                            <i class="pi pi-phone"></i>
                            {{ 'common.labels.phone' | translate }} <span class="required">*</span>
                        </label>
                        <input pInputText id="phone" formControlName="phone" [placeholder]="'quickCustomerDialog.phonePlaceholder' | translate" [class.invalid]="customerForm.get('phone')?.invalid && customerForm.get('phone')?.touched" />
                        <small class="error-text" *ngIf="customerForm.get('phone')?.invalid && customerForm.get('phone')?.touched"> {{ 'customers.form.phoneRequired' | translate }} </small>
                    </div>

                    <div class="form-field">
                        <label for="email">
                            <i class="pi pi-envelope"></i>
                            {{ 'common.labels.email' | translate }}
                        </label>
                        <input pInputText id="email" type="email" formControlName="email" [placeholder]="'quickCustomerDialog.emailPlaceholder' | translate" [class.invalid]="customerForm.get('email')?.invalid && customerForm.get('email')?.touched" />
                        <small class="error-text" *ngIf="customerForm.get('email')?.invalid && customerForm.get('email')?.touched"> {{ 'quickCustomerDialog.emailInvalid' | translate }} </small>
                    </div>
                </div>

                <!-- Customer Type & City Row -->
                <div class="form-row two-col">
                    <div class="form-field">
                        <label for="customerType">
                            <i class="pi pi-tag"></i>
                            {{ 'customers.form.typeLabel' | translate }}
                        </label>
                        <p-select id="customerType" formControlName="customerType" [options]="customerTypes" optionLabel="label" optionValue="value" [placeholder]="'customers.form.typePlaceholder' | translate" styleClass="w-full"></p-select>
                    </div>

                    <div class="form-field">
                        <label for="city">
                            <i class="pi pi-map-marker"></i>
                            {{ 'common.labels.city' | translate }}
                        </label>
                        <input pInputText id="city" formControlName="city" [placeholder]="'quickCustomerDialog.cityPlaceholder' | translate" />
                    </div>
                </div>

                <!-- Address Row -->
                <div class="form-row">
                    <div class="form-field">
                        <label for="address">
                            <i class="pi pi-home"></i>
                            {{ 'common.labels.address' | translate }}
                        </label>
                        <input pInputText id="address" formControlName="address" [placeholder]="'quickCustomerDialog.addressPlaceholder' | translate" />
                    </div>
                </div>

                <!-- Error Message -->
                <div *ngIf="error()" class="error-banner">
                    <i class="pi pi-exclamation-circle"></i>
                    <span>{{ error() }}</span>
                </div>
            </form>

            <ng-template pTemplate="footer">
                <div class="dialog-footer">
                    <button pButton type="button" [label]="'common.actions.cancel' | translate" icon="pi pi-times" class="p-button-text p-button-secondary" (click)="onCancel()"></button>
                    <button pButton type="button" [label]="'quickCustomerDialog.submit' | translate" icon="pi pi-check" class="p-button-success" [loading]="saving()" [disabled]="customerForm.invalid" (click)="onSubmit()"></button>
                </div>
            </ng-template>
        </p-dialog>
    `,
    styles: [
        `
            :host ::ng-deep .quick-customer-dialog {
                .p-dialog-header {
                    background: var(--accent);
                    padding: 1.25rem 1.5rem;
                    border-radius: 12px 12px 0 0;
                }

                .p-dialog-content {
                    padding: 1.5rem;
                    background: var(--surface-card);
                }

                .p-dialog-footer {
                    padding: 1rem 1.5rem;
                    background: var(--surface-ground);
                    border-top: 1px solid var(--surface-border);
                    border-radius: 0 0 12px 12px;
                }
            }

            .dialog-header {
                display: flex;
                align-items: center;
                gap: 1rem;
            }

            .header-icon {
                width: 48px;
                height: 48px;
                background: color-mix(in srgb, var(--accent-fg) 15%, transparent);
                border-radius: 12px;
                display: flex;
                align-items: center;
                justify-content: center;
                border: 1px solid color-mix(in srgb, var(--accent-fg) 20%, transparent);
            }

            .header-icon i {
                font-size: 1.5rem;
                color: var(--accent-fg);
            }

            .header-text h3 {
                margin: 0;
                font-size: 1.25rem;
                font-weight: 700;
                color: var(--accent-fg);
            }

            .header-text p {
                margin: 0.25rem 0 0 0;
                font-size: 0.85rem;
                color: color-mix(in srgb, var(--accent-fg) 70%, transparent);
            }

            .customer-form {
                display: flex;
                flex-direction: column;
                gap: 1.25rem;
            }

            .form-row {
                display: flex;
                gap: 1rem;
            }

            .form-row.two-col .form-field {
                flex: 1;
            }

            .form-field {
                display: flex;
                flex-direction: column;
                gap: 0.5rem;
                flex: 1;
            }

            .form-field label {
                font-size: 0.85rem;
                font-weight: 600;
                color: var(--text-color);
                display: flex;
                align-items: center;
                gap: 0.5rem;
            }

            .form-field label i {
                font-size: 0.8rem;
                color: var(--text-color-secondary);
            }

            .form-field label .required {
                color: var(--red);
            }

            .form-field input {
                width: 100%;
                padding: 0.75rem 1rem;
                font-size: 0.95rem;
                border: 1px solid var(--surface-border);
                border-radius: 8px;
                background: var(--surface-card);
                color: var(--text-color);
                transition: all 0.2s ease;
            }

            .form-field input:focus {
                outline: none;
                border-color: var(--accent);
                box-shadow: 0 0 0 3px var(--color-primary-light);
            }

            .form-field input.invalid {
                border-color: var(--red);
                background: var(--red-bg);
            }

            .form-field input::placeholder {
                color: var(--text-color-secondary);
            }

            :host ::ng-deep .form-field .p-select {
                width: 100%;
            }

            :host ::ng-deep .form-field .p-select .p-select-label {
                padding: 0.75rem 1rem;
            }

            .error-text {
                font-size: 0.75rem;
                color: var(--red);
            }

            .error-banner {
                display: flex;
                align-items: center;
                gap: 0.75rem;
                padding: 0.875rem 1rem;
                background: var(--red-bg);
                border: 1px solid var(--red-bg);
                border-radius: 8px;
                color: var(--red);
                font-size: 0.9rem;
            }

            .error-banner i {
                font-size: 1.1rem;
            }

            .dialog-footer {
                display: flex;
                justify-content: flex-end;
                gap: 0.75rem;
            }

            @media (max-width: 576px) {
                .form-row.two-col {
                    flex-direction: column;
                }
            }
        `
    ]
})
export class QuickCustomerDialogComponent {
    private readonly fb = inject(FormBuilder);
    private readonly customerService = inject(CustomerService);
    private readonly i18n = inject(I18nService);

    visible = signal(false);
    saving = signal(false);
    error = signal<string | null>(null);

    customerForm!: FormGroup;

    /** Getter, not a field: a field would freeze the labels in the language active at construction. */
    get customerTypes() {
        return [
            { label: this.i18n.t('quickCustomerDialog.types.individual'), value: 'INDIVIDUAL' },
            { label: this.i18n.t('quickCustomerDialog.types.business'), value: 'BUSINESS' },
            { label: this.i18n.t('quickCustomerDialog.types.dealer'), value: 'DEALER' },
            { label: this.i18n.t('quickCustomerDialog.types.wholesale'), value: 'WHOLESALE' }
        ];
    }

    // Output event when customer is created
    customerCreated = output<any>();

    constructor() {
        this.initializeForm();
    }

    initializeForm(): void {
        this.customerForm = this.fb.group({
            firstName: ['', [Validators.required, Validators.minLength(2)]],
            lastName: ['', []],
            phone: ['', [Validators.required]],
            email: ['', []],
            customerType: ['INDIVIDUAL'],
            city: [''],
            address: ['']
        });
    }

    open(): void {
        this.visible.set(true);
        this.customerForm.reset({ customerType: 'INDIVIDUAL' });
        this.error.set(null);
    }

    close(): void {
        this.visible.set(false);
    }

    onCancel(): void {
        this.close();
    }

    onSubmit(): void {
        if (this.customerForm.invalid) {
            Object.keys(this.customerForm.controls).forEach((key) => {
                this.customerForm.get(key)?.markAsTouched();
            });
            return;
        }

        this.saving.set(true);
        this.error.set(null);

        const formValue = this.customerForm.value;
        const request: CreateCustomerRequest = {
            customerCode: '', // Will be auto-generated by backend
            firstName: formValue.firstName,
            lastName: formValue.lastName,
            companyName: '',
            email: formValue.email || '',
            phone: formValue.phone,
            alternatePhone: '',
            billingAddress: formValue.address || '',
            shippingAddress: '',
            city: formValue.city || '',
            state: '',
            postalCode: '',
            country: '',
            customerType: formValue.customerType || 'INDIVIDUAL',
            primaryContactPerson: '',
            notes: ''
        };

        this.customerService.createCustomer(request).subscribe({
            next: (customer) => {
                this.customerCreated.emit(customer);
                this.saving.set(false);
                this.close();
            },
            error: (err) => {
                this.error.set(err.error?.message || this.i18n.t('quickCustomerDialog.createFailed'));
                this.saving.set(false);
                console.error('Error creating customer:', err);
            }
        });
    }
}
