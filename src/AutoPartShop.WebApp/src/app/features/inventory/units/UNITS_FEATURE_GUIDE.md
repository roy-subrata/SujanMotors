# Units Management Feature Guide

## Overview
The Units Management feature provides a comprehensive interface for managing measurement units (e.g., Kilogram, Liter, Piece) in the AutoPartShop inventory system. Built with Angular 20, PrimeNG, and modern web technologies, it offers a professional, user-friendly enterprise-level experience.

## Features

### 1. **List View with Advanced DataTable**
- Display units in a responsive, paginated table
- Support for multiple columns: Name, Code, Symbol, Description, Status
- Checkboxes for bulk selection
- Inline status badges (Active/Inactive)
- Hover effects and visual feedback
- Empty state message when no units exist
- Loading indicators for better UX

### 2. **Search & Filter**
- Real-time search across name, code, and symbol
- Keyboard-optimized input field
- Clear search functionality
- Pagination support (5, 10, 20, 50 records per page)

### 3. **Create New Units**
- Modal dialog with form validation
- Required fields: Name, Code, Symbol
- Optional description field (max 500 characters)
- Real-time validation feedback
- Success/error notifications

### 4. **Edit Existing Units**
- Pre-populated form with current values
- All fields editable
- Status toggle (Active/Inactive)
- Display order management
- Update validation

### 5. **Delete Units**
- Confirmation dialog before deletion
- Prevents accidental deletions
- Error handling if unit has dependencies
- Success feedback after deletion

### 6. **Status Management**
- Quick toggle between Active/Inactive states
- Click-to-toggle on status badge in list
- Confirmation dialog for status changes
- Visual indicators for current status

## Component Architecture

```
units/
├── units.component.ts          # Main component (coordinator)
├── units.component.html        # Main template
├── units.component.css         # Main styles
│
├── units-header/
│   ├── units-header.component.ts
│   ├── units-header.component.html
│   └── units-header.component.css
│
├── units-list/
│   ├── units-list.component.ts
│   ├── units-list.component.html
│   └── units-list.component.css
│
├── units-form-dialog/
│   ├── units-form-dialog.component.ts
│   ├── units-form-dialog.component.html
│   └── units-form-dialog.component.css
│
└── UNITS_FEATURE_GUIDE.md     # This file
```

## Service Architecture

### UnitService (`unit.service.ts`)
Handles all API communications with the backend:

**Methods:**
- `getAllUnits()` - Get all units
- `getActiveUnits()` - Get active units only
- `getUnitById(id)` - Get specific unit
- `getListUnits(pageNumber, pageSize, searchTerm)` - Get paginated list with search
- `createUnit(request)` - Create new unit
- `updateUnit(id, request)` - Update unit
- `deleteUnit(id)` - Delete unit
- `activateUnit(id)` - Activate unit
- `deactivateUnit(id)` - Deactivate unit

**Interfaces:**
```typescript
UnitResponse {
  id: string;
  name: string;
  code: string;
  symbol: string;
  description: string;
  isActive: boolean;
  displayOrder: number;
  createdBy: string;
  modifiedBy: string;
}

CreateUnitRequest {
  name: string;
  code: string;
  symbol: string;
  description: string;
}

UpdateUnitRequest {
  name: string;
  code: string;
  symbol: string;
  description: string;
  isActive: boolean;
  displayOrder: number;
}

UnitListResponse {
  data: UnitResponse[];
  pagination: {
    pageNumber: number;
    pageSize: number;
    totalCount: number;
    totalPages: number;
  };
}
```

## Key Features Explained

### Validation
- **Client-side validation** using Angular Reactive Forms
- **Real-time feedback** for invalid fields
- **Visual indicators** for valid/invalid/untouched fields
- **Error messages** explaining validation rules

### Error Handling
- Try-catch blocks in all components
- User-friendly error messages
- Toast notifications for feedback
- Prevents duplicate entries (conflict handling)

### User Experience
- **Confirmation dialogs** for destructive actions
- **Loading states** during API calls
- **Disabled buttons** during operations
- **Visual feedback** for all interactions
- **Responsive design** for mobile devices
- **Accessibility features** (ARIA labels, tooltips)

### Performance
- **Lazy loading** of routes
- **Pagination** to reduce initial data load
- **OnPush change detection** (can be implemented)
- **Unsubscribe from observables** (RxJS best practices)

## How to Use

### 1. Navigate to Units Feature
```
http://localhost:4200/inventory/units
```

