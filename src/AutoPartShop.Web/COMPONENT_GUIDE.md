# UI Component & Tailwind CSS Quick Reference

## Quick Start Commands

```bash
# Install dependencies
npm install

# Start development (watch mode)
npm run dev

# Build CSS for production
npm run tailwind:build

# Run Blazor app
dotnet run
```

## Component Examples

### Buttons

```html
<!-- Primary Button -->
<button class="btn-primary">
  <svg class="w-5 h-5 inline-block mr-2"></svg>
  Button Text
</button>

<!-- Secondary Button -->
<button class="btn-secondary">Secondary Action</button>

<!-- Outline Button -->
<button class="btn-outline">Outlined Action</button>
```

### Cards

```html
<!-- Large Card -->
<div class="card">
  <h3 class="text-lg font-bold">Card Title</h3>
  <p class="text-dark-600 mt-2">Card content here</p>
</div>

<!-- Small Card -->
<div class="card-sm">
  <p>Compact content</p>
</div>

<!-- Card with Icon -->
<div class="card group">
  <div class="flex items-center justify-between">
    <div>
      <p class="text-dark-500 text-sm">Metric</p>
      <p class="text-3xl font-bold">$1,234</p>
    </div>
    <div class="w-12 h-12 bg-primary-100 rounded-lg flex items-center justify-center group-hover:bg-primary-200">
      <svg class="w-6 h-6 text-primary-600"></svg>
    </div>
  </div>
</div>
```

### Badges

```html
<!-- Primary Badge -->
<span class="badge-primary">Primary</span>

<!-- Success Badge -->
<span class="badge-success">Completed</span>

<!-- Warning Badge -->
<span class="badge-warning">In Progress</span>

<!-- Danger Badge -->
<span class="badge-danger">Failed</span>
```

### Form Inputs

```html
<!-- Text Input -->
<input type="text" placeholder="Enter text..." class="input-field" />

<!-- With Icon -->
<div class="relative">
  <input type="text" placeholder="Search..." class="input-field pl-10" />
  <svg class="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-dark-400"></svg>
</div>

<!-- Select Dropdown -->
<select class="input-field">
  <option>Option 1</option>
  <option>Option 2</option>
</select>
```

### Tables

```html
<div class="overflow-x-auto">
  <table class="w-full table-striped table-hover">
    <thead>
      <tr class="border-b border-dark-200">
        <th class="text-left py-3 px-4 font-semibold">Header 1</th>
        <th class="text-left py-3 px-4 font-semibold">Header 2</th>
      </tr>
    </thead>
    <tbody>
      <tr class="border-b border-dark-100 hover:bg-primary-50">
        <td class="py-4 px-4">Cell 1</td>
        <td class="py-4 px-4">Cell 2</td>
      </tr>
    </tbody>
  </table>
</div>
```

### Navigation Menu Item

```html
<!-- Simple Link -->
<NavLink href="/page" class="nav-menu-item">
  <div class="flex items-center space-x-3">
    <svg class="w-5 h-5"></svg>
    <span class="text-sm font-medium">Menu Item</span>
  </div>
</NavLink>

<!-- Collapsible Submenu -->
<button class="w-full nav-menu-item flex items-center justify-between"
        onclick="toggleSubmenu(event, 'menu-id')">
  <div class="flex items-center space-x-3">
    <svg class="w-5 h-5"></svg>
    <span class="text-sm font-medium">Parent Menu</span>
  </div>
  <svg class="w-4 h-4 transition-transform submenu-arrow"></svg>
</button>

<div id="menu-id" class="submenu hidden pl-4 mt-2 space-y-2 border-l-2 border-primary-600/30">
  <NavLink href="/submenu" class="submenu-item">
    <span class="text-sm">Submenu Item</span>
  </NavLink>
</div>
```

## Layout Utilities

### Flexbox

```html
<!-- Flex Container -->
<div class="flex items-center justify-between space-x-4">
  <div>Left content</div>
  <div>Right content</div>
</div>

<!-- Column Layout -->
<div class="flex flex-col space-y-4">
  <div>Top</div>
  <div>Middle</div>
  <div>Bottom</div>
</div>
```

### Grid

```html
<!-- Responsive Grid -->
<div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
  <div class="card">Column 1</div>
  <div class="card">Column 2</div>
  <div class="card">Column 3</div>
  <div class="card">Column 4</div>
</div>
```

### Spacing

```html
<!-- Padding -->
<div class="p-6">Full padding</div>
<div class="px-4 py-6">Horizontal & vertical</div>

<!-- Margin -->
<div class="mt-6 mb-4">Top & bottom margin</div>

<!-- Gap (for flex/grid) -->
<div class="flex gap-4">Items with gap</div>
```

## Color Classes

### Text Colors

```html
<!-- Primary Text -->
<p class="text-primary-600">Primary text</p>

<!-- Dark Colors -->
<p class="text-dark-900">Dark text</p>
<p class="text-dark-500">Medium gray</p>
<p class="text-dark-300">Light gray</p>

<!-- Status Colors -->
<p class="text-green-600">Success</p>
<p class="text-red-600">Danger</p>
<p class="text-yellow-600">Warning</p>
<p class="text-blue-600">Info</p>
```

