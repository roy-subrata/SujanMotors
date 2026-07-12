# Handoff: Auto Parts POS — Mobile App (Phase 1)

## Overview
Mobile app (Android-first, phone) for **Sunjan Motors**, an auto parts shop POS & inventory system. This is the mobile companion to the existing web POS. Phase 1 covers 15 screens across 4 modules: Login & Products, Customers, Suppliers, and Sales. The architecture is deliberately modular so later web features (purchases, expenses, transfers, reports) slot in as new modules using the same shells.

## About the Design Files
The files in this bundle are **design references created in HTML** — prototypes showing the intended look and behavior, **not production code**. Your task is to **recreate these designs in your target codebase's environment** (Flutter, React Native, Kotlin, etc.) using its established patterns and libraries. If no mobile codebase exists yet, choose the most appropriate framework (Flutter recommended, given the user's context) and implement the designs there.

- `AutoParts Mobile App v2.dc.html` — **primary reference**: all 15 Phase-1 screens (the `<x-dc>` template holds the markup; the trailing script holds the mock data)
- `AutoParts Mobile App.dc.html` — earlier exploration (dashboard, tablet POS register) — useful for future phases
- `BottomNavV2.dc.html` — bottom navigation component
- `android-frame.jsx`, `support.js` — preview scaffolding only; ignore for implementation

Open the `.dc.html` files in a browser to view them.

## Fidelity
**High-fidelity.** Colors, typography, spacing, radii and copy are final intent. Recreate pixel-perfectly using your framework's components.

## Design Tokens

Colors:
- Background: `#f4f5f7` (screens), `#edeef1` (canvas only)
- Surface / cards: `#ffffff`; subtle surface: `#fafafb`
- Border: `#e6e8ec`; hairline dividers: `#eef0f3`; dashed dividers: `#e6e8ec`
- Ink (primary text, primary buttons, active states): `#0f172a`
- Secondary text: `#5b6472`; muted text: `#8a93a2`; disabled/chevrons: `#c3c9d2`
- Success/green: `#0d8a53` on bg `#e9f7f0`
- Danger/red (dues, returns): `#d63841` on bg `#fdeeef`, chip border `#f6d5d7`
- Warning/amber (low stock, payables): `#b26a00` on bg `#fdf3e2`, chip border `#f3e2bd`

Typography — **Instrument Sans** (Google Fonts), weights 400/500/600/700:
- Screen title: 17px / 700
- Detail page title: 16px / 700
- Card heading: 14px / 600
- List item primary: 13.5px / 550
- List item secondary: 11.5px / 400, muted
- Amounts: 600–700, `font-variant-numeric: tabular-nums`
- Section eyebrow: 11px / 600, uppercase, letter-spacing .08em
- Chips: 12px / 500–600
- Status pills: 10.5px / 600
- Big totals: 19–20px / 700; hero amount (dashboard) 28px / 700

Spacing & shape:
- Screen horizontal padding: 16px; vertical rhythm gap: 12px (8px between list cards)
- Card radius: 13–14px; inputs/buttons: 11–12px; chips/pills: 99px (full); FAB: 16px; bottom sheet top radius: 22px
- Card padding: 12–14px; list rows: 12px 14px
- Primary CTA: full-width, 15–16px padding, radius 14px
- FAB: 54×54, bottom-right, 16px inset, above the 64px bottom nav
- Shadows: primary CTA `0 8px 24px rgba(15,23,42,.25)`; green CTA `0 8px 24px rgba(13,138,83,.3)`; FAB `0 8px 24px rgba(15,23,42,.3)`; bottom sheet `0 -12px 40px rgba(15,23,42,.18)`
- Currency: Bangladeshi Taka `৳`, space after symbol (`৳ 1,850`)
- Min tap target 44px

## Recurring Patterns (build these once)

1. **Top app bar (list screens):** title left, 38×38 icon buttons right — 🔔 notification with red count badge, 🛒 cart with dark count badge. Badges: 16px min, radius full, 9.5px/700 white text, offset -5px top/right.
2. **Top app bar (detail screens):** back arrow, title (flex:1), contextual action (edit / status pill / period picker).
3. **Search row:** rounded input (radius 11, white, border) with placeholder `⌕ …`; product screens append a 44px dark **barcode scan** button.
4. **Filter chips row:** horizontally scrollable pills. Active = dark bg + white text. Semantic chips use the red/amber tinted styles (e.g. "Low stock · 6", "With due · 14").
5. **List card:** white card, radius 13, row layout: optional 40–44px thumbnail/avatar → name + meta (muted) → right-aligned amount + status pill.
6. **Status pill:** 10.5px/600, tinted bg + colored text (Paid=green, Due/Return/Out=red, Partial/Low/We-owe=amber).
7. **Sticky bottom CTA:** absolutely positioned over content with a `linear-gradient(transparent, #f4f5f7 30%)` backdrop; content scroll area gets ~110–150px bottom padding.
8. **Bottom nav:** 5 slots — Home ◧, Products ▦, center raised ＋ (New Sale, 48px dark square-round, -22px overlap, white 3px ring), Customers ◔, Sales 🧾. Active = ink color + weight 700; inactive = muted.
9. **Segmented method picker:** grid of equal cells (Cash/Card/bKash/Bank/Cheque/On credit); selected = dark bg white text.
10. **Checkbox rows** (apply payment to invoices, return item selection): 19px radius-6 checkbox, dark when checked.
11. **Statement table:** 4-col grid `1fr 76px 76px 84px` — entry+date, debit (red), credit (green), running balance (600). Header row: uppercase 10.5px/700 muted on `#fafafb`.
12. **Bottom sheet** (reminder): dimmed backdrop `rgba(15,23,42,.35)`, white sheet radius 22 top, drag handle 40×4.