### 2. Create a New Unit
1. Click "New Unit" button in header
2. Fill in required fields (Name, Code, Symbol)
3. Optionally add description
4. Click "Create" button
5. Confirmation toast appears

### 3. Search Units
1. Use search box in header
2. Type name, code, or symbol
3. Results update in real-time
4. Click "X" to clear search

### 4. Edit a Unit
1. Click pencil icon (Edit) in table row
2. Form dialog opens with pre-filled data
3. Modify fields as needed
4. Click "Update" button
5. Confirmation toast appears

### 5. Delete a Unit
1. Click trash icon (Delete) in table row
2. Confirmation dialog appears
3. Click "Yes" to confirm deletion
4. Unit is deleted from system
5. List refreshes automatically

### 6. Toggle Unit Status
1. Click on the status badge (Active/Inactive) in table
2. Confirmation dialog appears
3. Click "Yes" to confirm status change
4. Status updates immediately
5. Visual indicator changes

## API Endpoints

All requests go to: `http://localhost:5292/api/units`

| Method | Endpoint | Purpose |
|--------|----------|---------|
| GET | `/` | Get all units |
| GET | `/active` | Get active units only |
| GET | `/list` | Get paginated list with search |
| GET | `/:id` | Get unit by ID |
| POST | `/` | Create new unit |
| PUT | `/:id` | Update unit |
| DELETE | `/:id` | Delete unit |
| PATCH | `/:id/activate` | Activate unit |
| PATCH | `/:id/deactivate` | Deactivate unit |

## Styling & Design System

### Colors
- **Primary**: Blue (#3b82f6)
- **Success**: Green (#10b981)
- **Warning**: Amber (#f59e0b)
- **Error**: Red (#ef4444)
- **Info**: Sky (#0284c7)

### Typography
- **Headers**: Bold, larger font sizes
- **Body**: Regular weight, 0.95rem base
- **Labels**: 600 weight, clear hierarchy

### Responsive Design
- **Desktop** (>768px): Full layout with all features
- **Tablet** (480-768px): Optimized spacing
- **Mobile** (<480px): Stacked layout, touch-friendly buttons

## Browser Support
- Chrome (latest)
- Firefox (latest)
- Safari (latest)
- Edge (latest)

## Accessibility
- ARIA labels on buttons
- Keyboard navigation support
- High contrast ratios
- Focus indicators
- Semantic HTML

## Future Enhancements

1. **Bulk Operations**
   - Bulk activate/deactivate
   - Bulk delete with confirmation

2. **Advanced Filtering**
   - Filter by status
   - Filter by creation date
   - Multi-column sorting

3. **Export Functionality**
   - Export to CSV
   - Export to Excel

4. **Unit Conversions UI**
   - Manage conversions between units
   - Visual conversion matrix

5. **Advanced Validation**
   - Duplicate code/name detection
   - Server-side validation display

6. **Analytics**
   - Usage statistics
   - Most-used units

## Troubleshooting

### Units not loading?
- Check if API server is running on `http://localhost:5292`
- Check browser console for errors
- Verify network connection

### Create/Update failing?
- Check all required fields are filled
- Check for validation errors (red indicators)
- Check if code/name already exists
- Check API response in Network tab

### Delete not working?
- Unit may have active conversions
- Check error message in toast
- Try deactivating before deleting

## Performance Tips

1. **Pagination**: Use appropriate page size (default: 10)
2. **Search**: Let API handle filtering
3. **Caching**: Consider implementing in service layer
4. **Debouncing**: Search input already debounced

## Security Considerations

- All inputs are sanitized by Angular
- HTTPS recommended for production
- No sensitive data in localStorage
- CSRF tokens (should be added for production)

## Code Examples

### Creating a Unit Programmatically
```typescript
const unit: CreateUnitRequest = {
  name: 'Kilogram',
  code: 'KG',
  symbol: 'kg',
  description: 'Unit of mass'
};

this.unitService.createUnit(unit).subscribe(
  (response) => console.log('Unit created:', response),
  (error) => console.error('Error:', error)
);
```

### Searching Units
```typescript
this.unitService.getListUnits(1, 10, 'kg').subscribe(
  (response) => {
    this.units = response.data;
    this.totalRecords = response.pagination.totalCount;
  }
);
```

## Support & Maintenance

For issues or enhancements:
1. Check the GitHub issues
2. Review API documentation
3. Check browser console for errors
4. Verify backend API is accessible

---

**Version**: 1.0.0
**Last Updated**: 2024
**Author**: Development Team
