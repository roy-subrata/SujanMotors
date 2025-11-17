# Example Pages & Code Snippets

This document provides ready-to-use page templates following the established design system.

## Table of Contents

1. [Inventory Management Pages](#inventory-management-pages)
2. [Orders & Sales Pages](#orders--sales-pages)
3. [Customer Management](#customer-management)
4. [Settings Pages](#settings-pages)
5. [Form Examples](#form-examples)

---

## Inventory Management Pages

### Inventory/Parts List Page

Create file: `Components/Pages/Inventory/Parts.razor`

```razor
@page "/inventory/parts"

<PageTitle>All Parts - AutoParts Shop</PageTitle>

<div class="space-y-6">
    <!-- Page Header -->
    <div class="flex flex-col sm:flex-row sm:items-center sm:justify-between">
        <div>
            <h1 class="text-3xl font-bold text-dark-900">Parts Inventory</h1>
            <p class="text-dark-500 mt-1">Manage your automotive parts catalog</p>
        </div>
        <div class="mt-4 sm:mt-0 flex space-x-2">
            <button class="btn-secondary">
                <svg class="w-5 h-5 inline-block mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4"></path>
                </svg>
                Add Part
            </button>
            <button class="btn-primary">
                <svg class="w-5 h-5 inline-block mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 16v1a3 3 0 003 3h10a3 3 0 003-3v-1m-4-4l-4 4m0 0l-4-4m4 4V4"></path>
                </svg>
                Import
            </button>
        </div>
    </div>

    <!-- Filters -->
    <div class="card">
        <div class="grid grid-cols-1 md:grid-cols-4 gap-4">
            <div>
                <label class="block text-sm font-medium text-dark-900 mb-2">Search Parts</label>
                <input type="text" placeholder="Filter by name..." class="input-field" />
            </div>
            <div>
                <label class="block text-sm font-medium text-dark-900 mb-2">Category</label>
                <select class="input-field">
                    <option>All Categories</option>
                    <option>Filters</option>
                    <option>Brake System</option>
                    <option>Engine</option>
                </select>
            </div>
            <div>
                <label class="block text-sm font-medium text-dark-900 mb-2">Stock Status</label>
                <select class="input-field">
                    <option>All</option>
                    <option>In Stock</option>
                    <option>Low Stock</option>
                    <option>Out of Stock</option>
                </select>
            </div>
            <div class="flex items-end">
                <button class="btn-primary w-full">Apply Filters</button>
            </div>
        </div>
    </div>

    <!-- Parts Table -->
    <div class="card">
        <div class="overflow-x-auto">
            <table class="w-full table-striped table-hover">
                <thead>
                    <tr class="border-b border-dark-200">
                        <th class="text-left py-3 px-4 font-semibold text-dark-900 text-sm">Part Name</th>
                        <th class="text-left py-3 px-4 font-semibold text-dark-900 text-sm">SKU</th>
                        <th class="text-left py-3 px-4 font-semibold text-dark-900 text-sm">Category</th>
                        <th class="text-left py-3 px-4 font-semibold text-dark-900 text-sm">Price</th>
                        <th class="text-left py-3 px-4 font-semibold text-dark-900 text-sm">Stock</th>
                        <th class="text-left py-3 px-4 font-semibold text-dark-900 text-sm">Status</th>
                        <th class="text-left py-3 px-4 font-semibold text-dark-900 text-sm">Action</th>
                    </tr>
                </thead>
                <tbody>
                    <tr class="border-b border-dark-100 hover:bg-primary-50 transition-colors">
                        <td class="py-4 px-4 text-sm font-medium text-dark-900">Engine Oil Filter</td>
                        <td class="py-4 px-4 text-sm text-dark-700">EOF-001</td>
                        <td class="py-4 px-4 text-sm text-dark-700">Filters</td>
                        <td class="py-4 px-4 text-sm font-semibold text-dark-900">$12.99</td>
                        <td class="py-4 px-4 text-sm text-dark-900">
                            <span class="font-semibold">245</span> units
                        </td>
                        <td class="py-4 px-4">
                            <span class="badge badge-success">In Stock</span>
                        </td>
                        <td class="py-4 px-4">
                            <button class="text-primary-600 hover:text-primary-700 text-sm font-medium">Edit</button>
                        </td>
                    </tr>
                    <tr class="border-b border-dark-100 hover:bg-primary-50 transition-colors">
                        <td class="py-4 px-4 text-sm font-medium text-dark-900">Brake Pads Set</td>
                        <td class="py-4 px-4 text-sm text-dark-700">BPS-002</td>
                        <td class="py-4 px-4 text-sm text-dark-700">Brake System</td>
                        <td class="py-4 px-4 text-sm font-semibold text-dark-900">$45.50</td>
                        <td class="py-4 px-4 text-sm text-dark-900">
                            <span class="font-semibold">12</span> units
                        </td>
                        <td class="py-4 px-4">
                            <span class="badge badge-warning">Low Stock</span>
                        </td>
                        <td class="py-4 px-4">
                            <button class="text-primary-600 hover:text-primary-700 text-sm font-medium">Edit</button>
                        </td>
                    </tr>
                </tbody>
            </table>
        </div>

        <!-- Pagination -->
        <div class="flex items-center justify-between mt-6 pt-6 border-t border-dark-200">
            <p class="text-sm text-dark-600">Showing 1-10 of 245 parts</p>
            <div class="flex space-x-2">
                <button class="px-3 py-2 border border-dark-300 rounded-lg text-dark-600 hover:bg-dark-100">
                    ← Previous
                </button>
                <button class="px-3 py-2 border border-dark-300 rounded-lg text-dark-600 hover:bg-dark-100">
                    1
                </button>
                <button class="px-3 py-2 bg-primary-600 text-white rounded-lg">2</button>
                <button class="px-3 py-2 border border-dark-300 rounded-lg text-dark-600 hover:bg-dark-100">
                    3
                </button>
                <button class="px-3 py-2 border border-dark-300 rounded-lg text-dark-600 hover:bg-dark-100">
                    Next →
                </button>
            </div>
        </div>
    </div>
</div>
```

---

## Orders & Sales Pages

### Active Orders Page

Create file: `Components/Pages/Orders/Active.razor`

```razor
@page "/orders/active"

<PageTitle>Active Orders - AutoParts Shop</PageTitle>

<div class="space-y-6">
    <!-- Page Header with Stats -->
    <div class="space-y-4">
        <div>
            <h1 class="text-3xl font-bold text-dark-900">Active Orders</h1>
            <p class="text-dark-500 mt-1">Manage current orders and shipments</p>
        </div>

        <!-- Quick Stats -->
        <div class="grid grid-cols-1 sm:grid-cols-3 gap-4">
            <div class="card-sm">
                <p class="text-dark-500 text-sm">Total Active</p>
                <p class="text-2xl font-bold text-dark-900 mt-1">24</p>
            </div>
            <div class="card-sm">
                <p class="text-dark-500 text-sm">Pending Fulfillment</p>
                <p class="text-2xl font-bold text-orange-600 mt-1">8</p>
            </div>
            <div class="card-sm">
                <p class="text-dark-500 text-sm">Shipped Today</p>
                <p class="text-2xl font-bold text-green-600 mt-1">5</p>
            </div>
        </div>
    </div>

    <!-- Orders List -->
    <div class="space-y-4">
        <!-- Order Card 1 -->
        <div class="card">
            <div class="flex items-start justify-between mb-4">
                <div>
                    <h3 class="text-lg font-bold text-dark-900">#ORD-2024-001</h3>
                    <p class="text-sm text-dark-500 mt-1">Placed on Nov 15, 2024 • 2:30 PM</p>
                </div>
                <span class="badge badge-primary">Pending</span>
            </div>

            <div class="grid grid-cols-1 md:grid-cols-3 gap-4 mb-4 pb-4 border-b border-dark-200">
                <div>
                    <p class="text-xs font-semibold text-dark-500 uppercase">Customer</p>
                    <p class="text-dark-900 font-medium mt-1">John Automotive Repair</p>
                </div>
                <div>
                    <p class="text-xs font-semibold text-dark-500 uppercase">Total Amount</p>
                    <p class="text-dark-900 font-medium mt-1">$2,450.00</p>
                </div>
                <div>
                    <p class="text-xs font-semibold text-dark-500 uppercase">Order Type</p>
                    <p class="text-dark-900 font-medium mt-1">Standard</p>
                </div>
            </div>

            <!-- Order Items -->
            <div class="mb-4">
                <p class="text-xs font-semibold text-dark-500 uppercase mb-3">Items</p>
                <div class="space-y-2">
                    <div class="flex items-center justify-between text-sm">
                        <span class="text-dark-900">Engine Oil Filter x 10</span>
                        <span class="text-dark-700">$129.90</span>
                    </div>
                    <div class="flex items-center justify-between text-sm">
                        <span class="text-dark-900">Brake Pads Set x 5</span>
                        <span class="text-dark-700">$227.50</span>
                    </div>
                    <div class="flex items-center justify-between text-sm">
                        <span class="text-dark-900">Air Filter x 8</span>
                        <span class="text-dark-700">$159.92</span>
                    </div>
                </div>
            </div>

            <!-- Timeline/Status -->
            <div class="bg-dark-50 rounded-lg p-4 mb-4">
                <p class="text-xs font-semibold text-dark-500 uppercase mb-3">Order Status</p>
                <div class="space-y-2">
                    <div class="flex items-center">
                        <div class="w-3 h-3 bg-green-600 rounded-full mr-3"></div>
                        <p class="text-sm text-dark-900"><span class="font-semibold">Order Confirmed</span> - Nov 15, 2:30 PM</p>
                    </div>
                    <div class="flex items-center">
                        <div class="w-3 h-3 bg-primary-600 rounded-full mr-3"></div>
                        <p class="text-sm text-dark-900"><span class="font-semibold">Processing</span> - In Progress</p>
                    </div>
                    <div class="flex items-center">
                        <div class="w-3 h-3 bg-dark-300 rounded-full mr-3"></div>
                        <p class="text-sm text-dark-500">Pending Shipment</p>
                    </div>
                </div>
            </div>

            <!-- Actions -->
            <div class="flex space-x-2">
                <button class="btn-primary">
                    <svg class="w-4 h-4 inline-block mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"></path>
                    </svg>
                    View Details
                </button>
                <button class="btn-secondary">
                    <svg class="w-4 h-4 inline-block mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 19l9 2-9-18-9 18 9-2zm0 0v-8"></path>
                    </svg>
                    Ship Order
                </button>
            </div>
        </div>
    </div>
</div>
```

---

## Customer Management

### Customers List Page

Create file: `Components/Pages/Customers.razor`

```razor
@page "/customers"

<PageTitle>Customers - AutoParts Shop</PageTitle>

<div class="space-y-6">
    <!-- Page Header -->
    <div class="flex flex-col sm:flex-row sm:items-center sm:justify-between">
        <div>
            <h1 class="text-3xl font-bold text-dark-900">Customers</h1>
            <p class="text-dark-500 mt-1">Manage customer relationships and profiles</p>
        </div>
        <button class="mt-4 sm:mt-0 btn-primary">
            <svg class="w-5 h-5 inline-block mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4"></path>
            </svg>
            Add Customer
        </button>
    </div>

    <!-- Customer Cards Grid -->
    <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
        <!-- Customer Card -->
        <div class="card">
            <div class="flex items-start justify-between mb-4">
                <div class="flex items-center space-x-3">
                    <div class="w-12 h-12 rounded-full bg-gradient-to-br from-blue-400 to-blue-600 flex items-center justify-center text-white font-bold">
                        JA
                    </div>
                    <div>
                        <h3 class="font-bold text-dark-900">John Automotive</h3>
                        <p class="text-xs text-dark-500">Repair Shop</p>
                    </div>
                </div>
                <button class="text-dark-400 hover:text-dark-600">
                    <svg class="w-5 h-5" fill="currentColor" viewBox="0 0 24 24">
                        <path d="M6 10a2 2 0 11-4 0 2 2 0 014 0zM12 10a2 2 0 11-4 0 2 2 0 014 0zM16 12a2 2 0 100-4 2 2 0 000 4z"></path>
                    </svg>
                </button>
            </div>

            <div class="space-y-3 mb-4 pb-4 border-b border-dark-200">
                <div>
                    <p class="text-xs text-dark-500 font-medium">LOCATION</p>
                    <p class="text-sm text-dark-900">New York, NY</p>
                </div>
                <div>
                    <p class="text-xs text-dark-500 font-medium">EMAIL</p>
                    <p class="text-sm text-primary-600">john@automotive.com</p>
                </div>
                <div>
                    <p class="text-xs text-dark-500 font-medium">PHONE</p>
                    <p class="text-sm text-dark-900">+1 (555) 123-4567</p>
                </div>
            </div>

            <div class="space-y-3 mb-4">
                <div class="flex justify-between">
                    <span class="text-xs text-dark-500 font-medium">Total Orders</span>
                    <span class="font-semibold text-dark-900">24</span>
                </div>
                <div class="flex justify-between">
                    <span class="text-xs text-dark-500 font-medium">Total Spent</span>
                    <span class="font-semibold text-dark-900">$12,450</span>
                </div>
                <div class="flex justify-between">
                    <span class="text-xs text-dark-500 font-medium">Status</span>
                    <span class="badge badge-success">Active</span>
                </div>
            </div>

            <button class="w-full btn-outline">View Profile</button>
        </div>

        <!-- More customer cards would follow the same pattern -->
    </div>
</div>
```

---

## Settings Pages

### Company Settings Page

Create file: `Components/Pages/Settings/Company.razor`

```razor
@page "/settings/company"

<PageTitle>Company Settings - AutoParts Shop</PageTitle>

<div class="space-y-6">
    <!-- Page Header -->
    <div>
        <h1 class="text-3xl font-bold text-dark-900">Company Information</h1>
        <p class="text-dark-500 mt-1">Manage your business details and preferences</p>
    </div>

    <!-- Settings Form -->
    <div class="card max-w-2xl">
        <form class="space-y-6">
            <!-- Logo Section -->
            <div class="pb-6 border-b border-dark-200">
                <h3 class="text-lg font-bold text-dark-900 mb-4">Logo & Branding</h3>
                <div class="flex items-end space-x-6">
                    <div class="w-24 h-24 bg-dark-100 rounded-lg flex items-center justify-center border-2 border-dashed border-dark-300">
                        <svg class="w-12 h-12 text-dark-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 16l4.586-4.586a2 2 0 012.828 0L16 16m-2-2l1.586-1.586a2 2 0 012.828 0L20 14m-6-6h.01M6 20h12a2 2 0 002-2V6a2 2 0 00-2-2H6a2 2 0 00-2 2v12a2 2 0 002 2z"></path>
                        </svg>
                    </div>
                    <button type="button" class="btn-secondary">
                        Upload Logo
                    </button>
                </div>
            </div>

            <!-- Basic Info -->
            <div class="space-y-4">
                <h3 class="text-lg font-bold text-dark-900">Business Details</h3>

                <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
                    <div>
                        <label class="block text-sm font-medium text-dark-900 mb-2">Company Name</label>
                        <input type="text" value="AutoParts Shop Inc." class="input-field" />
                    </div>
                    <div>
                        <label class="block text-sm font-medium text-dark-900 mb-2">Business Type</label>
                        <select class="input-field">
                            <option>Automotive Parts Retailer</option>
                            <option>Automotive Supplier</option>
                            <option>Repair Shop</option>
                        </select>
                    </div>
                </div>

                <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
                    <div>
                        <label class="block text-sm font-medium text-dark-900 mb-2">Email</label>
                        <input type="email" value="info@autoparts.com" class="input-field" />
                    </div>
                    <div>
                        <label class="block text-sm font-medium text-dark-900 mb-2">Phone</label>
                        <input type="tel" value="+1 (555) 123-4567" class="input-field" />
                    </div>
                </div>

                <div>
                    <label class="block text-sm font-medium text-dark-900 mb-2">Website</label>
                    <input type="url" value="https://autoparts.example.com" class="input-field" />
                </div>
            </div>

            <!-- Address -->
            <div class="pt-6 border-t border-dark-200 space-y-4">
                <h3 class="text-lg font-bold text-dark-900">Address</h3>

                <div>
                    <label class="block text-sm font-medium text-dark-900 mb-2">Street Address</label>
                    <input type="text" value="123 Main Street" class="input-field" />
                </div>

                <div class="grid grid-cols-1 md:grid-cols-3 gap-4">
                    <div>
                        <label class="block text-sm font-medium text-dark-900 mb-2">City</label>
                        <input type="text" value="New York" class="input-field" />
                    </div>
                    <div>
                        <label class="block text-sm font-medium text-dark-900 mb-2">State/Province</label>
                        <input type="text" value="NY" class="input-field" />
                    </div>
                    <div>
                        <label class="block text-sm font-medium text-dark-900 mb-2">ZIP/Postal Code</label>
                        <input type="text" value="10001" class="input-field" />
                    </div>
                </div>

                <div>
                    <label class="block text-sm font-medium text-dark-900 mb-2">Country</label>
                    <select class="input-field">
                        <option>United States</option>
                        <option>Canada</option>
                        <option>Mexico</option>
                    </select>
                </div>
            </div>

            <!-- Form Actions -->
            <div class="flex justify-end space-x-3 pt-6 border-t border-dark-200">
                <button type="button" class="btn-secondary">Cancel</button>
                <button type="submit" class="btn-primary">Save Changes</button>
            </div>
        </form>
    </div>
</div>
```

---

## Form Examples

### Add/Edit Part Form

Create file: `Components/Pages/Inventory/PartForm.razor`

```razor
@page "/inventory/parts/add"
@page "/inventory/parts/{id:int}/edit"

<PageTitle>@(Id > 0 ? "Edit Part" : "Add New Part") - AutoParts Shop</PageTitle>

<div class="space-y-6">
    <!-- Page Header -->
    <div>
        <h1 class="text-3xl font-bold text-dark-900">@(Id > 0 ? "Edit Part" : "Add New Part")</h1>
        <p class="text-dark-500 mt-1">@(Id > 0 ? "Update part details" : "Create a new automotive part")</p>
    </div>

    <!-- Form Card -->
    <div class="card max-w-2xl">
        <form class="space-y-6" onsubmit="handleSubmit">
            <!-- Basic Information -->
            <div class="space-y-4">
                <h3 class="text-lg font-bold text-dark-900">Basic Information</h3>

                <div>
                    <label class="block text-sm font-medium text-dark-900 mb-2">Part Name *</label>
                    <input type="text" placeholder="e.g., Engine Oil Filter" class="input-field" required />
                </div>

                <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
                    <div>
                        <label class="block text-sm font-medium text-dark-900 mb-2">SKU *</label>
                        <input type="text" placeholder="e.g., EOF-001" class="input-field" required />
                    </div>
                    <div>
                        <label class="block text-sm font-medium text-dark-900 mb-2">Category *</label>
                        <select class="input-field" required>
                            <option value="">Select category</option>
                            <option value="filters">Filters</option>
                            <option value="brakes">Brake System</option>
                            <option value="engine">Engine</option>
                            <option value="transmission">Transmission</option>
                        </select>
                    </div>
                </div>

                <div>
                    <label class="block text-sm font-medium text-dark-900 mb-2">Description</label>
                    <textarea rows="4" placeholder="Detailed part description..." class="input-field"></textarea>
                </div>
            </div>

            <!-- Pricing -->
            <div class="pt-6 border-t border-dark-200 space-y-4">
                <h3 class="text-lg font-bold text-dark-900">Pricing</h3>

                <div class="grid grid-cols-1 md:grid-cols-3 gap-4">
                    <div>
                        <label class="block text-sm font-medium text-dark-900 mb-2">Cost Price *</label>
                        <div class="relative">
                            <span class="absolute left-3 top-1/2 -translate-y-1/2 text-dark-500">$</span>
                            <input type="number" step="0.01" placeholder="0.00" class="input-field pl-7" required />
                        </div>
                    </div>
                    <div>
                        <label class="block text-sm font-medium text-dark-900 mb-2">Selling Price *</label>
                        <div class="relative">
                            <span class="absolute left-3 top-1/2 -translate-y-1/2 text-dark-500">$</span>
                            <input type="number" step="0.01" placeholder="0.00" class="input-field pl-7" required />
                        </div>
                    </div>
                    <div>
                        <label class="block text-sm font-medium text-dark-900 mb-2">Profit Margin</label>
                        <input type="text" value="25%" class="input-field bg-dark-100" disabled />
                    </div>
                </div>
            </div>

            <!-- Stock Information -->
            <div class="pt-6 border-t border-dark-200 space-y-4">
                <h3 class="text-lg font-bold text-dark-900">Stock Information</h3>

                <div class="grid grid-cols-1 md:grid-cols-3 gap-4">
                    <div>
                        <label class="block text-sm font-medium text-dark-900 mb-2">Current Stock *</label>
                        <input type="number" placeholder="0" class="input-field" required />
                    </div>
                    <div>
                        <label class="block text-sm font-medium text-dark-900 mb-2">Reorder Level *</label>
                        <input type="number" placeholder="50" class="input-field" required />
                    </div>
                    <div>
                        <label class="block text-sm font-medium text-dark-900 mb-2">Reorder Quantity *</label>
                        <input type="number" placeholder="100" class="input-field" required />
                    </div>
                </div>

                <div>
                    <label class="block text-sm font-medium text-dark-900 mb-2">Supplier *</label>
                    <select class="input-field" required>
                        <option value="">Select supplier</option>
                        <option value="supplier1">Parts Direct Inc.</option>
                        <option value="supplier2">Auto Components Ltd.</option>
                        <option value="supplier3">Global Auto Parts</option>
                    </select>
                </div>
            </div>

            <!-- Form Actions -->
            <div class="flex justify-end space-x-3 pt-6 border-t border-dark-200">
                <button type="button" class="btn-secondary">Cancel</button>
                <button type="submit" class="btn-primary">
                    @(Id > 0 ? "Update Part" : "Add Part")
                </button>
            </div>
        </form>
    </div>
</div>

@code {
    [Parameter]
    public int Id { get; set; }

    private void handleSubmit(EventArgs e)
    {
        // Handle form submission
    }
}
```

---

## Reusable Pattern Components

### Filter Card Component

```html
<div class="card">
    <div class="grid grid-cols-1 md:grid-cols-4 gap-4">
        <div>
            <label class="block text-sm font-medium text-dark-900 mb-2">Filter Label</label>
            <input type="text" placeholder="Search..." class="input-field" />
        </div>
        <div>
            <label class="block text-sm font-medium text-dark-900 mb-2">Category</label>
            <select class="input-field">
                <option>All</option>
                <option>Option 1</option>
                <option>Option 2</option>
            </select>
        </div>
        <div>
            <label class="block text-sm font-medium text-dark-900 mb-2">Status</label>
            <select class="input-field">
                <option>All</option>
                <option>Active</option>
                <option>Inactive</option>
            </select>
        </div>
        <div class="flex items-end">
            <button class="btn-primary w-full">Apply</button>
        </div>
    </div>
</div>
```

### Status Timeline Component

```html
<div class="bg-dark-50 rounded-lg p-4">
    <p class="text-xs font-semibold text-dark-500 uppercase mb-3">Timeline</p>
    <div class="space-y-3">
        <div class="flex items-center">
            <div class="w-3 h-3 bg-green-600 rounded-full mr-3"></div>
            <p class="text-sm text-dark-900">
                <span class="font-semibold">Completed Step</span> - Date & Time
            </p>
        </div>
        <div class="flex items-center">
            <div class="w-3 h-3 bg-primary-600 rounded-full mr-3"></div>
            <p class="text-sm text-dark-900">
                <span class="font-semibold">Current Step</span> - In Progress
            </p>
        </div>
        <div class="flex items-center">
            <div class="w-3 h-3 bg-dark-300 rounded-full mr-3"></div>
            <p class="text-sm text-dark-500">Next Step</p>
        </div>
    </div>
</div>
```

---

## Tips for Creating New Pages

1. **Follow the Layout Pattern**:
   - Header with title and action buttons
   - Content in cards with proper spacing
   - Footer with pagination or actions

2. **Use Grid Layouts**:
   - `grid-cols-1 md:grid-cols-2 lg:grid-cols-3` for responsive lists
   - Adjust based on content complexity

3. **Consistent Spacing**:
   - Use `space-y-6` for section spacing
   - Use `space-x-2` or `space-x-3` for button groups
   - Use `gap-4` or `gap-6` for grids

4. **Color-Coded Status**:
   - Success (green): Completed, In Stock
   - Warning (orange): Low Stock, In Progress
   - Danger (red): Failed, Out of Stock
   - Primary (blue): Default, Pending

5. **Form Best Practices**:
   - Use `input-field` class for all inputs
   - Group related fields in sections
   - Show required fields with asterisk (*)
   - Add proper labels and placeholders

---

Ready to build more pages? Copy these patterns and customize with your data!
