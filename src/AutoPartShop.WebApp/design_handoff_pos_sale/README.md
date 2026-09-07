# Handoff: POS Quick Sale Terminal

## Overview

A single-screen point-of-sale terminal for a cashier serving a customer at a counter. The cashier builds a ticket from a product grid, optionally attaches a customer profile and a discount, takes payment across one or more tender types, and issues a receipt or tax invoice.

The design is deliberately **category-agnostic**. The same screen serves grocery, auto parts, pharmacy, restaurant and general retail; what differs between businesses is *configuration* (catalog, tax rule, context line, secondary action labels, receipt policy text), not layout. The prototype ships five such configurations so all variants can be inspected, but a real terminal is provisioned for exactly one.

## About the Design Files

The file in this bundle (`POS Sale.dc.html`) is a **design reference created in HTML** — a working prototype that demonstrates the intended look, layout and behavior. It is **not production code to copy directly**.

The task is to **recreate this design in the target codebase's existing environment** — React, Vue, SwiftUI, Android, whatever is already in use — following that codebase's established component library, styling approach, state management and routing conventions. If no environment exists yet, choose the framework most appropriate for the project and implement the design there.

The prototype uses a small in-house template runtime. Do not port that runtime. Read it for structure, values and behavior; rebuild in your own stack.

## Fidelity

**High fidelity.** Colors, typography, spacing, sizing and interaction states are final and specified exactly below. Recreate the UI to match, using the codebase's existing primitives where they map cleanly (buttons, dialogs, inputs) and matching the specified values where they do not.

**The layout is fluid.** It is designed for a landscape tablet or desktop register (~1280–1600 wide) but fills whatever viewport it is given and reflows down to roughly 380px. Two layout modes:

| Mode | Trigger | Effect |
|---|---|---|
| Side-by-side | viewport width ≥ 780px | Catalog and cart panel are columns in a row |
| Stacked | viewport width < 780px | Cart panel sits beneath the catalog, capped at 62% height |
| Compact footer | viewport height < 660px | Cart footer tightens so Charge always fits (see 1c) |

The modes are driven by two booleans in component state — `narrow` (width < 780) and `short` (height < 660) — set on mount and on `window.resize`. They are real state, not CSS media queries, because the prototype's styling is inline-only; in your implementation use media queries or container queries instead if the framework supports it.

**The scroll model is load-bearing and must survive any re-layout.** The root is `100%` wide, `min-height: 100vh`, `max-height: 100vh`, `overflow: hidden`. The body row is `flex: 1 1 auto; min-height: 0; overflow: hidden` — it never scrolls itself. Its two children also carry `min-height: 0`, and the cart panel is `overflow: hidden`, which is what makes the inner panes the actual scroll containers: the product grid scrolls inside the catalog column, and the line-item list is the *only* scrolling child of the cart. The customer bar, ticket header, totals block and Charge button are all `flex: 0 0 auto`, so they stay pinned at every viewport size.

This is why the compact footer mode exists rather than a scrolling cart: at a 540px-tall viewport the cart's fixed chrome intrinsically exceeded the available height and clipped the Charge button. **If a re-layout lets the page scroll as a whole, or lets the footer grow past the row, the Charge button leaves the fold and the design has failed.** Verify at 1440×900, 1280×800, 924×540 and 420×740 before calling it done.

---

## Screens / Views

### 1. Sale screen (root, always visible)

**Purpose:** The cashier's home. Build the ticket, attach customer and discounts, reach every other function.

**Layout:** Vertical flex, `width: 100%`, `min-height: 100vh`, `max-height: 100vh`, `overflow: hidden`, `position: relative` (overlays anchor to it).

| Band | Height | Notes |
|---|---|---|
| Header | `min-height: 66px`, grows when wrapped | Store identity, search, operator |
| Body | fills (`flex: 1 1 auto; min-height: 0; overflow: hidden`) | Catalog + cart; row when wide, column when stacked |
| Shortcut bar | 56px fixed | Dark F-key strip, scrolls horizontally |

Background `#F7F5F0`, text `#17171A`.

---

#### 1a. Header (66px)

Horizontal flex, `flex-wrap: wrap`, `gap: 13px`, `padding: 10px 20px`, background `#FFFFFF`, `border-bottom: 1px solid #E2DED4`. Wrapping to two lines on a narrow viewport is expected and fine.

Children left to right:

1. **Logo mark** — 40 × 40, `border-radius: 12px`, background `#1F6F4A`, white `P`, 17px/700, centered.
2. **Store identity** — two lines, `line-height: 1.25`. Name 16px/700, `letter-spacing: -0.01em`. Sub-line "Point of sale" 12px, `#6E6A62`.
3. **Spacer** — `flex: 1`.
4. **Search trigger** — 44px tall, `flex: 1 1 200px`, `max-width: 340px`, `min-width: 150px`, background `#F7F5F0`, `1px solid #E2DED4`, `border-radius: 11px`, `padding: 0 15px`, flex row, `gap: 10px`, cursor pointer. Contains: a 14px circle outline (`2px solid #6E6A62`, 50% radius) standing in for a magnifier icon — replace with a real icon; placeholder text 14px `#6E6A62`, ellipsized; and an `F2` key cap (11px mono, `1px solid #DDD8CD`, `border-radius: 6px`, `padding: 2px 6px`). Hover: `border-color: #17171A`. Click opens the Product search dialog. **Not a live input** — it is a button that opens the dialog.
5. **Divider** — 1 × 32, `#E2DED4`.
6. **Operator block** — right-aligned, `line-height: 1.3`. Line 1 "Register 1 · Ruth K." 14px/600. Line 2 the context line, 12px mono `#6E6A62`.

