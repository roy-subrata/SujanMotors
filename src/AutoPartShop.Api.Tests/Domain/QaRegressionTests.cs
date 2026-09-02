using AutoPartShop.Domain.Entities;
using AutoPartShop.Domain.Enums;

namespace AutoPartShop.Api.Tests.Domain;

/// <summary>
/// Guards the money-path defects found in the 2026-08-17 QA sweep. These are the behaviours whose
/// regression is expensive and silent — wrong stock, wrong refund, a claim that can be reversed
/// after approval — so they are pinned here rather than left to the next manual sweep to notice.
///
/// Pure domain: no database, no fixtures.
/// </summary>
public class StockLevelUnitDivergenceTests
{
    private static StockLevel LevelWith(int display, int baseUnits)
    {
        var level = StockLevel.Create(Guid.NewGuid(), Guid.NewGuid());
        level.AddStock(display, baseUnits);
        return level;
    }

    /// <summary>
    /// F1: RemoveStock used to check availability against the display quantity while sellability is
    /// judged on base units, so a multi-unit part could be sold down to display 0 while base units
    /// remained — phantom sellable stock with no way to clear it.
    /// </summary>
    [Fact]
    public void RemoveStock_WithBaseUnitQuantity_ChecksAvailabilityInBaseUnits()
    {
        // 3 display units of a x12 pack = 36 base units.
        var level = LevelWith(display: 3, baseUnits: 36);

        level.RemoveStock(quantity: 1, quantityInBaseUnit: 12);

        Assert.Equal(2, level.QuantityOnHand);
        Assert.Equal(24, level.QuantityOnHandInBaseUnit);
    }

    [Fact]
    public void RemoveStock_RejectsRemovalExceedingBaseUnitStock()
    {
        var level = LevelWith(display: 1, baseUnits: 12);

        var ex = Assert.Throws<InvalidOperationException>(
            () => level.RemoveStock(quantity: 1, quantityInBaseUnit: 24));

        Assert.Contains("Insufficient stock", ex.Message);
        Assert.Equal(12, level.QuantityOnHandInBaseUnit);
    }

    /// <summary>
    /// Without a base-unit quantity the caller has not supplied a conversion factor, so the base
    /// column must be left alone rather than decremented by the display amount.
    /// </summary>
    [Fact]
    public void RemoveStock_WithoutBaseUnitQuantity_LeavesBaseUnitsUntouched()
    {
        var level = LevelWith(display: 5, baseUnits: 60);

        level.RemoveStock(quantity: 2);

        Assert.Equal(3, level.QuantityOnHand);
        Assert.Equal(60, level.QuantityOnHandInBaseUnit);
    }

    /// <summary>
    /// F1 recovery: levels that drifted before the fix had no way back short of a direct DB edit.
    /// </summary>
    [Fact]
    public void ReconcileDisplayQuantities_RederivesDisplayFromBaseUnits()
    {
        var level = LevelWith(display: 5, baseUnits: 60);
        level.RemoveStock(quantity: 5);  // display now 0, base still 60 — the divergence

        Assert.Equal(0, level.QuantityOnHand);

        level.ReconcileDisplayQuantities(conversionFactor: 12m);

        Assert.Equal(5, level.QuantityOnHand);
        Assert.Equal(60, level.QuantityOnHandInBaseUnit);
    }

    [Fact]
    public void ReconcileDisplayQuantities_RejectsNonPositiveFactor()
    {
        var level = LevelWith(display: 1, baseUnits: 12);

        Assert.Throws<ArgumentException>(() => level.ReconcileDisplayQuantities(0m));
    }
}

public class InvoiceReturnCreditTests
{
    /// <summary>
    /// F16: a cash refund lowers AmountPaid, so without crediting the invoice a fully-settled
    /// customer read as owing the returned value.
    /// </summary>
    [Fact]
    public void ApplyReturnCredit_ReducesGrandTotalByTheRefundedValue()
    {
        var invoice = Invoice.Create("INV001", Guid.NewGuid(), subTotal: 200m, taxAmount: 0m);
        invoice.SetDiscount(20m);

        Assert.Equal(180m, invoice.GrandTotal);

        // Return one of two units on a discounted sale: the customer effectively paid 90 for it.
        invoice.ApplyReturnCredit(90m);

        Assert.Equal(90m, invoice.GrandTotal);
        Assert.Equal(90m, invoice.ReturnedAmount);
    }

