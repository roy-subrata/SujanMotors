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
  template: `
    <p-toast></p-toast>
    <p-confirmDialog></p-confirmDialog>

    <app-page-container>
      <app-page-header
        [title]="i18n.t('exchangeRates.title')"
        [subtitle]="i18n.t('exchangeRates.subtitle')"
        [count]="exchangeRates().length" [countLabel]="i18n.t('exchangeRates.countLabel')" countIcon="pi pi-sync">
        <ng-container actions>
          <button class="btn-primary" (click)="openDialog()">
            <i class="pi pi-plus"></i><span>{{ i18n.t('exchangeRates.addRate') }}</span>
          </button>
        </ng-container>
      </app-page-header>

      <section class="table-section desktop-only">
        <div class="table-container">
      <p-table
        [value]="pagedExchangeRates()"
        [loading]="loading()"
        [paginator]="false"
        [scrollable]="true"
        styleClass="app-table">

        <ng-template pTemplate="header">
          <tr>
            <th>{{ i18n.t('exchangeRates.table.fromCurrency') }}</th>
            <th>{{ i18n.t('exchangeRates.table.toCurrency') }}</th>
            <th>{{ i18n.t('exchangeRates.table.rate') }}</th>
            <th>{{ i18n.t('exchangeRates.table.effectiveDate') }}</th>
            <th>{{ i18n.t('exchangeRates.table.expiryDate') }}</th>
            <th>{{ i18n.t('exchangeRates.table.source') }}</th>
            <th>{{ i18n.t('common.labels.status') }}</th>
            <th>{{ i18n.t('common.labels.actions') }}</th>
          </tr>
        </ng-template>

        <ng-template pTemplate="body" let-rate>
          <tr>
            <td>
              <span class="font-mono font-semibold">{{ rate.fromCurrencyCode }}</span>
            </td>
            <td>
              <span class="font-mono font-semibold">{{ rate.toCurrencyCode }}</span>
            </td>
            <td>
              <span class="font-semibold">{{ rate.rate | number: '1.2-6' }}</span>
            </td>
            <td>{{ rate.effectiveDate | date: 'mediumDate' }}</td>
            <td>
              <span *ngIf="rate.expiryDate">{{ rate.expiryDate | date: 'mediumDate' }}</span>
              <span *ngIf="!rate.expiryDate" class="text-gray-400">{{ i18n.t('exchangeRates.table.noExpiry') }}</span>
            </td>
            <td>
              <span class="px-2 py-1 rounded text-xs font-semibold bg-gray-100 text-gray-800">
                {{ rate.source }}
              </span>
            </td>
            <td>
              <span class="status-pill" [attr.data-status]="rate.isActive ? 'active' : 'inactive'">
                {{ (rate.isActive ? 'common.status.active' : 'common.status.inactive') | translate }}
              </span>
            </td>
            <td>
              <div class="flex gap-2">
                <button
                  pButton
                  type="button"
                  icon="pi pi-pencil"
                  class="p-button-rounded p-button-text p-button-sm"
                  [pTooltip]="i18n.t('common.actions.edit')"
                  (click)="openDialog(rate)">
                </button>
                <button
                  pButton
                  type="button"
                  icon="pi pi-trash"
                  class="p-button-rounded p-button-text p-button-sm p-button-danger"
                  [pTooltip]="i18n.t('common.actions.delete')"
                  (click)="confirmDelete(rate)">
                </button>
              </div>
            </td>
          </tr>
        </ng-template>

        <ng-template pTemplate="emptymessage">
          <tr>
            <td colspan="8" class="text-center py-8 text-gray-500">
              {{ i18n.t('exchangeRates.empty') }}
            </td>
          </tr>
        </ng-template>
      </p-table>
        </div>
      </section>

      <app-data-pagination
        [first]="first()"
        [pageSize]="pageSize()"
        [totalRecords]="exchangeRates().length"
        [itemLabel]="i18n.t('exchangeRates.countLabel')"
        (pageChange)="goToPage($event)"
        (pageSizeChange)="onPageSizeChange($event)">
      </app-data-pagination>

      <!-- Exchange Rate Dialog -->
      <p-dialog
        [(visible)]="dialogVisible"
        [header]="isEditing ? i18n.t('exchangeRates.dialog.editTitle') : i18n.t('exchangeRates.dialog.addTitle')"
        [modal]="true"
        [style]="{width: '600px'}"
        [closable]="true">

        <form [formGroup]="exchangeRateForm" (ngSubmit)="saveExchangeRate()">
          <div class="flex flex-col gap-4">
            <!-- From Currency -->
            <div class="flex flex-col gap-2">
              <label htmlFor="fromCurrencyId">{{ i18n.t('exchangeRates.form.fromCurrency') }} <span class="text-red-500">*</span></label>
              <p-select
                id="fromCurrencyId"
                formControlName="fromCurrencyId"
                [options]="currencies()"
                optionLabel="code"
                optionValue="id"
                [placeholder]="i18n.t('exchangeRates.form.selectCurrency')"
                [filter]="true"
                class="w-full">
                <ng-template let-currency pTemplate="item">
                  <div class="flex items-center gap-2">
                    <span>{{ currency.symbol }}</span>
                    <span class="font-semibold">{{ currency.code }}</span>
                    <span class="text-sm text-gray-600">- {{ currency.name }}</span>
                  </div>
                </ng-template>
              </p-select>
              <small class="text-red-500" *ngIf="exchangeRateForm.get('fromCurrencyId')?.invalid && exchangeRateForm.get('fromCurrencyId')?.touched">
                {{ i18n.t('exchangeRates.form.fromRequired') }}
              </small>
            </div>

            <!-- To Currency -->
            <div class="flex flex-col gap-2">
              <label htmlFor="toCurrencyId">{{ i18n.t('exchangeRates.form.toCurrency') }} <span class="text-red-500">*</span></label>
              <p-select
                id="toCurrencyId"
                formControlName="toCurrencyId"
                [options]="currencies()"
                optionLabel="code"
                optionValue="id"
                [placeholder]="i18n.t('exchangeRates.form.selectCurrency')"
                [filter]="true"
                class="w-full">
                <ng-template let-currency pTemplate="item">
                  <div class="flex items-center gap-2">
                    <span>{{ currency.symbol }}</span>
                    <span class="font-semibold">{{ currency.code }}</span>
                    <span class="text-sm text-gray-600">- {{ currency.name }}</span>
                  </div>
                </ng-template>
              </p-select>
              <small class="text-red-500" *ngIf="exchangeRateForm.get('toCurrencyId')?.invalid && exchangeRateForm.get('toCurrencyId')?.touched">
                {{ i18n.t('exchangeRates.form.toRequired') }}
              </small>
            </div>

            <!-- Exchange Rate -->
            <div class="flex flex-col gap-2">
              <label htmlFor="rate">{{ i18n.t('exchangeRates.form.rate') }} <span class="text-red-500">*</span></label>
              <p-inputNumber
                id="rate"
                formControlName="rate"
                [min]="0"
                [minFractionDigits]="2"
                [maxFractionDigits]="6"
                placeholder="0.00"
                class="w-full">
              </p-inputNumber>
              <small class="text-red-500" *ngIf="exchangeRateForm.get('rate')?.invalid && exchangeRateForm.get('rate')?.touched">
                {{ i18n.t('exchangeRates.form.rateRequired') }}
              </small>
            </div>

            <!-- Effective Date -->
            <div class="flex flex-col gap-2">
              <label htmlFor="effectiveDate">{{ i18n.t('exchangeRates.form.effectiveDate') }} <span class="text-red-500">*</span></label>
              <p-datepicker
                id="effectiveDate"
                formControlName="effectiveDate"
                [showIcon]="true"
                dateFormat="yy-mm-dd"
                [placeholder]="i18n.t('exchangeRates.form.selectDate')"
                styleClass="w-full">
              </p-datepicker>
              <small class="text-red-500" *ngIf="exchangeRateForm.get('effectiveDate')?.invalid && exchangeRateForm.get('effectiveDate')?.touched">
                {{ i18n.t('exchangeRates.form.effectiveDateRequired') }}
              </small>
            </div>

            <!-- Expiry Date -->
            <div class="flex flex-col gap-2">
              <label htmlFor="expiryDate">{{ i18n.t('exchangeRates.form.expiryDateOptional') }}</label>
              <p-datepicker
                id="expiryDate"
                formControlName="expiryDate"
                [showIcon]="true"
                dateFormat="yy-mm-dd"
                [placeholder]="i18n.t('exchangeRates.form.selectDate')"
                [showClear]="true"
                styleClass="w-full">
              </p-datepicker>
            </div>

            <!-- Is Active -->
            <div class="flex items-center gap-2">
              <p-checkbox
                id="isActive"
                formControlName="isActive"
                [binary]="true">
              </p-checkbox>
              <label htmlFor="isActive">{{ i18n.t('exchangeRates.form.active') }}</label>
            </div>

            <!-- Notes -->
            <div class="flex flex-col gap-2">
              <label htmlFor="notes">{{ i18n.t('common.labels.notes') }}</label>
              <textarea
                pInputTextarea
                id="notes"
                formControlName="notes"
                rows="3"
                [placeholder]="i18n.t('exchangeRates.form.notesPlaceholder')"
                class="w-full">
              </textarea>
            </div>
          </div>

          <div class="flex justify-end gap-2 mt-4">
            <button
              pButton
              type="button"
              [label]="i18n.t('common.actions.cancel')"
              class="p-button-text"
              (click)="dialogVisible = false">
            </button>
            <button
              pButton
              type="submit"
              [label]="isEditing ? i18n.t('common.actions.update') : i18n.t('common.actions.create')"
              [loading]="saving()"
              [disabled]="exchangeRateForm.invalid">
            </button>
          </div>
        </form>
      </p-dialog>
    </app-page-container>
  `,
  styles: [`
    :host ::ng-deep {
      .p-datatable .p-datatable-thead > tr > th {
        background-color: var(--surface-ground);
        font-weight: 600;
      }

      /* Tailwind gray-scale utility classes used above are static and don't
         flip under .app-dark — re-point them at the theme-aware --color-*
         tokens from assets/_data-page.scss. */
      .text-gray-400 { color: var(--color-text-muted) !important; }
      .text-gray-500 { color: var(--color-text-muted) !important; }
      .text-gray-600 { color: var(--color-text-secondary) !important; }
      .text-gray-800 { color: var(--color-text-primary) !important; }
      .bg-gray-100 { background-color: var(--color-bg-secondary) !important; }
    }
  `]
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
