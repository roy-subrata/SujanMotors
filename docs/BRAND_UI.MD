# Brand Management — Frontend Specification

Module: `features/inventory/brands`

---

## Features

- Paginated brand list with search and filters
- Create brand (modal dialog)
- Edit brand (modal dialog, pre-filled)
- Toggle active/inactive status (context menu)
- Delete brand with confirmation
- Responsive: table on desktop, cards on mobile

---

## Routes

| Path              | Component            |
|-------------------|----------------------|
| `/inventory/brands` | `BrandsComponent`  |

---

## Component tree

```
BrandsComponent                    (brands.component)
  ├── BrandsListComponent          (brands-list/brands-list.component)
  └── BrandsFormDialogComponent    (brands-form-dialog/brands-form-dialog.component)
```

---

## Brand List Page

### Header

| Element        | Behaviour                                          |
|----------------|----------------------------------------------------|
| Page title     | "Brands"                                           |
| Total badge    | Shows total brand count from pagination response   |
| Refresh button | Reloads current page / filters                     |
| New Brand (+)  | Opens Create dialog                                |

### Filters

| Filter   | Type        | Behaviour                                      |
|----------|-------------|------------------------------------------------|
| Search   | Text input  | Triggers on Enter or Apply button click        |
| Status   | Dropdown    | All / Active / Inactive                        |
| Apply    | Button      | Sends all filters, resets to page 1            |
| Clear    | Button      | Resets all filters; shown only when any active |

Active filter chips appear below the filter bar and can be dismissed individually.

### Desktop Table Columns

| Column         | Field           | Notes                            |
|----------------|-----------------|----------------------------------|
| Logo           | `logoUrl`       | Image if set, letter avatar if not; broken images fall back to avatar |
| Name           | `name`          | With `description` beneath in muted text |
| Code           | `code`          | Monospace badge                  |
| Country        | `country`       | `—` when null                    |
| Website        | `website`       | Clickable external link; truncated display |
| Order          | `displayOrder`  | Numeric badge                    |
| Created        | `createdAt`     | `dd MMM yyyy` format             |
| Status         | `isActive`      | Active (green) / Inactive (red) pill |
| Actions        | —               | Edit button + ⋮ context menu     |

### Context Menu (⋮)

| Action          | Behaviour                                              |
|-----------------|--------------------------------------------------------|
| Edit            | Opens Edit dialog pre-filled with brand data           |
| Activate        | Calls PUT with `isActive: true` (shown when inactive)  |
| Deactivate      | Calls PUT with `isActive: false` (shown when active)   |
| Delete          | Shows confirmation dialog; soft-deletes on confirm     |

Toggle status is debounced by an in-flight guard — double-click is ignored until the first request resolves.

### Mobile Card Layout

Cards show: logo/avatar, code badge, name, description, country, website link, created date, status pill, ⋮ menu.

**Pagination** is `position: fixed; bottom: 0` on mobile — always visible at the bottom of the viewport regardless of list length.

**Cards grid** has `max-height: calc(100svh - 360px)` and scrolls independently so the fixed pagination is never obscured.

### Pagination

| Element           | Behaviour                                             |
|-------------------|-------------------------------------------------------|
| Page info         | "Showing X–Y of Z brands"                            |
| Rows per page     | 10 / 20 / 50 selector                                |
| First / Prev / Next / Last | Disabled when at respective boundaries     |
| Page indicator    | "currentPage / totalPages"                           |

**Edge cases:**
- Single page: navigation buttons disabled, page indicator shows "1 / 1".
- Delete last item on page > 1: automatically reloads page `currentPage - 1`.

---

## Create Brand Dialog

Route: modal (no URL change)

### Form fields

| Field          | Input type   | Required | Validation                              |
|----------------|--------------|----------|-----------------------------------------|
| Brand Name     | Text         | Yes      | Min 2, max 100 characters               |
| Code           | Text (auto)  | Yes      | Auto-generated; read-only; max 20 chars |
| Description    | Textarea     | No       | —                                       |
| Country        | Text         | No       | —                                       |
| Display Order  | Number       | No       | Min 0                                   |
| Logo URL       | URL          | No       | —                                       |
| Website        | URL          | No       | Must match `https?://.*` pattern        |
| Contact Email  | Email        | No       | Must be valid email format              |
| Contact Phone  | Tel          | No       | —                                       |
| Active         | Checkbox     | No       | Default checked                         |

**Code field:** auto-generated via `GET /api/code-generate/brand`; displayed as read-only. If generation fails a warning toast allows manual entry.

