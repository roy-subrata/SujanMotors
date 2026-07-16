# Backend Agent (.NET API)

## Role

Implements server-side features across the four backend projects. Owns
everything from the HTTP surface down to the database schema.

## Owns

```
src/AutoPartShop.Domain/           # entities, repository interfaces
src/AutoPartShop.Application/      # DTOs, service interfaces, use-cases
src/AutoPartShop.Infrastructure/   # DbContext, repositories, EF migrations
src/AutoPartShop.Api/              # controllers, middleware, background services
src/AutoPartShop.Api.Tests/        # backend tests
```

## Must load

- `team/standards/api.md`, `team/standards/database.md`, `team/standards/coding.md`, `team/standards/architecture.md`
- The feature spec + architect plan
- The named reference implementation

## Rules

- **Entities**: private setters + static factory methods + behavior methods
  (see `SalesOrder.Create`, `ApplyAdditionalDiscount`). Never add a public
  setter to route around a missing behavior method — add the method.
- **Transactions**: any multi-write flow (sales, stock, payments) uses the
  `CreateExecutionStrategy()` + `BeginTransactionAsync` pattern from
  `SalesOrderController`. Generate unique codes *inside* the retry lambda.
- **Money** is `decimal`, never `double`. **Stock** moves through lots (FIFO)
  and is keyed by `(PartId, VariantId?, Warehouse)` in base units.
- **DTOs only** at the API boundary — request and response. Never expose or
  bind EF entities.
- **Validation**: request DTO validation first (FluentValidation), then
  business rules. The API must never trust client-computed totals — recompute
  and verify server-side (see the quick-sale payment-total guard).
- **Authorization**: every new endpoint gets `[Authorize]` plus the correct
  `HasPermission` policy. No anonymous endpoints without a security review.
- **Migrations**: `dotnet ef migrations add <Name> --project src/AutoPartShop.Infrastructure --startup-project src/AutoPartShop.Api`.
  Review the generated migration by eye — EF sometimes drops what you didn't
  intend. Migrations apply automatically on startup.
- **Errors**: structured error responses (`{ message }` / ProblemDetails per
  `standards/api.md`); never leak stack traces.

## Verification gates

```bash
dotnet build AutoPartShop.sln
dotnet test src/AutoPartShop.Api.Tests
# then exercise the endpoint via Swagger (/docs) or the real UI flow
```

For money/stock changes, verify the numbers end-to-end (order total, invoice
total, stock level before/after) — not just the HTTP 200.

## Hand-offs

- Angular UI for the new endpoint → frontend agent
- New env var / config key → devops agent (`.env.*.example` + workflows)
- New permission seeded → note it for the security agent
