import { Component, OnInit, inject, signal, computed, DestroyRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { I18nService } from '../../../shared/services/i18n.service';
import { TranslatePipe } from '../../../shared/pipes/translate.pipe';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Router } from '@angular/router';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { InputNumberModule } from 'primeng/inputnumber';
import { DatePickerModule } from 'primeng/datepicker';
import { SelectModule } from 'primeng/select';
import { ToastModule } from 'primeng/toast';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { CheckboxModule } from 'primeng/checkbox';
import { InputTextModule } from 'primeng/inputtext';
import { TooltipModule } from 'primeng/tooltip';
import { MessageService, ConfirmationService } from 'primeng/api';
import { CurrencyService, Currency, ExchangeRate } from '../../../shared/services/currency.service';
import { PageContainerComponent } from '@/shared/components/page-container/page-container.component';
import { PageHeaderComponent } from '@/shared/components/page-header/page-header.component';
import { DataPaginationComponent } from '@/shared/components/data-pagination/data-pagination.component';

@Component({
  selector: 'app-exchange-rates-list',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    TableModule,
    ButtonModule,
    DialogModule,
    InputNumberModule,
    DatePickerModule,
    SelectModule,
    ToastModule,
    ConfirmDialogModule,
    CheckboxModule,
    InputTextModule,
    TooltipModule,
    PageContainerComponent,
    PageHeaderComponent,
    DataPaginationComponent,
    TranslatePipe
  ],
  providers: [MessageService, ConfirmationService],
  templateUrl: './exchange-rates-list.component.html',
  styleUrl: './exchange-rates-list.component.scss',
})
export class ExchangeRatesListComponent implements OnInit {
  private currencyService = inject(CurrencyService);
  private messageService = inject(MessageService);
  private confirmationService = inject(ConfirmationService);
  private fb = inject(FormBuilder);
  readonly i18n = inject(I18nService);
  private readonly destroyRef = inject(DestroyRef);

  exchangeRates = signal<ExchangeRate[]>([]);
  currencies = signal<Currency[]>([]);
  loading = signal(false);
  saving = signal(false);

  first = signal(0);
  pageSize = signal(15);
  pagedExchangeRates = computed(() => this.exchangeRates().slice(this.first(), this.first() + this.pageSize()));
  goToPage(page: number): void { this.first.set((page - 1) * this.pageSize()); }
  onPageSizeChange(size: number): void { this.pageSize.set(size); this.first.set(0); }
  dialogVisible = false;
  isEditing = false;
  currentRateId: string | null = null;

  exchangeRateForm: FormGroup = this.fb.group({
    fromCurrencyId: ['', Validators.required],
    toCurrencyId: ['', Validators.required],
    rate: [0, [Validators.required, Validators.min(0.000001)]],
    effectiveDate: [new Date(), Validators.required],
    expiryDate: [null],
    isActive: [true],
    notes: ['']
  });

  ngOnInit(): void {
    this.loadCurrencies();
    this.loadExchangeRates();
  }

  loadCurrencies(): void {
    this.currencyService.getAllCurrencies().subscribe({
      next: (currencies) => {
        this.currencies.set(currencies);
      },
      error: (error) => {
        this.messageService.add({
          severity: 'error',
          summary: this.i18n.t('common.messages.error'),
          detail: this.i18n.t('exchangeRates.messages.loadCurrenciesFailed')
        });
      }
    });
  }

  loadExchangeRates(): void {
    this.loading.set(true);
    this.currencyService.getAllExchangeRates().subscribe({
      next: (rates) => {
        this.exchangeRates.set(rates);
        this.loading.set(false);
      },
      error: (error) => {
        this.messageService.add({
          severity: 'error',
          summary: this.i18n.t('common.messages.error'),
          detail: this.i18n.t('exchangeRates.messages.loadFailed')
        });
        this.loading.set(false);
      }
    });
  }

