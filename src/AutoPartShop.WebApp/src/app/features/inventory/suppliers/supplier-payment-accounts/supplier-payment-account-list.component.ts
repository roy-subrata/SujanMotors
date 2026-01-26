import { Component, OnInit, inject, ViewChild, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { TableModule } from 'primeng/table';
import { InputTextModule } from 'primeng/inputtext';
import { ToastModule } from 'primeng/toast';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { DialogModule } from 'primeng/dialog';
import { ContextMenuModule, ContextMenu } from 'primeng/contextmenu';
import { RippleModule } from 'primeng/ripple';
import { MessageService, ConfirmationService, MenuItem } from 'primeng/api';
import { TagModule } from 'primeng/tag';
import { TooltipModule } from 'primeng/tooltip';
import { SupplierPaymentAccountService, SupplierPaymentAccountResponse } from '../../services/supplier-payment-account.service';
import { SupplierService } from '../../services/supplier.service';

@Component({
  selector: 'app-supplier-payment-account-list',
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
    TooltipModule
  ],
  providers: [MessageService, ConfirmationService],
  templateUrl: './supplier-payment-account-list.component.html',
  styleUrls: ['./supplier-payment-account-list.component.css']
})
export class SupplierPaymentAccountListComponent implements OnInit {
  @ViewChild('contextMenu') contextMenu: ContextMenu | undefined;
  @Input() supplierId: string | null = null;

  private readonly service = inject(SupplierPaymentAccountService);
  private readonly supplierService = inject(SupplierService);
  private readonly messageService = inject(MessageService);
  private readonly confirmationService = inject(ConfirmationService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  accounts: SupplierPaymentAccountResponse[] = [];
  supplierName: string = '';
  loading: boolean = false;
  contextMenuItems: MenuItem[] = [];
  selectedAccount: SupplierPaymentAccountResponse | null = null;

  ngOnInit(): void {
    // Get supplier ID from route if not provided as input
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
    this.initializeContextMenu();
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

  private initializeContextMenu(): void {
    this.contextMenuItems = [
      {
        label: 'Edit',
        icon: 'pi pi-pencil',
        command: () => {
          if (this.selectedAccount) {
            this.edit(this.selectedAccount);
          }
        }
      },
      {
        label: 'Set as Default',
        icon: 'pi pi-star',
        command: () => {
          if (this.selectedAccount) {
            this.setAsDefault(this.selectedAccount);
          }
        }
      },
      { separator: true },
      {
        label: 'Delete',
        icon: 'pi pi-trash',
        command: () => {
          if (this.selectedAccount) {
            this.delete(this.selectedAccount);
          }
        }
      }
    ];
  }

  showContextMenu(event: MouseEvent, account: SupplierPaymentAccountResponse): void {
    this.selectedAccount = account;
    // Update menu items based on selected account
    this.contextMenuItems[1].visible = !account.isDefault;
    this.contextMenu?.show(event);
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
          summary: 'Error',
          detail: 'Failed to load payment accounts'
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
      message: `Are you sure you want to delete '${account.accountName}'?`,
      header: 'Delete Payment Account',
      icon: 'pi pi-exclamation-triangle',
      accept: () => {
        this.service.delete(account.id).subscribe({
          next: () => {
            this.messageService.add({
              severity: 'success',
              summary: 'Success',
              detail: `Payment account '${account.accountName}' deleted successfully`
            });
            this.loadAccounts();
          },
          error: (error) => {
            this.messageService.add({
              severity: 'error',
              summary: 'Error',
              detail: error?.error?.message || 'Failed to delete payment account'
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
          summary: 'Success',
          detail: `'${account.accountName}' set as default`
        });
        this.loadAccounts();
      },
      error: (error) => {
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: error?.error?.message || 'Failed to set default account'
        });
      }
    });
  }

  goBack(): void {
    this.router.navigate(['/inventory/suppliers']);
  }

  getAccountTypeBadgeSeverity(accountType: string): 'success' | 'secondary' | 'info' | 'warn' | 'danger' | 'contrast' | undefined {
    switch (accountType?.toUpperCase()) {
      case 'BANK_TRANSFER':
        return 'info';
      case 'MOBILE_BANKING':
        return 'success';
      case 'CASH':
        return 'warn';
      case 'CHECK':
        return 'secondary';
      default:
        return 'secondary';
    }
  }

  getAccountTypeLabel(accountType: string): string {
    switch (accountType?.toUpperCase()) {
      case 'BANK_TRANSFER':
        return 'Bank Transfer';
      case 'MOBILE_BANKING':
        return 'Mobile Banking';
      case 'CASH':
        return 'Cash';
      case 'CHECK':
        return 'Check';
      case 'OTHER':
        return 'Other';
      default:
        return accountType;
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
