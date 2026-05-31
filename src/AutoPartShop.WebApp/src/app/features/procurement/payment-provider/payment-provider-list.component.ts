import { Component, OnInit, inject, ViewChild, DestroyRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { TableModule } from 'primeng/table';
import { InputTextModule } from 'primeng/inputtext';
import { ToastModule } from 'primeng/toast';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { DialogModule } from 'primeng/dialog';
import { ContextMenuModule, ContextMenu } from 'primeng/contextmenu';
import { RippleModule } from 'primeng/ripple';
import { MessageService, ConfirmationService, MenuItem } from 'primeng/api';
import { PaymentProviderService, PaymentProviderResponse, PaginatedPaymentProviderResponse } from '../services/payment-provider.service';
import { TagModule } from 'primeng/tag';
import { AppCurrencyPipe } from '../../../shared/pipes/app-currency.pipe';
import { I18nService } from '@/shared/services/i18n.service';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { PageContainerComponent } from '@/shared/components/page-container/page-container.component';
import { PageHeaderComponent } from '@/shared/components/page-header/page-header.component';
import { FilterBarComponent } from '@/shared/components/filter-bar/filter-bar.component';

@Component({
  selector: 'app-payment-provider-list',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ButtonModule,
    TableModule,
    InputTextModule,
    ToastModule,
    ConfirmDialogModule,
    DialogModule,
    ContextMenuModule,
    RippleModule,
    TagModule,
    AppCurrencyPipe,
    PageContainerComponent,
    PageHeaderComponent,
    FilterBarComponent
  ],
  providers: [MessageService, ConfirmationService],
  templateUrl: './payment-provider-list.component.html',
  styleUrls: ['./payment-provider-list.component.css']
})
export class PaymentProviderListComponent implements OnInit {
  @ViewChild('contextMenu') contextMenu: ContextMenu | undefined;

  private readonly paymentProviderService = inject(PaymentProviderService);
  private readonly messageService = inject(MessageService);
  private readonly confirmationService = inject(ConfirmationService);
  private readonly router = inject(Router);
  private readonly i18n = inject(I18nService);
  private readonly destroyRef = inject(DestroyRef);

  paymentProviders: PaymentProviderResponse[] = [];
  searchTerm: string = '';
  pageNumber: number = 1;
  pageSize: number = 10;
  totalCount: number = 0;
  loading: boolean = false;
  contextMenuItems: MenuItem[] = [];
  selectedProvider: PaymentProviderResponse | null = null;