**Context line** is per-configuration and is the single most important vertical-specific affordance. Examples: `Lane open · drawer $220.00` (grocery), `Vehicle: 2018 Camry LE 2.5L` (auto parts), `Patient: M. Ortiz · insurance verified` (pharmacy), `Table 12 · 2 guests · dine in` (restaurant), `Walk-in customer · no loyalty` (retail).

---

#### 1b. Catalog column

Vertical flex, `min-width: 0`, `min-height: 0`. Side-by-side: `flex: 1 1 420px`. Stacked: `flex: 1 1 auto`.

**Category chips row** — `padding: 15px 20px 3px`, flex wrap, `gap: 8px`.

Each chip: height 40, `padding: 0 17px`, `border-radius: 20px`, 14px/600, `white-space: nowrap`, cursor pointer.
- Idle: background `#FFFFFF`, `1px solid #E2DED4`, text `#3C3A36`.
- Selected: background `#17171A`, `1px solid #17171A`, text `#FFFFFF`.

Chips are single-select and toggle off when tapped again. Selecting one filters the grid to that category.

**Product grid** — `flex: 1`, `min-height: 0`, `overflow: auto`, `padding: 13px 20px 18px`. CSS grid, `repeat(auto-fill, minmax(190px, 1fr))`, `gap: 12px` — columns resolve to 4 at the design width and fall to 3, 2 and 1 as the viewport narrows. Do not hardcode a column count.

Each tile: height 134, background `#FFFFFF`, `1px solid #E2DED4`, `border-radius: 15px`, `padding: 15px`, vertical flex with `justify-content: space-between`, `transition: box-shadow 90ms ease, transform 90ms ease`.
- Hover: `border-color: #17171A`.
- Active: `transform: scale(0.985)`.
- Unavailable (`sold out`, `order in`): `opacity: 0.55`, background `#FAF8F4`, `cursor: not-allowed`, no click handler, stock label uppercased.

Tile contents:
- **Meta line** — 10px/700, `letter-spacing: 0.09em`, uppercase, `#6E6A62`, `margin-bottom: 7px`. Carries the vertical's secondary identity: `PER LB`, `FITS SELECTED`, `READY · COPAY`, `M · WHITE`.
- **Name** — 17px/600, `letter-spacing: -0.01em`, `line-height: 1.25`, `text-wrap: pretty`.
- **Footer row** — baseline-aligned space-between. Price 16px/500 mono. Stock 11px/600 mono, colored: `#C8461E` for sold out / order in / counsel / clearance, `#8A6110` for low stock ("N left"), `#6E6A62` otherwise.

Tapping an available tile adds one unit to the ticket.

Empty filter result: centered 15px `#6E6A62` text, "Nothing matches that filter.", `padding: 80px 20px`.

---

#### 1c. Cart panel

Background `#FFFFFF`, vertical flex, `min-height: 0`, `overflow: hidden`.
- Side-by-side: `flex: 0 1 430px`, `min-width: 300px`, `border-left: 1px solid #E2DED4`.
- Stacked: `flex: 0 0 auto`, `width: 100%`, `max-height: 62%`, `border-top: 1px solid #E2DED4`.

The customer bar, ticket header, totals footer and Charge button are all `flex: 0 0 auto`; the line-item list is the only child that scrolls.

**Customer bar** (top) — flex row, `gap: 12px`, `padding: 13px 20px`, `border-bottom: 1px solid #EFEBE2`, cursor pointer. Hover `#F2EFE8`.
- No customer: background `#FBFAF7`; avatar background `#9A968C`, glyph `+`; name "Walk-in customer" 15px/600; meta "Tap to attach a profile or loyalty card" 12px mono `#55524C`.
- Customer attached: background `#F0F5F2`; avatar background `#1F6F4A`, initials; name is the customer's; meta is `Tier · N pts · balance state`.
- Avatar is 38 × 38, 50% radius, white text 13px/700.
- Trailing `F4` key cap, same style as the header's but on a white background.

**Ticket header** — `padding: 13px 20px 11px`, `border-bottom: 1px solid #EFEBE2`, baseline space-between. Left: "Current sale" 18px/700 and, 3px below, `Ticket #1043 · grocery` 12px mono `#6E6A62`. Right: "Clear" 13px/600 `#C8461E`, underline on hover; clears cart, discount, points and tenders.

**Line items** — `flex: 1 1 auto`, `min-height: 0`, `overflow: auto`. This is the cart's only scroll container.

