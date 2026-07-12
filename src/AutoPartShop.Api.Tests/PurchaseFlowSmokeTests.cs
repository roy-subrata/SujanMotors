using AutoPartShop.Domain.Entities;
using Xunit;

namespace AutoPartShop.Api.Tests;

public class PurchaseFlowSmokeTests
{
    private readonly Guid _supplierId = Guid.NewGuid();
    private readonly Guid _warehouseId = Guid.NewGuid();
    private readonly Guid _partId = Guid.NewGuid();

    [Fact]
    public void PurchaseOrder_FullLifecycle_ShouldTransitionThroughAllStates()
    {
        var po = PurchaseOrder.Create("PO-001", _supplierId, _warehouseId,
            DateTime.UtcNow.AddDays(7), "Test PO", "BDT");
        Assert.Equal("DRAFT", po.Status);

        po.SyncLineItems([
            new LineItemData(null, _partId, null, 10, 100m, null, 10, "Test part")
        ]);
        Assert.Single(po.LineItems);
        Assert.Equal(1000m, po.SubTotal);

        po.Submit();
        Assert.Equal("SUBMITTED", po.Status);

        po.Confirm("admin");
        Assert.Equal("CONFIRMED", po.Status);
        Assert.Equal("admin", po.ApprovedBy);
        Assert.NotNull(po.ApprovedDate);
    }

    [Fact]
    public void PurchaseOrder_CalculateTotal_WithTaxAndDiscount_ShouldComputeCorrectly()
    {
        var po = PurchaseOrder.Create("PO-002", _supplierId, _warehouseId,
            DateTime.UtcNow.AddDays(7));
        po.SyncLineItems([
            new LineItemData(null, _partId, null, 10, 100m, null, 10)
        ]);
        Assert.Equal(1000m, po.SubTotal);

        po.SetTaxPercentage(10);
        po.SetDiscountPercentage(5);
        po.CalculateTotal();

        Assert.Equal(1000m, po.SubTotal);
        Assert.Equal(100m, po.TaxAmount);
        Assert.Equal(50m, po.DiscountAmount);
        Assert.Equal(1050m, po.TotalAmount);
    }

    [Fact]
    public void PurchaseOrder_RecordPayment_ShouldUpdatePaymentStatus()
    {
        var po = PurchaseOrder.Create("PO-003", _supplierId, _warehouseId,
            DateTime.UtcNow.AddDays(7));
        po.SyncLineItems([
            new LineItemData(null, _partId, null, 10, 100m, null, 10)
        ]);
        po.SetTaxPercentage(0);
        po.SetDiscountPercentage(0);
        po.CalculateTotal();
        Assert.Equal(1000m, po.TotalAmount);

        po.RecordPayment(600m);
        Assert.Equal("PARTIAL", po.PaymentStatus);
        Assert.Equal(600m, po.PaidAmount);

        po.RecordPayment(400m);
        Assert.Equal("PAID", po.PaymentStatus);
        Assert.Equal(1000m, po.PaidAmount);
    }

    [Fact]
    public void PurchaseOrder_Cancel_ShouldOnlyWorkBeforeDelivery()
    {
        var po = PurchaseOrder.Create("PO-004", _supplierId, _warehouseId,
            DateTime.UtcNow.AddDays(7));
        po.SyncLineItems([
            new LineItemData(null, _partId, null, 10, 100m, null, 10)
        ]);
        po.Cancel();
        Assert.Equal("CANCELLED", po.Status);
    }

    [Fact]
    public void PurchaseOrder_DraftValidation_ShouldRejectInvalidOperations()
    {
        var po = PurchaseOrder.Create("PO-005", _supplierId, _warehouseId,
            DateTime.UtcNow.AddDays(7));

        Assert.Throws<InvalidOperationException>(() => po.Submit());

        po.SyncLineItems([
            new LineItemData(null, _partId, null, 10, 100m, null, 10)
        ]);
        po.Submit();
        Assert.Throws<InvalidOperationException>(() => po.Submit());
    }

