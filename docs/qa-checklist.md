# QA Checklist — SujanMotors (AutoPartShop)

Current reference for verifying the system works end-to-end. The **API tester**
column is driven by the `qa` agent (`.opencode/agent/qa.md`) or a human with
`Invoke-RestMethod`/`curl` against the running API. The **UI tester** column is
a human or agent driving the Angular app at `http://localhost:4200`.

Legend: leave empty = not run · `PASS` / `FAIL` / `PARTIAL` / `SKIP` (with a
one-line note). A row is only `PASS` when both API and UI agree with the
"Expected" cell.

## Preflight (before any run)

- [ ] SQL Server up: `docker compose -f deployment/docker-compose.yml up -d`
- [ ] API up: `http://localhost:5000/docs` loads; `swagger/v1/swagger.json` parses
      (the Docker container publishes `5000->8080`; `:5001` is stale)
- [ ] **API image is current**: `docker image inspect smapi:local --format '{{.Created}}'`
      is newer than the branch under test — rebuild first, or you are QA'ing an old build (A1)
- [ ] Backend tests green: `dotnet test src/AutoPartShop.Api.Tests`
- [ ] Web app up: `http://localhost:4200` loads, login screen renders
- [ ] Mobile app builds/analyzes: `cd mobile && flutter analyze`
- [ ] Dev admin login works: `admin` / `Admin@1990` (see `DatabaseSeeder`)

## Accounts

| Role | Login | Purpose |
| --- | --- | --- |
| Admin | admin / Admin@1990 | Full access, role management |
| (create) Salesman | — | 403 checks: sell without admin rights |
| (create) Manager | — | 403 checks: report/module access |

---

## Latest run

Run 2026-08-17 — API base **`http://localhost:5000`** (not `:5001`; that address
in this doc and in `.opencode/agent/qa.md` was stale).

Two passes:

- **Pass A (stale image)** — modules 1, 2, 9. Findings prefixed `A`. Ran against
  the 2026-07-17 container image; see A1. Re-verify against current code.
- **Pass B (rebuilt image)** — the image was rebuilt from the working tree on
  2026-08-17 (`InboxNotifications` 404 → 401 confirms new code; a month of EF
  migrations applied). Module 3 findings prefixed `F`; module 5 in progress.

**Coverage warning: still incomplete.** Modules **4 (procurement), 6 (HR),
7 (warranty), 8 (finance/reports)** have not been run — the first attempt was
killed by a session limit. Their rows remain empty and untrusted.

**Note on Swagger:** `/docs` and `/swagger/v1/swagger.json` now 404 in the
container. This is deliberate — `Program.cs:323` gates Swagger behind
`IsDevelopment()` ("keep it out of production to avoid information disclosure").
The July image predated that hardening. For route discovery, generate an
inventory from the `[Route]`/`[Http*]` attributes under
`src/AutoPartShop.Api/Controllers/` instead.

### Preflight

| Check | Result | Evidence |
| --- | --- | --- |
| SQL Server / API / Web containers | PASS | `autopartshop.db`, `.api`, `.web` all up |
| Swagger | PASS | `/swagger/v1/swagger.json` parses, ~1.1 MB |
| Admin login | PASS | 200 + JWT, `roles:["Admin"]` |
| Backend tests | PASS (hollow) | `dotnet test` → **2 tests total** in the whole suite (A2) |

### Module results

| Module | Verdict |
| --- | --- |
| 1 Auth (pass A) | 6 PASS · 1 PARTIAL (1.7) · 1 FAIL (1.8) · 1.9 passes with a caveat (A17) |
| 2 Admin (pass A) | 4 PASS · 1 PARTIAL (2.5) · 1 FAIL (2.3) · 2.7 UI-only |
| 9 Audit & platform (pass A) | 3 PASS · 2 PARTIAL (9.1, 9.3) |
| 3 Inventory (pass B) | 9 PASS · 2 PARTIAL · **4 FAIL** (3.3, 3.6, 3.10, 3.15) · 3 S1 bugs |
| 3 `Stock/check` regression (pass B) | **PASS** — 24 probes, no 500s; the uncommitted fix is verified |
| 5 Sales (pass B) | 5 PASS · 6 PARTIAL · **4 FAIL** (5.2, 5.3, 5.8, 5.9) · 5 S1 bugs |
| 4 Procurement (pass B) | 5 PASS · 5 PARTIAL · **1 FAIL** (4.8) · 1 S1 bug — healthiest module |
| 6 HR (pass B) | 6 PASS · 1 PARTIAL · 0 FAIL — strongest module |
| 7 Warranty (pass B) | 2 PASS · 1 PARTIAL · 0 FAIL |
| 8 Finance/Reports (pass B) | 0 PASS · 2 PARTIAL · **2 FAIL** (8.2, 8.3) · 3 S1 bugs |

**All 9 modules now executed.** 53 findings total (F1–F53 from pass B, A1–A17
from pass A). 12 are S1.

**Highest-priority defects so far** (all confirmed in source): F14 sales-order
confirm 500s · F15 converted quotes un-confirmable · F16 cash-return over-refund ·
F17 credit note consumed without crediting · F18 debit note doesn't move the
balance · F1 stock quantity/value divergence · F2 unit edits 500 · F3 lot
movements don't update levels.

Cross-cutting verified: invalid JSON → 400 (not 500); wrong content-type → 415;
empty body → 400; unknown route → 404 authed and anon; pagination correct and
clamped (`pageSize=0`→50, `9999`→200, `page=-1`→1, overflow page → empty;
`totalPages = ceil(total/size)`); empty filters return empty collections.

### Findings

Severity: **S1** data loss / wrong money / broken core flow · **S2** missing
validation or edge case · **S3** cosmetic / DX / docs.

### Triage 2026-08-18 — verified against current source

Before fixing anything, every S1/S2 finding was re-checked against the working
tree. Two different things make a finding "already handled", and they must not be
confused:

- **STALE** — the defect was already absent from *committed* code at the QA run.
  Pass A ran on a month-stale image (A1), so several of its rows describe code
  that no longer existed. Nothing to do.
- **FIXED IN TREE** — the defect was real at branch baseline `5727338`, and was
  fixed by uncommitted work in the working tree (some of it landed while this
  triage was being written). Real bug, real fix — just already done.

Verify with `git show 5727338:<file>`, not `git log <file>`: the log shows the
last commit that touched a file, which says nothing about whether the current
content is committed.

| Finding | Verdict | Evidence in current source |
| --- | --- | --- |
| A4 | **STALE** | `AuthController.cs:264` binds to the authenticated principal; the body `username` is already ignored by design |
| A5 | **STALE** | `AuthController.cs:239` logout revokes the refresh token; `Program.cs:145` `OnTokenValidated` re-checks `IsActive` (60 s cache), so a deactivated user loses access |
| F4 | **FIXED IN TREE** | `ProductsController.cs:376-383` now returns 409 on a duplicate `partNumber`. Absent at baseline `5727338` — a real bug, fixed uncommitted |
| F6 | **FIXED IN TREE** | `BrandsController.cs:109` now returns 409 on a duplicate brand name. Absent at baseline |
| F22 | **FIXED IN TREE** | `SalesOrderController.cs:2323` now throws on `UnitPrice < 0` before pricing resolution. Absent at baseline |
| F43 | **FIXED IN TREE** | `Payslip.Recalculate()` now throws when deductions exceed gross and the controller maps it to 400. Absent at baseline |
| F19 | **PARTLY STALE** | Stock *is* restored (`SalesOrderController.cs:1659`) — but `stockDeductedStatuses` omits `DELIVERED`/`COMPLETED`, which is the exact scenario tested. Narrow gap, still real. |
| F20 | **REAL, misdiagnosed** | Not the F14 `ExecuteUpdateAsync` cause. `TillSessionRepository.UpdateAsync` calls `.Update()` on an already-tracked graph, so the new `TillCashDrop` (client-generated `Id`) is marked *Modified* instead of *Added* → UPDATE hits 0 rows → `DbUpdateConcurrencyException` |
| F17 | **REAL, misdiagnosed** | Root cause is `CustomerCreditNoteController.cs:319` writing a `CreditNote` id into `CustomerPayment.SourceAdvancePaymentId`, a **self-FK to `CustomerPayments.Id`** (`CustomerPaymentConfiguration.cs:81-84`) — the identical bug F29 describes on the supplier side |
| F21/F37 | **REAL, cheap** | `CodeGenerateService.GenerateAsync` already enlists in the ambient transaction (`CodeGenerateService.cs:125`). Numbers leak only where generation happens *before* `BeginTransactionAsync` — e.g. `SalesOrderController.cs:2286-2287` vs the transaction opened at 2288 |

Confirmed still real and in scope: A3, A6, A7, A9, A10, F1, F2, F3, F5, F7, F8,
F12, F14, F15, F16, F17, F18, F23, F24, F25, F27, F29–F36, F40–F42, F44–F46, F51.

