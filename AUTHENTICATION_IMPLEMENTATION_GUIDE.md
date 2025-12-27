# Authentication & Authorization Implementation Guide

## Overview
This document outlines the complete ASP.NET Identity authentication and authorization system implemented in the AutoPartShop application.

---

## Backend Implementation (✅ Completed)

### 1. Identity Domain Entities Created

#### ApplicationUser ([ApplicationUser.cs](src/AutoPartShop.Domain/Entities/ApplicationUser.cs))
- Extends `IdentityUser<Guid>`
- Properties: FirstName, LastName, ProfilePictureUrl, IsActive, CreatedAt, LastLoginAt
- Navigation property for UserRoles

#### ApplicationRole ([ApplicationRole.cs](src/AutoPartShop.Domain/Entities/ApplicationRole.cs))
- Extends `IdentityRole<Guid>`
- Properties: Description, IsActive, CreatedAt
- Navigation properties for UserRoles and RolePermissions

#### Permission ([Permission.cs](src/AutoPartShop.Domain/Entities/Permission.cs))
- Manages fine-grained permissions
- Properties: Name, DisplayName, Description, Category, IsActive
- Domain methods: Create, Update, Activate, Deactivate

#### RolePermission ([RolePermission.cs](src/AutoPartShop.Domain/Entities/RolePermission.cs))
- Many-to-many relationship between Roles and Permissions
- Tracks who granted the permission and when

### 2. Database Configuration

#### DbContext Updated ([AutoPartDbContext.cs](src/AutoPartShop.Infrastructure/Data/AutoPartDbContext.cs))
- Now extends `IdentityDbContext` with custom types
- Identity tables configured with custom names:
  - Users
  - Roles
  - UserRoles
  - UserClaims
  - RoleClaims
  - UserLogins
  - UserTokens
  - Permissions
  - RolePermissions

### 3. API Configuration ([Program.cs](src/AutoPartShop.Api/Program.cs))

#### ASP.NET Core Identity
```csharp
builder.Services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 8;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<AutoPartDbContext>()
.AddDefaultTokenProviders();
```

#### JWT Authentication
- Configured with symmetric key encryption
- Token expiry: 60 minutes (configurable)
- Refresh token support
- Integrated with Swagger for easy testing

#### JWT Settings ([appsettings.json](src/AutoPartShop.Api/appsettings.json))
```json
{
  "JwtSettings": {
    "SecretKey": "YourSuperSecretKeyForJWTTokenGenerationMustBe32CharsLong!@#",
    "Issuer": "AutoPartShopAPI",
    "Audience": "AutoPartShopClient",
    "ExpiryInMinutes": 60,
    "RefreshTokenExpiryInDays": 7
  }
}
```

⚠️ **IMPORTANT**: Change the SecretKey in production!

### 4. API Controllers

#### AuthController ([Controllers/AuthController.cs](src/AutoPartShop.Api/Controllers/AuthController.cs))

**Endpoints:**

1. **POST /api/auth/login**
   - Authenticates user with username/email and password
   - Returns JWT token and user information
   - Handles account lockout

2. **POST /api/auth/register**
   - Self-registration endpoint
   - Creates new user with default role
   - Auto-confirms email (can be changed)

3. **POST /api/auth/refresh-token**
   - Refreshes expired JWT token
   - Validates previous token and generates new one

4. **POST /api/auth/change-password**
   - Allows users to change their password
   - Requires current password verification

#### AdminController ([Controllers/AdminController.cs](src/AutoPartShop.Api/Controllers/AdminController.cs))

**User Management:**
- `GET /api/admin/users` - Get all users with roles
- `GET /api/admin/users/{id}` - Get user by ID
- `POST /api/admin/users` - Create new user
- `PUT /api/admin/users/{id}` - Update user information
- `PATCH /api/admin/users/{id}/toggle-status` - Activate/Deactivate user
- `POST /api/admin/users/{id}/reset-password` - Reset user password

**Role Management:**
- `GET /api/admin/roles` - Get all roles
- `POST /api/admin/roles` - Create new role
- `PUT /api/admin/roles/{id}` - Update role
- `DELETE /api/admin/roles/{id}` - Delete role (if no users assigned)

**User-Role Assignment:**
- `GET /api/admin/users/{userId}/roles` - Get roles for user
- `POST /api/admin/users/{userId}/roles` - Assign/Remove roles for user

**Permission Management:**
- `GET /api/admin/permissions` - Get all permissions
- `POST /api/admin/permissions` - Create new permission
- `GET /api/admin/roles/{roleId}/permissions` - Get permissions for role
- `POST /api/admin/roles/{roleId}/permissions` - Assign permissions to role

---

## Next Steps - Database Migration

### Step 1: Stop the Running API
The API is currently running (process ID: 15932). You need to stop it before running migrations.

### Step 2: Create Migration
```bash
dotnet ef migrations add AddIdentityTables --project "src/AutoPartShop.Infrastructure/AutoPartShop.Infrastructure.csproj" --startup-project "src/AutoPartShop.Api/AutoPartShop.Api.csproj" --context AutoPartDbContext
```

