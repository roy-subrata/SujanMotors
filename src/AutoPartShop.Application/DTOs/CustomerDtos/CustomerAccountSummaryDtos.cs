namespace AutoPartShop.Application.DTOs.CustomerDtos;

public class CustomerAccountSummaryQuery
{
    public Guid CustomerId { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class CustomerAccountSummaryDto
{
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerCode { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string CustomerType { get; set; } = string.Empty;
    public DateTime ReportDate { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }

    /// <summary>SUM(Invoice.GrandTotal) within date range</summary>
    public decimal TotalPurchaseAmount { get; set; }

    /// <summary>SUM(CustomerPayment.Amount) where Status=COMPLETED within date range</summary>
    public decimal TotalPaidAmount { get; set; }

    /// <summary>TotalPurchaseAmount - TotalPaidAmount (calculated dynamically)</summary>
    public decimal CurrentDue { get; set; }

    public int TotalInvoices { get; set; }
    public int TotalLineItems { get; set; }

    public List<CustomerPurchaseItemDto> PurchaseItems { get; set; } = new();
    public int PurchaseItemsTotalCount { get; set; }
    public int PurchaseItemsPageNumber { get; set; }
    public int PurchaseItemsPageSize { get; set; }
    public int PurchaseItemsTotalPages =>
        PurchaseItemsPageSize > 0
            ? (int)Math.Ceiling(PurchaseItemsTotalCount / (double)PurchaseItemsPageSize)
            : 0;
}

public class CustomerPurchaseItemDto
{
    public Guid InvoiceId { get; set; }
    public DateTime InvoiceDate { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public string InvoiceStatus { get; set; } = string.Empty;
    public Guid SalesOrderLineId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public string PartNumber { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Discount { get; set; }
    public decimal LineTotal { get; set; }
}
