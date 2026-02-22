import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AutoCompleteModule } from 'primeng/autocomplete';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { InputNumberModule } from 'primeng/inputnumber';
import { TableModule } from 'primeng/table';
import { CardModule } from 'primeng/card';
import { ToastModule } from 'primeng/toast';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { DialogModule } from 'primeng/dialog';
import { TooltipModule } from 'primeng/tooltip';
import { SelectModule } from 'primeng/select';
import { TextareaModule } from 'primeng/textarea';
import { MessageService, ConfirmationService } from 'primeng/api';

import { QuickSaleComponent } from '../quick-sale/quick-sale.component';
import { QuickCustomerDialogComponent } from '../components/quick-customer-dialog.component';
import { InvoicePreviewComponent } from '../components/invoice-preview.component';
import { LazyAutocompleteComponent, LazyRequest, LazyResponse } from '../../../shared/components/lazy-autocomplete';
import { TechnicianService, TechnicianResponse } from '../services/technician.service';
import { CustomerService, CustomerResponse } from '../services/customer.service';
import { StockComponent } from '../../inventory/stock/stock.component';
import { map } from 'rxjs/operators';

@Component({
  selector: 'app-quick-sale-shortcut',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    FormsModule,
    AutoCompleteModule,
    ButtonModule,
    InputTextModule,
    InputNumberModule,
    TableModule,
    CardModule,
    ToastModule,
    ConfirmDialogModule,
    DialogModule,
    TooltipModule,
    SelectModule,
    TextareaModule,
    RouterLink,
    QuickCustomerDialogComponent,
    InvoicePreviewComponent,
    LazyAutocompleteComponent,
    StockComponent
  ],
  providers: [MessageService, ConfirmationService],
  templateUrl: './quick-sale-shortcut.component.html',
  styleUrls: ['./quick-sale-shortcut.component.css']
})
export class QuickSaleShortcutComponent extends QuickSaleComponent {
  private readonly shortcutMessageService = inject(MessageService);
  private readonly shortcutTechnicianService = inject(TechnicianService);
  private readonly shortcutCustomerService = inject(CustomerService);
  vehicleNumber = '';
  shortcutNotes = '';
  showCheckoutDialog = false;
  showStockPageDialog = false;
  shortcutDiscount = 0;
  checkoutPaymentMethod: 'CASH' | 'CARD' | 'MOBILE_BANKING' | 'DUE' = 'CASH';
  checkoutPaymentAmount: number | null = null;

  override fetchCustomersLazy = (req: LazyRequest) =>
    this.shortcutCustomerService.getCustomers({
      search: req.search,
      pageNumber: req.pageNumber,
      pageSize: req.pageSize
    }).pipe(
      map(res => ({
        items: res.data,
        totalCount: res.pagination.totalCount
      } as LazyResponse<CustomerResponse>))
    );

  override fetchTechniciansLazy = (req: LazyRequest) =>
    this.shortcutTechnicianService.getTechnicians({
      search: req.search,
      pageNumber: req.pageNumber,
      pageSize: req.pageSize
    }).pipe(
      map(res => ({
        items: res.data.filter(t => t.status === 'ACTIVE'),
        totalCount: res.pagination.totalCount
      } as LazyResponse<TechnicianResponse>))
    );

  paymentMethodOptions = [
    { label: 'Cash', value: 'CASH' },
    { label: 'Card', value: 'CARD' },
    { label: 'Mobile Banking', value: 'MOBILE_BANKING' },
    { label: 'Due', value: 'DUE' }
  ];

  applyShortcutDiscount(): void {
    const value = Math.max(0, Math.min(100, Number(this.shortcutDiscount || 0)));
    this.shortcutDiscount = value;
    const items = this.cartItems();
    this.cartItems.set(
      items.map(item => ({ ...item, discount: value }))
    );
  }

  submitShortcut(): void {
    const vehicle = this.vehicleNumber.trim();
    const notes = this.shortcutNotes.trim();
    const parts = [
      vehicle ? `Car No: ${vehicle}` : '',
      notes
    ].filter(Boolean);
    this.saleNotes = parts.join(' | ');
    this.onSubmit();
  }

  resetShortcut(): void {
    this.resetForm();
    this.generateInvoiceNumber();
    this.vehicleNumber = '';
    this.shortcutNotes = '';
    this.showCheckoutDialog = false;
    this.shortcutDiscount = 0;
    this.checkoutPaymentMethod = 'CASH';
    this.checkoutPaymentAmount = null;
  }

  openCheckout(): void {
    if (this.cartItems().length === 0) {
      this.shortcutMessageService.add({
        severity: 'warn',
        summary: 'No Items',
        detail: 'Add items before checkout'
      });
      return;
    }
    this.checkoutPaymentAmount = this.grandTotal();
    this.showCheckoutDialog = true;
  }

  confirmCheckout(): void {
    if (!this.selectedCustomer()) {
      this.shortcutMessageService.add({
        severity: 'error',
        summary: 'Customer Required',
        detail: 'Please select a customer'
      });
      return;
    }

    if (this.checkoutPaymentMethod === 'DUE') {
      this.payments.set([]);
    } else {
      const amount = Number(this.checkoutPaymentAmount || this.grandTotal());
      this.payments.set([{ method: this.checkoutPaymentMethod, amount }]);
    }

    this.showCheckoutDialog = false;
    this.submitShortcut();
  }

  getBrandName(partId: string): string {
    const part = this.parts().find(p => p.id === partId);
    return part?.brandName || '-';
  }
}
