# Coding Standards

## Purpose

This document defines the coding standards and best practices that all developers must follow to ensure consistency, readability, maintainability, and scalability.

---

# 1. General Principles

## Follow

- SOLID Principles
- DRY (Don't Repeat Yourself)
- KISS (Keep It Simple)
- YAGNI (You Aren't Gonna Need It)
- Clean Code principles

## Goals

- Readable code
- Testable code
- Reusable code
- Maintainable code
- Consistent code

---

# 2. Naming Conventions

## Variables

Use meaningful names.

✅ Good

```ts
const customerName = '';
const totalPrice = 0;
const isActive = true;
```

❌ Bad

```ts
const a = '';
const data = '';
const x = 0;
```

---

## Functions

Function names should describe what they do.

✅ Good

```ts
calculateTax()
createOrder()
getCustomer()
```

❌ Bad

```ts
calc()
run()
doStuff()
```

---

## Boolean Variables

Always start with

```text
is
has
can
should
```

Example

```ts
isAdmin
hasPermission
canDelete
shouldReload
```

---

## Classes

Use PascalCase.

```text
ProductService
OrderRepository
CustomerComponent
```

---

## Interfaces

**TypeScript** — no prefix:

```text
Product
Customer
OrderItem
```

**C#** — `I` prefix, matching the entire backend:

```text
IProductRepository
ICurrentUserService
INotificationService
```

---

## Constants

Use UPPER_SNAKE_CASE.

```ts
MAX_PAGE_SIZE
DEFAULT_TIMEOUT
API_VERSION
```

---

# 3. Variable Declaration

Prefer

```ts
const
```

Use

```ts
let
```

only when reassignment is required.

Never use

```ts
var
```

---

# 4. Strong Typing

Never use

```ts
any
```

Use

```ts
Product
CustomerDto
OrderResponse
unknown
```

Always define explicit types.

---

# 5. Function Design

Functions should

- Do one thing
- Be easy to understand
- Be reusable

Maximum recommended length

- 20–40 lines

If longer, split into smaller functions.

---

# 6. Parameters

Avoid long parameter lists.

❌ Bad

```ts
createProduct(name, price, category, stock, supplier)
```

✅ Good

```ts
createProduct(request)
```

---

# 7. Single Responsibility

Each

- Class
- Function
- Service
- Component

should have one responsibility.

---

# 8. Early Return

Prefer

```ts
if (!product) {
    return;
}

save(product);
```

Avoid deeply nested conditions.

---

# 9. Avoid Duplicate Code

Extract repeated logic into

- Helper functions
- Shared services
- Utility classes

Never copy and paste business logic.

---

# 10. Comments

Comment

WHY

not

WHAT

✅ Good

```ts
// Prevent duplicate invoice creation when retrying.
```

❌ Bad

```ts
// Increment counter
counter++;
```

---

# 11. Magic Numbers

❌ Bad

```ts
if (stock < 5)
```

✅ Good

```ts
const LOW_STOCK_LIMIT = 5;

if (stock < LOW_STOCK_LIMIT)
```

---

# 12. Null Safety

Use

```ts
?.
```

and

```ts
??
```

Example

```ts
customer?.address?.city

price ?? 0
```

---

# 13. Error Handling

Handle expected errors.

Never swallow exceptions.

❌ Bad

```ts
catch {}
```

✅ Good

```ts
catch(error){
    logger.error(error);
}
```

---

# 14. Async Code

Prefer

```ts
async
await
```

Do not mix

```ts
await
```

with

```ts
.then()
```

in the same flow.

---

# 15. Immutability

Prefer

```ts
const updated = {
    ...product,
    price: 100
};
```

Avoid mutating shared objects.

---

# 16. Readability

Write code for humans.

Prefer

```ts
calculateOrderTotal()
```

instead of

```ts
calc()
```

---

# 17. Imports

Order imports consistently.

```text
Angular

Third-party

Application

Relative
```

Remove unused imports.

---

# 18. Formatting

Use

- ESLint
- Prettier

Never manually align spacing.

Maximum line length

120 characters.

---

# 19. Logging

Never commit

```ts
console.log()
```

Use

```ts
LoggerService
```

or

```text
ILogger
```

depending on the project.

---

# 20. Security

Never

- Trust client input
- Store secrets in code
- Hardcode passwords
- Disable validation

Always validate input.

---

# 21. Performance

Avoid

- Unnecessary loops
- Duplicate API calls
- Large methods
- Expensive calculations inside loops

Cache when appropriate.

---

# 22. Code Organization

Organize methods logically.

Recommended order

1. Properties
2. Constructor / inject()
3. Lifecycle methods
4. Public methods
5. Protected methods
6. Private methods
7. Helper methods

---

# 23. Code Review Checklist

Before creating a Pull Request

- No `any`
- No `console.log`
- No dead code
- No commented-out code
- Proper naming
- Small functions
- Strong typing
- Error handling implemented
- Tests updated
- ESLint passes
- Prettier passes

---

# 24. Pull Request Guidelines

- One feature per PR
- Small PRs (<500 lines when possible)
- Clear title
- Clear description
- Link related issue
- Include screenshots for UI changes

---

# 25. Clean Code Rules

Always

- Write expressive code
- Keep methods small
- Keep classes focused
- Remove duplication
- Prefer composition over inheritance
- Refactor continuously

---

# Golden Rule

> Code is read far more often than it is written.

Write code so that another developer can understand it without additional explanation.