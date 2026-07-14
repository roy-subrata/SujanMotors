# Angular Development Standards

## Purpose

This document defines the coding standards and best practices for Angular applications. Following these standards ensures consistency, maintainability, scalability, and performance across all Angular projects.

---

# 1. Project Structure

Organize the project **by feature**, not by file type.

## Recommended Structure

```text
src/
└── app/
    ├── core/
    │   ├── guards/
    │   ├── interceptors/
    │   ├── services/
    │   ├── models/
    │   └── utils/
    │
    ├── shared/
    │   ├── components/
    │   ├── directives/
    │   ├── pipes/
    │   ├── validators/
    │   └── utils/
    │
    ├── features/
    │   ├── products/
    │   │   ├── pages/
    │   │   ├── components/
    │   │   ├── services/
    │   │   ├── models/
    │   │   ├── store/
    │   │   └── product.routes.ts
    │   │
    │   └── customers/
    │       ├── pages/
    │       ├── components/
    │       ├── services/
    │       ├── models/
    │       └── customer.routes.ts
    │
    ├── app.routes.ts
    └── app.config.ts
```

### ✅ Do

- Organize by feature.
- Keep related files together.
- Lazy load every feature.

### ❌ Avoid

```text
components/
services/
models/
```

as global folders containing everything.

---

# 2. Standalone Components

Angular recommends standalone components for all new applications.

## Example

```ts
@Component({
  selector: 'app-product-list',
  standalone: true,
  imports: [],
  templateUrl: './product-list.component.html'
})
export class ProductListComponent {}
```

### Benefits

- No NgModules
- Simpler architecture
- Better lazy loading
- Easier testing

---

# 3. Lazy Load Every Feature

Every feature should be lazy loaded.

## Example

```ts
export const routes: Routes = [
  {
    path: 'products',
    loadChildren: () =>
      import('./features/products/product.routes')
  }
];
```

### Benefits

- Faster startup
- Smaller bundle size
- Better scalability

---

# 4. Folder Naming

Use kebab-case.

## Good

```text
customer-list
customer-detail
purchase-order
stock-adjustment
```

## Bad

```text
CustomerList
customerList
PurchaseOrder
```

---

# 5. File Naming

Use descriptive kebab-case names.

## Examples

```text
customer-list.component.ts
customer-list.component.html
customer-list.component.scss

customer.service.ts
customer-api.service.ts

customer.model.ts
customer.dto.ts

customer.routes.ts
```

---

# 6. Component Size

### Recommended

- 200–300 lines maximum.

### Split components when

- File exceeds 500 lines.
- Multiple responsibilities exist.
- HTML becomes difficult to understand.

---

# 7. Smart vs Presentational Components

## Smart Components

Responsible for:

- API calls
- State management
- Routing
- Business logic

Example

```text
ProductPageComponent
```

---

## Presentational Components

Responsible only for UI.

Example

```ts
@Input() product!: Product;

@Output() save = new EventEmitter<Product>();
```

### Rules

- No HTTP calls
- No routing
- No business logic

---

# 8. Services

Each service should have one responsibility.

## Good

```text
ProductApiService
ProductCacheService
ProductStateService
AuthenticationService
```

## Bad

```text
CommonService
UtilityService
HelperService
```

These become large and difficult to maintain.

---

# 9. Strong Typing

Never use `any`.

## Bad

```ts
products: any[];
```

## Good

```ts
products: ProductDto[];

response: ProductResponse;

request: ProductCreateRequest;
```

---

# 10. DTO Separation

Do not bind backend responses directly to UI.

Backend Response

```json
{
  "id": 1,
  "productName": "Oil Filter"
}
```

Create DTO

```ts
export interface ProductDto {
    id: number;
    productName: string;
}
```

Map DTO to UI model when necessary.

Benefits

- UI independence
- Easier refactoring
- Strong typing

---

# 11. Environment Configuration

Never hardcode URLs.

## Bad

```ts
'https://api.company.com'
```

## Good

```ts
environment.apiUrl
```

---

# 12. HTTP Layer

HTTP calls belong inside API services.

## Bad

```ts
this.http.get(...)
```

inside components.

## Good

```ts
ProductApiService
```

Components only call services.

---

# 13. Signals

Use Signals for local component state.

```ts
products = signal<Product[]>([]);
```

Use computed values

```ts
computed(() => ...)
```

Use effects

```ts
effect(() => ...)
```

Avoid unnecessary `BehaviorSubject` for local state.

---

# 14. RxJS Best Practices

Use

