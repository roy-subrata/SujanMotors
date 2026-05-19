namespace AutoPartShop.Domain.Entities;

public class Permission : AuditableEntity
{
    public string Name { get; private set; }
    public string DisplayName { get; private set; }
    public string? Description { get; private set; }
    public string Category { get; private set; }
    public bool IsActive { get; private set; } = true;

    // Navigation properties
    public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();

    private Permission() { }

    public static Permission Create(string name, string displayName, string category, string? description = null)
    {
        return new Permission
        {
            Name = name,
            DisplayName = displayName,
            Category = category,
            Description = description,
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        };
    }

    public void Update(string displayName, string category, string? description = null)
    {
        DisplayName = displayName;
        Category = category;
        Description = description;
        ModifiedDate = DateTime.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        ModifiedDate = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        ModifiedDate = DateTime.UtcNow;
    }
}