    [Fact]
    public void ApplyReturnCredit_CannotExceedTheInvoiceTotal()
    {
        var invoice = Invoice.Create("INV002", Guid.NewGuid(), subTotal: 100m, taxAmount: 0m);

        Assert.Throws<InvalidOperationException>(() => invoice.ApplyReturnCredit(150m));
        Assert.Equal(0m, invoice.ReturnedAmount);
    }

    [Fact]
    public void ApplyReturnCredit_AccumulatesAcrossPartialReturns()
    {
        var invoice = Invoice.Create("INV003", Guid.NewGuid(), subTotal: 300m, taxAmount: 0m);

        invoice.ApplyReturnCredit(100m);
        invoice.ApplyReturnCredit(50m);

        Assert.Equal(150m, invoice.ReturnedAmount);
        Assert.Equal(150m, invoice.GrandTotal);
    }
}

public class WarrantyClaimTransitionTests
{
    private static WarrantyClaim NewClaim() => WarrantyClaim.Create(
        "WC-001", Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow, "Faulty unit", "REPAIR");

    /// <summary>
    /// F44: an APPROVED claim could be rejected, leaving the record REJECTED while approvedBy and
    /// approvedDate stayed on it. Reject is now restricted to the states that precede a decision.
    /// </summary>
    [Fact]
    public void Reject_IsRefusedOnceTheClaimIsApproved()
    {
        var claim = NewClaim();
        claim.SubmitForReview();
        claim.Approve("manager");

        var ex = Assert.Throws<InvalidOperationException>(() => claim.Reject("changed mind", "manager"));

        Assert.Contains("Cannot reject", ex.Message);
        Assert.Equal(WarrantyClaimStatus.APPROVED, claim.Status);
        Assert.Equal("manager", claim.ApprovedBy);
    }

    [Theory]
    [InlineData(false)]  // PENDING
    [InlineData(true)]   // UNDER_REVIEW
    public void Reject_IsAllowedBeforeADecisionHasBeenMade(bool submitForReview)
    {
        var claim = NewClaim();
        if (submitForReview) claim.SubmitForReview();

        claim.Reject("Outside warranty terms", "manager");

        Assert.Equal(WarrantyClaimStatus.REJECTED, claim.Status);
    }

    [Fact]
    public void Reject_RequiresAReason()
    {
        var claim = NewClaim();

        Assert.Throws<ArgumentException>(() => claim.Reject("  ", "manager"));
    }
}

public class SalaryAdvanceApprovalTests
{
    private static AutoPartShop.Domain.Entities.HR.SalaryAdvance NewAdvance() =>
        AutoPartShop.Domain.Entities.HR.SalaryAdvance.Create(
            Guid.NewGuid(), DateTime.UtcNow.Date, 5000m);

    /// <summary>
    /// F51: an advance used to pay out on creation. It now starts as a request, so no cash-book
    /// expense is posted until someone approves it.
    /// </summary>
    [Fact]
    public void Create_YieldsARequestRatherThanAPayout()
    {
        var advance = NewAdvance();

        Assert.Equal(AutoPartShop.Domain.Enums.HR.SalaryAdvanceStatus.REQUESTED, advance.Status);
        Assert.Null(advance.ApprovedAt);
        Assert.Null(advance.ExpenseId);
    }

    [Fact]
    public void Approve_MovesTheAdvanceToOutstandingAndRecordsTheApprover()
    {
        var advance = NewAdvance();

        advance.Approve("manager");

        Assert.Equal(AutoPartShop.Domain.Enums.HR.SalaryAdvanceStatus.OUTSTANDING, advance.Status);
        Assert.Equal("manager", advance.ApprovedBy);
        Assert.NotNull(advance.ApprovedAt);
    }

    [Fact]
    public void Approve_IsRefusedOnAnAdvanceThatWasAlreadyDecided()
    {
        var advance = NewAdvance();
        advance.Approve("manager");

        Assert.Throws<InvalidOperationException>(() => advance.Approve("manager"));
    }

