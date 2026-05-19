import { Routes } from '@angular/router';
import { SalesOrdersComponent } from './sales-orders/sales-orders.component';
import { SalesOrdersListComponent } from './sales-orders/sales-orders-list/sales-orders-list.component';
import { SalesOrderFormComponent } from './sales-orders/sales-order-form/sales-order-form.component';
import { InvoicesComponent } from './invoices/invoices.component';
import { InvoicesListComponent } from './invoices/invoices-list/invoices-list.component';
import { InvoiceFormComponent } from './invoices/invoice-form/invoice-form.component';
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
