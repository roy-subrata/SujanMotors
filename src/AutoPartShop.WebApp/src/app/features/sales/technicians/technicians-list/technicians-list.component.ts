import { Component, OnInit, inject, DestroyRef } from '@angular/core';
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
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { I18nService } from '@/shared/services/i18n.service';

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
    private readonly i18n = inject(I18nService);
    private readonly destroyRef = inject(DestroyRef);

    technicians: TechnicianResponse[] = [];
    loading = false;
    totalRecords = 0;
    pageNumber = 1;
    pageSize = 25;
    pageSizeOptions = [10, 25, 50, 100];

    searchTerm = '';
    filterStatus = '';

    statusOptions: { label: string; value: string }[] = [];

    ngOnInit(): void {
        this.buildStatusOptions();
        this.i18n.translationsLoaded$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
            this.buildStatusOptions();
        });
        this.loadTechnicians();
    }

    private buildStatusOptions(): void {
        this.statusOptions = [
            { label: this.i18n.t('technicians.statusOptions.allStatuses'), value: '' },
            { label: this.i18n.t('technicians.statusOptions.active'),      value: 'ACTIVE' },
            { label: this.i18n.t('technicians.statusOptions.inactive'),    value: 'INACTIVE' }
        ];
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
                        summary: this.i18n.t('common.messages.error'),
                        detail: this.i18n.t('technicians.messages.loadFailed')
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
        if (this.technicians.length === 0) {
            this.messageService.add({
                severity: 'warn',
                summary: this.i18n.t('common.messages.warning'),
                detail: this.i18n.t('technicians.messages.exportNoData')
            });
            return;
        }
        if (format === 'csv') {
            this.exportToCSV(this.technicians);
        } else {
            this.exportToJSON(this.technicians);
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
            summary: this.i18n.t('common.messages.success'),
            detail: this.i18n.t('technicians.messages.exportCSVSuccess')
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
            summary: this.i18n.t('common.messages.success'),
            detail: this.i18n.t('technicians.messages.exportJSONSuccess')
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
        const isDeactivating = technician.status === 'ACTIVE';
        const confirmKey = isDeactivating ? 'technicians.messages.deactivateConfirm' : 'technicians.messages.activateConfirm';

        this.confirmationService.confirm({
            message: this.i18n.t(confirmKey, { name: technician.name }),
            header: this.i18n.t('common.actions.confirm'),
            icon: 'pi pi-exclamation-triangle',
            accept: () => {
                const serviceAction = technician.status === 'ACTIVE'
                    ? this.technicianService.deactivateTechnician(technician.id)
                    : this.technicianService.activateTechnician(technician.id);

                serviceAction.subscribe({
                    next: () => {
                        const detailKey = isDeactivating ? 'technicians.messages.deactivateSuccess' : 'technicians.messages.activateSuccess';
                        this.messageService.add({
                            severity: 'success',
                            summary: this.i18n.t('common.messages.success'),
                            detail: this.i18n.t(detailKey)
                        });
                        this.loadTechnicians();
                    },
                    error: () => {
                        const detailKey = isDeactivating ? 'technicians.messages.deactivateFailed' : 'technicians.messages.activateFailed';
                        this.messageService.add({
                            severity: 'error',
                            summary: this.i18n.t('common.messages.error'),
                            detail: this.i18n.t(detailKey)
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
