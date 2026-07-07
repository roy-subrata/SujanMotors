import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { EmployeeService, EmployeeResponse } from '../../services/employee.service';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { Select } from 'primeng/select';
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
    selector: 'app-employees-list',
    standalone: true,
    imports: [CommonModule, FormsModule, TableModule, ButtonModule, InputTextModule, Select, TooltipModule, ToastModule, ConfirmDialogModule,
        PageContainerComponent, PageHeaderComponent, FilterBarComponent, DataPaginationComponent],
    providers: [MessageService, ConfirmationService],
    templateUrl: './employees-list.component.html',
    styleUrls: ['./employees-list.component.css']
})
export class EmployeesListComponent implements OnInit {
    private readonly employeeService = inject(EmployeeService);
    private readonly router = inject(Router);
    private readonly messageService = inject(MessageService);
    private readonly confirmationService = inject(ConfirmationService);

    employees: EmployeeResponse[] = [];
    loading = false;
    totalRecords = 0;
    pageNumber = 1;
    pageSize = 25;
    pageSizeOptions = [10, 25, 50, 100];

    searchTerm = '';
    filterStatus = '';
    filterDepartment = '';

    statusOptions = [
        { label: 'All Statuses', value: '' },
        { label: 'Active', value: 'ACTIVE' },
        { label: 'Inactive', value: 'INACTIVE' }
    ];

    departmentOptions = [
        { label: 'All Departments', value: '' },
        { label: 'Sales', value: 'SALES' },
        { label: 'Warehouse', value: 'WAREHOUSE' },
        { label: 'Accounts', value: 'ACCOUNTS' },
        { label: 'Admin', value: 'ADMIN' }
    ];

    ngOnInit(): void {
        this.loadEmployees();
    }

    loadEmployees(): void {
        this.loading = true;

        this.employeeService
            .getEmployees({
                pageNumber: this.pageNumber,
                pageSize: this.pageSize,
                search: this.searchTerm,
                status: this.filterStatus,
                department: this.filterDepartment
            })
            .subscribe({
                next: (response) => {
                    this.employees = response.data;
                    this.totalRecords = response.pagination.totalCount;
                    this.loading = false;
                },
                error: (err) => {
                    this.messageService.add({
                        severity: 'error',
                        summary: 'Error',
                        detail: err?.error?.message || 'Failed to load employees'
                    });
                    console.error('Error loading employees:', err);
                    this.loading = false;
                }
            });
    }

    onSearch(): void {
        this.pageNumber = 1;
        this.loadEmployees();
    }

    onFilterChange(): void {
        this.pageNumber = 1;
        this.loadEmployees();
    }

    clearSearch(): void {
        this.searchTerm = '';
        this.pageNumber = 1;
        this.loadEmployees();
    }

    clearFilters(): void {
        this.searchTerm = '';
        this.filterStatus = '';
        this.filterDepartment = '';
        this.pageNumber = 1;
        this.loadEmployees();
    }

    onPageChange(event: PaginatorState): void {
        this.pageNumber = (event.page ?? 0) + 1;
        this.pageSize = event.rows ?? this.pageSize;
        this.loadEmployees();
    }

    exportEmployees(format: 'csv' | 'json'): void {
        if (this.employees.length === 0) {
            this.messageService.add({
                severity: 'warn',
                summary: 'Warning',
                detail: 'No employees to export'
            });
            return;
        }
        if (format === 'csv') {
            this.exportToCSV(this.employees);
        } else {
            this.exportToJSON(this.employees);
        }
    }

    private exportToCSV(data: EmployeeResponse[]): void {
        const headers = ['Code', 'Name', 'Designation', 'Department', 'Phone', 'Join Date', 'Status'];
        const csvData = data.map((emp) => [emp.employeeCode, emp.name, emp.designation || '', emp.department || '', emp.phone || '', emp.joinDate?.split('T')[0] || '', emp.status]);
        const csvContent = [headers.join(','), ...csvData.map((row) => row.map((cell) => `"${cell}"`).join(','))].join('\n');
        const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' });
        const url = window.URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = `employees_${new Date().toISOString().split('T')[0]}.csv`;
        link.click();
        window.URL.revokeObjectURL(url);
        this.messageService.add({ severity: 'success', summary: 'Success', detail: 'Employees exported to CSV' });
    }

    private exportToJSON(data: EmployeeResponse[]): void {
        const jsonContent = JSON.stringify(data, null, 2);
        const blob = new Blob([jsonContent], { type: 'application/json' });
        const url = window.URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = `employees_${new Date().toISOString().split('T')[0]}.json`;
        link.click();
        window.URL.revokeObjectURL(url);
        this.messageService.add({ severity: 'success', summary: 'Success', detail: 'Employees exported to JSON' });
    }

    createEmployee(): void {
        this.router.navigate(['/hr/employees/create']);
    }

    viewEmployee(employee: EmployeeResponse): void {
        this.router.navigate(['/hr/employees/view'], { queryParams: { id: employee.id, mode: 'view' } });
    }

    editEmployee(employee: EmployeeResponse): void {
        this.router.navigate(['/hr/employees/edit'], { queryParams: { id: employee.id } });
    }

    toggleStatus(employee: EmployeeResponse): void {
        const isDeactivating = employee.status === 'ACTIVE';
        const message = isDeactivating
            ? `Are you sure you want to deactivate ${employee.name}?`
            : `Are you sure you want to activate ${employee.name}?`;

        this.confirmationService.confirm({
            message,
            header: 'Confirm',
            icon: 'pi pi-exclamation-triangle',
            accept: () => {
                const serviceAction = isDeactivating
                    ? this.employeeService.deactivateEmployee(employee.id)
                    : this.employeeService.activateEmployee(employee.id);

                serviceAction.subscribe({
                    next: () => {
                        this.messageService.add({
                            severity: 'success',
                            summary: 'Success',
                            detail: isDeactivating ? 'Employee deactivated' : 'Employee activated'
                        });
                        this.loadEmployees();
                    },
                    error: (err) => {
                        this.messageService.add({
                            severity: 'error',
                            summary: 'Error',
                            detail: err?.error?.message || (isDeactivating ? 'Failed to deactivate employee' : 'Failed to activate employee')
                        });
                    }
                });
            }
        });
    }
}
