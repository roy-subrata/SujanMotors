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
        decimal totalPaid = CalculateTotalPaid(regularPayments);
        decimal totalAdvance = CalculateTotalAdvanceAmount(advancePayments);
        decimal totalDue = await CalculateTotalDueAsync(supplierId, totalPaid, cancellationToken);

        // Calculate payment balance
        decimal paymentBalance = totalDue - totalAdvance - totalPaid;

        // Get payment history (last 10)
        var paymentHistory = GetPaymentHistory(allPayments, limit: 10);

        // Get status breakdown
        var statusBreakdown = GetStatusBreakdown(allPayments);

        // Calculate credit utilization
        decimal creditUtilization = supplier.CreditLimit > 0
            ? (decimal)Math.Min(100, (totalDue / supplier.CreditLimit) * 100)
            : 0;

        // Count outstanding invoices
        int outstandingInvoiceCount = regularPayments
            .Where(p => p.Status != "COMPLETED" && p.Status != "CANCELLED")
            .Count();

        // Count completed payments
        int completedPayments = allPayments.Count(p => p.Status == "COMPLETED");
        int pendingPayments = allPayments.Count(p => p.Status == "PENDING");
        int failedPayments = allPayments.Count(p => p.Status == "FAILED");
        int processingPayments = allPayments.Count(p => p.Status == "PROCESSING");
        int cancelledPayments = allPayments.Count(p => p.Status == "CANCELLED");

        // Get last payment info
        var lastPayment = allPayments.OrderByDescending(p => p.PaymentDate).FirstOrDefault();

        return new SupplierPaymentHistorySummary
        {
            SupplierId = supplierId,
            SupplierName = supplier.Name,
            SupplierCode = supplier.Code,
            CreditLimit = supplier.CreditLimit,
            CreditUtilization = creditUtilization,
            TotalPaid = totalPaid,
            TotalAdvanceAmount = totalAdvance,
            TotalDue = totalDue,
            PaymentBalance = paymentBalance,
            TotalFees = CalculateTotalFees(allPayments),
            OutstandingInvoiceCount = outstandingInvoiceCount,
            CompletedPayments = completedPayments,
            PendingPayments = pendingPayments,
            FailedPayments = failedPayments,
            ProcessingPayments = processingPayments,
            CancelledPayments = cancelledPayments,
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
        return payments
            .Where(p => p.Status == "COMPLETED")
            .Sum(p => p.Amount);
    }

    private decimal CalculateTotalAdvanceAmount(IEnumerable<SupplierPayment> payments)
    {
        return payments
            .Where(p => p.Status == "COMPLETED" && p.PaymentType == PaymentType.ADVANCE)
            .Sum(p => p.Amount);
    }

    private decimal CalculateTotalFees(IEnumerable<SupplierPayment> payments)
    {
        return payments
            .Where(p => p.Status == "COMPLETED")
            .Sum(p => p.PaymentFee);
    }

    private async Task<decimal> CalculateTotalDueAsync(
        Guid supplierId,
        decimal paidAmount,
        CancellationToken cancellationToken = default)
    {
        // Get all purchase orders for this supplier
        var purchaseOrders = (await _purchaseOrderRepository.GetBySuppliersAsync(supplierId, cancellationToken))
            .Where(po => po.Status != "CANCELLED" && po.Status != "CLOSED")
            .ToList();

        decimal totalDue = 0;

        foreach (var po in purchaseOrders)
        {
            // Calculate PO total and paid amount
            decimal poTotal = po.TotalAmount;

            // Get payments linked to this PO
            var poPayments = (await _paymentRepository.GetByPurchaseOrderAsync(po.Id, cancellationToken))
                .Where(p => p.Status == "COMPLETED")
                .ToList();

            decimal poPaid = poPayments.Sum(p => p.Amount);
            decimal poRemaining = Math.Max(0, poTotal - poPaid);

            totalDue += poRemaining;
        }

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
                ProviderName = p.PaymentProvider?.ProviderName ?? "N/A"
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
