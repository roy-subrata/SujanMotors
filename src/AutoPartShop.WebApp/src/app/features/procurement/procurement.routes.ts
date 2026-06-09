import { Routes } from '@angular/router';
import { PurchaseOrdersComponent } from './purchase-orders/purchase-orders.component';
import { PurchaseOrderFormComponent } from './purchase-orders/purchase-order-form/purchase-order-form.component';
import { PurchaseReturnsComponent } from './purchase-returns/purchase-returns.component';
import { PurchaseReturnsFormComponent } from './purchase-returns/purchase-returns-form/purchase-returns-form.component';
import { GoodsReceiptsComponent } from './goods-receipts/goods-receipts.component';
import { GoodsReceiptFormComponent } from './goods-receipts/goods-receipt-form.component';
import { PaymentProviderListComponent } from './payment-provider/payment-provider-list.component';
import { PaymentProviderFormComponent } from './payment-provider/payment-provider-form.component';
import { SupplierPaymentListComponent } from './supplier-payment/supplier-payment-list.component';
import { SupplierPaymentFormComponent } from './supplier-payment/supplier-payment-form.component';
import { SupplierPaymentSummaryComponent } from './supplier-payment-summary/supplier-payment-summary.component';
import { SupplierAccountSummaryComponent } from './supplier-account-summary/supplier-account-summary.component';
import { DailyExpensesComponent } from './daily-expenses/daily-expenses.component';
import { SupplierPerformanceComponent } from './supplier-performance/supplier-performance.component';

export const procurementRoutes: Routes = [
  // Purchase Orders
  { path: 'purchase-orders', component: PurchaseOrdersComponent },
  { path: 'purchase-orders/create', component: PurchaseOrderFormComponent },
  { path: 'purchase-orders/edit', component: PurchaseOrderFormComponent },
  { path: 'purchase-orders/view', component: PurchaseOrderFormComponent },

  // Purchase Returns
  { path: 'purchase-returns', component: PurchaseReturnsComponent },
  { path: 'purchase-returns/create', component: PurchaseReturnsFormComponent },
  { path: 'purchase-returns/edit', component: PurchaseReturnsFormComponent },
  { path: 'purchase-returns/view', component: PurchaseReturnsFormComponent },

  // Goods Receipts
  { path: 'goods-receipts', component: GoodsReceiptsComponent },
  { path: 'goods-receipts/create', component: GoodsReceiptFormComponent },
  { path: 'goods-receipts/view', component: GoodsReceiptFormComponent },
  { path: 'goods-receipts/edit', component: GoodsReceiptFormComponent },
  // Payment Providers
  { path: 'payment-providers', component: PaymentProviderListComponent },
  { path: 'payment-providers/new', component: PaymentProviderFormComponent },
  { path: 'payment-providers/edit', component: PaymentProviderFormComponent },

  // Supplier Payments
  { path: 'supplier-payments', component: SupplierPaymentListComponent },
  { path: 'supplier-payments/new', component: SupplierPaymentFormComponent },
  { path: 'supplier-payments/edit', component: SupplierPaymentFormComponent },
  { path: 'supplier-payments/view', component: SupplierPaymentFormComponent },
  { path: 'supplier-payments/summary/:supplierId', component: SupplierPaymentSummaryComponent },

  // Supplier Account Summary
  { path: 'supplier-account-summary', component: SupplierAccountSummaryComponent },

  // Daily Expenses
  { path: 'daily-expenses', component: DailyExpensesComponent },

  // Supplier Performance report (damaged rate)
  { path: 'supplier-performance', component: SupplierPerformanceComponent }
];

