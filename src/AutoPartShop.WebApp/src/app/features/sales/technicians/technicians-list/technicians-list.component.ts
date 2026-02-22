import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { TechnicianService, TechnicianResponse } from '../../services/technician.service';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { Select } from 'primeng/select';
import { CardModule } from 'primeng/card';
import { TagModule } from 'primeng/tag';
import { TooltipModule } from 'primeng/tooltip';
import { ToastModule } from 'primeng/toast';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { PaginatorModule, PaginatorState } from 'primeng/paginator';
import { MessageService, ConfirmationService } from 'primeng/api';

@Component({
    selector: 'app-technicians-list',
    standalone: true,
    imports: [CommonModule, FormsModule, TableModule, ButtonModule, InputTextModule, Select, CardModule, TagModule, TooltipModule, ToastModule, ConfirmDialogModule, PaginatorModule],
    providers: [MessageService, ConfirmationService],
    templateUrl: './technicians-list.component.html',
    styleUrls: ['./technicians-list.component.css']
})
export class TechniciansListComponent implements OnInit {
    private readonly technicianService = inject(TechnicianService);
    private readonly router = inject(Router);
    private readonly messageService = inject(MessageService);
    private readonly confirmationService = inject(ConfirmationService);

    technicians: TechnicianResponse[] = [];
    loading = false;
    totalRecords = 0;
    pageNumber = 1;
    pageSize = 25;
    pageSizeOptions = [10, 25, 50, 100];

    // Filters
    searchTerm = '';
    filterStatus = '';

    statusOptions = [
        { label: 'All Statuses', value: '' },
        { label: 'Active', value: 'ACTIVE' },
        { label: 'Inactive', value: 'INACTIVE' }
    ];

    ngOnInit(): void {
        this.loadTechnicians();
    }

    loadTechnicians(): void {
        this.loading = true;

        this.technicianService
            .getTechnicians({
                pageNumber: this.pageNumber,
                pageSize: this.pageSize,
                search: this.searchTerm
            })
            .subscribe({
                next: (response) => {
                    // Apply filters if needed
                    let filteredData = response.data;

                    if (this.filterStatus) {
                        filteredData = filteredData.filter((tech) => tech.status === this.filterStatus);
                    }

                    this.technicians = filteredData;
                    this.totalRecords = response.pagination.totalCount;
                    this.loading = false;
                },
                error: (err) => {
                    this.messageService.add({
                        severity: 'error',
                        summary: 'Error',
                        detail: 'Failed to load technicians'
                    });
                    console.error('Error loading technicians:', err);
                    this.loading = false;
                }
            });
    }

    onSearch(): void {
        this.pageNumber = 1;
        this.loadTechnicians();
    }

    onFilterChange(): void {
        this.pageNumber = 1;
        this.loadTechnicians();
    }

    clearSearch(): void {
        this.searchTerm = '';
        this.pageNumber = 1;
        this.loadTechnicians();
    }

    clearFilters(): void {
        this.searchTerm = '';
        this.filterStatus = '';
        this.pageNumber = 1;
        this.loadTechnicians();
    }

    onPageChange(event: PaginatorState): void {
        this.pageNumber = (event.page ?? 0) + 1;
        this.pageSize = event.rows ?? this.pageSize;
        this.loadTechnicians();
    }

    exportTechnicians(format: 'csv' | 'json'): void {
        const dataToExport = this.technicians;

        if (dataToExport.length === 0) {
            this.messageService.add({
                severity: 'warn',
                summary: 'No Data',
                detail: 'No technicians available to export'
            });
            return;
        }

        if (format === 'csv') {
            this.exportToCSV(dataToExport);
        } else {
            this.exportToJSON(dataToExport);
        }
    }

    private exportToCSV(data: TechnicianResponse[]): void {
        const headers = ['Code', 'Name', 'Phone', 'Shop Name', 'City', 'Status'];
        const csvData = data.map((tech) => [tech.technicianCode, tech.name, tech.phone || '', tech.shopName || '', tech.city || '', tech.status]);

        const csvContent = [headers.join(','), ...csvData.map((row) => row.map((cell) => `"${cell}"`).join(','))].join('\n');

        const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' });
        const url = window.URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = `technicians_${new Date().toISOString().split('T')[0]}.csv`;
        link.click();
        window.URL.revokeObjectURL(url);

        this.messageService.add({
            severity: 'success',
            summary: 'Export Complete',
            detail: 'Technicians exported as CSV'
        });
    }

    private exportToJSON(data: TechnicianResponse[]): void {
        const jsonContent = JSON.stringify(data, null, 2);
        const blob = new Blob([jsonContent], { type: 'application/json' });
        const url = window.URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = `technicians_${new Date().toISOString().split('T')[0]}.json`;
        link.click();
        window.URL.revokeObjectURL(url);

        this.messageService.add({
            severity: 'success',
            summary: 'Export Complete',
            detail: 'Technicians exported as JSON'
        });
    }

    createTechnician(): void {
        this.router.navigate(['/sales/technicians/create']);
    }

    viewTechnician(technician: TechnicianResponse): void {
        this.router.navigate(['/sales/technicians/view'], { queryParams: { id: technician.id } });
    }

    editTechnician(technician: TechnicianResponse): void {
        this.router.navigate(['/sales/technicians/edit'], { queryParams: { id: technician.id } });
    }

    toggleStatus(technician: TechnicianResponse): void {
        const action = technician.status === 'ACTIVE' ? 'deactivate' : 'activate';
        const message = `Are you sure you want to ${action} ${technician.name}?`;

        this.confirmationService.confirm({
            message: message,
            header: 'Confirm',
            icon: 'pi pi-exclamation-triangle',
            accept: () => {
                const serviceAction = technician.status === 'ACTIVE' ? this.technicianService.deactivateTechnician(technician.id) : this.technicianService.activateTechnician(technician.id);

                serviceAction.subscribe({
                    next: () => {
                        this.messageService.add({
                            severity: 'success',
                            summary: 'Success',
                            detail: `Technician ${action}d successfully`
                        });
                        this.loadTechnicians();
                    },
                    error: () => {
                        this.messageService.add({
                            severity: 'error',
                            summary: 'Error',
                            detail: `Failed to ${action} technician`
                        });
                    }
                });
            }
        });
    }

    getStatusSeverity(status: string): 'success' | 'secondary' {
        const severityMap: Record<string, 'success' | 'secondary'> = {
            ACTIVE: 'success',
            INACTIVE: 'secondary'
        };
        return severityMap[status] || 'secondary';
    }
}