    [Fact]
    public void PurchaseReturn_FullLifecycle_ShouldTransitionThroughAllStates()
    {
        var po = PurchaseOrder.Create("PO-RET-001", _supplierId, _warehouseId,
            DateTime.UtcNow.AddDays(7));
        po.SyncLineItems([
            new LineItemData(null, _partId, null, 10, 100m, null, 10)
        ]);
        po.Submit();
        po.Confirm("admin");

        var lineItem = po.LineItems.First();

        var purchaseReturn = PurchaseReturn.Create("PR-001", po.Id, _supplierId,
            "DAMAGED", DateTime.UtcNow, "Damaged in transit");
        Assert.Equal("PENDING", purchaseReturn.Status);

        var returnLine = PurchaseReturnLine.Create(
            purchaseReturn.Id, lineItem.Id, _partId, 2, 100m, "DAMAGED");
        purchaseReturn.LineItems.Add(returnLine);
        purchaseReturn.CalculateRefund();
        Assert.Equal(200m, purchaseReturn.RefundAmount);

        purchaseReturn.Approve("manager");
        Assert.Equal("APPROVED", purchaseReturn.Status);
        Assert.Equal("manager", purchaseReturn.ApprovedBy);

        purchaseReturn.MarkAsReturned();
        Assert.Equal("RETURNED", purchaseReturn.Status);

        purchaseReturn.MarkAsReceived("warehouse_staff");
        Assert.Equal("RECEIVED", purchaseReturn.Status);

        purchaseReturn.IssueCreditNote(200m);
        Assert.Equal("CREDITED", purchaseReturn.Status);
        Assert.True(purchaseReturn.IsSettled);
        Assert.Equal(200m, purchaseReturn.SettledAmount);
    }

    [Fact]
    public void PurchaseReturn_SettleWithCash_ShouldRecordSettlement()
    {
        var po = PurchaseOrder.Create("PO-RET-002", _supplierId, _warehouseId,
            DateTime.UtcNow.AddDays(7));
        po.SyncLineItems([
            new LineItemData(null, _partId, null, 10, 100m, null, 10)
        ]);
        po.Submit();
        po.Confirm("admin");

        var purchaseReturn = PurchaseReturn.Create("PR-002", po.Id, _supplierId,
            "DEFECTIVE", DateTime.UtcNow);
        var line = PurchaseReturnLine.Create(
            purchaseReturn.Id, po.LineItems.First().Id, _partId, 3, 100m, "DEFECTIVE");
        purchaseReturn.LineItems.Add(line);
        purchaseReturn.CalculateRefund();
        purchaseReturn.Approve("manager");
        purchaseReturn.MarkAsReturned();
        purchaseReturn.MarkAsReceived("staff");

        purchaseReturn.SettleReturn(300m, "CASH", "Cash refund given");
        Assert.True(purchaseReturn.IsSettled);
        Assert.Equal("CASH", purchaseReturn.SettlementMethod);
        Assert.Equal(300m, purchaseReturn.SettledAmount);
    }

    [Fact]
    public void PurchaseReturn_InvalidTransitions_ShouldThrow()
    {
        var po = PurchaseOrder.Create("PO-RET-003", _supplierId, _warehouseId,
            DateTime.UtcNow.AddDays(7));
        po.SyncLineItems([
            new LineItemData(null, _partId, null, 10, 100m, null, 10)
        ]);
        po.Submit();
        po.Confirm("admin");

        var purchaseReturn = PurchaseReturn.Create("PR-003", po.Id, _supplierId,
            "WRONG_ITEM", DateTime.UtcNow);

        Assert.Throws<InvalidOperationException>(() => purchaseReturn.MarkAsReturned());
        Assert.Throws<InvalidOperationException>(() => purchaseReturn.MarkAsReceived("x"));
        Assert.Throws<InvalidOperationException>(() => purchaseReturn.IssueCreditNote(100m));
    }

