import { ReportPageConfig } from './report-config.model';

/** Purchase report group — backed by api/v1/reports/purchase/*. */
export const PURCHASE_REPORT_CONFIGS: ReportPageConfig[] = [
    {
        key: 'purchase-summary',
        group: 'purchase',
        title: 'Purchase Summary',
        subtitle: 'Purchase order totals bucketed by day, week or month',
        icon: 'pi pi-chart-line',
        endpoint: 'v1/reports/purchase/summary',
        paged: false,
        defaultRange: 'thisMonth',
        requiresDateRange: true,
        itemLabel: 'periods',
        filters: [
            { kind: 'dateRange', key: 'dateRange', label: 'Period' },
            {
                kind: 'select', key: 'groupBy', label: 'Group By', default: 'day',
                options: [
                    { label: 'Daily', value: 'day' },
                    { label: 'Weekly', value: 'week' },
                    { label: 'Monthly', value: 'month' }
                ]
            }
        ],
        columns: [
            { field: 'periodStart', header: 'Period', type: 'date', mobilePrimary: true },
            { field: 'poCount', header: 'POs', type: 'number' },
            { field: 'totalAmount', header: 'Total Amount', type: 'money' },
            { field: 'paidAmount', header: 'Paid', type: 'money' },
            { field: 'outstanding', header: 'Outstanding', type: 'money' }
        ],
        chart: {
            type: 'line',
            labelField: 'periodStart',
            labelType: 'date',
            series: [
                { field: 'totalAmount', label: 'Total Amount' },
                { field: 'paidAmount', label: 'Paid' }
            ]
        }
    },
    {
        key: 'purchases-by-supplier',
        group: 'purchase',
        title: 'Purchases by Supplier',
        subtitle: 'Purchases, received value, payments and balance per supplier for the period',
        icon: 'pi pi-truck',
        endpoint: 'v1/reports/purchase/by-supplier',
        paged: true,
        defaultRange: 'thisMonth',
        requiresDateRange: true,
        itemLabel: 'suppliers',
        filters: [
            { kind: 'dateRange', key: 'dateRange', label: 'Period' },
            { kind: 'search', key: 'search', label: 'Search', placeholder: 'Search by supplier name or code...' }
        ],
        columns: [
            { field: 'supplierCode', header: 'Code' },
            { field: 'supplierName', header: 'Supplier', mobilePrimary: true },
            { field: 'poCount', header: 'POs', type: 'number' },
            { field: 'totalAmount', header: 'Total Amount', type: 'money' },
            { field: 'receivedValue', header: 'Received Value', type: 'money' },
            { field: 'paidAmount', header: 'Paid', type: 'money' },
            { field: 'returnedValue', header: 'Returned Value', type: 'money' },
            { field: 'balance', header: 'Balance', type: 'money' }
        ]
    },
    {
        key: 'purchase-returns',
        group: 'purchase',
        title: 'Purchase Returns',
        subtitle: 'Returns to suppliers and their settlement status',
        icon: 'pi pi-replay',
        endpoint: 'v1/reports/purchase/returns',
        paged: true,
        defaultRange: 'thisMonth',
        requiresDateRange: true,
        itemLabel: 'returns',
        filters: [
            { kind: 'dateRange', key: 'dateRange', label: 'Period' }
        ],
        columns: [
            { field: 'returnDate', header: 'Return Date', type: 'date', mobilePrimary: true },
            { field: 'returnNumber', header: 'Return No.' },
            { field: 'poNumber', header: 'PO Number' },
            { field: 'supplierName', header: 'Supplier' },
            { field: 'status', header: 'Status' },
            { field: 'settlementStatus', header: 'Settlement' },
            { field: 'refundAmount', header: 'Refund Amount', type: 'money' }
        ]
    }
];
