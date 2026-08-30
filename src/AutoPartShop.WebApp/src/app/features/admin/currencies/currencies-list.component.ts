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
  templateUrl: './currencies-list.component.html',
  styleUrl: './currencies-list.component.scss',
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
