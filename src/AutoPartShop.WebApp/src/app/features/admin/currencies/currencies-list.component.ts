import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { InputNumberModule } from 'primeng/inputnumber';
import { CheckboxModule } from 'primeng/checkbox';
import { ToastModule } from 'primeng/toast';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { MessageService, ConfirmationService } from 'primeng/api';
import { CurrencyService, Currency } from '../../../shared/services/currency.service';

@Component({
  selector: 'app-currencies-list',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    TableModule,
    ButtonModule,
    DialogModule,
    InputTextModule,
    InputNumberModule,
    CheckboxModule,
    ToastModule,
    ConfirmDialogModule
  ],
  providers: [MessageService, ConfirmationService],
  template: `
    <div class="container mx-auto px-4 py-6">
      <p-toast></p-toast>
      <p-confirmDialog></p-confirmDialog>

      <!-- Header -->
      <div class="flex justify-between items-center mb-6">
        <div>
          <h1 class="text-3xl font-bold text-gray-800">Currency Management</h1>
          <p class="text-gray-600 mt-2">Manage system currencies and exchange rates</p>
        </div>
        <button
          pButton
          type="button"
          label="Add Currency"
          icon="pi pi-plus"
          class="p-button-success"
          (click)="openDialog()">
        </button>
      </div>

      <!-- Currencies Table -->
      <p-table
        [value]="currencies()"
        [loading]="loading()"
        [paginator]="true"
        [rows]="10"
        [rowsPerPageOptions]="[10, 25, 50]"
        [globalFilterFields]="['code', 'name', 'symbol']"
        responsiveLayout="scroll"
        styleClass="p-datatable-gridlines">

        <ng-template pTemplate="header">
          <tr>
            <th>Code</th>
            <th>Name</th>
            <th>Symbol</th>
            <th>Decimal Places</th>
            <th>Status</th>
            <th>Base Currency</th>
            <th>Default Currency</th>
            <th>Display Order</th>
            <th>Actions</th>
          </tr>
        </ng-template>

        <ng-template pTemplate="body" let-currency>
          <tr>
            <td><span class="font-mono font-semibold">{{ currency.code }}</span></td>
            <td>{{ currency.name }}</td>
            <td><span class="text-lg">{{ currency.symbol }}</span></td>
            <td>{{ currency.decimalPlaces }}</td>
            <td>
              <span
                class="px-2 py-1 rounded text-xs font-semibold"
                [class.bg-green-100]="currency.isActive"
                [class.text-green-800]="currency.isActive"
                [class.bg-red-100]="!currency.isActive"
                [class.text-red-800]="!currency.isActive">
                {{ currency.isActive ? 'Active' : 'Inactive' }}
              </span>
            </td>
            <td>
              <span
                *ngIf="currency.isBaseCurrency"
                class="px-2 py-1 rounded text-xs font-semibold bg-blue-100 text-blue-800">
                <i class="pi pi-star-fill"></i> Base
              </span>
              <button
                *ngIf="!currency.isBaseCurrency"
                pButton
                type="button"
                label="Set as Base"
                class="p-button-text p-button-sm"
                (click)="setAsBase(currency)">
              </button>
            </td>
            <td>
              <span
                *ngIf="isDefaultCurrency(currency.id)"
                class="px-2 py-1 rounded text-xs font-semibold bg-green-100 text-green-800">
                <i class="pi pi-check-circle"></i> Default
              </span>
              <button
                *ngIf="!isDefaultCurrency(currency.id)"
                pButton
                type="button"
                label="Set as Default"
                class="p-button-text p-button-sm"
                (click)="setAsDefault(currency)">
              </button>
            </td>
            <td>{{ currency.displayOrder }}</td>
            <td>
              <div class="flex gap-2">
                <button
                  pButton
                  type="button"
                  icon="pi pi-pencil"
                  class="p-button-rounded p-button-text p-button-sm"
                  pTooltip="Edit"
                  (click)="openDialog(currency)">
                </button>
                <button
                  pButton
                  type="button"
                  icon="pi pi-trash"
                  class="p-button-rounded p-button-text p-button-sm p-button-danger"
                  pTooltip="Delete"
                  [disabled]="currency.isBaseCurrency"
                  (click)="confirmDelete(currency)">
                </button>
              </div>
            </td>
          </tr>
        </ng-template>

        <ng-template pTemplate="emptymessage">
          <tr>
            <td colspan="8" class="text-center py-8 text-gray-500">
              No currencies found
            </td>
          </tr>
        </ng-template>
      </p-table>

      <!-- Currency Dialog -->
      <p-dialog
        [(visible)]="dialogVisible"
        [header]="isEditing ? 'Edit Currency' : 'Add Currency'"
        [modal]="true"
        [style]="{width: '550px'}"
        [closable]="true"
        styleClass="currency-dialog">

        <form [formGroup]="currencyForm" (ngSubmit)="saveCurrency()">
          <div class="p-fluid">
            <!-- Currency Code -->
            <div class="field mb-4">
              <label htmlFor="code" class="block font-semibold mb-2">
                Currency Code (ISO 4217) <span class="text-red-500">*</span>
              </label>
              <input
                pInputText
                id="code"
                formControlName="code"
                placeholder="e.g., USD, EUR, BDT"
                maxlength="3"
                [readonly]="isEditing"
                [class.p-invalid]="currencyForm.get('code')?.invalid && currencyForm.get('code')?.touched"
                style="text-transform: uppercase;" />
              <small class="p-error block mt-1" *ngIf="currencyForm.get('code')?.invalid && currencyForm.get('code')?.touched">
                3-character currency code is required
              </small>
            </div>

            <!-- Currency Name -->
            <div class="field mb-4">
              <label htmlFor="name" class="block font-semibold mb-2">
                Currency Name <span class="text-red-500">*</span>
              </label>
              <input
                pInputText
                id="name"
                formControlName="name"
                placeholder="e.g., US Dollar, Euro"
                [class.p-invalid]="currencyForm.get('name')?.invalid && currencyForm.get('name')?.touched" />
              <small class="p-error block mt-1" *ngIf="currencyForm.get('name')?.invalid && currencyForm.get('name')?.touched">
                Currency name is required
              </small>
            </div>

            <!-- Symbol -->
            <div class="field mb-4">
              <label htmlFor="symbol" class="block font-semibold mb-2">
                Symbol <span class="text-red-500">*</span>
              </label>
              <input
                pInputText
                id="symbol"
                formControlName="symbol"
                placeholder="e.g., $, €, ৳"
                [class.p-invalid]="currencyForm.get('symbol')?.invalid && currencyForm.get('symbol')?.touched" />
              <small class="p-error block mt-1" *ngIf="currencyForm.get('symbol')?.invalid && currencyForm.get('symbol')?.touched">
                Currency symbol is required
              </small>
            </div>

            <!-- Decimal Places and Display Order in a row -->
            <div class="formgrid grid mb-4">
              <div class="field col-6">
                <label htmlFor="decimalPlaces" class="block font-semibold mb-2">
                  Decimal Places
                </label>
                <p-inputNumber
                  inputId="decimalPlaces"
                  formControlName="decimalPlaces"
                  [min]="0"
                  [max]="4"
                  [showButtons]="true"
                  buttonLayout="horizontal"
                  spinnerMode="horizontal"
                  incrementButtonIcon="pi pi-plus"
                  decrementButtonIcon="pi pi-minus"
                  styleClass="w-full">
                </p-inputNumber>
              </div>

              <div class="field col-6">
                <label htmlFor="displayOrder" class="block font-semibold mb-2">
                  Display Order
                </label>
                <p-inputNumber
                  inputId="displayOrder"
                  formControlName="displayOrder"
                  [min]="0"
                  [showButtons]="true"
                  buttonLayout="horizontal"
                  spinnerMode="horizontal"
                  incrementButtonIcon="pi pi-plus"
                  decrementButtonIcon="pi pi-minus"
                  styleClass="w-full">
                </p-inputNumber>
              </div>
            </div>

            <!-- Checkboxes in a row -->
            <div class="field mb-3">
              <div class="flex flex-wrap gap-4">
                <div class="flex align-items-center">
                  <p-checkbox
                    inputId="isActive"
                    formControlName="isActive"
                    [binary]="true">
                  </p-checkbox>
                  <label htmlFor="isActive" class="ml-2 cursor-pointer">Active</label>
                </div>

                <div class="flex align-items-center" *ngIf="!isEditing">
                  <p-checkbox
                    inputId="isBaseCurrency"
                    formControlName="isBaseCurrency"
                    [binary]="true">
                  </p-checkbox>
                  <label htmlFor="isBaseCurrency" class="ml-2 cursor-pointer">Set as Base Currency</label>
                </div>
              </div>
            </div>
          </div>

          <div class="flex justify-content-end gap-2 pt-3 border-top-1 surface-border">
            <button
              pButton
              type="button"
              label="Cancel"
              icon="pi pi-times"
              class="p-button-text p-button-secondary"
              (click)="dialogVisible = false">
            </button>
            <button
              pButton
              type="submit"
              [label]="isEditing ? 'Update' : 'Create'"
              [icon]="isEditing ? 'pi pi-save' : 'pi pi-plus'"
              [loading]="saving()"
              [disabled]="currencyForm.invalid"
              class="p-button-success">
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

      .currency-dialog {
        .p-dialog-content {
          padding: 1.5rem;
        }

        .field {
          margin-bottom: 1rem;
        }

        label {
          display: block;
          margin-bottom: 0.5rem;
          font-weight: 600;
          color: var(--text-color);
        }

        .p-inputtext,
        .p-inputnumber {
          width: 100%;
        }

        .p-inputnumber-input {
          width: 100%;
        }

        .p-checkbox {
          margin-right: 0.5rem;
        }

        .p-error {
          color: #dc3545;
          font-size: 0.875rem;
        }

        .p-invalid {
          border-color: #dc3545;
        }
      }
    }
  `]
})
export class CurrenciesListComponent implements OnInit {
  private currencyService = inject(CurrencyService);
  private messageService = inject(MessageService);
  private confirmationService = inject(ConfirmationService);
  private fb = inject(FormBuilder);
  private router = inject(Router);