- `switchMap()` for searches
- `mergeMap()` for independent requests
- `concatMap()` for sequential execution
- `forkJoin()` for parallel requests
- `combineLatest()` for combining streams
- `takeUntilDestroyed()` for automatic cleanup

## Avoid

```ts
subscribe(() => {
    subscribe(() => {})
})
```

Instead

```ts
switchMap(...)
```

---

# 15. Dependency Injection

Prefer the `inject()` function.

```ts
private readonly productService = inject(ProductService);
```

Instead of constructor injection when appropriate.

---

# 16. Change Detection

Always use OnPush unless there is a specific reason not to.

```ts
changeDetection: ChangeDetectionStrategy.OnPush
```

Benefits

- Better performance
- Fewer change detection cycles

---

# 17. Reusable Components

Common reusable UI should live inside the Shared module.

Examples

- Table
- Button
- Modal
- Dropdown
- Dialog
- Confirmation Dialog
- Loading Spinner
- Empty State
- Pagination
- Search Box

Avoid duplicating UI.

---

# 18. Validation

Use Reactive Forms.

Create reusable validators.

Avoid placing validation logic inside components.

---

# 19. Routing

Each feature owns its routing.

Example

```text
products/
    product.routes.ts

customers/
    customer.routes.ts
```

Avoid one massive routing file.

---

# 20. State Management

Choose based on application complexity.

| Application Size | Recommendation |
|------------------|---------------|
| Small | Signals |
| Medium | Services + Signals |
| Large Enterprise | NgRx |

Only introduce NgRx when truly needed.

---

# 21. Error Handling

Use HTTP Interceptors for

- Authentication
- Authorization
- 401
- 403
- 404
- 500
- Logging
- Global error messages

Avoid repetitive

```ts
catchError()
```

inside every component.

---

# 22. ESLint

Enable rules such as

- no-explicit-any
- no-unused-vars
- consistent-type-imports
- prefer-readonly
- no-console

---

# 23. Prettier

Always use automatic formatting.

Never manually align spaces.

Format code before every commit.

---

# 24. Testing

Write

- Unit Tests
- Component Tests
- Integration Tests
- End-to-End Tests

Focus especially on business logic.

---

# 25. Naming Conventions

## Components

```text
ProductListComponent
CustomerDetailsComponent
```

## Services

```text
ProductService
ProductApiService
```

## Interfaces

```text
Product
Customer
```

## DTOs

```text
ProductDto
CustomerDto
```

## Enums

```text
ProductStatus
OrderStatus
```

## Constants

```text
MAX_UPLOAD_SIZE
DEFAULT_PAGE_SIZE
```

---

# 26. Barrel Files

Use `index.ts` to simplify imports.

Example

```text
components/
    button/
    dialog/
    index.ts
```

---

# 27. Avoid Business Logic in Components

## Bad

```ts
save() {
    // 300 lines of business logic
}
```

## Good

```ts
this.orderService.save(order);
```

Components should orchestrate UI only.

---

# 28. Security

- Never bypass Angular sanitization.
- Validate all user inputs.
- Never trust client-side validation.
- Avoid storing sensitive data in Local Storage.
- Prefer HttpOnly cookies when possible.
- Sanitize HTML when rendering user-generated content.

---

# 29. Performance

Follow these best practices:

- Lazy load every feature.
- Use `track` (Angular control flow) or `trackBy`.
- Use OnPush change detection.
- Use Signals for local state.
- Defer loading heavy components.
- Optimize images.
- Avoid unnecessary re-rendering.
- Cache API responses when appropriate.
- Split large components.
- Remove unused dependencies.

---

# General Principles

Always strive for:

- Readable code
- Maintainable architecture
- Reusable components
- Strong typing
- Single Responsibility Principle (SRP)
- Feature-based architecture
- Consistent naming
- Scalable folder structure
- Testable code
- High performance
- Secure implementation

---

# Summary Checklist

- ✅ Feature-based architecture
- ✅ Standalone components
- ✅ Lazy-loaded features
- ✅ Strong typing (no `any`)
- ✅ DTO separation
- ✅ Signals for local state
- ✅ Reactive Forms
- ✅ Reusable components
- ✅ OnPush change detection
- ✅ `inject()` dependency injection
- ✅ Environment configuration
- ✅ HTTP services
- ✅ Global error handling
- ✅ ESLint + Prettier
- ✅ Unit and E2E testing
- ✅ Security best practices
- ✅ Performance optimization
- ✅ Consistent naming conventions
- ✅ Small, focused components and services