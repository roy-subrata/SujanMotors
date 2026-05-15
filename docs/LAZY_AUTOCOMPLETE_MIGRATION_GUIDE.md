# Lazy Autocomplete Migration Guide

This guide provides step-by-step instructions for migrating all remaining `p-autoComplete` components to use `app-lazy-autocomplete` throughout the application.

## Completed Migrations

✅ **sales-return-form.component** - Sales Orders & Warehouses
✅ **customer-payment-form.component** - Customers & Payment Providers  
✅ **quick-sale.component** - Customers, Technicians, Payment Providers
✅ **quick-sale-shortcut.component** - Parts, Customers, Technicians (Already using lazy-autocomplete)

## Remaining Components to Migrate

### Priority 1 - Procurement Module

#### 1. **purchase-order-form.component**
**Location:** `src/app/features/procurement/purchase-orders/purchase-order-form/`
**Components using p-autoComplete:**
- Supplier selection (with search API)
- Parts selection (with search API)

**Steps:**
1. Update imports: Remove `AutoCompleteModule`, Add `LazyAutocompleteComponent`
2. Add lazy fetch functions in component.ts:
```typescript
fetchSuppliersLazy = (req: LazyRequest) =>
  this.supplierService.getSuppliers({
    search: req.search,
    pageNumber: req.pageNumber,
    pageSize: req.pageSize
  }).pipe(
    map(res => ({
      items: res.data,
      totalCount: res.pagination.totalCount
    } as LazyResponse<SupplierResponse>))
  );

fetchPartsLazy = (req: LazyRequest) =>
  this.partService.getParts({
    search: req.search,
    pageNumber: req.pageNumber,
    pageSize: req.pageSize
  }).pipe(
    map(res => ({
      items: res.data,
      totalCount: res.pagination.totalCount
    } as LazyResponse<PartResponse>))
  );
```
3. Remove old filter methods and filtered arrays
4. Update HTML templates with `app-lazy-autocomplete` tags
5. Remove `(completeMethod)` and suggestions bindings

#### 2. **purchase-orders-form-dialog.component**
**Location:** `src/app/features/procurement/purchase-orders/purchase-orders-form-dialog/`
**Similar pattern to purchase-order-form**

#### 3. **goods-receipt-form.component**
**Location:** `src/app/features/procurement/goods-receipts/`
**Uses:** Supplier & Purchase Order selection

#### 4. **purchase-returns-form.component**
**Location:** `src/app/features/procurement/purchase-returns/purchase-returns-form/`
**Uses:** Supplier & Purchase Order selection

#### 5. **supplier-payment-form.component**
**Location:** `src/app/features/procurement/supplier-payment/`
**Uses:** Supplier & Invoice selection

#### 6. **payment-provider-form.component**
**Location:** `src/app/features/procurement/payment-provider/`
**Uses:** Category/Type selection (static)

### Priority 2 - Inventory Module

#### 7. **part-form.component**
**Location:** `src/app/features/inventory/parts/part-form/`
**Uses:** 
- Category selection (pageable if large dataset)
- Unit selection (pageable if large dataset)
- Brand selection (pageable if large dataset)

#### 8. **vehicle-form.component**
**Location:** `src/app/features/inventory/vehicles/`
**Uses:** 
- Brand/Make selection
- Unit/Model selection
- Category selection

#### 9. **vehicle-compatibility.component**
**Location:** `src/app/features/inventory/vehicles/`
**Uses:** Vehicle & Part compatibility mapping

#### 10. **suppliers-form-dialog.component**
**Location:** `src/app/features/inventory/suppliers/suppliers-form-dialog/`
**Uses:** Static dropdowns/selections

### Priority 3 - Other Modules

#### 11. **warranty/claims-list.component**
**Location:** `src/app/features/warranty/claims-list/`
**Uses:** Customer & Product selection (if used for filtering)

## Migration Pattern Template

### TypeScript Changes
```typescript
// OLD
filteredItems: T[] = [];

// NEW - Add lazy fetch function
fetchItemsLazy = (req: LazyRequest) =>
  this.service.getItems({
    search: req.search,
    pageNumber: req.pageNumber,
    pageSize: req.pageSize
  }).pipe(
    map(res => ({
      items: res.data,
      totalCount: res.pagination.totalCount
    } as LazyResponse<T>))
  );

// Remove old methods:
// - filterItems(event)
// - loadInitialItems()
// - All filter-related operations
```

