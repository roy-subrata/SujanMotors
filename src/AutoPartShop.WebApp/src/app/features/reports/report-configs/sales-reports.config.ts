import { ReportPageConfig } from './report-config.model';

/** Sales report group — backed by api/v1/reports/sales/*. */
export const SALES_REPORT_CONFIGS: ReportPageConfig[] = [
    {
        key: 'sales-summary',
        group: 'sales',
        title: 'Sales Summary',
        subtitle: 'Orders, revenue, discount and tax bucketed by day, week or month',
        icon: 'pi pi-chart-line',
        endpoint: 'v1/reports/sales/summary',
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
            },
            { kind: 'lookup', key: 'warehouseId', label: 'Warehouse', lookup: 'warehouse' },
            {
                kind: 'select', key: 'channel', label: 'Channel',
                options: [
                    { label: 'POS', value: 'POS' },
                    { label: 'E-commerce', value: 'ECOMMERCE' }
                ]
            }
        ],
        columns: [
            { field: 'periodStart', header: 'Period', type: 'date', mobilePrimary: true },
            { field: 'orderCount', header: 'Orders', type: 'number' },
            { field: 'grossAmount', header: 'Gross', type: 'money' },
            { field: 'discountAmount', header: 'Discount', type: 'money' },
            { field: 'taxAmount', header: 'Tax', type: 'money' },
            { field: 'netAmount', header: 'Net', type: 'money' },
            { field: 'grandTotal', header: 'Grand Total', type: 'money' },
            { field: 'averageOrderValue', header: 'Avg Order', type: 'money' }
        ],
        chart: {
            type: 'line',
            labelField: 'periodStart',
            labelType: 'date',
            series: [
                { field: 'netAmount', label: 'Net Sales' },
                { field: 'grandTotal', label: 'Grand Total' }
            ]
        }
    },
    {
        key: 'sales-by-product',
        group: 'sales',
        title: 'Sales by Product',
        subtitle: 'Quantity sold and revenue per product, best sellers first',
        icon: 'pi pi-box',
        endpoint: 'v1/reports/sales/by-product',
        paged: true,
        defaultRange: 'thisMonth',
        requiresDateRange: true,
        itemLabel: 'products',
        filters: [
            { kind: 'dateRange', key: 'dateRange', label: 'Period' },
            { kind: 'search', key: 'search', label: 'Search', placeholder: 'Search by product name, part no, SKU...' },
            { kind: 'lookup', key: 'warehouseId', label: 'Warehouse', lookup: 'warehouse' },
            { kind: 'lookup', key: 'categoryId', label: 'Category', lookup: 'category' },
            { kind: 'lookup', key: 'brandId', label: 'Brand', lookup: 'brand' }
        ],
        columns: [
            { field: 'partNumber', header: 'Part No.' },
            { field: 'partName', header: 'Product', mobilePrimary: true },
            { field: 'sku', header: 'SKU' },
            { field: 'categoryName', header: 'Category' },
            { field: 'brandName', header: 'Brand' },
            { field: 'quantitySold', header: 'Qty Sold', type: 'number' },
            { field: 'grossRevenue', header: 'Gross Revenue', type: 'money' },
            { field: 'discountAmount', header: 'Discount', type: 'money' },
            { field: 'netRevenue', header: 'Net Revenue', type: 'money' }
        ]
    }
];