**A1 · S1 — the API under test is a month-stale container image.** `docker image
inspect smapi:local` → created 2026-07-17; the working tree has commits through
August. `InboxNotificationsController.cs` exists in source and is DI-wired, yet
`GET /api/v1/InboxNotifications` → 404 on `:5000`. The permission catalog and the
Manager/User roles are unseeded although `DatabaseSeeder` seeds them
unconditionally. Rebuild the image before the next sweep — until then no result
in this section describes the current branch.

**A2 · S1 — the backend has effectively no automated test coverage.** `dotnet
test src/AutoPartShop.Api.Tests` runs **2 tests**. Every guarantee in this
document rests on manual QA; nothing catches a regression between sweeps.

**A3 · S2 — duplicate exchange rate accepted, FX then silently picks one**
(row 2.3). POST `/api/v1/ExchangeRates` BDT→QAT @2.5 eff 2026-08-17, then the
same pair and date @3.0 → both 201, both `isActive:true`. `/convert` 100 BDT→QAT
returned **300.00 @3.0** with no warning. Expected: 400 duplicate. Money-affecting.

**A4 · S2 — `change-password` ignores the body `username` and always changes the
caller** (row 1.7). As user A, POST `/Auth/change-password` with B's username and
B's current password → 200 "Password changed successfully"; B is unchanged,
**A's password was changed**. A required contract field is a no-op that
misreports success.

**A5 · S2 — deactivated users keep API access; there is no revocation path**
(row 1.8). `PATCH /Admin/users/{id}/toggle-status` → `isActive:false`; login now
401, but the already-issued JWT still returns 200 on `/api/v1/Currencies` until
`exp` (~1 h). No logout endpoint exists in the API at all, and changing a
password does not invalidate issued tokens.

**A6 · S2 — soft-deletes are never audited as DELETE** (row 9.1). `DELETE
/api/v1/Currencies/{id}` → 204, then `/AuditLog/list?EntityId=…&Action=DELETE` →
`totalCount:0`. The deletion appears only as `UPDATE Isdeleted 'False'→'True'`,
so the `Action=DELETE` filter is dead for every soft-deleted entity and "who
deleted this?" cannot be answered from the UI.

**A7 · S2 — user creation accepts a non-existent role silently** (row 2.5). POST
`/Admin/users` with `"roles":["NoSuchRole"]` → 200 "User created successfully";
`GET /users/{id}/roles` → `[]`. The admin believes the user is provisioned; the
user has zero access.

**A8 · S2 — permission catalog empty in the running environment** (row 2.6).
`GET /Admin/permissions` → `[]`; `GET /Admin/roles` → only `Admin`, against the
28 permissions + Manager/User roles that `DatabaseSeeder.PermissionCatalog`
defines. Enforcement itself works (a hand-created `audit.view` granted → 200,
revoked → 403 after the ~60 s role cache). Downstream of A1, but it also means a
deploy from this image ships with no usable non-admin role.

**A9 · S2 — `/api/v1/files` owner validation is asymmetric and uploads are
public** (row 9.4). Upload with `ownerType` and no `ownerId` → 200, stored with
`ownerId:null`; `GET /files?ownerType=…` → 400 "ownerType and ownerId are
required" — the file is orphaned and unlistable. Responses show `isPublic:true`
with no way to set it, and `GET /files/{id}/content` returns bytes **with no
Authorization header**.

**A10 · S2 — three different error-body shapes across the API.** Duplicate
currency → 409 `text/plain` bare string; FX failure → 400 `{"error":"…"}`;
validation → ProblemDetails with `message`. The frontend convention
(`err.error.message`) resolves to `undefined` for the first two, so
duplicate-currency and FX errors reach the user with no message.

**A11 · S3 — the base currency cannot be converted out of** (row 2.2). `/convert`
100 BDT→INR → 400 "No exchange rate found", though an INR→BDT rate of 1.3 exists;
reciprocals are not derived. Cross-rate INR→NPR fails likewise (the error names
the BDT→NPR leg, so triangulation via base exists but inversion does not). Seed
data holds only X→BDT, so out-of-base conversion is impossible until an admin
enters every reverse pair.

**A12 · S3 — conversion input/rounding nits** (row 2.2). Unknown currency `XXX`
reported as "No exchange rate found for XXX to BDT" rather than "unknown
currency"; `amount:0` rejected as "must be greater than zero" though converting
zero is legitimate; results always round to 2 dp regardless of the target
currency's `decimalPlaces`.

**A13 · S3 — login response allows username enumeration** (rows 1.2/1.3). Wrong
password on an existing user → "Invalid credentials"; unknown user → "Invalid
credentials or account is inactive". The two messages should be identical.

**A14 · S3 — audit log writes unchanged-value rows and never captures IP/UA**
(row 9.1). One currency create + 1 update + 2 set-base + 1 delete produced **61
rows**; a single soft-delete wrote 12 rows of which 11 have `oldValue ==
newValue` (e.g. `CreatedBy 'admin'→'admin'`). `ipAddress` and `userAgent` are in
the DTO but null on every row.

**A15 · S3 — company profile unconfigured, so documents render blank headers**
(row 2.4). `/ApplicationSettings/categories` → `["BACKUP","CURRENCY"]`;
`/public/shop` → `name:""`, `address:""`, `phone:""`, `taxNo:""`. The controller
reads `BUSINESS` and `BRANDING`, which have no rows. Left unpopulated
deliberately (shared data).

**A16 · S3 — API contract nits.** `GET /Admin/users` has no `page`/`pageSize` and
returns the full array; `POST /Admin/users|/roles|/permissions` return 200 not
201; `GET /Currencies` returns `createdAt`/`updatedAt` as `0001-01-01T00:00:00`;
no delete endpoint for permissions.

**A17 · S3 — account lockout returns 401, not 429/403** (row 1.9). After 10 bad
logins: 401 "Account is locked. Please try again later." Lockout works and the
message is clear; only the status differs from this checklist's expectation —
decide which is authoritative and align one of them.

### Inventory findings (run 2026-08-17b, rebuilt image)

Module 3 was re-run after the rebuild, against current code on an empty DB.
`Stock/check` regression: **PASS** — 24 probes (qty -1/0/missing/decimal/"abc"/
int-overflow, PartId missing/empty-Guid/non-existent, body null/`{}`, bulk empty/
null/one-bad-entry/500-entry batch). All-or-nothing batch rejection confirmed, no
500s. The uncommitted `StockController.cs` fix is verified good.

**F1 · S1 — display-unit stock column diverges from base units: wrong valuation,
phantom sellable stock, no recovery path** (row 3.15). `StockLevel` keeps
`QuantityOnHand` (display units) and `QuantityOnHandInBaseUnit` separately, but
`StockLevel.RemoveStock` (`src/AutoPartShop.Domain/Entities/StockLevel.cs:77`)
guards on the **display** value. Receive 3 via a ×12 unit → display 3, base 36.
Result observed: a part sitting at display **1** / base **40** — `stock-summary`
reports `quantityOnHand: 1` alongside `stockValue: 4000.00`, `low-stock` raises a
false shortfall, `POST /api/Stock/check` returns `stockAvailable: 39,
available: true` so the POS will happily sell it, and `adjust −40` is refused with
"Insufficient stock available". The level then sticks at display 0 / base 39 with
no endpoint able to clear it — recovery needs a direct DB edit. **Verified in
source.**

**F2 · S1 — every unit update / activate / deactivate returns 500** (row 3.3).
`src/AutoPartShop.Infrastructure/Repositories/UnitRepository.cs:28` does
`Remove(existing); Add(entity)` where `existing` and `entity` are the *same
tracked instance* (`GetByIdAsync` tracks), so EF resolves the state to `Added` and
emits an INSERT → `SqlException 2627: Violation of PRIMARY KEY constraint
'PK_Units'`. Units can be created and deleted but never edited or deactivated.
The file still carries a `TODO: Replace with Entity Framework Core implementation`
header. **Verified in source.**

**F3 · S1 — lot movements never update the stock level, desynchronising lots from
levels** (row 3.10). `POST /api/StockLotMovement {movementType:"SALE",
quantity:5}` on a part at 20/20 → lot 20→15, level stays **20**, 201 returned.
`StockLotMovementController.Create` calls `lot.RemoveStock(...)` and contains no
`StockLevel` reference at all. Reports and `Stock/check` read the level while FIFO
sales and transfers read the lots — a 5-unit oversell, and later transfers fail
with "lot records are short by N. Run a stock reconciliation". **Verified in
source.**

**F4 · S2 — duplicate part number accepted; `by-code` then silently picks one**
(row 3.6). Two POSTs with the same `partNumber` → both 201 (SKU is auto-generated
so only the manufacturer number collides). `GET /api/v1/products/by-code` returns
whichever matched first with no ambiguity signal — a barcode scan at the POS can
select either of two products with different prices.

**F5 · S2 — unit conversion rounds to nearest even, creating and destroying
stock** (row 3.3). Factor 2.5: `+1` → 2 base (0.5 lost), `+3` → 8 base (0.5
created). `StockController.AdjustStock` uses `(int)Math.Round(...)` with .NET's
default banker's rounding, so three `+1` receipts (6) ≠ one `+3` receipt (8).

