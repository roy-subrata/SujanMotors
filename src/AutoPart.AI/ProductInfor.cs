

namespace AutoPart.AI
{
    public class ProductInfo
    {
        public ulong Id { get; set; }
        public required string Name { get; set; }
        public required string Description { get; set; }
        public decimal Price { get; set; }
        public required string Currency { get; set; }
        public required string SKU { get; set; }
        public required string Manufacturer { get; set; }
        public required string Category { get; set; }
        public ulong StockQuantity { get; set; }
        public required string ImageUrl { get; set; }
    }
}