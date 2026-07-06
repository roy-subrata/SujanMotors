namespace AutoPartShop.Domain.Entities;

public class AuditableEntity : BaseEntity
{
    public DateTime CreatedDate { get; set; } = default(DateTime);
    public DateTime ModifiedDate { get; set; } = default(DateTime);
    public string CreatedBy { get; set; } = default!;
    public string ModifiedBy { get; set; } = default!;
    public bool Isdeleted { get; set; } = false;
}