### HTML Changes
```html
<!-- OLD -->
<p-autoComplete 
  [suggestions]="filteredItems" 
  (completeMethod)="filterItems($event)"
  (onSelect)="selectItem($event.value)"
  field="name" 
  optionLabel="name"
  [dropdown]="true"
  [showClear]="true">
  <ng-template let-item pTemplate="item">
    <!-- template -->
  </ng-template>
</p-autoComplete>

<!-- NEW -->
<app-lazy-autocomplete 
  [fetchFn]="fetchItemsLazy"
  (onItemSelect)="selectItem($event)"
  field="name" 
  optionLabel="name"
  [showClear]="true"
  [minLength]="0">
  <ng-template let-item>
    <!-- template -->
  </ng-template>
</app-lazy-autocomplete>
```

### Imports Update
```typescript
// Remove
import { AutoCompleteModule } from 'primeng/autocomplete';
// imports: [ AutoCompleteModule, ... ]

// Add
import { LazyAutocompleteComponent, LazyRequest, LazyResponse } from 'path/to/lazy-autocomplete';
import { map } from 'rxjs/operators';
// imports: [ LazyAutocompleteComponent, ... ]
```

## Key Differences

| Aspect | p-autoComplete | app-lazy-autocomplete |
|--------|-----------------|----------------------|
| **Data Loading** | All at once, then filtered | Paginated on demand |
| **API Calls** | Manual (completeMethod) | Automatic via fetchFn |
| **Template** | `pTemplate="item"` | `ng-template let-item` |
| **Event Binding** | `(onSelect)="method($event.value)"` | `(onItemSelect)="method($event)"` |
| **Dropdown** | `[dropdown]="true"` | Not needed, always has a dropdown |
| **Suggestions** | `[suggestions]="array"` | Not needed, uses fetchFn |

## Tips & Best Practices

1. **Paginated APIs**: Always use lazy-autocomplete for paginated endpoints
   ```typescript
   fetchSuppliersLazy = (req: LazyRequest) =>
     this.supplierService.getSuppliers({  // Must return { data[], pagination }
       search: req.search,
       pageNumber: req.pageNumber,
       pageSize: req.pageSize
     }).pipe(...)
   ```

2. **Static Lists**: For small static lists, still use lazy-autocomplete for consistency
   ```typescript
   fetchCategoriesLazy = (req: LazyRequest) => of({
     items: this.categories.filter(c => !req.search || 
       c.name.toLowerCase().includes(req.search.toLowerCase())),
     totalCount: this.categories.length
   });
   ```

3. **Form Control Integration**: Works with both reactive forms and ngModel
   ```typescript
   // Reactive Forms
   <app-lazy-autocomplete formControlName="supplierId" ...>

   // ngModel
   <app-lazy-autocomplete [(ngModel)]="selectedSupplier" [ngModelOptions]="{standalone: true}" ...>
   ```

4. **Custom Templates**: Fully supported, just remove `pTemplate="item"`
   ```html
   <ng-template let-item>
     <div class="custom">{{ item.name }}</div>
   </ng-template>
   ```

5. **Disable State**: Use standard Angular disabled
   ```html
   <app-lazy-autocomplete [disabled]="isEditing" ...>
   ```

## Service Requirements

Ensure your services have paginated list endpoints with this signature:
```typescript
interface PaginatedResponse<T> {
  data: T[];
  pagination: {
    pageNumber: number;
    pageSize: number;
    totalCount: number;
    totalPages: number;
  };
}

getItems(query: { search?: string; pageNumber: number; pageSize: number }): Observable<PaginatedResponse<T>>
```

## Verification Checklist

After migrating each component:
- [ ] Remove `AutoCompleteModule` from imports
- [ ] Add `LazyAutocompleteComponent` to imports  
- [ ] Remove all `filtered*` arrays and signals
- [ ] Remove all `filter*` methods
- [ ] Add `fetchItemsLazy` function
- [ ] Update HTML templates
- [ ] Remove `(completeMethod)` bindings
- [ ] Remove `[suggestions]` bindings
- [ ] Update `(onSelect)` to `(onItemSelect)`
- [ ] Remove `[dropdown]="true"`
- [ ] Remove `pTemplate="item"` from ng-template
- [ ] Test the component in the browser
- [ ] Test search functionality
- [ ] Test pagination (scroll/load more)
- [ ] Test selection
- [ ] Test clearing values

## Additional Resources

- **Lazy Autocomplete Component:** `src/app/shared/components/lazy-autocomplete/`
- **Usage Examples:** 
  - `quick-sale-shortcut.component.html` (Parts, Customers, Technicians)
  - `sales-return-form.component.ts/html` (Sales Orders, Warehouses)
