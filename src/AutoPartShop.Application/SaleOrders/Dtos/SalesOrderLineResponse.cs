namespace AutoPartShop.Application.SaleOrders.Dtos
{
    public class SalesOrderLineResponse
    {
        public Guid Id { get; set; }
        public Guid PartId { get; set; }
        public string PartName { get; set; } = string.Empty;
        public string PartSku { get; set; } = string.Empty;
        public Guid? ProductVariantId { get; set; }
        public string? VariantName { get; set; }
        public string? VariantSku { get; set; }
        public Guid? UnitId { get; set; }
        public string UnitName { get; set; } = string.Empty;
        public string UnitSymbol { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public int QuantityInBaseUnit { get; set; }
        public int ShippedQuantity { get; set; }
        public int ShippedQuantityInBaseUnit { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Discount { get; set; }
        public decimal LineTotal { get; set; }
    }

}