### Step 3: Update Database
```bash
dotnet ef database update --project "src/AutoPartShop.Infrastructure/AutoPartShop.Infrastructure.csproj" --startup-project "src/AutoPartShop.Api/AutoPartShop.Api.csproj" --context AutoPartDbContext
```

---

## Initial Data Seeding (Recommended)

After running migrations, you should seed initial data:

### Create Initial Admin User
Use Swagger or Postman to call:

**POST** `http://localhost:5292/api/auth/register`
```json
{
  "username": "admin",
  "email": "admin@autopartshop.com",
  "password": "Admin@123",
  "firstName": "System",
  "lastName": "Administrator",
  "defaultRole": null
}
```

### Create Initial Roles
Use Swagger or Postman:

**POST** `http://localhost:5292/api/admin/roles`
```json
{
  "name": "Admin",
  "description": "Full system access"
}
```

**POST** `http://localhost:5292/api/admin/roles`
```json
{
  "name": "Manager",
  "description": "Department manager access"
}
```

**POST** `http://localhost:5292/api/admin/roles`
```json
{
  "name": "User",
  "description": "Standard user access"
}
```

### Create Permissions (Examples)
**POST** `http://localhost:5292/api/admin/permissions`
```json
{
  "name": "users.view",
  "displayName": "View Users",
  "category": "User Management",
  "description": "Can view user list"
}
```

```json
{
  "name": "users.create",
  "displayName": "Create Users",
  "category": "User Management",
  "description": "Can create new users"
}
```

```json
{
  "name": "inventory.manage",
  "displayName": "Manage Inventory",
  "category": "Inventory",
  "description": "Full inventory management access"
}
```

### Assign Admin Role to User
Get the admin user ID and role ID, then:

**POST** `http://localhost:5292/api/admin/users/{userId}/roles`
```json
{
  "roles": ["Admin"]
}
```

---

## Frontend Implementation (🔄 In Progress)

### Angular Components to Create:
1. ✅ Authentication Service
2. ✅ Login Page Component
3. ✅ Admin Settings Page (User/Role Management)
4. ✅ HTTP Interceptor for JWT tokens
5. ✅ Route Guards for role-based access
6. ✅ Permission-based UI visibility

---

## Security Best Practices

### 1. Password Requirements
- Minimum 8 characters
- Must contain: uppercase, lowercase, digit, special character
- Enforced by ASP.NET Identity

### 2. Account Lockout
- 5 failed attempts
- 15-minute lockout period
- Prevents brute-force attacks

### 3. Token Security
- JWT tokens expire after 60 minutes
- Refresh tokens expire after 7 days
- Tokens signed with HMAC-SHA256

### 4. HTTPS
- Always use HTTPS in production
- Update `Program.cs` to enforce HTTPS redirection

### 5. Secret Key Management
- **NEVER** commit real secret keys to source control
- Use User Secrets in development
- Use Azure Key Vault or similar in production

---

## API Testing with Swagger

1. Navigate to: `http://localhost:5292/docs`
2. You'll see a "Authorize" button in the top right
3. Login to get a JWT token:
   - Call `/api/auth/login`
   - Copy the token from the response
4. Click "Authorize" button
5. Enter: `Bearer YOUR_TOKEN_HERE`
6. Now all requests will include the authorization header

---

## Common Issues & Solutions

### Issue 1: "User not found or inactive"
- Check user's `IsActive` status
- Verify user exists in database

### Issue 2: "Invalid credentials"
- Check password requirements
- Verify account is not locked out

### Issue 3: Migration fails
- Ensure API is stopped
- Check connection string
- Verify all required packages are installed

### Issue 4: Token validation fails
- Verify JWT settings match in all environments
- Check token hasn't expired
- Ensure secret key is consistent

---

## Architecture Benefits

1. **Separation of Concerns**
   - Domain entities separate from Identity framework
   - Clean controller structure
   - Testable services

2. **Scalability**
   - Permission-based system allows fine-grained control
   - Easy to add new roles and permissions
   - Supports multi-tenant scenarios

3. **Security**
   - Industry-standard JWT authentication
   - ASP.NET Core Identity best practices
   - Audit trail support ready

4. **Flexibility**
   - Custom user properties
   - Extensible permission system
   - Role hierarchy ready

---

## Next Phase: Angular Frontend

The frontend will include:
- Modern login page with form validation
- Admin dashboard for user/role management
- JWT token storage and automatic refresh
- Route guards based on roles
- Permission-based UI element visibility
- Professional user management interface

---

## Support & Maintenance

### Adding New Permissions
1. Create permission via Admin API
2. Assign to appropriate roles
3. Update frontend guards/visibility checks

### Adding New Roles
1. Create role via Admin API
2. Assign relevant permissions
3. Update route guards if needed

### User Management
- Admins can create users directly
- Users can self-register (can be disabled)
- Password reset via admin
- Account activation/deactivation

---

**Implementation Status**: Backend Complete ✅ | Frontend In Progress 🔄