### Background Colors

```html
<div class="bg-primary-600">Primary background</div>
<div class="bg-dark-900">Dark background</div>
<div class="bg-primary-100">Light primary</div>
<div class="bg-success-100">Light success</div>
```

## Typography

```html
<!-- Headings -->
<h1 class="text-4xl font-bold">Heading 1</h1>
<h2 class="text-3xl font-bold">Heading 2</h2>
<h3 class="text-2xl font-bold">Heading 3</h3>
<h4 class="text-xl font-bold">Heading 4</h4>

<!-- Text Styles -->
<p class="text-sm font-medium">Small medium text</p>
<p class="text-lg font-light">Large light text</p>
<p class="font-semibold">Semibold text</p>
```

## Responsive Design

```html
<!-- Hidden on small, visible on medium+ -->
<div class="hidden md:block">
  Visible on tablet and larger
</div>

<!-- Different columns by breakpoint -->
<div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3">
  <div>Responsive column</div>
</div>

<!-- Flex direction change -->
<div class="flex flex-col md:flex-row gap-4">
  <div class="md:w-1/3">Sidebar</div>
  <div class="md:w-2/3">Main content</div>
</div>
```

## Common Patterns

### Header with Search and Actions

```html
<header class="sticky top-0 z-40 bg-white border-b border-dark-200">
  <div class="flex items-center justify-between h-16 px-6">
    <!-- Logo -->
    <div class="flex items-center space-x-3">
      <div class="w-10 h-10 bg-primary-600 rounded-lg"></div>
      <span class="text-lg font-bold">App Name</span>
    </div>

    <!-- Search -->
    <div class="relative flex-1 max-w-md">
      <input type="text" placeholder="Search..." class="input-field pl-10" />
      <svg class="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4"></svg>
    </div>

    <!-- Actions -->
    <div class="flex items-center space-x-4">
      <button class="relative w-10 h-10 rounded-lg hover:bg-dark-100">
        <svg></svg>
        <span class="absolute top-2 right-2 w-2 h-2 bg-red-500 rounded-full"></span>
      </button>
    </div>
  </div>
</header>
```

### Stats Grid

```html
<div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
  <div class="card">
    <p class="text-dark-500 text-sm">Metric Name</p>
    <p class="text-3xl font-bold mt-2">1,234</p>
    <p class="text-green-600 text-sm mt-2">+12.5% growth</p>
  </div>
  <!-- Repeat for other metrics -->
</div>
```

### Alert/Notification

```html
<!-- Success Alert -->
<div class="p-4 bg-green-100 border border-green-300 rounded-lg text-green-700">
  <p class="font-semibold">Success!</p>
  <p class="text-sm mt-1">Your action was successful.</p>
</div>

<!-- Error Alert -->
<div class="p-4 bg-red-100 border border-red-300 rounded-lg text-red-700">
  <p class="font-semibold">Error!</p>
  <p class="text-sm mt-1">Something went wrong.</p>
</div>
```

## Interactive Elements

### Dropdown Menu

```html
<div class="relative">
  <button class="btn-primary" onclick="toggleDropdown()">
    Menu
  </button>
  <div id="dropdown" class="hidden absolute right-0 mt-2 w-48 bg-white rounded-lg shadow-lg">
    <a href="#" class="block px-4 py-2 hover:bg-dark-100">Option 1</a>
    <a href="#" class="block px-4 py-2 hover:bg-dark-100">Option 2</a>
  </div>
</div>
```

### Modal/Dialog

```html
<div class="fixed inset-0 bg-black/50 hidden flex items-center justify-center z-50">
  <div class="bg-white rounded-lg shadow-xl max-w-md w-full mx-4">
    <div class="p-6 border-b border-dark-200">
      <h3 class="text-lg font-bold">Modal Title</h3>
    </div>
    <div class="p-6">
      <p>Modal content here</p>
    </div>
    <div class="p-6 border-t border-dark-200 flex justify-end space-x-2">
      <button class="btn-secondary">Cancel</button>
      <button class="btn-primary">Confirm</button>
    </div>
  </div>
</div>
```

## Breakpoints Reference

| Breakpoint | Min Width | CSS |
|------------|-----------|-----|
| None (Mobile) | 0px | Default |
| sm | 640px | `sm:` |
| md | 768px | `md:` |
| lg | 1024px | `lg:` |
| xl | 1280px | `xl:` |
| 2xl | 1536px | `2xl:` |

## Performance Tips

1. Use utility classes directly (don't create custom wrappers unnecessarily)
2. Leverage CSS Grid and Flexbox for layouts
3. Use responsive classes to avoid media queries
4. Minimize component-scoped CSS files
5. Keep shadow and animation usage reasonable

## Common Issues & Solutions

| Issue | Solution |
|-------|----------|
| Classes not applying | Rebuild CSS: `npm run tailwind:build` |
| Styles not updating | Check `tailwind.config.js` content paths |
| Spacing inconsistent | Use Tailwind spacing scale (4px base) |
| Colors look wrong | Verify color in design system |
| Mobile looks bad | Check responsive classes (sm:, md:, lg:) |

---

**For detailed Tailwind CSS documentation**: https://tailwindcss.com/docs