  currencies = signal<Currency[]>([]);
  loading = signal(false);
  saving = signal(false);
  dialogVisible = false;
  isEditing = false;
  currentCurrencyId: string | null = null;
  defaultCurrencyId = signal<string | null>(null);

  currencyForm: FormGroup = this.fb.group({
    code: ['', [Validators.required, Validators.minLength(3), Validators.maxLength(3)]],
    name: ['', Validators.required],
    symbol: ['', Validators.required],
    decimalPlaces: [2, [Validators.required, Validators.min(0), Validators.max(4)]],
    displayOrder: [0, [Validators.required, Validators.min(0)]],
    isActive: [true],
    isBaseCurrency: [false]
  });

  ngOnInit(): void {
    this.loadCurrencies();
    this.loadDefaultCurrency();
  }

  loadDefaultCurrency(): void {
    this.currencyService.getDefaultCurrencyId().subscribe({
      next: (id) => {
        this.defaultCurrencyId.set(id);
      },
      error: () => {
        console.error('Failed to load default currency');
      }
    });
  }

  isDefaultCurrency(currencyId: string): boolean {
    return this.defaultCurrencyId() === currencyId;
  }

  setAsDefault(currency: Currency): void {
    this.confirmationService.confirm({
      message: `Are you sure you want to set ${currency.name} (${currency.code}) as the default currency for new transactions?`,
      header: 'Confirm',
      icon: 'pi pi-exclamation-triangle',
      accept: () => {
        this.currencyService.setDefaultCurrency(currency.id).subscribe({
          next: () => {
            this.messageService.add({
              severity: 'success',
              summary: 'Success',
              detail: `${currency.code} set as default currency`
            });
            this.loadDefaultCurrency();
          },
          error: () => {
            this.messageService.add({
              severity: 'error',
              summary: 'Error',
              detail: 'Failed to set default currency'
            });
          }
        });
      }
    });
  }

