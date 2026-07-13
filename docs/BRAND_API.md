# Brand API

Base URL: `/api/v1/brands`

All endpoints require authentication (`Authorization: Bearer <token>`).
Write operations (POST, PUT, DELETE) additionally require the **Admin** role.

---

## Entity

| Field          | Type      | Nullable | Notes                                    |
|----------------|-----------|----------|------------------------------------------|
| `id`           | GUID      | No       | Unique identifier                        |
| `name`         | string    | No       | Brand name                               |
| `code`         | string    | No       | Unique code — always stored uppercase    |
| `description`  | string    | Yes      | Brand description                        |
| `logoUrl`      | string    | Yes      | URL to brand logo image                  |
| `website`      | string    | Yes      | Official website URL                     |
| `country`      | string    | Yes      | Country of origin                        |
| `contactEmail` | string    | Yes      | Contact email address                    |
| `contactPhone` | string    | Yes      | Contact phone number                     |
| `displayOrder` | int       | No       | UI sort order — lower = shown first      |
| `isActive`     | bool      | No       | Active status (default `true`)           |
| `createdAt`    | datetime  | No       | Creation timestamp (UTC, ISO 8601)       |
| `modifiedAt`   | datetime  | Yes      | Last update timestamp (UTC, ISO 8601)    |

**Notes on optional fields:**
- Fields `description`, `logoUrl`, `website`, `country`, `contactEmail`, `contactPhone` return `null` (not `""`) when not set.
- `code` is always normalised to uppercase before storage and comparison.

---

## Endpoints

---

### GET /api/v1/brands — List

Returns a paginated, ordered list. All query parameters are optional.

**Query parameters**

| Parameter  | Type   | Default | Description                              |
|------------|--------|---------|------------------------------------------|
| `search`   | string | —       | Name contains, case-insensitive          |
| `isActive` | bool   | —       | Filter by active status                  |
| `country`  | string | —       | Exact country match                      |
| `page`     | int    | `1`     | Page number (min 1)                      |
| `pageSize` | int    | `10`    | Items per page (min 1, max 100)          |

Results are ordered by `displayOrder ASC, name ASC`.

**Example**
```
GET /api/v1/brands?search=NGK&isActive=true&page=1&pageSize=10
```

**Response `200 OK`**
```json
{
  "data": [
    {
      "id": "6ec1b84b-5db9-4a1f-a53c-c6cb4c8a4e11",
      "name": "NGK",
      "code": "NGK",
      "description": "World's #1 spark plug brand",
      "logoUrl": "https://example.com/ngk-logo.png",
      "website": "https://www.ngkntk.com",
      "country": "Japan",
      "contactEmail": null,
      "contactPhone": null,
      "displayOrder": 1,
      "isActive": true,
      "createdAt": "2026-01-10T08:00:00Z",
      "modifiedAt": null
    }
  ],
  "pagination": {
    "page": 1,
    "pageSize": 10,
    "totalCount": 8,
    "totalPages": 1,
    "hasNextPage": false,
    "hasPreviousPage": false
  }
}
```

---

### GET /api/v1/brands/{id} — Get by ID

**Response `200 OK`** — `{ "data": { ...brand... } }`

**Response `404 Not Found`**
```json
{
  "type": "NOT_FOUND",
  "title": "Resource not found",
  "status": 404,
  "detail": "Brand '6ec1b84b-...' not found",
  "instance": "/api/v1/brands/6ec1b84b-..."
}
```

---

### GET /api/v1/brands/by-code?code=NGK — Get by code

Lookup by unique brand code. Code matching is case-insensitive.

**Response `200 OK`** — `{ "data": { ...brand... } }`

**Response `400 Bad Request`** — when `code` parameter is missing.

**Response `404 Not Found`** — same error shape as Get by ID.

---

### POST /api/v1/brands — Create

**Role required:** Admin

**Request body**
```json
{
  "name": "NGK",
  "code": "NGK",
  "description": "World's #1 spark plug brand",
  "logoUrl": "https://example.com/ngk-logo.png",
  "website": "https://www.ngkntk.com",
  "country": "Japan",
  "contactEmail": "support@ngk.com",
  "contactPhone": "+81-123456789",
  "displayOrder": 1,
  "isActive": true
}
```

| Field         | Required | Notes                                          |
|---------------|----------|------------------------------------------------|
| `name`        | Yes      | Min 1 character                                |
| `code`        | Yes      | Must be unique; stored uppercase automatically |
| `isActive`    | No       | Defaults to `true`                             |
| `displayOrder`| No       | Defaults to `0`                                |
| others        | No       | All optional string fields                     |

**Response `201 Created`** — `{ "data": { ...brand... } }`

**Response `409 Conflict`** — when `code` is already in use.

---

### PUT /api/v1/brands/{id} — Update (full replace)

**Role required:** Admin

ID comes from the URL only — do not include it in the body.
All fields are replaced; omitting a field resets it to its default/null.

**Request body** — same fields as Create (omit `code` to keep it unchanged, or supply new code).

**Important:** Always send `isActive` explicitly. The default is `true`; omitting it will keep the brand active.

**Response `200 OK`** — `{ "data": { ...updated brand... } }`

**Response `404 Not Found`** / **`409 Conflict`** — same error shapes as above.

---

### DELETE /api/v1/brands/{id} — Delete (soft)

**Role required:** Admin

Soft-deletes the brand (`isDeleted = true`). The record remains in the database for audit purposes. Deleted brands are excluded from all list and lookup endpoints.

**Response `204 No Content`** — success, no body.

**Response `404 Not Found`** — same error shape as above.

---

## Error shape

All error responses use this consistent structure:

```json
{
  "type": "VALIDATION_ERROR | NOT_FOUND | CONFLICT | BUSINESS_RULE_VIOLATION | INTERNAL_ERROR",
  "title": "Human-readable title",
  "status": 400,
  "detail": "Specific message describing the problem",
  "instance": "/api/v1/brands"
}
```

| HTTP Status | `type`             | When                                    |
|-------------|--------------------|-----------------------------------------|
| 400         | `VALIDATION_ERROR` | Missing required fields, bad input      |
| 401         | —                  | Not authenticated (standard 401 body)   |
| 403         | —                  | Authenticated but missing Admin role    |
| 404         | `NOT_FOUND`        | Brand ID or code does not exist         |
| 409         | `CONFLICT`         | Brand code already in use               |
| 500         | `INTERNAL_ERROR`   | Unexpected server error                 |

---

## Business rules

1. `code` must be unique across all active brands.
2. `code` is automatically normalised to uppercase (`TRIM().UPPER()`) before storage — `"bosch"` is stored as `"BOSCH"`.
3. `name` and `code` are required on both create and update.
4. Delete is a **soft delete** — `isDeleted` flag is set; records are never permanently removed.
5. `createdAt` is immutable after creation.
6. `modifiedAt` is automatically updated on every PUT.
7. Pagination: `page` is clamped to minimum `1`; `pageSize` is clamped to range `[1, 100]`.
8. Concurrent code conflict: if two requests simultaneously try to use the same code, the second will receive `409 Conflict` (enforced by DB unique index + application catch).

---

## Pagination response shape

```json
{
  "data": [...],
  "pagination": {
    "page": 1,
    "pageSize": 10,
    "totalCount": 42,
    "totalPages": 5,
    "hasNextPage": true,
    "hasPreviousPage": false
  }
}
```
