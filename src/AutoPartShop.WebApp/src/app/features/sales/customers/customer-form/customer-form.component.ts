import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { CustomerService, CreateCustomerRequest } from '../../services/customer.service';
import { CustomerVehicleService, CustomerVehicleResponse, CreateCustomerVehicleRequest } from '../../services/customer-vehicle.service';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { InputNumberModule } from 'primeng/inputnumber';
import { SelectModule } from 'primeng/select';
import { DatePickerModule } from 'primeng/datepicker';
import { CardModule } from 'primeng/card';
import { ToastModule } from 'primeng/toast';
import { TextareaModule } from 'primeng/textarea';
import { TooltipModule } from 'primeng/tooltip';
import { DialogModule } from 'primeng/dialog';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { MessageService, ConfirmationService } from 'primeng/api';
import { CodeGenerationService } from '@/shared/services/CodeGenerationService';
import { ItemResponse, CountryService, CustomerTypeService } from '@/shared/services/CountryService';
import { tap } from 'rxjs';
import { I18nService } from '@/shared/services/i18n.service';
import { TranslatePipe } from '@/shared/pipes/translate.pipe';
@Component({
    selector: 'app-customer-form',
    standalone: true,
    imports: [CommonModule, ReactiveFormsModule, ButtonModule, InputTextModule, InputNumberModule, SelectModule, DatePickerModule, CardModule, ToastModule, TextareaModule, TooltipModule, DialogModule, ConfirmDialogModule,
        TranslatePipe],
    providers: [MessageService, ConfirmationService],
    templateUrl: './customer-form.component.html',
    styleUrls: ['./customer-form.component.css']
})
export class CustomerFormComponent implements OnInit {
    private readonly fb = inject(FormBuilder);
    private readonly router = inject(Router);
    private readonly route = inject(ActivatedRoute);
    private readonly customerService = inject(CustomerService);
    private readonly vehicleService = inject(CustomerVehicleService);
    private readonly messageService = inject(MessageService);
    private readonly confirmationService = inject(ConfirmationService);
    private readonly codeGenerationService = inject(CodeGenerationService);
    private readonly countryService = inject(CountryService);
    private readonly customerTypeService = inject(CustomerTypeService);
    private readonly i18n = inject(I18nService);

    customerForm!: FormGroup;
    loading = signal(false);
    saving = signal(false);
    error = signal<string | null>(null);
    mode = signal<'create' | 'edit' | 'view'>('create');
    customerId = signal<string | null>(null);
    generatingCode = signal(false);

    // Vehicles (only manageable once the customer exists — i.e. edit/view mode)
    vehicles = signal<CustomerVehicleResponse[]>([]);
    vehiclesLoading = signal(false);
    vehicleDialogVisible = signal(false);
    editingVehicleId = signal<string | null>(null);
    savingVehicle = signal(false);

    vehicleForm: FormGroup = this.fb.group({
        registrationNo: ['', [Validators.required]],
        make: [''],
        model: [''],
        year: [null as number | null],
        engineType: [''],
        vin: [''],
        color: [''],
        mileage: [null as number | null],
        notes: ['']
    });

    customerTypes: ItemResponse[] = [];

    countries: ItemResponse[] = [];

    ngOnInit(): void {
        this.initializeForm();
        this.loadCountries();
        this.loadCustomerTypes();

        this.route.queryParams.subscribe((params) => {
            const id = params['id'];
            const mode = params['mode'];

            if (id) {
                this.customerId.set(id);
                this.mode.set(mode === 'view' ? 'view' : 'edit');
                this.loadCustomer(id);
                this.loadVehicles(id);
            } else {
                // Create mode - generate customer code automatically
                this.generateCustomerCode();
            }
        });

        if (this.mode() === 'view') {
            this.customerForm.disable();
        }
    }

    private loadCountries() {
        this.countryService
            .findAll({ query: '', page: 1, pageSize: 100 })
            .pipe(
                tap({
                    next: (value) => {
                        this.countries = value.items;
                    },
                    error: (error) => {
                        console.error('Failed to call country list:', error);
                        this.messageService.add({ severity: 'warn', summary: this.i18n.t('common.messages.warning'), detail: this.i18n.t('customers.form.messages.countryLoadFailed') });
                    }
                })
            )
            .subscribe();
    }

    private loadCustomerTypes() {
        this.customerTypeService
            .findAll({ query: '', page: 1, pageSize: 100 })
            .pipe(
                tap({
                    next: (value) => {
                        this.customerTypes = value.items;
                    },
                    error: (error) => {
                        console.error('Failed to call customer types list:', error);
                        this.messageService.add({ severity: 'warn', summary: this.i18n.t('common.messages.warning'), detail: this.i18n.t('customers.form.messages.typesLoadFailed') });
                    }
                })
            )
            .subscribe();
    }

