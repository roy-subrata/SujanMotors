# Stock Adjustment & Stock Take / Cycle Count

> Business explanation + real data-flow examples for the two inventory-correction tools.
> Implemented July 2026. Backend: `StockController.AdjustStock`, `StockTakeController`,
> shared `StockAdjustmentApplier`. Frontend: **Inventory → Stock Take** (`/inventory/stock-takes`).

---

## 1. The business problem

The system believes it knows how much stock you have. Reality disagrees, constantly:

- A filter falls behind a shelf and cracks (**breakage**)
- A helper gives a customer 2 but rings up 1 (**mis-pick**)
- Something is stolen (**shrinkage**)
- A carton actually contained 11 pieces, not 10 (**supplier over-pack**)

Every one of these makes **system stock ≠ physical stock**. If it is never corrected:

- you sell parts you don't have (angry customers, cancelled orders),
- you reorder parts you already have (dead cash on the shelf),
- your profit report is wrong, because "missing" stock is still counted as an asset.

Two tools fix this, at two different scales:

| | Stock Adjustment | Stock Take / Cycle Count |
|---|---|---|
| **When** | You *already know* one item is wrong, right now | You *want to find out* what's wrong, across the whole warehouse |
| **Scale** | One product, one correction | Hundreds of products, one exercise |
| **Who** | Manager, on the spot | Staff count, manager reviews and approves |
| **Input style** | Signed delta ("−1, damaged") | Physical count ("I see 2") — system computes the variance |
| **Analogy** | Fixing one wrong entry in the ledger | Closing the books at month-end |

**Stock adjustment is the pen; stock take is the exam.** The stock take does not replace
adjustments — its approval step literally *generates* adjustments through the same code path
(`StockAdjustmentApplier`), so both leave identical, audit-friendly footprints.

---

## 2. Stock Adjustment — data flow

**Business moment:** You drop a NAVANA 17-plate battery while moving it. It's dead.
You know exactly what happened — no investigation needed.

**Before:**

```
StockLevel  (NAVANA-17, Malawoori):  QuantityOnHand = 4
StockLot    LOT001:                  QuantityAvailable = 4   (cost ৳14,000 each)
```

**Action:** Stock page → Adjust → quantity **−1**, reason **DAMAGED**.
One API call: `POST /api/v1/stock/adjust`.

**What the system writes (one transaction):**

```
StockLevel        4 → 3
StockMovement     ADJUST  −1   reason: DAMAGED   ref: ADJUST-20260707...
StockLot LOT001   4 → 3
StockLotMovement  ADJUSTMENT  1   cost ৳14,000
```

**Business meaning of each row:**

| Record | Answers the question |
|---|---|
| `StockLevel` | "How many can the POS sell right now?" → 3 |
| `StockMovement` | "Who reduced it, when, and why?" (audit trail) |
| `StockLot` + `StockLotMovement` | "How much inventory **value** was lost?" → ৳14,000 |

The lot rows matter because product costing in this system is **lot-driven**
(see `docs/…cost model`). Before the July 2026 fix, `/stock/adjust` updated only the
`StockLevel` — the level said 3 while the lots still said 4, and inventory valuation
quietly drifted with every adjustment. Now every adjustment moves lots in step:

- **Decrease** → consumes AVAILABLE lots **FIFO** (oldest first, same as sales), one
  `ADJUSTMENT` lot movement per lot touched.
- **Increase** (found stock) → added to the **newest** lot (capacity raised), so it sells
  at the latest cost.
- **No lot data at all** → the level is still corrected; the response carries a
  `lotSyncWarnings` flag instead of failing.

---

## 3. Stock Take / Cycle Count — data flow

**Business moment:** Friday afternoon. Nothing is "known broken" — but nobody has
physically verified the shelf in months. You want the truth.

### Step 1 — Start (snapshot)

**Inventory → Stock Take → New Stock Take** → pick warehouse (optionally one category
for a cycle count). The system freezes what it *expects* at this moment:

```
StockTake ST001   status: COUNTING   snapshot: 07 Jul, 12:47
├─ BOSCH AIR FILTER     expected 3   cost ৳235
├─ LUCKY OIL FILTER     expected 5   cost ৳300
├─ NAVANA 17-PLATE      expected 4   cost ৳14,000
├─ NAVANA 21-PLATE      expected 3   cost ৳18,000
└─ NAVANA 29-PLATE      expected 2   cost ৳21,000
```

The snapshot matters because **the shop stays open**. Counting against live numbers that
change under you is how counts go wrong. Zero-stock items are included on purpose — that
is how "found" stock (system says 0, shelf has 2) gets caught.

Only **one open stock take per warehouse** is allowed (the API returns 409 otherwise) —
two overlapping counts would both adjust the same stock.