    [Fact]
    public void Reject_RequiresAReasonAndBlocksLaterApproval()
    {
        var advance = NewAdvance();

        Assert.Throws<ArgumentException>(() => advance.Reject("manager", ""));

        advance.Reject("manager", "Insufficient tenure");

        Assert.Equal(AutoPartShop.Domain.Enums.HR.SalaryAdvanceStatus.REJECTED, advance.Status);
        Assert.Equal("Insufficient tenure", advance.RejectionReason);
        Assert.Throws<InvalidOperationException>(() => advance.Approve("manager"));
    }

    /// <summary>Recovery still only applies to an approved advance.</summary>
    [Fact]
    public void Recover_IsRefusedWhileTheAdvanceIsOnlyRequested()
    {
        var advance = NewAdvance();

        Assert.Throws<InvalidOperationException>(() => advance.Recover(1000m, Guid.NewGuid()));
    }
}

/// <summary>
/// Pins the supplier-payment reversal path (fix #11): a REGULAR payment sent back to us must undo
/// the PO's paid amount exactly, and an ADVANCE consumed by that payment must have its balance
/// restored — never beyond what was consumed, and never on a REGULAR payment.
/// </summary>
public class SupplierPaymentReversalTests
{
    private static SupplierPayment NewAdvance() =>
        SupplierPayment.Create(Guid.NewGuid(), Guid.NewGuid(), 5000m, "CASH");

    [Fact]
    public void ReversePayment_DropsAPaidOrderBackToPartial()
    {
        var po = PurchaseOrder.Create("PO-REV-1", Guid.NewGuid(), null, DateTime.UtcNow.AddDays(7));
        po.SyncLineItems([new LineItemData(null, Guid.NewGuid(), null, 10, 100m, null, 0)]);
        po.CalculateTotal();

        po.RecordPayment(1000m);
        Assert.Equal(PurchaseOrderPaymentStatus.PAID, po.PaymentStatus);
        Assert.Equal(1000m, po.PaidAmount);

        po.ReversePayment(400m);

        Assert.Equal(600m, po.PaidAmount);
        Assert.Equal(PurchaseOrderPaymentStatus.PARTIAL, po.PaymentStatus);
    }

    [Fact]
    public void ReversePayment_DropsDownToPendingWhenNothingRemainsPaid()
    {
        var po = PurchaseOrder.Create("PO-REV-2", Guid.NewGuid(), null, DateTime.UtcNow.AddDays(7));
        po.SyncLineItems([new LineItemData(null, Guid.NewGuid(), null, 10, 100m, null, 0)]);
        po.CalculateTotal();

        po.RecordPayment(300m);
        po.ReversePayment(300m);

        Assert.Equal(0m, po.PaidAmount);
        Assert.Equal(PurchaseOrderPaymentStatus.PENDING, po.PaymentStatus);
    }

    [Fact]
    public void ReversePayment_IsRefusedBeyondWhatWasPaid()
    {
        var po = PurchaseOrder.Create("PO-REV-3", Guid.NewGuid(), null, DateTime.UtcNow.AddDays(7));
        po.SyncLineItems([new LineItemData(null, Guid.NewGuid(), null, 10, 100m, null, 0)]);
        po.CalculateTotal();
        po.RecordPayment(100m);

        var ex = Assert.Throws<InvalidOperationException>(() => po.ReversePayment(200m));

        // The reversal guard surfaces how much has actually been paid. Don't assert on the
        // currency-formatted amount — its symbol and separators vary by CI culture (¤ vs $, commas
        // vs periods). Assert the encapsulated facts instead: the numeric amount and that only the
        // lesser paid value was refused.
        Assert.Contains("200", ex.Message);
        Assert.Contains("100", ex.Message);
        Assert.Equal(100m, po.PaidAmount);
    }

    [Fact]
    public void ReversePayment_IsRefusedForNonPositiveAmounts()
    {
        var po = PurchaseOrder.Create("PO-REV-4", Guid.NewGuid(), null, DateTime.UtcNow.AddDays(7));
        po.SyncLineItems([new LineItemData(null, Guid.NewGuid(), null, 10, 100m, null, 0)]);
        po.CalculateTotal();

        Assert.Throws<ArgumentException>(() => po.ReversePayment(0m));
    }

    [Fact]
    public void RestoreRemainingAmount_PutsConsumedBalanceBackOnTheAdvance()
    {
        var advance = NewAdvance();
        advance.MarkAsAdvance();
        advance.ReduceRemainingAmount(2000m);

        advance.RestoreRemainingAmount(1500m);

        Assert.Equal(4500m, advance.RemainingAmount);
    }