**F6 · S2 — duplicate brand name accepted** (row 3.1). Second POST with an
existing name → 201. Warehouses reject a duplicate code with 409; brands don't.

**F7 · S2 — `Stock/check` can't distinguish "unknown part" from "no stock".** A
random GUID and a soft-deleted part both return 200
`{available:false, stockAvailable:0, message:"Insufficient stock..."}` — identical
to a real out-of-stock part. A POS typo reads as "out of stock", not "unknown
product".

**F8 · S2 — product import silently creates master data for unknown brand/category
names** (row 3.8). Rows with `Brand:"NoSuchBrandQA"` validate with `errors: []`
and `warnings: null`; commit returns `createdBrandsCount: 1,
createdCategoriesCount: 1`. Contradicts the FK-by-name design
([[project-parts-import]]: names must already exist). A spreadsheet typo
permanently pollutes the master lists with no confirmation.

**F9 · S3 — two StockLotMovement read endpoints 500 on concurrent DbContext use**
(row 3.10). `GET /api/StockLotMovement/lot/{id}` and `/type/{t}` →
`InvalidOperationException: A second operation was started on this context
instance`; `MapResponse` is awaited inside a `Select`, firing parallel queries on
the scoped DbContext. The other five read endpoints work.

**F10 · S3 — deletion guards trip on zero-quantity stock rows.** Once every level
is at 0, `DELETE /api/v1/products/{id}` still returns 409 "Cannot delete a product
that has stock records". The guard tests for the existence of a `StockLevel` row,
not a non-zero quantity, so any part that ever held stock is permanently
undeletable. Same for warehouses.

**F11 · S3 — `GET /api/Stock/levels/part/{id}` omits variant/unit names.**
`variantId` populated but `variantName`/`variantSku` null, while
`POST /api/Stock/levels/list` returns them correctly — a missing `.Include` on the
repository path. Same for `warehouseName`/`warehouseCode` on
`GET /api/v1/warehouse-locations/warehouse/{id}`.

**F12 · S3 — `PUT /api/v1/products/{id}` blanks omitted fields.** A PUT without
`partNumber` silently resets it to `""`. Any client patching a subset of fields
loses the rest.

