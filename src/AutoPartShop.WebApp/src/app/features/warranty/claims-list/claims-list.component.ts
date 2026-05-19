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
import { InputNumberModule } from 'primeng/inputnumber';
import { MessageService } from 'primeng/api';
import {
    WarrantyService,
    WarrantyClaimResponse,
    WarrantyRegistrationResponse,
    CreateWarrantyClaimRequest,
    CompleteClaimRequest,
    ReplacementLogisticsResponse,
    SendDefectiveItemRequest,
    ReceiveReplacementItemRequest
} from '../services/warranty.service';
import { CurrencyService } from '../../../shared/services/currency.service';
import { TechnicianService, TechnicianResponse } from '../../sales/services/technician.service';
import { SupplierService, SupplierResponse } from '../../inventory/services/supplier.service';
import { AuthService } from '../../../shared/services/auth.service';

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
        SelectModule,
        InputNumberModule
    ],
    providers: [MessageService],
    templateUrl: './claims-list.component.html',
    styleUrls: ['./claims-list.component.css']
})
export class ClaimsListComponent implements OnInit {
    private readonly warrantyService = inject(WarrantyService);
    private readonly messageService = inject(MessageService);
    private readonly currencyService = inject(CurrencyService);
    private readonly technicianService = inject(TechnicianService);
    private readonly supplierService = inject(SupplierService);
    private readonly authService = inject(AuthService);

    get currentUsername(): string {
        return this.authService.currentUser()?.username || 'admin';
    }

    claims: WarrantyClaimResponse[] = [];
    filteredClaims: WarrantyClaimResponse[] = [];
    isLoading = false;
    searchText = '';
    selectedStatus = '';

    // Metrics
    totalPending = 0;
    totalUnderReview = 0;
    totalInProgress = 0;
    totalCompleted = 0;

    statuses = [
        { label: 'All Statuses', value: '' },
        { label: 'Pending', value: 'PENDING' },
        { label: 'Under Review', value: 'UNDER_REVIEW' },
        { label: 'Approved', value: 'APPROVED' },
        { label: 'Rejected', value: 'REJECTED' },
        { label: 'In Progress', value: 'IN_PROGRESS' },
        { label: 'Completed', value: 'COMPLETED' },
        { label: 'Closed', value: 'CLOSED' }
    ];

    readonly standardFlow = ['PENDING', 'UNDER_REVIEW', 'APPROVED', 'IN_PROGRESS', 'COMPLETED', 'CLOSED'];

    // Dialog states
    showApproveDialog = false;
    showRejectDialog = false;
    showAssignDialog = false;
    showCompleteDialog = false;
    showServiceCostDialog = false;
    showCloseDialog = false;
    showSendDefectiveDialog = false;
    showReceiveReplacementDialog = false;
    showDetailDialog = false;
    showCreateDialog = false;
    selectedClaim: WarrantyClaimResponse | null = null;
    replacementLogistics: ReplacementLogisticsResponse | null = null;
    isLoadingReplacementLogistics = false;

    // Form fields
    rejectReason = '';
    resolutionDetails = '';
    refundType: 'CASH_REFUND' | 'STORE_CREDIT' = 'CASH_REFUND';
    refundAmount: number | null = null;
    refundReferenceNumber = '';
    refundNotes = '';
    refundReturnItemReceived = true;
    refundRestockAsSellable = false;
    closureNotes = '';
    serviceCost = 0;
    serviceNotes = '';
    sendDefectiveForm = {
        partnerType: 'SELLER' as 'SELLER' | 'MANUFACTURER',
        supplierId: '',
        manufacturerName: '',
        responsibleBy: '',
        referenceNumber: '',
        notes: ''
    };
    receiveReplacementForm = {
        partnerType: 'SELLER' as 'SELLER' | 'MANUFACTURER',
        supplierId: '',
        manufacturerName: '',
        responsibleBy: '',
        referenceNumber: '',
        notes: ''
    };

    suppliers: SupplierResponse[] = [];

