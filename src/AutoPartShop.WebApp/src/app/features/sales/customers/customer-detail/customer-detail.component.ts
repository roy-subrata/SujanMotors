import { Component, OnInit, DestroyRef, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { TagModule } from 'primeng/tag';
import { ToastModule } from 'primeng/toast';
import { TooltipModule } from 'primeng/tooltip';
import { AvatarModule } from 'primeng/avatar';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { InputNumberModule } from 'primeng/inputnumber';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { MessageService, ConfirmationService } from 'primeng/api';
import { CustomerService, CustomerResponse } from '../../services/customer.service';
import {
    CustomerVehicleService,
    CustomerVehicleResponse,
    CreateCustomerVehicleRequest
} from '../../services/customer-vehicle.service';
import { AppCurrencyPipe } from '../../../../shared/pipes/app-currency.pipe';
import { StatusDisplayService, StatusSeverity } from '@/shared/services/status-display.service';
import { I18nService } from '@/shared/services/i18n.service';
import { TranslatePipe } from '@/shared/pipes/translate.pipe';

@Component({
    selector: 'app-customer-detail',
    standalone: true,
    imports: [
        CommonModule, ReactiveFormsModule, ButtonModule, TagModule, ToastModule, TooltipModule,
        AvatarModule, DialogModule, InputTextModule, InputNumberModule, ConfirmDialogModule, AppCurrencyPipe,
        TranslatePipe
    ],
    providers: [MessageService, ConfirmationService],
    templateUrl: './customer-detail.component.html',
    styleUrls: ['./customer-detail.component.css']
})
export class CustomerDetailComponent implements OnInit {
    private readonly route = inject(ActivatedRoute);
    private readonly router = inject(Router);
    private readonly customerService = inject(CustomerService);
    private readonly vehicleService = inject(CustomerVehicleService);
    private readonly messageService = inject(MessageService);
    private readonly confirmationService = inject(ConfirmationService);
    private readonly fb = inject(FormBuilder);
    private readonly destroyRef = inject(DestroyRef);
    private readonly statusDisplay = inject(StatusDisplayService);
    private readonly i18n = inject(I18nService);

    customerId = signal<string>('');
    customer = signal<CustomerResponse | null>(null);
    loading = signal(true);

    // Vehicles
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

    ngOnInit(): void {
        this.route.queryParams.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((params) => {
            const id = params['id'];
            if (id) {
                this.customerId.set(id);
                this.loadCustomerData();
                this.loadVehicles();
            } else {
                this.router.navigate(['/sales/customers']);
            }
        });
    }

    loadCustomerData(): void {
        this.loading.set(true);
        this.customerService.getCustomerById(this.customerId())
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: (customer) => {
                    this.customer.set(customer);
                    this.loading.set(false);
                },
                error: (error) => {
                    this.messageService.add({
                        severity: 'error',
                        summary: this.i18n.t('common.messages.error'),
                        detail: this.i18n.t('customers.detail.messages.loadFailed')
                    });
                    console.error('Error loading customer:', error);
                    this.loading.set(false);
                }
            });
    }

    loadVehicles(): void {
        this.vehiclesLoading.set(true);
        this.vehicleService.getByCustomer(this.customerId())
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: (vehicles) => {
                    this.vehicles.set(vehicles);
                    this.vehiclesLoading.set(false);
                },
                error: (error) => {
                    console.error('Error loading vehicles:', error);
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
            ? this.vehicleService.update(this.customerId(), editingId, request)
            : this.vehicleService.create(this.customerId(), request);

        op$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
            next: () => {
                this.messageService.add({
                    severity: 'success',
                    summary: this.i18n.t('customers.form.messages.savedTitle'),
                    detail: this.i18n.t(editingId ? 'customers.form.messages.vehicleUpdated' : 'customers.form.messages.vehicleAdded')
                });
                this.savingVehicle.set(false);
                this.vehicleDialogVisible.set(false);
                this.loadVehicles();
            },
            error: (error) => {
                this.messageService.add({
                    severity: 'error',
                    summary: this.i18n.t('common.messages.error'),
                    detail: error?.error?.message || this.i18n.t('customers.form.messages.vehicleSaveFailed')
                });
                this.savingVehicle.set(false);
            }
        });
    }

    /** Customer status is a server enum rendered directly in a tag. */
    formatStatus(status: string): string {
        if (!status) return '';
        const key = 'common.status.' + status.toLowerCase()
            .replace(/_(.)/g, (_m, c: string) => c.toUpperCase());
        const label = this.i18n.t(key);
        return label === key ? status : label;
    }

    confirmDeleteVehicle(vehicle: CustomerVehicleResponse): void {
        this.confirmationService.confirm({
            message: this.i18n.t('customers.form.messages.vehicleDeleteConfirm', { label: vehicle.label }),
            header: this.i18n.t('customers.form.messages.vehicleDeleteHeader'),
            icon: 'pi pi-exclamation-triangle',
            acceptButtonStyleClass: 'p-button-danger',
            accept: () => {
                this.vehicleService.delete(this.customerId(), vehicle.id)
                    .pipe(takeUntilDestroyed(this.destroyRef))
                    .subscribe({
                        next: () => {
                            this.messageService.add({ severity: 'success', summary: this.i18n.t('customers.form.messages.deletedTitle'), detail: this.i18n.t('customers.form.messages.vehicleRemoved') });
                            this.loadVehicles();
                        },
                        error: (error) => {
                            this.messageService.add({
                                severity: 'error',
                                summary: this.i18n.t('common.messages.error'),
                                detail: error?.error?.message || this.i18n.t('customers.form.messages.vehicleDeleteFailed')
                            });
                        }
                    });
            }
        });
    }

    getInitials(customer: CustomerResponse): string {
        const firstName = customer.firstName || '';
        const lastName = customer.lastName || '';
        return `${firstName.charAt(0)}${lastName.charAt(0)}`.toUpperCase();
    }

    getStatusSeverity(status: string): StatusSeverity {
        return this.statusDisplay.getSeverity(status, 'customer');
    }

    editCustomer(): void {
        this.router.navigate(['/sales/customers/edit'], {
            queryParams: { id: this.customerId() }
        });
    }

    recordPayment(): void {
        this.router.navigate(['/sales/customer-payments/new'], {
            queryParams: { customerId: this.customerId() }
        });
    }

    viewPayments(): void {
        this.router.navigate(['/sales/customer-payments'], {
            queryParams: { customerId: this.customerId() }
        });
    }

    goBack(): void {
        this.router.navigate(['/sales/customers']);
    }
}