    [Fact]
    public void PurchaseOrderLine_ReceiveIncrements_ShouldUpdateCorrectly()
    {
        var po = PurchaseOrder.Create("PO-RL-001", _supplierId, _warehouseId,
            DateTime.UtcNow.AddDays(7));
        po.SyncLineItems([
            new LineItemData(null, _partId, null, 10, 100m, null, 10)
        ]);
        var line = po.LineItems.First();

        Assert.False(line.IsFullyReceived);
        line.UpdateReceivedQuantity(5);
        Assert.Equal(5, line.ReceivedQuantity);
        Assert.False(line.IsFullyReceived);

        line.UpdateReceivedQuantity(10);
        Assert.True(line.IsFullyReceived);
    }

    [Fact]
    public void PurchaseReturnLine_RejectQuantity_ShouldReduceRefund()
    {
        var purchaseReturn = PurchaseReturn.Create("PR-RL-001", Guid.NewGuid(), _supplierId,
            "DAMAGED", DateTime.UtcNow);

        var line = PurchaseReturnLine.Create(
            purchaseReturn.Id, Guid.NewGuid(), _partId, 10, 50m, "DAMAGED");

        Assert.Equal(500m, line.RefundAmount);

        line.RejectQuantity(2);
        Assert.Equal(2, line.RejectedQuantity);
        Assert.Equal(400m, line.RefundAmount);
    }

    // ========== SUPPLIER ==========

    [Fact]
    public void Supplier_Create_ShouldInitializeCorrectly()
    {
        var supplier = Supplier.Create("ABC Auto Parts", "SUP001", "John Contact",
            "john@abc.com", "555-0100", "123 Main St", "Dhaka", "Dhaka", "Bangladesh", "1000");

        Assert.Equal("ABC Auto Parts", supplier.Name);
        Assert.Equal("SUP001", supplier.Code);
        Assert.Equal("NET30", supplier.PaymentTerms);
        Assert.True(supplier.IsActive);
        Assert.Equal(5, supplier.Rating);
        Assert.Equal(0, supplier.CreditLimit);
    }

