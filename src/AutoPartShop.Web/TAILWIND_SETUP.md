# Tailwind CSS Integration Guide - AutoParts Shop Web

## Overview

This guide provides complete setup instructions for the Tailwind CSS integration with Blazor components and MudBlazor. The project now features an enterprise-grade UI with a minimalist design.

## What's Been Integrated

### 1. **Tailwind CSS Configuration**
- **File**: `tailwind.config.js`
- Extended color palette with custom `primary` and `dark` color schemes
- Custom spacing variables for sidebar layout
- Enhanced box shadows for modern UI
- Pre-configured typography

### 2. **PostCSS Configuration**
- **File**: `postcss.config.js`
- Autoprefixer for cross-browser compatibility
- Tailwind CSS processing

### 3. **CSS Input File**
- **File**: `Styles/input.css`
- Tailwind directives (@tailwind base, components, utilities)
- Custom utility classes (buttons, cards, badges, inputs)
- Global styles and animations

### 4. **NPM Configuration**
- **File**: `package.json`
- Build and watch scripts for Tailwind CSS
- Dev dependencies included

### 5. **Updated Blazor Components**

#### App.razor
- Added Tailwind CSS stylesheet link
- Included Inter and Fira Code fonts from Google Fonts
- Maintains existing app structure

#### MainLayout.razor
- Complete redesign with modern flex-based layout
- Fixed header with sticky positioning
- Responsive sidebar (hidden on mobile, visible on md+ breakpoints)
- Mobile menu toggle with overlay
- Logo and branding section
- Header actions (search, notifications, user profile)
- Main content area with padding

#### NavMenu.razor
- Multilevel navigation with collapsible submenus
- 6 main categories with dropdowns:
  - Dashboard
  - Inventory Management (All Parts, Categories, Stock, Suppliers)
  - Sales & Orders (Orders, Returns, Invoices)
  - Customers
  - Reports & Analytics (Sales, Inventory, Revenue, Customer)
  - Settings (Company, Users, Tax, General)
  - Help & Support
- Logout button in footer
- Smooth animations and transitions
- Active state highlighting

#### Home.razor (Dashboard)
- Enterprise-grade dashboard with 4 key metric cards
- Sales trend chart with gradient visualization
- Top products section
- Recent orders table with status badges
- Quick stats with alerts
- Responsive grid layout
- Color-coded metrics (success, warning, danger, primary)

## Installation & Setup

### Step 1: Install Node.js Dependencies

```bash
cd src/AutoPartShop.Web
npm install
```

This will install:
- tailwindcss@^3.4.1
- postcss@^8.4.32
- autoprefixer@^10.4.17

### Step 2: Generate Tailwind CSS

Run the build command to generate the initial CSS file:

```bash
npm run tailwind:build
```

This creates `wwwroot/tailwind.css` from `Styles/input.css`.

### Step 3: Start Development

For development with hot-reload:

```bash
npm run dev
```

This runs `npm run tailwind:watch` which monitors changes and regenerates CSS automatically.

### Step 4: Run the Blazor Application

In another terminal:

```bash
cd src/AutoPartShop.Web
dotnet run
```

Visit: https://localhost:7020 or http://localhost:5109

## Project Structure

```
AutoPartShop.Web/
├── Components/
│   ├── Layout/
│   │   ├── MainLayout.razor          (Main layout with header & sidebar)
│   │   └── NavMenu.razor             (Multilevel navigation menu)
│   └── Pages/
│       └── Home.razor                (Enterprise dashboard)
├── Styles/
│   └── input.css                     (Tailwind configuration & utilities)
├── wwwroot/
│   ├── tailwind.css                  (Generated - Do not edit)
│   ├── app.css                       (Legacy - keep for compatibility)
│   └── [other assets]
├── package.json                      (Node.js dependencies)
├── tailwind.config.js                (Tailwind configuration)
├── postcss.config.js                 (PostCSS plugins)
├── AutoPartShop.Web.csproj           (.NET project file)
├── Program.cs                        (Blazor startup)
└── App.razor                         (Root component)
```

## Key Features

### Color System

**Primary Colors** (Sky Blue):
- `primary-50` through `primary-900` (gradient)
- Used for actions, highlights, and primary UI elements

**Dark Colors** (Professional Gray):
- `dark-50` through `dark-900` (gradient)
- Used for backgrounds, text, and neutral elements

### Spacing & Layout

- **Sidebar**: `w-sidebar-expanded` (280px width)
- **Responsive breakpoints**: sm (640px), md (768px), lg (1024px), xl (1280px), 2xl (1536px)
- **Gap utilities**: Consistent spacing throughout

