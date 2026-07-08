/**
 * Config-driven report pages: every standard report (filter bar + optional chart + table +
 * pagination + exports) is described by a ReportPageConfig and rendered by the shared
 * ReportPageComponent, so adding a report means adding a config + backend endpoint — not a
 * new component. Bespoke pages (e.g. Profit & Loss statement) get their own component.
 *
 * Keep column lists in sync with the API's ReportColumnMaps (export column definitions).
 */

export type ReportGroup = 'sales' | 'inventory' | 'purchase' | 'financial';

export interface ReportColumnDef {
    /** camelCase field name on the row object returned by the API. */
    field: string;
    header: string;
    type?: 'text' | 'number' | 'money' | 'percent' | 'date';
    /** Used as the card title in the mobile card view. */
    mobilePrimary?: boolean;
}

export interface ReportSelectOption {
    label: string;
    /** Number for numeric ReportQuery fields (e.g. daysAhead) so it serializes as a JSON number, not a string. */
    value: string | number;
}

export interface ReportFilterDef {
    kind: 'dateRange' | 'search' | 'select' | 'lookup' | 'checkbox';
    /** ReportQuery property the value is posted as (ignored for dateRange/search). */
    key: string;
    label: string;
    /** For kind 'lookup': which reference list fills the dropdown. */
    lookup?: 'warehouse' | 'category' | 'brand';
    /** For kind 'select': static options. */
    options?: ReportSelectOption[];
    default?: unknown;
    placeholder?: string;
}

export interface ReportChartDef {
    type: 'line' | 'bar' | 'pie' | 'doughnut';
    labelField: string;
    labelType?: 'date' | 'text';
    series: { field: string; label: string }[];
}

/** One entry of the grand-totals strip shown above the table (reports with hasTotals). */
export interface ReportTotalDef {
    field: string;
    label: string;
    type?: 'money' | 'number';
}

export interface ReportPageConfig {
    /** Route segment: /reports/<key>. */
    key: string;
    group: ReportGroup;
    title: string;
    subtitle: string;
    /** PrimeIcons class shown on the hub card. */
    icon: string;
    /** API path relative to environment.apiUrl, e.g. 'v1/reports/sales/summary'. */
    endpoint: string;
    /** Paged responses are { data, pagination }; non-paged are a plain row array. */
    paged: boolean;
    /** Response carries a totals block ({ data, pagination, totals }). */
    hasTotals?: boolean;
    /** Preset applied to the date range on first load; 'none' hides the date filter default. */
    defaultRange?: 'today' | 'last7' | 'thisMonth' | 'lastMonth' | 'none';
    /** Server rejects the request without a date range (400). */
    requiresDateRange?: boolean;
    filters: ReportFilterDef[];
    columns: ReportColumnDef[];
    chart?: ReportChartDef;
    totals?: ReportTotalDef[];
    /** Pagination noun, e.g. 'products'. */
    itemLabel?: string;
}