    // Technician assignment
    technicians: TechnicianResponse[] = [];
    filteredTechnicians: TechnicianResponse[] = [];
    selectedTechnician: TechnicianResponse | null = null;

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
        { label: 'Repair Resolution', value: 'REPAIR' },
        { label: 'Replacement Resolution', value: 'REPLACEMENT' },
        { label: 'Refund Resolution', value: 'REFUND' }
    ];

    ngOnInit(): void {
        this.loadClaims();
        this.loadActiveWarranties();
        this.loadTechnicians();
        this.loadSuppliers();
    }

    loadSuppliers(): void {
        this.supplierService.getActiveSuppliers().subscribe({
            next: (suppliers) => {
                this.suppliers = suppliers || [];
            },
            error: (error) => {
                console.error('Error loading suppliers:', error);
            }
        });
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

    loadTechnicians(): void {
        this.technicianService.getAllTechnicians().subscribe({
            next: (techs) => {
                this.technicians = techs.filter(t => t.status === 'ACTIVE');
            },
            error: (error) => {
                console.error('Error loading technicians:', error);
            }
        });
    }

    loadClaims(): void {
        this.isLoading = true;
        this.warrantyService.getAllClaims().subscribe({
            next: (claims) => {
                this.claims = claims;
                this.updateMetrics();
                this.applyFilters();
                this.isLoading = false;
            },
            error: (error) => {
                const msg = typeof error?.error === 'string' ? error.error : (error?.error?.message || 'Failed to load warranty claims');
                this.messageService.add({ severity: 'error', summary: 'Error', detail: msg });
                this.isLoading = false;
            }
        });
    }

    updateMetrics(): void {
        this.totalPending = this.claims.filter(c => c.status === 'PENDING').length;
        this.totalUnderReview = this.claims.filter(c => c.status === 'UNDER_REVIEW').length;
        this.totalInProgress = this.claims.filter(c => c.status === 'IN_PROGRESS').length;
        this.totalCompleted = this.claims.filter(c => c.status === 'COMPLETED' || c.status === 'CLOSED').length;
    }

    applyFilters(): void {
        this.filteredClaims = this.claims.filter(claim => {
            const matchesSearch = !this.searchText ||
                claim.claimNumber.toLowerCase().includes(this.searchText.toLowerCase()) ||
                claim.warrantyNumber.toLowerCase().includes(this.searchText.toLowerCase()) ||
                claim.partName.toLowerCase().includes(this.searchText.toLowerCase()) ||
                claim.customerName.toLowerCase().includes(this.searchText.toLowerCase()) ||
                (claim.technicianName || '').toLowerCase().includes(this.searchText.toLowerCase());

            const matchesStatus = !this.selectedStatus || claim.status === this.selectedStatus;
            return matchesSearch && matchesStatus;
        });
    }

    onSearch(): void {
        this.applyFilters();
    }

    clearSearch(): void {
        this.searchText = '';
        this.applyFilters();
    }

    onStatusChange(): void {
        this.applyFilters();
    }

    formatDate(date: string | Date | null | undefined): string {
        if (!date) return '—';
        return new Date(date).toLocaleDateString('en-US', {
            year: 'numeric',
            month: 'short',
            day: 'numeric'
        });
    }

    formatCurrency(amount: number): string {
        return this.currencyService.formatCurrency(amount, this.currencyService.selectedCurrency());
    }

    getStatusSeverity(status: string): 'success' | 'info' | 'warn' | 'danger' | 'secondary' | 'contrast' {
        const map: Record<string, 'success' | 'info' | 'warn' | 'danger' | 'secondary' | 'contrast'> = {
            'PENDING': 'warn',
            'UNDER_REVIEW': 'info',
            'APPROVED': 'success',
            'REJECTED': 'danger',
            'IN_PROGRESS': 'info',
            'COMPLETED': 'success',
            'CLOSED': 'secondary'
        };
        return map[status] || 'info';
    }

    getStatusLabel(status: string): string {
        const map: Record<string, string> = {
            'PENDING': 'Pending',
            'UNDER_REVIEW': 'Under Review',
            'APPROVED': 'Approved',
            'REJECTED': 'Rejected',
            'IN_PROGRESS': 'In Progress',
            'COMPLETED': 'Completed',
            'CLOSED': 'Closed'
        };
        return map[status] || status;
    }

    getCoverageTypeLabel(coverageType: string | null | undefined): string {
        const map: Record<string, string> = {
            'MANUFACTURER': 'Manufacturer',
            'SELLER': 'Seller',
            'EXTENDED': 'Extended'
        };

        return map[(coverageType || '').toUpperCase()] || '—';
    }

    getCoverageTypeSeverity(coverageType: string | null | undefined): 'success' | 'info' | 'warn' | 'danger' | 'secondary' | 'contrast' {
        const map: Record<string, 'success' | 'info' | 'warn' | 'danger' | 'secondary' | 'contrast'> = {
            'MANUFACTURER': 'info',
            'SELLER': 'success',
            'EXTENDED': 'warn'
        };

        return map[(coverageType || '').toUpperCase()] || 'secondary';
    }

    getResolutionMethodLabel(serviceType: string): string {
        const map: Record<string, string> = {
            'REPAIR': 'Repair',
            'REPLACEMENT': 'Replacement',
            'REFUND': 'Refund'
        };

        return map[serviceType] || serviceType;
    }

    getResolutionMethodSeverity(serviceType: string): 'success' | 'info' | 'warn' | 'danger' | 'secondary' | 'contrast' {
        const map: Record<string, 'success' | 'info' | 'warn' | 'danger' | 'secondary' | 'contrast'> = {
            'REPAIR': 'info',
            'REPLACEMENT': 'success',
            'REFUND': 'warn'
        };

        return map[serviceType] || 'secondary';
    }

    getDaysOpenClass(days: number): string {
        if (days <= 7) return 'days-ok';
        if (days <= 21) return 'days-warning';
        return 'days-overdue';
    }

    // ==================== DETAIL ====================
    openDetailDialog(claim: WarrantyClaimResponse): void {
        this.selectedClaim = claim;
        this.showDetailDialog = true;
        if (claim.serviceType === 'REPLACEMENT' || claim.serviceType === 'REFUND') {
            this.loadReplacementLogistics(claim.id);
        } else {
            this.replacementLogistics = null;
        }
    }

    loadReplacementLogistics(claimId?: string): void {
        const id = claimId || this.selectedClaim?.id;
        if (!id) return;

        this.isLoadingReplacementLogistics = true;
        this.warrantyService.getReplacementLogistics(id).subscribe({
            next: (response) => {
                this.replacementLogistics = response;
                this.isLoadingReplacementLogistics = false;
            },
            error: (error: any) => {
                const msg = typeof error?.error === 'string' ? error.error : (error?.error?.message || 'Failed to load replacement logistics');
                this.messageService.add({ severity: 'error', summary: 'Error', detail: msg });
                this.isLoadingReplacementLogistics = false;
            }
        });
    }

    // ==================== SUBMIT FOR REVIEW ====================
    submitForReview(claim: WarrantyClaimResponse): void {
        this.warrantyService.submitClaimForReview(claim.id).subscribe({
            next: () => {
                this.messageService.add({ severity: 'success', summary: 'Success', detail: 'Claim submitted for review' });
                this.loadClaims();
            },
            error: (error: any) => {
                const msg = typeof error?.error === 'string' ? error.error : (error?.error?.message || 'Failed to submit for review');
                this.messageService.add({ severity: 'error', summary: 'Error', detail: msg });
            }
        });
    }

    // ==================== APPROVE ====================
    openApproveDialog(claim: WarrantyClaimResponse): void {
        this.selectedClaim = claim;
        this.showApproveDialog = true;
    }

    approveClaim(): void {
        if (!this.selectedClaim) return;
        this.warrantyService.approveClaim(this.selectedClaim.id, {
            approvedBy: this.currentUsername
        }).subscribe({
            next: () => {
                this.messageService.add({ severity: 'success', summary: 'Success', detail: 'Claim approved successfully' });
                this.showApproveDialog = false;
                this.loadClaims();
            },
            error: (error: any) => {
                const msg = typeof error?.error === 'string' ? error.error : (error?.error?.message || 'Failed to approve claim');
                this.messageService.add({ severity: 'error', summary: 'Error', detail: msg });
            }
        });
    }

    // ==================== REJECT ====================
    openRejectDialog(claim: WarrantyClaimResponse): void {
        this.selectedClaim = claim;
        this.rejectReason = '';
        this.showRejectDialog = true;
    }

    rejectClaim(): void {
        if (!this.selectedClaim || !this.rejectReason.trim()) {
            this.messageService.add({ severity: 'warn', summary: 'Validation', detail: 'Please enter a rejection reason' });
            return;
        }
        this.warrantyService.rejectClaim(this.selectedClaim.id, {
            rejectionReason: this.rejectReason,
            rejectedBy: this.currentUsername
        }).subscribe({
            next: () => {
                this.messageService.add({ severity: 'success', summary: 'Success', detail: 'Claim rejected' });
                this.showRejectDialog = false;
                this.loadClaims();
            },
            error: (error: any) => {
                const msg = typeof error?.error === 'string' ? error.error : (error?.error?.message || 'Failed to reject claim');
                this.messageService.add({ severity: 'error', summary: 'Error', detail: msg });
            }
        });
    }

    // ==================== ASSIGN TECHNICIAN ====================
    openAssignDialog(claim: WarrantyClaimResponse): void {
        this.selectedClaim = claim;
        this.selectedTechnician = null;
        this.filteredTechnicians = [...this.technicians];
        this.showAssignDialog = true;
    }

    searchTechnicians(event: any): void {
        const query = (event?.query ?? '').toString().trim().toLowerCase();
        this.filteredTechnicians = this.technicians.filter(t =>
            (t.name || '').toLowerCase().includes(query) ||
            (t.technicianCode || '').toLowerCase().includes(query) ||
            (t.shopName || '').toLowerCase().includes(query)
        );
    }

    assignTechnician(): void {
        if (!this.selectedClaim || !this.selectedTechnician) {
            this.messageService.add({ severity: 'warn', summary: 'Validation', detail: 'Please select a technician' });
            return;
        }
        this.warrantyService.assignTechnician(this.selectedClaim.id, {
            technicianId: this.selectedTechnician.id
        }).subscribe({
            next: () => {
                this.messageService.add({ severity: 'success', summary: 'Success', detail: 'Technician assigned successfully' });
                this.showAssignDialog = false;
                this.loadClaims();
            },
            error: (error: any) => {
                const msg = typeof error?.error === 'string' ? error.error : (error?.error?.message || 'Failed to assign technician');
                this.messageService.add({ severity: 'error', summary: 'Error', detail: msg });
            }
        });
    }

    // ==================== UPDATE SERVICE COST ====================
    openServiceCostUpdate(claim: WarrantyClaimResponse): void {
        this.selectedClaim = claim;
        this.serviceCost = claim.serviceCost || 0;
        this.serviceNotes = claim.serviceNotes || '';
        this.showServiceCostDialog = true;
    }

    updateServiceCost(): void {
        if (!this.selectedClaim) return;
        this.warrantyService.updateServiceCost(this.selectedClaim.id, {
            serviceCost: this.serviceCost,
            serviceNotes: this.serviceNotes
        }).subscribe({
            next: () => {
                this.messageService.add({ severity: 'success', summary: 'Success', detail: 'Service cost updated' });
                this.showServiceCostDialog = false;
                this.loadClaims();
            },
            error: (error: any) => {
                const msg = typeof error?.error === 'string' ? error.error : (error?.error?.message || 'Failed to update service cost');
                this.messageService.add({ severity: 'error', summary: 'Error', detail: msg });
            }
        });
    }

    canUpdateServiceCost(claim: WarrantyClaimResponse): boolean {
        return claim.serviceType === 'REPAIR' &&
            (claim.status === 'IN_PROGRESS' || claim.status === 'APPROVED');
    }

    // ==================== COMPLETE ====================
    openCompleteDialog(claim: WarrantyClaimResponse): void {
        this.selectedClaim = claim;
        this.resolutionDetails = '';
        this.refundType = 'CASH_REFUND';
        this.refundAmount = null;
        this.refundReferenceNumber = '';
        this.refundNotes = '';
        this.refundReturnItemReceived = true;
        this.refundRestockAsSellable = false;
        // Pre-fill any previously saved service cost for REPAIR
        this.serviceCost = claim.serviceCost || 0;
        this.serviceNotes = claim.serviceNotes || '';
        this.showCompleteDialog = true;
    }

    completeClaim(): void {
        if (!this.selectedClaim || !this.resolutionDetails.trim()) {
            this.messageService.add({ severity: 'warn', summary: 'Validation', detail: 'Please enter resolution details' });
            return;
        }

        if (this.selectedClaim.serviceType === 'REFUND') {
            if (!this.refundAmount || this.refundAmount <= 0) {
                this.messageService.add({ severity: 'warn', summary: 'Validation', detail: 'Please enter a valid refund amount' });
                return;
            }
        }

        const payload: CompleteClaimRequest = {
            resolutionDetails: this.resolutionDetails
        };

        if (this.selectedClaim.serviceType === 'REFUND') {
            payload.refundType = this.refundType;
            payload.refundAmount = this.refundAmount ?? undefined;
            payload.referenceNumber = this.refundReferenceNumber?.trim() || undefined;
            payload.refundNotes = this.refundNotes?.trim() || undefined;
            payload.returnItemReceived = this.refundReturnItemReceived;
            payload.restockAsSellable = this.refundReturnItemReceived ? this.refundRestockAsSellable : undefined;
        }

        // For REPAIR: save service cost before completing if cost > 0 or notes provided
        const saveServiceCost$ = (
            this.selectedClaim.serviceType === 'REPAIR' &&
            (this.serviceCost > 0 || this.serviceNotes.trim())
        )
            ? this.warrantyService.updateServiceCost(this.selectedClaim.id, {
                serviceCost: this.serviceCost,
                serviceNotes: this.serviceNotes.trim() || undefined
            })
            : null;

        const doComplete = () => {
            this.warrantyService.completeClaim(this.selectedClaim!.id, payload).subscribe({
                next: () => {
                    this.messageService.add({ severity: 'success', summary: 'Success', detail: 'Claim completed successfully' });
                    this.showCompleteDialog = false;
                    this.loadClaims();
                },
                error: (error: any) => {
                    const msg = typeof error?.error === 'string' ? error.error : (error?.error?.message || 'Failed to complete claim');
                    this.messageService.add({ severity: 'error', summary: 'Error', detail: msg });
                }
            });
        };

        if (saveServiceCost$) {
            saveServiceCost$.subscribe({
                next: () => doComplete(),
                error: (error: any) => {
                    const msg = typeof error?.error === 'string' ? error.error : (error?.error?.message || 'Failed to save service cost');
                    this.messageService.add({ severity: 'error', summary: 'Error', detail: msg });
                }
            });
        } else {
            doComplete();
        }
    }

    // ==================== CLOSE ====================
    openCloseDialog(claim: WarrantyClaimResponse): void {
        this.selectedClaim = claim;
        this.closureNotes = '';
        this.showCloseDialog = true;
    }

    openSendDefectiveDialog(claim: WarrantyClaimResponse): void {
        const defaultPartnerType = claim.warrantyCoverageType === 'MANUFACTURER' ? 'MANUFACTURER' : 'SELLER';
        this.selectedClaim = claim;
        this.sendDefectiveForm = {
            partnerType: defaultPartnerType,
            supplierId: '',
            manufacturerName: '',
            responsibleBy: this.currentUsername,
            referenceNumber: '',
            notes: ''
        };
        this.showSendDefectiveDialog = true;
    }

    submitSendDefective(): void {
        if (!this.selectedClaim) return;

        const destination = this.sendDefectiveForm.partnerType === 'SELLER'
            ? this.getSupplierName(this.sendDefectiveForm.supplierId)
            : this.sendDefectiveForm.manufacturerName.trim();

        if (!destination || !this.sendDefectiveForm.responsibleBy.trim()) {
            this.messageService.add({ severity: 'warn', summary: 'Validation', detail: 'Destination and responsible person are required' });
            return;
        }

        const payload: SendDefectiveItemRequest = {
            destination,
            responsibleBy: this.sendDefectiveForm.responsibleBy.trim(),
            referenceNumber: this.sendDefectiveForm.referenceNumber?.trim() || undefined,
            notes: this.sendDefectiveForm.notes?.trim() || undefined
        };

        this.warrantyService.sendDefectiveItem(this.selectedClaim.id, payload).subscribe({
            next: () => {
                this.messageService.add({ severity: 'success', summary: 'Success', detail: 'Defective item sent and tracked' });
                this.showSendDefectiveDialog = false;
                this.loadClaims();
                this.loadReplacementLogistics(this.selectedClaim?.id);
            },
            error: (error: any) => {
                const msg = typeof error?.error === 'string' ? error.error : (error?.error?.message || 'Failed to send defective item');
                this.messageService.add({ severity: 'error', summary: 'Error', detail: msg });
            }
        });
    }

    openReceiveReplacementDialog(claim: WarrantyClaimResponse): void {
        const defaultPartnerType = claim.warrantyCoverageType === 'MANUFACTURER' ? 'MANUFACTURER' : 'SELLER';
        this.selectedClaim = claim;
        this.receiveReplacementForm = {
            partnerType: defaultPartnerType,
            supplierId: '',
            manufacturerName: '',
            responsibleBy: this.currentUsername,
            referenceNumber: '',
            notes: ''
        };
        this.showReceiveReplacementDialog = true;
    }

    submitReceiveReplacement(): void {
        if (!this.selectedClaim) return;

        const source = this.receiveReplacementForm.partnerType === 'SELLER'
            ? this.getSupplierName(this.receiveReplacementForm.supplierId)
            : this.receiveReplacementForm.manufacturerName.trim();

        if (!source || !this.receiveReplacementForm.responsibleBy.trim()) {
            this.messageService.add({ severity: 'warn', summary: 'Validation', detail: 'Source and responsible person are required' });
            return;
        }

        const payload: ReceiveReplacementItemRequest = {
            source,
            responsibleBy: this.receiveReplacementForm.responsibleBy.trim(),
            referenceNumber: this.receiveReplacementForm.referenceNumber?.trim() || undefined,
            notes: this.receiveReplacementForm.notes?.trim() || undefined
        };

        this.warrantyService.receiveReplacementItem(this.selectedClaim.id, payload).subscribe({
            next: () => {
                this.messageService.add({ severity: 'success', summary: 'Success', detail: 'Replacement item received and added to stock' });
                this.showReceiveReplacementDialog = false;
                this.loadClaims();
                this.loadReplacementLogistics(this.selectedClaim?.id);
            },
            error: (error: any) => {
                const msg = typeof error?.error === 'string' ? error.error : (error?.error?.message || 'Failed to receive replacement item');
                this.messageService.add({ severity: 'error', summary: 'Error', detail: msg });
            }
        });
    }

    closeClaim(): void {
        if (!this.selectedClaim) return;
        this.warrantyService.closeClaim(this.selectedClaim.id, {
            closureNotes: this.closureNotes || undefined
        }).subscribe({
            next: () => {
                this.messageService.add({ severity: 'success', summary: 'Success', detail: 'Claim closed successfully' });
                this.showCloseDialog = false;
                this.loadClaims();
            },
            error: (error: any) => {
                const msg = typeof error?.error === 'string' ? error.error : (error?.error?.message || 'Failed to close claim');
                this.messageService.add({ severity: 'error', summary: 'Error', detail: msg });
            }
        });
    }

    // ==================== PERMISSION CHECKS ====================
    canSubmitForReview(claim: WarrantyClaimResponse): boolean {
        return claim.status === 'PENDING';
    }

    canApprove(claim: WarrantyClaimResponse): boolean {
        return claim.status === 'UNDER_REVIEW';
    }

    canReject(claim: WarrantyClaimResponse): boolean {
        return claim.status === 'UNDER_REVIEW' || claim.status === 'PENDING';
    }

    canAssignTechnician(claim: WarrantyClaimResponse): boolean {
        return claim.status === 'APPROVED' && claim.serviceType === 'REPAIR';
    }

    canComplete(claim: WarrantyClaimResponse): boolean {
        return claim.status === 'IN_PROGRESS' ||
            (claim.status === 'APPROVED' && (claim.serviceType === 'REPLACEMENT' || claim.serviceType === 'REFUND'));
    }

    canClose(claim: WarrantyClaimResponse): boolean {
        return claim.status === 'COMPLETED' || claim.status === 'REJECTED';
    }

    canRunQuickFlow(claim: WarrantyClaimResponse): boolean {
        return claim.status !== 'REJECTED' && claim.status !== 'CLOSED';
    }

    canOpenSendDefective(claim: WarrantyClaimResponse): boolean {
        return claim.serviceType === 'REPLACEMENT' && claim.canSendDefectiveItem;
    }

    canOpenReceiveReplacement(claim: WarrantyClaimResponse): boolean {
        return claim.serviceType === 'REPLACEMENT' && claim.canReceiveReplacementItem;
    }

    getLogisticsStateLabel(state: string | undefined): string {
        const map: Record<string, string> = {
            'PENDING_COMPLETION': 'Pending Completion',
            'DEFECTIVE_QUARANTINED': 'Defective Quarantined',
            'DEFECTIVE_SENT': 'Defective Sent',
            'REPLACEMENT_RECEIVED': 'Replacement Received',
            'REFUND_ITEM_RETURNED': 'Refund Item Returned',
            'NOT_APPLICABLE': 'Not Applicable'
        };
        return map[state || 'NOT_APPLICABLE'] || (state || 'Not Applicable');
    }

    getLogisticsStateSeverity(state: string | undefined): 'success' | 'info' | 'warn' | 'danger' | 'secondary' | 'contrast' {
        const map: Record<string, 'success' | 'info' | 'warn' | 'danger' | 'secondary' | 'contrast'> = {
            'PENDING_COMPLETION': 'secondary',
            'DEFECTIVE_QUARANTINED': 'warn',
            'DEFECTIVE_SENT': 'info',
            'REPLACEMENT_RECEIVED': 'success',
            'REFUND_ITEM_RETURNED': 'success',
            'NOT_APPLICABLE': 'secondary'
        };
        return map[state || 'NOT_APPLICABLE'] || 'secondary';
    }

    getLogisticsReasonLabel(reason: string): string {
        const map: Record<string, string> = {
            'WARRANTY_REPLACEMENT_OUT': 'Replacement Dispatched',
            'WARRANTY_DEFECTIVE_RETURN': 'Defective Returned (Quarantined)',
            'WARRANTY_REFUND_RETURN': 'Refund Item Returned',
            'WARRANTY_DEFECTIVE_SENT_TO_VENDOR': 'Defective Sent to Vendor',
            'WARRANTY_REPLACEMENT_RECEIVED_FROM_VENDOR': 'Replacement Received from Vendor'
        };
        return map[reason] || reason;
    }

    getSupplierName(supplierId: string): string {
        if (!supplierId) return '';
        const supplier = this.suppliers.find(s => s.id === supplierId);
        if (!supplier) return '';
        const parts = [supplier.name?.trim()];
        if (supplier.phone?.trim()) parts.push(supplier.phone.trim());
        return parts.filter(Boolean).join(' · ');
    }

    getQuickFlowLabel(claim: WarrantyClaimResponse): string {
        switch (claim.status) {
            case 'PENDING':
                return 'Send to Review';
            case 'UNDER_REVIEW':
                return 'Approve';
            case 'APPROVED':
                return claim.serviceType === 'REPAIR' ? 'Assign Tech' : 'Complete';
            case 'IN_PROGRESS':
                return 'Complete';
            case 'COMPLETED':
                return 'Close';
            case 'REJECTED':
                return 'Rejected';
            case 'CLOSED':
                return 'Closed';
            default:
                return 'Next Step';
        }
    }

    getQuickFlowIcon(claim: WarrantyClaimResponse): string {
        switch (claim.status) {
            case 'PENDING':
                return 'pi pi-send';
            case 'UNDER_REVIEW':
                return 'pi pi-check';
            case 'APPROVED':
                return claim.serviceType === 'REPAIR' ? 'pi pi-user-plus' : 'pi pi-check-circle';
            case 'IN_PROGRESS':
                return 'pi pi-check-circle';
            case 'COMPLETED':
                return 'pi pi-lock';
            default:
                return 'pi pi-step-forward';
        }
    }

    runQuickFlow(claim: WarrantyClaimResponse): void {
        switch (claim.status) {
            case 'PENDING':
                this.submitForReview(claim);
                return;
            case 'UNDER_REVIEW':
                this.quickApprove(claim);
                return;
            case 'APPROVED':
                if (claim.serviceType === 'REPAIR') {
                    this.openAssignDialog(claim);
                    this.messageService.add({
                        severity: 'info',
                        summary: 'Next Step',
                        detail: 'Assign a technician to continue this claim.'
                    });
                } else if (claim.serviceType === 'REFUND') {
                    this.openCompleteDialog(claim);
                } else {
                    this.quickComplete(claim);
                }
                return;
            case 'IN_PROGRESS':
                if (claim.serviceType === 'REFUND') {
                    this.openCompleteDialog(claim);
                } else {
                    this.quickComplete(claim);
                }
                return;
            case 'COMPLETED':
                this.quickClose(claim);
                return;
            default:
                this.messageService.add({
                    severity: 'info',
                    summary: 'No Next Step',
                    detail: 'This claim is already finalized.'
                });
                return;
        }
    }

    private quickApprove(claim: WarrantyClaimResponse): void {
        this.warrantyService.approveClaim(claim.id, { approvedBy: this.currentUsername }).subscribe({
            next: () => {
                this.messageService.add({ severity: 'success', summary: 'Success', detail: 'Claim approved and ready for technician assignment' });
                this.loadClaims();
            },
            error: (error: any) => {
                const msg = typeof error?.error === 'string' ? error.error : (error?.error?.message || 'Failed to approve claim');
                this.messageService.add({ severity: 'error', summary: 'Error', detail: msg });
            }
        });
    }

    private quickComplete(claim: WarrantyClaimResponse): void {
        const resolution = claim.serviceType === 'REPLACEMENT'
            ? 'Replacement completed and item handed to customer.'
            : 'Service completed and item handed to customer.';

        this.warrantyService.completeClaim(claim.id, { resolutionDetails: resolution }).subscribe({
            next: () => {
                this.messageService.add({ severity: 'success', summary: 'Success', detail: 'Claim marked as completed' });
                this.loadClaims();
            },
            error: (error: any) => {
                const msg = typeof error?.error === 'string' ? error.error : (error?.error?.message || 'Failed to complete claim');
                this.messageService.add({ severity: 'error', summary: 'Error', detail: msg });
            }
        });
    }

    private quickClose(claim: WarrantyClaimResponse): void {
        this.warrantyService.closeClaim(claim.id, {
            closureNotes: 'Closed via standard quick flow.'
        }).subscribe({
            next: () => {
                this.messageService.add({ severity: 'success', summary: 'Success', detail: 'Claim closed' });
                this.loadClaims();
            },
            error: (error: any) => {
                const msg = typeof error?.error === 'string' ? error.error : (error?.error?.message || 'Failed to close claim');
                this.messageService.add({ severity: 'error', summary: 'Error', detail: msg });
            }
        });
    }

    // ==================== CREATE CLAIM ====================
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

    createClaim(): void {
        if (!this.selectedWarranty) {
            this.messageService.add({ severity: 'warn', summary: 'Validation', detail: 'Please select a warranty' });
            return;
        }
        if (!this.newClaim.issueDescription.trim()) {
            this.messageService.add({ severity: 'warn', summary: 'Validation', detail: 'Please enter issue description' });
            return;
        }

        const request: CreateWarrantyClaimRequest = {
            warrantyRegistrationId: this.selectedWarranty.id,
            customerId: this.selectedWarranty.customerId,
            claimDate: this.newClaim.claimDate,
            issueDescription: this.newClaim.issueDescription,
            serviceType: this.newClaim.serviceType,
            serviceCostCurrency: this.currencyService.selectedCurrency()
        };

        this.warrantyService.createClaim(request).subscribe({
            next: () => {
                this.messageService.add({ severity: 'success', summary: 'Success', detail: 'Warranty claim created successfully' });
                this.showCreateDialog = false;
                this.loadClaims();
            },
            error: (error: any) => {
                const msg = typeof error?.error === 'string' ? error.error : (error?.error?.message || 'Failed to create warranty claim');
                this.messageService.add({ severity: 'error', summary: 'Error', detail: msg });
            }
        });
    }
}
