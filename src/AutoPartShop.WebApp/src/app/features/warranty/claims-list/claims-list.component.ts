import { Component, inject, OnInit, DestroyRef } from '@angular/core';
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
import { ClaimPrintService } from '../services/claim-print.service';
import { LazyAutocompleteComponent, LazyRequest, LazyResponse } from '@/shared/components/lazy-autocomplete';
import { Observable, map } from 'rxjs';
import { CurrencyService } from '../../../shared/services/currency.service';
import { TechnicianService, TechnicianResponse } from '../../sales/services/technician.service';
import { SupplierService, SupplierResponse } from '../../inventory/services/supplier.service';
import { AuthService } from '../../../shared/services/auth.service';
import { I18nService } from '@/shared/services/i18n.service';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { PageContainerComponent } from '@/shared/components/page-container/page-container.component';
import { PageHeaderComponent } from '@/shared/components/page-header/page-header.component';
import { FilterBarComponent } from '@/shared/components/filter-bar/filter-bar.component';

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
        InputNumberModule,
        PageContainerComponent,
        PageHeaderComponent,
        FilterBarComponent,
        LazyAutocompleteComponent
    ],
    providers: [MessageService],
    templateUrl: './claims-list.component.html',
    styleUrls: ['./claims-list.component.css']
})
export class ClaimsListComponent implements OnInit {
    private readonly warrantyService = inject(WarrantyService);
    private readonly claimPrintService = inject(ClaimPrintService);
    private readonly messageService = inject(MessageService);
    private readonly currencyService = inject(CurrencyService);
    private readonly technicianService = inject(TechnicianService);
    private readonly supplierService = inject(SupplierService);
    private readonly authService = inject(AuthService);
    private readonly i18n = inject(I18nService);
    private readonly destroyRef = inject(DestroyRef);

    get currentUsername(): string {
        return this.authService.currentUser()?.username || 'admin';
    }

    /** Claim approval/rejection/completion/closure and replacement logistics are manager-only. */
    get isManager(): boolean {
        return this.authService.hasAnyRole(['Admin', 'Manager']);
    }

    claims: WarrantyClaimResponse[] = [];
    filteredClaims: WarrantyClaimResponse[] = [];
    isLoading = false;
    searchText = '';
    selectedStatus = '';

    totalPending = 0;
    totalUnderReview = 0;
    totalInProgress = 0;
    totalCompleted = 0;

    statuses: { label: string; value: string }[] = [];

    readonly standardFlow = ['PENDING', 'UNDER_REVIEW', 'APPROVED', 'IN_PROGRESS', 'COMPLETED', 'CLOSED'];

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

    rejectReason = '';
    resolutionDetails = '';
    refundType: 'CASH_REFUND' | 'STORE_CREDIT' = 'CASH_REFUND';
    refundAmount: number | null = null;
    refundReferenceNumber = '';
    refundNotes = '';
    refundReturnItemReceived = true;
    refundRestockAsSellable = false;
    // REPLACEMENT completion options
    replacementFromVendor = false;
    replacementReturnItemReceived = true;
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

