using AutoPartShop.Application.DTOs.LedgerDtos;
using AutoPartShop.Domain.Entities;
using AutoPartShop.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AutoPartShop.Api.Services;

/// <summary>
/// Service for calculating and retrieving supplier ledger data.
/// Combines PurchaseOrders, SupplierPayments, PurchaseReturns, and CreditNotes into a unified ledger view.
/// </summary>
public class SupplierLedgerService : ISupplierLedgerService
{
    private readonly IPurchaseOrderRepository _purchaseOrderRepository;
    private readonly ISupplierPaymentRepository _supplierPaymentRepository;
    private readonly IPurchaseReturnRepository _purchaseReturnRepository;
    private readonly ICreditNoteRepository _creditNoteRepository;
    private readonly ISupplierRepository _supplierRepository;
    private readonly ILogger<SupplierLedgerService> _logger;

    public SupplierLedgerService(
        IPurchaseOrderRepository purchaseOrderRepository,
        ISupplierPaymentRepository supplierPaymentRepository,
        IPurchaseReturnRepository purchaseReturnRepository,
        ICreditNoteRepository creditNoteRepository,
        ISupplierRepository supplierRepository,
        ILogger<SupplierLedgerService> logger)
    {
        _purchaseOrderRepository = purchaseOrderRepository;
        _supplierPaymentRepository = supplierPaymentRepository;
        _purchaseReturnRepository = purchaseReturnRepository;
        _creditNoteRepository = creditNoteRepository;
        _supplierRepository = supplierRepository;
        _logger = logger;
    }

    public async Task<SupplierLedgerSummaryDto> GetLedgerSummaryAsync(
        Guid supplierId, int entryLimit = 20, CancellationToken ct = default)
    {
        var supplier = await _supplierRepository.GetByIdAsync(supplierId, ct);
        if (supplier == null)
            throw new InvalidOperationException($"Supplier with ID {supplierId} not found");

        // Calculate totals
        var totalPurchases = await GetTotalPurchasesAsync(supplierId, ct);
        var totalPayments = await GetTotalPaymentsAsync(supplierId, ct);
        var totalRefunds = await GetTotalRefundsAsync(supplierId, ct);
        var advanceCredit = await GetAvailableAdvanceCreditAsync(supplierId, ct);

        // Calculate current balance
        var currentBalance = totalPurchases - totalPayments - totalRefunds;

        // Get ledger entries
        var entries = await GetLedgerEntriesAsync(supplierId, null, null, ct);
        var recentEntries = entries.OrderByDescending(e => e.TransactionDate).Take(entryLimit).ToList();

        // Calculate running balances for entries
        CalculateRunningBalances(recentEntries);

        return new SupplierLedgerSummaryDto
        {
            SupplierId = supplierId,
            SupplierName = supplier.Name,
            SupplierCode = supplier.Code,
            TotalPurchases = totalPurchases,
            TotalPayments = totalPayments,
            TotalRefunds = totalRefunds,
            AvailableAdvanceCredit = advanceCredit,
            CurrentBalance = currentBalance,
            TransactionCount = entries.Count,
            LastTransactionDate = entries.MaxBy(e => e.TransactionDate)?.TransactionDate,
            Entries = recentEntries
        };
    }

    public async Task<decimal> CalculateCurrentBalanceAsync(Guid supplierId, CancellationToken ct = default)
    {
        var totalPurchases = await GetTotalPurchasesAsync(supplierId, ct);
        var totalPayments = await GetTotalPaymentsAsync(supplierId, ct);
        var totalRefunds = await GetTotalRefundsAsync(supplierId, ct);

        return totalPurchases - totalPayments - totalRefunds;
    }

    public async Task<PagedLedgerResult> GetLedgerEntriesAsync(
        SupplierLedgerQueryDto query, CancellationToken ct = default)
    {
        var allEntries = await GetLedgerEntriesAsync(
            query.SupplierId, query.FromDate, query.ToDate, ct);

        // Filter by transaction type if specified
        if (query.TransactionType.HasValue)
        {
            allEntries = allEntries
                .Where(e => e.TransactionType == query.TransactionType.Value)
                .ToList();
        }

        // Order by date descending
        allEntries = allEntries.OrderByDescending(e => e.TransactionDate).ToList();

        // Calculate running balances before pagination
        CalculateRunningBalances(allEntries);

        // Paginate
        var totalCount = allEntries.Count;
        var pagedEntries = allEntries
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToList();

        return new PagedLedgerResult
        {
            Entries = pagedEntries,
            TotalCount = totalCount,
            PageNumber = query.PageNumber,
            PageSize = query.PageSize
        };
    }