    [Fact]
    public void Supplier_Create_WithInvalidInputs_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() =>
            Supplier.Create("", "SUP002", "", "", "", "", "", "", "", ""));
        Assert.Throws<ArgumentException>(() =>
            Supplier.Create("Valid Name", "", "", "", "", "", "", "", "", ""));
    }

    [Fact]
    public void Supplier_Update_ShouldChangeValues()
    {
        var supplier = Supplier.Create("Old Name", "SUP003", "", "", "", "", "", "", "", "");
        supplier.Update("New Name", "New Contact", "new@email.com", "555-0200",
            "456 New St", "New City", "New State", "New Country", "2000", true, "NET60", 50000m);

        Assert.Equal("New Name", supplier.Name);
        Assert.Equal("New Contact", supplier.ContactPerson);
        Assert.Equal("NET60", supplier.PaymentTerms);
        Assert.Equal(50000m, supplier.CreditLimit);
    }

    [Fact]
    public void Supplier_SetRating_ShouldValidate()
    {
        var supplier = Supplier.Create("Test", "SUP004", "", "", "", "", "", "", "", "");

        Assert.Throws<ArgumentException>(() => supplier.SetRating(0));
        Assert.Throws<ArgumentException>(() => supplier.SetRating(6));

        supplier.SetRating(3);
        Assert.Equal(3, supplier.Rating);
    }

    [Fact]
    public void Supplier_ActivateDeactivate_ShouldToggle()
    {
        var supplier = Supplier.Create("Test", "SUP005", "", "", "", "", "", "", "", "");
        Assert.True(supplier.IsActive);

        supplier.Deactivate();
        Assert.False(supplier.IsActive);

        supplier.Activate();
        Assert.True(supplier.IsActive);
    }

    [Fact]
    public void Supplier_AdvanceAmount_ShouldStartAtZero()
    {
        var supplier = Supplier.Create("Test", "SUP006", "", "", "", "", "", "", "", "");
        Assert.Equal(0m, supplier.AdvanceAmount);
    }

    // ========== PURCHASE ORDER ADDITIONAL EDGE CASES ==========

    [Fact]
    public void PurchaseOrder_Create_WithPastDeliveryDate_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() =>
            PurchaseOrder.Create("PO-EDGE-001", _supplierId, _warehouseId,
                DateTime.UtcNow.AddDays(-1)));
    }

    [Fact]
    public void PurchaseOrder_Create_WithEmptyPONumber_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() =>
            PurchaseOrder.Create("", _supplierId, _warehouseId, DateTime.UtcNow.AddDays(7)));
    }

    [Fact]
    public void PurchaseOrder_Create_WithEmptySupplier_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() =>
            PurchaseOrder.Create("PO-EDGE-002", Guid.Empty, _warehouseId, DateTime.UtcNow.AddDays(7)));
    }

    [Fact]
    public void PurchaseOrder_MarkAsDelivered_FromWrongStatus_ShouldThrow()
    {
        var po = PurchaseOrder.Create("PO-EDGE-003", _supplierId, _warehouseId,
            DateTime.UtcNow.AddDays(7));
        Assert.Throws<InvalidOperationException>(() => po.MarkAsDelivered());
    }

    [Fact]
    public void PurchaseOrder_RecordPayment_ExceedingOutstanding_ShouldThrow()
    {
        var po = PurchaseOrder.Create("PO-EDGE-004", _supplierId, _warehouseId,
            DateTime.UtcNow.AddDays(7));
        po.SyncLineItems([new LineItemData(null, _partId, null, 5, 100m, null, 5)]);
        po.SetTaxPercentage(0);
        po.SetDiscountPercentage(0);
        po.CalculateTotal();

        Assert.Throws<ArgumentException>(() => po.RecordPayment(0));
        Assert.Throws<InvalidOperationException>(() => po.RecordPayment(1000m));
    }

    [Fact]
    public void PurchaseOrder_ApplyCredit_ShouldUpdatePaymentStatus()
    {
        var po = PurchaseOrder.Create("PO-EDGE-005", _supplierId, _warehouseId,
            DateTime.UtcNow.AddDays(7));
        po.SyncLineItems([new LineItemData(null, _partId, null, 10, 100m, null, 10)]);
        po.CalculateTotal();

        po.ApplyCredit(400m);
        Assert.Equal("PARTIAL", po.PaymentStatus);
        Assert.Equal(400m, po.CreditAppliedAmount);

        po.ApplyCredit(600m);
        Assert.Equal("PAID", po.PaymentStatus);
        Assert.Equal(1000m, po.CreditAppliedAmount);
    }

    [Fact]
    public void PurchaseOrder_ApplyCredit_ExceedingOutstanding_ShouldThrow()
    {
        var po = PurchaseOrder.Create("PO-EDGE-006", _supplierId, _warehouseId,
            DateTime.UtcNow.AddDays(7));
        po.SyncLineItems([new LineItemData(null, _partId, null, 5, 100m, null, 5)]);
        po.CalculateTotal();

        Assert.Throws<InvalidOperationException>(() => po.ApplyCredit(600m));
    }

    [Fact]
    public void PurchaseOrder_UpdateSupplier_OnlyOnDraft_ShouldWork()
    {
        var po = PurchaseOrder.Create("PO-EDGE-007", _supplierId, _warehouseId,
            DateTime.UtcNow.AddDays(7));
        var newSupplierId = Guid.NewGuid();
        po.UpdateSupplier(newSupplierId);
        Assert.Equal(newSupplierId, po.SupplierId);
    }

    [Fact]
    public void PurchaseOrder_UpdateSupplier_OnNonDraft_ShouldThrow()
    {
        var po = PurchaseOrder.Create("PO-EDGE-008", _supplierId, _warehouseId,
            DateTime.UtcNow.AddDays(7));
        po.SyncLineItems([new LineItemData(null, _partId, null, 5, 100m, null, 5)]);
        po.Submit();
        Assert.Throws<InvalidOperationException>(() => po.UpdateSupplier(Guid.NewGuid()));
    }

    [Fact]
    public void PurchaseOrder_SyncLineItems_OnNonDraft_ShouldThrow()
    {
        var po = PurchaseOrder.Create("PO-EDGE-009", _supplierId, _warehouseId,
            DateTime.UtcNow.AddDays(7));
        po.SyncLineItems([new LineItemData(null, _partId, null, 5, 100m, null, 5)]);
        po.Submit();
        Assert.Throws<InvalidOperationException>(() =>
            po.SyncLineItems([new LineItemData(null, Guid.NewGuid(), null, 10, 50m, null, 10)]));
    }

    [Fact]
    public void PurchaseOrder_UpdateNotes_ShouldWork()
    {
        var po = PurchaseOrder.Create("PO-EDGE-010", _supplierId, _warehouseId,
            DateTime.UtcNow.AddDays(7), "Original note");
        po.UpdateNotes("Updated note");
    }

    [Fact]
    public void PurchaseOrder_CancelPartialOrDelivered_ShouldThrow()
    {
        var po = PurchaseOrder.Create("PO-EDGE-011", _supplierId, _warehouseId,
            DateTime.UtcNow.AddDays(7));
        po.SyncLineItems([new LineItemData(null, _partId, null, 5, 100m, null, 5)]);
        po.Submit();
        po.Confirm("admin");
        po.MarkAsDelivered();
        Assert.Throws<InvalidOperationException>(() => po.Cancel());
    }

    [Fact]
    public void PurchaseOrderLine_Create_WithInvalidInputs_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() =>
            PurchaseOrderLine.Create(Guid.Empty, _partId, 5, 100m, 1));
        Assert.Throws<ArgumentException>(() =>
            PurchaseOrderLine.Create(Guid.NewGuid(), Guid.Empty, 5, 100m, 1));
        Assert.Throws<ArgumentException>(() =>
            PurchaseOrderLine.Create(Guid.NewGuid(), _partId, 0, 100m, 1));
        Assert.Throws<ArgumentException>(() =>
            PurchaseOrderLine.Create(Guid.NewGuid(), _partId, 5, 0m, 1));
        Assert.Throws<ArgumentException>(() =>
            PurchaseOrderLine.Create(Guid.NewGuid(), _partId, 5, 100m, 0));
    }

    [Fact]
    public void PurchaseOrderLine_UpdateReceivedQuantity_ExceedingOrdered_ShouldThrow()
    {
        var line = PurchaseOrderLine.Create(Guid.NewGuid(), _partId, 10, 100m, 1);
        Assert.Throws<InvalidOperationException>(() => line.UpdateReceivedQuantity(15));
    }

    [Fact]
    public void PurchaseReturn_Create_WithInvalidInputs_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() =>
            PurchaseReturn.Create("", Guid.NewGuid(), _supplierId, "DAMAGED"));
        Assert.Throws<ArgumentException>(() =>
            PurchaseReturn.Create("PR-X", Guid.Empty, _supplierId, "DAMAGED"));
        Assert.Throws<ArgumentException>(() =>
            PurchaseReturn.Create("PR-X", Guid.NewGuid(), Guid.Empty, "DAMAGED"));
        Assert.Throws<ArgumentException>(() =>
            PurchaseReturn.Create("PR-X", Guid.NewGuid(), _supplierId, ""));
    }

    [Fact]
    public void PurchaseReturnLine_Create_WithInvalidCondition_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() =>
            PurchaseReturnLine.Create(Guid.NewGuid(), Guid.NewGuid(), _partId, 5, 100m, "INVALID"));
    }
}
