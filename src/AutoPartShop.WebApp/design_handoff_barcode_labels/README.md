# Handoff: Barcode & Location Label Templates — Sujan Motors

## Overview
Three thermal-print label templates for an auto-parts POS/warehouse system: a compact product label, a larger product label with QR code, and a warehouse bin/rack/aisle location label. All printed at true physical size for thermal/label printers.

## About the Design Files
This is a **design reference built in HTML** (a prototyping format), not production code to copy directly. Recreate these designs in the target codebase's existing label/print pipeline (e.g. a label-printing library, ZPL/EPL generator, or HTML-to-PDF at fixed physical size) using its established patterns — or, if none exists, implement via a headless-Chrome print pipeline sized to the physical dimensions below.

## Fidelity
**High-fidelity.** Colors, typography, spacing, and layout are final and should be recreated precisely. Note: the "barcode" and "QR code" graphics in this prototype are **decorative placeholders** (procedurally generated bar widths / pseudo-random modules seeded from the code string) — not scannable. Production must render real Code128/QR using an actual barcode library, encoding the same code values.

## Global Design System
- Ink / text: `#1c1917`; muted: `#57534e`, `#78716c`; hairline: `#e7e5e4`, `#d6d3d1`
- Accent (brand): `#B0392E` (configurable; alt teal `#0F766E`, blue `#1E40AF`, slate `#3F3F46`) — used for the zone tab on the location label
- Fonts: **IBM Plex Sans** (400–700) for text, **IBM Plex Mono** (400–700) for all codes, prices, and part numbers
- No border-radius; square corners throughout (label/print aesthetic)
- Hairline dividers: `0.3–0.35mm solid #1c1917` between header/price/barcode zones
- Barcode bars: solid black (`#1c1917`) bars of varying width on white, no rounding, no gaps between bars (contiguous flex-based widths)
- QR modules: black/white grid with true QR finder-pattern corners (7×7 squares, top-left/top-right/bottom-left) for visual authenticity, body cells pseudo-random

## Label Specs

### 1. Product Label — Compact (50mm × 25mm)
Physical thermal label size, no margin. Vertical stack, edge padding 2mm/2.4mm:
- Top row: store name (6.5px bold, letter-spacing 1.2px) left, price in ৳ (12px bold monospace) right, separated by hairline
- Part/product name: 8.5px semibold, max 2 lines (clamped)
- Barcode: 8mm tall bar field, full width, black bars on white
- Code string below barcode: 7px monospace, letter-spacing 2px, centered
- Fields: store name, product name, price, part code

### 2. Product Label — With QR (70mm × 40mm)
No margin, 3mm padding:
- Header row (below hairline): store name + product name (11px bold, 2-line) + SKU/part code (monospace) on the left; a 15mm×15mm QR module grid on the right (bordered)
- Middle row: barcode field (11mm tall, flexes to fill width) + price (17px bold monospace) right-aligned
- Code string centered below, 8px monospace, letter-spacing 2.5px
- Fields: store name, product name, SKU, price, part code (encoded in both barcode + QR)

### 3. Warehouse Location Label (100mm × 50mm)
No margin. Left: 10mm-wide solid accent-colored vertical "ZONE {letter}" tab, white bold text rotated vertical. Right content area (padding 4mm/5mm):
- Header row: "SUJAN MOTORS · WAREHOUSE" (8.5px bold) + category name (e.g. "BRAKES & SUSPENSION") right-aligned, hairline below
- Middle row: large Aisle-Rack-Bin code (32px bold monospace, e.g. "04-B-12") left, with small uppercase caption "Aisle · Rack · Bin" above it; 22mm×22mm QR grid right, bordered
- Bottom (hairline above): barcode field (10mm tall) + code string below (9px monospace, letter-spacing 3.5px, centered)
- Fields: zone letter + zone accent color, category, aisle, rack, bin, full location code (encoded in barcode + QR)

## Interactions & Behavior
- Left sidebar lists templates under PRODUCT LABELS / WAREHOUSE LOCATION; clicking swaps the displayed label (single-state switch).
- Each label preview is scaled up on screen (2–2.5x, top-aligned) purely for visibility; the scale is reset to true 1:1 physical size at print via print-only CSS, so printed output matches the stated mm dimensions exactly.
- "Print / Save PDF" triggers the browser print dialog for the currently shown label.

## State Management
Single state value: selected label key (`small`, `medium`, `loc`). All label content is derived from small hard-coded sample records (one per label). Barcode bars and QR modules are generated deterministically from the code string (seeded PRNG) purely for consistent-looking placeholder art — production should replace this generation with a real barcode/QR encoding library fed the same code value.

## Assets
No external images. Store name is set in text, not a logo image — swap in the real Sujan Motors mark in production if desired.

## Files in This Bundle
- `Auto Parts Barcode Labels.dc.html` — main file, all 3 label templates behind the sidebar switcher
- `doc-page.js` — fixed-physical-size print shell (handles mm-accurate sizing/margins and print scale-reset; recreate its effect — true-size printing — in your target stack rather than porting as-is)

Open `Auto Parts Barcode Labels.dc.html` directly in a browser to view/print all three labels.
