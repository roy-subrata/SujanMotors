namespace AutoPartShop.Domain.Entities;

public class AuditableEntity : BaseEntity
{
    public string CreatedBy { get; set; } = default!;
    public string ModifiedBy { get; set; } = default!;
    public bool Isdeleted { get; set; } = false;
}