  ngOnInit(): void {
    this.rebuildContextMenu();
    this.i18n.translationsLoaded$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
      if (this.selectedProvider) this.rebuildContextMenu();
    });
    this.loadPaymentProviders();
  }

  private rebuildContextMenu(): void {
    const p = this.selectedProvider;
    this.contextMenuItems = [
      {
        label: this.i18n.t('common.actions.edit'),
        icon: 'pi pi-pencil',
        command: () => { if (p) this.edit(p); }
      },
      {
        label: this.i18n.t('common.actions.testConnection'),
        icon: 'pi pi-refresh',
        command: () => { if (p) this.testConnection(p); }
      },
      {
        label: this.i18n.t('common.actions.setAsDefault'),
        icon: 'pi pi-star',
        command: () => { if (p) this.setAsDefault(p); },
        visible: p ? !p.isDefault : false
      },
      { separator: true },
      {
        label: this.i18n.t('common.actions.delete'),
        icon: 'pi pi-trash',
        command: () => { if (p) this.delete(p); }
      }
    ];
  }

  showContextMenu(event: MouseEvent, provider: PaymentProviderResponse): void {
    this.selectedProvider = provider;
    this.rebuildContextMenu();
    this.contextMenu?.show(event);
  }

  loadPaymentProviders(): void {
    this.loading = true;
    this.paymentProviderService.getPaymentProviders(this.pageNumber, this.pageSize, this.searchTerm).subscribe({
      next: (response: PaginatedPaymentProviderResponse) => {
        this.paymentProviders = response.items;
        this.totalCount = response.totalCount;
        this.loading = false;
      },
      error: (error) => {
        this.messageService.add({
          severity: 'error',
          summary: this.i18n.t('common.messages.error'),
          detail: this.i18n.t('paymentProviders.messages.loadFailed')
        });
        console.error('Error loading payment providers:', error);
        this.loading = false;
      }
    });
  }

  onSearch(): void {
    this.pageNumber = 1;
    this.loadPaymentProviders();
  }

  createNew(): void {
    this.router.navigate(['/procurement/payment-providers/new']);
  }

  edit(provider: PaymentProviderResponse): void {
    this.router.navigate(['/procurement/payment-providers/edit'], { queryParams: { id: provider.id } });
  }

  delete(provider: PaymentProviderResponse): void {
    this.confirmationService.confirm({
      message: this.i18n.t('paymentProviders.messages.deleteConfirm', { name: provider.providerName }),
      header: this.i18n.t('common.messages.confirmDeletion'),
      icon: 'pi pi-exclamation-triangle',
      accept: () => {
        this.paymentProviderService.deletePaymentProvider(provider.id).subscribe({
          next: () => {
            this.messageService.add({
              severity: 'success',
              summary: this.i18n.t('common.messages.success'),
              detail: this.i18n.t('paymentProviders.messages.deleteSuccess')
            });
            this.loadPaymentProviders();
          },
          error: (error) => {
            this.messageService.add({
              severity: 'error',
              summary: this.i18n.t('common.messages.error'),
              detail: error?.error?.message || this.i18n.t('paymentProviders.messages.deleteFailed')
            });
            console.error('Error deleting payment provider:', error);
          }
        });
      }
    });
  }

  setAsDefault(provider: PaymentProviderResponse): void {
    this.paymentProviderService.setDefault(provider.id).subscribe({
      next: () => {
        this.messageService.add({
          severity: 'success',
          summary: this.i18n.t('common.messages.success'),
          detail: this.i18n.t('paymentProviders.messages.setDefaultSuccess')
        });
        this.loadPaymentProviders();
      },
      error: (error) => {
        this.messageService.add({
          severity: 'error',
          summary: this.i18n.t('common.messages.error'),
          detail: error?.error?.message || this.i18n.t('paymentProviders.messages.setDefaultFailed')
        });
        console.error('Error setting default:', error);
      }
    });
  }

  testConnection(provider: PaymentProviderResponse): void {
    this.paymentProviderService.testConnection(provider.id).subscribe({
      next: (response) => {
        this.messageService.add({
          severity: response.success ? 'success' : 'error',
          summary: response.success ? this.i18n.t('common.messages.success') : this.i18n.t('common.messages.error'),
          detail: response.message
        });
        this.loadPaymentProviders();
      },
      error: (error) => {
        this.messageService.add({
          severity: 'error',
          summary: this.i18n.t('common.messages.error'),
          detail: error?.error?.message || this.i18n.t('paymentProviders.messages.testConnectionFailed')
        });
        console.error('Error testing connection:', error);
      }
    });
  }

  onPageChange(event: any): void {
    this.pageNumber = (event.first / event.rows) + 1;
    this.pageSize = event.rows;
    this.loadPaymentProviders();
  }

  getStatusSeverity(status: string): 'success' | 'warn' | 'danger' | 'info' {
    switch (status?.toUpperCase()) {
      case 'ACTIVE': return 'success';
      case 'INACTIVE': return 'warn';
      case 'DISABLED': return 'danger';
      default: return 'info';
    }
  }

  getAccountDisplay(provider: PaymentProviderResponse): string {
    switch (provider.providerType?.toUpperCase()) {
      case 'BANK_TRANSFER': return provider.bankAccountNumber || '-';
      case 'MOBILE_BANKING': return provider.mobileNumber || '-';
      case 'ONLINE_GATEWAY':
      case 'CRYPTO': return provider.merchantId || '-';
      default: return '-';
    }
  }

  getAccountLabel(providerType: string): string {
    switch (providerType?.toUpperCase()) {
      case 'BANK_TRANSFER': return 'Account';
      case 'MOBILE_BANKING': return 'Mobile';
      case 'ONLINE_GATEWAY':
      case 'CRYPTO': return 'Merchant';
      default: return 'Account';
    }
  }
}
