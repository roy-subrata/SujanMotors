# Unit Conversions Feature Guide

## Overview
The Unit Conversions feature provides a comprehensive interface for managing conversion factors between different measurement units (e.g., converting kilograms to grams, liters to milliliters). Built with Angular 20, PrimeNG, and Reactive Forms, it offers professional, user-friendly enterprise-level experience.

## Features

### 1. **Conversions List View**
- Display all unit conversions in a responsive, paginated table
- Show conversion details: From Unit → To Unit | Conversion Factor | Description
- Visual display of conversion formula (e.g., "1 kg = 1000 g")
- Click-to-toggle status badges (Active/Inactive)
- Inline action buttons (Edit, Delete)
- Search conversions by unit names or codes
- Pagination with configurable page size
- Empty state messaging

### 2. **Create New Conversions**
- Modal dialog with smart unit selection
- Dual dropdowns for "From Unit" and "To Unit"
- Real-time validation preventing same unit selection
- Conversion factor input with precision support
- Optional description field
- Visual arrow indicator showing conversion direction
- Success notifications after creation

### 3. **Edit Conversions**
- Pre-populated form with read-only unit selection
- Editable conversion factor
- Status toggle (Active/Inactive)
- Update validation
- Confirmation feedback

### 4. **Delete Conversions**
- Confirmation dialog with unit pair display
- Prevents accidental deletions
- Success feedback after deletion
- Error handling

### 5. **Status Management**
- Quick toggle between Active/Inactive states
- Click-to-toggle on status badge
- Confirmation dialog for status changes
- Visual status indicators

### 6. **Search & Filter**
- Real-time search across from/to unit names and codes
- Keyboard-optimized search input
- Clear search functionality
- Client-side filtering for instant results

## Component Architecture

```
units/
├── conversions-header/
│   ├── conversions-header.component.ts
│   ├── conversions-header.component.html
│   └── conversions-header.component.css
│
├── conversions-list/
│   ├── conversions-list.component.ts
│   ├── conversions-list.component.html
│   └── conversions-list.component.css
│
├── conversions-form-dialog/
│   ├── conversions-form-dialog.component.ts
│   ├── conversions-form-dialog.component.html
│   └── conversions-form-dialog.component.css
│
└── CONVERSIONS_FEATURE_GUIDE.md  # This file
```

## Service Architecture

### UnitConversionService (`unit-conversion.service.ts`)
Handles all API communications for conversions:

**Methods:**
- `getAllConversions()` - Get all conversions
- `getConversionsForUnit(unitId)` - Get conversions for specific unit
- `getConversion(fromUnitId, toUnitId)` - Get specific conversion
- `createConversion(request)` - Create new conversion
- `updateConversion(id, request)` - Update conversion
- `deleteConversion(id)` - Delete conversion

**Interfaces:**
```typescript
UnitConversionResponse {
  id: string;
  fromUnitId: string;
  toUnitId: string;
  fromUnitName: string;
  fromUnitCode: string;
  toUnitName: string;
  toUnitCode: string;
  conversionFactor: number;
  description: string;
  isActive: boolean;
  createdBy: string;
  modifiedBy: string;
}

CreateUnitConversionRequest {
  fromUnitId: string;
  toUnitId: string;
  conversionFactor: number;
  description: string;
}

UpdateUnitConversionRequest {
  conversionFactor: number;
  description: string;
  isActive: boolean;
}
```

## Key Features Explained

### Conversion Formula Display
- Shows conversion factor with visual arrow: "1 kg → 1000 g"
- Helps users understand the conversion direction
- Displayed in formula card with colored background

### Validation Rules
1. **Unit Selection Validation**
   - From Unit and To Unit must be different
   - Both units must exist in the system
   - Real-time validation with error messages

2. **Conversion Factor Validation**
   - Must be greater than 0
   - Supports decimal values (e.g., 0.001)
   - No negative numbers allowed
   - Minimum value: 0.000001

3. **Description Validation**
   - Optional field
   - Maximum 500 characters
   - Character count displayed

### User Experience
- **Confirmation dialogs** for all destructive actions
- **Loading states** during API calls
- **Disabled buttons** during operations
- **Visual feedback** for all interactions
- **Responsive design** for mobile devices
- **Accessibility features** (ARIA labels, tooltips)

### Data Display
- **Conversion Matrix**: Shows all unit conversions clearly
- **Unit Information**: Displays both name and code
- **Conversion Formula**: Visual representation of conversion
- **Status Indicators**: Quick visual status check

## How to Use

### 1. Navigate to Conversions
1. Go to Units management page: `http://localhost:4200/inventory/units`
2. Click the "Conversions" tab

### 2. Create a New Conversion
1. Click "New Conversion" button in header
2. Select "From Unit" (e.g., Kilogram)
3. Select "To Unit" (e.g., Gram)
4. Enter "Conversion Factor" (e.g., 1000 for kg to g)
5. Optionally add description
6. Click "Create" button
7. Confirmation toast appears

### 3. Search Conversions
1. Use search box in header
2. Type unit name or code
3. Results update in real-time
4. Click "X" to clear search

### 4. Edit Conversion
1. Click pencil icon (Edit) in table row
2. Form dialog opens with pre-filled data
3. Modify conversion factor and description
4. Toggle status if needed
5. Click "Update" button
6. Confirmation toast appears

### 5. Delete Conversion
1. Click trash icon (Delete) in table row
2. Confirmation dialog shows conversion pair
3. Click "Yes" to confirm deletion
4. Conversion is deleted
5. List refreshes automatically