    public async Task<List<SupplierLedgerEntryDto>> GetLedgerEntriesAsync(
        Guid supplierId, DateTime? fromDate, DateTime? toDate, CancellationToken ct = default)
    {
        var entries = new List<SupplierLedgerEntryDto>();

        // Get purchase order entries (PURCHASE type - debits)
        var purchaseEntries = await GetPurchaseOrderEntriesAsync(supplierId, fromDate, toDate, ct);
        entries.AddRange(purchaseEntries);

        // Get payment entries (PAYMENT type - credits)
        var paymentEntries = await GetPaymentEntriesAsync(supplierId, fromDate, toDate, ct);
        entries.AddRange(paymentEntries);

        // Get purchase return entries (REFUND type - credits)
        var refundEntries = await GetRefundEntriesAsync(supplierId, fromDate, toDate, ct);
        entries.AddRange(refundEntries);

        // Sort by date
        return entries.OrderByDescending(e => e.TransactionDate).ToList();
    }

    public async Task<decimal> GetTotalPurchasesAsync(Guid supplierId, CancellationToken ct = default)
    {
        var purchaseOrders = await _purchaseOrderRepository.GetBySuppliersAsync(supplierId, ct);

        // Only include confirmed or later POs (not DRAFT, SUBMITTED, or CANCELLED)
        return purchaseOrders
            .Where(po => po.Status != "DRAFT" &&
                        po.Status != "SUBMITTED" &&
                        po.Status != "CANCELLED")
            .Sum(po => po.TotalAmount);
    }

    public async Task<decimal> GetTotalPaymentsAsync(Guid supplierId, CancellationToken ct = default)
    {
        var payments = await _supplierPaymentRepository.GetBySupplierAsync(supplierId, ct);

        // Only include COMPLETED payments
        // Exclude payments created from advance credit application to avoid double-counting
        // (the original advance payment is already counted)
        return payments
            .Where(p => p.Status == "COMPLETED" &&
                       p.PaymentMethod != "REFUND" &&  // Exclude old refund records
                       (p.PaymentType == PaymentType.ADVANCE || p.SourceAdvancePaymentId == null))
            .Sum(p => p.Amount);
    }

    public async Task<decimal> GetTotalRefundsAsync(Guid supplierId, CancellationToken ct = default)
    {
        var returns = await _purchaseReturnRepository.GetBySupplierAsync(supplierId, ct);

        // Sum settled return amounts
        return returns
            .Where(r => r.SettlementStatus == "SETTLED")
            .Sum(r => r.SettledAmount);
    }

    public async Task<decimal> GetAvailableAdvanceCreditAsync(Guid supplierId, CancellationToken ct = default)
    {
        // Get advance payment credits
        var payments = await _supplierPaymentRepository.GetBySupplierAsync(supplierId, ct);
        var advanceCredit = payments
            .Where(p => p.PaymentType == PaymentType.ADVANCE &&
                       p.Status == "COMPLETED" &&
                       p.RemainingAmount > 0)
            .Sum(p => p.RemainingAmount);

        // Get credit note credits (from returns)
        var creditNoteCredit = await _creditNoteRepository.GetTotalAvailableCreditAsync(supplierId, ct);

        return advanceCredit + creditNoteCredit;
    }

    #region Private Helper Methods

