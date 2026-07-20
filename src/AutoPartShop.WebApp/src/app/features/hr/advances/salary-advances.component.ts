import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { SalaryAdvanceService, SalaryAdvanceResponse } from '../services/salary-advance.service';
import { EmployeeService, EmployeeResponse } from '../services/employee.service';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { InputNumberModule } from 'primeng/inputnumber';
import { Select } from 'primeng/select';
import { DatePickerModule } from 'primeng/datepicker';
import { DialogModule } from 'primeng/dialog';
import { TooltipModule } from 'primeng/tooltip';
import { ToastModule } from 'primeng/toast';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { PaginatorState } from 'primeng/paginator';
import { MessageService, ConfirmationService } from 'primeng/api';
import { PageContainerComponent } from '@/shared/components/page-container/page-container.component';
import { PageHeaderComponent } from '@/shared/components/page-header/page-header.component';
import { FilterBarComponent } from '@/shared/components/filter-bar/filter-bar.component';
import { DataPaginationComponent } from '@/shared/components/data-pagination/data-pagination.component';

@Component({
    selector: 'app-salary-advances',
    standalone: true,
    imports: [CommonModule, FormsModule, TableModule, ButtonModule, InputTextModule, InputNumberModule, Select, DatePickerModule,
        DialogModule, TooltipModule, ToastModule, ConfirmDialogModule,
        PageContainerComponent, PageHeaderComponent, FilterBarComponent, DataPaginationComponent],
    providers: [MessageService, ConfirmationService],
    templateUrl: './salary-advances.component.html',
    styleUrls: ['./salary-advances.component.css']
})
export class SalaryAdvancesComponent implements OnInit {
    private readonly advanceService = inject(SalaryAdvanceService);
    private readonly employeeService = inject(EmployeeService);
    private readonly messageService = inject(MessageService);
    private readonly confirmationService = inject(ConfirmationService);

    advances: SalaryAdvanceResponse[] = [];
    loading = false;
    totalRecords = 0;
    pageNumber = 1;
    pageSize = 25;
    pageSizeOptions = [10, 25, 50, 100];

    searchTerm = '';
    filterStatus = '';

    statusOptions = [
        { label: 'All Statuses', value: '' },
        { label: 'Outstanding', value: 'OUTSTANDING' },
        { label: 'Settled', value: 'SETTLED' }
    ];

    paymentMethodOptions = [
        { label: 'Cash', value: 'CASH' },
        { label: 'Bank Transfer', value: 'BANK_TRANSFER' },
        { label: 'Check', value: 'CHECK' }
    ];

    dialogVisible = false;
    saving = false;
    submitted = false;
    employees: EmployeeResponse[] = [];
    form = this.emptyForm();

    ngOnInit(): void {
        this.loadAdvances();
    }

    private emptyForm() {
        return {
            employeeId: null as string | null,
            advanceDate: new Date(),
            amount: 0,
            paymentMethod: 'CASH',
            notes: ''
        };
    }

    private toDateOnly(value: Date): string {
        const month = String(value.getMonth() + 1).padStart(2, '0');
        const day = String(value.getDate()).padStart(2, '0');
        return `${value.getFullYear()}-${month}-${day}`;
    }

    loadAdvances(): void {
        this.loading = true;
        this.advanceService
            .getAdvances({
                pageNumber: this.pageNumber,
                pageSize: this.pageSize,
                search: this.searchTerm,
                status: this.filterStatus
            })
            .subscribe({
                next: (response) => {
                    this.advances = response.data;
                    this.totalRecords = response.pagination.totalCount;
                    this.loading = false;
                },
                error: (err) => {
                    this.messageService.add({
                        severity: 'error',
                        summary: 'Error',
                        detail: err?.error?.message || 'Failed to load salary advances'
                    });
                    console.error('Error loading advances:', err);
                    this.loading = false;
                }
            });
    }

    onSearch(): void {
        this.pageNumber = 1;
        this.loadAdvances();
    }

    onFilterChange(): void {
        this.pageNumber = 1;
        this.loadAdvances();
    }

    clearSearch(): void {
        this.searchTerm = '';
        this.pageNumber = 1;
        this.loadAdvances();
    }

    clearFilters(): void {
        this.searchTerm = '';
        this.filterStatus = '';
        this.pageNumber = 1;
        this.loadAdvances();
    }

    onPageChange(event: PaginatorState): void {
        this.pageNumber = (event.page ?? 0) + 1;
        this.pageSize = event.rows ?? this.pageSize;
        this.loadAdvances();
    }

    openGive(): void {
        this.submitted = false;
        this.form = this.emptyForm();
        if (this.employees.length === 0) {
            this.employeeService.getAllEmployees().subscribe({
                next: (employees) => (this.employees = employees.filter(e => e.status === 'ACTIVE')),
                error: (err) => console.error('Failed to load employees:', err)
            });
        }
        this.dialogVisible = true;
    }

    giveAdvance(): void {
        this.submitted = true;
        if (!this.form.employeeId || !this.form.amount || this.form.amount <= 0) {
            this.messageService.add({
                severity: 'warn',
                summary: 'Validation',
                detail: 'Employee and a positive amount are required'
            });
            return;
        }

        this.saving = true;
        this.advanceService.giveAdvance({
            employeeId: this.form.employeeId,
            advanceDate: this.toDateOnly(this.form.advanceDate),
            amount: this.form.amount,
            paymentMethod: this.form.paymentMethod,
            notes: this.form.notes || ''
        }).subscribe({
            next: () => {
                this.saving = false;
                this.dialogVisible = false;
                this.messageService.add({
                    severity: 'success',
                    summary: 'Success',
                    detail: 'Advance recorded — expense posted to the cash book'
                });
                this.loadAdvances();
            },
            error: (err) => {
                this.saving = false;
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: err?.error?.message || 'Failed to record advance'
                });
                console.error('Error giving advance:', err);
            }
        });
    }

    cancelAdvance(advance: SalaryAdvanceResponse): void {
        this.confirmationService.confirm({
            message: `Cancel advance of ${advance.amount} for ${advance.employeeName}? Its cash-book expense will be removed too.`,
            header: 'Cancel Advance',
            icon: 'pi pi-exclamation-triangle',
            accept: () => {
                this.advanceService.cancelAdvance(advance.id).subscribe({
                    next: () => {
                        this.messageService.add({ severity: 'success', summary: 'Success', detail: 'Advance cancelled' });
                        this.loadAdvances();
                    },
                    error: (err) => {
                        this.messageService.add({
                            severity: 'error',
                            summary: 'Error',
                            detail: err?.error?.message || 'Failed to cancel advance'
                        });
                    }
                });
            }
        });
    }
}
