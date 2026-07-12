using Xunit;
using AutoPartShop.Domain.Entities;

namespace AutoPartShop.Api.Tests;

public class PaymentFinancialSmokeTests
{
    private readonly Guid _customerId = Guid.NewGuid();
    private readonly Guid _supplierId = Guid.NewGuid();
    private readonly Guid _salesOrderId = Guid.NewGuid();
    private readonly Guid _invoiceId = Guid.NewGuid();
    private readonly Guid _purchaseOrderId = Guid.NewGuid();
    private readonly Guid _paymentProviderId = Guid.NewGuid();

    // ========== CUSTOMER PAYMENT ==========

    [Fact]
    public void CustomerPayment_Create_ShouldInitializeCorrectly()
    {
        var cp = CustomerPayment.Create(_customerId, _paymentProviderId, 500m, "CASH");
        Assert.Equal(500m, cp.Amount);
        Assert.Equal(500m, cp.NetAmount);
        Assert.Equal("CASH", cp.PaymentMethod);
        Assert.Equal("PENDING", cp.Status);
        Assert.Equal(CustomerPaymentType.REGULAR, cp.PaymentType);
    }

    [Fact]
    public void CustomerPayment_CreateWithNegativeAmount_ShouldThrowUnlessRefund()
    {
        Assert.Throws<ArgumentException>(() =>
            CustomerPayment.Create(_customerId, _paymentProviderId, -100m, "CASH"));
    }

    [Fact]
    public void CustomerPayment_CreateWithNegativeRefund_ShouldSucceed()
    {
        var cp = CustomerPayment.Create(_customerId, _paymentProviderId, -100m, "REFUND");
        Assert.Equal(-100m, cp.Amount);
    }

