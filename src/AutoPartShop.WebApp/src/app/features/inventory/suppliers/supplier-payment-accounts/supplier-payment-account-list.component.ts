import { Component, OnInit, inject, ViewChild, Input, DestroyRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, ActivatedRoute, RouterModule } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { TableModule } from 'primeng/table';
import { InputTextModule } from 'primeng/inputtext';
import { ToastModule } from 'primeng/toast';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { MenuModule, Menu } from 'primeng/menu';
import { MessageService, ConfirmationService, MenuItem } from 'primeng/api';
import { TagModule } from 'primeng/tag';
import { TooltipModule } from 'primeng/tooltip';
import { SupplierPaymentAccountService, SupplierPaymentAccountResponse } from '../../services/supplier-payment-account.service';
import { SupplierService } from '../../services/supplier.service';
import { I18nService } from '@/shared/services/i18n.service';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-supplier-payment-account-list',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    RouterModule,
    ButtonModule,
    TableModule,
    InputTextModule,
    ToastModule,
    ConfirmDialogModule,
    MenuModule,
    TagModule,
    TooltipModule
  ],
  providers: [MessageService, ConfirmationService],
  templateUrl: './supplier-payment-account-list.component.html',
  styleUrls: ['./supplier-payment-account-list.component.css']
})
export class SupplierPaymentAccountListComponent implements OnInit {
  @ViewChild('actionMenu') actionMenu!: Menu;
  @Input() supplierId: string | null = null;

  private readonly service = inject(SupplierPaymentAccountService);
  private readonly supplierService = inject(SupplierService);
  private readonly messageService = inject(MessageService);
  private readonly confirmationService = inject(ConfirmationService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly i18n = inject(I18nService);
  private readonly destroyRef = inject(DestroyRef);

  accounts: SupplierPaymentAccountResponse[] = [];
  supplierName: string = '';
  loading: boolean = false;
  contextMenuItems: MenuItem[] = [];
  selectedAccount: SupplierPaymentAccountResponse | null = null;

  ngOnInit(): void {
    if (!this.supplierId) {
      this.route.queryParams.subscribe(params => {
        this.supplierId = params['supplierId'];
        if (this.supplierId) {
          this.loadSupplierInfo();
          this.loadAccounts();
        }
      });
    } else {
      this.loadSupplierInfo();
      this.loadAccounts();
    }
  }

  private loadSupplierInfo(): void {
    if (!this.supplierId) return;
    this.supplierService.getSupplierById(this.supplierId).subscribe({
      next: (supplier) => {
        this.supplierName = supplier.name;
      },
      error: () => {
        this.supplierName = 'Unknown Supplier';
      }
    });
  }

  showContextMenu(event: MouseEvent, account: SupplierPaymentAccountResponse): void {
    this.selectedAccount = account;
    this.contextMenuItems = [
      {
        label: this.i18n.t('common.actions.edit'),
        icon: 'pi pi-pencil',
        command: () => this.edit(account)
      },
      ...(!account.isDefault ? [{
        label: this.i18n.t('common.actions.setAsDefault'),
        icon: 'pi pi-star',
        command: () => this.setAsDefault(account)
      }] : []),
      { separator: true },
      {
        label: this.i18n.t('common.actions.delete'),
        icon: 'pi pi-trash',
        command: () => this.delete(account)
      }
    ];
    this.actionMenu.toggle(event);
  }

  loadAccounts(): void {
    if (!this.supplierId) return;
    this.loading = true;
    this.service.getBySupplier(this.supplierId).subscribe({
      next: (accounts) => {
        this.accounts = accounts;
        this.loading = false;
      },
      error: (error) => {
        this.messageService.add({
          severity: 'error',
          summary: this.i18n.t('common.messages.error'),
          detail: this.i18n.t('supplierPaymentAccounts.messages.loadFailed')
        });
        console.error('Error loading payment accounts:', error);
        this.loading = false;
      }
    });
  }

  createNew(): void {
    this.router.navigate(['/inventory/suppliers/payment-accounts/new'], {
      queryParams: { supplierId: this.supplierId }
    });
  }

  edit(account: SupplierPaymentAccountResponse): void {
    this.router.navigate(['/inventory/suppliers/payment-accounts/edit'], {
      queryParams: { id: account.id, supplierId: this.supplierId }
    });
  }

  delete(account: SupplierPaymentAccountResponse): void {
    this.confirmationService.confirm({
      message: this.i18n.t('supplierPaymentAccounts.messages.deleteConfirm', { name: account.accountName }),
      header: this.i18n.t('common.messages.confirmDeletion'),
      icon: 'pi pi-exclamation-triangle',
      accept: () => {
        this.service.delete(account.id).subscribe({
          next: () => {
            this.messageService.add({
              severity: 'success',
              summary: this.i18n.t('common.messages.success'),
              detail: this.i18n.t('supplierPaymentAccounts.messages.deleteSuccess')
            });
            this.loadAccounts();
          },
          error: (error) => {
            this.messageService.add({
              severity: 'error',
              summary: this.i18n.t('common.messages.error'),
              detail: error?.error?.message || this.i18n.t('supplierPaymentAccounts.messages.deleteFailed')
            });
          }
        });
      }
    });
  }

  setAsDefault(account: SupplierPaymentAccountResponse): void {
    this.service.setDefault(account.id).subscribe({
      next: () => {
        this.messageService.add({
          severity: 'success',
          summary: this.i18n.t('common.messages.success'),
          detail: this.i18n.t('supplierPaymentAccounts.messages.setDefaultSuccess')
        });
        this.loadAccounts();
      },
      error: (error) => {
        this.messageService.add({
          severity: 'error',
          summary: this.i18n.t('common.messages.error'),
          detail: error?.error?.message || this.i18n.t('supplierPaymentAccounts.messages.setDefaultFailed')
        });
      }
    });
  }

  goBack(): void {
    this.router.navigate(['/inventory/suppliers']);
  }

  getAccountTypeBadgeSeverity(accountType: string): 'success' | 'secondary' | 'info' | 'warn' | 'danger' | 'contrast' | undefined {
    switch (accountType?.toUpperCase()) {
      case 'BANK_TRANSFER': return 'info';
      case 'MOBILE_BANKING': return 'success';
      case 'CASH': return 'warn';
      case 'CHECK': return 'secondary';
      default: return 'secondary';
    }
  }

  getAccountTypeLabel(accountType: string): string {
    switch (accountType?.toUpperCase()) {
      case 'BANK_TRANSFER': return 'Bank Transfer';
      case 'MOBILE_BANKING': return 'Mobile Banking';
      case 'CASH': return 'Cash';
      case 'CHECK': return 'Check';
      case 'OTHER': return 'Other';
      default: return accountType;
    }
  }

  getAccountDisplay(account: SupplierPaymentAccountResponse): string {
    switch (account.accountType?.toUpperCase()) {
      case 'BANK_TRANSFER':
        return account.bankAccountNumber
          ? `${account.bankName} - ${account.bankAccountNumber}`
          : account.bankName || '-';
      case 'MOBILE_BANKING':
        return account.mobileNumber
          ? `${account.mobileProvider} - ${account.mobileNumber}`
          : account.mobileProvider || '-';
      default:
        return account.displayText || '-';
    }
  }
}
