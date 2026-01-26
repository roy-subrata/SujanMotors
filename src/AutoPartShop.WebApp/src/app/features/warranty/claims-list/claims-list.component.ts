import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { CardModule } from 'primeng/card';
import { ToastModule } from 'primeng/toast';
import { TagModule } from 'primeng/tag';
import { TooltipModule } from 'primeng/tooltip';
import { DialogModule } from 'primeng/dialog';
import { TextareaModule } from 'primeng/textarea';
import { AutoCompleteModule } from 'primeng/autocomplete';
import { DatePickerModule } from 'primeng/datepicker';
import { SelectModule } from 'primeng/select';
import { MessageService } from 'primeng/api';
import { WarrantyService, WarrantyClaimResponse, WarrantyRegistrationResponse, CreateWarrantyClaimRequest } from '../services/warranty.service';

@Component({
    selector: 'app-claims-list',
    standalone: true,
    imports: [
        CommonModule,
        FormsModule,
        TableModule,
        ButtonModule,
        InputTextModule,
        CardModule,
        ToastModule,
        TagModule,
        TooltipModule,
        DialogModule,
        TextareaModule,
        AutoCompleteModule,
        DatePickerModule,
        SelectModule
    ],
    providers: [MessageService],
    templateUrl: './claims-list.component.html',
    styleUrls: ['./claims-list.component.css']
})
export class ClaimsListComponent implements OnInit {
    private readonly warrantyService = inject(WarrantyService);
    private readonly messageService = inject(MessageService);

    claims: WarrantyClaimResponse[] = [];
    filteredClaims: WarrantyClaimResponse[] = [];
    isLoading = false;
    searchText = '';
    selectedStatus = '';

    statuses = [
        { label: 'All', value: '' },
        { label: 'Pending', value: 'PENDING' },
        { label: 'Investigating', value: 'INVESTIGATING' },
        { label: 'Approved', value: 'APPROVED' },
        { label: 'Rejected', value: 'REJECTED' },
        { label: 'In Repair', value: 'IN_REPAIR' },
        { label: 'Repaired', value: 'REPAIRED' },
        { label: 'Replaced', value: 'REPLACED' },
        { label: 'Closed', value: 'CLOSED' },
        { label: 'Cancelled', value: 'CANCELLED' }
    ];

    // Dialog states
    showApproveDialog = false;
    showRejectDialog = false;
    showAssignDialog = false;
    showCreateDialog = false;
    selectedClaim: WarrantyClaimResponse | null = null;

    // Form fields
    approvalNotes = '';
    approvalType: 'REPAIR' | 'REPLACEMENT' | 'REFUND' = 'REPAIR';
    estimatedCost = 0;
    rejectReason = '';
    technicianId = '';
    assignmentNotes = '';

    // Create claim form fields
    activeWarranties: WarrantyRegistrationResponse[] = [];
    filteredWarranties: WarrantyRegistrationResponse[] = [];
    selectedWarranty: WarrantyRegistrationResponse | null = null;
    newClaim = {
        warrantyNumber: '',
        issueDescription: '',
        serviceType: 'REPAIR',
        claimDate: new Date()
    };
    serviceTypes = [
        { label: 'Repair', value: 'REPAIR' },
        { label: 'Replacement', value: 'REPLACEMENT' },
        { label: 'Refund', value: 'REFUND' }
    ];

    ngOnInit(): void {
        this.loadClaims();
        this.loadActiveWarranties();
    }

    loadActiveWarranties(): void {
        this.warrantyService.getActiveWarranties().subscribe({
            next: (warranties) => {
                this.activeWarranties = warranties;
            },
            error: (error) => {
                console.error('Error loading active warranties:', error);
            }
        });
    }