### Step 2 — Count

Staff walk the shelf and type only what they physically **see** — never a plus/minus:

```
BOSCH     counted 2   → variance −1   (−৳235)   ← one missing
LUCKY     counted 6   → variance +1   (+৳300)   ← one extra found
NAVANA-17 counted 4   → variance  0             ← perfect
21-PLATE, 29-PLATE    → not counted yet
```

Making staff compute deltas themselves is where counting errors come from; here the
system computes the variance.

### Step 3 — Review

**Submit for Review** locks the counts. The manager sees the variance list *before
anything touches stock*: "one BOSCH missing, one LUCKY found, net **+৳65**".

If a −1 had appeared on a ৳21,000 battery instead, this is the moment to say
"go recount that one" (**Reopen Counting**) — not after stock already changed.

### Step 4 — Approve

One click → one transaction:

```
BOSCH:   delta = 2 − 3 = −1 → StockLevel 3→2, LOT005 −1, StockMovement ADJUST −1
                              reason COUNT_CORRECTION, ref: ST001
LUCKY:   delta = 6 − 5 = +1 → StockLevel 5→6, LOT004 capacity 5→6
NAVANA-17: variance 0       → untouched
Uncounted lines             → skipped, reported ("2 lines skipped")
StockTake ST001             → COMPLETED
```

**End result:** the same `StockMovement` / `StockLotMovement` records a manual adjustment
would create — but all stamped **ST001**. Six months later, "why did BOSCH drop by 1 on
7 July?" has a one-word answer: stock take ST001, counted by X, approved by Y, worth ৳235.

---

## 4. Why "apply the variance", not "set stock to the count"

Approval applies `delta = counted − expected(snapshot)` — a **delta**, not an overwrite.

Example: you counted BOSCH at 2, then sold one *before approving*. Current stock is now 2.

- Overwriting stock *to* 2 would resurrect the sold unit → **wrong**.
- Applying the −1 variance gives 1 → **correct**: one was missing, one was sold, one remains.

Sales made between snapshot and approval are therefore never double-counted.

**Guard:** if so much was sold in between that a negative variance no longer fits,
approval applies **nothing** (all-or-nothing transaction) and returns the conflicting
lines: *"stock moved since counting — recount this item"*. Reopen → recount → approve again.

---

## 5. Status lifecycle

```
        create (snapshot)          submit                approve (apply variances)
  ──────────────────────► COUNTING ──────► REVIEW ─────────────────────► COMPLETED
                              ▲              │
                              └── reopen ────┘
                              (recount needed)

  COUNTING / REVIEW ──cancel──► CANCELLED   (counts kept for reference, stock untouched)
```

Terminal states: `COMPLETED`, `CANCELLED`. Double-approval is blocked by the status check
plus optimistic concurrency (`RowVersion`).

---

## 6. Practical rhythm for the shop

| Cadence | Tool | Example |
|---|---|---|
| Whenever damage/loss happens | Stock Adjustment | Dropped battery → −1 DAMAGED, seconds, no ceremony |
| Weekly / monthly | Cycle count (stock take with a **Category** filter) | Batteries this week, filters next week |
| Before year-end accounts | Full-warehouse stock take | Everything counted, variance report to the accountant |

The `TotalVarianceValue` on a completed stock take (Σ variance × lot cost) is the number
your accountant wants: the shrinkage/write-off value for the period.

---

## 7. API quick reference

| Endpoint | Purpose |
|---|---|
| `POST /api/v1/stock/adjust` | Single lot-aware adjustment (Admin/Manager) |
| `POST /api/v1/stocktake` | Start stock take (snapshot); body: `warehouseId`, optional `categoryId`, `notes` |
| `GET /api/v1/stocktake` | Paged list (`status`, `warehouseId`, `search` filters) |
| `GET /api/v1/stocktake/{id}` | Header + all lines (count sheet / variance review) |
| `PUT /api/v1/stocktake/{id}/counts` | Batch count entry; `countedQuantity: null` clears a count |
| `POST /api/v1/stocktake/{id}/submit` | COUNTING → REVIEW |
| `POST /api/v1/stocktake/{id}/reopen` | REVIEW → COUNTING (recount) |
| `POST /api/v1/stocktake/{id}/approve` | Apply variances in one transaction → COMPLETED |
| `POST /api/v1/stocktake/{id}/cancel` | Cancel without touching stock |

All quantities are in **base units**. Adjustment reasons in use: `DAMAGED`, `EXPIRED`,
`LOST`, `FOUND`, `COUNT_CORRECTION` (stock-take approvals always use `COUNT_CORRECTION`
with the stock-take number as reference).
