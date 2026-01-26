using AutoPartShop.Application.DTOs.PaymentDtos;
using AutoPartShop.Domain.Entities;
using AutoPartShop.Infrastructure.Repositories;

namespace AutoPartShop.Api.Services;

/// <summary>
/// Service for calculating and retrieving comprehensive supplier payment summaries
/// </summary>
public class SupplierPaymentSummaryService
{
    private readonly ISupplierPaymentRepository _paymentRepository;
    private readonly ISupplierRepository _supplierRepository;
    private readonly IPurchaseOrderRepository _purchaseOrderRepository;
    private readonly IGoodsReceiptRepository _goodsReceiptRepository;

    public SupplierPaymentSummaryService(
        ISupplierPaymentRepository paymentRepository,
        ISupplierRepository supplierRepository,
        IPurchaseOrderRepository purchaseOrderRepository,
        IGoodsReceiptRepository goodsReceiptRepository)
    {
        _paymentRepository = paymentRepository;
        _supplierRepository = supplierRepository;
        _purchaseOrderRepository = purchaseOrderRepository;
        _goodsReceiptRepository = goodsReceiptRepository;
    }

    /// <summary>
    /// Get comprehensive payment summary for a supplier including balance calculations
    /// </summary>
    public async Task<SupplierPaymentHistorySummary> GetSupplierPaymentSummaryAsync(
        Guid supplierId,
        CancellationToken cancellationToken = default)
    {
        // Fetch supplier
        var supplier = await _supplierRepository.GetByIdAsync(supplierId, cancellationToken);
        if (supplier == null)
            throw new InvalidOperationException($"Supplier with ID {supplierId} not found");

        // Fetch all payments for this supplier
        var allPayments = (await _paymentRepository.GetBySupplierAsync(supplierId, cancellationToken)).ToList();

        // Separate by payment type
        var regularPayments = allPayments.Where(p => p.PaymentType == PaymentType.REGULAR).ToList();
        var advancePayments = allPayments.Where(p => p.PaymentType == PaymentType.ADVANCE).ToList();

        // Calculate totals
        // IMPORTANT: Pass ALL payments to CalculateTotalPaid - it has internal logic to include both
        // completed advance payments AND regular payments (excluding advance credit payments)
        decimal totalPaid = CalculateTotalPaid(allPayments);
        decimal totalAdvance = CalculateTotalAdvanceAmount(advancePayments);

        // For calculating total due, we need to pass only the regular payment amounts
        // because advance payments don't reduce the PO balance directly
        decimal regularPaymentAmount = CalculateTotalRegularPaymentAmount(regularPayments);
        decimal totalDue = await CalculateTotalDueAsync(supplierId, regularPaymentAmount, cancellationToken);

        // Calculate payment balance (Outstanding Balance)
        // This shows the NET amount owed after considering available advance credit
        // Outstanding = Total Due - Available Advance Credit
        decimal paymentBalance = Math.Max(0, totalDue - totalAdvance);

        // Get payment history (last 10)
        var paymentHistory = GetPaymentHistory(allPayments, limit: 10);

        // Get status breakdown
        var statusBreakdown = GetStatusBreakdown(allPayments);

        // Count outstanding invoices
        int outstandingInvoiceCount = regularPayments
            .Count(p => p.Status != "COMPLETED" && p.Status != "CANCELLED");

        // Count completed payments
        int completedPayments = allPayments.Count(p => p.Status == "COMPLETED");
        int pendingPayments = allPayments.Count(p => p.Status == "PENDING");
        int failedPayments = allPayments.Count(p => p.Status == "FAILED");
        int processingPayments = allPayments.Count(p => p.Status == "PROCESSING");
        int cancelledPayments = allPayments.Count(p => p.Status == "CANCELLED");
        int returnedPayments = allPayments.Count(p => p.Status == "RETURNED" || p.PaymentMethod == "REFUND");

        // Calculate total refunds (from purchase returns)
        decimal totalRefunds = CalculateTotalRefunds(allPayments);

        // Get last payment info
        var lastPayment = allPayments.OrderByDescending(p => p.PaymentDate).FirstOrDefault();

        return new SupplierPaymentHistorySummary
        {
            SupplierId = supplierId,
            SupplierName = supplier.Name,
            SupplierCode = supplier.Code,
            TotalPaid = totalPaid,
            TotalAdvanceAmount = totalAdvance,
            TotalRefunds = totalRefunds,
            TotalDue = totalDue,
            PaymentBalance = paymentBalance,
            TotalFees = CalculateTotalFees(allPayments),
            OutstandingInvoiceCount = outstandingInvoiceCount,
            CompletedPayments = completedPayments,
            PendingPayments = pendingPayments,
            FailedPayments = failedPayments,
            ProcessingPayments = processingPayments,
            CancelledPayments = cancelledPayments,
            ReturnedPayments = returnedPayments,
            LastPaymentDate = lastPayment?.PaymentDate,
            LastPaymentAmount = lastPayment?.Amount ?? 0,
            StatusBreakdown = statusBreakdown,
            PaymentHistory = paymentHistory
        };
    }