Empty state: `padding: 56px 40px`, centered, `#6E6A62`. A 52 × 52 `2px dashed #D8D3C7` square, `border-radius: 15px`, `margin: 0 auto 14px`, then 15px text `line-height: 1.5`: "Scan an item or tap the grid to start serving this customer."

Each row: `padding: 13px 20px`, `border-bottom: 1px solid #F3F0E9`, flex row, `gap: 12px`, `align-items: flex-start`.
- **Name block** (`flex: 1`): name 15px/600 `letter-spacing: -0.01em` `text-wrap: pretty`; unit line 12px mono `#6E6A62`, `margin-top: 4px`, format `$4.29 each`.
- **Stepper**: flex row, `gap: 2px`, background `#F7F5F0`, `border-radius: 10px`, `padding: 3px`. Minus and plus are 32 × 32, `border-radius: 8px`, 19px/600 `#55524C`, `user-select: none`, hover background `#FFFFFF`. Quantity between them: `min-width: 24px`, centered, 15px/700 mono. Decrementing to zero removes the line.
- **Line total**: `width: 76px`, right-aligned, 15px/500 mono.

**Totals footer** — `border-top: 1px solid #EFEBE2`, `flex: 0 0 auto`, vertical flex. Standard: `padding: 15px 20px 16px`, `gap: 8px`. Compact (`short`): `padding: 10px 16px 12px`, `gap: 5px`.

Rows, in order (conditional ones only when non-zero):
| Row | Style |
|---|---|
| `Subtotal (N items)` | 14px `#55524C`, value mono |
| Discount label | 14px/600 `#1F6F4A`, value `−$X.XX` mono |
| `Points redeemed` | 14px/600 `#1F6F4A`, value `−$X.XX` mono |
| Tax label | 14px `#55524C`, value mono |
| — | 1px `#EFEBE2` rule, `margin: 1px 0` |
| `Total` | label 17px/700; value 29px/700 mono, `letter-spacing: -0.02em` (23px in compact mode) |

Item count pluralizes: `(1 item)` / `(3 items)`.

**Secondary actions** — four buttons, `border-radius: 11px`, `1px solid #DDD8CD`, background `#FBFAF7`, 600 weight, centered, `white-space: nowrap`. Hover: `border-color: #17171A`, background `#FFFFFF`. Labels are per-configuration (see Configuration).
- Standard: 2 × 2 grid, `gap: 8px`, `margin-top: 2px`, each 43px tall, 14px.
- Compact (`short`): single horizontal row, `gap: 7px`, `overflow-x: auto`, each 40px tall, `padding: 0 14px`, 13px, `flex: 0 0 auto`.

**Charge button** — `border-radius: 13px`, `padding: 0 20px`, `flex: 0 0 auto`, space-between, white text. Label "Charge" 17px/700; amount 21px/700 mono. 62px tall standard, 52px in compact mode — never below 44px.
- Enabled (cart non-empty): background `#17171A`, hover `#1F6F4A`, cursor pointer.
- Disabled (cart empty): background `#B9B5AC`, `cursor: not-allowed`, no handler.

---

#### 1d. Shortcut bar (56px)

Background `#17171A`, flex row, `gap: 7px`, `padding: 0 20px`, `overflow-x: auto`. Keys never wrap or shrink — the strip scrolls horizontally when the viewport cannot hold all eight. If your platform offers a better narrow affordance (an overflow menu), use it; the keys must stay reachable.

Each key button: height 37, `padding: 0 13px`, `border-radius: 9px`, background `#262629`, text `#F0EEE9` 13px/600, flex row `gap: 8px`, `white-space: nowrap`. Hover background `#3C3C40`. Leading key cap: 11px mono, text `#17171A` on `#C9C4B8`, `border-radius: 5px`, `padding: 2px 5px`.

| Key | Label | Action |
|---|---|---|
| F2 | Search | Product search dialog |
| F3 | Stock | Stock lookup dialog |
| F4 | Customer | Customer lookup dialog |
| F6 | Discount | Discount dialog |
| F7 | Hold | Park current ticket immediately (no dialog) |
| F8 | Recall | Held tickets dialog |
| F9 | Reprint | Recent invoices dialog |
| F10 | Returns | Recent invoices dialog (returns share the invoice list) |

Right-aligned status line, 12px mono `#A5A199`, `white-space: nowrap`: `N held · drawer $220.00 · printer ready · online`. Hidden entirely in stacked mode rather than clipped.

Real keyboard bindings mirror these. `Escape` closes the active overlay.

---

### 2. List dialogs (search, stock, customer, discount, recall, reprint)

One shared shell, six content sets. Backdrop `rgba(23,23,26,0.45)` over the whole screen, centered, `padding: clamp(12px, 3vw, 40px)`, `overflow: auto`. Backdrop click closes. Panel click must stop propagation.

**Panel:** `width: min(720px, 100%)`, `max-height: 100%`, background `#FFFFFF`, `border-radius: 18px`, `overflow: hidden`, vertical flex, `box-shadow: 0 30px 80px rgba(0,0,0,0.35)`.

