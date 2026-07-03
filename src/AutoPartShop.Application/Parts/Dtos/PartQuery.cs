using AutoPartShop.Application.Common;

namespace AutoPartShop.Application.Parts.Dtos
{
    public class ProductQuery : BaseQuery
    {
        public bool? IsActive { get; set; } = true;
        public bool FlattenVariants { get; set; } = false;
        public Guid? CategoryId { get; set; }
    }
}
