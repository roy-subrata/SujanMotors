using AutoPartShop.Application.Common;

namespace AutoPartShop.Application.Parts.Dtos
{
    public class PartQuery : BaseQuery
    {
        public bool? IsActive { get; set; } = true;
        public bool FlattenVariants { get; set; } = false;
    }
}