**F13 · S3 — circular-reference check parameter doesn't bind.** `GET
/api/v1/categories/{parentId}/check-circular-reference?parentCategoryId={childId}`
always returns `wouldCreateCircularReference: false` — the query-string name
doesn't match the action binding, so it always evaluates "move to root".
Relatedly, re-parenting a category to itself or its own child returns 200 and
silently ignores the change.

### Sales findings (run 2026-08-17b, rebuilt image)

**Money consistency verdict: NO.** Quick-sale-only flows are exact to the paisa
(SO, invoice, print-data, payments, ledger, VAT 15% → 115.00, and a 2280 till
close reconstructed precisely). Every flow touching a **return, credit note or
debit note** breaks. Oversell protection held under 3 concurrent quick sales on a
1-unit part: exactly one 201, rest 400, stock 0, never negative.

**F14 · S1 — sales order confirm always 500s; the whole credit-sale flow is
dead** (row 5.3). `PATCH /api/SalesOrder/{id}/confirm` → 500 on every order
tested. `order.Confirm()` mutates the tracked entity, then `ExecuteUpdateAsync`
(`SalesOrderController.cs:793`) writes the same row and bumps its `RowVersion`
(`SalesOrderConfiguration.cs:12` `IsRowVersion()`); the following
`SaveChangesAsync` then re-updates it with a stale token → 0 rows →
`DbUpdateConcurrencyException` at line 836. The transaction rolls back cleanly
(no stock or status damage), but challan, delivery and all non-POS invoicing are
unreachable. Same root cause breaks till cash-drops (F20). **Verified in source.**

**F15 · S1 — quotation → SO conversion drops the warehouse, producing a
permanently un-confirmable order** (row 5.2). `POST /api/quotations/{id}/convert`
→ 200, but confirming the result gives 400 "Warehouse is required for stock
deduction". `QuotationController.ConvertToSalesOrder(Guid id, CancellationToken)`
takes no warehouse argument and never passes one to `SalesOrder.Create`; the word
"warehouse" does not appear anywhere in that controller, and `Quotation` carries
no warehouse either. Every converted quote is a dead end with no recovery path.
**Verified in source.**

**F16 · S1 — cash sales return over-refunds by the order discount and leaves a
phantom receivable** (row 5.8). Quick sale 2 × 100 with a 20 order discount →
grand 180, fully paid. Return 1 unit as `CASH_REFUND` → `refundAmount: 100.00`
where the customer effectively paid **90**. The invoice is left at grandTotal 180
with amountPaid 180→80 and status PAID→ISSUED, so the system shows the customer
owing 100 on goods he partly returned while the ledger shows him at 0. The
`STORE_CREDIT` path on the same endpoint is correct — only `CASH_REFUND` is wrong.

**F17 · S1 — applying a customer credit note consumes it without crediting the
invoice or the ledger** (row 5.9). `POST /api/customer-credit-notes/apply` (100
against INV002) → 200; the note goes `usedAmount 100 / FULLY_USED` and the sales
order's `amountPaid` moves 80→180, but the **invoice stays at amountPaid 80 /
PARTIALLY_PAID**, no ledger entry is written, the balance does not move, and no
`CustomerPayment` row is persisted. The customer's 100 of store credit vanishes.

**F18 · S1 — customer debit note has no effect on the balance** (row 5.9). `POST
/api/customer-debit-notes` amount 250.55 → 201, DN ISSUED — and
`/api/customer-ledger/{id}/balance` is byte-identical before and after
(`currentBalance −100.00`). No ledger entry, invisible in the entries list and
account summary. Row 5.9's "balance direction correct" has no direction at all.

**F19 · S2 — cancelling an invoice returns no stock and leaves the order
inconsistent** (row 5.7). After refunding the payment, `PATCH
/api/SalesOrder/invoices/{id}/cancel` → 200, invoice CANCELLED, but stock stays
decremented and the SO remains **DELIVERED with outstandingAmount 100**.
`CancelInvoice` (`SalesOrderController.cs:1650`) cancels the invoice and its
payments only — no stock or order-status handling. Net: a delivered order with a
cancelled invoice, one unit of inventory lost, and 100 the SO channel still counts
as receivable while the ledger has dropped it.

**F20 · S2 — till cash-drops always 409, and a live session reports zero cash**
(row 5.11). `POST /api/till-sessions/{id}/cash-drops` → 409
`CONCURRENCY_CONFLICT` on a session nothing else had touched
(`DbUpdateConcurrencyException` again — see F14). Separately, while OPEN the
session reports `cashSalesTotal 0.00 / expectedAmount 0.00` after ~2280 of cash
sales, so a cashier cannot see expected drawer contents mid-shift; the numbers
only appear at close.

**F21 · S2 — invoice and SO number sequences leak numbers on rejected sales.**
Three rejected quick-sale attempts (400) burned INV004/005/006, and later
INV011/012/013 — confirmed absent from `GET /api/SalesOrder/invoices`. The code
number is generated **before** payment/total validation runs. Gapless numbering is
normally a fiscal-document requirement.

**F22 · S2 — negative unit price silently coerced to the catalog price** (row
5.5). Quick sale with `unitPrice: -100` → **201**, order created at 100.00 and
stock decremented. `SalesOrderController.cs:2287`:
`var itemUnitPrice = item.UnitPrice > 0 ? item.UnitPrice : part.SellingPrice;`.
Money lands right (server-authoritative pricing) but malformed input is accepted.
Positive: total-tampering probes (claiming grandTotal 1 on a 100 basket, paying
500 on a 100 bill) were both correctly rejected. **Verified in source.**

**F23 · S2 — sales order payload reports `discount: 0` for fixed-amount
discounts.** `GET /api/SalesOrder/{id}` on a quick sale with a 20 fixed discount
returns `subTotal 200, discount 0.0, grandTotal 180.0` — a header that doesn't
foot. `SalesOrderController.cs:1970` maps `Discount = order.DiscountPercentage`
while quick sale sets `DiscountAmount`. Sales-by-product likewise reports
`discountAmount: 0.00`, so POS discounts are invisible in both the order screen
and the report.

**F24 · S2 — sales reports don't reconcile with invoices or the ledger** (row
5.14). Summary counts 8 orders / 2691.65 **including three orders that were never
confirmed and never shipped** (1161.10) while excluding one whose stock did ship.
by-customer reports `outstanding 1511.65` against a ledger balance of 150.55 — the
gap is exactly the uninvoiced orders plus 200 of returns the reports never deduct.
by-product still counts the 2 returned units in `quantitySold`.

**F25 · S2 — `runningBalance` is 0 on every customer ledger entry** (row 5.10).
13 entries with correct `debitAmount`/`creditAmount` but `runningBalance: 0`
throughout, so the statement cannot be read as a running account.

**F26 · S3 — line total and order subtotal disagree by 0.01.** 3 × 33.33 with a
10% line discount → line reports `lineTotal 90.00` while the order and invoice
say **89.99**. The per-unit discount is rounded to 2dp for the line but used
unrounded for the header, so a printed invoice will not foot.

**F27 · S3 — 100% discount is rejected.** Subtotal 100, discount 100, grand 0 →
400 "Grand total must be greater than 0". A zero-value goodwill or
warranty-replacement sale cannot be rung up.

**F28 · S3 — payload/contract issues.** `customerCode` in `CreateCustomerRequest`
silently ignored; `creditLimit` accepted by PUT but never echoed; `paymentNumber`
null on every row of `/api/customer-payments/customer/{id}`; refunds stored as
`CustomerPayment` rows with **negative amounts**; `BUSINESS_RULE_VIOLATION` bodies
carry `"status": 422` while the HTTP response is 400. Also (outside module 5):
`POST /api/Stock/levels` with `"quantity": 100` creates the level at **0** — the
field is ignored.

### Procurement findings (run 2026-08-17c, rebuilt image)

Procurement is the **healthiest module tested**: the PO lifecycle, cancellation
rules, goods-receipt stock/lot/cost effects, purchase returns and purchase reports
all passed, and the ledger closed to exactly 0.00 across a full
purchase→receive→pay→return cycle. The PO controllers do **not** use the
`ExecuteUpdateAsync`-before-`SaveChangesAsync` pattern that breaks sales (F14).

**F29 · S1 — applying a supplier credit note consumes it, credits nothing, and
500s** (row 4.8). `POST /api/CreditNote/apply` (120 of a 200 note against PO009) →
**500**. The note is still debited (`used 0→120, PARTIALLY_USED`) while the PO's
outstanding stays 500.00, `amountPaid` stays 0.00, and the ledger is unchanged;
repeating drove the note to FULLY_USED with zero value delivered. Cause:
`CreditNoteController.ApplyCredit:188` calls
`SupplierPayment.CreateFromAdvance(sourceAdvancePaymentId: creditNote.Id, …)` —
a **CreditNote id written into a self-FK that points at `SupplierPayments.Id`** →
`FK_SupplierPayments_SupplierPayments_SourceAdvancePaymentId` violation. The
insert fails *after* the consumption was already committed, and the PO credit at
line 203 never runs. There is **no `BeginTransaction` anywhere in that
controller**. This is the supplier-side twin of F17, but worse — no transaction
wrapping. **Verified in source.**
*Also check while fixing*: `issue-credit-note` already books the credit as a
ledger REFUND at issue time, so a working `apply` would credit the same money
twice.

**F30 · S2 — `PUT /api/suppliers/{id}` wipes the credit limit and resets payment
terms** (row 4.1). Edit a supplier with `paymentTerms:"NET15",
creditLimit:150000` → stored as **NET30 / 0**.
`SupplierRepository.UpdateAsync:25` re-invokes `existing.Update(...)` passing only
**10 of the 12** parameters (Name…IsActive), so `paymentTerms` falls to its
`null`→"NET30" default and `creditLimit` to `0`. Every supplier edit from any
surface silently zeroes the credit limit used for credit checks. **Verified in
source.**

**F31 · S2 — `PUT /api/payment-provider/{id}` silently drops providerName and
providerType** (row 4.10). PUT with a new name → 200, fees update, name unchanged.
`PaymentProviderController.Update:139-148` calls SetBankDetails,
SetMobileBankingDetails, SetTransactionFees, SetCurrencies, SetWebhookUrl and
UpdateNotes but never applies name or type. A provider can never be renamed.

**F32 · S2 — overpaying a PO returns 500 instead of 400** (row 4.6). Paying 5000
against a 922.74 outstanding → **500** "An error occurred". Data is safe
(transaction rolls back). `InvalidOperationException: Payment exceeds outstanding
amount` is thrown from `PurchaseOrder.RecordPayment` *inside* the
`strategy.ExecuteAsync` block, so the controller's `catch
(InvalidOperationException)` never sees it. Same shape as F14/F20 in spirit: a
correct business rule surfacing as a server error.

**F33 · S2 — deleted daily expenses stay in the cash book** (row 4.9). Create
1234.56 → update to 1000.00 → DELETE (204); the expense is gone from its own list
and 404s by id, but the cash book still lists it and `totalCashOut` still includes
it. `CashBookController` lines 61 and 86 query `_db.DailyExpenses` with no
`!e.Isdeleted` predicate and there is no global query filter, so soft-deleted
expenses inflate both the opening balance and in-range cash-out.

**F34 · S2 — credit-note settlements are reported as cash out** (row 4.9). Two
`issue-credit-note` calls (200 + 300) appear in `/api/cash-book/daily` as
`SUPPLIER_PAYMENT` rows with `paymentMethod: "CREDIT_NOTE"` in `cashOut`, adding
500.00 to `totalCashOut`. No cash moved — the supplier settled with credit. The
supplier ledger correctly excludes them, so **cash book and ledger disagree by
500.00**.

**F35 · S2 — damaged / quarantine stock is invisible in every stock API
response** (row 4.4). A GRN with 1 damaged + 1 wrong creates LOT003 (DAMAGED) and
LOT004 (QUARANTINE) with correct 125.50 cost, and `StockLevel.AddDamagedStock`
persists the buckets — but `GET /api/Stock/levels/part/{id}` returns
`damagedQuantity: 0, quarantineQuantity: 0`. No mapping code anywhere assigns
`StockLevelResponse.DamagedQuantity`. Held stock cannot be seen or reconciled
through the API.

**F36 · S2 — deleting an in-use payment provider 500s instead of 409** (row
4.10). FK violation on `FK_SupplierPayments_PaymentProviders_PaymentProviderId`;
no in-use check. `DELETE /api/suppliers/{id}` gets this right (409 "Cannot delete
supplier with existing payment history") — the provider path just lacks the guard.

**F37 · S3 — PO / GRN / credit-note numbers are consumed by rejected creates.**
Three rejected PO creates → the next successful PO is **PO005**, with
PO002/003/004 confirmed absent. Same for GRN (first real one is **GRN003**) and
credit notes (first is **CN003**). Numbers are allocated before validation. Same
defect class as F21 on the sales side — worth one shared fix.

**F38 · S3 — `POST /api/suppliers` silently ignores the `code` in the request.**
Sent `code:"QA817C1"`, got `SUP001`. Codes are auto-generated by
`ICodeGenerateService`, but `CreateSupplierRequest.Code` still exists and is
accepted without comment.

**F39 · S3 — `GET /api/PurchaseReturn/available-lots/{partId}` reports
`isFromSameSupplier: false` for the correct supplier.** Lots carry a `supplierId`
equal to the PO's supplier, yet the flag is false on every row — the returns UI
would wrongly warn on legitimate lots.

### HR / warranty / finance findings (run 2026-08-17d, rebuilt image)

HR is the strongest module: employee CRUD, shifts, holidays, leave transitions and
the payroll dry run all passed — the DRAFT run matched a hand calculation to the
cent (gross 32000 − tax 500 − advance 5000 = 26500; absence 20000/31 = 645.16).
`PATCH /Payroll/{id}/pay` was **never called**; the run was deleted afterwards.

**F40 · S1 — cash book counts soft-deleted expenses and payments** (row 8.2).
Two salary advances (104999 total) were deleted (204); `daily-expense/by-date-range`,
`Dashboard/summary`, `profit-loss` and the expenses report all correctly return 0,
but `GET /api/v1/cash-book/daily` still returns both rows with
`totalCashOut 104999`. `CashBookController` has no `!Isdeleted` predicate on
`DailyExpenses`, `CustomerPayments` or `SupplierPayments` — in either the in-range
or the opening-balance queries — though it *does* filter `Deposits` (lines 70,
104), so the omission is inconsistent rather than deliberate. **`grep
HasQueryFilter` across Infrastructure returns nothing**, so there is no global
safety net. Same root cause as F33. **Verified in source.**

**F41 · S1 — cash book double-counts advance payments when applied** (row 8.2). A
1000.00 advance deposit and the 250.55 later applied from it both appear as
inflows (`totalCashIn 2735.54`). `FinancialSummaryService:101` deliberately
excludes rows with `SourceAdvancePaymentId != null`; the cash book has no such
condition, so cash book and dashboard disagree by exactly the applied amount
(2735.54 vs 2384.99). `priorCustomerNet` inherits it, so opening balances are
wrong too.

**F42 · S1 — customer ledger double-counts sales-return refunds** (row 8.3).
`GET /api/v1/customer-ledger/{id}/balance` → `currentBalance −598.90`, where
invoiced − net payments = **−398.90** (independently confirmed by the
receivables-aging report and the dashboard: 350.55 due − 749.45 advance credit).
`CustomerLedgerService.cs:46` computes `totalInvoiced - totalPayments -
totalRefunds`, but a processed refund is already stored as a **negative
`CustomerPayment`** (see F28), so refunds are subtracted twice. Off by 200 now and
growing with every refund. **Verified in source.**

**F43 · S2 — payroll adjustment allows deductions above gross → negative net
pay.** `PUT /Payroll/{run}/payslips/{slip}` with `otherDeduction: 999999` → 200,
`netPay −967999`. Negative amounts are already rejected, but nothing caps a
deduction at gross. The `/pay` guard only checks the **run-level** total, so a
single negative payslip masked by positive ones would be paid out.

**F44 · S2 — a warranty claim can be rejected after it was approved** (row 7.2).
APPROVED → `PATCH /reject` → 200, status flips to REJECTED while `approvedBy` and
`approvedDate` remain on the record. `WarrantyClaim.Reject` blocks REJECTED,
IN_PROGRESS, COMPLETED and CLOSED — but not APPROVED. Every other out-of-order
transition is correctly refused.

**F45 · S2 — attendance marking with an unknown or repeated employeeId → 500**
(row 6.2). `POST /api/v1/Attendance/daily` with a non-existent employeeId →
`DbUpdateException` FK violation; the same employeeId twice in one payload →
duplicate-key violation on `IX_AttendanceRecords_EmployeeId_D…`. Both surface as
500. The controller already 400s for future dates and bad times.

**F46 · S2 — an expired warranty stays ACTIVE until someone calls check-expiry**
(row 7.3). A registration expiring 2021-01-01 still reports ACTIVE and appears
under `/active`, while `/expired` returns `[]`. Only `PATCH /{id}/check-expiry`
flips it. Claims are still correctly blocked, so this is a reporting/visibility
defect, not a money one.

**F47 · S3 — future-dated warranty claim accepted.** A claim dated 2027-01-01
(inside the window) was accepted, then approved and closed "today", giving
`approvedDate < claimDate` and `daysOpen: -136`. Only `claimDate > expiry` is
validated; nothing checks against today.

**F48 · S3 — P&L accepts an inverted date range and returns zeros.**
`profit-loss` with from 2026-08-20 / to 2026-08-01 → 200 with all figures 0,
reading as "no business", while `expenses` and `vat` return 400 "fromDate must not
be after toDate" for the identical body.

**F49 · S3 — receivables/payables aging silently ignore the date range.**
Payloads for 1990, 2090, inverted and omitted dates all return the same row. Aging
is legitimately an as-of snapshot, but the endpoints accept and discard the range
with no indication.

**F50 · S3 — employee detail omits `shiftName`.** `GET /Employees/{id}` and
`GET /Employees` return `shiftId` set but `shiftName: null`; `POST
/Employees/list` and the attendance daily sheet resolve it correctly.

**F51 · S3 — salary advance has no approval step and no sanity cap** (row 6.6).
Row 6.6 expects request → approve → deduct, but `POST /SalaryAdvances` pays out
immediately (cash-book expense in the same transaction), and 99999 was accepted
against a 20000 monthly salary. Payroll's installment clamp keeps net ≥ 0, so no
money is lost — but there is no authorisation gate on cash leaving the till.

**F52 · S3 — declared holidays never reach attendance or payroll.** A holiday
created for 2026-08-15 leaves `holidayDays: 0` in the monthly summary and puts no
marker on the daily sheet; `holidayDays` only counts manually-marked HOLIDAY
records. Weekend holidays are accepted without comment.

**F53 · S3 — misleading block reason on an expired warranty** (row 7.3). A claim
against a 2021-expired registration returns "Warranty is not valid. Status:
ACTIVE" — the `IsValid()` branch fires before the expiry-specific message.
Separately, no Part/Product response DTO exposes `HasWarranty` or the warranty
period, so a client cannot tell which parts are warrantable.

**Not executed:** `PATCH /Payroll/{id}/pay` on an APPROVED run (irreversible —
posts a SALARIES cash expense and settles advances); `POST /Payroll/{id}/send-payslips`
(real email/SMS); device-punch key rejection (`Hr:DeviceApiKey` unset → every call
503s before the key check; code review confirms 401/404 paths exist);
`POST /cash-book/deposits` happy path (no delete/cancel endpoint exists).

Carried over from the 2026-08-16 run:
- **S2 — `POST /api/Stock/check` accepts negative quantity** — fix written in
  `StockController.cs` (uncommitted): rejects `quantity <= 0` on both the single
  and bulk endpoints, and validates `PartId` on the bulk one. **Verification
  incomplete** — the inventory agent was terminated before finishing the probe
  matrix, and the fix is not in the running image (A1). Re-verify next sweep.
- **S3 — duplicate legacy routes**: several controllers expose both `/api/<name>` and `/api/v1/<name>` (two `[Route]` attributes); others only `/api/v1/<name>` (e.g. `/api/brands` → 404, `/api/v1/brands` → 200). Frontend consistently calls v1, so no user impact — dead routes only. Steps: `GET /api/brands` → 404. Expected: 404 is arguably fine; inconsistency is the issue.
- ~~**S3 — `docs/QUICK_SALE_API_ENDPOINTS.md` stale**~~ — fixed in commit 6885f90.

### Not executed + why

- Modules 3–8 — agents terminated by a session limit mid-run.
- Row 2.7 (permission-based UI visibility) — UI row, API-only run.
- Row 9.3 inbox list / mark-read — 404 on the stale image (A1). Source has
  `InboxNotificationsController` with `GET /`, `GET /unread-count`,
  `PATCH /{id}/read`, `POST /mark-all-read`.
- Backup create/restore — out of scope by policy; only `GET /backups`,
  `/status`, `/drive-status` were called.
- Deleting the base currency (BDT) — guard untested; a bug there would
  soft-delete the system base currency on a shared dev DB. Test on an isolated DB.
- Notification sends (`send-invoice-email`, `send-payment-reminder`,
  `reorder-alert/run`, `test-signalr`) — side-effecting.
- Payroll pay run — dry-run only by policy.

### Cleanup residue

All `QA-0817-` currencies, exchange rates, settings, files and the QA role were
deleted. Three QA users remain **deactivated** with roles cleared (no user-delete
endpoint exists). Two permission rows remain — `qa0817.probe` (test artifact,
unassigned) and `audit.view` (a legitimate catalog entry) — the API exposes no
permission-delete endpoint.


### Fix pass 2026-08-18 (branch feature/theme-sujan-motors)

Scope agreed with the maintainer: **S1 + S2**, plus four design items (gapless
document numbering, salary-advance approval, token revocation, sales-report
reconciliation). Backend plus the frontend work those changes force. S3 findings
are deferred. Every change below builds clean and the suite is green (19 tests,
up from 2).

**Fixed**

| Finding | What changed |
| --- | --- |
| F1 | `StockLevel.RemoveStock` guards on base units when given them; new `POST /api/Stock/levels/{id}/reconcile-units` recovers levels that already drifted |
| F2 | `UnitRepository.UpdateAsync` mutates the tracked entity instead of Remove+Add |
| F3 | Lot movements now adjust the matching `StockLevel` |
| F5 | Unit conversion rounds away from zero, not to nearest even |
| F7 | `Stock/check` returns `partFound:false` for an unknown or deleted part |
| F8 | Import creates brands/categories/units only when `allowNewReferenceData` is set; otherwise unknown names are row errors |
| F14 | Sales-order confirm no longer mixes `ExecuteUpdateAsync` with `SaveChangesAsync` on a RowVersion entity |
| F15 | Convert takes a warehouse (required, existence-checked); web UI gained a picker |
| F16 | Cash refund is netted by the order discount **and** credited against the invoice via new `Invoice.ReturnedAmount` |
| F17 | Credit-note apply no longer writes a `CreditNote` id into `CustomerPayment.SourceAdvancePaymentId` (self-FK); ledger counts the credit once |
| F18 | Debit notes reach the customer ledger |
| F19 | DELIVERED/COMPLETED added to the stock-restore set; the order is cancelled when its last live invoice is |
| F20 | `TillSessionRepository.UpdateAsync` no longer `Update()`s a tracked graph (that was the 409); open sessions report live cash |
| F21/F37 | Document numbers are allocated inside the transaction, or after validation where there is none |
| F23 | Sales-order payload reports the discount in money, plus explicit percentage/amount fields |
| F24 | Report procs exclude PENDING orders, deduct returns, and reconcile `outstanding` with the ledger |
| F25 | Running balance populated on ledger entries |
| F26 | Per-unit discount rounded once, so line and header foot |
| F27 | Zero-value (100% discount) sales accepted |
| F28 | `POST /api/Stock/levels` rejects a quantity instead of silently ignoring it |
| F29 | Supplier credit-note apply is transactional and no longer double-credits |
| F30 | Supplier PUT keeps payment terms and credit limit |
| F31 | Payment provider PUT applies name and type |
| F32 | Overpaying a PO returns 400, not 500 |
| F33/F40 | Cash book filters soft-deleted expenses and payments |
| F34 | Credit-note settlements excluded from the cash book and cash-basis revenue |
| F35 | Damaged/quarantine quantities surface in stock responses |
| F36 | Deleting an in-use payment provider returns 409 |
| F41 | Cash book no longer double-counts applied advances |
| F42 | Customer ledger stops subtracting refunds twice |
| F44 | An APPROVED warranty claim can no longer be rejected |
| F45 | Unknown *and* duplicate employee ids in an attendance payload return 400 |
| F46 | Expired registrations flip on read |
| F51 | Salary advances are REQUESTED → approved → recovered, capped at the monthly salary |
| A3 | Duplicate exchange rate for a pair/date returns 409 |
| A6 | Soft deletes audit as DELETE; unchanged-value audit rows are no longer written |
| A7 | Unknown role names are reported instead of silently dropped |
| A9 | `ownerType`/`ownerId` must be supplied together |
| A10 | ~40 bare-string / `{error}` bodies normalised to `{message}` across 14 controllers |
| A2 | Regression tests added for the F1/F16/F44/F51 money paths |

**Needed no change** — A4, A5 (already in committed code), F4, F6, F22, F43
(fixed in the working tree before this pass). See the triage table above.

**Still open** — every S3 finding: F9–F13, F38, F39, F47–F50, F52, F53, A11–A17,
and the duplicate legacy `/api/<name>` routes. A15 is a data-config task. A1/A8
are environment issues that a rebuilt image resolves.

**Re-verification required.** None of the above has been exercised against a
running API — the sweep that produced these findings must be re-run. Rebuild the
image first (A1) and use `http://localhost:5000`.