    /// <summary>
    /// Get payment status breakdown for a supplier
    /// </summary>
    public PaymentStatusBreakdown GetStatusBreakdownAsync(Guid supplierId)
    {
        var payments = _paymentRepository.GetBySupplierAsync(supplierId).Result.ToList();
        return GetStatusBreakdown(payments);
    }

    /// <summary>
    /// Get payment history for a supplier
    /// </summary>
    public async Task<List<PaymentHistoryItem>> GetPaymentHistoryAsync(
        Guid supplierId,
        int limit = 10,
        CancellationToken cancellationToken = default)
    {
        var payments = await _paymentRepository.GetBySupplierAsync(supplierId, cancellationToken);
        return GetPaymentHistory(payments.ToList(), limit);
    }

    /// <summary>
    /// Get advance payments for a supplier
    /// </summary>
    public async Task<List<SupplierPaymentResponse>> GetAdvancePaymentsAsync(
        Guid supplierId,
        CancellationToken cancellationToken = default)
    {
        var payments = await _paymentRepository.GetBySupplierAsync(supplierId, cancellationToken);
        var advancePayments = payments.Where(p => p.PaymentType == PaymentType.ADVANCE).ToList();

        return advancePayments.Select(p => MapToResponse(p)).ToList();
    }

    // Private helper methods

    private decimal CalculateTotalPaid(IEnumerable<SupplierPayment> payments)
    {
        // Only include COMPLETED payments that represent NEW money paid
        // Excludes payments created from advance (to prevent double-counting)
        // PENDING payments are commitments but not yet cleared
        return payments
            .Where(p => p.Status == "COMPLETED" &&
                       (p.PaymentType == PaymentType.ADVANCE ||  // Original advance payments
                        p.SourceAdvancePaymentId == null))       // New regular payments (not from advance)
            .Sum(p => p.Amount);
    }

    private decimal CalculateTotalRegularPaymentAmount(IEnumerable<SupplierPayment> regularPayments)
    {
        // Calculate total amount of completed regular payments for PO balance reduction
        // Include ALL completed regular payments (including those from advance credit)
        // because they ALL reduce the purchase order balance and supplier current balance
        return regularPayments
            .Where(p => p.Status == "COMPLETED")
            .Sum(p => p.Amount);
    }

    private decimal CalculateTotalAdvanceAmount(IEnumerable<SupplierPayment> payments)
    {
        // Only include COMPLETED advance payments with remaining balance
        // PENDING advances should not reduce the balance until confirmed
        // Use RemainingAmount instead of Amount to reflect actual available advance credit
        return payments
            .Where(p => p.PaymentType == PaymentType.ADVANCE &&
                       p.Status == "COMPLETED" &&
                       p.RemainingAmount > 0)
            .Sum(p => p.RemainingAmount);
    }

    private decimal CalculateTotalFees(IEnumerable<SupplierPayment> payments)
    {
        return payments
            .Where(p => p.Status == "COMPLETED")
            .Sum(p => p.PaymentFee);
    }

    private decimal CalculateTotalRefunds(IEnumerable<SupplierPayment> payments)
    {
        // Calculate total refunds from purchase returns
        // Refund payments have PaymentMethod == "REFUND" and Status == "RETURNED"
        return payments
            .Where(p => p.PaymentMethod == "REFUND" || p.Status == "RETURNED")
            .Sum(p => p.Amount);
    }

