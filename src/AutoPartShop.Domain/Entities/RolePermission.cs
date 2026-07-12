namespace AutoPartShop.Domain.Entities;

public class RolePermission : BaseEntity
{
    public Guid RoleId { get; private set; }
    public Guid PermissionId { get; private set; }
    public DateTime GrantedAt { get; private set; }
    public string GrantedBy { get; private set; } = string.Empty;

    // Navigation properties
    public virtual ApplicationRole Role { get; set; } = default!;
    public virtual Permission Permission { get; set; } = default!;

    private RolePermission() { }

    public static RolePermission Create(Guid roleId, Guid permissionId, string grantedBy)
    {
        return new RolePermission
        {
            RoleId = roleId,
            PermissionId = permissionId,
            GrantedAt = DateTime.UtcNow,
            GrantedBy = grantedBy
        };
    }
}