## Screens / Views

### A1 · Login
Centered column, 28px side padding. Logo block (56px radius-14 dark square "SM"), store name + subtitle. Fields: Email/phone, Password (with Show toggle), right-aligned "Forgot password?" link. Primary dark "Sign in" button; secondary outlined "▦ Use PIN instead". Footer: "Store: Main Branch · v2.4" muted, centered.

### A2 · Product list
Top bar with bell + cart. Search + barcode button. Chips: All · 49 (active), Low stock · 6 (red), category chips. List cards: 44px image placeholder, name, `SKU · brand`, price right, stock pill (green "24 in stock" / amber "4 left" / red "Out of stock" or "2 left"). Dark FAB ＋ (add product) above bottom nav. Bottom nav active: Products.

### A3 · Product detail (stock & lots by warehouse)
Back + name + edit. Hero card: 72px image, name, `SKU · brand · category`, selling price + `cost · margin%` muted. 3-stat grid: Total stock / Reserved / Reorder at. **"Stock by warehouse · lots" card**: per warehouse a `#fafafb` header row (🏬 name … qty pcs), then indented lot rows (lot code, `Recv date · cost`, qty) separated by dashed hairlines; "FIFO" annotation top-right. "Recent movement" card: ± icon squares (green in / red out), description, signed qty. Sticky bottom: outlined "⇧ Stock In" + dark "🛒 Add to cart".

### B1 · Customer list
Bell + cart top bar. Search by name/phone. Chips: All · 128, With due · 14 (red), Workshops. Rows: circular initials avatar, name, `phone · N orders`, right: due amount (red if due, green ৳ 0 if clear) + label. FAB add customer. Bottom nav active: Customers.

### B2 · Customer detail
Header card: avatar, name, `phone · type · since`; 3-stat grid — Due balance (red tinted), Lifetime, Orders; 2×2 action grid: **Receive payment** (dark primary), Send reminder, Statement, New sale. Tab chips: Invoices (active) / Payments / Returns. Invoice list card: INV no, `date · items`, amount, status pill, chevron.

### B3 · Invoice detail
Header: back, INV-0982, red "Due ৳ 12,300" pill. Meta card (Customer/Date/Sold by rows). Lines card: name, `price × qty`, line total; totals footer on `#fafafb`: Subtotal, Discount, Paid (green), then dashed rule and **Balance due** in red 19px/700. Payment history card. Sticky bottom: Print ▾ / ↩ Return / dark "৳ Receive payment".

### B4 · Receive customer payment
Customer card with red total due + "Change" link. **Amount received**: large input (2px dark border, 19px/700 value) + quick chips (৳ 5,000 / ৳ 10,000 / Full). Method grid: Cash (selected) / Card / bKash / Bank. **Apply to invoices (oldest first)**: checkbox rows with per-invoice applied amount; footer "Remaining due after payment" red. Optional note field. Sticky green CTA "✓ Confirm payment · ৳ 10,000".

### B5 · Send reminder (bottom sheet)
Over dimmed customer screen. Title + context line (`due · oldest N days`). Send via: SMS (selected) / WhatsApp / Email. Editable message preview on `#f4f5f7` with bolded amount; "☑ Attach statement PDF" checkbox. Dark "Send reminder" CTA.

### B6 · Customer statement
Header: back, title, period picker button ("1 Apr – 5 Jul ▾"). Sub-header: customer name, `Opening X · Closing Y` (closing red if due). Statement table (pattern 11): opening balance row, then sales (debit) and payments (credit) with running balance. Sticky bottom: "🖨 Print" outlined + dark "⇪ Generate PDF & share" (generates statement for the selected period).

### C1 · Supplier list
Back + title + bell. Search. Chips: All · 17, We owe · 5 (amber). Rows: square initials avatar (radius 11), name, `phone · category`, right: payable (amber "we owe" / green clear). FAB add supplier. (Suppliers is reached from Home/More — not a bottom-nav tab.)