### S3 pass 2026-08-18

The deferred S3 findings were picked up in a second pass. All of them are now
closed or explicitly resolved as not-a-defect.

| Finding | What changed |
| --- | --- |
| A11 | Rate lookup falls back to the reciprocal of the opposite pair, so the base currency can be converted out of and cross-rates work on X→base seed data alone |
| A12 | Unknown currency codes are reported as unknown rather than as a missing rate; converting an amount of 0 is allowed |
| A13 | `/login` returns one message for unknown user and wrong password, closing the enumeration oracle |
| A14 | `ipAddress`/`userAgent` stamped on every audit row (X-Forwarded-For aware). The unchanged-value noise was already fixed with A6 |
| A15 | BUSINESS/BRANDING setting rows seeded (blank values), so the categories exist and Company Profile has fields to fill |
| A16 | `GET /Admin/users` paged; `POST users\|roles\|permissions` return 201; currency `createdAt`/`updatedAt` mapped; `DELETE /Admin/permissions/{id}` added with an in-use guard |
| A17 | Lockout returns 429 + `Retry-After` instead of 401 |
| F9 | StockLotMovement list endpoints no longer fire concurrent DbContext reads (four endpoints, not the two reported) |
| F11 | Missing `Include`s restored on StockLevel and WarehouseLocation reads, so variant/unit/warehouse names populate |
| F12 | `PUT /products/{id}` keeps omitted optional fields; an explicit `""` still clears |
| F13 | `check-circular-reference` binds `parentCategoryId` as well as `newParentId` |
| F28 | `BusinessRule` body status matches the response (400/409); customer code honoured when supplied; dead `creditLimit` field removed |
| F38 | Supplier code honoured when supplied and free |
| F39 | `isFromSameSupplier` is nullable — null when no supplier was given — so the returns UI stops labelling every lot "Other" |
| F49 | Aging reports refuse a date range with a message naming `asOfDate`, instead of discarding it silently |
| F52 | Declared holidays reach the monthly summary (deduplicated against marked ones) and the daily sheet (`isHoliday`/`holidayName`) |