    [Fact]
    public void CustomerPayment_CreateWithZeroAmount_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() =>
            CustomerPayment.Create(_customerId, _paymentProviderId, 0m, "CASH"));
    }

    [Fact]
    public void CustomerPayment_CreateWithEmptyCustomer_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() =>
            CustomerPayment.Create(Guid.Empty, _paymentProviderId, 100m, "CASH"));
    }

    [Fact]
    public void CustomerPayment_FullLifecycle_ShouldTransitionCorrectly()
    {
        var cp = CustomerPayment.Create(_customerId, _paymentProviderId, 500m, "BANK_TRANSFER");
        Assert.Equal("PENDING", cp.Status);

        cp.MarkAsProcessing();
        Assert.Equal("PROCESSING", cp.Status);

        cp.MarkAsCompleted();
        Assert.Equal("COMPLETED", cp.Status);

        cp.Reconcile();
        Assert.True(cp.IsReconciled);
        Assert.NotNull(cp.ReconciledDate);
    }

    [Fact]
    public void CustomerPayment_MarkAsProcessing_FromWrongStatus_ShouldThrow()
    {
        var cp = CustomerPayment.Create(_customerId, _paymentProviderId, 100m, "CASH");
        cp.MarkAsCompleted();
        Assert.Throws<InvalidOperationException>(() => cp.MarkAsProcessing());
    }

    [Fact]
    public void CustomerPayment_MarkAsFailed_ShouldWorkFromProcessing()
    {
        var cp = CustomerPayment.Create(_customerId, _paymentProviderId, 100m, "CARD");
        cp.MarkAsProcessing();
        cp.MarkAsFailed();
        Assert.Equal("FAILED", cp.Status);
    }

    [Fact]
    public void CustomerPayment_MarkAsCompleted_FromFailed_ShouldThrow()
    {
        var cp = CustomerPayment.Create(_customerId, _paymentProviderId, 100m, "CASH");
        cp.MarkAsFailed();
        Assert.Throws<InvalidOperationException>(() => cp.MarkAsCompleted());
    }

    [Fact]
    public void CustomerPayment_MarkAsRefunded_ShouldValidate()
    {
        var cp = CustomerPayment.Create(_customerId, _paymentProviderId, 500m, "CASH");
        cp.MarkAsCompleted();

        Assert.Throws<ArgumentException>(() => cp.MarkAsRefunded(0));
        Assert.Throws<InvalidOperationException>(() => cp.MarkAsRefunded(600m));

        cp.MarkAsRefunded(200m);
        Assert.Equal("REFUNDED", cp.Status);
    }

    [Fact]
    public void CustomerPayment_Cancel_ShouldOnlyWorkBeforeComplete()
    {
        var cp = CustomerPayment.Create(_customerId, _paymentProviderId, 100m, "CASH");
        cp.Cancel();
        Assert.Equal("CANCELLED", cp.Status);
    }

    [Fact]
    public void CustomerPayment_Cancel_AfterCompleted_ShouldThrow()
    {
        var cp = CustomerPayment.Create(_customerId, _paymentProviderId, 100m, "CASH");
        cp.MarkAsCompleted();
        Assert.Throws<InvalidOperationException>(() => cp.Cancel());
    }

    [Fact]
    public void CustomerPayment_SetFee_ShouldReduceNetAmount()
    {
        var cp = CustomerPayment.Create(_customerId, _paymentProviderId, 500m, "CARD");
        cp.SetFee(15m);
        Assert.Equal(15m, cp.PaymentFee);
        Assert.Equal(485m, cp.NetAmount);
    }

    [Fact]
    public void CustomerPayment_SetFee_Negative_ShouldThrow()
    {
        var cp = CustomerPayment.Create(_customerId, _paymentProviderId, 100m, "CASH");
        Assert.Throws<ArgumentException>(() => cp.SetFee(-5m));
    }

    [Fact]
    public void CustomerPayment_MarkAsAdvance_ShouldTrackRemaining()
    {
        var cp = CustomerPayment.Create(_customerId, _paymentProviderId, 1000m, "CASH");
        cp.MarkAsAdvance();
        Assert.Equal(CustomerPaymentType.ADVANCE, cp.PaymentType);
        Assert.Equal(1000m, cp.RemainingAmount);
        Assert.True(cp.HasRemainingBalance());
    }

    [Fact]
    public void CustomerPayment_ReduceRemainingAmount_ShouldDecrease()
    {
        var cp = CustomerPayment.Create(_customerId, _paymentProviderId, 1000m, "CASH");
        cp.MarkAsAdvance();
        cp.ReduceRemainingAmount(300m);
        Assert.Equal(700m, cp.RemainingAmount);
        Assert.Throws<InvalidOperationException>(() => cp.ReduceRemainingAmount(800m));
    }

    [Fact]
    public void CustomerPayment_MarkAsRegular_WithRemainingBalance_ShouldThrow()
    {
        var cp = CustomerPayment.Create(_customerId, _paymentProviderId, 500m, "CASH");
        cp.MarkAsAdvance();
        Assert.Throws<InvalidOperationException>(() => cp.MarkAsRegular());
    }

    [Fact]
    public void CustomerPayment_CreateFromAdvance_ShouldCreateCompletedPayment()
    {
        var cp = CustomerPayment.CreateFromAdvance(_customerId, _invoiceId, Guid.NewGuid(),
            _paymentProviderId, 200m, "Applied advance credit");

        Assert.Equal("COMPLETED", cp.Status);
        Assert.Equal("ADVANCE_CREDIT", cp.PaymentMethod);
        Assert.Equal(CustomerPaymentType.REGULAR, cp.PaymentType);
        Assert.Equal(200m, cp.Amount);
    }

    // ========== SUPPLIER PAYMENT ==========

    [Fact]
    public void SupplierPayment_Create_ShouldInitializeCorrectly()
    {
        var sp = SupplierPayment.Create(_supplierId, _paymentProviderId, 1000m, "BANK_TRANSFER");
        Assert.Equal(1000m, sp.Amount);
        Assert.Equal("BANK_TRANSFER", sp.PaymentMethod);
        Assert.Equal("PENDING", sp.Status);
        Assert.Equal(PaymentType.REGULAR, sp.PaymentType);
        Assert.False(sp.IsReconciled);
    }

    [Fact]
    public void SupplierPayment_Create_WithInvalidInputs_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() =>
            SupplierPayment.Create(Guid.Empty, _paymentProviderId, 100m, "CASH"));
        Assert.Throws<ArgumentException>(() =>
            SupplierPayment.Create(_supplierId, Guid.Empty, 100m, "CASH"));
        Assert.Throws<ArgumentException>(() =>
            SupplierPayment.Create(_supplierId, _paymentProviderId, 0m, "CASH"));
        Assert.Throws<ArgumentException>(() =>
            SupplierPayment.Create(_supplierId, _paymentProviderId, 100m, ""));
    }

    [Fact]
    public void SupplierPayment_FullLifecycle_ShouldTransitionCorrectly()
    {
        var sp = SupplierPayment.Create(_supplierId, _paymentProviderId, 5000m, "BANK_TRANSFER");
        Assert.Equal("PENDING", sp.Status);

        sp.MarkAsProcessing();
        Assert.Equal("PROCESSING", sp.Status);

        sp.MarkAsProcessed("finance_user");
        Assert.Equal("COMPLETED", sp.Status);
        Assert.NotNull(sp.ProcessedDate);
        Assert.Equal("finance_user", sp.ProcessedBy);

        sp.ConfirmReceipt("supplier_contact");
        Assert.NotNull(sp.ConfirmedDate);
        Assert.Equal("supplier_contact", sp.ConfirmedBy);

        sp.Reconcile();
        Assert.True(sp.IsReconciled);

        sp.LinkToPurchaseOrder(_purchaseOrderId);
        sp.SetFee(25m);
        Assert.Equal(4975m, sp.NetAmount);
    }

    [Fact]
    public void SupplierPayment_StatusTransitions_ShouldValidate()
    {
        var sp = SupplierPayment.Create(_supplierId, _paymentProviderId, 100m, "CASH");

        Assert.Throws<InvalidOperationException>(() => sp.ConfirmReceipt("x"));
        Assert.Throws<InvalidOperationException>(() => sp.Reconcile());

        sp.MarkAsProcessing();
        sp.MarkAsProcessed("user");

        Assert.Throws<InvalidOperationException>(() => sp.MarkAsProcessing());
    }

    [Fact]
    public void SupplierPayment_MarkAsFailed_ShouldWorkFromAnyNonFinalState()
    {
        var sp = SupplierPayment.Create(_supplierId, _paymentProviderId, 100m, "CASH");
        sp.MarkAsProcessing();
        sp.MarkAsFailed();
        Assert.Equal("FAILED", sp.Status);
    }

    [Fact]
    public void SupplierPayment_MarkAsReturned_ShouldOnlyWorkFromCompleted()
    {
        var sp = SupplierPayment.Create(_supplierId, _paymentProviderId, 100m, "CASH");
        Assert.Throws<InvalidOperationException>(() => sp.MarkAsReturned());

        sp.MarkAsProcessing();
        sp.MarkAsProcessed("user");
        sp.MarkAsReturned();
        Assert.Equal("RETURNED", sp.Status);
    }

    [Fact]
    public void SupplierPayment_Cancel_ShouldThrowOnCompletedOrReturned()
    {
        var sp = SupplierPayment.Create(_supplierId, _paymentProviderId, 100m, "CASH");
        sp.Cancel();
        Assert.Equal("CANCELLED", sp.Status);
    }

    [Fact]
    public void SupplierPayment_AdvanceFlow_ShouldTrackRemaining()
    {
        var sp = SupplierPayment.Create(_supplierId, _paymentProviderId, 2000m, "BANK_TRANSFER");
        sp.MarkAsProcessing();
        sp.MarkAsProcessed("user");
        sp.MarkAsAdvance();
        Assert.Equal(PaymentType.ADVANCE, sp.PaymentType);
        Assert.Equal(2000m, sp.RemainingAmount);
        Assert.True(sp.HasRemainingBalance());

        sp.ReduceRemainingAmount(500m);
        Assert.Equal(1500m, sp.RemainingAmount);

        sp.MarkAsRegular();
        Assert.Equal(PaymentType.REGULAR, sp.PaymentType);
        Assert.Equal(0m, sp.RemainingAmount);
    }

    [Fact]
    public void SupplierPayment_ReduceRemainingAmount_ShouldValidate()
    {
        var sp = SupplierPayment.Create(_supplierId, _paymentProviderId, 1000m, "CASH");
        sp.MarkAsProcessing();
        sp.MarkAsProcessed("user");
        sp.MarkAsAdvance();

        Assert.Throws<ArgumentException>(() => sp.ReduceRemainingAmount(0));
        Assert.Throws<InvalidOperationException>(() => sp.ReduceRemainingAmount(2000m));
    }

    [Fact]
    public void SupplierPayment_LinkToSourceAdvance_ShouldOnlyWorkForRegular()
    {
        var sp = SupplierPayment.Create(_supplierId, _paymentProviderId, 500m, "CASH");
        sp.MarkAsAdvance();
        Assert.Throws<InvalidOperationException>(() => sp.LinkToSourceAdvance(Guid.NewGuid()));
    }

    [Fact]
    public void SupplierPayment_CreateFromAdvance_ShouldCreateCompleted()
    {
        var sp = SupplierPayment.CreateFromAdvance(
            _supplierId, _purchaseOrderId, Guid.NewGuid(), _paymentProviderId, 300m, "Applied advance");

        Assert.Equal("COMPLETED", sp.Status);
        Assert.Equal(PaymentType.REGULAR, sp.PaymentType);
        Assert.Equal("ADVANCE_CREDIT", sp.PaymentMethod);
        Assert.Equal(300m, sp.Amount);
    }

    // ========== CREDIT NOTE (Supplier) ==========

    [Fact]
    public void CreditNote_Create_ShouldInitializeCorrectly()
    {
        var cn = CreditNote.Create("CN-001", _supplierId, Guid.NewGuid(), 1000m, "BDT");
        Assert.Equal("CN-001", cn.CreditNoteNumber);
        Assert.Equal(1000m, cn.TotalAmount);
        Assert.Equal(1000m, cn.AvailableAmount);
        Assert.Equal("AVAILABLE", cn.Status);
    }

    [Fact]
    public void CreditNote_Create_WithInvalidInputs_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() =>
            CreditNote.Create("", _supplierId, null, 100m));
        Assert.Throws<ArgumentException>(() =>
            CreditNote.Create("CN-X", Guid.Empty, null, 100m));
        Assert.Throws<ArgumentException>(() =>
            CreditNote.Create("CN-X", _supplierId, null, 0m));
        Assert.Throws<ArgumentException>(() =>
            CreditNote.Create("CN-X", _supplierId, null, 100m, expiryDate: DateTime.UtcNow.AddDays(-1),
                issueDate: DateTime.UtcNow));
    }

    [Fact]
    public void CreditNote_ApplyToPurchaseOrder_ShouldReduceAvailable()
    {
        var cn = CreditNote.Create("CN-002", _supplierId, Guid.NewGuid(), 1000m, "BDT");

        var remaining = cn.ApplyToPurchaseOrder(_purchaseOrderId, 400m);
        Assert.Equal(600m, remaining);
        Assert.Equal(600m, cn.AvailableAmount);
        Assert.Equal(400m, cn.UsedAmount);
        Assert.Equal("PARTIALLY_USED", cn.Status);
    }

    [Fact]
    public void CreditNote_ApplyFully_ShouldMarkFullyUsed()
    {
        var cn = CreditNote.Create("CN-003", _supplierId, Guid.NewGuid(), 500m);
        cn.ApplyToPurchaseOrder(_purchaseOrderId, 500m);
        Assert.Equal("FULLY_USED", cn.Status);
        Assert.Equal(0m, cn.AvailableAmount);
    }

    [Fact]
    public void CreditNote_ApplyExceedingAvailable_ShouldThrow()
    {
        var cn = CreditNote.Create("CN-004", _supplierId, Guid.NewGuid(), 500m);
        Assert.Throws<InvalidOperationException>(() =>
            cn.ApplyToPurchaseOrder(_purchaseOrderId, 600m));
    }

    [Fact]
    public void CreditNote_ApplyCancelled_ShouldThrow()
    {
        var cn = CreditNote.Create("CN-005", _supplierId, Guid.NewGuid(), 500m);
        cn.Cancel();
        Assert.Throws<InvalidOperationException>(() =>
            cn.ApplyToPurchaseOrder(_purchaseOrderId, 100m));
    }

    [Fact]
    public void CreditNote_Cancel_WithUsage_ShouldThrow()
    {
        var cn = CreditNote.Create("CN-006", _supplierId, Guid.NewGuid(), 500m);
        cn.ApplyToPurchaseOrder(_purchaseOrderId, 200m);
        Assert.Throws<InvalidOperationException>(() => cn.Cancel());
    }

    [Fact]
    public void CreditNote_MarkAsExpired_ShouldUpdateStatus()
    {
        var cn = CreditNote.Create("CN-007", _supplierId, Guid.NewGuid(), 500m);
        cn.MarkAsExpired();
        Assert.Equal("EXPIRED", cn.Status);
        Assert.False(cn.IsAvailable());
    }

    [Fact]
    public void CreditNote_IsAvailable_ShouldReturnTrueWhenValid()
    {
        var cn = CreditNote.Create("CN-008", _supplierId, Guid.NewGuid(), 500m);
        Assert.True(cn.IsAvailable());
        cn.ApplyToPurchaseOrder(_purchaseOrderId, 500m);
        Assert.False(cn.IsAvailable());
    }

    // ========== CUSTOMER CREDIT NOTE ==========

    [Fact]
    public void CustomerCreditNote_Create_ShouldInitializeCorrectly()
    {
        var ccn = CustomerCreditNote.Create("CCN-001", _customerId, Guid.NewGuid(), 500m, "BDT");
        Assert.Equal("CCN-001", ccn.CreditNoteNumber);
        Assert.Equal(500m, ccn.TotalAmount);
        Assert.Equal("AVAILABLE", ccn.Status);
    }

    [Fact]
    public void CustomerCreditNote_Create_WithInvalidInputs_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() =>
            CustomerCreditNote.Create("", _customerId, null, 100m));
        Assert.Throws<ArgumentException>(() =>
            CustomerCreditNote.Create("CCN-X", Guid.Empty, null, 100m));
        Assert.Throws<ArgumentException>(() =>
            CustomerCreditNote.Create("CCN-X", _customerId, null, 0m));
    }

    [Fact]
    public void CustomerCreditNote_ApplyToInvoice_ShouldReduceAvailable()
    {
        var ccn = CustomerCreditNote.Create("CCN-002", _customerId, Guid.NewGuid(), 300m, "BDT");
        ccn.ApplyToInvoice(_invoiceId, _salesOrderId, 150m);
        Assert.Equal(150m, ccn.AvailableAmount);
        Assert.Equal("PARTIALLY_USED", ccn.Status);

        ccn.ApplyToInvoice(Guid.NewGuid(), _salesOrderId, 150m);
        Assert.Equal("FULLY_USED", ccn.Status);
        Assert.Equal(0m, ccn.AvailableAmount);
    }

    [Fact]
    public void CustomerCreditNote_ApplyExceedingAvailable_ShouldThrow()
    {
        var ccn = CustomerCreditNote.Create("CCN-003", _customerId, null, 200m);
        Assert.Throws<InvalidOperationException>(() =>
            ccn.ApplyToInvoice(_invoiceId, _salesOrderId, 300m));
    }

    [Fact]
    public void CustomerCreditNote_Cancel_WithUsage_ShouldThrow()
    {
        var ccn = CustomerCreditNote.Create("CCN-004", _customerId, null, 500m);
        ccn.ApplyToInvoice(_invoiceId, _salesOrderId, 100m);
        Assert.Throws<InvalidOperationException>(() => ccn.Cancel());
    }

    [Fact]
    public void CustomerCreditNote_LinkToWarrantyClaim_ShouldSetReference()
    {
        var ccn = CustomerCreditNote.Create("CCN-005", _customerId, null, 200m);
        var warrantyClaimId = Guid.NewGuid();
        ccn.LinkToWarrantyClaim(warrantyClaimId);
    }

    // ========== INVOICE ==========

    [Fact]
    public void Invoice_Create_ShouldInitializeCorrectly()
    {
        var inv = Invoice.Create("INV-001", _salesOrderId, 1000m, 50m, DateTime.UtcNow.AddDays(30));
        Assert.Equal("INV-001", inv.InvoiceNumber);
        Assert.Equal(1000m, inv.SubTotal);
        Assert.Equal(50m, inv.TaxAmount);
        Assert.Equal(1050m, inv.GrandTotal);
        Assert.Equal("DRAFT", inv.Status);
    }

    [Fact]
    public void Invoice_Create_WithInvalidInputs_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() =>
            Invoice.Create("", _salesOrderId, 100m, 0));
        Assert.Throws<ArgumentException>(() =>
            Invoice.Create("INV-X", Guid.Empty, 100m, 0));
        Assert.Throws<ArgumentException>(() =>
            Invoice.Create("INV-X", _salesOrderId, 0m, 0));
        Assert.Throws<ArgumentException>(() =>
            Invoice.Create("INV-X", _salesOrderId, 100m, -5m));
    }

    [Fact]
    public void Invoice_FullLifecycle_ShouldTransitionCorrectly()
    {
        var inv = Invoice.Create("INV-002", _salesOrderId, 2000m, 100m);
        Assert.Equal("DRAFT", inv.Status);

        inv.Issue();
        Assert.Equal("ISSUED", inv.Status);

        inv.MarkAsDue();
        Assert.Equal("DUE", inv.Status);
    }

    [Fact]
    public void Invoice_SetDiscount_ShouldValidate()
    {
        var inv = Invoice.Create("INV-003", _salesOrderId, 1000m, 0m);

        Assert.Throws<ArgumentException>(() => inv.SetDiscount(-1m));
        Assert.Throws<ArgumentException>(() => inv.SetDiscount(2000m));

        inv.SetDiscount(100m);
        Assert.Equal(100m, inv.DiscountAmount);
        Assert.Equal(900m, inv.GrandTotal);
    }

    [Fact]
    public void Invoice_Cancel_ShouldThrowOnPaid()
    {
        var inv = Invoice.Create("INV-004", _salesOrderId, 500m, 0);
        inv.Cancel();
        Assert.Equal("CANCELLED", inv.Status);
    }

    [Fact]
    public void Invoice_Cancel_PaidOrPartiallyPaid_ShouldThrow()
    {
        var inv = Invoice.Create("INV-005", _salesOrderId, 500m, 0);
        inv.Issue();
        var payment = CustomerPayment.Create(_customerId, _paymentProviderId, 200m, "CASH");
        payment.MarkAsCompleted();
        inv.CustomerPayments.Add(payment);
        inv.UpdatePaymentStatus();
        Assert.Equal("PARTIALLY_PAID", inv.Status);
        Assert.Throws<InvalidOperationException>(() => inv.Cancel());
    }

    [Fact]
    public void Invoice_Issue_FromWrongStatus_ShouldThrow()
    {
        var inv = Invoice.Create("INV-006", _salesOrderId, 500m, 0);
        inv.Issue();
        Assert.Throws<InvalidOperationException>(() => inv.Issue());
    }

    [Fact]
    public void Invoice_UpdatePaymentStatus_WithCompletedPayment_ShouldSetPaid()
    {
        var inv = Invoice.Create("INV-007", _salesOrderId, 1000m, 0);
        inv.Issue();

        var payment = CustomerPayment.Create(_customerId, _paymentProviderId, 1000m, "CASH");
        payment.MarkAsCompleted();
        inv.CustomerPayments.Add(payment);

        inv.UpdatePaymentStatus();
        Assert.Equal("PAID", inv.Status);
    }

    [Fact]
    public void Invoice_UpdatePaymentStatus_WithPartialPayment_ShouldSetPartiallyPaid()
    {
        var inv = Invoice.Create("INV-008", _salesOrderId, 1000m, 0);
        inv.Issue();

        var payment = CustomerPayment.Create(_customerId, _paymentProviderId, 400m, "CASH");
        payment.MarkAsCompleted();
        inv.CustomerPayments.Add(payment);

        inv.UpdatePaymentStatus();
        Assert.Equal("PARTIALLY_PAID", inv.Status);
    }

    [Fact]
    public void Invoice_IsOverdue_ShouldReturnTrueWhenPastDue()
    {
        var inv = Invoice.Create("INV-009", _salesOrderId, 500m, 0,
            dueDate: DateTime.UtcNow.AddDays(-5));
        inv.Issue();
        Assert.True(inv.IsOverdue);
    }

    [Fact]
    public void Invoice_CreditBalance_ShouldExceedWhenOverpaid()
    {
        var inv = Invoice.Create("INV-010", _salesOrderId, 500m, 0);
        inv.Issue();

        var payment = CustomerPayment.Create(_customerId, _paymentProviderId, 600m, "CASH");
        payment.MarkAsCompleted();
        inv.CustomerPayments.Add(payment);

        Assert.Equal(600m, inv.AmountPaid);
        Assert.Equal(100m, inv.CreditBalance);
        Assert.True(inv.HasCredit);
    }

    [Fact]
    public void Invoice_OutstandingAmount_ShouldReturnRemaining()
    {
        var inv = Invoice.Create("INV-011", _salesOrderId, 1000m, 0);
        inv.Issue();

        var payment = CustomerPayment.Create(_customerId, _paymentProviderId, 300m, "CASH");
        payment.MarkAsCompleted();
        inv.CustomerPayments.Add(payment);

        Assert.Equal(700m, inv.OutstandingAmount);
    }
}
