import { Component, OnInit, inject, signal, computed, DestroyRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { I18nService } from '../../../shared/services/i18n.service';
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
import { MessageService, ConfirmationService } from 'primeng/api';
import { CurrencyService, Currency, ExchangeRate } from '../../../shared/services/currency.service';

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
    InputTextModule
  ],
  providers: [MessageService, ConfirmationService],
  template: `
    <div class="container mx-auto px-4 py-6">
      <p-toast></p-toast>
      <p-confirmDialog></p-confirmDialog>

      <!-- Header -->
      <div class="flex justify-between items-center mb-6">
        <div>
          <h1 class="text-3xl font-bold text-gray-800">Exchange Rate Management</h1>
          <p class="text-gray-600 mt-2">Manage currency exchange rates</p>
        </div>
        <button
          pButton
          type="button"
          label="Add Exchange Rate"
          icon="pi pi-plus"
          class="p-button-success"
          (click)="openDialog()">
        </button>
      </div>

      <!-- Exchange Rates Table -->
      <p-table
        [value]="exchangeRates()"
        [loading]="loading()"
        [paginator]="true"
        [rows]="15"
        [rowsPerPageOptions]="[15, 30, 50]"
        responsiveLayout="scroll"
        styleClass="p-datatable-gridlines">

        <ng-template pTemplate="header">
          <tr>
            <th>From Currency</th>
            <th>To Currency</th>
            <th>Rate</th>
            <th>Effective Date</th>
            <th>Expiry Date</th>
            <th>Source</th>
            <th>Status</th>
            <th>Actions</th>
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
              <span *ngIf="!rate.expiryDate" class="text-gray-400">No expiry</span>
            </td>
            <td>
              <span class="px-2 py-1 rounded text-xs font-semibold bg-gray-100 text-gray-800">
                {{ rate.source }}
              </span>
            </td>
            <td>
              <span
                class="px-2 py-1 rounded text-xs font-semibold"
                [class.bg-green-100]="rate.isActive"
                [class.text-green-800]="rate.isActive"
                [class.bg-red-100]="!rate.isActive"
                [class.text-red-800]="!rate.isActive">
                {{ rate.isActive ? 'Active' : 'Inactive' }}
              </span>
            </td>
            <td>
              <div class="flex gap-2">
                <button
                  pButton
                  type="button"
                  icon="pi pi-pencil"
                  class="p-button-rounded p-button-text p-button-sm"
                  pTooltip="Edit"
                  (click)="openDialog(rate)">
                </button>
                <button
                  pButton
                  type="button"
                  icon="pi pi-trash"
                  class="p-button-rounded p-button-text p-button-sm p-button-danger"
                  pTooltip="Delete"
                  (click)="confirmDelete(rate)">
                </button>
              </div>
            </td>
          </tr>
        </ng-template>

        <ng-template pTemplate="emptymessage">
          <tr>
            <td colspan="8" class="text-center py-8 text-gray-500">
              No exchange rates found
            </td>
          </tr>
        </ng-template>
      </p-table>

      <!-- Exchange Rate Dialog -->
      <p-dialog
        [(visible)]="dialogVisible"
        [header]="isEditing ? 'Edit Exchange Rate' : 'Add Exchange Rate'"
        [modal]="true"
        [style]="{width: '600px'}"
        [closable]="true">

        <form [formGroup]="exchangeRateForm" (ngSubmit)="saveExchangeRate()">
          <div class="flex flex-column gap-4">
            <!-- From Currency -->
            <div class="flex flex-column gap-2">
              <label htmlFor="fromCurrencyId">From Currency <span class="text-red-500">*</span></label>
              <p-select
                id="fromCurrencyId"
                formControlName="fromCurrencyId"
                [options]="currencies()"
                optionLabel="code"
                optionValue="id"
                placeholder="Select currency"
                [filter]="true"
                class="w-full">
                <ng-template let-currency pTemplate="item">
                  <div class="flex align-items-center gap-2">
                    <span>{{ currency.symbol }}</span>
                    <span class="font-semibold">{{ currency.code }}</span>
                    <span class="text-sm text-gray-600">- {{ currency.name }}</span>
                  </div>
                </ng-template>
              </p-select>
              <small class="text-red-500" *ngIf="exchangeRateForm.get('fromCurrencyId')?.invalid && exchangeRateForm.get('fromCurrencyId')?.touched">
                From currency is required
              </small>
            </div>

            <!-- To Currency -->
            <div class="flex flex-column gap-2">
              <label htmlFor="toCurrencyId">To Currency <span class="text-red-500">*</span></label>
              <p-select
                id="toCurrencyId"
                formControlName="toCurrencyId"
                [options]="currencies()"
                optionLabel="code"
                optionValue="id"
                placeholder="Select currency"
                [filter]="true"
                class="w-full">
                <ng-template let-currency pTemplate="item">
                  <div class="flex align-items-center gap-2">
                    <span>{{ currency.symbol }}</span>
                    <span class="font-semibold">{{ currency.code }}</span>
                    <span class="text-sm text-gray-600">- {{ currency.name }}</span>
                  </div>
                </ng-template>
              </p-select>
              <small class="text-red-500" *ngIf="exchangeRateForm.get('toCurrencyId')?.invalid && exchangeRateForm.get('toCurrencyId')?.touched">
                To currency is required
              </small>
            </div>

            <!-- Exchange Rate -->
            <div class="flex flex-column gap-2">
              <label htmlFor="rate">Exchange Rate <span class="text-red-500">*</span></label>
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
                Exchange rate is required and must be greater than 0
              </small>
            </div>

            <!-- Effective Date -->
            <div class="flex flex-column gap-2">
              <label htmlFor="effectiveDate">Effective Date <span class="text-red-500">*</span></label>
              <p-datepicker
                id="effectiveDate"
                formControlName="effectiveDate"
                [showIcon]="true"
                dateFormat="yy-mm-dd"
                placeholder="Select date"
                styleClass="w-full">
              </p-datepicker>
              <small class="text-red-500" *ngIf="exchangeRateForm.get('effectiveDate')?.invalid && exchangeRateForm.get('effectiveDate')?.touched">
                Effective date is required
              </small>
            </div>

            <!-- Expiry Date -->
            <div class="flex flex-column gap-2">
              <label htmlFor="expiryDate">Expiry Date (Optional)</label>
              <p-datepicker
                id="expiryDate"
                formControlName="expiryDate"
                [showIcon]="true"
                dateFormat="yy-mm-dd"
                placeholder="Select date"
                [showClear]="true"
                styleClass="w-full">
              </p-datepicker>
            </div>

            <!-- Is Active -->
            <div class="flex align-items-center gap-2">
              <p-checkbox
                id="isActive"
                formControlName="isActive"
                [binary]="true">
              </p-checkbox>
              <label htmlFor="isActive">Active</label>
            </div>

            <!-- Notes -->
            <div class="flex flex-column gap-2">
              <label htmlFor="notes">Notes</label>
              <textarea
                pInputTextarea
                id="notes"
                formControlName="notes"
                rows="3"
                placeholder="Enter any notes about this exchange rate..."
                class="w-full">
              </textarea>
            </div>
          </div>

          <div class="flex justify-end gap-2 mt-4">
            <button
              pButton
              type="button"
              label="Cancel"
              class="p-button-text"
              (click)="dialogVisible = false">
            </button>
            <button
              pButton
              type="submit"
              [label]="isEditing ? 'Update' : 'Create'"
              [loading]="saving()"
              [disabled]="exchangeRateForm.invalid">
            </button>
          </div>
        </form>
      </p-dialog>
    </div>
  `,
  styles: [`
    :host ::ng-deep {
      .p-datatable .p-datatable-thead > tr > th {
        background-color: var(--surface-ground);
        font-weight: 600;
      }
    }
  `]
})
export class ExchangeRatesListComponent implements OnInit {
  private currencyService = inject(CurrencyService);
  private messageService = inject(MessageService);
  private confirmationService = inject(ConfirmationService);
  private fb = inject(FormBuilder);
  private readonly i18n = inject(I18nService);
  private readonly destroyRef = inject(DestroyRef);

  exchangeRates = signal<ExchangeRate[]>([]);
  currencies = signal<Currency[]>([]);
  loading = signal(false);
  saving = signal(false);
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
      effectiveDate: formValue.effectiveDate,
      expiryDate: formValue.expiryDate,
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
}