**Resolved as not-a-defect**

- **F28 "paymentNumber null on every row"** — no such field exists anywhere in
  the codebase. The response carries `transactionNumber`, which is populated.
- **F28 "refunds stored as negative CustomerPayment rows"** — this is the model
  the till reconciliation, cash book and customer ledger all read (F41/F42), so
  changing the storage would undo those fixes for a presentational complaint.
- **F13 "re-parenting silently ignored"** — re-parenting is unimplemented rather
  than ignored: `UpdateCategory` has no parent field and `Category` no setter.
  Supporting it means cascading DepthLevel and breadcrumb updates to every
  descendant, which is a feature rather than a nit.
- **F10, F26, F27, F37, F47, F48, F50, F51, F53** — closed earlier in the S1/S2
  pass or by the concurrent work it absorbed.

Still unverified against a running API, as with the S1/S2 pass.


### Verification sweep 2026-08-18 (rebuilt image, `http://localhost:5000`)

The image was rebuilt from this branch before running (`smapi:local` created
2026-08-18), all pending migrations applied on startup, and the A15 settings
seeded. This is the first run where the container matches the source.

**Three regressions were found and fixed during the sweep** — two of them
introduced by earlier fixes in this branch, which is exactly what the run was
for:

- **F20 was still broken.** The earlier fix blamed `DbSet.Update()`. With that
  ruled out the change tracker still showed `TillCashDrop => Modified`: EF treats
  a child discovered through a navigation collection as an existing row when its
  key is already set, and `BaseEntity` assigns the Guid in the constructor. Fixed
  by inserting the drop explicitly (`AddCashDropAsync`).
- **The customer ledger double-counted returns.** Crediting the return against
  the invoice (F16) made `totalInvoiced` net of returns while `totalRefunds`
  subtracted the same return again — a 90 refund moved the balance by 190. The
  balance is now `invoiced - payments + DN - CN` with refunds as negative
  payments, and the entries list matches it.
- **Sales reports deducted the gross return value.** They used
  `SalesReturn.RefundAmount` (100) rather than what was credited (90), so a day
  with two discounted returns read 160 against 180 retained. Now read from
  `Invoice.ReturnedAmount`.
- **F19 was half-fixed.** Adding DELIVERED/COMPLETED to the restore set was not
  enough: the lookup only matched `ReferenceType="SalesOrderLine"`, and quick
  sales tag movements `"QuickSale"` against the order id, so POS invoices still
  destroyed stock on cancel.

**Verified working against the live API**

| Finding | Evidence |
| --- | --- |
| A3 | duplicate NPR->INR rate for the same date: 201 then **409** |
| A6 | soft-deleted currency audits as `DELETE Isdeleted 'False'->'True'` |
| A7 | unknown role -> 400 naming `NoSuchRoleQA` |
| A9 | ownerType without ownerId -> 400, both directions; both/neither -> 200 |
| A11 | 100 BDT->INR = **76.92**, cross-rate 100 INR->NPR = **158.54**, direct INR->BDT still 130.00 |
| A12 | amount 0 -> 200; `XXX` -> 400 "Unknown currency 'XXX'." |
| A13 | wrong password and unknown user both return `Invalid credentials` |
| A14 | every audit row carries `ipAddress` and `userAgent` (15 of 15) |
| A15 | 11 BUSINESS/BRANDING settings seeded on startup |
| A16 | users paged (`pageSize=9999` clamps to 200); user/role/permission creates return **201**; currency `createdAt` populated; `DELETE /permissions/{id}` 204, and 409 while granted to a role |
| A17 | lockout -> **429** with `Retry-After: 900` |
| F3 | 3 lot SALE movements: lot 98->95 **and** level 98->95 |
| F7 | unknown part -> `partFound:false`, "Product not found"; quantity 0 -> 400 |
| F9 | `/lot/{id}` and `/type/SALE` both 200 with 6 and 13 rows, all lot numbers resolved |
| F11 | `/levels/part/{id}` returns partName, variantName, variantSku, unitName, warehouseName |
| F12 | omitted optional fields survive a PUT; explicit `""` still clears |
| F13 | `parentCategoryId` binds; parent-under-child **true**, child-under-parent **false**, self **true**, root **false** |
| F16 | 2x100 less 20, return 1 unit: refund **-90.00**, invoice grandTotal 90 / paid 90 / **PAID**, no phantom receivable |
| F17 | apply 100 note: 200, note FULLY_USED, invoice **amountPaid 200 / outstanding 0 / PAID** |
| F18 | debit note 250.55 moves the balance by exactly **+250.55** |
| F19 | quick sale 90->87, cancel invoice -> **90 restored**, order CANCELLED |
| F20 | two cash drops 200; `cashDropsTotal` 100 then 150; `expectedAmount` 1000->900->850; close 1000+660-180-150 = **1330**, overShort 0.00 |
| F21 | 3 rejected quick sales then INV016 -> **INV017**, contiguous |
| F23 | order detail `discount=20.0` and the header foots 200-20=180 |
| F24 | summary net **180.00** (two 180 sales, one 90 refund each); by-customer revenue 180 / paid 180 / outstanding **0.00**; by-product quantitySold **2** (4 sold - 2 returned) |
| F25 | running balances populated, latest matches the summary balance exactly (-248.35) |
| F30 | supplier PUT keeps `NET15` / `150000` |
| F38 | supplier code `QA0818SUP` honoured; duplicate -> 409 |
| F51 | 99999 against a 20000 salary -> 400 with the limit; request is `REQUESTED` with no cash-book row; approve -> `OUTSTANDING` + 5000 expense posted |
| F52 | declared holiday gives `holidayDays=1` and `isHoliday:true`/`holidayName` on the daily sheet; a normal date stays false |

**Second batch — procurement, warranty and import**

