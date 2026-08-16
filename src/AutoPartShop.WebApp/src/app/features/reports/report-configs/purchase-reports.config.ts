import { ReportPageConfig } from './report-config.model';

/**
 * Purchase report group — backed by api/v1/reports/purchase/*.
 * All user-facing strings below are i18n keys (see ReportPageConfig docs).
 */
export const PURCHASE_REPORT_CONFIGS: ReportPageConfig[] = [
    {
        key: 'purchase-summary',
        group: 'purchase',
        title: 'reports.pages.purchaseSummary.title',
        subtitle: 'reports.pages.purchaseSummary.subtitle',
        icon: 'pi pi-chart-line',
        endpoint: 'v1/reports/purchase/summary',
        paged: false,
        defaultRange: 'thisMonth',
        requiresDateRange: true,
        itemLabel: 'reports.items.periods',
        filters: [
            { kind: 'dateRange', key: 'dateRange', label: 'reports.filters.period' },
            {
                kind: 'select', key: 'groupBy', label: 'reports.filters.groupBy', default: 'day',
                options: [
                    { label: 'reports.options.daily', value: 'day' },
                    { label: 'reports.options.weekly', value: 'week' },
                    { label: 'reports.options.monthly', value: 'month' }
                ]
            }
        ],
        columns: [
            { field: 'periodStart', header: 'reports.columns.period', type: 'date', mobilePrimary: true },
            { field: 'poCount', header: 'reports.columns.pos', type: 'number' },
            { field: 'totalAmount', header: 'common.labels.totalAmount', type: 'money' },
            { field: 'paidAmount', header: 'common.status.paid', type: 'money' },
            { field: 'outstanding', header: 'reports.columns.outstanding', type: 'money' }
        ],
        chart: {
            type: 'line',
            labelField: 'periodStart',
            labelType: 'date',
            series: [
                { field: 'totalAmount', label: 'common.labels.totalAmount' },
                { field: 'paidAmount', label: 'common.status.paid' }
            ]
        }
    },
    {
        key: 'purchases-by-supplier',
        group: 'purchase',
        title: 'reports.pages.purchasesBySupplier.title',
        subtitle: 'reports.pages.purchasesBySupplier.subtitle',
        icon: 'pi pi-truck',
        endpoint: 'v1/reports/purchase/by-supplier',
        paged: true,
        defaultRange: 'thisMonth',
        requiresDateRange: true,
        itemLabel: 'reports.items.suppliers',
        filters: [
            { kind: 'dateRange', key: 'dateRange', label: 'reports.filters.period' },
            { kind: 'search', key: 'search', label: 'common.actions.search', placeholder: 'reports.filters.searchSupplier' }
        ],
        columns: [
            { field: 'supplierCode', header: 'common.labels.code' },
            { field: 'supplierName', header: 'reports.columns.supplier', mobilePrimary: true },
            { field: 'poCount', header: 'reports.columns.pos', type: 'number' },
            { field: 'totalAmount', header: 'common.labels.totalAmount', type: 'money' },
            { field: 'receivedValue', header: 'reports.columns.receivedValue', type: 'money' },
            { field: 'paidAmount', header: 'common.status.paid', type: 'money' },
            { field: 'returnedValue', header: 'reports.columns.returnedValue', type: 'money' },
            { field: 'balance', header: 'reports.columns.balance', type: 'money' }
        ]
    },
    {
        key: 'purchase-returns',
        group: 'purchase',
        title: 'reports.pages.purchaseReturns.title',
        subtitle: 'reports.pages.purchaseReturns.subtitle',
        icon: 'pi pi-replay',
        endpoint: 'v1/reports/purchase/returns',
        paged: true,
        defaultRange: 'thisMonth',
        requiresDateRange: true,
        itemLabel: 'reports.items.returns',
        filters: [
            { kind: 'dateRange', key: 'dateRange', label: 'reports.filters.period' }
        ],
        columns: [
            { field: 'returnDate', header: 'reports.columns.returnDate', type: 'date', mobilePrimary: true },
            { field: 'returnNumber', header: 'reports.columns.returnNo' },
            { field: 'poNumber', header: 'reports.columns.poNumber' },
            { field: 'supplierName', header: 'reports.columns.supplier' },
            { field: 'status', header: 'common.labels.status' },
            { field: 'settlementStatus', header: 'reports.columns.settlement' },
            { field: 'refundAmount', header: 'reports.columns.refundAmount', type: 'money' }
        ]
    }
];
