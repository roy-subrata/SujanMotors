import { Injectable } from '@angular/core';

/**
 * Centralized status → display mapping.
 *
 * This service is the single source of truth for turning a raw API status string (e.g.
 * "PARTIALLY_SHIPPED") into:
 *  - a PrimeNG `p-tag` / `p-badge` `severity`, and
 *  - the `data-status` value to bind on the shared `.status-pill` element (see
 *    `assets/_data-page.scss`, used by the Parts/Brands/Sales Orders reference pages).
 *
 * It does NOT invent any new colors — the mappings below are lifted as-is from the three
 * places that already implement this logic ad hoc:
 *  - `features/procurement/components/status-badge.component.ts`
 *    (`getPOSeverity` / `getGRNSeverity` / `getPaymentSeverity`)
 *  - `features/warranty/claims-list/claims-list.component.ts` (`getStatusSeverity`)
 *  - `features/inventory/stock-takes/stock-takes.component.ts` (`pillStatus`)
 * plus the generic `.status-pill[data-status="..."]` palette already shared across ~25 migrated
 * list pages in `assets/_data-page.scss`.
 *
 * Rollout tasks (#8/#9) should replace each feature's local switch/`Record` status-color logic
 * with calls into this service instead of reimplementing it. It's fine — and expected — to add
 * more `StatusDomain` entries (and their override tables) here as those features are migrated;
 * just keep reusing the existing five categories/severities rather than introducing new colors.
 */

/** PrimeNG `p-tag` / `p-badge` severity values used across the app. */
export type StatusSeverity = 'success' | 'info' | 'warn' | 'danger' | 'secondary' | 'contrast';

/**
 * Semantic status bucket. Drives both the PrimeNG severity and the `.status-pill` CSS palette —
 * callers that only care about "is this good/bad/in-progress" should use this instead of
 * hardcoding a color.
 */
export type StatusCategory = 'success' | 'warning' | 'info' | 'neutral' | 'danger';

/**
 * Named contexts that need a different mapping than the global default for the same raw status
 * string (e.g. "CONFIRMED" is informational for a Sales Order but "in progress/amber" for a
 * Purchase Order). Mirrors the old `StatusBadgeComponent`'s `StatusType` discriminator, extended
 * with the other two components that had their own switch statements.
 */
export type StatusDomain =
  | 'default'
  | 'purchase-order'
  | 'goods-receipt'
  | 'payment'
  | 'warranty-claim'
  | 'stock-take'
  | 'hr-leave'
  | 'hr-attendance'
  | 'hr-payroll'
  | 'backup'
  | 'sales-order'
  | 'quotation'
  | 'invoice'
  | 'proforma-invoice'
  | 'sales-return'
  | 'customer'
  | 'customer-payment'
  | 'customer-credit-note'
  | 'customer-debit-note'
  | 'technician'
  | 'till-session'
  | 'purchase-return'
  | 'supplier-payment'
  | 'payment-provider'
  | 'warranty-registration';

/** category → PrimeNG severity */
const CATEGORY_SEVERITY: Record<StatusCategory, StatusSeverity> = {
  success: 'success',
  warning: 'warn',
  info: 'info',
  neutral: 'secondary',
  danger: 'danger'
};

/**
 * Global default status → category map, lifted directly from the `.status-pill` palette in
 * `assets/_data-page.scss` (already the cross-feature "shared palette" referenced by
 * `stock-takes.component.ts`). Keys are the raw wire status values, upper-cased.
 */
const DEFAULT_CATEGORY: Record<string, StatusCategory> = {
  // Green — positive / completed states
  ACTIVE: 'success',
  SUCCESS: 'success',
  APPROVED: 'success',
  ACCEPTED: 'success',
  VERIFIED: 'success',
  PROCESSED: 'success',
  RECEIVED: 'success',
  CREDITED: 'success',
  DELIVERED: 'success',
  COMPLETED: 'success',
  PAID: 'success',

  // Amber — in-progress / attention states
  PENDING: 'warning',
  WARNING: 'warning',
  PARTIAL: 'warning',
  PARTIALLY_SHIPPED: 'warning',
  READY_FOR_DELIVERY: 'warning',
  PACKED: 'warning',

  // Blue — informational states
  INFO: 'info',
  CONFIRMED: 'info',
  SHIPPED: 'info',

  // Grey — neutral states
  DRAFT: 'neutral',

  // Red — negative / terminal states
  INACTIVE: 'danger',
  DANGER: 'danger',
  CANCELLED: 'danger',
  REJECTED: 'danger',
  RETURNED: 'danger',
  BLACKLISTED: 'danger',
  SUSPENDED: 'danger',
  UNPAID: 'danger',
  OVERDUE: 'danger'
};

