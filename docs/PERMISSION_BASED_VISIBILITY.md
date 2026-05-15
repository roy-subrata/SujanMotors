# Permission-Based Visibility Implementation

## Overview
Implemented permission-based show/hide functionality for UI elements based on user permissions, in addition to the existing role-based visibility.

## Features Implemented

### 1. Backend Changes

#### **Auth Controller** (`src/AutoPartShop.Api/Controllers/AuthController.cs`)
- **Added** `Permissions` field to `LoginResponse` DTO
- **Created** `GetUserPermissionsAsync()` method to fetch all permissions for user's roles
- **Updated** Login endpoint to return user permissions along with roles

```csharp
private async Task<List<string>> GetUserPermissionsAsync(List<string> roleNames)
{
    // Gets all role IDs for the user's roles
    var roleIds = await _dbContext.Roles
        .Where(r => roleNames.Contains(r.Name!))
        .Select(r => r.Id)
        .ToListAsync();

    // Gets all permission names for those roles
    var permissionNames = await _dbContext.Set<RolePermission>()
        .Where(rp => roleIds.Contains(rp.RoleId))
        .Join(_dbContext.Set<Permission>(),
            rp => rp.PermissionId,
            p => p.Id,
            (rp, p) => p.Name)
        .Distinct()
        .ToListAsync();

    return permissionNames;
}
```

### 2. Frontend Changes

#### **AuthService** (`src/AutoPartShop.WebApp/src/app/shared/services/auth.service.ts`)

**Updated Interfaces:**
```typescript
export interface LoginResponse {
  token: string;
  username: string;
  email: string;
  fullName: string;
  roles: string[];
  permissions?: string[]; // Added
}

export interface User {
  username: string;
  email: string;
  fullName: string;
  roles: string[];
  permissions: string[]; // Added
}
```

**Added Permission Checking Methods:**
```typescript
// Check if user has a specific permission
hasPermission(permission: string): boolean

// Check if user has any of the specified permissions
hasAnyPermission(permissions: string[]): boolean

// Check if user has all specified permissions
hasAllPermissions(permissions: string[]): boolean

// Get current user permissions
getUserPermissions(): string[]
```

#### **HasPermissionDirective** (`src/AutoPartShop.WebApp/src/app/shared/directives/has-permission.directive.ts`)

**Created** new structural directive for permission-based visibility.

## Usage Examples

### 1. Show/Hide Based on Single Permission

```html
<!-- Show button only if user has 'users.create' permission -->
<button *appHasPermission="'users.create'">Create User</button>
```

### 2. Show/Hide Based on Multiple Permissions (ANY)

```html
<!-- Show if user has ANY of the specified permissions -->
<div *appHasPermission="['users.create', 'users.update']">
  User Management Actions
</div>
```

### 3. Show/Hide Based on Multiple Permissions (ALL)

```html
<!-- Show only if user has ALL specified permissions -->
<div *appHasPermission="['users.create', 'users.delete']; requireAll: true">
  Advanced User Management
</div>
```

### 4. With Else Template

```html
<!-- Show create button or no access message -->
<div *appHasPermission="'users.create'; else noAccess">
  <button>Create User</button>
</div>
<ng-template #noAccess>
  <p>You don't have permission to create users</p>
</ng-template>
```

### 5. Combined with Role-Based Visibility

```html
<!-- Admin users with specific permission -->
<div *appHasRole="'Admin'">
  <button *appHasPermission="'users.delete'">Delete User</button>
</div>
```

### 6. Menu Item Visibility

```html
<!-- Only show menu item if user has permission -->
<li *appHasPermission="'reports.view'">
  <a routerLink="/reports">Reports</a>
</li>
```

## Permission Naming Convention

Permissions follow the format: `{resource}.{action}`

Examples:
- `users.create` - Create users
- `users.update` - Update users
- `users.delete` - Delete users
- `users.view` - View users
- `roles.manage` - Manage roles
- `permissions.assign` - Assign permissions
- `inventory.view` - View inventory
- `inventory.manage` - Manage inventory
- `sales.create` - Create sales
- `reports.view` - View reports

## How It Works

1. **Login**: When user logs in, backend fetches all permissions associated with user's roles
2. **Storage**: Permissions are stored in localStorage along with other user data
3. **Checking**: Directive subscribes to auth state and checks permissions dynamically
4. **Rendering**: UI elements are shown/hidden based on permission check results

## Benefits

1. **Fine-grained Control**: Control visibility at the permission level, not just role level
2. **Dynamic**: Permissions can be reassigned without changing code
3. **Reusable**: Same directive can be used throughout the application
4. **Type-safe**: Permission strings are validated at runtime
5. **Performance**: Permissions are cached in user session

## Migration Path

Existing code using `*appHasRole` continues to work. New code can use `*appHasPermission` for more granular control.

**Before** (role-based):
```html
<button *appHasRole="'Admin'">Delete User</button>
```

**After** (permission-based):
```html
<button *appHasPermission="'users.delete'">Delete User</button>
```

## Testing

To test permission-based visibility:

1. Log in as a user
2. Check which permissions the user has in Admin Settings → Roles
3. Navigate to pages where permission-based elements exist
4. Verify elements show/hide based on permissions

## Build Status

✅ **Backend**: Built successfully
✅ **Frontend**: Built successfully

Both backend and frontend are ready for deployment with permission-based visibility feature.
