# Quick Sale (POS) — API endpoints

The Quick Sale / POS screen (`features/sales/quick-sale-shortcut`) is implemented and in
use. **Swagger at `/docs` on the running API is the authoritative contract** — request and
response shapes live there and stay in sync with the code. This page exists only to point
at which controllers the POS talks to.

## Routes the POS uses

| Purpose | Route | Controller |
|---|---|---|
| Recent customers | `GET /api/customers/recent` | `CustomerController` |
| Customer lookup by phone | `GET /api/customers/search-by-phone` | `CustomerController` |
| Product search | `GET /api/v1/products` | `ProductsController` |
| Stock availability probe | `POST /api/Stock/check` | `StockController` |
| Batch stock probe | `POST /api/Stock/check-multiple` | `StockController` |
| Technicians | `GET /api/Technician` | `TechnicianController` |
| Sales orders (POS checkout) | `/api/SalesOrder` | `SalesOrderController` |
| Invoice number generation | `/api/code-generate` | `CodeGenerateController` |

Notes on routes that are easy to get wrong:

- Customers are at **`api/customers`** (plural), not `api/customer`.
- Products are at **`api/v1/products`**; there is no `api/parts/active`. Brands are
  likewise `api/v1/brands` only — an unversioned `/api/brands` correctly 404s.
- `StockController` uses `[Route("api/[controller]")]`, so the path is case-sensitively
  `api/Stock`.

## Validation worth knowing

`POST /api/Stock/check` and `/check-multiple` reject `PartId == Guid.Empty` and any
`Quantity <= 0` with a 400 before touching the repository — a negative or zero quantity
does not return `available: true`.

## History

This file was originally a pre-implementation requirements list written 19 May 2026,
before the POS backend existed. It carried a "Backend (C# API) ⚠️ — needs implementation"
checklist and several route names that were never adopted, which made it actively
misleading once the endpoints shipped. Replaced with the above on 16 Aug 2026.

If you are looking for POS behaviour rather than routes, read the component and its
service directly: `features/sales/quick-sale-shortcut/` and
`features/sales/services/quick-sale.service.ts`.
