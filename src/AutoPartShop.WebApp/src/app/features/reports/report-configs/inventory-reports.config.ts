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
    },
    {
        key: 'low-stock',
        group: 'inventory',
        title: 'Low Stock',
        subtitle: 'Parts at or below their configured minimum stock level',
        icon: 'pi pi-exclamation-triangle',
        endpoint: 'v1/reports/inventory/low-stock',
        paged: true,
        defaultRange: 'none',
        itemLabel: 'parts',
        filters: [
            { kind: 'lookup', key: 'warehouseId', label: 'Warehouse', lookup: 'warehouse' },
            { kind: 'lookup', key: 'categoryId', label: 'Category', lookup: 'category' }
        ],
        columns: [
            { field: 'partNumber', header: 'Part No.' },
            { field: 'partName', header: 'Product', mobilePrimary: true },
            { field: 'variantName', header: 'Variant' },
            { field: 'categoryName', header: 'Category' },
            { field: 'warehouseName', header: 'Warehouse' },
            { field: 'quantityOnHand', header: 'On Hand', type: 'number' },
            { field: 'minimumStock', header: 'Minimum', type: 'number' },
            { field: 'reorderLevel', header: 'Reorder Level', type: 'number' },
            { field: 'shortfall', header: 'Shortfall', type: 'number' }
        ]
    },
    {
        key: 'stock-movements',
        group: 'inventory',
        title: 'Stock Movement Ledger',
        subtitle: 'Audit trail of every stock in/out/adjustment/transfer',
        icon: 'pi pi-history',
        endpoint: 'v1/reports/inventory/stock-movements',
        paged: true,
        defaultRange: 'last7',
        requiresDateRange: true,
        itemLabel: 'movements',
        filters: [
            { kind: 'dateRange', key: 'dateRange', label: 'Period' },
            { kind: 'lookup', key: 'warehouseId', label: 'Warehouse', lookup: 'warehouse' },
            {
                kind: 'select', key: 'movementType', label: 'Movement Type',
                options: [
                    { label: 'In', value: 'IN' },
                    { label: 'Out', value: 'OUT' },
                    { label: 'Return', value: 'RETURN' },
                    { label: 'Adjust', value: 'ADJUST' },
                    { label: 'Transfer', value: 'TRANSFER' }
                ]
            }
        ],
        columns: [
            { field: 'movementDate', header: 'Date', type: 'date', mobilePrimary: true },
            { field: 'partNumber', header: 'Part No.' },
            { field: 'partName', header: 'Product' },
            { field: 'warehouseName', header: 'Warehouse' },
            { field: 'movementType', header: 'Type' },
            { field: 'quantity', header: 'Quantity', type: 'number' },
            { field: 'reason', header: 'Reason' },
            { field: 'referenceNumber', header: 'Reference' }
        ]
    },
    {
        key: 'expiring-lots',
        group: 'inventory',
        title: 'Expiring Lots',
        subtitle: 'Stock lots nearing or past their expiry date',
        icon: 'pi pi-calendar-times',
        endpoint: 'v1/reports/inventory/expiring-lots',
        paged: true,
        defaultRange: 'none',
        itemLabel: 'lots',
        filters: [
            { kind: 'lookup', key: 'warehouseId', label: 'Warehouse', lookup: 'warehouse' },
            {
                kind: 'select', key: 'daysAhead', label: 'Horizon', default: 90,
                options: [
                    { label: 'Next 30 days', value: 30 },
                    { label: 'Next 90 days', value: 90 },
                    { label: 'Next 180 days', value: 180 },
                    { label: 'Next 365 days', value: 365 }
                ]
            },
            { kind: 'checkbox', key: 'includeExpired', label: 'Include already expired', default: false }
        ],
        columns: [
            { field: 'lotNumber', header: 'Lot No.' },
            { field: 'partName', header: 'Product', mobilePrimary: true },
            { field: 'warehouseName', header: 'Warehouse' },
            { field: 'supplierName', header: 'Supplier' },
            { field: 'expiryDate', header: 'Expiry', type: 'date' },
            { field: 'daysToExpiry', header: 'Days to Expiry', type: 'number' },
            { field: 'quantityAvailable', header: 'Qty Available', type: 'number' },
            { field: 'stockValue', header: 'Stock Value', type: 'money' }
        ]
    },
    {
        key: 'slow-moving-stock',
        group: 'inventory',
        title: 'Slow-Moving / Dead Stock',
        subtitle: 'Stock with no sale in the configured window',
        icon: 'pi pi-inbox',
        endpoint: 'v1/reports/inventory/slow-moving',
        paged: true,
        defaultRange: 'none',
        itemLabel: 'parts',
        filters: [
            { kind: 'lookup', key: 'warehouseId', label: 'Warehouse', lookup: 'warehouse' },
            { kind: 'lookup', key: 'categoryId', label: 'Category', lookup: 'category' },
            {
                kind: 'select', key: 'noSaleDays', label: 'No Sale For', default: 90,
                options: [
                    { label: '30+ days', value: 30 },
                    { label: '60+ days', value: 60 },
                    { label: '90+ days', value: 90 },
                    { label: '180+ days', value: 180 }
                ]
            }
        ],
        columns: [
            { field: 'partNumber', header: 'Part No.' },
            { field: 'partName', header: 'Product', mobilePrimary: true },
            { field: 'categoryName', header: 'Category' },
            { field: 'warehouseName', header: 'Warehouse' },
            { field: 'quantityOnHand', header: 'On Hand', type: 'number' },
            { field: 'stockValue', header: 'Stock Value', type: 'money' },
            { field: 'lastSaleDate', header: 'Last Sale', type: 'date' },
            { field: 'daysSinceLastSale', header: 'Days Since Sale', type: 'number' }
        ]
    }
];
