using AutoPartShop.Application.DTOs.CustomerDtos;
using AutoPartShop.Domain.Entities;
using AutoPartShop.Domain.Repositories;
using AutoPartShop.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AutoPartShop.Api.Services;

public class CustomerAccountSummaryService : ICustomerAccountSummaryService
{
    private readonly ICustomerRepository _customerRepository;
    private readonly AutoPartDbContext _dbContext;
    private readonly ILogger<CustomerAccountSummaryService> _logger;

    public CustomerAccountSummaryService(
        ICustomerRepository customerRepository,
        AutoPartDbContext dbContext,
        ILogger<CustomerAccountSummaryService> logger)
    {
        _customerRepository = customerRepository;
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<CustomerAccountSummaryDto> GetAccountSummaryAsync(
        CustomerAccountSummaryQuery query, CancellationToken ct = default)
    {
        var customer = await _customerRepository.GetByIdAsync(query.CustomerId, ct);
        if (customer == null)
            throw new InvalidOperationException($"Customer with ID {query.CustomerId} not found");

        // Build date boundaries (inclusive on both ends)
        DateTime? fromDate = query.FromDate?.Date;
        DateTime? toDate = query.ToDate?.Date.AddDays(1).AddTicks(-1);

        // 1. Total Purchase Amount: SUM(Invoice.GrandTotal) for this customer's invoices
        //    GrandTotal is a computed property (SubTotal + TaxAmount - DiscountAmount),
        //    so we materialize matching invoices then sum in memory.
        var invoiceQuery = _dbContext.Invoices
            .AsNoTracking()
            .Include(i => i.SalesOrder)
            .Where(i => !i.Isdeleted
                && i.Status != "CANCELLED"
                && i.SalesOrder != null
                && i.SalesOrder.CustomerId == query.CustomerId);

        if (fromDate.HasValue)
            invoiceQuery = invoiceQuery.Where(i => i.InvoiceDate >= fromDate.Value);
        if (toDate.HasValue)
            invoiceQuery = invoiceQuery.Where(i => i.InvoiceDate <= toDate.Value);
        if (query.CustomerVehicleId.HasValue)
            invoiceQuery = invoiceQuery.Where(i => i.SalesOrder!.CustomerVehicleId == query.CustomerVehicleId.Value);

        var invoices = await invoiceQuery.ToListAsync(ct);
        decimal totalPurchaseAmount = invoices.Sum(i => i.GrandTotal);
        int totalInvoices = invoices.Count;
        string currency = invoices.Select(i => i.Currency)
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .FirstOrDefault() ?? "BDT";
        var currencyEntity = await _dbContext.Set<Currency>()
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Code == currency && !c.Isdeleted, ct);
        string currencySymbol = currencyEntity?.Symbol ?? currency;

        // 2. Total Paid Amount: SUM(CustomerPayment.Amount) where Status=COMPLETED
        //    Exclude advance-applied payments to avoid double-counting
        //    (same logic as SupplierLedgerService.GetTotalPaymentsAsync)
        var paymentQuery = _dbContext.CustomerPayments
            .Where(p => !p.Isdeleted
                && p.CustomerId == query.CustomerId
                && p.Status == "COMPLETED"
                && (p.PaymentType == CustomerPaymentType.ADVANCE
                    || p.SourceAdvancePaymentId == null));

        if (fromDate.HasValue)
            paymentQuery = paymentQuery.Where(p => p.PaymentDate >= fromDate.Value);
        if (toDate.HasValue)
            paymentQuery = paymentQuery.Where(p => p.PaymentDate <= toDate.Value);

        // When filtering by vehicle, only payments tied to that vehicle's invoices count.
        // Unlinked advance/credit payments (InvoiceId == null) have no vehicle, so they are
        // intentionally excluded from a vehicle-filtered paid total.
        if (query.CustomerVehicleId.HasValue)
            paymentQuery = paymentQuery.Where(p =>
                p.Invoice != null
                && p.Invoice.SalesOrder!.CustomerVehicleId == query.CustomerVehicleId.Value);

        decimal totalPaidAmount = await paymentQuery.SumAsync(p => (decimal?)p.Amount, ct) ?? 0;

        // Most recent completed payment in scope (respects date + vehicle filters above)
        var lastPayment = await paymentQuery
            .OrderByDescending(p => p.PaymentDate)
            .Select(p => new { p.PaymentDate, p.Amount })
            .FirstOrDefaultAsync(ct);

        // 3. Purchase Item Details (paginated)
        //    SalesOrderLine -> SalesOrder -> Invoice, filtered by customer + dates
        //    Uses direct navigation instead of IN clause for better performance
        var lineItemsBaseQuery = _dbContext.Set<SalesOrderLine>()
            .AsNoTracking()
            .Where(li => !li.Isdeleted
                && li.SalesOrder != null
                && li.SalesOrder.CustomerId == query.CustomerId
                && li.SalesOrder.Invoice != null
                && !li.SalesOrder.Invoice.Isdeleted
                && li.SalesOrder.Invoice.Status != "CANCELLED");

        if (fromDate.HasValue)
            lineItemsBaseQuery = lineItemsBaseQuery.Where(li => li.SalesOrder!.Invoice!.InvoiceDate >= fromDate.Value);
        if (toDate.HasValue)
            lineItemsBaseQuery = lineItemsBaseQuery.Where(li => li.SalesOrder!.Invoice!.InvoiceDate <= toDate.Value);
        if (query.CustomerVehicleId.HasValue)
            lineItemsBaseQuery = lineItemsBaseQuery.Where(li => li.SalesOrder!.CustomerVehicleId == query.CustomerVehicleId.Value);

        int totalLineItems = await lineItemsBaseQuery.CountAsync(ct);

        var pagedItems = await lineItemsBaseQuery
            .OrderByDescending(li => li.SalesOrder!.Invoice!.InvoiceDate)
            .ThenBy(li => li.SalesOrder!.Invoice!.InvoiceNumber)
            .ThenBy(li => li.LineNumber)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(li => new CustomerPurchaseItemDto
            {
                InvoiceId = li.SalesOrder!.Invoice!.Id,
                InvoiceDate = li.SalesOrder.Invoice.InvoiceDate,
                InvoiceNumber = li.SalesOrder.Invoice.InvoiceNumber,
                InvoiceStatus = li.SalesOrder.Invoice.Status,
                CustomerVehicleId = li.SalesOrder.CustomerVehicleId,
                VehicleLabel = li.SalesOrder.VehicleLabel,
                SalesOrderLineId = li.Id,
                ItemName = li.Part != null ? li.Part.Name : "",
                ItemLocalName = li.Part != null ? li.Part.LocalName : null,
                PartNumber = li.Part != null ? li.Part.PartNumber.Value : "",
                SKU = li.Part != null ? li.Part.SKU : "",
                Quantity = li.Quantity,
                UnitPrice = li.UnitPrice,
                Discount = li.Discount,
                LineTotal = (li.Quantity * li.UnitPrice) - (li.Quantity * li.Discount)
            })
            .ToListAsync(ct);

        return new CustomerAccountSummaryDto
        {
            CustomerId = customer.Id,
            CustomerName = customer.GetFullName(),
            CustomerCode = customer.CustomerCode,
            CustomerPhone = customer.Phone,
            CustomerType = customer.CustomerType,
            ReportDate = DateTime.UtcNow,
            FromDate = query.FromDate,
            ToDate = query.ToDate,
            Currency = currency,
            CurrencySymbol = currencySymbol,
            TotalPurchaseAmount = totalPurchaseAmount,
            TotalPaidAmount = totalPaidAmount,
            CurrentDue = totalPurchaseAmount - totalPaidAmount,
            LastPaymentDate = lastPayment?.PaymentDate,
            LastPaymentAmount = lastPayment?.Amount ?? 0,
            TotalInvoices = totalInvoices,
            TotalLineItems = totalLineItems,
            PurchaseItems = pagedItems,
            PurchaseItemsTotalCount = totalLineItems,
            PurchaseItemsPageNumber = query.PageNumber,
            PurchaseItemsPageSize = query.PageSize
        };
    }
}
