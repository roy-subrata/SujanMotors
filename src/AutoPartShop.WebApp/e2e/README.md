# E2E tests (Playwright)

Covers: login, product create/edit/search, supplier creation, customer creation
(+ duplicate-phone rejection), the full purchase→stock→sale→stock business flow with a
database-level assertion, permission/role enforcement, invoices, reports, barcode label
printing, purchase returns, sales returns, till sessions (cash drop), customer & supplier
advance payments, a full warranty registration → claim → Closed lifecycle, catalog
master-data CRUD (categories/brands/units/discounts/warehouses), vehicles & technicians,
a full stock-take lifecycle (snapshot → count → review → approve), a full sales-order
lifecycle via the standalone form (create → confirm → generate proforma → ready for
delivery → generate challan → deliver), debit notes, finance/audit/notifications smoke
coverage, and HR (employees, attendance, leave requests, holidays, shifts, salary
advances, payroll smoke).

## Prerequisites

Two servers, already running, **before** you invoke Playwright — it does not start or
manage them:

```bash
# 1. SQL Server (if not already up)
docker compose -f deployment/docker-compose.yml up -d autopartshop.db

# 2. API — Development env, port 5001, uses AutoPartShopDevDb (NOT the prod DB the
#    docker-compose "autopartshop.api" container serves — keep this separate so the
#    suite's writes never touch real data)
ASPNETCORE_ENVIRONMENT=Development dotnet run --project ../../src/AutoPartShop.Api

# 3. Angular dev server — port 4301 (4200 is normally taken by the docker web container)
npm run start -- --port 4301 --proxy-config proxy.conf.json
```

The business-flow spec (`05-business-flow.spec.ts`) also shells out to
`docker exec autopartshop.db sqlcmd ...` to confirm stock levels directly against the
database, so the `autopartshop.db` container specifically must be the one running (not a
different SQL Server instance).

## Running

```bash
npm run e2e            # headless, once
npm run e2e:ui         # interactive UI mode
npm run e2e:report     # open the last HTML report
```

`auth.setup.ts` runs first (via the `setup` project in `playwright.config.ts`), logs in
as `admin`/`Admin@1990` through the real login form, and saves the session to
`e2e/.auth/admin.json` for every other spec to reuse — specs don't each pay the login
cost. `01-login.spec.ts` and `06-permissions.spec.ts` opt back out of that saved session
(`test.use({ storageState: { cookies: [], origins: [] } })`) since they're specifically
testing unauthenticated/differently-privileged access.

`06-permissions.spec.ts` depends on a limited-privilege user that must exist first:

- username `e2elimiteduser`, password `TestPass123!`, role `User` only.
- Created once via Admin Settings → Users → Add User, then Assign Roles → `User`.

If that user doesn't exist, only the permissions spec fails — every other spec is
independent.

## Notes on flakiness sources found while writing this suite

- Several PrimeNG fields (the date picker, the POS payment amount field) parse
  **keystrokes** as they arrive — Playwright's `.fill()` sets the DOM value directly and
  skips that parsing, leaving the field looking correct but the form control invalid.
  Use `.click()` + `.pressSequentially()` for those; plain `.fill()` is fine for ordinary
  text/number inputs.
- Several "auto-generated code" placeholders (`p-select`/`p-autocomplete` wrapper
  components) render their placeholder text on both the wrapper element and the real
  `<input>` inside it — `getByPlaceholder` matches both. Use `.last()`.
- Several page headers render twice (once in the fixed topbar, once in the page's own
  header) — `getByRole('heading', { name })` needs `.first()`.
- The Parts list search box only searches on Enter or the search button — typing alone
  (`.fill()`) does not trigger it, unlike the Stock Management search box, which does.
  The Goods Receipts list search box has the same `(keyup.enter)`-only behavior — always
  `.press('Enter')` after filling it, not just `.fill()`.
