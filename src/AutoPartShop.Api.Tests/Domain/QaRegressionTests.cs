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
