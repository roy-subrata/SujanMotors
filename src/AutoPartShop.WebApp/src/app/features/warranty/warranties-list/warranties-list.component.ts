import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { TagModule } from 'primeng/tag';
import { ToastModule } from 'primeng/toast';
import { CardModule } from 'primeng/card';
import { TooltipModule } from 'primeng/tooltip';
import { MessageService } from 'primeng/api';
import { WarrantyService, WarrantyRegistrationResponse } from '../services/warranty.service';

@Component({
    selector: 'app-warranties-list',
    standalone: true,
    imports: [
        CommonModule,
        FormsModule,
        TableModule,
        ButtonModule,
        InputTextModule,
        TagModule,
        ToastModule,
        CardModule,
        TooltipModule
    ],
    providers: [MessageService],
    templateUrl: './warranties-list.component.html',
    styleUrls: ['./warranties-list.component.css']
})
export class WarrantiesListComponent implements OnInit {
    private readonly warrantyService = inject(WarrantyService);
    private readonly messageService = inject(MessageService);

    warranties: WarrantyRegistrationResponse[] = [];
    filteredWarranties: WarrantyRegistrationResponse[] = [];
    isLoading = false;
    searchText = '';
    selectedStatus = '';

    statuses = [
        { label: 'All', value: '' },
        { label: 'Active', value: 'ACTIVE' },
        { label: 'Expired', value: 'EXPIRED' },
        { label: 'Claimed', value: 'CLAIMED' },
        { label: 'Void', value: 'VOID' }
    ];

    ngOnInit(): void {
        this.loadWarranties();
    }

    loadWarranties(): void {
        this.isLoading = true;
        this.warrantyService.getAllWarranties().subscribe({
            next: (data) => {
                this.warranties = data;
                this.filteredWarranties = data;
                this.applyFilters();
                this.isLoading = false;
            },
            error: (error) => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: 'Failed to load warranties'
                });
                console.error('Error loading warranties:', error);
                this.isLoading = false;
            }
        });
    }

    applyFilters(): void {
        this.filteredWarranties = this.warranties.filter(warranty => {
            const matchesSearch = !this.searchText ||
                warranty.warrantyNumber.toLowerCase().includes(this.searchText.toLowerCase()) ||
                warranty.partName.toLowerCase().includes(this.searchText.toLowerCase()) ||
                warranty.customerName.toLowerCase().includes(this.searchText.toLowerCase());

            const matchesStatus = !this.selectedStatus || warranty.status === this.selectedStatus;

            return matchesSearch && matchesStatus;
        });
    }

    onSearch(): void {
        this.applyFilters();
    }

    onStatusChange(): void {
        this.applyFilters();
    }

    getStatusSeverity(status: string): string {
        switch (status) {
            case 'ACTIVE': return 'success';
            case 'EXPIRED': return 'warning';
            case 'CLAIMED': return 'info';
            case 'VOID': return 'danger';
            default: return 'secondary';
        }
    }

    getDaysUntilExpiryDisplay(warranty: WarrantyRegistrationResponse): string {
        if (warranty.status === 'EXPIRED') return 'Expired';
        if (warranty.status === 'VOID') return 'Voided';
        if (warranty.daysUntilExpiry < 0) return 'Expired';
        if (warranty.daysUntilExpiry === 0) return 'Expires today';
        if (warranty.daysUntilExpiry === 1) return '1 day left';
        return `${warranty.daysUntilExpiry} days left`;
    }

    formatDate(date: Date): string {
        return new Date(date).toLocaleDateString();
    }

    voidWarranty(warranty: WarrantyRegistrationResponse): void {
        const reason = prompt('Please enter the reason for voiding this warranty:');
        if (!reason) return;

        this.warrantyService.voidWarranty(warranty.id, { reason }).subscribe({
            next: () => {
                this.messageService.add({
                    severity: 'success',
                    summary: 'Success',
                    detail: 'Warranty voided successfully'
                });
                this.loadWarranties();
            },
            error: (error) => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: error?.error?.message || 'Failed to void warranty'
                });
            }
        });
    }
}
