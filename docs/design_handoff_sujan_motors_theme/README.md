# Handoff: Sujan Motors — Modern ERP Theme

## Overview
A modernized theme and navigation shell for the SujanMotors Angular/PrimeNG auto-parts ERP (github.com/roy-subrata/SujanMotors), replacing the current UI with a cleaner, denser, bilingual (English/Bangla) design. Covers the full app shell, dashboard, and every module screen (catalog, inventory, purchasing, sales, POS/quick sale, till sessions, service/warranty, finance, HR, admin).

## About the Design Files
The bundled file (`Sujan-Motors-Theme.dc.html`) is a **design reference built in HTML** — a clickable prototype showing intended layout, styling, states, and interactions. It is **not production code to copy verbatim**. The task is to recreate this design inside SujanMotors' existing Angular + PrimeNG environment, using PrimeNG components, Angular routing, and the app's existing services/state — not to port raw HTML/inline-styles into the app.

Open the HTML file in a browser to click through every screen. All interactivity shown (sidebar nav, filters, dialogs, tabs, the quick-sale flow) is functional in the prototype — use it as the interaction spec, not just a static image.

## Fidelity
**High-fidelity.** Colors, spacing, typography, and component states are final. Implement pixel-close using PrimeNG's theming system (see Design Tokens below) rather than the prototype's inline styles.

## ⚠️ Implementation approach: build in phases, not one shot
Do not attempt to implement this entire theme in a single pass. Work through the phases below in order, and treat each as a separate PR/session with its own review. Each phase should leave the app in a working, deployable state — don't let the whole app be broken between phases.

### Phase 1 — Design tokens & PrimeNG theme
Set up the color/typography/spacing tokens (see Design Tokens section) as a PrimeNG preset/theme override. Do not touch any screens yet. Verify buttons, inputs, and tables from PrimeNG's demo pages pick up the new theme correctly before moving on.

### Phase 2 — App shell (sidebar + header)
Replace the existing nav with the single flat dark sidebar (see "App Shell" below): logo, jump-to-search, one scrolling list of all routes grouped under plain section captions, till-total + user footer. Add the header (hamburger toggle, global search, language switcher, Quick Sale button, notifications bell). Wire the hamburger to collapse/expand the sidebar. Existing routes can keep their current page content for now — only the shell changes in this phase.

### Phase 3 — Shared list-page pattern
Build ONE reusable list-page component (stat strip + inline filter pills + "More filters" dialog + data table + pagination) per the "List Pattern" section below. Migrate 2-3 existing list screens (e.g., Parts, Invoices) to use it as a proof of concept before rolling out to all ~32 list screens.

### Phase 4 — Roll out list pattern to remaining screens
Apply the Phase 3 component to the rest of the list screens (categories, brands, stock, purchase orders, goods receipts, suppliers, customers, warranty claims, employees, etc. — full list in "Screens" below). Each screen only needs its column config and data source; layout is shared.

### Phase 5 — Dashboard
Build the dashboard screen (KPI cards, sales chart, needs-attention queue, low-stock table, top-brands panel) per spec. This depends on real data endpoints — coordinate with backend if aggregates don't already exist.

### Phase 6 — Quick Sale (POS)
Build the Quick Sale screen: barcode/search field, category chips + product tile grid, line-item cart, totals, payment tiles, Hold/Charge, the customer-assignment dialog (search customers, view balance/vehicles/credit position), and the post-charge receipt dialog. This is the highest-interaction screen — budget the most time here and test with a real barcode scanner if available.

### Phase 7 — Till Session, part detail, reports, forms, remaining custom screens
Till Session (session history + cash reconciliation + cash drops), Part Detail (tabbed: overview/stock/pricing/suppliers/movement/warranty), Reports, the tabbed form pattern (Company Profile, Settings), Notifications feed, Customer Statement, and the Shortcuts reference page.

### Phase 8 — Bilingual support (EN/বাংলা)
Wire the language switcher to real i18n (e.g., ngx-translate or Angular i18n) using the string list in "Localization" below as the translation key set. Do this last so translation keys are added against finished, stable markup rather than mid-refactor.

---

## App Shell
**Sidebar** (single layer, ~234px, dark `#14161d` background, collapsible via header hamburger to 0px width with a CSS transition):
- Header: 30×30px logo mark (orange-to-red gradient, rounded 8px, bold "S"), company name (bold 13.5px white) + subtitle (uppercase 9.5px gray)
- "Jump to a screen" search field (dark input style)
- One scrolling nav list: plain uppercase section captions (9.5px, letter-spacing 0.09em, color `#565d69`, NOT clickable/expandable) followed by flat nav items — icon (PrimeIcons) + label + optional count badge. Active item: orange-tinted background `rgba(249,115,22,0.18)`, white text, orange icon `#fb923c`. Inactive: gray text `#9aa1ad`, hover background `rgba(255,255,255,0.07)`.
- Footer: till total (bold white, green session count) + user row (avatar circle, name, role, sign-out icon)

