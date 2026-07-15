using AutoPartShop.Application.Common;

namespace AutoPartShop.Application.Parts.Dtos
{
    public class ProductQuery : BaseQuery
    {
        public bool? IsActive { get; set; } = true;
        public bool FlattenVariants { get; set; } = false;
        public Guid? CategoryId { get; set; }

        /// <summary>
        /// When true, only returns parts with at least one stock level at or below its
        /// reorder point (ReorderLevel &gt; 0 opt-in — same rule as the reorder alerts).
        /// </summary>
        public bool LowStockOnly { get; set; } = false;
    }
}
