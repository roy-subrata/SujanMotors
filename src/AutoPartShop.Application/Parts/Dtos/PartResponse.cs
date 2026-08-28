namespace AutoPartShop.Application.Parts.Dtos
{
    public class ProductResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string PartNumber { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public string? OemNumber { get; set; }
        public string? LocalName { get; set; }
        public string? VehicleFit { get; set; }
        public Guid CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public Guid? BrandId { get; set; }
        public string? BrandName { get; set; }
        public Guid? BaseUnitId { get; set; }
        public string? BaseUnitName { get; set; }
        public string? BaseUnitCode { get; set; }
        public Guid? UnitId { get; set; }
        public string? UnitName { get; set; }
        public decimal CostPrice { get; set; }
        public decimal SellingPrice { get; set; }
        public int MinimumStock { get; set; }
        public bool IsActive { get; set; }
        // Warranty Information
        public bool HasWarranty { get; set; }
        public int? WarrantyPeriodMonths { get; set; }
        public string? WarrantyType { get; set; }
        public string? WarrantyTerms { get; set; }
        public string? WarrantyCertificateTemplate { get; set; }

        // Universal product fields
        public string? Barcode { get; set; }
        public string? Tags { get; set; }
        public string ProductType { get; set; } = "PHYSICAL";
        public bool IsPerishable { get; set; }
        public decimal? WeightKg { get; set; }
        public string? TaxCode { get; set; }

        // Variant fields — populated when FlattenVariants=true
        public bool HasVariants { get; set; } = false;
        public int VariantCount { get; set; } = 0;
        public bool IsVariant { get; set; } = false;
        public Guid? VariantId { get; set; }
        public string? VariantName { get; set; }
        public string? VariantCode { get; set; }
        public string? VariantSKU { get; set; }
        public string? VariantBarcode { get; set; }
        public decimal EffectiveCostPrice { get; set; }
        public decimal EffectiveSellingPrice { get; set; }
        // "Engine Oil" for base, "Engine Oil - 5W-30 1L" for variant (base name + variant label, composed)
        public string DisplayName { get; set; } = string.Empty;

        public string CreatedBy { get; set; } = string.Empty;
        public string ModifiedBy { get; set; } = string.Empty;

        /// <summary>Cosine similarity (0..1, higher = closer) — populated only by semantic search.</summary>
        public double? SimilarityScore { get; set; }

        /// <summary>Total available stock across all warehouses (QuantityOnHand − QuantityReserved). Populated by list endpoints only.</summary>
        public int TotalStock { get; set; }

        /// <summary>Product-scoped EAV attribute values (e.g. Material). Variant-scoped values are not included here.</summary>
        public List<ProductAttributeValueSummary> AttributeValues { get; set; } = new();

        /// <summary>
        /// When the search term matched an attribute value (and not the visible Name/SKU/LocalName/
        /// PartNumber/OemNumber), the attribute label that explains the match, e.g. "Color: White".
        /// Null when the row matched by name/SKU or when there was no search term.
        /// </summary>
        public string? MatchedAttributeLabel { get; set; }

        /// <summary>Count of distinct variants whose attribute value matched the search term. Only set when more than one variant matched.</summary>
        public int? MatchedVariantCount { get; set; }
    }

    public class ProductAttributeValueSummary
    {
        public Guid AttributeId { get; set; }
        public string AttributeName { get; set; } = string.Empty;
        public string? DataType { get; set; }
        public Guid? OptionId { get; set; }
        public string? OptionValue { get; set; }
        public string? ValueText { get; set; }
        public decimal? ValueNumber { get; set; }
        public bool? ValueBool { get; set; }
    }
}
