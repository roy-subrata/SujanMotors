# Handoff: POS Document Templates — Sujan Motors

## Overview
A set of 13 printable business documents for a point-of-sale system for an auto-parts retailer (Sujan Motors, Dhaka, Bangladesh). Covers the full sales/purchase/reporting lifecycle: quotation through invoice through payment, plus purchasing and back-office reports. All documents share one visual system so they read as a single product.

## About the Design Files
The files in this bundle are **design references built in HTML** (a prototyping format used during design) — they demonstrate exact layout, typography, spacing, and content structure, not production code to copy directly. The task is to **recreate these HTML designs in the target codebase's existing environment** (e.g., a PDF-generation library, a React print view, a templating engine) using its established patterns — or, if no such environment exists yet, choose the most appropriate approach (e.g., a headless-Chrome/HTML-to-PDF pipeline, or a native PDF library like pdfmake/ReportLab) and implement the designs there.

## Fidelity
**High-fidelity.** Colors, typography, spacing, and content layout shown are final and should be recreated precisely.

## Global Design System

### Colors
- Ink / primary text: `#1c1917`
- Secondary text: `#44403c`
- Muted / label text: `#57534e` and `#78716c`
- Hairline borders: `#e7e5e4`
- Divider (medium): `#d6d3d1`
- Accent (brand red, used for title text, logo mark, price/reference highlights): `#B0392E` — configurable; also offered as teal `#0F766E`, blue `#1E40AF`, slate `#3F3F46`
- Page background (screen only, not print): `#dedcd8`
- White: `#ffffff`

### Typography
- Body/UI font: **IBM Plex Sans** (400/500/600/700)
- Numeric/code/monospace font: **IBM Plex Mono** (400/500/600) — used for all figures, dates, document numbers, part codes, amounts
- Google Fonts import: `IBM+Plex+Sans:wght@400;500;600;700` and `IBM+Plex+Mono:wght@400;500;600`
- Document title (e.g. "TAX INVOICE"): 23px, weight 700, letter-spacing 3px, uppercase, accent color
- Section labels (e.g. "BILL TO", "TERMS"): 9px, weight 600, letter-spacing 1.2px, uppercase, color `#78716c`
- Body copy: 10–11px, line-height 1.6–1.7
- Table headers: 9px, weight 600, letter-spacing 1.2px, uppercase, `#44403c`
- Table cells: 11px, monospace for all numeric columns

### Page Setup
- Paper size: **A4**, margin **0.55in**
- Each document is a single continuous printable sheet (no manual page-break styling needed — printing paginates automatically)

## Shared Components

### 1. Document Header (`DocHeader.dc.html`)
Used at the top of every full-page document.
- Layout: flex row, space-between, bottom border 3px solid `#1c1917`, 14px padding-bottom
- Left: 46×46px accent-colored square logo mark with initials "SM" (white, bold, 19px) + company block:
  - "SUJAN MOTORS" — 19px bold, letter-spacing 2px
  - Tagline "Genuine Auto Parts & Accessories" — 10px, uppercase, accent color
  - Address block — 9.5px, `#57534e`, line-height 1.55: address, phone, email, BIN/VAT registration numbers
- Right: document title (right-aligned, uppercase, accent color) + a 2-column meta grid (label/value pairs, right-justified) showing document No., Date, and up to 2 extra fields (e.g. "Payment Due", "Ref. Order") — labels `#78716c`, values monospace
- Props: `title`, `num`, `date`, `extraLabel`/`extraVal`, `extra2Label`/`extra2Val`, `accent`

### 2. Line-Items Table (`ItemsTable.dc.html`)
Used on all documents with itemized goods.
- Table columns: `#` | Part No | Description | Qty | Rate (৳) | Amount (৳) — header row uppercase 9px with 2px bottom border, body rows 11px with 1px `#e7e5e4` row dividers
- Totals block: right-aligned 300px-wide stack of label/value rows (subtotal, discount, VAT), then a bold grand-total row bordered top+bottom 2px solid ink
- "In words" line below totals for the amount spelled out (Bangladeshi numbering: Crore/Lakh/Thousand)
- Props: `items[]` (sn, code, name, qty, rate, amount), `totals[]` (label/value pairs), `grandLabel`, `grandValue`, `words`