One more defect surfaced: **F29 was only half-fixed.** The 500 was gone and the
note consumed correctly, but applying 100 to PO010 left outstanding at 300.00 —
`PurchaseOrder.ApplyCredit` records `CreditAppliedAmount` while every read path
computed outstanding as `TotalAmount - PaidAmount`, so the credit was stored and
then ignored. Fixed by giving PurchaseOrder an `OutstandingAmount` that nets
payments *and* credits, and using it in the read paths and both domain guards.

| Finding | Evidence |
| --- | --- |
| F8 | unknown brand/category/unit -> three row errors naming each value, control row valid; with `allowNewReferenceData=true` both rows valid and the creations listed under newBrands/newCategories/newUnits; the dry run creates nothing |
| F29 | apply 100 to PO010: 200, note FULLY_USED, outstanding **300 -> 200**; a 250 payment then refused; paying 200 settles it to exactly **0.00** (400 cash + 100 credit = 500) |
| F31 | PUT renames and retypes a provider (`QA-0818 Renamed` / `MOBILE_BANKING`) |
| F32 | overpay 5000 against 500 -> **400** with the balance named; valid 200 payment moves outstanding 500 -> 300 |
| F33 | expense 1234.56: cash out 5180 -> 6414.56 -> **back to 5180** after delete |
| F34 | issuing a 100 credit note leaves `totalCashOut` **unchanged at 5380.00**, zero CREDIT_NOTE rows in cashOut |
| F35 | GRN 10 received / 1 damaged / 1 wrong: stock 87 -> **95** good, `damagedQuantity=1`, `quarantineQuantity=1` |
| F36 | unused provider deletes; in-use provider -> **409** with a clear message |
| F37 | rejected GRN create did not burn a number — next real one is **GRN008**; credit note **CN005** likewise contiguous |
| F39 | no supplierId -> `isFromSameSupplier: null`; with it, **true** for that supplier's lots and **false** for an unrelated lot |
| F44 | rejecting an APPROVED claim -> **400** "Cannot reject. Current status: APPROVED", approval left intact |
| F46 | registration expiring 2022-01-01 self-corrects to **EXPIRED** on read; absent from `/active`, present in `/expired` |
| F47 | claim dated 2027-01-01 -> 400 "Claim date cannot be in the future" |
| F49 | aging report *and* export both 400 on a date range; export with `asOfDate` returns a 6.9 KB xlsx; expenses export still accepts a range |
| F53 | claim on an expired warranty -> "Warranty expired on 2022-01-01. New claims are not allowed." (was "Status: ACTIVE") |

**Known residual:** SalesByProduct still deducts the gross line value in its money
column; quantities are exact. Apportioning an order-level discount across parts is
a separate question — see UseInvoiceReturnCreditInSalesReports.

**Residue:** QA users `qa0818probe` / `qa0818valid` / `qa0818valid2` (no delete
endpoint), role `QA0818Role`, supplier `QA0818SUP`, employee `EMP004` with an
approved 5000 advance, warranty registrations WR-2026-00004/5 and claim
WC-2026-00003, PO010 with GRN008 / PR004 / CN005, and sales orders SO022-SO026
with their invoices. The QA exchange rate, holiday, provider and daily expense
were deleted. The test part QA-0817b-S-Part2 now has warranty enabled.

---

## 1. Auth