### 6. Toggle Status
1. Click on status badge (Active/Inactive) in table
2. Confirmation dialog appears
3. Click "Yes" to confirm
4. Status updates immediately

## API Endpoints

All requests go to: `http://localhost:5292/api/units`

| Method | Endpoint | Purpose |
|--------|----------|---------|
| GET | `/conversions/all` | Get all conversions |
| GET | `/:id/conversions` | Get conversions for unit |
| GET | `/conversions/:fromId/to/:toId` | Get specific conversion |
| POST | `/conversions` | Create new conversion |
| PUT | `/conversions/:id` | Update conversion |
| DELETE | `/conversions/:id` | Delete conversion |

## Design System

### Colors
- **Primary**: Purple (#667eea)
- **Success**: Green (#10b981)
- **Warning**: Amber (#f59e0b)
- **Danger**: Red (#ef4444)
- **Info**: Blue (#0284c7)
- **Background**: Light Gray (#f3f4f6)

### Header Styling
- **Gradient**: Pink to Red (#f093fb to #f5576c)
- **Icon**: Arrows-H (↔) for conversions
- **Title**: Large, bold typography

### Table Styling
- **Headers**: Light gray background (#f9fafb)
- **Rows**: White background with hover effect
- **Borders**: Subtle light gray
- **Status Badges**: Colored with icons

### Form Dialog
- **Fields**: Arranged vertically
- **Validation**: Real-time feedback
- **Visual Indicators**: Success (green), Error (red)
- **Conversion Display**: Animated arrow icon

## Conversion Examples

### Length Conversions
```
1 Kilometer = 1000 Meters
1 Meter = 100 Centimeters
1 Centimeter = 10 Millimeters
```

### Weight Conversions
```
1 Kilogram = 1000 Grams
1 Gram = 1000 Milligrams
1 Pound = 0.453592 Kilograms
```

### Volume Conversions
```
1 Liter = 1000 Milliliters
1 Gallon = 3.78541 Liters
1 Milliliter = 1 Cubic Centimeter
```

## Advanced Features

### Search Capabilities
Search by:
- From Unit name (e.g., "Kilogram")
- From Unit code (e.g., "KG")
- To Unit name (e.g., "Gram")
- To Unit code (e.g., "G")

### Status Management
- **Active**: Conversion is available for use
- **Inactive**: Conversion is disabled but retained
- Toggle status without deleting data

### Error Handling
- **Duplicate Prevention**: Cannot create same conversion twice
- **Unit Validation**: Both units must exist
- **Dependency Checking**: Shows if issues with units
- **User-Friendly Messages**: Clear error descriptions

## Performance Considerations

1. **Data Loading**: Conversions loaded on tab access
2. **Search**: Client-side filtering for instant results
3. **Pagination**: Improves table performance with large datasets
4. **Caching**: Consider implementing service-level caching
5. **Lazy Loading**: Tab content lazy loads when accessed

## Browser Support
- Chrome (latest)
- Firefox (latest)
- Safari (latest)
- Edge (latest)

## Accessibility
- ARIA labels on interactive elements
- Keyboard navigation support
- High contrast ratios
- Focus indicators
- Semantic HTML

## Responsive Design
- **Desktop** (>1024px): Full layout with all features
- **Tablet** (768-1024px): Optimized spacing and text sizes
- **Mobile** (<768px): Stacked layout, touch-friendly buttons

## Future Enhancements

1. **Conversion Calculator**
   - Input amount in from unit
   - Calculate conversion to target unit
   - Display result with formula

2. **Bulk Operations**
   - Bulk activate/deactivate
   - Bulk delete with confirmation

3. **Conversion History**
   - Track conversion factor changes
   - Audit trail of modifications

4. **Smart Suggestions**
   - Suggest common conversions
   - Auto-populate conversion factors

5. **Import/Export**
   - Import conversions from CSV
   - Export conversion table to Excel

6. **Unit Conversion Chain**
   - Automatically calculate indirect conversions
   - Find optimal conversion path

## Troubleshooting

### Conversions not loading?
- Check if API server is running on `http://localhost:5292`
- Check browser console for errors
- Verify network connection

### Cannot create conversion?
- Ensure both units are selected and different
- Check conversion factor is valid number > 0
- Verify conversion doesn't already exist

### Search not working?
- Try clearing search and retry
- Search is case-insensitive
- Works with partial matches

### Delete not working?
- Ensure conversion exists
- Check if dependent records exist
- Verify API permissions

## Code Examples

### Creating a Conversion Programmatically
```typescript
const conversionRequest: CreateUnitConversionRequest = {
  fromUnitId: '550e8400-e29b-41d4-a716-446655440000',
  toUnitId: '6ba7b810-9dad-11d1-80b4-00c04fd430c8',
  conversionFactor: 1000,
  description: 'Standard kilogram to gram conversion'
};

this.conversionService.createConversion(conversionRequest).subscribe(
  (response) => console.log('Conversion created:', response),
  (error) => console.error('Error:', error)
);
```

### Searching Conversions
```typescript
this.conversionsListComponent.search('kg');
// Shows only conversions involving KG units
```

## Integration with Units Tab

The Conversions tab works seamlessly with the Units tab:

1. **Shared Context**: Both tabs use same Units dropdown
2. **Dependent Creation**: Create units first, then conversions
3. **Related Operations**: Delete unit removes its conversions
4. **Synchronized State**: Both tabs stay in sync

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
