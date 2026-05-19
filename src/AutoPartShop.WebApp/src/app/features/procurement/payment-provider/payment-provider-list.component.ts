import { Component, OnInit, inject, ViewChild } from '@angular/core';
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
    AppCurrencyPipe
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

  paymentProviders: PaymentProviderResponse[] = [];
  searchTerm: string = '';
  pageNumber: number = 1;
  pageSize: number = 10;
  totalCount: number = 0;
  loading: boolean = false;
  contextMenuItems: MenuItem[] = [];
  selectedProvider: PaymentProviderResponse | null = null;

  ngOnInit(): void {
    this.initializeContextMenu();
    this.loadPaymentProviders();
  }

  /**
   * Initialize context menu items
   */
  private initializeContextMenu(): void {
    this.contextMenuItems = [
      {
        label: 'Edit',
        icon: 'pi pi-pencil',
        command: () => {
          if (this.selectedProvider) {
            this.edit(this.selectedProvider);
          }
        }
      },
      {
        label: 'Test Connection',
        icon: 'pi pi-refresh',
        command: () => {
          if (this.selectedProvider) {
            this.testConnection(this.selectedProvider);
          }
        }
      },
      {
        label: 'Set as Default',
        icon: 'pi pi-star',
        command: () => {
          if (this.selectedProvider) {
            this.setAsDefault(this.selectedProvider);
          }
        },
        visible: this.selectedProvider ? !this.selectedProvider.isDefault : false
      },
      { separator: true },
      {
        label: 'Delete',
        icon: 'pi pi-trash',
        command: () => {
          if (this.selectedProvider) {
            this.delete(this.selectedProvider);
          }
        }
      }
    ];
  }

  /**
   * Show context menu
   */
  showContextMenu(event: MouseEvent, provider: PaymentProviderResponse): void {
    this.selectedProvider = provider;
    this.initializeContextMenu();
    this.contextMenu?.show(event);
  }

  /**
   * Load payment providers
   */
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
          summary: 'Error',
          detail: 'Failed to load payment providers'
        });
        console.error('Error loading payment providers:', error);
        this.loading = false;
      }
    });
  }

  /**
   * Search payment providers
   */
  onSearch(): void {
    this.pageNumber = 1;
    this.loadPaymentProviders();
  }

  /**
   * Create new payment provider
   */
  createNew(): void {
    this.router.navigate(['/procurement/payment-providers/new']);
  }

  /**
   * Edit payment provider
   */
  edit(provider: PaymentProviderResponse): void {
    this.router.navigate(['/procurement/payment-providers/edit'], { queryParams: { id: provider.id } });
  }

  /**
   * Delete payment provider
   */
  delete(provider: PaymentProviderResponse): void {
    this.confirmationService.confirm({
      message: `Are you sure you want to delete '${provider.providerName}'?`,
      header: 'Delete Payment Provider',
      icon: 'pi pi-exclamation-triangle',
      accept: () => {
        this.paymentProviderService.deletePaymentProvider(provider.id).subscribe({
          next: () => {
            this.messageService.add({
              severity: 'success',
              summary: 'Success',
              detail: `Payment Provider '${provider.providerName}' deleted successfully`
            });
            this.loadPaymentProviders();
          },
          error: (error) => {
            this.messageService.add({
              severity: 'error',
              summary: 'Error',
              detail: error?.error?.message || 'Failed to delete payment provider'
            });
            console.error('Error deleting payment provider:', error);
          }
        });
      }
    });
  }

  /**
   * Set as default payment provider
   */
  setAsDefault(provider: PaymentProviderResponse): void {
    this.paymentProviderService.setDefault(provider.id).subscribe({
      next: () => {
        this.messageService.add({
          severity: 'success',
          summary: 'Success',
          detail: `'${provider.providerName}' set as default`
        });
        this.loadPaymentProviders();
      },
      error: (error) => {
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: error?.error?.message || 'Failed to set default provider'
        });
        console.error('Error setting default:', error);
      }
    });
  }

  /**
   * Test payment provider connection
   */
  testConnection(provider: PaymentProviderResponse): void {
    this.paymentProviderService.testConnection(provider.id).subscribe({
      next: (response) => {
        this.messageService.add({
          severity: response.success ? 'success' : 'error',
          summary: response.success ? 'Success' : 'Failed',
          detail: response.message
        });
        this.loadPaymentProviders();
      },
      error: (error) => {
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: error?.error?.message || 'Failed to test connection'
        });
        console.error('Error testing connection:', error);
      }
    });
  }

  /**
   * Handle pagination
   */
  onPageChange(event: any): void {
    this.pageNumber = (event.first / event.rows) + 1;
    this.pageSize = event.rows;
    this.loadPaymentProviders();
  }

  /**
   * Get status badge severity
   */
  getStatusSeverity(status: string): 'success' | 'warn' | 'danger' | 'info' {
    switch (status?.toUpperCase()) {
      case 'ACTIVE':
        return 'success';
      case 'INACTIVE':
        return 'warn';
      case 'DISABLED':
        return 'danger';
      default:
        return 'info';
    }
  }

  /**
   * Get account/number display based on provider type
   */
  getAccountDisplay(provider: PaymentProviderResponse): string {
    switch (provider.providerType?.toUpperCase()) {
      case 'BANK_TRANSFER':
        return provider.bankAccountNumber || '-';
      case 'MOBILE_BANKING':
        return provider.mobileNumber || '-';
      case 'ONLINE_GATEWAY':
      case 'CRYPTO':
        return provider.merchantId || '-';
      default:
        return '-';
    }
  }

  /**
   * Get account label based on provider type
   */
  getAccountLabel(providerType: string): string {
    switch (providerType?.toUpperCase()) {
      case 'BANK_TRANSFER':
        return 'Account';
      case 'MOBILE_BANKING':
        return 'Mobile';
      case 'ONLINE_GATEWAY':
      case 'CRYPTO':
        return 'Merchant';
      default:
        return 'Account';
    }
  }
}
