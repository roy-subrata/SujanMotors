# Handoff: POS Dashboard Modernization (Sunjan Motors)

## Overview
A modernized redesign of the POS/ERP dashboard for an auto-parts shop. Replaces the colorful icon-chip card style with a clean "modern SaaS" look: slate/near-black accent, bordered white cards with trend deltas, a consolidated 6-stat insight strip, restyled collapsible sidebar, refined line chart, cash-flow panel, and full light/dark theming.

## About the Design Files
The file in this bundle (`POS Dashboard.dc.html`) is a **design reference created in HTML** — a prototype showing intended look and behavior, NOT production code to copy directly. Your task is to **recreate this design in your existing Angular + PrimeNG codebase**, using your established module structure, PrimeNG components, and theming system.

## Fidelity
**High-fidelity.** Colors, typography, spacing, and states are final. Recreate the UI pixel-close using PrimeNG components + custom CSS where PrimeNG defaults differ.

## Target stack notes (Angular + PrimeNG)
- Theme via PrimeNG design tokens (v18+ `definePreset`) or CSS custom properties. The design already uses CSS variables (see Design Tokens) — map them to `--p-*` tokens:
  - `--p-primary-color` → `#0f172a` (light) / `#e2e8f0` (dark)
  - `--p-content-background` → `--surface`, `--p-content-border-color` → `--border`
  - `--p-text-color` → `--text`, `--p-text-muted-color` → `--text2`
- Dark mode: toggle a `data-theme="dark"` attribute (or PrimeNG `.p-dark` class) on `<html>`; all colors are variable-driven.
- Components to use: `p-table` (tables), `p-button` (severity=primary for New Sale, outlined/text for icon buttons), `p-select` (date range), `p-tag` (delta/margin pills), `p-avatar`, PanelMenu or custom nav for sidebar, Chart.js via `p-chart` for the trend chart.
- Font: **Instrument Sans** (Google Fonts), weights 400/500/600/700. Numbers use `font-variant-numeric: tabular-nums`.

## Screens / Views

### App Shell
- Grid: sidebar `248px` + main `1fr`, min-height 100vh.
- **Sidebar** (`--surface`, right border `1px --border`, sticky, full height):
  - Header: 32×32 rounded-8 accent square with "SM" monogram + brand name (14px/600) + subtitle (11px `--text3`). Bottom border `--border2`. Padding 16px.
  - Nav: 10px padding, 2px gap. Group headers: uppercase 10.5px/600, letter-spacing .09em, `--text3`, clickable to collapse (chevron ▾/▸). Items: 13px, padding 7px 10px, radius 7px, icon 16px + label + optional badge pill (10.5px, `--red-bg`/`--red`, radius 99px). Active item (Dashboard): background `--accent`, text `--accent-fg`, weight 600. Hover: `--surface2`.
  - Groups: Catalog (Parts, Categories, Brands, Units, Attribute Groups, Discounts), Inventory (Stock Management, Warehouses), Purchasing (Purchase Orders, Goods Receipts, Purchase Returns, Suppliers, Supplier Payments, Supplier Statements, Daily Expenses, Payment Providers), Sales (Sales Orders, Invoices, Pending Deliveries, Sales Returns, Customers, Customer Payments, Customer Statements), Service & Warranty (Technicians, Vehicles, Warranty Registrations, Warranty Claims [badge "2"]), Finance (Daily Cash Book, Exchange Rates), Administration (Online Store, Settings).
  - Footer: avatar circle 30px + "System Admin" 12.5px/600 + email 11px `--text3`, top border.

- **Top bar** (56px, `--surface`, bottom border, sticky, z-index 5, padding 0 24px, gap 14px):
  - Search field 300px: `--surface2` bg, 1px border, radius 8, 13px `--text3` placeholder "Search parts, invoices, customers…", ⌘K kbd hint.
  - "＋ New Sale" button: `--accent` bg, `--accent-fg` text, radius 8, padding 8px 14px, 13px/600.
  - Theme toggle + notification icon buttons: 34×34, 1px border, radius 8. Notification has 7px red dot top-right.

### Dashboard page (main, padding 24px, max-width 1440px, 24px vertical gap)
1. **Page header**: "Dashboard" 22px/700 tracking -0.01em; subtitle 13px `--text2` "Business performance · Jul 1 – Jul 31, 2026". Right: "This Month ▾" select-style pill, refresh icon button, "↧ Export" outlined button — all 34px tall, radius 8, `--surface` bg.
2. **KPI row** — grid 4 columns, 14px gap. Card: `--surface`, 1px `--border`, radius 12, padding 18px, shadow `0 1px 2px rgba(15,23,42,.05)`, hover border → `--text3`.
   - Row 1: label 12.5px/500 `--text2` + delta pill (11px/600, radius 99, padding 2px 8px).
   - Value: 24px/700, tracking -0.02em, tabular-nums.
   - Sub: 12px `--text3`.
   - Cards: Total Sales ৳19,120.00 (+12.4% green) · Revenue Collected ৳19,140.00 (+8.1% green) · Total Expenses ৳21,250.00 (+31% amber) · Net Profit −৳2,130.00 (value in `--red`, "▼ loss" red pill).
