import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { PayrollService, PayrollRunResponse } from '../../services/payroll.service';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { Select } from 'primeng/select';
import { DialogModule } from 'primeng/dialog';
import { TooltipModule } from 'primeng/tooltip';
import { ToastModule } from 'primeng/toast';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { MessageService, ConfirmationService } from 'primeng/api';
import { PageContainerComponent } from '@/shared/components/page-container/page-container.component';
import { PageHeaderComponent } from '@/shared/components/page-header/page-header.component';

@Component({
    selector: 'app-payroll-runs',
    standalone: true,
    imports: [CommonModule, FormsModule, TableModule, ButtonModule, Select, DialogModule, TooltipModule, ToastModule, ConfirmDialogModule,
        PageContainerComponent, PageHeaderComponent],
    providers: [MessageService, ConfirmationService],
    templateUrl: './payroll-runs.component.html',
    styleUrls: ['./payroll-runs.component.css']
})
export class PayrollRunsComponent implements OnInit {
    private readonly payrollService = inject(PayrollService);
    private readonly router = inject(Router);
    private readonly messageService = inject(MessageService);
    private readonly confirmationService = inject(ConfirmationService);

    runs: PayrollRunResponse[] = [];
    loading = false;

    dialogVisible = false;
    generating = false;
    genYear = new Date().getFullYear();
    genMonth = new Date().getMonth() + 1;

    yearOptions: { label: string; value: number }[] = [];
    monthOptions = [
        { label: 'January', value: 1 }, { label: 'February', value: 2 }, { label: 'March', value: 3 },
        { label: 'April', value: 4 }, { label: 'May', value: 5 }, { label: 'June', value: 6 },
        { label: 'July', value: 7 }, { label: 'August', value: 8 }, { label: 'September', value: 9 },
        { label: 'October', value: 10 }, { label: 'November', value: 11 }, { label: 'December', value: 12 }
    ];

    ngOnInit(): void {
        const current = new Date().getFullYear();
        for (let y = current - 2; y <= current; y++) {
            this.yearOptions.push({ label: String(y), value: y });
        }
        this.loadRuns();
    }

    loadRuns(): void {
        this.loading = true;
        this.payrollService.getRuns().subscribe({
            next: (runs) => {
                this.runs = runs;
                this.loading = false;
            },
            error: (err) => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: err?.error?.message || 'Failed to load payroll runs'
                });
                console.error('Error loading payroll runs:', err);
                this.loading = false;
            }
        });
    }

    monthLabel(run: PayrollRunResponse): string {
        return `${this.monthOptions[run.month - 1]?.label} ${run.year}`;
    }

    openGenerate(): void {
        this.genYear = new Date().getFullYear();
        this.genMonth = new Date().getMonth() + 1;
        this.dialogVisible = true;
    }

    generate(): void {
        this.generating = true;
        this.payrollService.generate(this.genYear, this.genMonth).subscribe({
            next: (run) => {
                this.generating = false;
                this.dialogVisible = false;
                this.messageService.add({
                    severity: 'success',
                    summary: 'Success',
                    detail: `Draft payroll generated for ${this.monthLabel(run)}`
                });
                this.router.navigate(['/hr/payroll/view'], { queryParams: { id: run.id } });
            },
            error: (err) => {
                this.generating = false;
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: err?.error?.message || 'Failed to generate payroll'
                });
                console.error('Error generating payroll:', err);
            }
        });
    }

    viewRun(run: PayrollRunResponse): void {
        this.router.navigate(['/hr/payroll/view'], { queryParams: { id: run.id } });
    }

    deleteRun(run: PayrollRunResponse): void {
        this.confirmationService.confirm({
            message: `Delete ${run.status.toLowerCase()} payroll run ${run.runCode} (${this.monthLabel(run)})?`,
            header: 'Confirm',
            icon: 'pi pi-exclamation-triangle',
            accept: () => {
                this.payrollService.deleteRun(run.id).subscribe({
                    next: () => {
                        this.messageService.add({ severity: 'success', summary: 'Success', detail: 'Payroll run deleted' });
                        this.loadRuns();
                    },
                    error: (err) => {
                        this.messageService.add({
                            severity: 'error',
                            summary: 'Error',
                            detail: err?.error?.message || 'Failed to delete payroll run'
                        });
                    }
                });
            }
        });
    }
}
