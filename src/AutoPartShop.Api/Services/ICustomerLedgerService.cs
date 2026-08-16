using AutoPartShop.Application.DTOs.LedgerDtos;

namespace AutoPartShop.Api.Services;

/// <summary>
/// Service for calculating and retrieving customer ledger data.
/// Combines Invoices, CustomerPayments, and processed SalesReturns into a unified ledger view.
/// Mirrors ISupplierLedgerService's shape from the customer side of the relationship.
/// </summary>
public interface ICustomerLedgerService
{
    /// <summary>
    /// Get complete ledger summary for a customer including calculated balance and recent entries
    /// </summary>
    Task<CustomerLedgerSummaryDto> GetLedgerSummaryAsync(Guid customerId, int entryLimit = 20, CancellationToken ct = default);

    /// <summary>
    /// Calculate the current balance for a customer from all transactions.
    /// Balance = TotalInvoiced - TotalCompletedPayments - TotalProcessedRefunds
    /// </summary>
    Task<decimal> CalculateCurrentBalanceAsync(Guid customerId, CancellationToken ct = default);

    /// <summary>
    /// Get paginated ledger entries with optional filtering
    /// </summary>
    Task<PagedCustomerLedgerResult> GetLedgerEntriesAsync(CustomerLedgerQueryDto query, CancellationToken ct = default);

    /// <summary>
    /// Get all ledger entries for a customer within a date range
    /// </summary>
    Task<List<CustomerLedgerEntryDto>> GetLedgerEntriesAsync(Guid customerId, DateTime? fromDate, DateTime? toDate, CancellationToken ct = default);

    /// <summary>
    /// Get total invoiced amount (non-cancelled invoices) for a customer
    /// </summary>
    Task<decimal> GetTotalInvoicedAsync(Guid customerId, CancellationToken ct = default);

    /// <summary>
    /// Get total payments (completed payment amounts) for a customer
    /// </summary>
    Task<decimal> GetTotalPaymentsAsync(Guid customerId, CancellationToken ct = default);

    /// <summary>
    /// Get total refunds (processed sales return amounts) for a customer
    /// </summary>
    Task<decimal> GetTotalRefundsAsync(Guid customerId, CancellationToken ct = default);

    /// <summary>
    /// Get available advance credit for a customer
    /// </summary>
    Task<decimal> GetAvailableAdvanceCreditAsync(Guid customerId, CancellationToken ct = default);
}