**Header** — `padding: 22px 24px 16px`, `border-bottom: 1px solid #EFEBE2`, space-between. Title 20px/700 `letter-spacing: -0.01em`; subtitle 13px `#55524C`, `margin-top: 4px`. Close button 38 × 38, `border-radius: 10px`, `1px solid #DDD8CD`, `×` 19px `#55524C`, hover `border-color: #17171A`.

**Filter input** (search, stock, customer only) — `padding: 16px 24px 0`, full-width, 50px tall, `1px solid #E2DED4`, `border-radius: 11px`, `padding: 0 15px`, 16px text, background `#F7F5F0`, no outline on focus.

**Rows** — scroll region, `padding: 12px 16px 16px`. Each row: flex, `gap: 16px`, `align-items: center`, `padding: 14px 16px`, `border-radius: 12px`, cursor pointer, hover background `#F7F5F0`.
- Left block (`flex: 1`): primary 16px/600 `letter-spacing: -0.01em`; secondary 13px `#55524C`, `margin-top: 3px`.
- Right block, right-aligned: value 15px/600 mono in a row-specific color; sub-label 12px mono `#6E6A62`, `margin-top: 3px`.

Selected rows (discount dialog) additionally get background `#F0F5F2` and `1px solid #1F6F4A`; unselected rows carry `1px solid transparent` so nothing shifts.

Empty state: `padding: 50px 20px`, centered, 15px `#6E6A62`, with dialog-specific copy.

**Footer** (customer and reprint only) — `border-top: 1px solid #EFEBE2`, `padding: 14px 20px`, flex `gap: 10px`. Note text 13px `#55524C` on the left (`flex: 1`); action button 46px tall, `padding: 0 20px`, `border-radius: 11px`, background `#17171A`, white 14px/600, hover `#1F6F4A`.

**Per-dialog content:**

| Dialog | Title / subtitle | Rows | Right column | On row click |
|---|---|---|---|---|
| Search | "Product search" / "Search by name, code or category, then add straight to the ticket." | Catalog items matching name, meta or category; max 8 | price / `N on hand` | Add to ticket, close |
| Stock | "Stock lookup" / "On-hand quantity across this store and the warehouse." | Items matching name; max 8; secondary shows `category · warehouse N` | `N here` colored `#C8461E` at 0, `#8A6110` under 5, else `#1F6F4A`; sub is the stock phrase | Close |
| Customer | "Customer lookup" / "Attach a profile to see points, account balance and visit history." | Customers matching name or phone; secondary `Tier · phone · N visits` | `N pts`; sub is balance state, `#C8461E` when owing, `#1F6F4A` otherwise | Attach, close |
| Discount | "Apply discount" / "Order-level discounts and loyalty redemption." | Five discounts plus a points row when a customer is attached | `Applied` or the rate; sub `tap to apply` / `tap to remove` | Toggle, close |
| Recall | "Held tickets" / "Park a sale to serve the next customer, then bring it back here." | Held tickets; secondary `customer · N lines` | total; sub `tap to recall` | Restore cart and customer, remove from held, close |
| Reprint | "Recent invoices" / "Reprint a receipt or start a return against a past sale." | Four static invoices; secondary `when · N items · payment` | total; sub `tap to reprint` | Open its 80mm receipt stamped "reprint" |

Customer footer appears only when a customer is attached: note `<Name> is attached to this ticket.`, button "Remove customer".
Reprint footer is always present: note names the last sale on this register if there is one, button "Reprint last receipt" (falls back to the newest invoice).

---

### 3. Tender dialog

Opened by Charge. Panel `width: min(960px, 100%)`, `max-height: 100%`, `border-radius: 20px`, `overflow: auto`, horizontal flex with `flex-wrap: wrap` — the dark rail sits left of the payment pane when there is room and stacks above it when there is not.

#### Left rail (dark)

`flex: 1 1 300px`, `min-width: 280px`, `max-width: 340px`. Background `#17171A`, white text, `padding: 26px 24px`, vertical flex.

- Eyebrow "Balance due" — 11px/700, `letter-spacing: 0.11em`, uppercase, `#A5A199`.
- Amount — 50px/700 mono, `letter-spacing: -0.03em`, `margin-top: 4px`.
- Sub "Ticket total $X.XX" — 12px mono `#A5A199`.
- Rule `#3C3C40`, `margin: 20px 0 14px`.
- Eyebrow "Tendered", same style as above.
- Tender list, `flex: 1`, scroll, `gap: 8px`. Empty copy: "Take the full amount or split it across several payment types." 14px `#A5A199`. Each tender chip: background `#262629`, `border-radius: 10px`, `padding: 11px 13px`, flex; type 14px/600 (`flex: 1`), amount 15px mono, then a removal `×` 19px `#F0A188`.
- **Change block** (only when overtendered): `margin-top: 12px`, background `#1F6F4A`, `border-radius: 12px`, `padding: 13px 15px`, baseline space-between. Label "Change due" 14px/700; amount 23px/700 mono.

#### Right pane

`flex: 1 1 420px`, `min-width: 300px`, `padding: 24px`, vertical flex, `gap: 16px`.

