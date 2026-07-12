namespace AutoPartShop.Api.Authorization;

/// <summary>
/// The 28 permission names seeded by DatabaseSeeder. Keep in sync — these strings
/// are matched against Permissions.Name at authorization time.
/// </summary>
public static class Permissions
{
    // User Management
    public const string UsersView = "users.view";
    public const string UsersCreate = "users.create";
    public const string UsersEdit = "users.edit";
    public const string UsersDelete = "users.delete";
    public const string UsersAssignRoles = "users.assign-roles";

    // Role Management
    public const string RolesView = "roles.view";
    public const string RolesCreate = "roles.create";
    public const string RolesEdit = "roles.edit";
    public const string RolesDelete = "roles.delete";
    public const string RolesAssignPermissions = "roles.assign-permissions";

    // Inventory (catalog + stock)
    public const string InventoryView = "inventory.view";
    public const string InventoryCreate = "inventory.create";
    public const string InventoryEdit = "inventory.edit";
    public const string InventoryDelete = "inventory.delete";
    public const string InventoryAdjustStock = "inventory.adjust-stock";

    // Sales (orders, customers, invoicing, fulfilment, warranty)
    public const string SalesView = "sales.view";
    public const string SalesCreate = "sales.create";
    public const string SalesEdit = "sales.edit";
    public const string SalesDelete = "sales.delete";
    public const string SalesProcessPayment = "sales.process-payment";

    // Procurement (purchase orders, suppliers, supplier payments)
    public const string ProcurementView = "procurement.view";
    public const string ProcurementCreate = "procurement.create";
    public const string ProcurementEdit = "procurement.edit";
    public const string ProcurementDelete = "procurement.delete";
    public const string ProcurementApprove = "procurement.approve";

    // Reports
    public const string ReportsView = "reports.view";
    public const string ReportsExport = "reports.export";

    // Audit
    public const string AuditView = "audit.view";
}