    [Fact]
    public void RestoreRemainingAmount_IsRefusedBeyondWhatWasConsumed()
    {
        var advance = NewAdvance();
        advance.MarkAsAdvance();
        advance.ReduceRemainingAmount(2000m);

        var ex = Assert.Throws<InvalidOperationException>(() => advance.RestoreRemainingAmount(5001m));

        Assert.Contains("consumed", ex.Message);
        Assert.Equal(3000m, advance.RemainingAmount);
    }

    [Fact]
    public void RestoreRemainingAmount_IsRefusedOnRegularPayments()
    {
        var regular = SupplierPayment.Create(Guid.NewGuid(), Guid.NewGuid(), 500m, "CASH");
        regular.MarkAsRegular();

        Assert.Throws<InvalidOperationException>(() => regular.RestoreRemainingAmount(100m));
    }

    [Fact]
    public void MarkAsReturned_IsOnlyReachableFromCompleted()
    {
        var payment = SupplierPayment.Create(Guid.NewGuid(), Guid.NewGuid(), 500m, "CASH");

        Assert.Throws<InvalidOperationException>(() => payment.MarkAsReturned());

        payment.MarkAsProcessed("cashier");
        payment.MarkAsReturned();

        Assert.Equal(SupplierPaymentStatus.RETURNED, payment.Status);
    }
}

/// <summary>
/// Pins the purchase-return write-back (fix #10): returning goods to the supplier must reduce the
/// PO line's received count so the units can be re-received, and the warehouse that manual returns
/// resolve to can only be fixed on a DRAFT PO.
/// </summary>
public class PurchaseReturnWritebackTests
{
    private static (PurchaseOrder po, PurchaseOrderLine line) PoWithReceivedLine(int quantity, int quantityInBaseUnit, int received, int receivedInBase)
    {
        var po = PurchaseOrder.Create($"PO-RTN-{Guid.NewGuid():N}", Guid.NewGuid(), null, DateTime.UtcNow.AddDays(7));
        po.SyncLineItems([new LineItemData(null, Guid.NewGuid(), null, quantity, 100m, null, quantityInBaseUnit)]);
        var line = po.LineItems.First();
        line.UpdateReceivedQuantity(received, receivedInBase);
        return (po, line);
    }

    [Fact]
    public void ReduceReceivedQuantityForReturn_FreesUpTheReceivedAmount()
    {
        var (po, line) = PoWithReceivedLine(quantity: 10, quantityInBaseUnit: 120, received: 10, receivedInBase: 120);

        po.ReduceReceivedQuantityForReturn(line.Id, 4, 48);

        Assert.Equal(6, line.ReceivedQuantity);
        Assert.Equal(72, line.ReceivedQuantityInBaseUnit);
    }

    [Fact]
    public void ReduceReceivedQuantityForReturn_ClampsAtZero()
    {
        var (po, line) = PoWithReceivedLine(quantity: 10, quantityInBaseUnit: 120, received: 5, receivedInBase: 60);

        po.ReduceReceivedQuantityForReturn(line.Id, 99, 999);

        Assert.Equal(0, line.ReceivedQuantity);
        Assert.Equal(0, line.ReceivedQuantityInBaseUnit);
    }

    [Fact]
    public void ReduceReceivedQuantityForReturn_ThrowsForAStaleLineId()
    {
        var (po, _) = PoWithReceivedLine(quantity: 10, quantityInBaseUnit: 120, received: 3, receivedInBase: 36);

        Assert.Throws<InvalidOperationException>(() => po.ReduceReceivedQuantityForReturn(Guid.NewGuid(), 1, 12));
    }

    [Fact]
    public void UpdateWarehouse_IsRefusedOnceThePoLeavesDraft()
    {
        var po = PurchaseOrder.Create("PO-WH-1", Guid.NewGuid(), null, DateTime.UtcNow.AddDays(7));
        po.SyncLineItems([new LineItemData(null, Guid.NewGuid(), null, 1, 100m, null, 0)]);
        po.Submit();
        po.Confirm("manager");

        var ex = Assert.Throws<InvalidOperationException>(() => po.UpdateWarehouse(Guid.NewGuid()));

        Assert.Contains("draft", ex.Message);
    }
}