Route groups (in order): Overview (Dashboard, Reports, Notifications) · Catalog (Parts, Categories, Brands, Units, Attribute groups, Discounts) · Inventory (Stock, Stock take, Warehouses, Locations) · Purchasing (Purchase orders, Goods receipts, Purchase returns, Suppliers, Supplier payments, Daily expenses) · Sales (Quick sale, Quotations, Sales orders, Invoices, Pending deliveries, Sales returns, Customers, Customer payments, Till sessions, Customer statements) · Service (Technicians, Vehicles, Warranty registrations, Warranty claims) · Finance (Reports, Daily cash book, Exchange rates) · HR (Employees, Attendance, Shifts, Leave requests, Payroll) · Admin (Company profile, Users & roles, Settings, Keyboard shortcuts, Audit trail)

**Header** (52px, white, bottom border `#e7e9ee`):
- Hamburger icon button (32×32, toggles sidebar)
- Global search field (300px, gray background `#f4f5f7`, ⌘K hint)
- Language switcher: two-tab pill (EN / বাংলা), active tab dark `#14161d` background white text
- "Quick Sale" button: orange `#ea580c` background, white bold text, bolt icon, opens Quick Sale screen
- Notification bell with red dot badge

## List Pattern (used by ~32 screens)
- Page header: breadcrumb (11px gray) + title (19px, weight 800) + right-aligned Export / New buttons
- Stat strip: 4-up grid, thin `#e7e9ee` gutters, each cell: label (11px gray) + big number (17-20px, weight 800) + small caption
- Filter row: search field (300px) + status pills inline (rendered from the page's status options, active pill orange-tinted background/border/text) + "More filters" text-button (opens dialog, not a drawer) + row count on the right
- "More filters" dialog: centered modal, dark overlay, header + full checkbox list of the same status options + footer with Cancel and "Show N" (applies and closes)
- Data table: uppercase 9.5px column headers on light gray `#fafbfc` background, rows 8px vertical padding, hover background `#fafbfc`, status values render as pill badges (colors below), monospace font for SKU/ID columns
- Pagination footer: row count + page-number chips

## Quick Sale Screen
- Left column: barcode/search field (orange-focused border), category pill row, product tile grid (SKU + name + price + stock, flex-fills remaining vertical space, scrolls internally)
- Right column: cart panel — header ("Current sale" / assigned customer name, Assign/Change button with person icon) → customer chip row (shows when assigned: name, phone·terms, vehicle reg, Remove link) → line items (qty +/− stepper, rate, line total) → totals (Subtotal/Discount/VAT) → Total → 4-up payment-method tiles → Hold + Charge buttons → "Keyboard shortcuts" link at the bottom
- Customer dialog: left = searchable customer list (name, phone, terms, vehicle count, balance, over-limit flag pill); right = selected customer detail (account position: credit limit/outstanding/available/oldest invoice/last payment, over-limit warning banner, vehicle list with registration/model/year/warranty pill); footer = "Continue as walk-in" / "Use this customer"
- Receipt dialog (after Charge): success icon, invoice number, sold-to name, payable/tendered/change, Print again / Email receipt / New sale

## Till Session Screen
- Left: session history list (label + OPEN/CLOSED status pill, selectable rows)
- Right: cashier/opened/opening-float summary row, cash reconciliation panel (Opening Float, Cash Sales, Cash Refunds, Cash Drops, Expected in Drawer), cash drops list (empty state: "No cash drops recorded for this session.")
- Header actions: "Record Cash Drop", "Close Till"

## Design Tokens
**Colors**
- Primary/accent: `#ea580c` (orange-600), hover `#c2410c`, tint background `#fff7ed`, tint text `#c2410c`, tint border `#fed7aa`
- Sidebar dark: `#14161d`; sidebar muted text `#9aa1ad`; sidebar hover `rgba(255,255,255,0.07)`
- Page background: `#f4f5f7`; card background `#fff`; card border `#e7e9ee`; divider `#f1f3f6` / `#eef0f3`
- Text: primary `#1c1f26`, secondary `#6b7280`, muted `#9ca3af`
- Status: success bg `#f0fdf4` / fg `#15803d`; warning bg `#fefce8` / fg `#a16207`; danger bg `#fef2f2` / fg `#dc2626`; info bg `#eff6ff` / fg `#1d4ed8`; neutral bg `#f4f5f7` / fg `#6b7280`

**Typography**
- Font: Public Sans (Latin) + Noto Sans Bengali (Bangla), weights 400/500/600/700/800
- Monospace (SKU/ID/codes): JetBrains Mono, weight 500/600
- Scale: page title 19-20px/800, section title 13.5px/700, body 12-12.5px/500-600, caption 10-11.5px/600, table header 9.5px/700 uppercase letter-spacing 0.06em

**Spacing / shape**
- Card radius 12px, pill/badge radius 20px, small control radius 7-9px
- Card padding 15-17px, table row padding 8px vertical / 16-17px horizontal
- Grid gaps 12px (cards), 6-8px (pills/chips)

**Currency/locale**: BDT with ৳ symbol, Bangla numerals in Bangla mode, dates as "DD MMM YYYY"

## Localization
Full bilingual coverage (English / বাংলা) is a requirement, not decoration — every label, status pill, column header, button, and dialog string in the prototype has a Bangla counterpart. Numeric data (part names, customer names, SKUs, amounts) stays as entered by the user and is not translated. Use the prototype's language toggle to see every string pair before setting up translation keys.

## Files
- `Sujan-Motors-Theme.dc.html` — full interactive prototype (open in any browser). Click the sidebar to move between all ~44 screens; use the language toggle top-right of the header; the Quick Sale and Till Session flows are fully clickable end to end.