    generateCustomerCode(): void {
        this.generatingCode.set(true);
        this.codeGenerationService.generateCustomerCode().subscribe({
            next: (code) => {
                this.customerForm.patchValue({ customerCode: code });
                this.generatingCode.set(false);
            },
            error: (err) => {
                console.error('Failed to generate customer code:', err);
                this.messageService.add({ severity: 'warn', summary: this.i18n.t('common.messages.warning'), detail: this.i18n.t('customers.form.messages.codeGenerateFailed') });
                this.generatingCode.set(false);
            }
        });
    }

    initializeForm(): void {
        this.customerForm = this.fb.group({
            customerCode: ['', [Validators.required, Validators.minLength(3)]],
            firstName: ['', [Validators.required, Validators.minLength(2)]],
            lastName: ['', [Validators.required, Validators.minLength(2)]],
            companyName: [''],
            email: ['', [Validators.email]],
            phone: ['', [Validators.required]],
            alternatePhone: [''],
            billingAddress: ['', []],
            shippingAddress: [''],
            city: ['', []],
            state: ['', []],
            postalCode: ['', []],
            country: ['Bangladesh', []],
            customerType: ['RETAIL', [Validators.required]],
            primaryContactPerson: [''],
            notes: ['']
        });
    }

    loadCustomer(id: string): void {
        this.loading.set(true);
        this.error.set(null);

        this.customerService.getCustomerById(id).subscribe({
            next: (customer: any) => {
                this.customerForm.patchValue({
                    customerCode: customer.customerCode,
                    firstName: customer.firstName,
                    lastName: customer.lastName,
                    companyName: customer.companyName,
                    email: customer.email,
                    phone: customer.phone,
                    alternatePhone: customer.alternatePhone,
                    billingAddress: customer.billingAddress,
                    shippingAddress: customer.shippingAddress,
                    city: customer.city,
                    state: customer.state,
                    postalCode: customer.postalCode,
                    customerType: customer.customerType,
                    primaryContactPerson: customer.primaryContactPerson,
                    notes: customer.notes
                });
                this.loading.set(false);

                if (this.mode() === 'view') {
                    this.customerForm.disable();
                }
            },
            error: (err: any) => {
                this.error.set('Failed to load customer');
                this.loading.set(false);
                this.messageService.add({ severity: 'error', summary: this.i18n.t('common.messages.error'), detail: this.i18n.t('customers.form.messages.loadFailed') });
                console.error('Error loading customer:', err);
            }
        });
    }

    onSubmit(): void {
        if (this.customerForm.invalid) {
            Object.keys(this.customerForm.controls).forEach((key) => {
                const control = this.customerForm.get(key);
                if (control?.invalid) {
                    control.markAsTouched();
                }
            });
            this.messageService.add({ severity: 'warn', summary: this.i18n.t('customers.form.messages.validationTitle'), detail: this.i18n.t('customers.form.messages.fillRequired') });
            return;
        }

        this.saving.set(true);
        this.error.set(null);

        // Use getRawValue() to include disabled fields like customerCode
        const formValue = this.customerForm.getRawValue();

        const isEdit = this.mode() === 'edit' && this.customerId();
        const request: CreateCustomerRequest = {
            // customerCode is a *preview* of the next code (see CodeGenerateController), not a
            // reserved one — submitting it on create raced every other creation of this entity
            // type against the same unconsumed number and 409'd/500'd from the second one on.
            // Only send it back unchanged when editing an existing customer.
            customerCode: isEdit ? formValue.customerCode : undefined,
            firstName: formValue.firstName,
            lastName: formValue.lastName,
            companyName: formValue.companyName,
            email: formValue.email,
            phone: formValue.phone,
            alternatePhone: formValue.alternatePhone,
            billingAddress: formValue.billingAddress,
            shippingAddress: formValue.shippingAddress || formValue.billingAddress,
            city: formValue.city,
            state: formValue.state,
            postalCode: formValue.postalCode,
            country: formValue.country,
            customerType: formValue.customerType,
            primaryContactPerson: formValue.primaryContactPerson,
            notes: formValue.notes
        };

        const operation = this.mode() === 'edit' && this.customerId() ? this.customerService.updateCustomer(this.customerId()!, request) : this.customerService.createCustomer(request);

        operation.subscribe({
            next: () => {
                this.messageService.add({
                    severity: 'success',
                    summary: this.i18n.t('common.messages.success'),
                    detail: this.i18n.t(this.mode() === 'edit' ? 'customers.form.messages.updateSuccess' : 'customers.form.messages.createSuccess')
                });
                setTimeout(() => {
                    this.router.navigate(['/sales/customers']);
                }, 1000);
            },
            error: (err: any) => {
                const failedMessage = this.i18n.t(
                    this.mode() === 'edit' ? 'customers.form.messages.updateFailed' : 'customers.form.messages.createFailed'
                );
                this.error.set(failedMessage);
                this.saving.set(false);
                this.messageService.add({
                    severity: 'error',
                    summary: this.i18n.t('common.messages.error'),
                    detail: failedMessage
                });
                console.error(`Error ${this.mode() === 'edit' ? 'updating' : 'creating'} customer:`, err);
            }
        });
    }

