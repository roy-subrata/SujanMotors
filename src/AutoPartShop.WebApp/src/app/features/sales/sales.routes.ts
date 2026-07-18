import { Routes } from '@angular/router';
import { QuotationsComponent } from './quotations/quotations.component';
import { QuotationsListComponent } from './quotations/quotations-list/quotations-list.component';
import { QuotationFormComponent } from './quotations/quotation-form/quotation-form.component';
import { ProformaInvoicesComponent } from './proforma-invoices/proforma-invoices.component';
import { ProformaInvoicesListComponent } from './proforma-invoices/proforma-invoices-list/proforma-invoices-list.component';
import { DebitNotesComponent } from './debit-notes/debit-notes.component';
import { DebitNotesListComponent } from './debit-notes/debit-notes-list/debit-notes-list.component';
import { DebitNoteFormComponent } from './debit-notes/debit-note-form/debit-note-form.component';
import { TillSessionsComponent } from './till-sessions/till-sessions.component';
import { TillSessionCurrentComponent } from './till-sessions/till-session-current/till-session-current.component';
import { TillSessionsListComponent } from './till-sessions/till-sessions-list/till-sessions-list.component';
import { SalesOrdersComponent } from './sales-orders/sales-orders.component';
import { SalesOrdersListComponent } from './sales-orders/sales-orders-list/sales-orders-list.component';
import { SalesOrderFormComponent } from './sales-orders/sales-order-form/sales-order-form.component';
import { InvoicesComponent } from './invoices/invoices.component';
import { InvoicesListComponent } from './invoices/invoices-list/invoices-list.component';
import { InvoiceFormComponent } from './invoices/invoice-form/invoice-form.component';
import { InvoicePrintComponent } from './invoices/invoice-print/invoice-print.component';
import { PendingDeliveriesComponent } from './pending-deliveries/pending-deliveries.component';
import { ChallanPrintComponent } from './challans/challan-print/challan-print.component';
import { SalesReturnsComponent } from './sales-returns/sales-returns.component';
import { SalesReturnsListComponent } from './sales-returns/sales-returns-list/sales-returns-list.component';
import { SalesReturnFormComponent } from './sales-returns/sales-return-form/sales-return-form.component';
import { CustomerPaymentsComponent } from './customer-payments/customer-payments.component';
import { CustomerPaymentListComponent } from './customer-payments/customer-payment-list.component';
import { CustomerPaymentFormComponent } from './customer-payments/customer-payment-form.component';
import { CustomerPaymentSummaryComponent } from './customer-payment-summary/customer-payment-summary.component';
import { CustomerAccountSummaryComponent } from './customer-account-summary/customer-account-summary.component';
import { CustomersComponent } from './customers/customers.component';
import { CustomersListComponent } from './customers/customers-list/customers-list.component';
import { CustomerFormComponent } from './customers/customer-form/customer-form.component';
import { CustomerDetailComponent } from './customers/customer-detail/customer-detail.component';
import { TechniciansComponent } from './technicians/technicians.component';
import { TechniciansListComponent } from './technicians/technicians-list/technicians-list.component';
import { TechnicianFormComponent } from './technicians/technician-form/technician-form.component';

export const salesRoutes: Routes = [
    // Quotations — separate from the legacy /api/v1/quotes "Save as Quotation" shortcut
    {
        path: 'quotations',
        component: QuotationsComponent,
        children: [
            { path: '', component: QuotationsListComponent },
            { path: 'create', component: QuotationFormComponent },
            { path: 'view', component: QuotationFormComponent }
        ]
    },

    // Proforma Invoices — thin wrapper generated FROM an existing Sales Order; no line items or
    // a create form of its own, so there is only a list (the "Generate Proforma" dialog is shared
    // with the Sales Order row action and lives under proforma-invoices/generate-proforma-dialog).
    {
        path: 'proforma-invoices',
        component: ProformaInvoicesComponent,
        children: [
            { path: '', component: ProformaInvoicesListComponent }
        ]
    },

    // Customer Debit Notes — standalone flat balance-owed adjustment against a customer
    {
        path: 'debit-notes',
        component: DebitNotesComponent,
        children: [
            { path: '', component: DebitNotesListComponent },
            { path: 'create', component: DebitNoteFormComponent }
        ]
    },

    // Till Sessions — standalone admin cash-drawer lifecycle (open/cash-drop/close/shift report).
    // Deliberately separate from checkout — the Quick Sale shortcut has no till-session awareness.
    {
        path: 'till-sessions',
        component: TillSessionsComponent,
        children: [
            { path: '', component: TillSessionCurrentComponent },
            { path: 'history', component: TillSessionsListComponent }
        ]
    },

    // Sales Orders
    {
        path: 'sales-orders',
        component: SalesOrdersComponent,
        children: [
            { path: '', component: SalesOrdersListComponent },
            { path: 'create', component: SalesOrderFormComponent },
            { path: 'edit', component: SalesOrderFormComponent },
            { path: 'view', component: SalesOrderFormComponent }
        ]
    },

    // Invoice print — standalone, no shell layout wrapper
    { path: 'invoices/:id/print', component: InvoicePrintComponent },

    // Pending deliveries
    { path: 'pending-deliveries', component: PendingDeliveriesComponent },

    // Challan print — standalone (opens in new tab)
    { path: 'challans/:id/print', component: ChallanPrintComponent },

    // Invoices
    {
        path: 'invoices',
        component: InvoicesComponent,
        children: [
            { path: '', component: InvoicesListComponent },
            { path: 'create', component: InvoiceFormComponent },
            { path: 'view', component: InvoiceFormComponent }
        ]
    },

    // Sales Returns
    {
        path: 'sales-returns',
        component: SalesReturnsComponent,
        children: [
            { path: '', component: SalesReturnsListComponent },
            { path: 'create', component: SalesReturnFormComponent },
            { path: 'view', component: SalesReturnFormComponent }
        ]
    },

    // Customer Payments — summary must be declared BEFORE the parent route that has a :customerId child
    {
        path: 'customer-payments/summary/:customerId',
        component: CustomerPaymentSummaryComponent
    },
    {
        path: 'customer-payments',
        component: CustomerPaymentsComponent,
        children: [
            { path: '', component: CustomerPaymentListComponent },
            { path: 'new', component: CustomerPaymentFormComponent },
            { path: 'edit', component: CustomerPaymentFormComponent },
            { path: 'view', component: CustomerPaymentFormComponent },
            { path: ':customerId', component: CustomerPaymentListComponent }
        ]
    },

    // Customer Account Summary Report
    {
        path: 'customer-account-summary',
        component: CustomerAccountSummaryComponent
    },

    // Customers
    {
        path: 'customers',
        component: CustomersComponent,
        children: [
            { path: '', component: CustomersListComponent },
            { path: 'create', component: CustomerFormComponent },
            { path: 'edit', component: CustomerFormComponent },
            { path: 'detail', component: CustomerDetailComponent }
        ]
    },

    // Technicians
    {
        path: 'technicians',
        component: TechniciansComponent,
        children: [
            { path: '', component: TechniciansListComponent },
            { path: 'create', component: TechnicianFormComponent },
            { path: 'edit', component: TechnicianFormComponent },
            { path: 'view', component: TechnicianFormComponent }
        ]
    }
];
