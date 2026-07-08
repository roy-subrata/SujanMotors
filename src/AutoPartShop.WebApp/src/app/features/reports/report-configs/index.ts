import { ReportGroup, ReportPageConfig } from './report-config.model';
import { SALES_REPORT_CONFIGS } from './sales-reports.config';
import { INVENTORY_REPORT_CONFIGS } from './inventory-reports.config';

export * from './report-config.model';

/** All configured reports, in hub display order. Purchase/financial groups arrive in later phases. */
export const ALL_REPORT_CONFIGS: ReportPageConfig[] = [
    ...SALES_REPORT_CONFIGS,
    ...INVENTORY_REPORT_CONFIGS
];

/** Lookup by route key used by the generic report page. */
export const REPORT_REGISTRY: ReadonlyMap<string, ReportPageConfig> =
    new Map(ALL_REPORT_CONFIGS.map(config => [config.key, config]));

export interface ReportGroupInfo {
    group: ReportGroup;
    title: string;
    icon: string;
    reports: ReportPageConfig[];
}

const GROUP_META: { group: ReportGroup; title: string; icon: string }[] = [
    { group: 'sales', title: 'Sales Reports', icon: 'pi pi-shopping-cart' },
    { group: 'inventory', title: 'Inventory Reports', icon: 'pi pi-warehouse' },
    { group: 'purchase', title: 'Purchase Reports', icon: 'pi pi-truck' },
    { group: 'financial', title: 'Financial Reports', icon: 'pi pi-wallet' }
];

/** Groups that currently have reports, for the hub page. */
export const REPORT_GROUPS: ReportGroupInfo[] = GROUP_META
    .map(meta => ({ ...meta, reports: ALL_REPORT_CONFIGS.filter(c => c.group === meta.group) }))
    .filter(g => g.reports.length > 0);
