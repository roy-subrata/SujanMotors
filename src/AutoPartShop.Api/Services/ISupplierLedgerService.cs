using AutoPartShop.Application.DTOs.LedgerDtos;

namespace AutoPartShop.Api.Services;

/// <summary>
/// Service for calculating and retrieving supplier ledger data.
/// Combines PurchaseOrders, SupplierPayments, and PurchaseReturns into a unified ledger view.
/// </summary>
public interface ISupplierLedgerService
{
    /// <summary>
    /// Get complete ledger summary for a supplier including calculated balance and recent entries
    /// </summary>
    Task<SupplierLedgerSummaryDto> GetLedgerSummaryAsync(Guid supplierId, int entryLimit = 20, CancellationToken ct = default);

    /// <summary>
    /// Calculate the current balance for a supplier from all transactions.
    /// Balance = TotalConfirmedPOs - TotalCompletedPayments - TotalSettledRefunds
    /// </summary>
    Task<decimal> CalculateCurrentBalanceAsync(Guid supplierId, CancellationToken ct = default);

    /// <summary>
    /// Get paginated ledger entries with optional filtering
    /// </summary>
    Task<PagedLedgerResult> GetLedgerEntriesAsync(SupplierLedgerQueryDto query, CancellationToken ct = default);

    /// <summary>
    /// Get all ledger entries for a supplier within a date range
    /// </summary>
    Task<List<SupplierLedgerEntryDto>> GetLedgerEntriesAsync(Guid supplierId, DateTime? fromDate, DateTime? toDate, CancellationToken ct = default);

    /// <summary>
    /// Get total purchases (confirmed PO amounts) for a supplier
    /// </summary>
    Task<decimal> GetTotalPurchasesAsync(Guid supplierId, CancellationToken ct = default);

    /// <summary>
    /// Get total payments (completed payment amounts) for a supplier
    /// </summary>
    Task<decimal> GetTotalPaymentsAsync(Guid supplierId, CancellationToken ct = default);

    /// <summary>
    /// Get total refunds (settled purchase return amounts) for a supplier
    /// </summary>
    Task<decimal> GetTotalRefundsAsync(Guid supplierId, CancellationToken ct = default);

    /// <summary>
    /// Get available advance credit for a supplier
    /// </summary>
    Task<decimal> GetAvailableAdvanceCreditAsync(Guid supplierId, CancellationToken ct = default);
}
