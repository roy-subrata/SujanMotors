import { ReportPageConfig } from './report-config.model';

/**
 * Inventory report group — backed by api/v1/reports/inventory/*.
 * All user-facing strings below are i18n keys (see ReportPageConfig docs).
 */
export const INVENTORY_REPORT_CONFIGS: ReportPageConfig[] = [
    {
        key: 'stock-summary',
        group: 'inventory',
        title: 'reports.pages.stockSummary.title',
        subtitle: 'reports.pages.stockSummary.subtitle',
        icon: 'pi pi-warehouse',
        endpoint: 'v1/reports/inventory/stock-summary',
        paged: true,
        hasTotals: true,
        defaultRange: 'none',
        itemLabel: 'reports.items.stockRows',
        filters: [
            { kind: 'search', key: 'search', label: 'common.actions.search', placeholder: 'reports.filters.searchProduct' },
            { kind: 'lookup', key: 'warehouseId', label: 'reports.filters.warehouse', lookup: 'warehouse' },
            { kind: 'lookup', key: 'categoryId', label: 'common.labels.category', lookup: 'category' },
            { kind: 'lookup', key: 'brandId', label: 'reports.filters.brand', lookup: 'brand' },
            { kind: 'checkbox', key: 'includeZeroStock', label: 'reports.filters.includeZeroStock', default: false }
        ],
        columns: [
            { field: 'partNumber', header: 'reports.columns.partNo' },
            { field: 'partName', header: 'reports.columns.product', mobilePrimary: true },
            { field: 'variantName', header: 'reports.columns.variant' },
            { field: 'categoryName', header: 'common.labels.category' },
            { field: 'warehouseName', header: 'reports.filters.warehouse' },
            { field: 'quantityOnHand', header: 'reports.columns.onHand', type: 'number' },
            { field: 'quantityReserved', header: 'reports.columns.reserved', type: 'number' },
            { field: 'quantityDamaged', header: 'reports.columns.damaged', type: 'number' },
            { field: 'quantityAvailable', header: 'reports.columns.available', type: 'number' },
            { field: 'averageCost', header: 'reports.columns.avgCost', type: 'money' },
            { field: 'stockValue', header: 'reports.columns.stockValue', type: 'money' }
        ],
        totals: [
            { field: 'totalStockValue', label: 'reports.totals.totalStockValue', type: 'money' },
            { field: 'distinctPartCount', label: 'reports.totals.distinctProducts', type: 'number' },
            { field: 'totalQuantityOnHand', label: 'reports.totals.totalOnHand', type: 'number' }
        ]
    },
    {
        key: 'low-stock',
        group: 'inventory',
        title: 'reports.pages.lowStock.title',
        subtitle: 'reports.pages.lowStock.subtitle',
        icon: 'pi pi-exclamation-triangle',
        endpoint: 'v1/reports/inventory/low-stock',
        paged: true,
        defaultRange: 'none',
        itemLabel: 'reports.items.parts',
        filters: [
            { kind: 'lookup', key: 'warehouseId', label: 'reports.filters.warehouse', lookup: 'warehouse' },
            { kind: 'lookup', key: 'categoryId', label: 'common.labels.category', lookup: 'category' }
        ],
        columns: [
            { field: 'partNumber', header: 'reports.columns.partNo' },
            { field: 'partName', header: 'reports.columns.product', mobilePrimary: true },
            { field: 'variantName', header: 'reports.columns.variant' },
            { field: 'categoryName', header: 'common.labels.category' },
            { field: 'warehouseName', header: 'reports.filters.warehouse' },
            { field: 'quantityOnHand', header: 'reports.columns.onHand', type: 'number' },
            { field: 'minimumStock', header: 'reports.columns.minimum', type: 'number' },
            { field: 'reorderLevel', header: 'reports.columns.reorderLevel', type: 'number' },
            { field: 'shortfall', header: 'reports.columns.shortfall', type: 'number' }
        ]
    },
    {
        key: 'stock-movements',
        group: 'inventory',
        title: 'reports.pages.stockMovements.title',
        subtitle: 'reports.pages.stockMovements.subtitle',
        icon: 'pi pi-history',
        endpoint: 'v1/reports/inventory/stock-movements',
        paged: true,
        defaultRange: 'last7',
        requiresDateRange: true,
        itemLabel: 'reports.items.movements',
        filters: [
            { kind: 'dateRange', key: 'dateRange', label: 'reports.filters.period' },
            { kind: 'lookup', key: 'warehouseId', label: 'reports.filters.warehouse', lookup: 'warehouse' },
            {
                kind: 'select', key: 'movementType', label: 'reports.filters.movementType',
                options: [
                    { label: 'reports.options.movementIn', value: 'IN' },
                    { label: 'reports.options.movementOut', value: 'OUT' },
                    { label: 'reports.options.movementReturn', value: 'RETURN' },
                    { label: 'reports.options.movementAdjust', value: 'ADJUST' },
                    { label: 'reports.options.movementTransfer', value: 'TRANSFER' }
                ]
            }
        ],
        columns: [
            { field: 'movementDate', header: 'common.labels.date', type: 'date', mobilePrimary: true },
            { field: 'partNumber', header: 'reports.columns.partNo' },
            { field: 'partName', header: 'reports.columns.product' },
            { field: 'warehouseName', header: 'reports.filters.warehouse' },
            { field: 'movementType', header: 'common.labels.type' },
            { field: 'quantity', header: 'common.labels.quantity', type: 'number' },
            { field: 'reason', header: 'common.labels.reason' },
            { field: 'referenceNumber', header: 'common.labels.reference' }
        ]
    },
    {
        key: 'expiring-lots',
        group: 'inventory',
        title: 'reports.pages.expiringLots.title',
        subtitle: 'reports.pages.expiringLots.subtitle',
        icon: 'pi pi-calendar-times',
        endpoint: 'v1/reports/inventory/expiring-lots',
        paged: true,
        defaultRange: 'none',
        itemLabel: 'reports.items.lots',
        filters: [
            { kind: 'lookup', key: 'warehouseId', label: 'reports.filters.warehouse', lookup: 'warehouse' },
            {
                kind: 'select', key: 'daysAhead', label: 'reports.filters.horizon', default: 90,
                options: [
                    { label: 'reports.options.next30Days', value: 30 },
                    { label: 'reports.options.next90Days', value: 90 },
                    { label: 'reports.options.next180Days', value: 180 },
                    { label: 'reports.options.next365Days', value: 365 }
                ]
            },
            { kind: 'checkbox', key: 'includeExpired', label: 'reports.filters.includeExpired', default: false }
        ],
        columns: [
            { field: 'lotNumber', header: 'reports.columns.lotNo' },
            { field: 'partName', header: 'reports.columns.product', mobilePrimary: true },
            { field: 'warehouseName', header: 'reports.filters.warehouse' },
            { field: 'supplierName', header: 'reports.columns.supplier' },
            { field: 'expiryDate', header: 'reports.columns.expiry', type: 'date' },
            { field: 'daysToExpiry', header: 'reports.columns.daysToExpiry', type: 'number' },
            { field: 'quantityAvailable', header: 'reports.columns.qtyAvailable', type: 'number' },
            { field: 'stockValue', header: 'reports.columns.stockValue', type: 'money' }
        ]
    },
    {
        key: 'slow-moving-stock',
        group: 'inventory',
        title: 'reports.pages.slowMovingStock.title',
        subtitle: 'reports.pages.slowMovingStock.subtitle',
        icon: 'pi pi-inbox',
        endpoint: 'v1/reports/inventory/slow-moving',
        paged: true,
        defaultRange: 'none',
        itemLabel: 'reports.items.parts',
        filters: [
            { kind: 'lookup', key: 'warehouseId', label: 'reports.filters.warehouse', lookup: 'warehouse' },
            { kind: 'lookup', key: 'categoryId', label: 'common.labels.category', lookup: 'category' },
            {
                kind: 'select', key: 'noSaleDays', label: 'reports.filters.noSaleFor', default: 90,
                options: [
                    { label: 'reports.options.days30Plus', value: 30 },
                    { label: 'reports.options.days60Plus', value: 60 },
                    { label: 'reports.options.days90Plus', value: 90 },
                    { label: 'reports.options.days180Plus', value: 180 }
                ]
            }
        ],
        columns: [
            { field: 'partNumber', header: 'reports.columns.partNo' },
            { field: 'partName', header: 'reports.columns.product', mobilePrimary: true },
            { field: 'categoryName', header: 'common.labels.category' },
            { field: 'warehouseName', header: 'reports.filters.warehouse' },
            { field: 'quantityOnHand', header: 'reports.columns.onHand', type: 'number' },
            { field: 'stockValue', header: 'reports.columns.stockValue', type: 'money' },
            { field: 'lastSaleDate', header: 'reports.columns.lastSale', type: 'date' },
            { field: 'daysSinceLastSale', header: 'reports.columns.daysSinceSale', type: 'number' }
        ]
    }
];
