import { ReportPageConfig } from './report-config.model';

/**
 * Financial report group — backed by api/v1/reports/financial/*.
 * All user-facing strings below are i18n keys (see ReportPageConfig docs).
 * profit-loss is a hub-card entry only: its route ('profit-loss') is registered as a static
 * path in reports.routes.ts ahead of the generic ':reportKey' route, so it always resolves to
 * the bespoke ProfitLossReportComponent rather than this config's (unused) endpoint/columns.
 */
export const FINANCIAL_REPORT_CONFIGS: ReportPageConfig[] = [
    {
        key: 'receivables-aging',
        group: 'financial',
        title: 'reports.pages.receivablesAging.title',
        subtitle: 'reports.pages.receivablesAging.subtitle',
        icon: 'pi pi-arrow-down-left',
        endpoint: 'v1/reports/financial/receivables-aging',
        paged: true,
        hasTotals: true,
        defaultRange: 'none',
        itemLabel: 'reports.items.customers',
        filters: [
            { kind: 'search', key: 'search', label: 'common.actions.search', placeholder: 'reports.filters.searchCustomer' }
        ],
        columns: [
            { field: 'customerCode', header: 'common.labels.code' },
            { field: 'customerName', header: 'reports.columns.customer', mobilePrimary: true },
            { field: 'currentAmount', header: 'reports.columns.current', type: 'money' },
            { field: 'days1To30', header: 'reports.columns.days1To30', type: 'money' },
            { field: 'days31To60', header: 'reports.columns.days31To60', type: 'money' },
            { field: 'days61To90', header: 'reports.columns.days61To90', type: 'money' },
            { field: 'days90Plus', header: 'reports.columns.days90Plus', type: 'money' },
            { field: 'total', header: 'common.labels.total', type: 'money' }
        ],
        totals: [
            { field: 'total', label: 'reports.totals.totalReceivables', type: 'money' },
            { field: 'days90Plus', label: 'reports.totals.days90PlusOverdue', type: 'money' },
            { field: 'rowCount', label: 'reports.items.customers', type: 'number' }
        ]
    },
    {
        key: 'payables-aging',
        group: 'financial',
        title: 'reports.pages.payablesAging.title',
        subtitle: 'reports.pages.payablesAging.subtitle',
        icon: 'pi pi-arrow-up-right',
        endpoint: 'v1/reports/financial/payables-aging',
        paged: true,
        hasTotals: true,
        defaultRange: 'none',
        itemLabel: 'reports.items.suppliers',
        filters: [
            { kind: 'search', key: 'search', label: 'common.actions.search', placeholder: 'reports.filters.searchSupplier' }
        ],
        columns: [
            { field: 'supplierCode', header: 'common.labels.code' },
            { field: 'supplierName', header: 'reports.columns.supplier', mobilePrimary: true },
            { field: 'currentAmount', header: 'reports.columns.current', type: 'money' },
            { field: 'days1To30', header: 'reports.columns.days1To30', type: 'money' },
            { field: 'days31To60', header: 'reports.columns.days31To60', type: 'money' },
            { field: 'days61To90', header: 'reports.columns.days61To90', type: 'money' },
            { field: 'days90Plus', header: 'reports.columns.days90Plus', type: 'money' },
            { field: 'total', header: 'common.labels.total', type: 'money' }
        ],
        totals: [
            { field: 'total', label: 'reports.totals.totalPayables', type: 'money' },
            { field: 'days90Plus', label: 'reports.totals.days90PlusOverdue', type: 'money' },
            { field: 'rowCount', label: 'reports.items.suppliers', type: 'number' }
        ]
    },
    {
        key: 'expense-report',
        group: 'financial',
        title: 'reports.pages.expenseReport.title',
        subtitle: 'reports.pages.expenseReport.subtitle',
        icon: 'pi pi-receipt',
        endpoint: 'v1/reports/financial/expenses',
        paged: false,
        defaultRange: 'thisMonth',
        requiresDateRange: true,
        itemLabel: 'reports.items.groups',
        filters: [
            { kind: 'dateRange', key: 'dateRange', label: 'reports.filters.period' },
            {
                kind: 'select', key: 'groupBy', label: 'reports.filters.groupBy', default: 'day',
                options: [
                    { label: 'reports.options.byDay', value: 'day' },
                    { label: 'reports.options.byCategory', value: 'category' }
                ]
            }
        ],
        columns: [
            { field: 'groupKey', header: 'reports.columns.group', mobilePrimary: true },
            { field: 'expenseCount', header: 'reports.columns.count', type: 'number' },
            { field: 'totalAmount', header: 'common.labels.totalAmount', type: 'money' }
        ],
        chart: {
            type: 'pie',
            labelField: 'groupKey',
            labelType: 'text',
            series: [{ field: 'totalAmount', label: 'common.labels.amount' }]
        }
    },
    {
        key: 'profit-loss',
        group: 'financial',
        title: 'reports.profitLoss.title',
        subtitle: 'reports.profitLoss.subtitle',
        icon: 'pi pi-money-bill',
        endpoint: 'v1/reports/financial/profit-loss',
        paged: false,
        defaultRange: 'thisMonth',
        requiresDateRange: true,
        itemLabel: 'reports.items.lines',
        filters: [],
        columns: []
    },
    {
        // Hub-card entry only: its route ('vat') is registered as a static path in
        // reports.routes.ts ahead of the generic ':reportKey' route, so it always resolves to
        // the bespoke VatReportComponent rather than this config's (unused) endpoint/columns.
        key: 'vat',
        group: 'financial',
        title: 'reports.vat.title',
        subtitle: 'reports.vat.subtitle',
        icon: 'pi pi-percentage',
        endpoint: 'v1/reports/financial/vat',
        paged: false,
        defaultRange: 'thisMonth',
        requiresDateRange: true,
        itemLabel: 'reports.items.lines',
        filters: [],
        columns: []
    }
];