    technicians: TechnicianResponse[] = [];
    filteredTechnicians: TechnicianResponse[] = [];
    selectedTechnician: TechnicianResponse | null = null;

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
        this.buildStatuses();
        this.i18n.translationsLoaded$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
            this.buildStatuses();
        });
        this.loadClaims();
        this.loadActiveWarranties();
        this.loadTechnicians();
        this.loadSuppliers();
    }

    private buildStatuses(): void {
        this.statuses = [
            { label: this.i18n.t('warrantyClaims.statuses.allStatuses'), value: '' },
            { label: this.i18n.t('warrantyClaims.statuses.pending'),     value: 'PENDING' },
            { label: this.i18n.t('warrantyClaims.statuses.underReview'), value: 'UNDER_REVIEW' },
            { label: this.i18n.t('warrantyClaims.statuses.approved'),    value: 'APPROVED' },
            { label: this.i18n.t('warrantyClaims.statuses.rejected'),    value: 'REJECTED' },
            { label: this.i18n.t('warrantyClaims.statuses.inProgress'),  value: 'IN_PROGRESS' },
            { label: this.i18n.t('warrantyClaims.statuses.completed'),   value: 'COMPLETED' },
            { label: this.i18n.t('warrantyClaims.statuses.closed'),      value: 'CLOSED' }
        ];
    }

    loadSuppliers(): void {
        this.supplierService.getActiveSuppliers().subscribe({
            next: (suppliers) => { this.suppliers = suppliers || []; },
            error: (error) => { console.error('Error loading suppliers:', error); }
        });
    }

    loadActiveWarranties(): void {
        this.warrantyService.getActiveWarranties().subscribe({
            next: (warranties) => { this.activeWarranties = warranties; },
            error: (error) => { console.error('Error loading active warranties:', error); }
        });
    }

    loadTechnicians(): void {
        this.technicianService.getAllTechnicians().subscribe({
            next: (techs) => { this.technicians = techs.filter(t => t.status === 'ACTIVE'); },
            error: (error) => { console.error('Error loading technicians:', error); }
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
                const msg = typeof error?.error === 'string' ? error.error : (error?.error?.message || this.i18n.t('warrantyClaims.messages.loadFailed'));
                this.messageService.add({ severity: 'error', summary: this.i18n.t('common.messages.error'), detail: msg });
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

    onSearch(): void { this.applyFilters(); }
    clearSearch(): void { this.searchText = ''; this.applyFilters(); }
    onStatusChange(): void { this.applyFilters(); }

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
            'PENDING': this.i18n.t('warrantyClaims.statuses.pending'),
            'UNDER_REVIEW': this.i18n.t('warrantyClaims.statuses.underReview'),
            'APPROVED': this.i18n.t('warrantyClaims.statuses.approved'),
            'REJECTED': this.i18n.t('warrantyClaims.statuses.rejected'),
            'IN_PROGRESS': this.i18n.t('warrantyClaims.statuses.inProgress'),
            'COMPLETED': this.i18n.t('warrantyClaims.statuses.completed'),
            'CLOSED': this.i18n.t('warrantyClaims.statuses.closed')
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
                const msg = typeof error?.error === 'string' ? error.error : (error?.error?.message || this.i18n.t('common.messages.loadFailed'));
                this.messageService.add({ severity: 'error', summary: this.i18n.t('common.messages.error'), detail: msg });
                this.isLoadingReplacementLogistics = false;
            }
        });
    }

    // ==================== PRINT ====================
    printClaim(claim: WarrantyClaimResponse): void {
        this.claimPrintService.print(claim);
    }

    // ==================== START SERVICE (repair without technician) ====================
    canStartService(claim: WarrantyClaimResponse): boolean {
        return claim.serviceType === 'REPAIR' && claim.status === 'APPROVED';
    }

    startService(claim: WarrantyClaimResponse): void {
        this.warrantyService.startService(claim.id).subscribe({
            next: () => {
                this.messageService.add({ severity: 'success', summary: this.i18n.t('common.messages.success'), detail: 'Repair service started' });
                this.loadClaims();
            },
            error: (e: any) => this.showActionError(e)
        });
    }

    // ==================== DEFECTIVE DISPOSITION (replacement) ====================
    scrapDefective(claim: WarrantyClaimResponse): void { this.disposeDefective(claim, 'scrap'); }
    restockDefective(claim: WarrantyClaimResponse): void { this.disposeDefective(claim, 'restock'); }

    private disposeDefective(claim: WarrantyClaimResponse, disposition: 'scrap' | 'restock'): void {
        const verb = disposition === 'scrap' ? 'scrap (write off)' : 'return to sellable stock';
        if (!confirm(`Are you sure you want to ${verb} the defective item for claim ${claim.claimNumber}?`)) return;
        this.warrantyService.disposeDefectiveItem(claim.id, disposition, { responsibleBy: this.currentUsername }).subscribe({
            next: () => {
                this.messageService.add({
                    severity: 'success', summary: this.i18n.t('common.messages.success'),
                    detail: disposition === 'scrap' ? 'Defective item scrapped' : 'Defective item returned to sellable stock'
                });
                this.loadClaims();
                if (this.selectedClaim?.id === claim.id) this.loadReplacementLogistics(claim.id);
            },
            error: (e: any) => this.showActionError(e)
        });
    }

    // ==================== REPAIR LOGISTICS (send/receive to manufacturer) ====================
    showSendForRepairDialog = false;
    sendForRepairForm = { partnerType: 'MANUFACTURER', partnerName: '', responsibleBy: '', referenceNumber: '', expectedReturnDate: null as Date | null, notes: '' };

    openSendForRepairDialog(claim: WarrantyClaimResponse): void {
        this.selectedClaim = claim;
        this.sendForRepairForm = { partnerType: 'MANUFACTURER', partnerName: '', responsibleBy: this.currentUsername, referenceNumber: '', expectedReturnDate: null, notes: '' };
        this.showSendForRepairDialog = true;
    }

    submitSendForRepair(): void {
        if (!this.selectedClaim) return;
        if (!this.sendForRepairForm.partnerName.trim() || !this.sendForRepairForm.responsibleBy.trim()) {
            this.messageService.add({ severity: 'warn', summary: this.i18n.t('common.messages.warning'), detail: 'Partner name and responsible person are required' });
            return;
        }
        this.warrantyService.sendForRepair(this.selectedClaim.id, {
            partnerType: this.sendForRepairForm.partnerType,
            partnerName: this.sendForRepairForm.partnerName.trim(),
            responsibleBy: this.sendForRepairForm.responsibleBy.trim(),
            referenceNumber: this.sendForRepairForm.referenceNumber?.trim() || undefined,
            expectedReturnDate: this.sendForRepairForm.expectedReturnDate || undefined,
            notes: this.sendForRepairForm.notes?.trim() || undefined
        }).subscribe({
            next: () => {
                this.messageService.add({ severity: 'success', summary: this.i18n.t('common.messages.success'), detail: 'Item sent for repair' });
                this.showSendForRepairDialog = false;
                this.loadClaims();
            },
            error: (e: any) => this.showActionError(e)
        });
    }

    receiveFromRepair(claim: WarrantyClaimResponse): void {
        if (!confirm(`Confirm the repaired item for claim ${claim.claimNumber} has been received back?`)) return;
        this.warrantyService.receiveFromRepair(claim.id, { responsibleBy: this.currentUsername }).subscribe({
            next: () => {
                this.messageService.add({ severity: 'success', summary: this.i18n.t('common.messages.success'), detail: 'Repaired item received' });
                this.loadClaims();
            },
            error: (e: any) => this.showActionError(e)
        });
    }

    private showActionError(error: any): void {
        const msg = typeof error?.error === 'string' ? error.error : (error?.error?.message || 'Operation failed');
        this.messageService.add({ severity: 'error', summary: this.i18n.t('common.messages.error'), detail: msg });
    }

    // ==================== SUBMIT FOR REVIEW ====================
    submitForReview(claim: WarrantyClaimResponse): void {
        this.warrantyService.submitClaimForReview(claim.id).subscribe({
            next: () => {
                this.messageService.add({ severity: 'success', summary: this.i18n.t('common.messages.success'), detail: this.i18n.t('warrantyClaims.messages.submitForReviewSuccess') });
                this.loadClaims();
            },
            error: (error: any) => {
                const msg = typeof error?.error === 'string' ? error.error : (error?.error?.message || this.i18n.t('warrantyClaims.messages.submitForReviewFailed'));
                this.messageService.add({ severity: 'error', summary: this.i18n.t('common.messages.error'), detail: msg });
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
                this.messageService.add({ severity: 'success', summary: this.i18n.t('common.messages.success'), detail: this.i18n.t('warrantyClaims.messages.approveSuccess') });
                this.showApproveDialog = false;
                this.loadClaims();
            },
            error: (error: any) => {
                const msg = typeof error?.error === 'string' ? error.error : (error?.error?.message || this.i18n.t('warrantyClaims.messages.approveFailed'));
                this.messageService.add({ severity: 'error', summary: this.i18n.t('common.messages.error'), detail: msg });
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
            this.messageService.add({ severity: 'warn', summary: this.i18n.t('common.messages.warning'), detail: this.i18n.t('warrantyClaims.messages.validationRejectReason') });
            return;
        }
        this.warrantyService.rejectClaim(this.selectedClaim.id, {
            rejectionReason: this.rejectReason,
            rejectedBy: this.currentUsername
        }).subscribe({
            next: () => {
                this.messageService.add({ severity: 'success', summary: this.i18n.t('common.messages.success'), detail: this.i18n.t('warrantyClaims.messages.rejectSuccess') });
                this.showRejectDialog = false;
                this.loadClaims();
            },
            error: (error: any) => {
                const msg = typeof error?.error === 'string' ? error.error : (error?.error?.message || this.i18n.t('warrantyClaims.messages.rejectFailed'));
                this.messageService.add({ severity: 'error', summary: this.i18n.t('common.messages.error'), detail: msg });
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

    /** Server-side technician search for the lazy autocomplete (matches name, code, or shop). */
    searchTechniciansLazy = (req: LazyRequest): Observable<LazyResponse<TechnicianResponse>> =>
        this.technicianService.getTechnicians({
            search: req.search,
            pageNumber: req.pageNumber,
            pageSize: req.pageSize
        }).pipe(map(res => ({
            items: (res.data || []).filter(t => t.status === 'ACTIVE'),
            totalCount: res.pagination?.totalCount ?? 0
        })));

    assignTechnician(): void {
        if (!this.selectedClaim || !this.selectedTechnician) {
            this.messageService.add({ severity: 'warn', summary: this.i18n.t('common.messages.warning'), detail: this.i18n.t('warrantyClaims.messages.validationSelectTechnician') });
            return;
        }
        this.warrantyService.assignTechnician(this.selectedClaim.id, {
            technicianId: this.selectedTechnician.id
        }).subscribe({
            next: () => {
                this.messageService.add({ severity: 'success', summary: this.i18n.t('common.messages.success'), detail: this.i18n.t('warrantyClaims.messages.assignTechnicianSuccess') });
                this.showAssignDialog = false;
                this.loadClaims();
            },
            error: (error: any) => {
                const msg = typeof error?.error === 'string' ? error.error : (error?.error?.message || this.i18n.t('warrantyClaims.messages.assignTechnicianFailed'));
                this.messageService.add({ severity: 'error', summary: this.i18n.t('common.messages.error'), detail: msg });
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
                this.messageService.add({ severity: 'success', summary: this.i18n.t('common.messages.success'), detail: this.i18n.t('warrantyClaims.messages.updateServiceCostSuccess') });
                this.showServiceCostDialog = false;
                this.loadClaims();
            },
            error: (error: any) => {
                const msg = typeof error?.error === 'string' ? error.error : (error?.error?.message || this.i18n.t('warrantyClaims.messages.updateServiceCostFailed'));
                this.messageService.add({ severity: 'error', summary: this.i18n.t('common.messages.error'), detail: msg });
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
        this.replacementFromVendor = false;
        this.replacementReturnItemReceived = true;
        this.serviceCost = claim.serviceCost || 0;
        this.serviceNotes = claim.serviceNotes || '';
        this.showCompleteDialog = true;
    }

    completeClaim(): void {
        if (!this.selectedClaim || !this.resolutionDetails.trim()) {
            this.messageService.add({ severity: 'warn', summary: this.i18n.t('common.messages.warning'), detail: this.i18n.t('warrantyClaims.messages.validationResolutionDetails') });
            return;
        }

        if (this.selectedClaim.serviceType === 'REFUND') {
            if (!this.refundAmount || this.refundAmount <= 0) {
                this.messageService.add({ severity: 'warn', summary: this.i18n.t('common.messages.warning'), detail: this.i18n.t('warrantyClaims.messages.validationRefundAmount') });
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

        if (this.selectedClaim.serviceType === 'REPLACEMENT') {
            payload.replacementFromVendor = this.replacementFromVendor;
            payload.returnItemReceived = this.replacementReturnItemReceived;
        }

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
                    this.messageService.add({ severity: 'success', summary: this.i18n.t('common.messages.success'), detail: this.i18n.t('warrantyClaims.messages.completeSuccess') });
                    this.showCompleteDialog = false;
                    this.loadClaims();
                },
                error: (error: any) => {
                    const msg = typeof error?.error === 'string' ? error.error : (error?.error?.message || this.i18n.t('warrantyClaims.messages.completeFailed'));
                    this.messageService.add({ severity: 'error', summary: this.i18n.t('common.messages.error'), detail: msg });
                }
            });
        };

        if (saveServiceCost$) {
            saveServiceCost$.subscribe({
                next: () => doComplete(),
                error: (error: any) => {
                    const msg = typeof error?.error === 'string' ? error.error : (error?.error?.message || this.i18n.t('warrantyClaims.messages.updateServiceCostFailed'));
                    this.messageService.add({ severity: 'error', summary: this.i18n.t('common.messages.error'), detail: msg });
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
            this.messageService.add({ severity: 'warn', summary: this.i18n.t('common.messages.warning'), detail: this.i18n.t('warrantyClaims.messages.validationDestinationRequired') });
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
                this.messageService.add({ severity: 'success', summary: this.i18n.t('common.messages.success'), detail: this.i18n.t('warrantyClaims.messages.sendDefectiveSuccess') });
                this.showSendDefectiveDialog = false;
                this.loadClaims();
                this.loadReplacementLogistics(this.selectedClaim?.id);
            },
            error: (error: any) => {
                const msg = typeof error?.error === 'string' ? error.error : (error?.error?.message || this.i18n.t('warrantyClaims.messages.sendDefectiveFailed'));
                this.messageService.add({ severity: 'error', summary: this.i18n.t('common.messages.error'), detail: msg });
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
            this.messageService.add({ severity: 'warn', summary: this.i18n.t('common.messages.warning'), detail: this.i18n.t('warrantyClaims.messages.validationSourceRequired') });
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
                this.messageService.add({ severity: 'success', summary: this.i18n.t('common.messages.success'), detail: this.i18n.t('warrantyClaims.messages.receiveReplacementSuccess') });
                this.showReceiveReplacementDialog = false;
                this.loadClaims();
                this.loadReplacementLogistics(this.selectedClaim?.id);
            },
            error: (error: any) => {
                const msg = typeof error?.error === 'string' ? error.error : (error?.error?.message || this.i18n.t('warrantyClaims.messages.receiveReplacementFailed'));
                this.messageService.add({ severity: 'error', summary: this.i18n.t('common.messages.error'), detail: msg });
            }
        });
    }

    closeClaim(): void {
        if (!this.selectedClaim) return;
        this.warrantyService.closeClaim(this.selectedClaim.id, {
            closureNotes: this.closureNotes || undefined
        }).subscribe({
            next: () => {
                this.messageService.add({ severity: 'success', summary: this.i18n.t('common.messages.success'), detail: this.i18n.t('warrantyClaims.messages.closeSuccess') });
                this.showCloseDialog = false;
                this.loadClaims();
            },
            error: (error: any) => {
                const msg = typeof error?.error === 'string' ? error.error : (error?.error?.message || this.i18n.t('warrantyClaims.messages.closeFailed'));
                this.messageService.add({ severity: 'error', summary: this.i18n.t('common.messages.error'), detail: msg });
            }
        });
    }

    // ==================== PERMISSION CHECKS ====================
    // Staff (any authenticated user) may file claims and submit them for review.
    canSubmitForReview(claim: WarrantyClaimResponse): boolean { return claim.status === 'PENDING'; }
    canAssignTechnician(claim: WarrantyClaimResponse): boolean { return claim.status === 'APPROVED' && claim.serviceType === 'REPAIR'; }
    // Approval, rejection, completion, closure and replacement logistics are manager-only (mirrors the API).
    canApprove(claim: WarrantyClaimResponse): boolean { return this.isManager && claim.status === 'UNDER_REVIEW'; }
    canReject(claim: WarrantyClaimResponse): boolean { return this.isManager && (claim.status === 'UNDER_REVIEW' || claim.status === 'PENDING'); }
    canComplete(claim: WarrantyClaimResponse): boolean {
        return this.isManager && (claim.status === 'IN_PROGRESS' ||
            (claim.status === 'APPROVED' && (claim.serviceType === 'REPLACEMENT' || claim.serviceType === 'REFUND')));
    }
    canClose(claim: WarrantyClaimResponse): boolean { return this.isManager && (claim.status === 'COMPLETED' || claim.status === 'REJECTED'); }
    canRunQuickFlow(claim: WarrantyClaimResponse): boolean { return this.isManager && claim.status !== 'REJECTED' && claim.status !== 'CLOSED'; }
    canOpenSendDefective(claim: WarrantyClaimResponse): boolean { return this.isManager && claim.serviceType === 'REPLACEMENT' && claim.canSendDefectiveItem; }
    canOpenReceiveReplacement(claim: WarrantyClaimResponse): boolean { return this.isManager && claim.serviceType === 'REPLACEMENT' && claim.canReceiveReplacementItem; }

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
            case 'PENDING': return this.i18n.t('common.actions.submitForReview');
            case 'UNDER_REVIEW': return this.i18n.t('common.actions.approve');
            case 'APPROVED': return claim.serviceType === 'REPAIR' ? this.i18n.t('common.actions.assignTechnician') : this.i18n.t('common.actions.complete');
            case 'IN_PROGRESS': return this.i18n.t('common.actions.complete');
            case 'COMPLETED': return this.i18n.t('common.actions.close');
            case 'REJECTED': return this.i18n.t('warrantyClaims.statuses.rejected');
            case 'CLOSED': return this.i18n.t('warrantyClaims.statuses.closed');
            default: return 'Next Step';
        }
    }

    getQuickFlowIcon(claim: WarrantyClaimResponse): string {
        switch (claim.status) {
            case 'PENDING': return 'pi pi-send';
            case 'UNDER_REVIEW': return 'pi pi-check';
            case 'APPROVED': return claim.serviceType === 'REPAIR' ? 'pi pi-user-plus' : 'pi pi-check-circle';
            case 'IN_PROGRESS': return 'pi pi-check-circle';
            case 'COMPLETED': return 'pi pi-lock';
            default: return 'pi pi-step-forward';
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
                        detail: this.i18n.t('warrantyClaims.messages.nextStepInfo')
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
                    detail: this.i18n.t('warrantyClaims.messages.noNextStep')
                });
                return;
        }
    }

    private quickApprove(claim: WarrantyClaimResponse): void {
        this.warrantyService.approveClaim(claim.id, { approvedBy: this.currentUsername }).subscribe({
            next: () => {
                this.messageService.add({ severity: 'success', summary: this.i18n.t('common.messages.success'), detail: this.i18n.t('warrantyClaims.messages.quickApproveSuccess') });
                this.loadClaims();
            },
            error: (error: any) => {
                const msg = typeof error?.error === 'string' ? error.error : (error?.error?.message || this.i18n.t('warrantyClaims.messages.approveFailed'));
                this.messageService.add({ severity: 'error', summary: this.i18n.t('common.messages.error'), detail: msg });
            }
        });
    }

    private quickComplete(claim: WarrantyClaimResponse): void {
        const resolution = claim.serviceType === 'REPLACEMENT'
            ? 'Replacement completed and item handed to customer.'
            : 'Service completed and item handed to customer.';

        this.warrantyService.completeClaim(claim.id, { resolutionDetails: resolution }).subscribe({
            next: () => {
                this.messageService.add({ severity: 'success', summary: this.i18n.t('common.messages.success'), detail: this.i18n.t('warrantyClaims.messages.quickCompleteSuccess') });
                this.loadClaims();
            },
            error: (error: any) => {
                const msg = typeof error?.error === 'string' ? error.error : (error?.error?.message || this.i18n.t('warrantyClaims.messages.completeFailed'));
                this.messageService.add({ severity: 'error', summary: this.i18n.t('common.messages.error'), detail: msg });
            }
        });
    }

    private quickClose(claim: WarrantyClaimResponse): void {
        this.warrantyService.closeClaim(claim.id, {
            closureNotes: 'Closed via standard quick flow.'
        }).subscribe({
            next: () => {
                this.messageService.add({ severity: 'success', summary: this.i18n.t('common.messages.success'), detail: this.i18n.t('warrantyClaims.messages.quickCloseSuccess') });
                this.loadClaims();
            },
            error: (error: any) => {
                const msg = typeof error?.error === 'string' ? error.error : (error?.error?.message || this.i18n.t('warrantyClaims.messages.closeFailed'));
                this.messageService.add({ severity: 'error', summary: this.i18n.t('common.messages.error'), detail: msg });
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
            this.messageService.add({ severity: 'warn', summary: this.i18n.t('common.messages.warning'), detail: this.i18n.t('warrantyClaims.messages.validationSelectWarranty') });
            return;
        }
        if (!this.newClaim.issueDescription.trim()) {
            this.messageService.add({ severity: 'warn', summary: this.i18n.t('common.messages.warning'), detail: this.i18n.t('warrantyClaims.messages.validationIssueDescription') });
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
                this.messageService.add({ severity: 'success', summary: this.i18n.t('common.messages.success'), detail: this.i18n.t('warrantyClaims.messages.createSuccess') });
                this.showCreateDialog = false;
                this.loadClaims();
            },
            error: (error: any) => {
                const msg = typeof error?.error === 'string' ? error.error : (error?.error?.message || this.i18n.t('warrantyClaims.messages.createFailed'));
                this.messageService.add({ severity: 'error', summary: this.i18n.t('common.messages.error'), detail: msg });
            }
        });
    }
}