    // ── Vehicles ─────────────────────────────────────────────────────────────
    loadVehicles(customerId: string): void {
        this.vehiclesLoading.set(true);
        this.vehicleService.getByCustomer(customerId).subscribe({
            next: (vehicles) => {
                this.vehicles.set(vehicles);
                this.vehiclesLoading.set(false);
            },
            error: () => {
                this.vehicles.set([]);
                this.vehiclesLoading.set(false);
            }
        });
    }

    openAddVehicle(): void {
        this.editingVehicleId.set(null);
        this.vehicleForm.reset({ registrationNo: '', make: '', model: '', year: null, engineType: '', vin: '', color: '', mileage: null, notes: '' });
        this.vehicleDialogVisible.set(true);
    }

    openEditVehicle(vehicle: CustomerVehicleResponse): void {
        this.editingVehicleId.set(vehicle.id);
        this.vehicleForm.reset({
            registrationNo: vehicle.registrationNo,
            make: vehicle.make,
            model: vehicle.model,
            year: vehicle.year ?? null,
            engineType: vehicle.engineType,
            vin: vehicle.vin,
            color: vehicle.color,
            mileage: vehicle.mileage ?? null,
            notes: vehicle.notes
        });
        this.vehicleDialogVisible.set(true);
    }

    saveVehicle(): void {
        const customerId = this.customerId();
        if (!customerId) return;
        if (this.vehicleForm.invalid) {
            this.vehicleForm.markAllAsTouched();
            return;
        }

        const v = this.vehicleForm.value;
        const request: CreateCustomerVehicleRequest = {
            registrationNo: v.registrationNo,
            vin: v.vin ?? '',
            make: v.make ?? '',
            model: v.model ?? '',
            year: v.year ?? null,
            engineType: v.engineType ?? '',
            color: v.color ?? '',
            mileage: v.mileage ?? null,
            notes: v.notes ?? '',
            catalogVehicleId: null
        };

        this.savingVehicle.set(true);
        const editingId = this.editingVehicleId();
        const op$ = editingId
            ? this.vehicleService.update(customerId, editingId, request)
            : this.vehicleService.create(customerId, request);

        op$.subscribe({
            next: () => {
                this.messageService.add({ severity: 'success', summary: this.i18n.t('customers.form.messages.savedTitle'), detail: this.i18n.t(editingId ? 'customers.form.messages.vehicleUpdated' : 'customers.form.messages.vehicleAdded') });
                this.savingVehicle.set(false);
                this.vehicleDialogVisible.set(false);
                this.loadVehicles(customerId);
            },
            error: (error) => {
                this.messageService.add({ severity: 'error', summary: this.i18n.t('common.messages.error'), detail: error?.error?.message || this.i18n.t('customers.form.messages.vehicleSaveFailed') });
                this.savingVehicle.set(false);
            }
        });
    }

    confirmDeleteVehicle(vehicle: CustomerVehicleResponse): void {
        const customerId = this.customerId();
        if (!customerId) return;
        this.confirmationService.confirm({
            message: this.i18n.t('customers.form.messages.vehicleDeleteConfirm', { label: vehicle.label }),
            header: this.i18n.t('customers.form.messages.vehicleDeleteHeader'),
            icon: 'pi pi-exclamation-triangle',
            acceptButtonStyleClass: 'p-button-danger',
            accept: () => {
                this.vehicleService.delete(customerId, vehicle.id).subscribe({
                    next: () => {
                        this.messageService.add({ severity: 'success', summary: this.i18n.t('customers.form.messages.deletedTitle'), detail: this.i18n.t('customers.form.messages.vehicleRemoved') });
                        this.loadVehicles(customerId);
                    },
                    error: (error) => {
                        this.messageService.add({ severity: 'error', summary: this.i18n.t('common.messages.error'), detail: error?.error?.message || this.i18n.t('customers.form.messages.vehicleDeleteFailed') });
                    }
                });
            }
        });
    }

    cancel(): void {
        this.router.navigate(['/sales/customers']);
    }

    getPageTitle(): string {
        switch (this.mode()) {
            case 'create':
                return this.i18n.t('customers.form.pageTitleCreate');
            case 'edit':
                return this.i18n.t('customers.form.pageTitleEdit');
            case 'view':
                return this.i18n.t('customers.form.pageTitleView');
            default:
                return this.i18n.t('customers.form.pageTitleDefault');
        }
    }

    getPageSubtitle(): string {
        switch (this.mode()) {
            case 'create':
                return this.i18n.t('customers.form.pageSubtitleCreate');
            case 'edit':
                return this.i18n.t('customers.form.pageSubtitleEdit');
            case 'view':
                return this.i18n.t('customers.form.pageSubtitleView');
            default:
                return '';
        }
    }
}
