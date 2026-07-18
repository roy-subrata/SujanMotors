import { ReportPageConfig } from './report-config.model';

/**
 * Financial report group — backed by api/v1/reports/financial/*.
 * profit-loss is a hub-card entry only: its route ('profit-loss') is registered as a static
 * path in reports.routes.ts ahead of the generic ':reportKey' route, so it always resolves to
 * the bespoke ProfitLossReportComponent rather than this config's (unused) endpoint/columns.
 */
export const FINANCIAL_REPORT_CONFIGS: ReportPageConfig[] = [
    {
        key: 'receivables-aging',
        group: 'financial',
        title: 'Receivables Aging',
        subtitle: 'Outstanding customer invoices bucketed by how overdue they are',
        icon: 'pi pi-arrow-down-left',
        endpoint: 'v1/reports/financial/receivables-aging',
        paged: true,
        hasTotals: true,
        defaultRange: 'none',
        itemLabel: 'customers',
        filters: [
            { kind: 'search', key: 'search', label: 'Search', placeholder: 'Search by customer name or code...' }
        ],
        columns: [
            { field: 'customerCode', header: 'Code' },
            { field: 'customerName', header: 'Customer', mobilePrimary: true },
            { field: 'currentAmount', header: 'Current', type: 'money' },
            { field: 'days1To30', header: '1-30 Days', type: 'money' },
            { field: 'days31To60', header: '31-60 Days', type: 'money' },
            { field: 'days61To90', header: '61-90 Days', type: 'money' },
            { field: 'days90Plus', header: '90+ Days', type: 'money' },
            { field: 'total', header: 'Total', type: 'money' }
        ],
        totals: [
            { field: 'total', label: 'Total Receivables', type: 'money' },
            { field: 'days90Plus', label: '90+ Days Overdue', type: 'money' },
            { field: 'rowCount', label: 'Customers', type: 'number' }
        ]
    },
    {
        key: 'payables-aging',
        group: 'financial',
        title: 'Payables Aging',
        subtitle: 'Outstanding supplier balances bucketed by purchase order age',
        icon: 'pi pi-arrow-up-right',
        endpoint: 'v1/reports/financial/payables-aging',
        paged: true,
        hasTotals: true,
        defaultRange: 'none',
        itemLabel: 'suppliers',
        filters: [
            { kind: 'search', key: 'search', label: 'Search', placeholder: 'Search by supplier name or code...' }
        ],
        columns: [
            { field: 'supplierCode', header: 'Code' },
            { field: 'supplierName', header: 'Supplier', mobilePrimary: true },
            { field: 'currentAmount', header: 'Current', type: 'money' },
            { field: 'days1To30', header: '1-30 Days', type: 'money' },
            { field: 'days31To60', header: '31-60 Days', type: 'money' },
            { field: 'days61To90', header: '61-90 Days', type: 'money' },
            { field: 'days90Plus', header: '90+ Days', type: 'money' },
            { field: 'total', header: 'Total', type: 'money' }
        ],
        totals: [
            { field: 'total', label: 'Total Payables', type: 'money' },
            { field: 'days90Plus', label: '90+ Days Overdue', type: 'money' },
            { field: 'rowCount', label: 'Suppliers', type: 'number' }
        ]
    },
    {
        key: 'expense-report',
        group: 'financial',
        title: 'Expense Report',
        subtitle: 'Daily operating expenses grouped by day or category',
        icon: 'pi pi-receipt',
        endpoint: 'v1/reports/financial/expenses',
        paged: false,
        defaultRange: 'thisMonth',
        requiresDateRange: true,
        itemLabel: 'groups',
        filters: [
            { kind: 'dateRange', key: 'dateRange', label: 'Period' },
            {
                kind: 'select', key: 'groupBy', label: 'Group By', default: 'day',
                options: [
                    { label: 'By Day', value: 'day' },
                    { label: 'By Category', value: 'category' }
                ]
            }
        ],
        columns: [
            { field: 'groupKey', header: 'Group', mobilePrimary: true },
            { field: 'expenseCount', header: 'Count', type: 'number' },
            { field: 'totalAmount', header: 'Total Amount', type: 'money' }
        ],
        chart: {
            type: 'pie',
            labelField: 'groupKey',
            labelType: 'text',
            series: [{ field: 'totalAmount', label: 'Amount' }]
        }
    },
    {
        key: 'profit-loss',
        group: 'financial',
        title: 'Profit & Loss',
        subtitle: 'Revenue, expenses and profitability for the period',
        icon: 'pi pi-money-bill',
        endpoint: 'v1/reports/financial/profit-loss',
        paged: false,
        defaultRange: 'thisMonth',
        requiresDateRange: true,
        itemLabel: 'lines',
        filters: [],
        columns: []
    },
    {
        // Hub-card entry only: its route ('vat') is registered as a static path in
        // reports.routes.ts ahead of the generic ':reportKey' route, so it always resolves to
        // the bespoke VatReportComponent rather than this config's (unused) endpoint/columns.
        key: 'vat',
        group: 'financial',
        title: 'VAT Report',
        subtitle: 'Output VAT (sales) vs. input VAT (purchases) reconciliation for the period',
        icon: 'pi pi-percentage',
        endpoint: 'v1/reports/financial/vat',
        paged: false,
        defaultRange: 'thisMonth',
        requiresDateRange: true,
        itemLabel: 'lines',
        filters: [],
        columns: []
    }
];
