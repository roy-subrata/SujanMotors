# AutoPartShop Layout Guide

## Project Structure Overview

Your Angular application now has a professional layout system with sidebar navigation and top bar. Here's the structure:

```
src/app/
├── shared/
│   ├── components/
│   │   ├── topbar/
│   │   │   ├── topbar.component.ts
│   │   │   ├── topbar.component.html
│   │   │   └── topbar.component.css
│   │   └── sidebar/
│   │       ├── sidebar.component.ts
│   │       ├── sidebar.component.html
│   │       └── sidebar.component.css
│   ├── layout/
│   │   ├── layout.component.ts
│   │   ├── layout.component.html
│   │   └── layout.component.css
│   └── index.ts (barrel export)
├── features/
│   ├── dashboard/
│   ├── inventory/
│   │   ├── categories/
│   │   ├── products/
│   │   └── stock/
│   ├── orders/
│   │   ├── orders-list/
│   │   └── orders-create/
│   ├── reports/
│   └── settings/
├── app.ts
├── app.routes.ts
└── app.config.ts
```

## Components Overview

### 1. **Topbar Component** (`shared/components/topbar/`)
Professional navigation bar with:
- **Logo & Branding** - AutoPartShop truck icon and name
- **Main Navigation Menu** - Dashboard, Inventory, Orders, Reports, Settings
- **Search Bar** - For quick search (hidden on mobile)
- **Notifications** - Bell icon with badge
- **User Menu** - Profile, Settings, and Logout options

**Key Features:**
- Responsive design with hidden elements on small screens
- Dark mode support
- PrimeNG Menu component with submenus
- Professional styling with Tailwind CSS

### 2. **Sidebar Component** (`shared/components/sidebar/`)
Collapsible drawer navigation with:
- **Comprehensive Menu** - All application sections
- **Nested Items** - Categorized menu items for better organization
- **Mobile Toggle** - Sidebar toggle button for mobile devices
- **Icons** - PrimeNG icons for visual hierarchy

**Features:**
- Smooth animations
- Dark mode support
- Responsive behavior (collapses on mobile)
- Professional hover effects and active states

### 3. **Layout Component** (`shared/layout/`)
Main application container that wraps:
- Topbar at the top
- Sidebar on the left
- Main content area with router outlet
- Responsive flex layout

## How to Use

### Basic Usage
The layout is already integrated into your app. All pages will automatically have:
- Top navigation bar
- Side navigation drawer
- Professional styling

### Adding New Pages
1. Create a new component in the `features/` folder:
```typescript
import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-example',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './example.component.html',
  styleUrl: './example.component.css'
})
export class ExampleComponent {}
```

2. Add route in `app.routes.ts`:
```typescript
{
  path: 'example',
  loadComponent: () => import('./features/example/example.component').then(m => m.ExampleComponent)
}
```

3. Add menu item in the appropriate component (topbar or sidebar):
```typescript
{
  label: 'Example',
  icon: 'pi pi-star',
  routerLink: ['/example']
}
```

### Customizing the Layout

#### Change Colors/Theme
Edit the `::ng-deep` styles in:
- `topbar.component.css`
- `sidebar.component.css`

Example custom colors:
```css
.p-menu-item > .p-menuitem-content:hover {
  background-color: #your-color;
  color: #your-text-color;
}
```

#### Modify Menu Items
Edit the `items` or `menuItems` signal in:
- `topbar.component.ts` - For top menu
- `sidebar.component.ts` - For sidebar menu

```typescript
menuItems = signal<MenuItem[]>([
  {
    label: 'Custom Item',
    icon: 'pi pi-star',
    routerLink: ['/custom']
  }
]);
```

#### Update Logo
In `topbar.component.html` and `sidebar.component.html`, modify:
```html
<i class="pi pi-truck text-2xl text-blue-600"></i>
<span class="font-bold">AutoPartShop</span>
```

## Available PrimeNG Components Used

- **Toolbar** - Top navigation bar container
- **Menu** - Navigation menu with submenus
- **Sidebar** - Drawer for mobile navigation
- **Button** - Action buttons with icons

## PrimeNG Icons

All icons use the `pi` class. Common icons used:
- `pi-home` - Dashboard
- `pi-box` - Inventory/Products
- `pi-shopping-cart` - Orders
- `pi-chart-bar` - Reports
- `pi-cog` - Settings
- `pi-user` - User profile
- `pi-sign-out` - Logout
- `pi-bell` - Notifications
- `pi-bars` - Menu toggle

Browse more at: https://primeng.org/icons

## Styling

### Tailwind CSS Classes Used
- `flex`, `flex-col` - Flexbox layouts
- `items-center`, `justify-between` - Flexbox alignment
- `gap-*` - Spacing between items
- `px-*`, `py-*` - Padding utilities
- `text-*` - Text sizes and colors
- `bg-*` - Background colors
- `border-*` - Border utilities
- `dark:*` - Dark mode variants
- `hover:*` - Hover states
- `rounded-*` - Border radius

### Dark Mode
Dark mode is automatically supported via Tailwind's dark mode feature. The layout respects system preferences.

## Responsive Design

The layout is fully responsive:
- **Desktop (lg+)**: Full sidebar, horizontal menu in topbar
- **Tablet (md)**: Collapsible sidebar, responsive menu
- **Mobile (sm)**: Drawer sidebar, minimal topbar

## Performance Tips

1. **Lazy Loading** - Routes use lazy loading for better performance
2. **Standalone Components** - All components are standalone for better tree-shaking
3. **OnPush Strategy** - Consider adding `ChangeDetectionStrategy.OnPush` for better performance:
   ```typescript
   @Component({
     ...
     changeDetection: ChangeDetectionStrategy.OnPush
   })
   ```

## Next Steps

1. **Add Authentication** - Implement login/logout functionality
2. **API Integration** - Connect to your Backend API endpoints
3. **Data Display** - Replace placeholder components with actual content
4. **Forms** - Add product/order management forms
5. **Dashboards** - Create analytics and statistics views

## Common Tasks

### Add Breadcrumb Navigation
Add to `layout.component.html` above main content

### Add Search Functionality
Enhance the search bar in `topbar.component.ts`

### Add User Avatar
Update user menu in `topbar.component.html`

### Add Mobile Responsive Sidebar
Sidebar is already responsive and uses p-drawer for mobile

## Troubleshooting

### Icons not showing
- Ensure PrimeNG icons are installed: `npm install primeicons`
- Check that `primeicons` CSS is imported in your styles

### Styling not applied
- Make sure Tailwind CSS is properly configured
- Clear browser cache and rebuild the project

### Routes not working
- Verify routes are defined in `app.routes.ts`
- Check component paths are correct
- Ensure lazy loading syntax is correct

## Resources

- [PrimeNG Documentation](https://primeng.org/)
- [Angular Documentation](https://angular.io/docs)
- [Tailwind CSS Documentation](https://tailwindcss.com/docs)
- [PrimeIcons](https://primeng.org/icons)

---

Happy coding! Your professional AutoPartShop inventory management system is ready to be built upon.
