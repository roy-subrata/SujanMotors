using AutoPartShop.Domain.Enums;

namespace AutoPartShop.Application.SaleOrders.Dtos
{
    public class SaleOrderResponse
    {
        public Guid Id { get; set; }
        public string SONumber { get; set; } = string.Empty;
        public Guid CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        public string CustomerCity { get; set; } = string.Empty;
        public Guid? WarehouseId { get; set; }
        public Guid? TechnicianId { get; set; }
        public string? TechnicianName { get; set; }
        public Guid? CustomerVehicleId { get; set; }
        public string VehicleLabel { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public DateTime DeliveryDate { get; set; }
        public SalesOrderStatus Status { get; set; }
        public string Channel { get; set; } = string.Empty; // POS | MOBILE | API
        public DateTime? PaidDate { get; set; }
        public DateTime? PackedDate { get; set; }
        public DateTime? CompletedDate { get; set; }
        public decimal SubTotal { get; set; }
        public decimal TaxAmount { get; set; }
        /// <summary>
        /// Total order-level discount in currency. Previously carried DiscountPercentage, which
        /// reads 0 for the fixed-amount discounts a quick sale applies — so the header did not
        /// foot (subTotal 200, discount 0, grandTotal 180).
        /// </summary>
        public decimal Discount { get; set; }

        /// <summary>Percentage form of the order discount, when one was given as a percentage.</summary>
        public decimal DiscountPercentage { get; set; }

        /// <summary>Fixed-amount form of the order discount, as applied by quick sale.</summary>
        public decimal DiscountAmount { get; set; }

        /// <summary>Promo code that was applied to this order (if any).</summary>
        public string? AppliedPromoCode { get; set; }

        /// <summary>FK to the Discount rule applied at cart level (if any).</summary>
        public Guid? CartDiscountRuleId { get; set; }

        public decimal GrandTotal { get; set; }
        public string Currency { get; set; } = string.Empty;
        public decimal AmountPaid { get; set; }
        public decimal OutstandingAmount { get; set; }
        public bool IsOverdue { get; set; }
        public string Notes { get; set; } = string.Empty;
        public List<SalesOrderLineResponse> Lines { get; set; } = new();
        public DateTime CreatedAt { get; set; }
    }

}