| # | Workflow | Expected | API | UI |
| --- | --- | --- | --- | --- |
| 1.1 | Login with valid creds | 200, JWT + user/roles returned | PASS | |
| 1.2 | Login with wrong password | 401, no token | PASS (msg differs, A13) | |
| 1.3 | Login with unknown user | 401 | PASS (msg differs, A13) | |
| 1.4 | Call an endpoint with no token | 401 | PASS (malformed/tampered/`alg:none` also 401) | |
| 1.5 | Call an Admin-only endpoint as Salesman | 403 | PASS (403 on users/roles/audit/backups/settings) | |
| 1.6 | Refresh token | New access token, old still valid until expiry | PASS (new `jti`; garbage → 401) | |
| 1.7 | Change own password → re-login with new, old rejected | 200 then 401 on old | PARTIAL (works, but body `username` ignored — A4) | |
| 1.8 | Logout | Token invalidated / client clears state | FAIL (no logout endpoint; deactivated user's JWT still works — A5) | |
| 1.9 | Lockout after N failed attempts | 429/403, clear message | PASS w/ caveat (10 fails → 401 not 429/403, A17) | |

## 2. Admin

| # | Workflow | Expected | API | UI |
| --- | --- | --- | --- | --- |
| 2.1 | Currencies CRUD + set default | Create/list/update/delete; default applied | PASS (`set-base` exclusive; dup code 409) | |
| 2.2 | Exchange rates: add rate, convert amount | Conversion math correct at 2dp | PASS (100 INR→BDT = 130.00; 33.333 → 43.33) — see A11/A12 | |
| 2.3 | Exchange rate validation (duplicate date/currency) | 400 with message | **FAIL** (dup accepted 201, both active — A3) | |
| 2.4 | Application settings: get/update company profile | Changes persist + reflected in app | PASS (CRUD ok; profile itself unpopulated — A15) | |
| 2.5 | Users: create user, assign role | Login works with new creds | PARTIAL (unknown role silently accepted — A7) | |
| 2.6 | Roles/permissions: revoke a permission | Feature disappears / 403 for that user | PASS (403 after ~60s cache; catalog empty — A8) | |
| 2.7 | Permission-based visibility: pages hidden per role | UI reflects permission set | — (UI row) | |

## 3. Inventory

| # | Workflow | Expected | API | UI |
| --- | --- | --- | --- | --- |
| 3.1 | Brands CRUD | Create/list/update/delete | PARTIAL (CRUD ok; duplicate brand name accepted — F6) | |
| 3.2 | Categories CRUD (incl. hierarchy) | Create parent/child, list by parent | PASS (breadcrumb, ancestors, delete-with-children 422; circular check param broken — F13) | |
| 3.3 | Units + conversions | Multi-unit conversion correct (see `MULTI_UNIT_IMPLEMENTATION_GUIDE.md`) | **FAIL** (every unit update/activate/deactivate 500s — F2; fractional factors round — F5) | |
| 3.4 | Parts CRUD + variants + pricing | Create part, add variant, price set | PASS (auto SKU, 2 variants, dup variant code 409) | |
| 3.5 | Part with 0 price / 0 stock saves | Allowed or clearly validated | PASS (0 price → 201; negative price → 400) | |
| 3.6 | Duplicate part code/SKU | 400 with message, no dup row | **FAIL** (dup `partNumber` → 201, dup row; `by-code` picks one — F4) | |
| 3.7 | Product media: upload image to part | File stored, URL returned, image renders | PASS (PNG round-trips; `.exe` rejected) | |
| 3.8 | Product import (CSV) | Valid rows imported, invalid reported | PARTIAL (5 valid/3 errors correct; unknown brand+category silently created — F8) | |
| 3.9 | Warehouses + locations CRUD | Create/list; assign part to location | PASS (dup code 409, dup bin 409, delete-with-stock 409) | |
| 3.10 | Stock lot create + movements | Lot in, lot out adjusts quantity | **FAIL** (lot movement never updates StockLevel — F3; 2 read endpoints 500 — F9) | |
| 3.11 | Stock adjustment | Qty changes, audit trail records it | PASS (+100→−30 = 70; audit row with actor) | |
| 3.12 | Stock take: count → variance posted | Variance = counted - system | PASS (variance −6 @100 = −600; REVIEW→approve applied) | |
| 3.13 | Sell more than available stock | 400 or clear validation, no negative stock | PASS (steps 70→0 then 400; negative stock unreachable) | |
| 3.14 | Stock by variant vs base part | Deltas land on the right level | PASS (15/7 on variant levels, base untouched) | |
| 3.15 | Inventory reports (value, low-stock, movement) | Numbers reconcile with stock | **FAIL** (`quantityOnHand:1` vs `stockValue:4000` for 40 units — F1) | |

## 4. Procurement

| # | Workflow | Expected | API | UI |
| --- | --- | --- | --- | --- |
| 4.1 | Supplier CRUD + ledger opening balance | Create; balance shown | PARTIAL (CRUD + ledger correct; PUT wipes creditLimit → 0 — F30) | |
| 4.2 | Purchase order: create → submit → confirm → deliver | Status transitions enforced; no deliver before confirm | PASS (all 5 illegal transitions 400; totals 1354.99 +5% = 1422.74 exact; no 500s) | |
| 4.3 | Cancel PO at each stage | Cancelled before confirm; blocked after | PASS (ledger reversed on confirmed-cancel; PARTIAL/DELIVERED blocked) | |
| 4.4 | Goods receipt verify → accept/reject | Accepted lines raise stock; rejected lines don't | PARTIAL (stock + lots + cost correct, over-receipt blocked; damaged/quarantine always read 0 — F35) | |
| 4.5 | Purchase return (full/partial) | Stock restored, money flows correct | PASS (partial 15→13, full →10, lot avail 0, ledger closes to 0.00) | |
| 4.6 | Supplier payment (cash/bank) + ledger | Payment posts; ledger balance updates | PARTIAL (money + running balances correct; overpay rejected as 500 not 400 — F32) | |
| 4.7 | Supplier payment account CRUD | Create/list/update/delete | PASS (single-default invariant held) | |
| 4.8 | Credit note from supplier + apply to payment | Balance reduces; partial apply allowed | **FAIL** (apply 500s while still consuming the note — F29) | |
| 4.9 | Daily expense entry | Expense posts to cash book | PARTIAL (posts correctly; deleted expenses persist — F33; credit notes counted as cash — F34) | |
| 4.10 | Payment providers configuration | Create/list/update/delete | PARTIAL (PUT never applies name/type — F31; delete-in-use 500 — F36) | |
| 4.11 | Purchase reports reconcile with POs | Totals match the order/invoice data | PASS (summary + by-supplier match the POs; cancelled excluded) | |

## 5. Sales (highest risk — full smoke pass required)

| # | Workflow | Expected | API | UI |
| --- | --- | --- | --- | --- |
| 5.1 | Customer CRUD + vehicles | Create customer, attach vehicle | PASS (vehicleLabel flows onto SO/invoice/print-data) | |
| 5.2 | Quotation: create → accept → convert to SO | Quote status updates; SO created from it | **FAIL** (converts, but the SO has no warehouse and can never confirm — F15) | |
| 5.3 | Sales order: create → challan → invoice | Ordered qty reserved; invoice decrements stock | **FAIL** (confirm 500s on every order; challan/credit-sale path dead — F14) | |
| 5.4 | Quick sale / POS: search → cart → discount → pay cash + due | Totals match on SO, invoice, receipt | PASS (SO/invoice/print-data all 180; paid 100 due 80; stock 100→98) | |
| 5.5 | Discount > subtotal / negative qty / negative price | 400, no order created | PARTIAL (all 400 with no order — except negative unit price → 201, F22) | |
| 5.6 | Invoice: issue → partial pay → settle due | Due balance updates correctly | PASS (100→130→180 PAID; overpay rejected) | |
| 5.7 | Cancel invoice mid-payment | Paid amount refunded/credited correctly | PARTIAL (block-while-paid correct; after refund, stock not returned, SO left DELIVERED owing 100 — F19) | |
| 5.8 | Sales return (full/partial) → refund / customer credit | Stock restored; money/credit correct | **FAIL** (store-credit correct; CASH_REFUND refunds 100 on a 90 unit + phantom 100 due — F16) | |
| 5.9 | Customer debit note / credit note | Balance direction correct | **FAIL** (debit note moves balance 0.00 — F18; credit note consumed without crediting — F17) | |
| 5.10 | Customer payment + ledger | Payment posts; ledger balance matches | PARTIAL (totals foot; `runningBalance` 0 on every row — F25; DN/CN absent) | |
| 5.11 | Till session: open → collect → close | Cash in till reconciles with sales | PARTIAL (close math exact: 1000+2280−100=3180 vs 3200 → over 20; cash-drops 409 always, live session shows 0.00 — F20) | |
| 5.12 | Proforma invoice | Creates document without stock impact | PASS (PI001 = 501.10, stock unchanged, SO untouched) | |
| 5.13 | Technician assignment on SO | Records to technician ledger | PARTIAL (id/name persist; **no technician ledger exists**; due booked to customer) | |
| 5.14 | Sales reports (day, month, by customer, by product) | Totals reconcile with invoices | PARTIAL (reports agree with each other, not with reality — F24) | |
| 5.15 | Cross-channel: same sale on web POS vs mobile | Identical totals everywhere | PASS for held sale + advance credit (no mobile client available) | |

## 6. HR

| # | Workflow | Expected | API | UI |
| --- | --- | --- | --- | --- |
| 6.1 | Employee CRUD | Create/list/update/delete | PASS (full lifecycle + photo set/clear; negative salary 400) | |
| 6.2 | Attendance: clock in/out, mark absent | Records create; summary counts correct | PARTIAL (counts correct; unknown/duplicate employeeId → 500 — F45; device punch unreachable) | |
| 6.3 | Shifts CRUD + assign to employee | Schedule shows correctly | PASS (delete blocked while assigned; `shiftName` null on GET-by-id — F50) | |
| 6.4 | Holidays CRUD | List filters by year | PASS (dup date 400; year filter correct) — but holidays never reach attendance/payroll (F52) | |
| 6.5 | Leave request: apply → approve/reject | Status transitions enforced | PASS (overlap, double-approve, edit-approved all 400; balance + entitlement enforced) | |
| 6.6 | Salary advance: request → approve → deduct | Advance deducts from next payroll | PASS w/ design gap (deducts exactly; **no approve step, no cap** — F51) | |
| 6.7 | Payroll run (DRY-RUN only unless explicitly asked) | Gross/net/months correct; do not post real run | PASS (matched hand-calc to the cent; `/pay` never called, run deleted) — but F43 | |

## 7. Warranty

| # | Workflow | Expected | API | UI |
| --- | --- | --- | --- | --- |
| 7.1 | Warranty registration on sale | Registration appears for customer | PASS (WR-2026-00001, expiry = start+12mo, visible on all 3 lookups) | |
| 7.2 | Claim: submit → approve → complete | Status flow enforced; dates valid | PARTIAL (full flow works; **APPROVED claim can be rejected** — F44; future-dated claim accepted — F47) | |
| 7.3 | Claim outside warranty window | 400 or blocked with reason | PASS (blocked 400; message misleading — F53; expired stays ACTIVE — F46) | |

## 8. Finance / Reports

| # | Workflow | Expected | API | UI |
| --- | --- | --- | --- | --- |
| 8.1 | Dashboard: today's sales, stock, receivables | Numbers match invoices/payments | PARTIAL (receivables + cashflow tie out; inherits the sales-report issue F24) | |
| 8.2 | Cash book: entries list + balance | Balances reconcile | **FAIL** (counts soft-deleted rows — F40; double-counts applied advances — F41) | |
| 8.3 | Financial reports (P&L, receivables, payables) | Reconcile with ledgers | **FAIL** (P&L/dashboard/expenses agree; customer ledger off by 200 — F42) | |
| 8.4 | Date-range filtering on reports | Empty range handled, boundary dates inclusive | PARTIAL (boundaries inclusive, no 500s, xlsx/pdf valid; P&L accepts from>to — F48; aging ignores range — F49) | |

## 9. Audit & Platform

| # | Workflow | Expected | API | UI |
| --- | --- | --- | --- | --- |
| 9.1 | Audit log records create/update/delete | Entry exists with actor + timestamp | PARTIAL (no DELETE row for soft-deletes — A6; noise + null IP/UA — A14) | |
| 9.2 | Audit log filter by entity/user/date | Correct rows returned | PASS (entity/user/date filters all correct) | |
| 9.3 | Notifications: inbox list, mark read | State persists | PARTIAL (logs+settings 200; inbox 404 on stale image — A1) | |
| 9.4 | File upload (`/api/files`) | File stored; URL retrievable | PASS (bytes byte-identical; fake magic bytes 400) — but A9 | |
| 9.5 | Backups: list/create (do NOT restore) | Backup file listed | PASS (list only; nothing run) | |

---

## Cross-cutting (run once per sweep)

- [x] Pagination: list endpoints honor `page`/`pageSize`; total counts match
      (2026-08-17: clamped correctly, `totalPages = ceil(total/size)`; `/Admin/users` unpaged — A16)
- [ ] Filtering: status/date filters on every list return sensible results
- [x] Empty collections render as "no data", not an error (except `/files` — A9)
- [x] Invalid JSON body → 400, not 500 (also: wrong content-type → 415, empty body → 400)
- [x] Unknown route → 404 (not 401/500) — verified authed and anonymous
- [ ] Money: 2dp rounding consistent everywhere (BDT ৳)
- [ ] No console/network errors during exercised flows (DevTools)
- [ ] Audit trail visible for every mutation tested above

## Production-readiness (before deploy)

- [ ] JWT secret is a strong value from env, not the dev default
- [ ] CORS is restricted (`AllowAnyOrigin` is dev/Cloudflare-tunnel only)
- [ ] HTTPS enforced / behind TLS terminator
- [ ] `Seq:Url` points at real log sink; logs show no sensitive payloads
- [ ] EF migrations match schema (no pending `dotnet ef migrations list`)
- [ ] Seeder disabled or guarded in production
- [ ] Backups scheduled; restore procedure documented and tested
- [ ] `docs/` accurate: no one-off fix summaries, current-state only

## Regression: recent fixes (must re-verify each sweep)

| # | Fix | How to verify | API | UI |
| --- | --- | --- | --- | --- |
| R1 | Purchase-order credit notes section (frontend) | On an order with a credit note: the credit-note section renders and lists them; totals include the credit | PASS (code: PO form renders credit-applied + net payable) | UI pending |
| R2 | Debugger/pause artifact removed from frontend | Open the app, run a full workflow (5.4): no `debugger` statement breaks the JS, no dev-only overlay/log | PASS (no `debugger` in src) | UI pending |
| R3 | Missing translation keys (i18n) added | Switch language in UI: no raw/blank key text anywhere in the exercised screens | PARTIAL (keys present in PO form) | UI pending |
