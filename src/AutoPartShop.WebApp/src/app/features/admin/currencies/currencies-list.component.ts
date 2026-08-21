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
import { InputTextModule } from 'primeng/inputtext';
import { TooltipModule } from 'primeng/tooltip';
import { InputNumberModule } from 'primeng/inputnumber';
import { CheckboxModule } from 'primeng/checkbox';
import { ToastModule } from 'primeng/toast';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { MessageService, ConfirmationService } from 'primeng/api';
import { CurrencyService, Currency } from '../../../shared/services/currency.service';
import { PageContainerComponent } from '@/shared/components/page-container/page-container.component';
import { PageHeaderComponent } from '@/shared/components/page-header/page-header.component';
import { DataPaginationComponent } from '@/shared/components/data-pagination/data-pagination.component';

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
    ConfirmDialogModule,
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
        [title]="i18n.t('currencies.title')"
        [subtitle]="i18n.t('currencies.subtitle')"
        [count]="currencies().length" [countLabel]="i18n.t('currencies.countLabel')" countIcon="pi pi-dollar">
        <ng-container actions>
          <button class="btn-primary" (click)="openDialog()">
            <i class="pi pi-plus"></i><span>{{ i18n.t('currencies.addCurrency') }}</span>
          </button>
        </ng-container>
      </app-page-header>

      <section class="table-section desktop-only">
        <div class="table-container">
      <p-table
        [value]="pagedCurrencies()"
        [loading]="loading()"
        [paginator]="false"
        [scrollable]="true"
        styleClass="app-table">

        <ng-template pTemplate="header">
          <tr>
            <th>{{ i18n.t('common.labels.code') }}</th>
            <th>{{ i18n.t('common.labels.name') }}</th>
            <th>{{ i18n.t('currencies.table.symbol') }}</th>
            <th>{{ i18n.t('currencies.table.decimalPlaces') }}</th>
            <th>{{ i18n.t('common.labels.status') }}</th>
            <th>{{ i18n.t('currencies.table.baseCurrency') }}</th>
            <th>{{ i18n.t('currencies.table.defaultCurrency') }}</th>
            <th>{{ i18n.t('common.labels.displayOrder') }}</th>
            <th>{{ i18n.t('common.labels.actions') }}</th>
          </tr>
        </ng-template>

        <ng-template pTemplate="body" let-currency>
          <tr>
            <td><span class="font-mono font-semibold">{{ currency.code }}</span></td>
            <td>{{ currency.name }}</td>
            <td><span class="text-lg">{{ currency.symbol }}</span></td>
            <td>{{ currency.decimalPlaces }}</td>
            <td>
              <span class="status-pill" [attr.data-status]="currency.isActive ? 'active' : 'inactive'">
                {{ (currency.isActive ? 'common.status.active' : 'common.status.inactive') | translate }}
              </span>
            </td>
            <td>
              <span
                *ngIf="currency.isBaseCurrency"
                class="px-2 py-1 rounded text-xs font-semibold bg-blue-100 text-blue-800">
                <i class="pi pi-star-fill"></i> {{ i18n.t('currencies.badge.base') }}
              </span>
              <button
                *ngIf="!currency.isBaseCurrency"
                pButton
                type="button"
                [label]="i18n.t('common.actions.setAsBase')"
                class="p-button-text p-button-sm"
                (click)="setAsBase(currency)">
              </button>
            </td>
            <td>
              <span
                *ngIf="isDefaultCurrency(currency.id)"
                class="px-2 py-1 rounded text-xs font-semibold bg-green-100 text-green-800">
                <i class="pi pi-check-circle"></i> {{ i18n.t('currencies.badge.default') }}
              </span>
              <button
                *ngIf="!isDefaultCurrency(currency.id)"
                pButton
                type="button"
                [label]="i18n.t('common.actions.setAsDefault')"
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
                  [pTooltip]="i18n.t('common.actions.edit')"
                  (click)="openDialog(currency)">
                </button>
                <button
                  pButton
                  type="button"
                  icon="pi pi-trash"
                  class="p-button-rounded p-button-text p-button-sm p-button-danger"
                  [pTooltip]="i18n.t('common.actions.delete')"
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
              {{ i18n.t('currencies.empty') }}
            </td>
          </tr>
        </ng-template>
      </p-table>
        </div>
      </section>

      <app-data-pagination
        [first]="first()"
        [pageSize]="pageSize()"
        [totalRecords]="currencies().length"
        [itemLabel]="i18n.t('currencies.countLabel')"
        (pageChange)="goToPage($event)"
        (pageSizeChange)="onPageSizeChange($event)">
      </app-data-pagination>

      <!-- Currency Dialog -->
      <p-dialog
        [(visible)]="dialogVisible"
        [header]="isEditing ? i18n.t('currencies.dialog.editTitle') : i18n.t('currencies.dialog.addTitle')"
        [modal]="true"
        [style]="{width: '550px'}"
        [closable]="true"
        styleClass="currency-dialog">

        <form [formGroup]="currencyForm" (ngSubmit)="saveCurrency()">
          <div class="p-fluid">
            <!-- Currency Code -->
            <div class="field mb-4">
              <label htmlFor="code" class="block font-semibold mb-2">
                {{ i18n.t('currencies.form.codeLabel') }} <span class="text-red-500">*</span>
              </label>
              <input
                pInputText
                id="code"
                formControlName="code"
                [placeholder]="i18n.t('currencies.form.codePlaceholder')"
                maxlength="3"
                [readonly]="isEditing"
                [class.p-invalid]="currencyForm.get('code')?.invalid && currencyForm.get('code')?.touched"
                style="text-transform: uppercase;" />
              <small class="p-error block mt-1" *ngIf="currencyForm.get('code')?.invalid && currencyForm.get('code')?.touched">
                {{ i18n.t('currencies.form.codeRequired') }}
              </small>
            </div>

            <!-- Currency Name -->
            <div class="field mb-4">
              <label htmlFor="name" class="block font-semibold mb-2">
                {{ i18n.t('currencies.form.nameLabel') }} <span class="text-red-500">*</span>
              </label>
              <input
                pInputText
                id="name"
                formControlName="name"
                [placeholder]="i18n.t('currencies.form.namePlaceholder')"
                [class.p-invalid]="currencyForm.get('name')?.invalid && currencyForm.get('name')?.touched" />
              <small class="p-error block mt-1" *ngIf="currencyForm.get('name')?.invalid && currencyForm.get('name')?.touched">
                {{ i18n.t('currencies.form.nameRequired') }}
              </small>
            </div>

            <!-- Symbol -->
            <div class="field mb-4">
              <label htmlFor="symbol" class="block font-semibold mb-2">
                {{ i18n.t('currencies.form.symbolLabel') }} <span class="text-red-500">*</span>
              </label>
              <input
                pInputText
                id="symbol"
                formControlName="symbol"
                [placeholder]="i18n.t('currencies.form.symbolPlaceholder')"
                [class.p-invalid]="currencyForm.get('symbol')?.invalid && currencyForm.get('symbol')?.touched" />
              <small class="p-error block mt-1" *ngIf="currencyForm.get('symbol')?.invalid && currencyForm.get('symbol')?.touched">
                {{ i18n.t('currencies.form.symbolRequired') }}
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
                  {{ i18n.t('currencies.form.displayOrder') }}
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
                  <label htmlFor="isActive" class="ml-2 cursor-pointer">{{ i18n.t('currencies.form.active') }}</label>
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
              [label]="i18n.t('common.actions.cancel')"
              icon="pi pi-times"
              class="p-button-text p-button-secondary"
              (click)="dialogVisible = false">
            </button>
            <button
              pButton
              type="submit"
              [label]="isEditing ? i18n.t('common.actions.update') : i18n.t('common.actions.create')"
              [icon]="isEditing ? 'pi pi-save' : 'pi pi-plus'"
              [loading]="saving()"
              [disabled]="currencyForm.invalid"
              class="p-button-success">
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

      /* Tailwind gray utility is static and doesn't flip under .app-dark. */
      .text-gray-500 { color: var(--color-text-muted) !important; }

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
          color: var(--red);
          font-size: 0.875rem;
        }

        .p-invalid {
          border-color: var(--red);
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
  readonly i18n = inject(I18nService);
  private readonly destroyRef = inject(DestroyRef);

  currencies = signal<Currency[]>([]);
  loading = signal(false);
  saving = signal(false);

  first = signal(0);
  pageSize = signal(10);
  pagedCurrencies = computed(() => this.currencies().slice(this.first(), this.first() + this.pageSize()));
  goToPage(page: number): void { this.first.set((page - 1) * this.pageSize()); }
  onPageSizeChange(size: number): void { this.pageSize.set(size); this.first.set(0); }
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
      message: this.i18n.t('currencies.messages.setDefaultConfirm', { name: currency.name, code: currency.code }),
      header: this.i18n.t('common.actions.confirm'),
      icon: 'pi pi-exclamation-triangle',
      accept: () => {
        this.currencyService.setDefaultCurrency(currency.id).subscribe({
          next: () => {
            this.messageService.add({
              severity: 'success',
              summary: this.i18n.t('common.messages.success'),
              detail: this.i18n.t('currencies.messages.setDefaultSuccess')
            });
            this.loadDefaultCurrency();
          },
          error: () => {
            this.messageService.add({
              severity: 'error',
              summary: this.i18n.t('common.messages.error'),
              detail: this.i18n.t('currencies.messages.setDefaultFailed')
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
          summary: this.i18n.t('common.messages.error'),
          detail: this.i18n.t('currencies.messages.loadFailed')
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
          summary: this.i18n.t('common.messages.success'),
          detail: this.i18n.t(this.isEditing ? 'currencies.messages.updateSuccess' : 'currencies.messages.createSuccess')
        });
        this.dialogVisible = false;
        this.loadCurrencies();
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

  setAsBase(currency: Currency): void {
    this.confirmationService.confirm({
      message: this.i18n.t('currencies.messages.setBaseConfirm', { name: currency.name, code: currency.code }),
      header: this.i18n.t('common.actions.confirm'),
      icon: 'pi pi-exclamation-triangle',
      accept: () => {
        this.currencyService.setAsBaseCurrency(currency.id).subscribe({
          next: () => {
            this.messageService.add({
              severity: 'success',
              summary: this.i18n.t('common.messages.success'),
              detail: this.i18n.t('currencies.messages.setBaseSuccess')
            });
            this.loadCurrencies();
          },
          error: (error) => {
            this.messageService.add({
              severity: 'error',
              summary: this.i18n.t('common.messages.error'),
              detail: this.i18n.t('currencies.messages.setBaseFailed')
            });
          }
        });
      }
    });
  }

  confirmDelete(currency: Currency): void {
    this.confirmationService.confirm({
      message: this.i18n.t('currencies.messages.deleteConfirm', { name: currency.name, code: currency.code }),
      header: this.i18n.t('common.actions.confirm'),
      icon: 'pi pi-exclamation-triangle',
      accept: () => {
        this.currencyService.deleteCurrency(currency.id).subscribe({
          next: () => {
            this.messageService.add({
              severity: 'success',
              summary: this.i18n.t('common.messages.success'),
              detail: this.i18n.t('currencies.messages.deleteSuccess')
            });
            this.loadCurrencies();
          },
          error: (error) => {
            this.messageService.add({
              severity: 'error',
              summary: this.i18n.t('common.messages.error'),
              detail: this.i18n.t('currencies.messages.deleteFailed')
            });
          }
        });
      }
    });
  }
}