### 3. Signature Row (`SignRow.dc.html`)
Three equal-width columns, each a 1px top border with a centered uppercase caption below it (e.g. "Prepared By", "Checked By", "Authorized Signatory"). Sits 64px below the last content block on every document.

## Documents (13 total)

Each is a full A4 page built from Header + (custom body) + ItemsTable (where applicable) + SignRow. All share one live "brand accent" color and one VAT-rate value (default 15%) driven from a single source of truth.

1. **Quotation** — customer-facing price quote. Header extra field: "Valid Until". Body: "Quotation To" block, items table (label "Total"), Terms & Conditions (4 numbered lines: price validity, delivery window, advance %, warranty note). Sign row: Prepared/Checked/Authorized.
2. **Sales Order** — internal order confirmation. Header extras: "Customer PO", "Delivery By". Two-column Bill To / Ship To. Items table (label "Order Total"). One-line note referencing Delivery Challan + Tax Invoice to follow. Sign row: Prepared/Customer Confirmation/Authorized.
3. **Proforma Invoice** — pre-payment bill. Header extras: "Valid Until", "Ref. Order". Bill To block. Items table (label "Total Payable"). Two-column footer: Bank Details (bank/branch/account/routing) + Note ("not a tax invoice", advance requirement).
4. **Delivery Challan** — goods-dispatch document, no prices shown. Header extras: "Ref. Order", "Vehicle". Two-column Deliver To / Dispatch Details (driver, dispatch time). Custom items table with columns `#`/Part No/Description/Qty/Unit (no rate/amount) + a totalled Qty footer row. Note referencing the invoice for pricing. Sign row: Prepared/Driver/Received By (Customer).
5. **Tax Invoice** — the legal, final bill. Header extras: "Payment Due", "Ref. Challan". Two-column Bill To / Payment terms (credit days, VAT note, Mushak-6.3 reference). Items table (label "Grand Total"). Two-column footer: Bank Details + Terms (return window, warranty). Sign row: Prepared/Customer Signature/Authorized.
6. **Payment Receipt** — proof of payment. Header extra: "Mode" (e.g. bKash). "Received From" block. A bordered highlight box showing Amount Received in large (26px) monospace accent-colored text. Amount-in-words line. A key/value table: Payment Mode, Transaction Ref, Against Invoice, Invoice Total, Balance Due. Closing note confirming invoice settled.
7. **Credit Note** — returns/refunds document. Header extra: "Ref. Invoice". Two-column Issued To / Reason for Credit. Items table (label "Total Credit") scoped to returned items only. Note on adjustment/refund handling and that Debit Notes use the same layout.
8. **Purchase Order** — supplier order. Header extras: "Delivery By", "Payment" (credit terms). Two-column Supplier / Deliver To. Items table (label "PO Total") sourced at cost price. Numbered Conditions list (genuine parts only, challan+invoice required, replacement window). Sign row: Prepared/Approved/Supplier Acknowledgement.
9. **Stock Report** — inventory snapshot. Header extra: "As Of" timestamp. Full-width table: Part No, Description, Category, On Hand, Reorder level, Value, Status — status rendered as a pill (accent-filled "LOW" vs neutral "OK" when on-hand ≤ reorder level). Bold total-value footer row. Sign row: Store Keeper/Checked/Manager.
10. **Daily Sales (Z) Report** — end-of-day summary. Header extras: "Business Day" hours, "Terminal". A 4-up KPI card row (Gross Sales, Net Sales, VAT Collected, Receipts count) — bordered boxes, small uppercase label + large monospace figure. A gross→net reconciliation table (gross, less returns, less discounts, net). Two-column split: By Payment Method table + By Category table. Closing note with average-sale figure and cross-reference to Shift Reports. Sign row: Head Cashier/Accountant/Manager.
11. **VAT Report** — tax compliance summary. Header extra: "Period" (date range). Table: Description / Taxable Value / VAT — rows for output VAT on sales, less reversed VAT on credit notes, less input VAT on purchases, bold Net VAT Payable total. Note referencing the monthly return filing and supporting registers. Sign row: Accountant/VAT Consultant/Proprietor.
12. **Shift Report** — per-cashier reconciliation. Header extras: "Shift" (letter + hours), "Terminal". Two-column Cashier info / Transaction counts by payment method. Cash Drawer Reconciliation table (opening float, cash sales, refunds, drops to safe, expected vs counted, over/short in accent color). Closing note on discrepancy and cash-drop timestamps. Sign row: Cashier/Head Cashier/Manager.
13. **Thermal Receipt** — checkout slip, shown in two physical widths side by side for comparison:
    - **80mm** (302px @ 1px≈0.264mm design scale): centered store header, dashed divider rules (`1px dashed #999`), line items with qty×rate under each description and right-aligned line total, subtotal/discount/VAT/TOTAL block, cash/change block, item/qty count footer, "THANK YOU!" + no-refund note, a fake barcode bar (CSS repeating-gradient) + code string.
    - **58mm** (219px): same structure, condensed type sizes (8.5–12.5px vs 9.5–15px on 80mm).
    - All in `IBM Plex Mono`, black on white, drop-shadowed card presentation (shadow is screen-only affordance, not part of the print output).

