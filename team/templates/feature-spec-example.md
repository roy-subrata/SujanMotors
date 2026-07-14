# Feature Spec — Low-Stock Reorder Alerts

<!-- EXAMPLE: a completed spec for a real feature in this repo, showing the
     level of detail that gives an LLM enough knowledge to implement without
     guessing. Use it as a calibration reference for your own specs. -->

| | |
|---|---|
| **Status** | Done |
| **Branch** | `feature/reorder-alerts` |
| **Owner** | Roy |
| **Date** | 2026-07-05 |

## 1. Problem / Goal

Stock runs out silently: staff only notice a part is unavailable when a
customer asks for it. Managers want to be told proactively which parts have
fallen to or below their reorder level, so purchasing can act before a
stock-out loses a sale.

## 2. Scope

**In scope:**
- Daily automatic low-stock check with an in-app notification digest
- A manual "check now" trigger for admins
- Deep link from the notification to a filtered low-stock view

**Out of scope:**
- Automatic PO creation from alerts
- Email/SMS delivery of the digest (in-app only for v1)
- Mobile app notification

## 3. Actors & Channels

- [x] Web back-office (admin/manager) — receives the alerts
- [x] Background job / scheduled task — produces the daily digest
- [ ] Web POS, Mobile, Ecommerce — not affected

## 4. Functional Requirements

- FR-1: A background service runs once per day and finds every product whose
  total available stock is at or below its `ReorderLevel`.
- FR-2: Only products with `ReorderLevel > 0` participate (0 = opted out).
- FR-3: The result is broadcast to staff as a single digest notification via
  the existing SignalR notification bell, not one notification per product.
- FR-4: An authorized user can trigger the same check on demand via
  `POST /api/v1/reorder-alerts/run`.
- FR-5: Clicking the notification navigates to `stock?tab=low`, which lists
  the affected products.

## 5. Business Rules & Edge Cases

- Rule: "available" = on-hand minus reserved, in base units, summed across
  warehouses (same availability definition as the POS stock check).
- Rule: variant-level stock — a product is low if the summed availability of
  the part (across variants) is at/below reorder level; per-variant thresholds
  are out of scope for v1.
- Edge case: no products are low → **no notification at all** (no "all good"
  noise).
- Edge case: service restarts twice in a day → the digest must not fire twice;
  track the last-run date.

## 6. Data Model Changes

No schema changes. `Product.ReorderLevel` already exists.

## 7. API Contract

```
POST /api/v1/reorder-alerts/run
Response: 200 { "data": { "lowStockCount": 7 } }
Errors:   403 when caller lacks permission
```

Authorization: `Stock.Manage` permission (Admin bypasses, as everywhere).

## 8. UI / UX

- Notification bell: digest entry "7 products at or below reorder level",
  same visual pattern as existing sale notifications.
- Stock page: existing page gains `?tab=low` query-param handling to
  pre-select the low-stock filter.

## 9. Reference Implementation

- Background service pattern: existing `BackgroundService` implementations in
  `src/AutoPartShop.Api/Services/`
- SignalR staff broadcast: the notification service used for sale
  notifications (`docs/` notification feature)
- Stock availability math: `StockLevelRepository.GetByPartAndVariantAsync`
  usage in `SalesOrderController.CreateQuickSale`

## 10. Non-Functional Requirements

- The daily check must not block startup; run on a timer after app start.
- Query must be a single set-based query, not per-product round-trips
  (catalog is ~10k products).

## 11. Acceptance Criteria

- [x] Set `ReorderLevel = 5` on a product with 3 available → daily run (or
  manual trigger) produces a digest containing that product.
- [x] Product with `ReorderLevel = 0` and 0 stock → never appears.
- [x] Digest click lands on the stock page with the low-stock tab active.
- [x] Trigger endpoint returns 403 for a user without `Stock.Manage`.
- [x] `dotnet build`, `dotnet test`, `ng build` pass.

## 12. Open Questions

- Q: Should purchasing get an email copy? → A: deferred to v2 (out of scope).
