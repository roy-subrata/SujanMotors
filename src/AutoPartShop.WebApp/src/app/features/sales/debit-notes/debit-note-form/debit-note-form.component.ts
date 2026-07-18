import { Component, OnDestroy, OnInit, ViewChild, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule, FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { CardModule } from 'primeng/card';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { InputNumberModule } from 'primeng/inputnumber';
import { TextareaModule } from 'primeng/textarea';
import { TooltipModule } from 'primeng/tooltip';
import { ToastModule } from 'primeng/toast';
import { MessageService } from 'primeng/api';
import { Subject, takeUntil, map } from 'rxjs';

import { LazyAutocompleteComponent, LazyRequest, LazyResponse } from '@/shared/components/lazy-autocomplete';
import { CurrencySelectorComponent } from '@/shared/components/currency-selector/currency-selector.component';
import { CurrencyService } from '@/shared/services/currency.service';
import { CustomerService, CustomerResponse } from '../../services/customer.service';
import { InvoiceService, InvoiceResponse } from '../../services/invoice.service';
import {
    CustomerDebitNoteService,
    CreateCustomerDebitNoteRequest
} from '../../services/customer-debit-note.service';

@Component({
    selector: 'app-debit-note-form',
    standalone: true,
    imports: [
        CommonModule,
        ReactiveFormsModule,
        FormsModule,
        CardModule,
        ButtonModule,
        InputTextModule,
        InputNumberModule,
        TextareaModule,
        TooltipModule,
        ToastModule,
        CurrencySelectorComponent,
        LazyAutocompleteComponent
    ],
    providers: [MessageService],
    templateUrl: './debit-note-form.component.html',
    styleUrls: ['./debit-note-form.component.scss']
})
export class DebitNoteFormComponent implements OnInit, OnDestroy {
    private readonly fb = inject(FormBuilder);
    private readonly router = inject(Router);
    private readonly debitNoteService = inject(CustomerDebitNoteService);
    private readonly customerService = inject(CustomerService);
    private readonly invoiceService = inject(InvoiceService);
    private readonly currencyService = inject(CurrencyService);
    private readonly messageService = inject(MessageService);

    @ViewChild('invoicePicker') invoicePicker?: LazyAutocompleteComponent<InvoiceResponse>;

    private readonly destroy$ = new Subject<void>();

    debitNoteForm!: FormGroup;
    saving = signal(false);
    error = signal<string | null>(null);

    // Customer selection (required)
    selectedCustomer: CustomerResponse | null = null;
    selectedCustomerId = '';

    // Invoice selection (optional reference)
    selectedInvoice: InvoiceResponse | null = null;

    searchCustomers = (req: LazyRequest) => {
        return this.customerService
            .getCustomers({ search: req.search, pageNumber: req.pageNumber, pageSize: req.pageSize })
            .pipe(
                map((response) => ({ items: response.data, totalCount: response.pagination.totalCount }) as LazyResponse<CustomerResponse>)
            );
    };

    searchInvoices = (req: LazyRequest) => {
        return this.invoiceService
            .getAllInvoices(req.pageNumber, req.pageSize, {
                searchTerm: req.search,
                customerId: this.selectedCustomerId || undefined
            })
            .pipe(
                map((response) => ({ items: response.data, totalCount: response.pagination.totalCount }) as LazyResponse<InvoiceResponse>)
            );
    };

    ngOnInit(): void {
        this.initializeForm();
    }

    ngOnDestroy(): void {
        this.destroy$.next();
        this.destroy$.complete();
    }

    initializeForm(): void {
        const defaultCurrency = this.currencyService.selectedCurrency();

        this.debitNoteForm = this.fb.group({
            amount: [null, [Validators.required, Validators.min(0.01)]],
            reason: ['', [Validators.required, Validators.minLength(3)]],
            currency: [defaultCurrency, [Validators.required]],
            notes: ['']
        });
    }

    // ── Customer selection ──────────────────────────────────────────────
    onCustomerSelected(customer: CustomerResponse): void {
        this.selectedCustomer = customer;
        this.selectedCustomerId = customer.id;
        // A previously selected invoice may no longer belong to the new customer.
        this.selectedInvoice = null;
        this.invoicePicker?.refresh();
    }

    onCustomerCleared(): void {
        this.selectedCustomer = null;
        this.selectedCustomerId = '';
        this.selectedInvoice = null;
        this.invoicePicker?.refresh();
    }

    // ── Invoice selection (optional) ────────────────────────────────────
    onInvoiceSelected(invoice: InvoiceResponse): void {
        this.selectedInvoice = invoice;
    }

    onInvoiceCleared(): void {
        this.selectedInvoice = null;
    }

    onSubmit(): void {
        if (!this.selectedCustomerId) {
            this.error.set('Please select a customer from the dropdown');
            return;
        }

        if (this.debitNoteForm.invalid) {
            Object.keys(this.debitNoteForm.controls).forEach((key) => {
                const control = this.debitNoteForm.get(key);
                if (control?.invalid) control.markAsTouched();
            });
            this.error.set('Please fill in all required fields');
            return;
        }

        this.saving.set(true);
        this.error.set(null);

        const formValue = this.debitNoteForm.value;
        const request: CreateCustomerDebitNoteRequest = {
            customerId: this.selectedCustomerId,
            invoiceId: this.selectedInvoice?.id ?? null,
            amount: formValue.amount,
            reason: formValue.reason,
            currency: formValue.currency,
            notes: formValue.notes
        };

        this.debitNoteService.create(request).pipe(takeUntil(this.destroy$)).subscribe({
            next: (debitNote) => {
                this.messageService.add({
                    severity: 'success',
                    summary: 'Success',
                    detail: `Debit note ${debitNote.debitNoteNumber} created successfully!`
                });
                this.router.navigate(['/sales/debit-notes']);
            },
            error: (err) => {
                let errorMessage = 'Failed to create debit note';
                if (err.error?.message) errorMessage = err.error.message;
                else if (err.error?.errors) errorMessage = Object.values(err.error.errors).flat().join(', ');
                else if (err.message) errorMessage = err.message;

                this.error.set(errorMessage);
                this.saving.set(false);
                console.error('Error creating debit note:', err);
            }
        });
    }

    cancel(): void {
        this.router.navigate(['/sales/debit-notes']);
    }
}
