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
    },
    {
        key: 'sales-by-category',
        group: 'sales',
        title: 'Sales by Category',
        subtitle: 'Revenue split across product categories, with share of total',
        icon: 'pi pi-tags',
        endpoint: 'v1/reports/sales/by-category',
        paged: false,
        defaultRange: 'thisMonth',
        requiresDateRange: true,
        itemLabel: 'categories',
        filters: [
            { kind: 'dateRange', key: 'dateRange', label: 'Period' },
            { kind: 'lookup', key: 'warehouseId', label: 'Warehouse', lookup: 'warehouse' }
        ],
        columns: [
            { field: 'categoryName', header: 'Category', mobilePrimary: true },
            { field: 'orderCount', header: 'Orders', type: 'number' },
            { field: 'quantitySold', header: 'Qty Sold', type: 'number' },
            { field: 'netRevenue', header: 'Net Revenue', type: 'money' },
            { field: 'percentOfTotal', header: '% of Total', type: 'percent' }
        ],
        chart: {
            type: 'pie',
            labelField: 'categoryName',
            labelType: 'text',
            series: [{ field: 'netRevenue', label: 'Net Revenue' }]
        }
    },
    {
        key: 'sales-by-customer',
        group: 'sales',
        title: 'Sales by Customer',
        subtitle: 'Revenue, payments and outstanding balance per customer',
        icon: 'pi pi-users',
        endpoint: 'v1/reports/sales/by-customer',
        paged: true,
        defaultRange: 'thisMonth',
        requiresDateRange: true,
        itemLabel: 'customers',
        filters: [
            { kind: 'dateRange', key: 'dateRange', label: 'Period' },
            { kind: 'search', key: 'search', label: 'Search', placeholder: 'Search by customer name or code...' },
            {
                kind: 'select', key: 'customerType', label: 'Customer Type',
                options: [
                    { label: 'Retail', value: 'RETAIL' },
                    { label: 'Wholesale', value: 'WHOLESALE' },
                    { label: 'Corporate', value: 'CORPORATE' },
                    { label: 'Distributor', value: 'DISTRIBUTOR' }
                ]
            }
        ],
        columns: [
            { field: 'customerCode', header: 'Code' },
            { field: 'customerName', header: 'Customer', mobilePrimary: true },
            { field: 'customerType', header: 'Type' },
            { field: 'orderCount', header: 'Orders', type: 'number' },
            { field: 'revenue', header: 'Revenue', type: 'money' },
            { field: 'paidAmount', header: 'Paid', type: 'money' },
            { field: 'outstanding', header: 'Outstanding', type: 'money' },
            { field: 'lastPurchaseDate', header: 'Last Purchase', type: 'date' }
        ]
    },
    {
        key: 'sales-by-salesperson',
        group: 'sales',
        title: 'Sales by Salesperson',
        subtitle: 'Orders and revenue per technician; unassigned orders group separately',
        icon: 'pi pi-user',
        endpoint: 'v1/reports/sales/by-salesperson',
        paged: false,
        defaultRange: 'thisMonth',
        requiresDateRange: true,
        itemLabel: 'salespeople',
        filters: [
            { kind: 'dateRange', key: 'dateRange', label: 'Period' },
            { kind: 'lookup', key: 'warehouseId', label: 'Warehouse', lookup: 'warehouse' }
        ],
        columns: [
            { field: 'technicianName', header: 'Salesperson', mobilePrimary: true },
            { field: 'orderCount', header: 'Orders', type: 'number' },
            { field: 'quantitySold', header: 'Qty Sold', type: 'number' },
            { field: 'revenue', header: 'Revenue', type: 'money' },
            { field: 'averageOrderValue', header: 'Avg Order', type: 'money' }
        ]
    },
    {
        key: 'sales-by-cashier',
        group: 'sales',
        title: 'Sales by Cashier',
        subtitle: 'Orders and revenue per staff user who processed the sale',
        icon: 'pi pi-id-card',
        endpoint: 'v1/reports/sales/by-cashier',
        paged: false,
        defaultRange: 'thisMonth',
        requiresDateRange: true,
        itemLabel: 'cashiers',
        filters: [
            { kind: 'dateRange', key: 'dateRange', label: 'Period' },
            { kind: 'lookup', key: 'warehouseId', label: 'Warehouse', lookup: 'warehouse' }
        ],
        columns: [
            { field: 'cashierName', header: 'Cashier', mobilePrimary: true },
            { field: 'orderCount', header: 'Orders', type: 'number' },
            { field: 'quantitySold', header: 'Qty Sold', type: 'number' },
            { field: 'revenue', header: 'Revenue', type: 'money' },
            { field: 'averageOrderValue', header: 'Avg Order', type: 'money' }
        ]
    },
    {
        key: 'sales-returns',
        group: 'sales',
        title: 'Sales Returns',
        subtitle: 'Customer returns and refunds in the period',
        icon: 'pi pi-replay',
        endpoint: 'v1/reports/sales/returns',
        paged: true,
        defaultRange: 'thisMonth',
        requiresDateRange: true,
        itemLabel: 'returns',
        filters: [
            { kind: 'dateRange', key: 'dateRange', label: 'Period' },
            { kind: 'search', key: 'search', label: 'Search', placeholder: 'Search by return no, SO no, customer...' },
            { kind: 'lookup', key: 'warehouseId', label: 'Warehouse', lookup: 'warehouse' }
        ],
        columns: [
            { field: 'returnDate', header: 'Return Date', type: 'date', mobilePrimary: true },
            { field: 'returnNumber', header: 'Return No.' },
            { field: 'soNumber', header: 'SO Number' },
            { field: 'customerName', header: 'Customer' },
            { field: 'status', header: 'Status' },
            { field: 'refundType', header: 'Refund Type' },
            { field: 'refundAmount', header: 'Refund Amount', type: 'money' },
            { field: 'reason', header: 'Reason' }
        ]
    },
    {
        key: 'payment-collections',
        group: 'sales',
        title: 'Payment Collections',
        subtitle: 'Completed customer payments grouped by day or method',
        icon: 'pi pi-wallet',
        endpoint: 'v1/reports/sales/payment-collections',
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
                    { label: 'By Method', value: 'method' }
                ]
            },
            {
                kind: 'select', key: 'paymentMethod', label: 'Payment Method',
                options: [
                    { label: 'Cash', value: 'CASH' },
                    { label: 'Credit Card', value: 'CREDIT_CARD' },
                    { label: 'Bank Transfer', value: 'BANK_TRANSFER' },
                    { label: 'Check', value: 'CHECK' }
                ]
            }
        ],
        columns: [
            { field: 'groupKey', header: 'Group', mobilePrimary: true },
            { field: 'paymentCount', header: 'Payments', type: 'number' },
            { field: 'totalAmount', header: 'Total Amount', type: 'money' }
        ],
        chart: {
            type: 'bar',
            labelField: 'groupKey',
            labelType: 'text',
            series: [{ field: 'totalAmount', label: 'Collected' }]
        }
    },
    {
        key: 'profit-by-product',
        group: 'sales',
        title: 'Profit by Product',
        subtitle: 'Net revenue vs. actual cost (COGS) and margin per product',
        icon: 'pi pi-percentage',
        endpoint: 'v1/reports/sales/profit-by-product',
        paged: true,
        defaultRange: 'thisMonth',
        requiresDateRange: true,
        itemLabel: 'products',
        filters: [
            { kind: 'dateRange', key: 'dateRange', label: 'Period' },
            { kind: 'search', key: 'search', label: 'Search', placeholder: 'Search by product name, part no, SKU...' },
            { kind: 'lookup', key: 'warehouseId', label: 'Warehouse', lookup: 'warehouse' },
            { kind: 'lookup', key: 'categoryId', label: 'Category', lookup: 'category' }
        ],
        columns: [
            { field: 'partNumber', header: 'Part No.' },
            { field: 'partName', header: 'Product', mobilePrimary: true },
            { field: 'quantitySold', header: 'Qty Sold', type: 'number' },
            { field: 'netRevenue', header: 'Net Revenue', type: 'money' },
            { field: 'cogs', header: 'COGS', type: 'money' },
            { field: 'grossProfit', header: 'Gross Profit', type: 'money' },
            { field: 'marginPercent', header: 'Margin %', type: 'percent' }
        ]
    }
];
