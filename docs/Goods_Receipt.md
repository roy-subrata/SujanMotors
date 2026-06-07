# Goods Receipt (GRN) UI Specification

## Overview

The Goods Receipt screen allows to receive products against a Purchase Order and record any discrepancies such as damaged or incorrect items.

### Design Principles

* Fast data entry
* Minimal training required
* One-click acceptance for perfect deliveries
* Automatic inventory status management
* Clear visibility of issues
* Mobile-friendly workflow

---

# User Flow

```text
Purchase Order
    ↓
Goods Receipt
    ↓
Review Items
    ↓
Post Receipt
    ↓
Inventory Updated

Good Items      → Available Stock
Damaged Items   → Damaged Stock
Wrong Items     → Quarantine Stock
```

---

# Screen Layout

```text
+----------------------------------------------------------------+
| Goods Receipt #GRN-00125                                       |
+----------------------------------------------------------------+

Supplier: ABC Auto Parts
Purchase Order: PO-00145
Receipt Date: 2026-06-07
Supplier Invoice: INV-2026-500

+----------------------------------------------------------------+

| Product     | Ord | Rec | Good | Dam | Wrong | Notes          |
|-------------|-----|-----|------|-----|-------|----------------|
| Brake Pad   |100  |100  |95    |3    |2      | Box damaged    |
| Engine Oil  |50   |50   |50    |0    |0      |                |

+----------------------------------------------------------------+

Summary

Good Items:      145
Damaged Items:     3
Wrong Items:       2

+----------------------------------------------------------------+

[ Save Draft ]
[ Post Receipt ]
[ Post & Create Return ]
```

---

# Header Section

## Fields

| Field                   | Required | Editable |
| ----------------------- | -------- | -------- |
| Supplier                | Yes      | No       |
| Purchase Order          | Yes      | No       |
| Receipt Date            | Yes      | Yes      |
| Supplier Invoice Number | No       | Yes      |
| Received By             | Auto     | No       |
| Notes                   | No       | Yes      |

---

# Item Grid

## Columns

| Column     | Description                  |
| ---------- | ---------------------------- |
| Product    | Product Name                 |
| Ordered    | Quantity ordered on PO       |
| Received   | Quantity physically received |
| Good       | Acceptable quantity          |
| Damaged    | Damaged quantity             |
| Wrong Item | Incorrect product quantity   |
| Notes      | Optional comments            |

---

# Auto Calculation Rules

## Rule 1

When Received, Damaged, and Wrong Item are entered:

```text
Good Quantity =
Received Quantity
- Damaged Quantity
- Wrong Item Quantity
```

Example:

```text
Received = 100
Damaged = 3
Wrong = 2

Good = 95
```

---

# Quick Actions

## Accept All

Purpose:

Used when all items are received in perfect condition.

Action:

```text
Received = Ordered

Good = Ordered

Damaged = 0

Wrong Item = 0
```

Example:

```text
Ordered = 100

Click:
[ Accept All ]

Result:

Received = 100
Good = 100
Damaged = 0
Wrong = 0
```

---

# Condition Capture

Warehouse users should see business-friendly labels.

Visible values:

```text
Good
Damaged
Wrong Item
```

Inventory statuses should remain hidden from users.

System mapping:

```text
Good       → Available

Damaged    → Damaged

Wrong Item → Quarantine
```

---

# Row Detail Drawer

Selecting a row opens a detail panel.

Example:

```text
-------------------------------------------------

Product: Brake Pad

Ordered Quantity: 100

Received Quantity:
[ 100 ]

Condition:

(●) All Good

( ) Damaged

Damaged Qty:
[ 3 ]

( ) Wrong Item

Wrong Qty:
[ 2 ]

Notes:

[ Box crushed during transport ]

-------------------------------------------------
```

---

# Summary Panel

Display real-time totals.

Example:

```text
Receipt Summary

Products: 12

Good Items: 580

Damaged Items: 8

Wrong Items: 4

Potential Returns: 12
```

---

# Validation Rules

## Received Quantity

```text
Received Quantity >= 0
```

## Damaged Quantity

```text
Damaged Quantity <= Received Quantity
```

## Wrong Item Quantity

```text
Wrong Item Quantity <= Received Quantity
```

## Good Quantity

```text
Good Quantity >= 0
```

---

# Posting Logic

When user clicks:

```text
Post Receipt
```

The system creates inventory transactions.

Example:

```text
Brake Pad

Good: 95
Damaged: 3
Wrong: 2
```

Transactions:

```text
+95 Available

+3 Damaged

+2 Quarantine
```

---

# Purchase Return Integration

When damaged or wrong items exist:

```text
Damaged > 0
OR
Wrong Item > 0
```

Enable:

```text
[ Post & Create Return ]
```

The system automatically generates a draft Purchase Return document.

Example:

```text
Purchase Return

Brake Pad

Damaged Qty: 3

Wrong Item Qty: 2

Reason:
Generated from Goods Receipt
```

---


# Future Enhancements

* Barcode scanning
* QR code scanning
* Batch tracking
* Serial number tracking
* Product image verification
* Supplier quality scorecards
* Mobile warehouse application
* Multi-warehouse receiving

---

# Recommended Inventory Status Model

```text
Available
Damaged
Quarantine
Reserved
```

Future statuses:

```text
Expired
Repair
Blocked
In Transit
```

This model provides full inventory traceability while keeping the Goods Receipt user experience simple and warehouse-friendly.