  loadCurrencies(): void {
    this.loading.set(true);
    this.currencyService.getAllCurrencies().subscribe({
      next: (currencies) => {
        this.currencies.set(currencies);
        this.loading.set(false);
      },
      error: (error) => {
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: 'Failed to load currencies'
        });
        this.loading.set(false);
      }
    });
  }

  openDialog(currency?: Currency): void {
    this.isEditing = !!currency;
    this.currentCurrencyId = currency?.id || null;

    if (currency) {
      this.currencyForm.patchValue({
        code: currency.code,
        name: currency.name,
        symbol: currency.symbol,
        decimalPlaces: currency.decimalPlaces,
        displayOrder: currency.displayOrder,
        isActive: currency.isActive,
        isBaseCurrency: currency.isBaseCurrency
      });
    } else {
      this.currencyForm.reset({
        code: '',
        name: '',
        symbol: '',
        decimalPlaces: 2,
        displayOrder: 0,
        isActive: true,
        isBaseCurrency: false
      });
    }

    this.dialogVisible = true;
  }

  saveCurrency(): void {
    if (this.currencyForm.invalid) {
      this.currencyForm.markAllAsTouched();
      return;
    }

    this.saving.set(true);
    const formValue = this.currencyForm.value;

    const request = {
      code: formValue.code?.toUpperCase(),
      name: formValue.name,
      symbol: formValue.symbol,
      decimalPlaces: formValue.decimalPlaces,
      displayOrder: formValue.displayOrder,
      isActive: formValue.isActive,
      isBaseCurrency: formValue.isBaseCurrency || false
    };

    const operation = this.isEditing
      ? this.currencyService.updateCurrency(this.currentCurrencyId!, request)
      : this.currencyService.createCurrency(request);

    operation.subscribe({
      next: () => {
        this.messageService.add({
          severity: 'success',
          summary: 'Success',
          detail: `Currency ${this.isEditing ? 'updated' : 'created'} successfully`
        });
        this.dialogVisible = false;
        this.loadCurrencies();
        this.saving.set(false);
      },
      error: (error) => {
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: `Failed to ${this.isEditing ? 'update' : 'create'} currency`
        });
        this.saving.set(false);
      }
    });
  }

  setAsBase(currency: Currency): void {
    this.confirmationService.confirm({
      message: `Are you sure you want to set ${currency.name} (${currency.code}) as the base currency?`,
      header: 'Confirm',
      icon: 'pi pi-exclamation-triangle',
      accept: () => {
        this.currencyService.setAsBaseCurrency(currency.id).subscribe({
          next: () => {
            this.messageService.add({
              severity: 'success',
              summary: 'Success',
              detail: `${currency.code} set as base currency`
            });
            this.loadCurrencies();
          },
          error: (error) => {
            this.messageService.add({
              severity: 'error',
              summary: 'Error',
              detail: 'Failed to set base currency'
            });
          }
        });
      }
    });
  }

  confirmDelete(currency: Currency): void {
    this.confirmationService.confirm({
      message: `Are you sure you want to delete ${currency.name} (${currency.code})?`,
      header: 'Confirm',
      icon: 'pi pi-exclamation-triangle',
      accept: () => {
        this.currencyService.deleteCurrency(currency.id).subscribe({
          next: () => {
            this.messageService.add({
              severity: 'success',
              summary: 'Success',
              detail: 'Currency deleted successfully'
            });
            this.loadCurrencies();
          },
          error: (error) => {
            this.messageService.add({
              severity: 'error',
              summary: 'Error',
              detail: 'Failed to delete currency'
            });
          }
        });
      }
    });
  }
}