**Title row** — "Take payment" 20px/700; beneath it 13px `#55524C` showing the attached customer and tier, or "Walk-in customer". Close button as in list dialogs.

**Payment type grid** — eyebrow "Payment type" (11px/700, `0.11em`, uppercase, `#6E6A62`, `margin-bottom: 9px`), then `repeat(auto-fit, minmax(130px, 1fr))`, `gap: 9px`. Six tiles, each 62px tall, `border-radius: 12px`, `1px solid #DDD8CD`, left-aligned vertical flex, `gap: 3px`, `padding: 0 15px`. Label 15px/600; note 11px mono `#6E6A62`.

| Type | Note |
|---|---|
| Cash | drawer opens |
| Card | chip / tap |
| Mobile wallet | QR or NFC |
| Gift card | check balance |
| Store credit | on account |
| Split | add another |

Tapping a tile tenders the **entire remaining balance** in that type. Once the balance reaches zero all six dim to `opacity: 0.5`, background `#FAF8F4`, `cursor: not-allowed`, and their notes change to "balance settled".

**Manual entry + quick cash** — hidden entirely once the balance is settled. Horizontal flex, `gap: 16px`, `flex-wrap: wrap`.

*Left — `flex: 1 1 210px`, `max-width: 260px`, `min-width: 190px`:*
- Eyebrow "Amount tendered".
- Readout: 48px tall, `1px solid #DDD8CD`, `border-radius: 11px`, white, right-aligned, `padding: 0 14px`, 23px/700 mono. `#9A968C` while empty (showing `$0.00`), `#17171A` once digits are keyed.
- Keypad: `repeat(3, 1fr)`, `gap: 6px`, `margin-top: 8px`. Twelve keys `7 8 9 / 4 5 6 / 1 2 3 / 0 00 ⌫`, each 46px tall, `border-radius: 10px`, `1px solid #DDD8CD`, white, 17px/600 mono, `user-select: none`.

Entry is **cents-first**: digits append to a string, parsed as an integer and divided by 100, so keying `2`,`0`,`0`,`0`,`0` yields `$200.00`. Leading zeros are stripped; the string caps at 8 characters; `⌫` removes the last character.

*Right — `flex: 1 1 260px`, `min-width: 220px`:*
- Eyebrow "Quick cash", then `repeat(auto-fit, minmax(84px, 1fr))`, `gap: 8px`. Each button 52px tall, `border-radius: 11px`, `1px solid #DDD8CD`, white, centered vertical flex, `gap: 2px`. Amount 15px/600 mono; below it the change preview 10px/600 mono — `change $X.XX` in `#8A6110`, or `exact` in `#1F6F4A`.
- Hint line, `margin-top: 12px`, 12px `#6E6A62`, `line-height: 1.5`. Empty: "Key an amount for a partial payment or an overtender, or tap a quick-cash note." Keyed above due: "Change from $X will be $Y." Exact: "Exact amount." Below due: "$X will remain on the ticket."
- Spacer, then two buttons in a `1fr 1fr` grid, `gap: 8px`, each 48px tall, `border-radius: 11px`, 14px/700: "Tender cash" (filled `#17171A`, white) and "Tender card" (outlined `#17171A`). Both disabled — `#C9C4B8` fill / `#DDD8CD` border and `#9A968C` text, `cursor: not-allowed` — until a non-zero amount is keyed. Posting a tender clears the pad.

**Quick cash denominations** are computed, not hardcoded: the exact balance, its ceiling, the balance rounded up to the next 5 / 10 / 20 / 50, and every note in `[5, 10, 20, 50, 100, 200, 500]` larger than the balance — deduplicated, ascending, first six. A $150.00 balance therefore surfaces $200 as a tap target, and its button reads `change $50.00`.

**Footer** — flex `gap: 10px`, `flex-wrap: wrap`. "Back to sale" `flex: 1 1 130px`, `max-width: 150px`, 58px tall, `border-radius: 12px`, `1px solid #DDD8CD`, white, 15px/600. Complete button `flex: 2 1 200px`, 58px tall, 16px/700 white text:
- Balance outstanding: background `#B9B5AC`, `cursor: not-allowed`, label `Balance $X.XX remaining`.
- Settled: background `#1F6F4A`, cursor pointer, label "Complete sale".

---

### 4. Receipt confirmation

Panel `width: min(420px, 100%)`, `max-height: 100%`, `overflow: auto`, background `#FFFFFF`, `border-radius: 18px`, `padding: 28px`, vertical flex `gap: 16px`.

- Centered header: 54 × 54 circle background `#1F6F4A`, white `✓` 26px, `margin: 0 auto 12px`. Title "Sale complete" 21px/700. Reference `INV-10420 · grocery` 13px mono `#55524C`.
- Summary card: background `#F7F5F0`, `border-radius: 12px`, `padding: 15px 16px`, `gap: 9px`. Rows at 14px: "Ticket total" `#55524C`/500; "Discount applied" `#1F6F4A`/600 when present; "Points redeemed" `#1F6F4A`/600 when present; "Paid with" `#55524C`/500; "Change given" `#17171A`/700 when overtendered. Values mono.
- Two print buttons, `1fr 1fr`, `gap: 9px`, each 48px tall, `border-radius: 11px`, `1px solid #DDD8CD`, two-line centered: "Print receipt" / "80mm thermal" and "Tax invoice" / "A4 sheet" (sub-label 11px mono `#6E6A62`). Hover `border-color: #17171A`.
- "Next customer" — 54px tall, `border-radius: 12px`, background `#17171A`, white 16px/700, hover `#1F6F4A`.