### C2 · Supplier payment
Same skeleton as B4 with supplier context: amber "We owe ৳ 68,400"; methods Cash / **Bank (selected)** / Cheque / bKash; **Reference** text field (bank/TRX no); "Against purchase bills" checkbox rows (PO numbers); footer "Remaining payable" amber. Dark CTA "✓ Confirm payment · ৳ 50,000".

### C3 · Supplier statement
Same as B6 with columns Purchase (amber) / Paid (green) / Balance, period picker "Q2 2026 ▾", closing shown as "payable".

### D1 · Sale list
Bell + cart bar. Search + month picker. Chips: All · 214, Paid, Due · 9 (red), Returns. Grouped by day with eyebrow headers including day total ("Today · ৳ 48,250"). Rows: INV no, `customer · items · time`, amount, status pill (Paid/Due/Partial/Return; returns show negative amount). Bottom nav active: Sales.

### D2 · Sale cart & checkout
Header: back, Cart, `INV-1025 · draft`. Lines card: name, `unit · price`, qty stepper (− qty ＋), line total; footer row "＋ Add more items … scan ▦". Customer card (attached customer, due, Change link). Totals card: Subtotal, editable Discount chip, dashed rule, Total 20px/700. Payment method grid incl. **On credit**; amount-tendered input with computed green "Change ৳ 850". Sticky: green "✓ Complete Sale · ৳ 4,150" + secondary "Hold sale" / "Print · 80mm ▾".

### D3 · Sale return
Header: back, title, `from INV-1022`. Source invoice card with Change link. "Select items to return": checkbox rows with `sold qty × price` and return-qty stepper (return qty ≤ sold qty). Reason chips: Wrong part (active) / Defective / Customer changed mind. Summary card: Return value, **Restock to** warehouse selector, **Refund method** selector (Adjust against due / Cash refund), dashed rule, Refund total red 19px/700. Sticky red CTA "↩ Confirm return · ৳ 3,700".

## Interactions & Behavior
- **Navigation:** bottom tabs = root stacks (Home, Products, Customers, Sales); center ＋ opens New Sale flow; detail/action screens push onto the stack with back arrows. Suppliers lives under Home/More.
- **Search:** debounced live filtering on lists (name / SKU / phone / invoice no).
- **Barcode:** scan button opens camera scanner; a match opens product detail (from Products) or adds the item to cart (from Sale flow); no match → "Product not found" with Add-product shortcut.
- **Notifications (🔔):** low stock alerts, due reminders; badge = unread count. **Cart (🛒):** badge = current draft cart line count; tap → D2.
- **Steppers:** − disabled at 0/removes line after confirm; ＋ capped at available stock (sale) or sold qty (return).
- **Payments:** quick-amount chips prefill; applying to invoices auto-allocates oldest-first, editable per row; confirm shows success state then returns to detail with refreshed balances.
- **Statements:** period picker (presets: This month, Last month, Quarter, Custom range); Generate PDF renders the table to a shareable PDF (system share sheet).
- **Reminder:** sends via chosen channel; log the event in customer timeline.
- **Sticky CTAs:** always visible above keyboard/safe-area; content scrolls behind gradient.
- **Hover/press states:** cards darken border to `#8a93a2`; buttons drop to ~92% opacity; list rows tint `#fafafb`.
- **Loading:** skeleton cards matching list-card geometry. **Empty states:** icon + one-line hint + primary action.
- **Errors:** inline field errors in `#d63841`; payment > due warns before allowing overpayment credit.

## State Management
- `auth`: session/user, store branch
- `products`: list w/ filters + pagination; `productDetail`: stock per warehouse → lots (lot code, received date, cost, qty), movements
- `cart` (persistent draft): lines {productId, qty, price}, customer, discount, payment method, tendered amount
- `customers` / `customerDetail`: profile, balances, invoices, payments, returns; `payment` flow state (amount, method, allocations)
- `suppliers`: mirror of customers with payables + POs
- `sales`: list w/ date grouping + filters; `saleReturn`: source invoice, selected lines, reason, restock warehouse, refund method
- `notifications`: unread count + feed
- Data fetching: REST/GraphQL against the existing web app's backend; lists paginated; balances re-fetched after any payment/sale/return mutation.

## Assets
- Font: [Instrument Sans](https://fonts.google.com/specimen/Instrument+Sans) (Google Fonts, OFL)
- Icons in the mocks are unicode/emoji placeholders — replace with your icon set (Material Symbols or Lucide recommended): search, barcode-scan, bell, cart, chevron, edit, printer, share, warehouse, plus
- Product images: gray striped placeholders — real product photos come from the backend
- Logo "SM" placeholder — replace with the shop's real logo

## Files
- `AutoParts Mobile App v2.dc.html` — all Phase-1 screens (primary)
- `BottomNavV2.dc.html` — bottom nav component
- `AutoParts Mobile App.dc.html` — earlier exploration incl. tablet POS register + dashboard
- `BottomNav.dc.html` — v1 bottom nav (used by the earlier file)
- `android-frame.jsx`, `support.js` — preview-only scaffolding