### Custom Component Classes

#### Buttons
- `.btn-primary` - Blue action button
- `.btn-secondary` - Gray secondary button
- `.btn-outline` - Outlined variant

#### Cards
- `.card` - Main card component with shadow
- `.card-sm` - Smaller card variant

#### Badges
- `.badge` - Base badge
- `.badge-primary` - Blue badge
- `.badge-success` - Green badge
- `.badge-warning` - Yellow badge
- `.badge-danger` - Red badge

#### Forms
- `.input-field` - Styled input with focus ring

### Animations

- `animate-slide-in` - Slide in from left
- `animate-slide-out` - Slide out to left
- `animate-fade-in` - Fade in animation

## Responsive Behavior

### Mobile (< 640px)
- Sidebar hidden, accessible via hamburger menu
- Header adapts with simplified layout
- Search bar hidden
- User info text hidden

### Tablet (640px - 1024px)
- Sidebar visible
- Full header layout
- Two-column grid layouts

### Desktop (> 1024px)
- Full sidebar with text
- Complete header with all actions
- Multi-column grid layouts

## Customization

### Add New Colors

Edit `tailwind.config.js` in the `theme.extend.colors` section:

```javascript
colors: {
  accent: {
    50: '#f0fdf4',
    // ... other shades
    900: '#064e3b',
  }
}
```

### Modify Sidebar Width

Edit `tailwind.config.js` in `theme.extend.spacing`:

```javascript
'sidebar-expanded': '300px', // Change from 280px
```

### Add Custom Utilities

Edit `Styles/input.css` in the `@layer components` section:

```css
@layer components {
  .btn-custom {
    @apply px-6 py-3 bg-custom-color rounded-xl font-bold;
  }
}
```

## Tailwind CSS Output File

The generated `wwwroot/tailwind.css` file contains:
- Base styles (reset, typography)
- Component utilities
- Custom utility classes
- Responsive variants
- Dark mode variants (if enabled)

**Do not manually edit this file** - it's auto-generated. Modify only `Styles/input.css`.

## Integration with MudBlazor

For future MudBlazor integration:

1. Install MudBlazor NuGet package
2. Add MudBlazor theme configuration to `tailwind.config.js`
3. Ensure color system is compatible with Mud component themes
4. Test component styling and make adjustments as needed

## Build for Production

When deploying:

```bash
npm run tailwind:build
```

This creates an optimized, minified `tailwind.css` file.

## Troubleshooting

### Tailwind Classes Not Appearing

1. Verify `tailwind.config.js` content paths include your files
2. Rebuild CSS: `npm run tailwind:build`
3. Clear browser cache (Ctrl+Shift+Delete)
4. Check browser DevTools for stylesheet loading

### CSS Conflicts

1. Check specificity in `Styles/input.css`
2. Ensure scoped Blazor CSS doesn't override Tailwind
3. Use `!important` sparingly in custom styles

### Watch Not Working

1. Stop the process (Ctrl+C)
2. Reinstall dependencies: `rm -r node_modules && npm install`
3. Restart watch: `npm run dev`

## Browser Support

Tailwind CSS supports all modern browsers:
- Chrome/Edge: Latest 2 versions
- Firefox: Latest 2 versions
- Safari: Latest 2 versions
- IE: Not supported

## Performance Tips

1. Use utility classes directly instead of creating custom classes when possible
2. Remove unused CSS with Tailwind's purge configuration (already set)
3. Minimize component-scoped CSS in `.razor.css` files
4. Use async/defer on script tags

## Next Steps

1. ✅ Create additional pages for Inventory Management
2. ✅ Build Orders and Sales management pages
3. ✅ Implement Customer management interface
4. ✅ Create Reports & Analytics pages
5. ✅ Add Settings and configuration pages
6. ✅ Integrate actual data with API
7. ✅ Add authentication/authorization UI
8. ✅ Implement real charts with Chart.js or similar

## Resources

- [Tailwind CSS Documentation](https://tailwindcss.com/docs)
- [Tailwind CSS Configuration](https://tailwindcss.com/docs/configuration)
- [Tailwind CSS Utilities](https://tailwindcss.com/docs/utility-first)
- [Blazor Documentation](https://learn.microsoft.com/en-us/aspnet/core/blazor/)

## Support & Notes

For issues or questions:
1. Check Tailwind documentation for utility syntax
2. Review custom styles in `Styles/input.css`
3. Inspect elements in browser DevTools
4. Verify npm dependencies are installed

---

**Last Updated**: November 2024
**Tailwind Version**: 3.4.1
**Target Framework**: .NET 9.0