/**
 * The exact set of raw values the `.status-pill` SCSS palette special-cases via
 * `[data-status="..."]`. When a status is one of these, its lower-cased form can be bound
 * directly to `data-status`; otherwise fall back to the generic category bucket name (which the
 * palette also recognizes: "success" | "warning" | "info" | "danger", or "draft" for neutral).
 */
const RECOGNIZED_PILL_VALUES = new Set(Object.keys(DEFAULT_CATEGORY));

/**
 * Per-domain severity overrides — copied verbatim from the existing switch statements so no
 * color choices are changed, only relocated.
 */
const DOMAIN_SEVERITY_OVERRIDES: Partial<Record<StatusDomain, Record<string, StatusSeverity>>> = {
  // features/procurement/components/status-badge.component.ts → getPOSeverity
  'purchase-order': {
    DRAFT: 'secondary',
    SUBMITTED: 'info',
    CONFIRMED: 'warn',
    PARTIAL: 'warn',
    DELIVERED: 'success',
    CANCELLED: 'danger'
  },
  // features/procurement/components/status-badge.component.ts → getGRNSeverity
  'goods-receipt': {
    PENDING: 'secondary',
    VERIFIED: 'info',
    ACCEPTED: 'success',
    REJECTED: 'danger'
  },
  // features/procurement/components/status-badge.component.ts → getPaymentSeverity
  payment: {
    PENDING: 'secondary',
    PROCESSING: 'info',
    CONFIRMED: 'warn',
    RECONCILED: 'success',
    FAILED: 'danger',
    CANCELLED: 'danger'
  },
  // features/warranty/claims-list/claims-list.component.ts → getStatusSeverity
  'warranty-claim': {
    PENDING: 'warn',
    UNDER_REVIEW: 'info',
    APPROVED: 'success',
    REJECTED: 'danger',
    IN_PROGRESS: 'info',
    COMPLETED: 'success',
    CLOSED: 'secondary'
  },
  // features/hr/leave-requests/leave-requests.component.ts → getStatusSeverity
  // (only CANCELLED diverges from DEFAULT_CATEGORY, which maps it to 'danger')
  'hr-leave': {
    PENDING: 'warn',
    APPROVED: 'success',
    REJECTED: 'danger',
    CANCELLED: 'secondary'
  },
  // features/admin/backups/backups.component.ts → statusSeverity
  // NOTE: wire values are PascalCase ("Succeeded", not "SUCCEEDED"), but getSeverity/getCategory
  // uppercase the lookup key before checking this table, so the keys here must be SCREAMING_CASE
  // even though `BackupRecordStatus` itself is PascalCase.
  backup: {
    PENDING: 'secondary',
    RUNNING: 'info',
    SUCCEEDED: 'success',
    UPLOADFAILED: 'warn',
    FAILED: 'danger'
  },
  // features/sales/sales-orders/sales-orders-list/sales-orders-list.component.ts → getStatusSeverity
  // (also reused by sales-order-form.component.ts, which had its own slightly-stale copy)
  'sales-order': {
    PENDING: 'secondary',
    PAID: 'info'
  },
  // features/sales/quotations/{quotations-list,quotation-form}/*.component.ts → getStatusSeverity
  quotation: {
    SENT: 'info',
    ACCEPTED: 'success',
    CONVERTED: 'contrast',
    EXPIRED: 'warn'
  },
  // features/sales/invoices/{invoices-list,invoice-form}/*.component.ts and
  // customer-account-summary.component.ts → getStatusSeverity (all three agreed already)
  invoice: {
    ISSUED: 'info',
    PARTIALLY_PAID: 'warn',
    CANCELLED: 'secondary'
  },
  // features/sales/proforma-invoices/proforma-invoices-list/*.component.ts → getStatusSeverity
  'proforma-invoice': {
    ISSUED: 'success',
    EXPIRED: 'warn'
  },
  // features/sales/sales-returns/sales-returns-list/*.component.ts → getStatusSeverity
  // (RECEIVED was the invalid PrimeNG severity 'primary' in the original switch — not a real
  // p-tag severity, so it silently rendered unstyled; mapped to 'info' here to keep it visually
  // distinct from PROCESSED's 'success' instead of reproducing the bug)
  'sales-return': {
    APPROVED: 'info',
    RECEIVED: 'info'
  },
  // features/sales/customers/{customers-list,customer-detail}/*.component.ts → getStatusSeverity
  // (customers-list and customer-detail previously disagreed on SUSPENDED — 'warn' vs 'danger' —
  // and on unmapped-status fallback; harmonized on customers-list's mapping as the canonical one)
  customer: {
    INACTIVE: 'secondary',
    SUSPENDED: 'warn'
  },
  // features/sales/customer-payments/customer-payment-list.component.ts → getStatusSeverity
  'customer-payment': {
    PROCESSING: 'info'
  },
  // features/sales/debit-notes/debit-notes-list/debit-notes-list.component.ts → getStatusSeverity
  'customer-debit-note': {
    ISSUED: 'info',
    SETTLED: 'success'
  },
  // features/sales/technicians/technicians-list/technicians-list.component.ts → getStatusSeverity
  technician: {
    INACTIVE: 'secondary'
  },
  // features/procurement/purchase-returns/{purchase-returns-list,purchase-returns-form}/*.component.ts
  // → getStatusSeverity (purchase-returns-list had RETURNED as the invalid PrimeNG severity
  // 'primary' — mapped to 'info' to match purchase-returns-form's equivalent branch instead of
  // reproducing the bug)
  'purchase-return': {
    APPROVED: 'info',
    RETURNED: 'info'
  },
  // features/procurement/supplier-payment-summary/supplier-payment-summary.component.ts
  // → getStatusSeverity (used for the mixed-domain supplier ledger entry status, not strictly
  // SupplierPaymentStatus — REFUNDED/RETURNED intentionally diverge from DEFAULT_CATEGORY's
  // RETURNED:'danger' to keep refund-type entries visually distinct from failures)
  'supplier-payment': {
    PENDING: 'secondary',
    PROCESSING: 'info',
    REFUNDED: 'warn',
    RETURNED: 'warn'
  },
  // features/procurement/payment-provider/payment-provider-list.component.ts → getStatusSeverity
  // (the switch also had a dead 'DISABLED' branch → 'danger'; PaymentProviderStatus has no such
  // value on the wire, so it's dropped rather than carried forward)
  'payment-provider': {
    INACTIVE: 'warn'
  },
  // features/warranty/warranties-list/warranties-list.component.ts → getStatusSeverity
  'warranty-registration': {
    EXPIRED: 'warn',
    CLAIMED: 'info',
    VOID: 'danger'
  }
};