    private async Task<List<SupplierLedgerEntryDto>> GetPurchaseOrderEntriesAsync(
        Guid supplierId, DateTime? fromDate, DateTime? toDate, CancellationToken ct)
    {
        var purchaseOrders = await _purchaseOrderRepository.GetBySuppliersAsync(supplierId, ct);

        var entries = purchaseOrders
            .Where(po => po.Status != "DRAFT" &&
                        po.Status != "SUBMITTED" &&
                        po.Status != "CANCELLED")
            .Where(po => !fromDate.HasValue || po.PODate >= fromDate.Value)
            .Where(po => !toDate.HasValue || po.PODate <= toDate.Value)
            .Select(po => new SupplierLedgerEntryDto
            {
                Id = po.Id,
                TransactionDate = po.ApprovedDate ?? po.PODate,
                TransactionType = SupplierLedgerTransactionType.PURCHASE,
                ReferenceNumber = po.PONumber,
                ReferenceId = po.Id,
                DebitAmount = po.TotalAmount,
                CreditAmount = 0,
                Description = $"Purchase Order - {po.LineItems?.Count ?? 0} items",
                Status = po.Status
            })
            .ToList();

        return entries;
    }

    private async Task<List<SupplierLedgerEntryDto>> GetPaymentEntriesAsync(
        Guid supplierId, DateTime? fromDate, DateTime? toDate, CancellationToken ct)
    {
        var payments = await _supplierPaymentRepository.GetBySupplierAsync(supplierId, ct);

        var entries = payments
            .Where(p => p.Status == "COMPLETED" && p.PaymentMethod != "REFUND")
            .Where(p => !fromDate.HasValue || p.PaymentDate >= fromDate.Value)
            .Where(p => !toDate.HasValue || p.PaymentDate <= toDate.Value)
            .Select(p => new SupplierLedgerEntryDto
            {
                Id = p.Id,
                TransactionDate = p.PaymentDate,
                TransactionType = p.PaymentType == PaymentType.ADVANCE
                    ? SupplierLedgerTransactionType.ADVANCE
                    : SupplierLedgerTransactionType.PAYMENT,
                ReferenceNumber = p.TransactionNumber,
                ReferenceId = p.Id,
                DebitAmount = 0,
                CreditAmount = p.Amount,
                Description = GetPaymentDescription(p),
                Status = p.Status
            })
            .ToList();

        return entries;
    }

    private async Task<List<SupplierLedgerEntryDto>> GetRefundEntriesAsync(
        Guid supplierId, DateTime? fromDate, DateTime? toDate, CancellationToken ct)
    {
        var returns = await _purchaseReturnRepository.GetBySupplierAsync(supplierId, ct);

        var entries = returns
            .Where(r => r.SettlementStatus == "SETTLED")
            .Where(r => !fromDate.HasValue || r.SettledDate >= fromDate.Value)
            .Where(r => !toDate.HasValue || r.SettledDate <= toDate.Value)
            .Select(r => new SupplierLedgerEntryDto
            {
                Id = r.Id,
                TransactionDate = r.SettledDate ?? r.ReturnDate,
                TransactionType = SupplierLedgerTransactionType.REFUND,
                ReferenceNumber = r.ReturnNumber,
                ReferenceId = r.Id,
                DebitAmount = 0,
                CreditAmount = r.SettledAmount,
                Description = $"Purchase Return - {r.Reason} ({r.SettlementMethod})",
                Status = r.Status
            })
            .ToList();

        return entries;
    }

    private static string GetPaymentDescription(SupplierPayment payment)
    {
        var description = payment.PaymentType == PaymentType.ADVANCE
            ? "Advance Payment"
            : "Payment";

        if (!string.IsNullOrEmpty(payment.PaymentMethod))
            description += $" - {payment.PaymentMethod}";

        if (payment.SourceAdvancePaymentId.HasValue)
            description = "Applied from Advance Credit";

        return description;
    }

    private static void CalculateRunningBalances(List<SupplierLedgerEntryDto> entries)
    {
        if (!entries.Any()) return;

        // Sort by date ascending for running balance calculation
        var sortedEntries = entries.OrderBy(e => e.TransactionDate).ToList();

        decimal runningBalance = 0;
        foreach (var entry in sortedEntries)
        {
            runningBalance += entry.DebitAmount - entry.CreditAmount;
            entry.RunningBalance = runningBalance;
        }

        // Re-sort by date descending (most recent first)
        var finalOrder = entries.OrderByDescending(e => e.TransactionDate).ToList();
        entries.Clear();
        entries.AddRange(finalOrder);
    }

    #endregion
}
