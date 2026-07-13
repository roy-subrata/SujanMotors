# API Development Standards

## Purpose

This document defines the standards and best practices for designing and implementing REST APIs. The goal is to ensure consistency, maintainability, scalability, security, and ease of integration across all backend services.

---

# 1. General Principles

- Follow RESTful design principles.
- Keep APIs predictable and consistent.
- Use JSON for request and response bodies.
- Use HTTPS for all endpoints.
- APIs should be stateless.
- Never expose database entities directly.

---

# 2. API Versioning

Use URL versioning.

## Good

```text
/api/v1/products
/api/v1/customers
/api/v2/products
```

## Avoid

```text
/api/products_v1
/api/products2
```

**Current state of this repo**: routes are mixed — about half the controllers
use `[Route("api/v1/[controller]")]` and half the legacy
`[Route("api/[controller]")]`. Every **new** controller must use `api/v1`.
Migrate legacy routes opportunistically, never silently (frontend and mobile
call them).

---

# 3. Resource Naming

Use plural nouns.

## Good

```text
GET    /products
GET    /customers
GET    /orders
```

## Avoid

```text
/getProducts
/createProduct
/deleteCustomer
```

HTTP methods describe the action.

---

# 4. HTTP Methods

| Method | Purpose |
|---------|----------|
| GET | Retrieve data |
| POST | Create resource |
| PUT | Replace resource |
| PATCH | Partial update |
| DELETE | Delete resource |

---

# 5. URL Design

Good

```text
GET /products
GET /products/10
GET /products/10/images
GET /customers/25/orders
```

Avoid

```text
GET /GetProductById
POST /DeleteProduct
```

---

# 6. Request DTOs

Always create request DTOs.

```csharp
public sealed class CreateProductRequest
{
    public string Name { get; init; }
    public decimal Price { get; init; }
}
```

Never accept EF entities directly.

---

# 7. Response DTOs

Return response DTOs.

```csharp
public sealed class ProductResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; }
    public decimal Price { get; init; }
}
```

Never expose database models.

---

# 8. Response Format

Successful response

```json
{
  "data": {
    "id": 1,
    "name": "Oil Filter"
  }
}
```

Collection response

```json
{
  "data": [
    ...
  ]
}
```

---

# 9. Error Response

Return consistent errors.

```json
{
  "title": "Validation Failed",
  "status": 400,
  "errors": {
    "name": [
      "Name is required."
    ]
  }
}
```

---

# 10. HTTP Status Codes

| Status | Meaning |
|---------|---------|
|200|OK|
|201|Created|
|204|No Content|
|400|Bad Request|
|401|Unauthorized|
|403|Forbidden|
|404|Not Found|
|409|Conflict|
|422|Validation Error|
|500|Internal Server Error|

---

# 11. Validation

Validate all incoming requests.

Use FluentValidation.

Return validation errors before business logic executes.

---

# 12. Pagination

Use query parameters.

```text
GET /products?pageNumber=1&pageSize=20
```

Response — use the shared `PagedResult<T>` / `PaginationMeta`
(`src/AutoPartShop.Application/Common/`); do not invent a new shape:

```json
{
  "data": [],
  "pagination": {
    "pageNumber": 1,
    "pageSize": 20,
    "totalCount": 240,
    "totalPages": 12,
    "hasPreviousPage": false,
    "hasNextPage": true
  }
}
```

---

# 13. Filtering

```text
GET /products?category=Filter
```

Multiple filters

```text
GET /products?brand=Bosch&status=Active
```

---

# 14. Sorting

```text
GET /products?sort=name
GET /products?sort=-price
```

---

# 15. Searching

```text
GET /products?search=oil
```

---

# 16. Authentication

Use JWT or secure cookies.

Every protected endpoint requires authentication.

---

# 17. Authorization

Use policies or roles.

```text
Admin
Manager
Employee
```

Do not hardcode authorization logic.

---

# 18. Logging

Log

- Requests
- Exceptions
- Warnings
- Audit events

Never log passwords or tokens.

---

# 19. Exception Handling

Use global exception middleware.

Never use try/catch in every controller.

---

# 20. Dependency Injection

Inject services.

Controllers should never access repositories directly.

Flow

```text
Controller
    ↓
Application Service
    ↓
Repository
    ↓
Database
```

---

# 21. Repository Pattern

Repositories contain only data access.

Business rules belong in services.

---

# 22. Transactions

Use transactions when multiple writes must succeed together.

---

# 23. Async Programming

Always use async/await.

Avoid synchronous database access.

---

# 24. Security

- HTTPS only
- Validate all input
- Prevent SQL Injection
- Prevent XSS
- Prevent CSRF where applicable
- Never expose stack traces
- Rate limit public APIs

---

# 25. API Documentation

Maintain Swagger/OpenAPI.

Every endpoint should include

- Summary
- Description
- Request model
- Response model
- Status codes
- Authorization requirements

---

# 26. Naming

Controllers

```text
ProductsController
OrdersController
```

Services

```text
ProductService
OrderService
```

DTOs

```text
ProductDto
CreateProductRequest
UpdateProductRequest
ProductResponse
```

---

# 27. Performance

- Cache frequently requested data
- Use pagination
- Avoid N+1 queries
- Select only required columns
- Compress responses
- Use database indexes

---

# 28. API Checklist

- RESTful endpoints
- Versioned APIs
- DTOs only
- FluentValidation
- Global exception handling
- Consistent error responses
- Pagination
- Filtering
- Sorting
- Authentication
- Authorization
- Logging
- Swagger documentation
- Async programming
- Secure by default