    private async Task<decimal> CalculateTotalDueAsync(
        Guid supplierId,
        decimal totalRegularPaymentsAmount,
        CancellationToken cancellationToken = default)
    {
        // Get all purchase orders for this supplier
        // IMPORTANT: Only include CONFIRMED or later POs to match supplier balance accounting
        // DRAFT and SUBMITTED POs are not yet committed and don't affect supplier balance
        var purchaseOrders = (await _purchaseOrderRepository.GetBySuppliersAsync(supplierId, cancellationToken))
            .Where(po => po.Status != "DRAFT" &&
                        po.Status != "SUBMITTED" &&
                        po.Status != "CANCELLED" &&
                        po.Status != "CLOSED")
            .ToList();

        // Calculate total amount from all confirmed/active purchase orders
        decimal totalPOAmount = purchaseOrders.Sum(po => po.TotalAmount);

        // Total due = Total PO amount - All regular payments (linked or unlinked)
        // This ensures that payments made before linking to a PO are also counted
        decimal totalDue = Math.Max(0, totalPOAmount - totalRegularPaymentsAmount);

        return totalDue;
    }

    private PaymentStatusBreakdown GetStatusBreakdown(IEnumerable<SupplierPayment> payments)
    {
        var paymentList = payments.ToList();

        return new PaymentStatusBreakdown
        {
            Pending = paymentList.Count(p => p.Status == "PENDING"),
            Completed = paymentList.Count(p => p.Status == "COMPLETED"),
            Failed = paymentList.Count(p => p.Status == "FAILED"),
            Processing = paymentList.Count(p => p.Status == "PROCESSING"),
            Cancelled = paymentList.Count(p => p.Status == "CANCELLED"),
            Reconciled = paymentList.Count(p => p.IsReconciled)
        };
    }

    private List<PaymentHistoryItem> GetPaymentHistory(
        IEnumerable<SupplierPayment> payments,
        int limit = 10)
    {
        return payments
            .OrderByDescending(p => p.PaymentDate)
            .Take(limit)
            .Select(p => new PaymentHistoryItem
            {
                Id = p.Id,
                Amount = p.Amount,
                PaymentDate = p.PaymentDate,
                Status = p.Status,
                PaymentMethod = p.PaymentMethod,
                PaymentType = p.PaymentType,
                InvoiceNumber = p.InvoiceNumber,
                TransactionNumber = p.TransactionNumber,
                ProviderName = p.PaymentMethod == "ADVANCE_CREDIT"
                    ? "Advance Credit"
                    : (p.PaymentProvider?.ProviderName ?? "N/A"),
                SourceAdvancePaymentId = p.SourceAdvancePaymentId,
                SourceAdvanceTransactionNumber = p.SourceAdvancePayment?.TransactionNumber,
                PurchaseOrderId = p.PurchaseOrderId,
                PurchaseOrderNumber = p.PurchaseOrder?.PONumber,
                GoodsReceiptId = p.GoodsReceiptId,
                GoodsReceiptNumber = p.GoodsReceipt?.GRNNumber
            })
            .ToList();
    }

    private SupplierPaymentResponse MapToResponse(SupplierPayment payment)
    {
        return new SupplierPaymentResponse
        {
            Id = payment.Id,
            SupplierId = payment.SupplierId,
            SupplierName = payment.Supplier?.Name ?? string.Empty,
            PurchaseOrderId = payment.PurchaseOrderId,
            PaymentProviderId = payment.PaymentProviderId,
            ProviderName = payment.PaymentProvider?.ProviderName ?? string.Empty,
            TransactionNumber = payment.TransactionNumber,
            Amount = payment.Amount,
            PaymentFee = payment.PaymentFee,
            NetAmount = payment.NetAmount,
            Currency = payment.Currency,
            PaymentDate = payment.PaymentDate,
            PaymentMethod = payment.PaymentMethod,
            Status = payment.Status,
            ReferenceNumber = payment.ReferenceNumber,
            AuthorizationCode = payment.AuthorizationCode,
            InvoiceNumber = payment.InvoiceNumber,
            Notes = payment.Notes,
            ProcessedDate = payment.ProcessedDate,
            ProcessedBy = payment.ProcessedBy,
            ConfirmedDate = payment.ConfirmedDate,
            ConfirmedBy = payment.ConfirmedBy,
            IsReconciled = payment.IsReconciled,
            ReconciledDate = payment.ReconciledDate,
            CreatedAt = DateTime.UtcNow,
            PaymentType = payment.PaymentType,
            Description = payment.Description
        };
    }
}
