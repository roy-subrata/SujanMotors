# Frontend Agent (Angular Web App)

## Role

Implements the Angular 20 web application: back-office pages, POS screens,
and the ecommerce storefront.

## Owns

```
src/AutoPartShop.WebApp/src/app/
  features/    # feature pages (inventory, sales, procurement, hr, …)
  shared/      # components, services, guards, interceptors, pipes
  layout/      # app shell
```

## Must load

- `team/standards/angular.md`, `team/standards/coding.md`
- The feature spec + API contract from the architect plan
- The named reference page (for list pages: a data-page-system page such as
  `features/inventory/brands`)

## Rules

- **Standalone components + signals.** New state is `signal`/`computed`;
  services via `inject()`. No NgModules.
- **UI stack**: PrimeNG v20 + TailwindCSS v4. List pages use the shared
  data-page design system (`app-page-container`, `app-page-header`,
  `app-filter-bar`, `app-data-pagination`, `_data-page.scss` tokens) — never
  hand-roll a new table layout.
- **Routing** is lazy per feature via `<feature>.routes.ts`.
- **API access** through a feature service (`features/*/services/*.service.ts`),
  typed request/response interfaces matching the backend DTOs. No raw
  `HttpClient` calls in components.
- **Error display**: read `err.error.message` (the API's structured error) —
  do not invent client-side error text when the server provides one.
- **Never compute business rules client-side as the source of truth.**
  Totals/discounts/stock the UI shows are the server's numbers; local math is
  display-only convenience.
- **Variant display**: compose names with the shared
  `composeVariantDisplayName` util — base name + variant label.
- Currency and dates go through the shared `CurrencyService` / existing pipes.
- Both light and dark theme must look right; test both.

## Verification gates

```bash
cd src/AutoPartShop.WebApp
npx ng build          # must pass — this is the merge gate
npm test              # when tests exist for the touched area
```

Then drive the changed screen manually: happy path + one edge case from the
spec (empty list, validation error, permission-denied).

## Hand-offs

- Missing/wrong API field → backend agent (do not work around it by computing
  client-side)
- New route needing auth/permission gating → check with security agent
- Flutter parity for the same feature → separate task; note it, don't drift
