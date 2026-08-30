using AutoPartShop.Application.Common;

namespace AutoPartShop.Application.Parts.Dtos
{
    public class ProductQuery : BaseQuery
    {
        public bool? IsActive { get; set; } = true;
        public bool FlattenVariants { get; set; } = false;
        public Guid? CategoryId { get; set; }

        /// <summary>
        /// When set, only returns parts linked to at least one of these vehicles via a
        /// PartVehicleCompatibility row (IsCompatible == true). A part can be compatible with
        /// multiple vehicles, so this is a multi-select filter.
        /// </summary>
        public IReadOnlyCollection<Guid>? VehicleIds { get; set; }

        /// <summary>
        /// When true, only returns parts with at least one stock level at or below its
        /// reorder point (ReorderLevel &gt; 0 opt-in — same rule as the reorder alerts).
        /// </summary>
        public bool LowStockOnly { get; set; } = false;

        /// <summary>
        /// Flat list of selected <c>ProductAttributeOption</c> ids for faceted filtering (e.g. Color:
        /// Red, Size: Large). Options are grouped by their attribute — a product must match ALL
        /// selected attributes (AND across attributes), but only ANY one of the selected options
        /// within each attribute (OR within an attribute). Only option/dropdown-type attributes are
        /// supported; text/number/boolean attribute filtering is not covered by this filter.
        /// </summary>
        public IReadOnlyCollection<Guid>? AttributeOptionIds { get; set; }
    }
}
