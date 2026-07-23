import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { LeaveRequestService, LeaveRequestResponse } from '../services/leave-request.service';
import { EmployeeService, EmployeeResponse } from '../services/employee.service';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { TextareaModule } from 'primeng/textarea';
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
    selector: 'app-leave-requests',
    standalone: true,
    imports: [CommonModule, FormsModule, TableModule, ButtonModule, InputTextModule, TextareaModule, Select, DatePickerModule,
        DialogModule, TooltipModule, ToastModule, ConfirmDialogModule,
        PageContainerComponent, PageHeaderComponent, FilterBarComponent, DataPaginationComponent],
    providers: [MessageService, ConfirmationService],
    templateUrl: './leave-requests.component.html',
    styleUrls: ['./leave-requests.component.css']
})
export class LeaveRequestsComponent implements OnInit {
    private readonly leaveRequestService = inject(LeaveRequestService);
    private readonly employeeService = inject(EmployeeService);
    private readonly messageService = inject(MessageService);
    private readonly confirmationService = inject(ConfirmationService);

    requests: LeaveRequestResponse[] = [];
    loading = false;
    totalRecords = 0;
    pageNumber = 1;
    pageSize = 25;
    pageSizeOptions = [10, 25, 50, 100];

    searchTerm = '';
    filterStatus = '';

    statusOptions = [
        { label: 'All Statuses', value: '' },
        { label: 'Pending', value: 'PENDING' },
        { label: 'Approved', value: 'APPROVED' },
        { label: 'Rejected', value: 'REJECTED' },
        { label: 'Cancelled', value: 'CANCELLED' }
    ];

    leaveTypeOptions = [
        { label: 'Casual', value: 'CASUAL' },
        { label: 'Sick', value: 'SICK' },
        { label: 'Annual', value: 'ANNUAL' },
        { label: 'Unpaid', value: 'UNPAID' }
    ];

    // Create/edit dialog
    dialogVisible = false;
    saving = false;
    submitted = false;
    editingId: string | null = null;
    employees: EmployeeResponse[] = [];
    form = this.emptyForm();

    ngOnInit(): void {
        this.loadRequests();
    }

    private emptyForm() {
        return {
            employeeId: null as string | null,
            leaveType: 'CASUAL',
            fromDate: null as Date | null,
            toDate: null as Date | null,
            reason: ''
        };
    }

    private toDateOnly(value: Date): string {
        const month = String(value.getMonth() + 1).padStart(2, '0');
        const day = String(value.getDate()).padStart(2, '0');
        return `${value.getFullYear()}-${month}-${day}`;
    }

    loadRequests(): void {
        this.loading = true;
        this.leaveRequestService
            .getLeaveRequests({
                pageNumber: this.pageNumber,
                pageSize: this.pageSize,
                search: this.searchTerm,
                status: this.filterStatus
            })
            .subscribe({
                next: (response) => {
                    this.requests = response.data;
                    this.totalRecords = response.pagination.totalCount;
                    this.loading = false;
                },
                error: (err) => {
                    this.messageService.add({
                        severity: 'error',
                        summary: 'Error',
                        detail: err?.error?.message || 'Failed to load leave requests'
                    });
                    console.error('Error loading leave requests:', err);
                    this.loading = false;
                }
            });
    }

    onSearch(): void {
        this.pageNumber = 1;
        this.loadRequests();
    }

    onFilterChange(): void {
        this.pageNumber = 1;
        this.loadRequests();
    }

    clearSearch(): void {
        this.searchTerm = '';
        this.pageNumber = 1;
        this.loadRequests();
    }

    clearFilters(): void {
        this.searchTerm = '';
        this.filterStatus = '';
        this.pageNumber = 1;
        this.loadRequests();
    }

    onPageChange(event: PaginatorState): void {
        this.pageNumber = (event.page ?? 0) + 1;
        this.pageSize = event.rows ?? this.pageSize;
        this.loadRequests();
    }

