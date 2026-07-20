import { Component, inject, signal, output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { DialogModule } from 'primeng/dialog';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { SelectModule } from 'primeng/select';
import { CustomerService, CreateCustomerRequest } from '../services/customer.service';

@Component({
    selector: 'app-quick-customer-dialog',
    standalone: true,
    imports: [CommonModule, ReactiveFormsModule, DialogModule, ButtonModule, InputTextModule, SelectModule],
    template: `
        <p-dialog [(visible)]="visible" [modal]="true" [style]="{ width: '550px', maxWidth: '95vw' }" [draggable]="false" [resizable]="false" [closable]="true" styleClass="quick-customer-dialog">
            <ng-template pTemplate="header">
                <div class="dialog-header">
                    <div class="header-icon">
                        <i class="pi pi-user-plus"></i>
                    </div>
                    <div class="header-text">
                        <h3>Quick Add Customer</h3>
                        <p>Add a new customer for this sale</p>
                    </div>
                </div>
            </ng-template>

            <form [formGroup]="customerForm" (ngSubmit)="onSubmit()" class="customer-form">
                <!-- Name Row -->
                <div class="form-row two-col">
                    <div class="form-field">
                        <label for="firstName"> First Name <span class="required">*</span> </label>
                        <input pInputText id="firstName" formControlName="firstName" placeholder="Enter first name" [class.invalid]="customerForm.get('firstName')?.invalid && customerForm.get('firstName')?.touched" />
                        <small class="error-text" *ngIf="customerForm.get('firstName')?.invalid && customerForm.get('firstName')?.touched"> First name is required (min 2 characters) </small>
                    </div>

                    <div class="form-field">
                        <label for="lastName"> Last Name </label>
                        <input pInputText id="lastName" formControlName="lastName" placeholder="Enter last name" [class.invalid]="customerForm.get('lastName')?.invalid && customerForm.get('lastName')?.touched" />
                    </div>
                </div>

                <!-- Phone & Email Row -->
                <div class="form-row two-col">
                    <div class="form-field">
                        <label for="phone">
                            <i class="pi pi-phone"></i>
                            Phone <span class="required">*</span>
                        </label>
                        <input pInputText id="phone" formControlName="phone" placeholder="e.g. 9801234567" [class.invalid]="customerForm.get('phone')?.invalid && customerForm.get('phone')?.touched" />
                        <small class="error-text" *ngIf="customerForm.get('phone')?.invalid && customerForm.get('phone')?.touched"> Phone number is required </small>
                    </div>

                    <div class="form-field">
                        <label for="email">
                            <i class="pi pi-envelope"></i>
                            Email
                        </label>
                        <input pInputText id="email" type="email" formControlName="email" placeholder="email@example.com" [class.invalid]="customerForm.get('email')?.invalid && customerForm.get('email')?.touched" />
                        <small class="error-text" *ngIf="customerForm.get('email')?.invalid && customerForm.get('email')?.touched"> Invalid email format </small>
                    </div>
                </div>

                <!-- Customer Type & City Row -->
                <div class="form-row two-col">
                    <div class="form-field">
                        <label for="customerType">
                            <i class="pi pi-tag"></i>
                            Customer Type
                        </label>
                        <p-select id="customerType" formControlName="customerType" [options]="customerTypes" optionLabel="label" optionValue="value" placeholder="Select type" styleClass="w-full"></p-select>
                    </div>

                    <div class="form-field">
                        <label for="city">
                            <i class="pi pi-map-marker"></i>
                            City
                        </label>
                        <input pInputText id="city" formControlName="city" placeholder="Enter city" />
                    </div>
                </div>

                <!-- Address Row -->
                <div class="form-row">
                    <div class="form-field">
                        <label for="address">
                            <i class="pi pi-home"></i>
                            Address
                        </label>
                        <input pInputText id="address" formControlName="address" placeholder="Enter billing address (optional)" />
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
                    <button pButton type="button" label="Cancel" icon="pi pi-times" class="p-button-text p-button-secondary" (click)="onCancel()"></button>
                    <button pButton type="button" label="Add Customer" icon="pi pi-check" class="p-button-success" [loading]="saving()" [disabled]="customerForm.invalid" (click)="onSubmit()"></button>
                </div>
            </ng-template>
        </p-dialog>
    `,
    styles: [
        `
            :host ::ng-deep .quick-customer-dialog {
                .p-dialog-header {
                    background: linear-gradient(135deg, #1e3a5f 0%, #2d5a87 100%);
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
                background: rgba(255, 255, 255, 0.15);
                border-radius: 12px;
                display: flex;
                align-items: center;
                justify-content: center;
                border: 1px solid rgba(255, 255, 255, 0.2);
            }

            .header-icon i {
                font-size: 1.5rem;
                color: #ffd700;
            }

            .header-text h3 {
                margin: 0;
                font-size: 1.25rem;
                font-weight: 700;
                color: #ffffff;
            }

            .header-text p {
                margin: 0.25rem 0 0 0;
                font-size: 0.85rem;
                color: rgba(255, 255, 255, 0.7);
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

    visible = signal(false);
    saving = signal(false);
    error = signal<string | null>(null);

    customerForm!: FormGroup;

    customerTypes = [
        { label: 'Individual', value: 'INDIVIDUAL' },
        { label: 'Business', value: 'BUSINESS' },
        { label: 'Dealer', value: 'DEALER' },
        { label: 'Wholesale', value: 'WHOLESALE' }
    ];

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
                this.error.set(err.error?.message || 'Failed to create customer');
                this.saving.set(false);
                console.error('Error creating customer:', err);
            }
        });
    }
}
