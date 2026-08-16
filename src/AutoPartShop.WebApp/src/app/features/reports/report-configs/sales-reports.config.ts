import { ReportPageConfig } from './report-config.model';

/**
 * Sales report group — backed by api/v1/reports/sales/*.
 * All user-facing strings below are i18n keys (see ReportPageConfig docs).
 */
export const SALES_REPORT_CONFIGS: ReportPageConfig[] = [
    {
        key: 'sales-summary',
        group: 'sales',
        title: 'reports.pages.salesSummary.title',
        subtitle: 'reports.pages.salesSummary.subtitle',
        icon: 'pi pi-chart-line',
        endpoint: 'v1/reports/sales/summary',
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
            },
            { kind: 'lookup', key: 'warehouseId', label: 'reports.filters.warehouse', lookup: 'warehouse' },
            {
                kind: 'select', key: 'channel', label: 'reports.filters.channel',
                options: [
                    { label: 'reports.options.pos', value: 'POS' },
                    { label: 'reports.options.mobile', value: 'MOBILE' }
                ]
            }
        ],
        columns: [
            { field: 'periodStart', header: 'reports.columns.period', type: 'date', mobilePrimary: true },
            { field: 'orderCount', header: 'reports.columns.orders', type: 'number' },
            { field: 'grossAmount', header: 'reports.columns.gross', type: 'money' },
            { field: 'discountAmount', header: 'common.labels.discount', type: 'money' },
            { field: 'taxAmount', header: 'common.labels.tax', type: 'money' },
            { field: 'netAmount', header: 'reports.columns.net', type: 'money' },
            { field: 'grandTotal', header: 'reports.columns.grandTotal', type: 'money' },
            { field: 'averageOrderValue', header: 'reports.columns.avgOrder', type: 'money' }
        ],
        chart: {
            type: 'line',
            labelField: 'periodStart',
            labelType: 'date',
            series: [
                { field: 'netAmount', label: 'reports.series.netSales' },
                { field: 'grandTotal', label: 'reports.columns.grandTotal' }
            ]
        }
    },
    {
        key: 'sales-by-product',
        group: 'sales',
        title: 'reports.pages.salesByProduct.title',
        subtitle: 'reports.pages.salesByProduct.subtitle',
        icon: 'pi pi-box',
        endpoint: 'v1/reports/sales/by-product',
        paged: true,
        defaultRange: 'thisMonth',
        requiresDateRange: true,
        itemLabel: 'reports.items.products',
        filters: [
            { kind: 'dateRange', key: 'dateRange', label: 'reports.filters.period' },
            { kind: 'search', key: 'search', label: 'common.actions.search', placeholder: 'reports.filters.searchProduct' },
            { kind: 'lookup', key: 'warehouseId', label: 'reports.filters.warehouse', lookup: 'warehouse' },
            { kind: 'lookup', key: 'categoryId', label: 'common.labels.category', lookup: 'category' },
            { kind: 'lookup', key: 'brandId', label: 'reports.filters.brand', lookup: 'brand' }
        ],
        columns: [
            { field: 'partNumber', header: 'reports.columns.partNo' },
            { field: 'partName', header: 'reports.columns.product', mobilePrimary: true },
            { field: 'sku', header: 'reports.columns.sku' },
            { field: 'categoryName', header: 'common.labels.category' },
            { field: 'brandName', header: 'reports.columns.brand' },
            { field: 'quantitySold', header: 'reports.columns.qtySold', type: 'number' },
            { field: 'grossRevenue', header: 'reports.columns.grossRevenue', type: 'money' },
            { field: 'discountAmount', header: 'common.labels.discount', type: 'money' },
            { field: 'netRevenue', header: 'reports.columns.netRevenue', type: 'money' }
        ]
    },
    {
        key: 'sales-by-category',
        group: 'sales',
        title: 'reports.pages.salesByCategory.title',
        subtitle: 'reports.pages.salesByCategory.subtitle',
        icon: 'pi pi-tags',
        endpoint: 'v1/reports/sales/by-category',
        paged: false,
        defaultRange: 'thisMonth',
        requiresDateRange: true,
        itemLabel: 'reports.items.categories',
        filters: [
            { kind: 'dateRange', key: 'dateRange', label: 'reports.filters.period' },
            { kind: 'lookup', key: 'warehouseId', label: 'reports.filters.warehouse', lookup: 'warehouse' }
        ],
        columns: [
            { field: 'categoryName', header: 'common.labels.category', mobilePrimary: true },
            { field: 'orderCount', header: 'reports.columns.orders', type: 'number' },
            { field: 'quantitySold', header: 'reports.columns.qtySold', type: 'number' },
            { field: 'netRevenue', header: 'reports.columns.netRevenue', type: 'money' },
            { field: 'percentOfTotal', header: 'reports.columns.percentOfTotal', type: 'percent' }
        ],
        chart: {
            type: 'pie',
            labelField: 'categoryName',
            labelType: 'text',
            series: [{ field: 'netRevenue', label: 'reports.columns.netRevenue' }]
        }
    },
    {
        key: 'sales-by-customer',
        group: 'sales',
        title: 'reports.pages.salesByCustomer.title',
        subtitle: 'reports.pages.salesByCustomer.subtitle',
        icon: 'pi pi-users',
        endpoint: 'v1/reports/sales/by-customer',
        paged: true,
        defaultRange: 'thisMonth',
        requiresDateRange: true,
        itemLabel: 'reports.items.customers',
        filters: [
            { kind: 'dateRange', key: 'dateRange', label: 'reports.filters.period' },
            { kind: 'search', key: 'search', label: 'common.actions.search', placeholder: 'reports.filters.searchCustomer' },
            {
                kind: 'select', key: 'customerType', label: 'reports.filters.customerType',
                options: [
                    { label: 'reports.options.retail', value: 'RETAIL' },
                    { label: 'reports.options.wholesale', value: 'WHOLESALE' },
                    { label: 'reports.options.corporate', value: 'CORPORATE' },
                    { label: 'reports.options.distributor', value: 'DISTRIBUTOR' }
                ]
            }
        ],
        columns: [
            { field: 'customerCode', header: 'common.labels.code' },
            { field: 'customerName', header: 'reports.columns.customer', mobilePrimary: true },
            { field: 'customerType', header: 'common.labels.type' },
            { field: 'orderCount', header: 'reports.columns.orders', type: 'number' },
            { field: 'revenue', header: 'reports.columns.revenue', type: 'money' },
            { field: 'paidAmount', header: 'common.status.paid', type: 'money' },
            { field: 'outstanding', header: 'reports.columns.outstanding', type: 'money' },
            { field: 'lastPurchaseDate', header: 'reports.columns.lastPurchase', type: 'date' }
        ]
    },
    {
        key: 'sales-by-salesperson',
        group: 'sales',
        title: 'reports.pages.salesBySalesperson.title',
        subtitle: 'reports.pages.salesBySalesperson.subtitle',
        icon: 'pi pi-user',
        endpoint: 'v1/reports/sales/by-salesperson',
        paged: false,
        defaultRange: 'thisMonth',
        requiresDateRange: true,
        itemLabel: 'reports.items.salespeople',
        filters: [
            { kind: 'dateRange', key: 'dateRange', label: 'reports.filters.period' },
            { kind: 'lookup', key: 'warehouseId', label: 'reports.filters.warehouse', lookup: 'warehouse' }
        ],
        columns: [
            { field: 'technicianName', header: 'reports.columns.salesperson', mobilePrimary: true },
            { field: 'orderCount', header: 'reports.columns.orders', type: 'number' },
            { field: 'quantitySold', header: 'reports.columns.qtySold', type: 'number' },
            { field: 'revenue', header: 'reports.columns.revenue', type: 'money' },
            { field: 'averageOrderValue', header: 'reports.columns.avgOrder', type: 'money' }
        ]
    },
    {
        key: 'sales-by-cashier',
        group: 'sales',
        title: 'reports.pages.salesByCashier.title',
        subtitle: 'reports.pages.salesByCashier.subtitle',
        icon: 'pi pi-id-card',
        endpoint: 'v1/reports/sales/by-cashier',
        paged: false,
        defaultRange: 'thisMonth',
        requiresDateRange: true,
        itemLabel: 'reports.items.cashiers',
        filters: [
            { kind: 'dateRange', key: 'dateRange', label: 'reports.filters.period' },
            { kind: 'lookup', key: 'warehouseId', label: 'reports.filters.warehouse', lookup: 'warehouse' }
        ],
        columns: [
            { field: 'cashierName', header: 'reports.columns.cashier', mobilePrimary: true },
            { field: 'orderCount', header: 'reports.columns.orders', type: 'number' },
            { field: 'quantitySold', header: 'reports.columns.qtySold', type: 'number' },
            { field: 'revenue', header: 'reports.columns.revenue', type: 'money' },
            { field: 'averageOrderValue', header: 'reports.columns.avgOrder', type: 'money' }
        ]
    },
    {
        key: 'sales-returns',
        group: 'sales',
        title: 'reports.pages.salesReturns.title',
        subtitle: 'reports.pages.salesReturns.subtitle',
        icon: 'pi pi-replay',
        endpoint: 'v1/reports/sales/returns',
        paged: true,
        defaultRange: 'thisMonth',
        requiresDateRange: true,
        itemLabel: 'reports.items.returns',
        filters: [
            { kind: 'dateRange', key: 'dateRange', label: 'reports.filters.period' },
            { kind: 'search', key: 'search', label: 'common.actions.search', placeholder: 'reports.filters.searchSalesReturn' },
            { kind: 'lookup', key: 'warehouseId', label: 'reports.filters.warehouse', lookup: 'warehouse' }
        ],
        columns: [
            { field: 'returnDate', header: 'reports.columns.returnDate', type: 'date', mobilePrimary: true },
            { field: 'returnNumber', header: 'reports.columns.returnNo' },
            { field: 'soNumber', header: 'reports.columns.soNumber' },
            { field: 'customerName', header: 'reports.columns.customer' },
            { field: 'status', header: 'common.labels.status' },
            { field: 'refundType', header: 'reports.columns.refundType' },
            { field: 'refundAmount', header: 'reports.columns.refundAmount', type: 'money' },
            { field: 'reason', header: 'common.labels.reason' }
        ]
    },
    {
        key: 'payment-collections',
        group: 'sales',
        title: 'reports.pages.paymentCollections.title',
        subtitle: 'reports.pages.paymentCollections.subtitle',
        icon: 'pi pi-wallet',
        endpoint: 'v1/reports/sales/payment-collections',
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
                    { label: 'reports.options.byMethod', value: 'method' }
                ]
            },
            {
                kind: 'select', key: 'paymentMethod', label: 'reports.filters.paymentMethod',
                options: [
                    { label: 'reports.options.cash', value: 'CASH' },
                    { label: 'reports.options.creditCard', value: 'CREDIT_CARD' },
                    { label: 'reports.options.bankTransfer', value: 'BANK_TRANSFER' },
                    { label: 'reports.options.check', value: 'CHECK' }
                ]
            }
        ],
        columns: [
            { field: 'groupKey', header: 'reports.columns.group', mobilePrimary: true },
            { field: 'paymentCount', header: 'reports.columns.payments', type: 'number' },
            { field: 'totalAmount', header: 'common.labels.totalAmount', type: 'money' }
        ],
        chart: {
            type: 'bar',
            labelField: 'groupKey',
            labelType: 'text',
            series: [{ field: 'totalAmount', label: 'reports.series.collected' }]
        }
    },
    {
        key: 'profit-by-product',
        group: 'sales',
        title: 'reports.pages.profitByProduct.title',
        subtitle: 'reports.pages.profitByProduct.subtitle',
        icon: 'pi pi-percentage',
        endpoint: 'v1/reports/sales/profit-by-product',
        paged: true,
        defaultRange: 'thisMonth',
        requiresDateRange: true,
        itemLabel: 'reports.items.products',
        filters: [
            { kind: 'dateRange', key: 'dateRange', label: 'reports.filters.period' },
            { kind: 'search', key: 'search', label: 'common.actions.search', placeholder: 'reports.filters.searchProduct' },
            { kind: 'lookup', key: 'warehouseId', label: 'reports.filters.warehouse', lookup: 'warehouse' },
            { kind: 'lookup', key: 'categoryId', label: 'common.labels.category', lookup: 'category' }
        ],
        columns: [
            { field: 'partNumber', header: 'reports.columns.partNo' },
            { field: 'partName', header: 'reports.columns.product', mobilePrimary: true },
            { field: 'quantitySold', header: 'reports.columns.qtySold', type: 'number' },
            { field: 'netRevenue', header: 'reports.columns.netRevenue', type: 'money' },
            { field: 'cogs', header: 'reports.columns.cogs', type: 'money' },
            { field: 'grossProfit', header: 'reports.columns.grossProfit', type: 'money' },
            { field: 'marginPercent', header: 'reports.columns.marginPercent', type: 'percent' }
        ]
    },
    {
        // Hub-card entry only: its route ('daily-z-report') is registered as a static path in
        // reports.routes.ts ahead of the generic ':reportKey' route, so it always resolves to
        // the bespoke DailyZReportComponent. There is no JSON preview endpoint for this report —
        // it's PDF-generation-only, so this config's endpoint/columns are unused.
        key: 'daily-z-report',
        group: 'sales',
        title: 'reports.dailyZ.title',
        subtitle: 'reports.dailyZ.subtitle',
        icon: 'pi pi-calculator',
        endpoint: 'v1/reports/sales/daily-z-report',
        paged: false,
        defaultRange: 'today',
        requiresDateRange: true,
        itemLabel: 'reports.items.lines',
        filters: [],
        columns: []
    }
];