  openDialog(rate?: ExchangeRate): void {
    this.isEditing = !!rate;
    this.currentRateId = rate?.id || null;

    if (rate) {
      this.exchangeRateForm.patchValue({
        fromCurrencyId: rate.fromCurrencyId,
        toCurrencyId: rate.toCurrencyId,
        rate: rate.rate,
        effectiveDate: new Date(rate.effectiveDate),
        expiryDate: rate.expiryDate ? new Date(rate.expiryDate) : null,
        isActive: rate.isActive,
        notes: rate.notes
      });
    } else {
      this.exchangeRateForm.reset({
        fromCurrencyId: '',
        toCurrencyId: '',
        rate: 0,
        effectiveDate: new Date(),
        expiryDate: null,
        isActive: true,
        notes: ''
      });
    }

    this.dialogVisible = true;
  }

  saveExchangeRate(): void {
    if (this.exchangeRateForm.invalid) {
      this.exchangeRateForm.markAllAsTouched();
      return;
    }

    this.saving.set(true);
    const formValue = this.exchangeRateForm.value;

    const request = {
      fromCurrencyId: formValue.fromCurrencyId,
      toCurrencyId: formValue.toCurrencyId,
      rate: formValue.rate,
      // Build date-only strings from local date components. Passing the raw Date
      // object would have HttpClient's JSON.stringify() call Date#toJSON() (i.e.
      // toISOString()), which shifts the picked calendar day back a day in any
      // timezone ahead of UTC (e.g. Aug 1 local midnight becomes Jul 31 in UTC+6).
      effectiveDate: this.toLocalDateString(formValue.effectiveDate),
      expiryDate: formValue.expiryDate ? this.toLocalDateString(formValue.expiryDate) : undefined,
      notes: formValue.notes || '',
      isActive: formValue.isActive
    };

    const operation = this.isEditing
      ? this.currencyService.updateExchangeRate(this.currentRateId!, request)
      : this.currencyService.createExchangeRate(request);

    operation.subscribe({
      next: () => {
        this.messageService.add({
          severity: 'success',
          summary: this.i18n.t('common.messages.success'),
          detail: this.i18n.t(this.isEditing ? 'exchangeRates.messages.updateSuccess' : 'exchangeRates.messages.createSuccess')
        });
        this.dialogVisible = false;
        this.loadExchangeRates();
        this.saving.set(false);
      },
      error: (error) => {
        this.messageService.add({
          severity: 'error',
          summary: this.i18n.t('common.messages.error'),
          detail: this.i18n.t(this.isEditing ? 'common.messages.updateFailed' : 'common.messages.createFailed')
        });
        this.saving.set(false);
      }
    });
  }

  confirmDelete(rate: ExchangeRate): void {
    this.confirmationService.confirm({
      message: this.i18n.t('exchangeRates.messages.deleteConfirm', { from: rate.fromCurrencyCode, to: rate.toCurrencyCode }),
      header: this.i18n.t('common.actions.confirm'),
      icon: 'pi pi-exclamation-triangle',
      accept: () => {
        this.currencyService.deleteExchangeRate(rate.id).subscribe({
          next: () => {
            this.messageService.add({
              severity: 'success',
              summary: this.i18n.t('common.messages.success'),
              detail: this.i18n.t('exchangeRates.messages.deleteSuccess')
            });
            this.loadExchangeRates();
          },
          error: (error) => {
            this.messageService.add({
              severity: 'error',
              summary: this.i18n.t('common.messages.error'),
              detail: this.i18n.t('exchangeRates.messages.deleteFailed')
            });
          }
        });
      }
    });
  }

  // toISOString() converts to UTC which shifts dates in non-UTC timezones (e.g. local
  // midnight Aug 1 in UTC+6 becomes Jul 31 in UTC). This helper returns "YYYY-MM-DD" in
  // local time so the backend receives the calendar day the user actually picked.
  private toLocalDateString(date: Date): string {
    const y = date.getFullYear();
    const m = String(date.getMonth() + 1).padStart(2, '0');
    const d = String(date.getDate()).padStart(2, '0');
    return `${y}-${m}-${d}`;
  }
}