### Submit behaviour

| Outcome     | Behaviour                                                        |
|-------------|------------------------------------------------------------------|
| Success     | Success toast → dialog closes → list reloads                    |
| 409 Conflict | "Duplicate Code" error toast; dialog stays open; form preserved |
| Other error  | Error toast; dialog stays open; form preserved                  |

Cancel button and dialog close are disabled while a create request is in-flight.

---

## Edit Brand Dialog

Opens pre-filled via `ngOnChanges` when `selectedBrand` input changes.

### Form fields

Same as Create, except:
- **Code** is editable (brand codes can be changed; backend checks for conflicts).
- If a new code conflicts with an existing brand, `409 Conflict` is shown.

### Submit behaviour

Same as Create but calls `PUT /api/v1/brands/{id}`.

Cancel button disabled while update is in-flight.

---

## Delete Brand

Triggered from context menu.

### Confirmation dialog

> *Are you sure you want to delete "BrandName"?*

Buttons: **Confirm** (soft-deletes) / **Cancel**

After deletion:
- Success toast shown.
- If the deleted item was the last on page > 1, automatically moves to `page - 1`.

---

## API Integration

| Action              | Method | Endpoint                       | Auth         |
|---------------------|--------|-------------------------------|--------------|
| List brands         | GET    | `/api/v1/brands`              | Authenticated |
| Get by ID           | GET    | `/api/v1/brands/{id}`         | Authenticated |
| Get by code         | GET    | `/api/v1/brands/by-code?code=`| Authenticated |
| Create              | POST   | `/api/v1/brands`              | Admin only   |
| Update              | PUT    | `/api/v1/brands/{id}`         | Admin only   |
| Delete              | DELETE | `/api/v1/brands/{id}`         | Admin only   |
| Active brands list  | GET    | `/api/v1/brands?isActive=true&pageSize=500` | Authenticated |
| Generate code       | GET    | `/api/code-generate/brand`    | Authenticated |

**Response envelope (list):**
```ts
{
  data: BrandResponse[];
  pagination: {
    page: number;
    pageSize: number;
    totalCount: number;
    totalPages: number;
    hasNextPage: boolean;
    hasPreviousPage: boolean;
  }
}
```

**Response envelope (single):**
```ts
{ data: BrandResponse }
```

---

## TypeScript models

```ts
export interface BrandResponse {
  id: string;
  name: string;
  code: string;
  description: string | null;
  logoUrl: string | null;
  website: string | null;
  country: string | null;
  contactEmail: string | null;
  contactPhone: string | null;
  displayOrder: number;
  isActive: boolean;
  createdAt: string;       // ISO 8601 UTC
  modifiedAt: string | null;
}

export interface CreateBrandRequest {
  name: string;
  code: string;
  description?: string | null;
  logoUrl?: string | null;
  website?: string | null;
  country?: string | null;
  contactEmail?: string | null;
  contactPhone?: string | null;
  displayOrder?: number;   // default 0
  isActive?: boolean;      // default true
}

export interface UpdateBrandRequest {
  name: string;
  code: string;
  description?: string | null;
  logoUrl?: string | null;
  website?: string | null;
  country?: string | null;
  contactEmail?: string | null;
  contactPhone?: string | null;
  displayOrder?: number;
  isActive?: boolean;      // default true — always send explicitly
}
```

---

## Error handling

| HTTP Status | UI Behaviour                                        |
|-------------|-----------------------------------------------------|
| 400         | Toast with `error.detail` message                   |
| 401         | Interceptor redirects to login                      |
| 403         | Toast "Access denied"                               |
| 404         | Toast "Not found" (stale data, refresh list)        |
| 409         | Toast with "Duplicate Code" summary + `error.detail`|
| 500         | Generic error toast                                 |

---

## Authorization

| Action          | Required role |
|-----------------|---------------|
| View brand list | Any authenticated user |
| Create brand    | Admin          |
| Update brand    | Admin          |
| Delete brand    | Admin          |
| Toggle status   | Admin          |

---

## Responsive behaviour

| Breakpoint   | Layout                                              |
|--------------|-----------------------------------------------------|
| > 768px      | Full data table with all columns                    |
| ≤ 768px      | Card list; filters stack vertically; pagination fixed at viewport bottom |
| ≤ 640px      | Form dialog fields switch to single-column layout   |

---

## Known limitations / future enhancements

- Brand logo upload (currently URL-only)
- Bulk activate/deactivate
- Bulk delete
- Brand import/export (CSV)
- Drag-and-drop display order reordering
- Audit history viewer