## Interactions & Behavior
- Left sidebar (228px, dark `#1c1917` background) lists all documents grouped under SALES / PURCHASING / REPORTS; clicking a nav row swaps which document is shown in the main pane (single-page-app style state switch, not real navigation/routing).
- Active nav row: dark highlight background `#38342f`, white text, 3px accent-colored left border.
- "Print / Save PDF" button (bottom of sidebar, accent-filled) calls the browser print dialog for whichever document is currently displayed. Print CSS hides the sidebar and lets the doc flow naturally onto A4.
- No other interactivity (no forms, no editable fields) — this is a reference/preview surface for a set of static print outputs, not an editing tool.

## State Management
The prototype keeps one piece of state: which document key is currently selected (`quote`, `so`, `proforma`, `challan`, `invoice`, `receipt`, `credit`, `thermal`, `po`, `stock`, `zreport`, `vat`, `shift`). All document data (line items, totals, report figures) is derived/computed from a small set of hard-coded sample datasets and two adjustable inputs: **VAT rate** (default 15%) and **brand accent color**. In a production system these would instead be populated from real order/inventory/payment records; the thermal-receipt figures in this prototype are static sample values and are the one place NOT wired to the VAT-rate input.

## Design Tokens
- Spacing scale used throughout: 4, 5, 6, 7, 8, 10, 12, 14, 16, 18, 20, 22, 24px
- Border weights: 1px (row dividers, sign-row rule), 1.5–2px (table header rule, grand-total rule), 3px (document header rule)
- No border-radius anywhere in the design — all corners are square (deliberate print/paper aesthetic)
- No box-shadows on the printed documents themselves; only the thermal-receipt card mockups use a shadow (`0 4px 18px rgba(0,0,0,0.18)`) as a screen presentation affordance

## Assets
No external images or icons. The "logo" is a plain colored square with the initials "SM" set in the body font — replace with the real Sujan Motors logo mark in production. The thermal-receipt "barcode" is a decorative CSS gradient, not a scannable barcode — see the companion **Auto Parts Barcode Labels** handoff (if included) for real barcode/QR patterns if this system also needs to print scannable codes.

## Files in This Bundle
- `Sujan Motors POS Documents.dc.html` — main file; renders all 13 documents behind the sidebar switcher described above
- `DocHeader.dc.html` — shared document header component
- `ItemsTable.dc.html` — shared line-items + totals component
- `SignRow.dc.html` — shared 3-column signature block component
- `doc-page.js` — paged-document print shell (handles A4 sizing/margins and print pagination; not something to port as-is — recreate its effect, i.e. correct page size/margins and clean print output, in your target stack)

Open `Sujan Motors POS Documents.dc.html` directly in a browser to view/print all 13 documents.