    openCreate(): void {
        this.editingId = null;
        this.submitted = false;
        this.form = this.emptyForm();
        this.loadEmployeesForDialog();
        this.dialogVisible = true;
    }

    openEdit(request: LeaveRequestResponse): void {
        this.editingId = request.id;
        this.submitted = false;
        this.form = {
            employeeId: request.employeeId,
            leaveType: request.leaveType,
            fromDate: new Date(request.fromDate),
            toDate: new Date(request.toDate),
            reason: request.reason
        };
        this.loadEmployeesForDialog();
        this.dialogVisible = true;
    }

    private loadEmployeesForDialog(): void {
        if (this.employees.length > 0) return;
        this.employeeService.getAllEmployees().subscribe({
            next: (employees) => (this.employees = employees.filter(e => e.status === 'ACTIVE')),
            error: (err) => console.error('Failed to load employees:', err)
        });
    }

    saveRequest(): void {
        this.submitted = true;
        if (!this.form.employeeId || !this.form.fromDate || !this.form.toDate) {
            this.messageService.add({
                severity: 'warn',
                summary: 'Validation',
                detail: 'Employee, From date and To date are required'
            });
            return;
        }

        this.saving = true;
        const payload = {
            leaveType: this.form.leaveType,
            fromDate: this.toDateOnly(this.form.fromDate),
            toDate: this.toDateOnly(this.form.toDate),
            reason: this.form.reason || ''
        };

        const action = this.editingId
            ? this.leaveRequestService.updateLeaveRequest(this.editingId, payload)
            : this.leaveRequestService.createLeaveRequest({ ...payload, employeeId: this.form.employeeId });

        action.subscribe({
            next: () => {
                this.saving = false;
                this.dialogVisible = false;
                this.messageService.add({
                    severity: 'success',
                    summary: 'Success',
                    detail: this.editingId ? 'Leave request updated' : 'Leave request created'
                });
                this.loadRequests();
            },
            error: (err) => {
                this.saving = false;
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: err?.error?.message || 'Failed to save leave request'
                });
                console.error('Error saving leave request:', err);
            }
        });
    }

    approve(request: LeaveRequestResponse): void {
        this.confirmationService.confirm({
            message: `Approve ${request.leaveType} leave for ${request.employeeName} (${request.totalDays} day(s))? Attendance will be marked as LEAVE for the range.`,
            header: 'Approve Leave',
            icon: 'pi pi-check-circle',
            accept: () => {
                this.leaveRequestService.approveLeaveRequest(request.id).subscribe({
                    next: () => {
                        this.messageService.add({ severity: 'success', summary: 'Success', detail: 'Leave approved' });
                        this.loadRequests();
                    },
                    error: (err) => {
                        this.messageService.add({
                            severity: 'error',
                            summary: 'Error',
                            detail: err?.error?.message || 'Failed to approve leave'
                        });
                    }
                });
            }
        });
    }

    reject(request: LeaveRequestResponse): void {
        this.confirmationService.confirm({
            message: `Reject ${request.leaveType} leave for ${request.employeeName}?`,
            header: 'Reject Leave',
            icon: 'pi pi-times-circle',
            accept: () => {
                this.leaveRequestService.rejectLeaveRequest(request.id).subscribe({
                    next: () => {
                        this.messageService.add({ severity: 'success', summary: 'Success', detail: 'Leave rejected' });
                        this.loadRequests();
                    },
                    error: (err) => {
                        this.messageService.add({
                            severity: 'error',
                            summary: 'Error',
                            detail: err?.error?.message || 'Failed to reject leave'
                        });
                    }
                });
            }
        });
    }

    getStatusSeverity(status: string): string {
        const map: Record<string, string> = {
            PENDING: 'warn',
            APPROVED: 'success',
            REJECTED: 'danger',
            CANCELLED: 'secondary'
        };
        return map[status] || 'secondary';
    }
}