    loadClaims(): void {
        this.isLoading = true;
        this.warrantyService.getAllClaims().subscribe({
            next: (claims) => {
                this.claims = claims;
                this.applyFilters();
                this.isLoading = false;
            },
            error: (error) => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: 'Failed to load warranty claims'
                });
                console.error('Error loading claims:', error);
                this.isLoading = false;
            }
        });
    }

    applyFilters(): void {
        this.filteredClaims = this.claims.filter(claim => {
            const matchesSearch = !this.searchText ||
                claim.claimNumber.toLowerCase().includes(this.searchText.toLowerCase()) ||
                claim.warrantyNumber.toLowerCase().includes(this.searchText.toLowerCase()) ||
                claim.partName.toLowerCase().includes(this.searchText.toLowerCase()) ||
                claim.customerName.toLowerCase().includes(this.searchText.toLowerCase());

            const matchesStatus = !this.selectedStatus || claim.status === this.selectedStatus;

            return matchesSearch && matchesStatus;
        });
    }

    onSearch(): void {
        this.applyFilters();
    }

    onStatusChange(): void {
        this.applyFilters();
    }

    formatDate(date: string | Date | null): string {
        if (!date) return '';
        return new Date(date).toLocaleDateString('en-US', {
            year: 'numeric',
            month: 'short',
            day: 'numeric'
        });
    }

    getStatusSeverity(status: string): 'success' | 'info' | 'warning' | 'danger' {
        const severityMap: { [key: string]: 'success' | 'info' | 'warning' | 'danger' } = {
            'PENDING': 'info',
            'INVESTIGATING': 'info',
            'APPROVED': 'success',
            'REJECTED': 'danger',
            'IN_REPAIR': 'warning',
            'REPAIRED': 'success',
            'REPLACED': 'success',
            'CLOSED': 'success',
            'CANCELLED': 'danger'
        };
        return severityMap[status] || 'info';
    }

    getPrioritySeverity(priority: string): 'success' | 'info' | 'warning' | 'danger' {
        const severityMap: { [key: string]: 'success' | 'info' | 'warning' | 'danger' } = {
            'LOW': 'success',
            'MEDIUM': 'info',
            'HIGH': 'warning',
            'URGENT': 'danger'
        };
        return severityMap[priority] || 'info';
    }

    // Action methods
    openApproveDialog(claim: WarrantyClaimResponse): void {
        this.selectedClaim = claim;
        this.approvalNotes = '';
        this.approvalType = 'REPAIR';
        this.estimatedCost = 0;
        this.showApproveDialog = true;
    }

    openRejectDialog(claim: WarrantyClaimResponse): void {
        this.selectedClaim = claim;
        this.rejectReason = '';
        this.showRejectDialog = true;
    }

    openAssignDialog(claim: WarrantyClaimResponse): void {
        this.selectedClaim = claim;
        this.technicianId = '';
        this.assignmentNotes = '';
        this.showAssignDialog = true;
    }

    approveClaim(): void {
        if (!this.selectedClaim) return;
        this.warrantyService.approveClaim(this.selectedClaim.id, {
            approvedBy: 'admin' // Replace with actual user if available
        }).subscribe({
            next: () => {
                this.messageService.add({
                    severity: 'success',
                    summary: 'Success',
                    detail: 'Claim approved successfully'
                });
                this.showApproveDialog = false;
                this.loadClaims();
            },
            error: (error: any) => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: error?.error?.message || 'Failed to approve claim'
                });
            }
        });
    }

    rejectClaim(): void {
        if (!this.selectedClaim || !this.rejectReason.trim()) {
            this.messageService.add({
                severity: 'warn',
                summary: 'Validation Error',
                detail: 'Please enter a rejection reason'
            });
            return;
        }

        this.warrantyService.rejectClaim(this.selectedClaim.id, {
            rejectionReason: this.rejectReason,
            rejectedBy: 'admin' // Replace with actual user if available
        }).subscribe({
            next: () => {
                this.messageService.add({
                    severity: 'success',
                    summary: 'Success',
                    detail: 'Claim rejected successfully'
                });
                this.showRejectDialog = false;
                this.loadClaims();
            },
            error: (error: any) => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: error?.error?.message || 'Failed to reject claim'
                });
            }
        });
    }

    assignTechnician(): void {
        if (!this.selectedClaim || !this.technicianId.trim()) {
            this.messageService.add({
                severity: 'warn',
                summary: 'Validation Error',
                detail: 'Please enter a technician ID'
            });
            return;
        }

        this.warrantyService.assignTechnician(this.selectedClaim.id, {
            technicianId: this.technicianId
        }).subscribe({
            next: () => {
                this.messageService.add({
                    severity: 'success',
                    summary: 'Success',
                    detail: 'Technician assigned successfully'
                });
                this.showAssignDialog = false;
                this.loadClaims();
            },
            error: (error: any) => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: error?.error?.message || 'Failed to assign technician'
                });
            }
        });
    }

    updateStatus(claim: WarrantyClaimResponse, newStatus: string): void {
        const statusUpdateMap: { [key: string]: () => void } = {
            'INVESTIGATING': () => this.warrantyService.markAsInvestigating(claim.id).subscribe({
                next: () => this.handleStatusUpdateSuccess('Claim marked as investigating'),
                error: (error: any) => this.handleStatusUpdateError(error)
            }),
            'IN_REPAIR': () => this.warrantyService.markAsInRepair(claim.id).subscribe({
                next: () => this.handleStatusUpdateSuccess('Claim marked as in repair'),
                error: (error: any) => this.handleStatusUpdateError(error)
            }),
            'REPAIRED': () => this.warrantyService.markAsRepaired(claim.id).subscribe({
                next: () => this.handleStatusUpdateSuccess('Claim marked as repaired'),
                error: (error: any) => this.handleStatusUpdateError(error)
            }),
            'REPLACED': () => this.warrantyService.markAsReplaced(claim.id).subscribe({
                next: () => this.handleStatusUpdateSuccess('Claim marked as replaced'),
                error: (error: any) => this.handleStatusUpdateError(error)
            }),
            'CLOSED': () => this.warrantyService.closeClaim(claim.id).subscribe({
                next: () => this.handleStatusUpdateSuccess('Claim closed successfully'),
                error: (error: any) => this.handleStatusUpdateError(error)
            })
        };

        const updateFn = statusUpdateMap[newStatus];
        if (updateFn) {
            updateFn();
        }
    }

    private handleStatusUpdateSuccess(message: string): void {
        this.messageService.add({
            severity: 'success',
            summary: 'Success',
            detail: message
        });
        this.loadClaims();
    }

    private handleStatusUpdateError(error: any): void {
        this.messageService.add({
            severity: 'error',
            summary: 'Error',
            detail: error?.error?.message || 'Failed to update claim status'
        });
    }

    canApprove(claim: WarrantyClaimResponse): boolean {
        return claim.status === 'PENDING' || claim.status === 'INVESTIGATING';
    }

    canReject(claim: WarrantyClaimResponse): boolean {
        return claim.status === 'PENDING' || claim.status === 'INVESTIGATING';
    }

    canAssignTechnician(claim: WarrantyClaimResponse): boolean {
        return claim.status === 'APPROVED' && !claim.technicianId;
    }

    canUpdateStatus(claim: WarrantyClaimResponse): boolean {
        return ['INVESTIGATING', 'APPROVED', 'IN_REPAIR', 'REPAIRED', 'REPLACED'].includes(claim.status);
    }

    // Create claim methods
    openCreateDialog(): void {
        this.selectedWarranty = null;
        this.newClaim = {
            warrantyNumber: '',
            issueDescription: '',
            serviceType: 'REPAIR',
            claimDate: new Date()
        };
        this.showCreateDialog = true;
    }

    searchWarranties(event: any): void {
        const query = event.query.toLowerCase();
        this.filteredWarranties = this.activeWarranties.filter(warranty =>
            warranty.warrantyNumber.toLowerCase().includes(query) ||
            warranty.partName.toLowerCase().includes(query) ||
            warranty.customerName.toLowerCase().includes(query) ||
            warranty.certificateNumber.toLowerCase().includes(query)
        );
    }

    onWarrantySelect(event: any): void {
        const warranty = event.value;
        this.selectedWarranty = warranty;
        this.newClaim.warrantyNumber = warranty.warrantyNumber;
    }

    getWarrantyDisplayText(warranty: WarrantyRegistrationResponse): string {
        return `${warranty.warrantyNumber} - ${warranty.partName} (${warranty.customerName})`;
    }

    createClaim(): void {
        if (!this.selectedWarranty) {
            this.messageService.add({
                severity: 'warn',
                summary: 'Validation Error',
                detail: 'Please select a warranty'
            });
            return;
        }

        if (!this.newClaim.issueDescription.trim()) {
            this.messageService.add({
                severity: 'warn',
                summary: 'Validation Error',
                detail: 'Please enter issue description'
            });
            return;
        }

        const request: CreateWarrantyClaimRequest = {
            warrantyRegistrationId: this.selectedWarranty.id,
            customerId: this.selectedWarranty.customerId,
            claimDate: this.newClaim.claimDate,
            issueDescription: this.newClaim.issueDescription,
            serviceType: this.newClaim.serviceType,
            serviceCostCurrency: 'BDT'
        };

        this.warrantyService.createClaim(request).subscribe({
            next: () => {
                this.messageService.add({
                    severity: 'success',
                    summary: 'Success',
                    detail: 'Warranty claim created successfully'
                });
                this.showCreateDialog = false;
                this.loadClaims();
            },
            error: (error: any) => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: error?.error?.message || 'Failed to create warranty claim'
                });
            }
        });
    }
}