**Critical behavior:** once a sale is completed the app holds a `finalised` flag. *Every* exit from the receipt or either print preview — the Next customer button, the close buttons, `Escape`, a backdrop click — must reset the ticket (cart, tenders, discount, points, customer), advance the ticket number and clear the flag. A finalised sale must never fall back to an editable cart. Reprints opened from F9 do **not** set the flag and must leave any open ticket untouched.

---

### 5. Print previews

Two documents render from the completed (or reprinted) sale. Both are wrapped in a centered column (`max-width: 100%`) with the sheet on top and a wrapping, center-justified button row beneath: "Close" (white, 46px, `border-radius: 11px`, `padding: 0 20px`, 14px/600), a format-switch button (background `#3C3C40`, white — "Switch to A4 invoice" / "Switch to 80mm receipt"), and "Send to printer" (background `#1F6F4A`, white 14px/700, `padding: 0 22px`).

#### 5a. 80mm thermal receipt

Sheet `width: min(320px, 100%)`, `max-height: 62vh` with scroll, white, `padding: 22px 20px 26px`. Entire document is mono, 12px, `line-height: 1.55`, `#17171A`. Sections are separated by `1px dashed #17171A` rules.

Order: centered store block (name 15px/700 `letter-spacing: 0.04em`, address 11px, `VAT <id>` 11px) → reference row and timestamp, register and operator, context line (all 11px) → line items, each a bold name over a space-between row of `N × $unit` and the line amount → totals (subtotal, discount, points, tax, then `TOTAL` at 16px/700) → tenders and change → centered footer: policy text 11px, a barcode glyph row (`▌│█║▌║▌║█│▌║`, 22px, `letter-spacing: -1px`), and the reference again.

#### 5b. A4 tax invoice

Sheet `width: min(620px, 100%)`, `max-height: 62vh` with scroll, white, `padding: clamp(20px, 4vw, 40px) clamp(20px, 4.5vw, 44px)`.

These are on-screen previews of print output. When wiring real printing, drive the thermal document at 80mm width and the invoice at A4 from print stylesheets — the viewport-relative caps here exist only so the preview fits the screen.

- **Letterhead** — space-between, `padding-bottom: 20px`, `border-bottom: 2px solid #17171A`. Left: store name 20px/700, then address and `VAT <id>` 12px `#55524C` `line-height: 1.6`. Right: eyebrow "Tax invoice" 13px/700 `0.12em` uppercase `#6E6A62`; reference 19px/700 mono; timestamp 12px mono `#55524C`.
- **Party block** — `1fr 1fr`, `gap: 24px`, `padding: 20px 0`, `border-bottom: 1px solid #E2DED4`. "Billed to": attached customer name 14px/600 plus phone and tier 12px `#55524C`, or "Cash customer" / "No account attached to this sale". "Sale reference": context line, register and operator, `Paid by <tenders>`, 12px mono `line-height: 1.7`.
- **Line table** — columns `minmax(0, 1fr) 44px 78px 88px` (Description, Qty, Unit, Amount), `gap: 10px`. Header row 10px/700 `0.1em` uppercase `#6E6A62`, `padding: 14px 0 10px`, bottom border `#E2DED4`. Body rows 13px, `padding: 11px 0`, `border-bottom: 1px solid #F3F0E9`; description 600 weight, numerics mono and right-aligned, amount 600.
- **Totals** — right-aligned 280px column, `gap: 7px`: subtotal, discount, points, tax at 13px, then a 1px `#17171A` rule, then the tender lines at 13px `#55524C`. `TOTAL` renders at 16px/700 within the same list.
- **Terms** — `margin-top: 28px`, `padding-top: 16px`, `border-top: 1px solid #E2DED4`, space-between, 11px `#55524C` `line-height: 1.7`. Policy text on the left; on the right a 180px signature rule (`border-top: 1px solid #9A968C`, `padding-top: 26px`, bottom-aligned) captioned "Authorised signature".

---

## Interactions & Behavior

**Adding and editing items** — tapping an available tile increments that product by one. Unavailable tiles are inert. Steppers adjust quantity; decrementing past one removes the line. Clear wipes cart, discount, points and tenders but keeps the attached customer.

**Filtering** — chips are single-select toggles over category. The search and stock dialogs filter independently by their own query and cap at eight rows.

**Customer** — attaching sets the ticket's customer, recolors the customer bar to the green state and unlocks the loyalty row in the discount dialog. Removing it also stops any points redemption.

**Discounts** — one order-level discount at a time; selecting a second replaces the first, selecting the active one removes it. Points redemption is independent and can stack with a discount. Both dialogs close on selection.