- `p-select`/`p-autocomplete` triggers often can't be opened via their placeholder text
  once a value is already selected (the placeholder isn't rendered once non-empty) — open
  them via their *currently displayed* label instead (e.g. a form that defaults its
  resolution-method dropdown to "Repair Resolution" needs `getByText('Repair Resolution')`
  as the trigger, not the placeholder text, which never appears).
- A `p-autoComplete`/`app-lazy-autocomplete` option's accessible name (`role="option"`) is
  driven by the component's own `field`/`optionLabel` input, which is frequently *not* the
  full text the custom item template renders (e.g. a customer option's accessible name is
  just the first name "Walk-in", not "Walk-in Customer"; a warranty option's accessible
  name is the warranty number, not the part name shown in the dropdown row). Prefer
  matching on the option's actual rendered text (`getByText`) over its accessible name
  when the two can diverge, or pass the correct override text into `pickAutocompleteOption`.
- Buttons whose PrimeNG icon glyph precedes a text label (e.g. a `pi-check` icon next to
  "Record") can expose that icon in the computed accessible name, breaking `exact: true`
  matches unpredictably. Prefer matching the inner label `<span>`'s exact text via
  `getByText(..., { exact: true })` over `getByRole('button', { name, exact: true })` for
  these.
- `p-inputNumber` fields differ in whether they need `.pressSequentially()` (keystroke
  parsing) or plain `.fill()` — it isn't purely a function of `mode="currency"` vs plain
  numeric. When `.pressSequentially()` visibly leaves a currency field unchanged, try
  `.fill()` instead (confirmed via `inputValue()` in a scratch test) rather than assuming
  the click/typing approach always applies.
- A `p-datepicker` field that already has a default value (e.g. `returnDate: [new Date()]`
  in the form's `fb.group` init) should usually be left untouched rather than typed into —
  typing appends to the existing text instead of replacing it, producing an invalid
  doubled-up date string that fails silently (the "required" validation error is the only
  visible symptom, easy to misread as "nothing was typed").
- Toast/success-message assertions immediately after a create action are prone to a race
  where the toast has already faded by the time the assertion runs on a slower CI-like
  machine; prefer asserting on the resulting navigation/URL and the created record's
  presence in the destination list over the transient toast text.
- Found via this suite: the Goods Receipts list's server-side search
  (`GET /api/v1/purchaseorder/grn/list?searchTerm=`) advertised "PO number" search in its
  placeholder but only matched `PurchaseOrderId.ToString()` (a GUID) — a human-typed PO
  number like "PO019" could never match. Fixed in `PurchaseOrderController.GetGRNList` to
  search `PurchaseOrder.PONumber` and `Warehouse.Name` as well.
- Found via this suite: `WarehousesController.Create` trusted the client-submitted
  warehouse code directly instead of atomically reserving one server-side — the frontend
  only ever *previews* a code via the non-consuming `PeekAsync` endpoint (by design, per
  `CodeGenerateController`'s own doc comment), so two warehouses created back-to-back
  collide on the same previewed code and the second gets a 409. Fixed to fall back to
  `ICodeGenerateService.GenerateAsync` on a collision instead of rejecting outright — the
  same architecture class of bug previously fixed for customers/suppliers.
- Found via this suite: `CreateCategory.Description` and `CreateUnitRequest.Description`
  were non-nullable `string` properties documented as "(optional)", but ASP.NET Core's
  implicit-required-for-non-nullable-reference-types model validation rejects an explicit
  JSON `null` for them — any category/unit created without a description (the normal case,
  since the UI's own placeholder says "Optional") always failed with 400. Fixed both to
  `string?`.
- A dialog's `(onShow)` handler that calls `form.reset(...)` races a fill that happens
  immediately after the trigger click — the dialog is visually present before the reset
  fires, so typed values can get silently wiped a few hundred ms later. Add a short
  `waitForTimeout` (700–800ms observed reliable) after opening this class of dialog before
  filling anything.
- A PrimeNG calendar's keystroke-parsed text can visibly update the input and even
  highlight the right day in its own popup grid, while leaving the *reactive form
  control* still null/invalid — clicking the day cell directly (the picker's own supported
  selection gesture) is the reliable way to actually clear a "required" error, not typing.
- A field that already fired its "required" validation once can leave a stale error
  visible even after a valid value is typed in a way the form control never picks up
  (e.g. `.fill()` on a datepicker's raw `<input>`) — don't assume a filled-looking field
  is a validated field; check the actual submit outcome (or the control's real value) when
  a submit mysteriously stays blocked.
- Some list/detail pages render two layouts in the DOM at once (a `<table>` and a mobile
  card grid, CSS-toggled) — a bare `tr`/`hasText` locator can resolve to a row in the
  hidden layout, whose action button clicks silently do nothing. Scope row locators to
  `table tbody tr` explicitly rather than a bare element selector.
- One popup action menu (Sales Orders list's row "⋮" menu) proved genuinely flaky under
  the full suite's background load — it would sometimes fail to open on the first click,
  or its DOM would be torn down between becoming visible and being clicked (a "detached
  from the DOM, retrying" Playwright error). A short retry loop (reopen the trigger, retry
  the click with a tight per-attempt timeout, `Escape` between attempts) resolved it;
  a single click-then-assert did not.
- A datepicker inside a dialog can auto-open its calendar overlay on the dialog's own
  `onShow` (no interaction from the test), covering a nearby submit button. `Escape` may
  close the *whole dialog* rather than just the calendar overlay in this case — a plain
  click can also silently reopen the calendar. `.click({ force: true })` on the real
  target button (bypassing the actionability/"receives events" check) proved the most
  reliable way through, given the button itself is the genuine, un-obscured element.
- Business-rule collisions across repeated runs are a real flakiness source once a suite
  is run many times against the same dev DB: a fixed holiday date, a fixed leave-request
  date range, or reusing "any employee/record matching a loose text filter" will
  eventually collide with state an earlier run left behind (a holiday already on that
  date, a pending leave that overlaps, etc.) and get a legitimate 400 from a real business
  rule — not a bug. Prefer a value that varies per run (a computed day-of-month, a
  freshly created dedicated record) over a fixed one anywhere the backend enforces
  uniqueness or non-overlap.