3. **Insight strip** — one card, grid of 6 equal cells divided by 1px `--border2` right borders. Cell: padding 14px 18px; label 11.5px `--text3`; value 16px/650 tabular; sub 11px `--text3`. Cells: Gross Profit −৳2,130.00 (red) · Avg Sale Value ৳4,780.00 · New Customers 1 · Low Stock Items 0 · Customer Due ৳100.00 (amber) · Supplier Due ৳141,450.00.
4. **Chart + Cash Flow row** — grid `1fr 320px`, gap 14px.
   - Chart card: title "Sales & Profit Trend" 15px/600 + sub 12px; legend chips (10×3px rounded bars + label 12px) for Sales `#3b82f6`, Purchases `#f59e0b`, Profit `#10b981`. Chart: line chart, 31 days (Jul 1–31), y −5k…20k gridlines, 2px lines, subtle area fills (sales 9%, purchases 7% opacity), dots on data days. Use Chart.js (`p-chart type="line"`).
   - Cash Flow card: title 15px/600; three rows (label 12.5px `--text2` left, value 14px/600 tabular right) in `--surface2` boxes radius 9, 1px `--border2`: Opening ৳0.00 · Cash Inflow +৳19,140.00 (green) · Cash Outflow −৳0.00. Footer bar pinned bottom: `--accent` bg, `--accent-fg`, radius 9, "Closing Balance / ৳ 19,140.00" 17px/700.
5. **Tables row** — grid `repeat(auto-fit, minmax(480px, 1fr))`, gap 14px (stacks below ~1000px).
   - Card header: title 15px/600 + "View all →" link 12.5px `--text2`.
   - Column header row: `--surface2` bg, top/bottom `--border2` borders, uppercase 10.5px/600 letter-spacing .07em `--text3`.
   - Top Products columns: Product(1fr) Qty(56) Revenue(110) Profit(100) Margin(80), right-aligned numerics. Row: name 13px/550 + SKU 11px `--text3`; margin as pill (green bg for positive, neutral for 0%). Rows: Navana Batter 21 Plate SKU001 / 1 / ৳18,500.00 / ৳0.00 / 0.0%; Looking Glass 10 INCH SKU002 / 3 / ৳620.00 / +৳200.00 (green) / 32.3% (green pill).
   - Top Customers columns: Customer(1fr) Orders(60) Revenue(110) Last(90). Row: 28px initials avatar + name + phone. Rows: Bhadhan Shaha +880 1716 625369 / 3 / ৳18,900.00 / Jul 2; Walk In 000000 / 1 / ৳220.00 / Jul 2.
   - Row: padding 12px 18px, bottom border `--border2`, hover `--surface2`.

## Interactions & Behavior
- Sidebar group headers toggle collapse (chevron flips).
- Nav items + table rows: hover `--surface2` background.
- KPI cards: hover border-color → `--text3`.
- Theme toggle (☾/☀): flips `data-theme` on `<html>`; every color is a CSS variable so the whole UI restyles. Persist preference (localStorage/user setting).
- Buttons: hover slight opacity/surface change. All transitions can be `120ms ease`.
- Date-range select drives all dashboard data (wire to existing API).
- Responsive: tables row collapses to 1 column below ~1000px container width.

## State Management
- `theme: 'light' | 'dark'`
- `openGroups: Record<string, boolean>` for sidebar collapse
- `dateRange` filter → dashboard query
- Dashboard data: kpis, insights, dailySeries (sales/purchases/profit), cashflow, topProducts, topCustomers — from your existing endpoints.

## Design Tokens
Light:
- `--bg #f4f5f7` · `--surface #ffffff` · `--surface2 #fafafb`
- `--border #e6e8ec` · `--border2 #eef0f3`
- `--text #0f172a` · `--text2 #5b6472` · `--text3 #8a93a2`
- `--accent #0f172a` · `--accent-fg #ffffff`
- `--green #0d8a53` / bg `#e9f7f0` · `--red #d63841` / bg `#fdeeef` · `--amber #b26a00` / bg `#fdf3e2`
- shadow `0 1px 2px rgba(15,23,42,.05)`

Dark (`data-theme="dark"`):
- `--bg #0d1017` · `--surface #151922` · `--surface2 #1a1f2a`
- `--border #252b38` · `--border2 #1f2530`
- `--text #eef1f6` · `--text2 #9aa4b5` · `--text3 #68738a`
- `--accent #e2e8f0` · `--accent-fg #0f172a`
- `--green #34d399` / bg `#0e2b20` · `--red #f8717a` / bg `#331418` · `--amber #fbbf24` / bg `#2e2410`

Chart: sales `#3b82f6`, purchases `#f59e0b`, profit `#10b981` (same both themes).

Scale: radii 7/8/9/12px (nav/buttons/inner boxes/cards); pills radius 99. Spacing: 14px card gaps, 24px page padding/section gap, 18px card padding. Type: Instrument Sans; sizes 10.5 (table headers) / 11–12 (meta) / 13 (body) / 15 (card titles) / 22 (page title) / 24 (KPI values).

## Assets
No image assets. Icons in the prototype are Unicode placeholders — use **PrimeIcons** (`pi pi-*`) in production: e.g. pi-home, pi-box, pi-tags, pi-truck, pi-users, pi-wallet, pi-chart-line, pi-cog, pi-search, pi-bell, pi-moon/pi-sun, pi-plus, pi-refresh, pi-download.

## Files
- `POS Dashboard.dc.html` — the full interactive design reference (open in a browser; template markup + logic are inside the file).