**Hold and recall** — F7 parks the current ticket into an in-memory list, capturing cart, customer and mode, and immediately clears the register. It no-ops on an empty cart. F8 lists held tickets; recalling restores the cart and customer and removes it from the list. The held count shows in the status line.

**Tender** — payment-type tiles post the full remaining balance. The keypad posts an arbitrary amount as cash or card. Multiple tenders accumulate and are individually removable, which returns their amount to the balance. Completion is blocked while any balance remains.

**Change** — overtendering never blocks completion. Change is `paid − total`, surfaced in three places: the quick-cash button's preview before the tap, the green block in the tender rail after it, and the "Change given" row on the receipt and both printed documents.

**Print** — either format can be opened from the receipt, and each preview can switch to the other. "Send to printer" returns to the receipt (a real implementation dispatches the job). F9 reprints open the thermal format directly with the timestamp suffixed "reprint".

**Keyboard** — F2, F3, F4, F6, F8, F9, F10 open their dialogs; F7 holds; Escape closes. All are bound at the window level with `preventDefault` so browser defaults do not fire. Bind on mount, unbind on unmount.

**Transitions** — tiles only: `box-shadow` and `transform` over 90ms ease, `scale(0.985)` on press. Nothing else animates. Dialogs appear without transition, deliberately — a register should feel instant.

---

## State Management

| State | Type | Purpose |
|---|---|---|
| `cart` | `{ [productName]: qty }` | The open ticket |
| `chip` | `string \| null` | Active category filter |
| `customer` | `Customer \| null` | Attached profile |
| `discount` | `Discount \| null` | Order-level discount |
| `usePoints` | `boolean` | Loyalty redemption active |
| `tenders` | `{ id, type, amount }[]` | Payments taken so far |
| `amt` | `string` | Raw keypad digits, cents-first |
| `overlay` | `null \| 'search' \| 'stock' \| 'customer' \| 'discount' \| 'recall' \| 'reprint' \| 'tender' \| 'receipt' \| 'thermal' \| 'invoice'` | Single active overlay |
| `dialogQuery` | `string` | Filter text in list dialogs |
| `held` | `HeldTicket[]` | Parked tickets |
| `lastSale` | `Sale \| null` | Snapshot driving receipt and print documents |
| `finalised` | `boolean` | Sale completed; any overlay exit must reset |
| `narrow` | `boolean` | Viewport width < 780px — stacked layout |
| `short` | `boolean` | Viewport height < 660px — compact cart footer |
| `seq` | `number` | Ticket / invoice counter |

**Derived totals** — recompute on every render from cart and modifiers; never store them:

```
subtotal = Σ unitPrice × qty
discount = kind === 'pct' ? subtotal × value : min(value, subtotal)
maxPoints = customer.points / 100
points   = usePoints ? min(maxPoints, subtotal − discount) : 0
taxable  = max(0, subtotal − discount − points)
tax      = taxable × taxRate
total    = taxable + tax
paid     = Σ tender amounts
due      = max(0, total − paid)
change   = max(0, paid − total)
```

Round to 2 decimals at every step (`Math.round(n * 100) / 100`). Discount and points reduce the taxable base before tax is calculated — verify this against local tax law before shipping.

**Data fetching** — the prototype is fully in-memory. A real build needs: catalog with live stock, customer search, discount rules with approval levels, held tickets (server-side so any terminal can recall), recent invoices, and a payment terminal integration. Every mutation that touches money must be idempotent and survive a mid-sale reload.

---

## Configuration

The five modes in the prototype are configuration objects, not variants. One is selected per deployment. Each supplies:

| Field | Purpose |
|---|---|
| `search` | Search trigger placeholder |
| `context` | Header context line, repeated on both printed documents |
| `taxLabel` / `taxRate` | Tax row copy and rate |
| `chips` | Category filters |
| `actions` | Four secondary button labels and their targets |
| `items` | Catalog: name, meta, price, stock phrase, category, on-hand count |
| store block | Name, address, VAT id, receipt policy footer |

Shipped configurations:

| Mode | Tax | Secondary actions | Store |
|---|---|---|---|
| Grocery | 2% (food exempt) | Price check, Manual weight, Coupon, Void line | Parkway Market |
| Auto parts | 8.25% | Fitment check, Core charge, Order in, Void line | Parkway Auto Parts |
| Pharmacy | 0% (Rx exempt) | Counsel required, Insurance, Partial fill, Void line | Parkway Pharmacy |
| Restaurant | 7% + service | Modifiers, Send to kitchen, Split bill, Void line | Parkway Kitchen |
| Retail | 8.25% | Size / color, Loyalty lookup, Gift receipt, Void line | Parkway & Co. |

Receipt policy text is category-specific and legally load-bearing — auto parts covers fitted electrical parts and core returns, pharmacy states Rx items are non-returnable and to retain for insurance, retail gives the 30-day tag policy. Treat these as content owned by the business, not design copy.

---

## Design Tokens

**Color**

