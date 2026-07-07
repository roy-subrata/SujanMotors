import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { PayrollService, PayrollRunResponse, PayslipResponse } from '../../services/payroll.service';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { InputNumberModule } from 'primeng/inputnumber';
import { Select } from 'primeng/select';
import { DialogModule } from 'primeng/dialog';
import { TooltipModule } from 'primeng/tooltip';
import { ToastModule } from 'primeng/toast';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { MessageService, ConfirmationService } from 'primeng/api';
import { PageContainerComponent } from '@/shared/components/page-container/page-container.component';
import { PageHeaderComponent } from '@/shared/components/page-header/page-header.component';

@Component({
    selector: 'app-payroll-run-detail',
    standalone: true,
    imports: [CommonModule, FormsModule, TableModule, ButtonModule, InputTextModule, InputNumberModule, Select,
        DialogModule, TooltipModule, ToastModule, ConfirmDialogModule,
        PageContainerComponent, PageHeaderComponent],
    providers: [MessageService, ConfirmationService],
    templateUrl: './payroll-run-detail.component.html',
    styleUrls: ['./payroll-run-detail.component.css']
})
export class PayrollRunDetailComponent implements OnInit {
    private readonly payrollService = inject(PayrollService);
    private readonly router = inject(Router);
    private readonly route = inject(ActivatedRoute);
    private readonly messageService = inject(MessageService);
    private readonly confirmationService = inject(ConfirmationService);

    run: PayrollRunResponse | null = null;
    loading = false;
    savingPayslipId: string | null = null;
    approving = false;
    paying = false;

    payDialogVisible = false;
    paymentMethod = 'CASH';
    paymentMethodOptions = [
        { label: 'Cash', value: 'CASH' },
        { label: 'Bank Transfer', value: 'BANK_TRANSFER' },
        { label: 'Check', value: 'CHECK' }
    ];

    monthNames = ['January', 'February', 'March', 'April', 'May', 'June',
        'July', 'August', 'September', 'October', 'November', 'December'];

    get isDraft(): boolean {
        return this.run?.status === 'DRAFT';
    }

    get monthLabel(): string {
        return this.run ? `${this.monthNames[this.run.month - 1]} ${this.run.year}` : '';
    }

    ngOnInit(): void {
        this.route.queryParams.subscribe(params => {
            const id = params['id'];
            if (id) {
                this.loadRun(id);
            } else {
                this.back();
            }
        });
    }

    loadRun(id: string): void {
        this.loading = true;
        this.payrollService.getRun(id).subscribe({
            next: (run) => {
                this.run = run;
                this.loading = false;
            },
            error: (err) => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: err?.error?.message || 'Failed to load payroll run'
                });
                console.error('Error loading payroll run:', err);
                this.loading = false;
            }
        });
    }

    savePayslip(payslip: PayslipResponse): void {
        if (!this.run) return;

        this.savingPayslipId = payslip.id;
        this.payrollService.updatePayslip(this.run.id, payslip.id, {
            overtimeAmount: payslip.overtimeAmount || 0,
            bonusAmount: payslip.bonusAmount || 0,
            otherAllowance: payslip.otherAllowance || 0,
            advanceDeduction: payslip.advanceDeduction || 0,
            otherDeduction: payslip.otherDeduction || 0,
            adjustmentNotes: payslip.adjustmentNotes || ''
        }).subscribe({
            next: (run) => {
                this.run = run;
                this.savingPayslipId = null;
                this.messageService.add({
                    severity: 'success',
                    summary: 'Saved',
                    detail: `Payslip updated for ${payslip.employeeName}`
                });
            },
            error: (err) => {
                this.savingPayslipId = null;
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: err?.error?.message || 'Failed to update payslip'
                });
                console.error('Error updating payslip:', err);
            }
        });
    }

    approve(): void {
        if (!this.run) return;

        this.confirmationService.confirm({
            message: `Approve payroll for ${this.monthLabel} (${this.run.employeeCount} employees, net ${this.run.totalNet.toFixed(2)} ${this.run.currency})? Payslips will be locked.`,
            header: 'Approve Payroll',
            icon: 'pi pi-check-circle',
            accept: () => {
                this.approving = true;
                this.payrollService.approve(this.run!.id).subscribe({
                    next: (run) => {
                        this.run = run;
                        this.approving = false;
                        this.messageService.add({ severity: 'success', summary: 'Success', detail: 'Payroll approved' });
                    },
                    error: (err) => {
                        this.approving = false;
                        this.messageService.add({
                            severity: 'error',
                            summary: 'Error',
                            detail: err?.error?.message || 'Failed to approve payroll'
                        });
                    }
                });
            }
        });
    }

    openPay(): void {
        this.paymentMethod = 'CASH';
        this.payDialogVisible = true;
    }

    pay(): void {
        if (!this.run) return;

        this.paying = true;
        this.payrollService.pay(this.run.id, this.paymentMethod).subscribe({
            next: (run) => {
                this.run = run;
                this.paying = false;
                this.payDialogVisible = false;
                this.messageService.add({
                    severity: 'success',
                    summary: 'Success',
                    detail: 'Payroll paid — salary expense recorded in the cash book'
                });
            },
            error: (err) => {
                this.paying = false;
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: err?.error?.message || 'Failed to record payment'
                });
                console.error('Error paying payroll:', err);
            }
        });
    }

    print(): void {
        window.print();
    }

    back(): void {
        this.router.navigate(['/hr/payroll']);
    }
}
