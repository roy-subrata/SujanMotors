import { ReportPageConfig } from './report-config.model';

/** Inventory report group — backed by api/v1/reports/inventory/*. */
export const INVENTORY_REPORT_CONFIGS: ReportPageConfig[] = [
    {
        key: 'stock-summary',
        group: 'inventory',
        title: 'Stock Summary & Valuation',
        subtitle: 'Current stock per product and warehouse, valued at actual lot cost',
        icon: 'pi pi-warehouse',
        endpoint: 'v1/reports/inventory/stock-summary',
        paged: true,
        hasTotals: true,
        defaultRange: 'none',
        itemLabel: 'stock rows',
        filters: [
            { kind: 'search', key: 'search', label: 'Search', placeholder: 'Search by product name, part no, SKU...' },
            { kind: 'lookup', key: 'warehouseId', label: 'Warehouse', lookup: 'warehouse' },
            { kind: 'lookup', key: 'categoryId', label: 'Category', lookup: 'category' },
            { kind: 'lookup', key: 'brandId', label: 'Brand', lookup: 'brand' },
            { kind: 'checkbox', key: 'includeZeroStock', label: 'Include zero stock', default: false }
        ],
        columns: [
            { field: 'partNumber', header: 'Part No.' },
            { field: 'partName', header: 'Product', mobilePrimary: true },
            { field: 'variantName', header: 'Variant' },
            { field: 'categoryName', header: 'Category' },
            { field: 'warehouseName', header: 'Warehouse' },
            { field: 'quantityOnHand', header: 'On Hand', type: 'number' },
            { field: 'quantityReserved', header: 'Reserved', type: 'number' },
            { field: 'quantityDamaged', header: 'Damaged', type: 'number' },
            { field: 'quantityAvailable', header: 'Available', type: 'number' },
            { field: 'averageCost', header: 'Avg Cost', type: 'money' },
            { field: 'stockValue', header: 'Stock Value', type: 'money' }
        ],
        totals: [
            { field: 'totalStockValue', label: 'Total Stock Value', type: 'money' },
            { field: 'distinctPartCount', label: 'Distinct Products', type: 'number' },
            { field: 'totalQuantityOnHand', label: 'Total On Hand', type: 'number' }
        ]
    }
];
