

namespace AutoPart.AI
{
    public class Data
    {
        static public List<ProductInfo> Products = new List<ProductInfo>()
        {
            new ProductInfo
            {
                Id = 1,
                Name = "Brake Pads",
                Description = "High-performance brake pads for superior stopping power.",
                Price = 49.99m,
                Currency = "USD",
                SKU = "BP-1234",
                Manufacturer = "AutoParts Co.",
                Category = "Brakes",
                StockQuantity = 150,
                ImageUrl = "https://example.com/images/brake_pads.jpg"
            },
            new ProductInfo
            {            Id = 2,
                Name = "Oil Filter",
                Description = "Durable oil filter to keep your engine running smoothly.",
                Price = 15.99m,
                Currency = "USD",
                SKU = "OF-5678",
                Manufacturer = "EngineCare Inc.",
                Category = "Engine",
                StockQuantity = 300,
                ImageUrl = "https://example.com/images/oil_filter.jpg"
            },
            new ProductInfo
            {
                Id = 3,
                Name = "Air Filter",
                Description = "High-efficiency air filter for improved engine performance.",
                Price = 22.50m,
                Currency = "USD",
                SKU = "AF-9101",
                Manufacturer = "CleanAir Solutions",
                Category = "Engine",
                StockQuantity = 200,
                ImageUrl = "https://example.com/images/air_filter.jpg"
            },
            new ProductInfo
            {
                Id = 4,
                Name = "Spark Plugs",
                Description = "Set of 4 spark plugs for optimal engine ignition.",
                Price = 34.75m,
                Currency = "USD",
                SKU = "SP-1122",
                Manufacturer = "IgniteTech",
                Category = "Engine",
                StockQuantity = 250,
                ImageUrl = "https://example.com/images/spark_plugs.jpg"
            },
            new ProductInfo
            {
                Id = 5,
                Name = "Car Battery",
                Description = "Reliable car battery with long-lasting performance.",
                Price = 129.99m,
                Currency = "USD",
                SKU = "CB-3344",
                Manufacturer = "PowerDrive Batteries",
                Category = "Electrical",
                StockQuantity = 100,
                ImageUrl = "https://example.com/images/car_battery.jpg"
            }
        };
    }
}