| Token | Hex | Use |
|---|---|---|
| Ink | `#17171A` | Text, dark surfaces, primary buttons |
| Ink 2 | `#262629` | Shortcut key buttons, tender chips |
| Ink 3 | `#3C3C40` | Rules on dark, switch button |
| Body | `#55524C` | Secondary text |
| Muted | `#6E6A62` | Labels, eyebrows, tertiary text |
| Faint | `#9A968C` | Placeholders, empty avatar |
| Disabled | `#B9B5AC` | Disabled fills |
| Key cap | `#C9C4B8` | Key cap background |
| Line | `#E2DED4` | Primary borders |
| Line 2 | `#EFEBE2` / `#F3F0E9` | Interior rules |
| Border | `#DDD8CD` | Button borders |
| Canvas | `#F7F5F0` | App background, inputs |
| Canvas 2 | `#FBFAF7` / `#FAF8F4` | Secondary buttons, disabled tiles |
| Chrome | `#F2EFE8` | Hover on customer bar |
| Surface | `#FFFFFF` | Panels, cards, sheets |
| Green | `#1F6F4A` | Brand, success, discounts, change |
| Green tint | `#F0F5F2` | Attached-customer and selected-row backgrounds |
| Red | `#C8461E` | Clear, unavailable, balance owed |
| Amber | `#8A6110` | Low stock, change preview |
| On dark muted | `#A5A199` | Eyebrows and status on dark |
| On dark body | `#F0EEE9` | Key button text |
| Removal | `#F0A188` | Tender remove glyph |

**Type** — Archivo 400/500/600/700 for UI; JetBrains Mono 400/500/700 for every number, reference, timestamp and key cap. The monospace rule is strict: prices, quantities, totals, invoice numbers and clock times are all mono so columns align and digits do not jitter as they change.

Scale: 10px eyebrow (700, `0.09–0.12em`, uppercase) · 11px key caps and notes · 12px meta · 13px secondary · 14px body · 15–16px emphasis · 17–21px titles · 23–30px amounts · 50px tender balance. Tight tracking (`-0.01em` to `-0.03em`) on everything 16px and above.

**Spacing** — 2, 3, 4, 6, 7, 8, 9, 10, 12, 13, 14, 15, 16, 20, 22, 24, 26, 40. Panel padding 20–24; dialog padding 22–28; sheet padding 22–44.

**Radius** — 5 key caps · 8 stepper buttons · 9 shortcut buttons · 10 tender chips, keypad, close buttons · 11 inputs, secondary buttons, quick cash · 12 tender tiles, dialog rows, summary cards · 13 charge button · 15 product tiles · 18 dialog panels · 20 tender panel · 50% avatars.

**Shadow** — overlay panels `0 30px 80px rgba(0,0,0,0.35)`; print sheets `0 24px 60px rgba(0,0,0,0.35)`; tile hover relies on border color only.

**Sizing** — cart panel 430 · tender rail 340 · list dialog 720 · tender panel 960 × 620 · receipt 420 · thermal sheet 320 · invoice sheet 620 · header 66 · shortcut bar 56 · product tile 134 · charge 62 · tender complete 58 · keypad key 46.

**Touch targets** — nothing interactive is under 32px; primary actions are 46–66px. Preserve this on any re-layout.

---

## Assets

None. No images, no icon files, no external media.

Two glyphs stand in for real assets and should be replaced during implementation:
- The **search icon** is a bare circle outline (`14px`, `2px solid #6E6A62`, 50% radius) in the header trigger. Substitute the codebase's magnifier icon.
- The **barcode** on the thermal receipt is a text run of box-drawing characters. Substitute a real barcode encoding the invoice reference (Code 128 is standard for POS).

The empty-cart placeholder square, the `+` in the empty avatar, the `✓` in the receipt header and the `×` close glyphs are all intentional as-is, but may be swapped for icon-set equivalents.

Fonts load from Google Fonts: Archivo (400, 500, 600, 700) and JetBrains Mono (400, 500, 700). Self-host both for a register that must work offline.

---

## Out of Scope

Present in the design as labels but not implemented, and worth scoping before build:

- **Manager override** — the 20% discount is marked "Requires manager PIN" but no PIN prompt exists. Voids, returns and price overrides need the same gate.
- **Returns and exchanges** — F10 currently opens the invoice list. A real return flow needs line selection, reason codes, restocking rules and a negative-total tender path.
- **Cash drawer** — open, count-in, count-out, X and Z reports, paid-in and paid-out.
- **Line-level actions** — per-line discount, void with reason, price override, modifiers for restaurant items.
- **Offline mode** — the status line claims "online" but there is no queue-and-sync behavior.
- **Phone layout** — the design is fluid down to roughly 380px and stacks the cart below the catalog, but it is still one layout. A purpose-built phone register would differ: a collapsed cart bar that opens the ticket full-screen, a 2-up catalog, shortcuts behind an overflow menu, and a full-screen tender sheet.

---

## Files

- `POS Sale.dc.html` — the complete prototype: layout, styling, catalog data and all behavior described above.
- `support.js` — runtime for the prototype's template syntax. **Do not port.** Present only so the HTML opens and runs in a browser for reference.