/**
 * Per-domain category overrides — for consumers of the `.status-pill` CSS palette that need a
 * bucket different from `DEFAULT_CATEGORY` for the same raw status string. Copied verbatim from
 * `stock-takes.component.ts` → `pillStatus`.
 */
const DOMAIN_CATEGORY_OVERRIDES: Partial<Record<StatusDomain, Record<string, StatusCategory>>> = {
  'stock-take': {
    COUNTING: 'warning',
    REVIEW: 'info',
    COMPLETED: 'success',
    CANCELLED: 'danger'
    // no entry for anything else → falls back to the domain default ('neutral' / "draft")
  }
};

@Injectable({ providedIn: 'root' })
export class StatusDisplayService {
  /**
   * Resolve the semantic category for a raw status value, optionally scoped to a `StatusDomain`
   * that has its own overrides. Unknown values fall back to `'neutral'`.
   */
  getCategory(status: string | null | undefined, domain: StatusDomain = 'default'): StatusCategory {
    const key = (status ?? '').toUpperCase();
    const override = DOMAIN_CATEGORY_OVERRIDES[domain]?.[key];
    if (override) {
      return override;
    }
    return DEFAULT_CATEGORY[key] ?? 'neutral';
  }

  /**
   * Resolve the PrimeNG `p-tag` / `p-badge` severity for a raw status value, optionally scoped to
   * a `StatusDomain` whose colors diverge from the global default for the same raw value.
   */
  getSeverity(status: string | null | undefined, domain: StatusDomain = 'default'): StatusSeverity {
    const key = (status ?? '').toUpperCase();
    const override = DOMAIN_SEVERITY_OVERRIDES[domain]?.[key];
    if (override) {
      return override;
    }
    return CATEGORY_SEVERITY[this.getCategory(status, domain)];
  }

  /**
   * Resolve the value to bind to `[attr.data-status]` on a `.status-pill` element (see
   * `assets/_data-page.scss`). Returns the raw status lower-cased when the palette already
   * special-cases it (e.g. "pending", "cancelled"); otherwise falls back to the generic bucket
   * name the palette also understands ("success" | "warning" | "info" | "danger" | "draft").
   */
  getPillAttr(status: string | null | undefined, domain: StatusDomain = 'default'): string {
    const key = (status ?? '').toUpperCase();
    if (!DOMAIN_CATEGORY_OVERRIDES[domain]?.[key] && RECOGNIZED_PILL_VALUES.has(key)) {
      return key.toLowerCase();
    }
    const category = this.getCategory(status, domain);
    return category === 'neutral' ? 'draft' : category;
  }

  /**
   * Humanize a raw `SCREAMING_SNAKE_CASE` status into Title Case words, e.g.
   * "PARTIALLY_SHIPPED" -> "Partially Shipped". Intended as a fallback display label only —
   * features with i18n keys for their statuses (e.g. Sales Orders, Warranty Claims) should keep
   * using `I18nService` for the actual label and reserve this for values with no translation yet.
   */
  getDefaultLabel(status: string | null | undefined): string {
    if (!status) {
      return '';
    }
    return status
      .split('_')
      .map(word => (word ? word.charAt(0) + word.slice(1).toLowerCase() : word))
      .join(' ');
  }
}
