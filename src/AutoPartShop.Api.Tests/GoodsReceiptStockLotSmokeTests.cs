using Xunit;
using AutoPartShop.Domain.Entities;

namespace AutoPartShop.Api.Tests;

public class GoodsReceiptStockLotSmokeTests
{
    private readonly Guid _purchaseOrderId = Guid.NewGuid();
    private readonly Guid _purchaseOrderLineId = Guid.NewGuid();
    private readonly Guid _partId = Guid.NewGuid();
    private readonly Guid _warehouseId = Guid.NewGuid();
    private readonly Guid _supplierId = Guid.NewGuid();
    private readonly Guid _goodsReceiptLineId = Guid.NewGuid();
    private readonly Guid _unitId = Guid.NewGuid();

    // ========== GOODS RECEIPT ==========

    [Fact]
    public void GoodsReceipt_Create_ShouldInitializeCorrectly()
    {
        var gr = GoodsReceipt.Create("GRN-001", _purchaseOrderId, _warehouseId);
        Assert.Equal("GRN-001", gr.GRNNumber);
        Assert.Equal("PENDING", gr.Status);
        Assert.Equal(0, gr.TotalItemsReceived);
    }

    [Fact]
    public void GoodsReceipt_Create_WithInvalidInputs_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() =>
            GoodsReceipt.Create("", _purchaseOrderId, _warehouseId));
        Assert.Throws<ArgumentException>(() =>
            GoodsReceipt.Create("GRN-X", Guid.Empty, _warehouseId));
        Assert.Throws<ArgumentException>(() =>
            GoodsReceipt.Create("GRN-X", _purchaseOrderId, Guid.Empty));
    }

    [Fact]
    public void GoodsReceipt_FullLifecycle_ShouldTransitionCorrectly()
    {
        var gr = GoodsReceipt.Create("GRN-002", _purchaseOrderId, _warehouseId);

        gr.Verify("inspector1");
        Assert.Equal("VERIFIED", gr.Status);
        Assert.Equal("inspector1", gr.VerifiedBy);
        Assert.NotNull(gr.VerificationDate);

        gr.Accept();
        Assert.Equal("ACCEPTED", gr.Status);
    }

    [Fact]
    public void GoodsReceipt_Verify_FromWrongStatus_ShouldThrow()
    {
        var gr = GoodsReceipt.Create("GRN-003", _purchaseOrderId, _warehouseId);
        gr.Verify("user");
        gr.Accept();
        Assert.Throws<InvalidOperationException>(() => gr.Verify("user2"));
    }

    [Fact]
    public void GoodsReceipt_Accept_FromPending_ShouldThrow()
    {
        var gr = GoodsReceipt.Create("GRN-004", _purchaseOrderId, _warehouseId);
        Assert.Throws<InvalidOperationException>(() => gr.Accept());
    }

    [Fact]
    public void GoodsReceipt_Reject_ShouldWorkFromPendingOrVerified()
    {
        var gr = GoodsReceipt.Create("GRN-005", _purchaseOrderId, _warehouseId);
        gr.Reject("Damaged packaging");
        Assert.Equal("REJECTED", gr.Status);
    }

    [Fact]
    public void GoodsReceipt_RejectAccepted_ShouldThrow()
    {
        var gr = GoodsReceipt.Create("GRN-006", _purchaseOrderId, _warehouseId);
        gr.Verify("user");
        gr.Accept();
        Assert.Throws<InvalidOperationException>(() => gr.Reject());
    }

    [Fact]
    public void GoodsReceipt_RejectAlreadyRejected_ShouldThrow()
    {
        var gr = GoodsReceipt.Create("GRN-007", _purchaseOrderId, _warehouseId);
        gr.Reject();
        Assert.Throws<InvalidOperationException>(() => gr.Reject());
    }

    [Fact]
    public void GoodsReceipt_UpdateCounts_ShouldComputeCorrectly()
    {
        var gr = GoodsReceipt.Create("GRN-008", _purchaseOrderId, _warehouseId);

        var line1 = GoodsReceiptLine.Create(
            gr.Id, _purchaseOrderLineId, _partId, 10, 10, unitCost: 100m, damagedQuantity: 2);
        var line2 = GoodsReceiptLine.Create(
            gr.Id, Guid.NewGuid(), _partId, 5, 5, unitCost: 50m);

        gr.LineItems.Add(line1);
        gr.LineItems.Add(line2);
        gr.UpdateCounts();

        Assert.Equal(15, gr.TotalItemsReceived);
        Assert.Equal(1, gr.DiscrepancyCount);
    }

    [Fact]
    public void GoodsReceipt_SetInvoiceInformation_ShouldUpdate()
    {
        var gr = GoodsReceipt.Create("GRN-009", _purchaseOrderId, _warehouseId);
        gr.SetInvoiceInformation("INV-SUP-001", DateTime.UtcNow);
        Assert.Equal("INV-SUP-001", gr.SupplierInvoiceNumber);
        Assert.False(gr.InvoiceNotProvided);

        gr.SetInvoiceInformation(string.Empty, null, true);
        Assert.True(gr.InvoiceNotProvided);
        Assert.Equal(string.Empty, gr.SupplierInvoiceNumber);
    }

    [Fact]
    public void GoodsReceipt_SetDeliveryInformation_ShouldUpdate()
    {
        var gr = GoodsReceipt.Create("GRN-010", _purchaseOrderId, _warehouseId);
        gr.SetDeliveryInformation(DateTime.UtcNow, "WB-123", "FedEx", "John", "Handle with care");
        Assert.Equal("WB-123", gr.DeliveryReference);
        Assert.Equal("FedEx", gr.CarrierName);
    }

    // ========== GOODS RECEIPT LINE ==========

    [Fact]
    public void GoodsReceiptLine_Create_ShouldInitializeCorrectly()
    {
        var line = GoodsReceiptLine.Create(
            Guid.NewGuid(), _purchaseOrderLineId, _partId, 10, 8, unitCost: 100m,
            damagedQuantity: 1, wrongQuantity: 1);

        Assert.Equal(8, line.ReceivedQuantity);
        Assert.Equal(10, line.OrderedQuantity);
        Assert.Equal(1, line.DamagedQuantity);
        Assert.Equal(1, line.WrongQuantity);
        Assert.Equal(2, line.RejectedQuantity);
        Assert.Equal(6, line.AcceptedQuantity);
        Assert.True(line.HasDiscrepancy);
    }

    [Fact]
    public void GoodsReceiptLine_Create_WithInvalidInputs_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() =>
            GoodsReceiptLine.Create(Guid.Empty, _purchaseOrderLineId, _partId, 10, 10));
        Assert.Throws<ArgumentException>(() =>
            GoodsReceiptLine.Create(Guid.NewGuid(), Guid.Empty, _partId, 10, 10));
        Assert.Throws<ArgumentException>(() =>
            GoodsReceiptLine.Create(Guid.NewGuid(), _purchaseOrderLineId, Guid.Empty, 10, 10));
        Assert.Throws<ArgumentException>(() =>
            GoodsReceiptLine.Create(Guid.NewGuid(), _purchaseOrderLineId, _partId, 0, 10));
        Assert.Throws<ArgumentException>(() =>
            GoodsReceiptLine.Create(Guid.NewGuid(), _purchaseOrderLineId, _partId, 10, -1));
        Assert.Throws<InvalidOperationException>(() =>
            GoodsReceiptLine.Create(Guid.NewGuid(), _purchaseOrderLineId, _partId, 10, 10,
                damagedQuantity: 8, wrongQuantity: 5));
    }

    [Fact]
    public void GoodsReceiptLine_SetDiscrepancy_ShouldUpdateCorrectly()
    {
        var line = GoodsReceiptLine.Create(
            Guid.NewGuid(), _purchaseOrderLineId, _partId, 10, 10, unitCost: 100m);

        line.SetDiscrepancy(2, 1, reason: "Damaged in transit");
        Assert.Equal(2, line.DamagedQuantity);
        Assert.Equal(1, line.WrongQuantity);
        Assert.Equal(3, line.RejectedQuantity);
        Assert.Equal(7, line.AcceptedQuantity);
        Assert.Equal("Damaged in transit", line.RejectionReason);
    }

    [Fact]
    public void GoodsReceiptLine_SetDiscrepancy_ExceedingReceived_ShouldThrow()
    {
        var line = GoodsReceiptLine.Create(
            Guid.NewGuid(), _purchaseOrderLineId, _partId, 10, 10, unitCost: 100m);
        Assert.Throws<InvalidOperationException>(() => line.SetDiscrepancy(6, 6));
    }

    [Fact]
    public void GoodsReceiptLine_ComputedCosts_ShouldBeCorrect()
    {
        var line = GoodsReceiptLine.Create(
            Guid.NewGuid(), _purchaseOrderLineId, _partId, 10, 8, unitCost: 100m,
            currency: "BDT", damagedQuantity: 2);

        Assert.Equal(800m, line.TotalCost);
        Assert.Equal(600m, line.AcceptedTotalCost);
    }

    [Fact]
    public void GoodsReceiptLine_ConditionValidation_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() =>
            GoodsReceiptLine.Create(Guid.NewGuid(), _purchaseOrderLineId, _partId,
                10, 10, condition: "INVALID"));
    }

    // ========== STOCK LOT ==========

    [Fact]
    public void StockLot_Create_ShouldInitializeCorrectly()
    {
        var lot = StockLot.Create("LOT-001", _partId, _warehouseId, _supplierId,
            _goodsReceiptLineId, 100, 50m, DateTime.UtcNow);

        Assert.Equal("LOT-001", lot.LotNumber);
        Assert.Equal(100, lot.QuantityReceived);
        Assert.Equal(100, lot.QuantityAvailable);
        Assert.Equal(50m, lot.CostPrice);
        Assert.Equal("AVAILABLE", lot.Status);
        Assert.True(lot.IsActive);
    }

    [Fact]
    public void StockLot_Create_WithInvalidInputs_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() =>
            StockLot.Create("", _partId, _warehouseId, _supplierId, _goodsReceiptLineId, 100, 50m, DateTime.UtcNow));
        Assert.Throws<ArgumentException>(() =>
            StockLot.Create("LOT-X", Guid.Empty, _warehouseId, _supplierId, _goodsReceiptLineId, 100, 50m, DateTime.UtcNow));
        Assert.Throws<ArgumentException>(() =>
            StockLot.Create("LOT-X", _partId, Guid.Empty, _supplierId, _goodsReceiptLineId, 100, 50m, DateTime.UtcNow));
        Assert.Throws<ArgumentException>(() =>
            StockLot.Create("LOT-X", _partId, _warehouseId, Guid.Empty, _goodsReceiptLineId, 100, 50m, DateTime.UtcNow));
        Assert.Throws<ArgumentException>(() =>
            StockLot.Create("LOT-X", _partId, _warehouseId, _supplierId, Guid.Empty, 100, 50m, DateTime.UtcNow));
        Assert.Throws<ArgumentException>(() =>
            StockLot.Create("LOT-X", _partId, _warehouseId, _supplierId, _goodsReceiptLineId, 0, 50m, DateTime.UtcNow));
        Assert.Throws<ArgumentException>(() =>
            StockLot.Create("LOT-X", _partId, _warehouseId, _supplierId, _goodsReceiptLineId, 100, -1m, DateTime.UtcNow));
    }

    [Fact]
    public void StockLot_ExpiryBeforeReceiving_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() =>
            StockLot.Create("LOT-X", _partId, _warehouseId, _supplierId, _goodsReceiptLineId,
                100, 50m, DateTime.UtcNow, expiryDate: DateTime.UtcNow.AddDays(-1)));
    }

    [Fact]
    public void StockLot_RemoveStock_ShouldReduceAvailable()
    {
        var lot = StockLot.Create("LOT-002", _partId, _warehouseId, _supplierId,
            _goodsReceiptLineId, 100, 50m, DateTime.UtcNow);

        lot.RemoveStock(30);
        Assert.Equal(70, lot.QuantityAvailable);

        lot.RemoveStock(70);
        Assert.Equal(0, lot.QuantityAvailable);
        Assert.True(lot.IsEmpty);
    }

    [Fact]
    public void StockLot_RemoveStock_ExceedingAvailable_ShouldThrow()
    {
        var lot = StockLot.Create("LOT-003", _partId, _warehouseId, _supplierId,
            _goodsReceiptLineId, 50, 100m, DateTime.UtcNow);
        Assert.Throws<InvalidOperationException>(() => lot.RemoveStock(60));
    }

    [Fact]
    public void StockLot_AddStock_ShouldIncreaseButNotExceedReceived()
    {
        var lot = StockLot.Create("LOT-004", _partId, _warehouseId, _supplierId,
            _goodsReceiptLineId, 100, 50m, DateTime.UtcNow);
        lot.RemoveStock(40);
        Assert.Equal(60, lot.QuantityAvailable);

        lot.AddStock(20);
        Assert.Equal(80, lot.QuantityAvailable);

        lot.AddStock(50);
        Assert.Equal(100, lot.QuantityAvailable);
    }

    [Fact]
    public void StockLot_IncreaseCapacity_ShouldExpandLimit()
    {
        var lot = StockLot.Create("LOT-005", _partId, _warehouseId, _supplierId,
            _goodsReceiptLineId, 100, 50m, DateTime.UtcNow);
        lot.RemoveStock(100);
        Assert.True(lot.IsEmpty);

        lot.IncreaseCapacity(50);
        lot.AddStock(30);
        Assert.Equal(30, lot.QuantityAvailable);
    }

    [Fact]
    public void StockLot_GetTotalCost_ShouldCompute()
    {
        var lot = StockLot.Create("LOT-006", _partId, _warehouseId, _supplierId,
            _goodsReceiptLineId, 10, 150m, DateTime.UtcNow);
        Assert.Equal(1500m, lot.GetTotalCost());
        Assert.Equal(1500m, lot.GetAvailableCost());

        lot.RemoveStock(3);
        Assert.Equal(1050m, lot.GetAvailableCost());
    }

    [Fact]
    public void StockLot_IsExpired_ShouldCheckDate()
    {
        var lot = StockLot.Create("LOT-007", _partId, _warehouseId, _supplierId,
            _goodsReceiptLineId, 100, 50m, DateTime.UtcNow.AddDays(-7),
            expiryDate: DateTime.UtcNow.AddDays(-1));

        Assert.True(lot.IsExpired);
    }

    [Fact]
    public void StockLot_UpdateDetails_ShouldValidateExpiry()
    {
        var lot = StockLot.Create("LOT-008", _partId, _warehouseId, _supplierId,
            _goodsReceiptLineId, 100, 50m, DateTime.UtcNow);

        Assert.Throws<ArgumentException>(() =>
            lot.UpdateDetails("MFG-123", DateTime.UtcNow.AddDays(-1), ""));
    }

    [Fact]
    public void StockLot_UpdateDetails_ShouldWork()
    {
        var lot = StockLot.Create("LOT-009", _partId, _warehouseId, _supplierId,
            _goodsReceiptLineId, 100, 50m, DateTime.UtcNow);
        lot.UpdateDetails("MFG-123", DateTime.UtcNow.AddMonths(6), "Updated notes");
    }

    [Fact]
    public void StockLot_UpdateWarranty_ShouldTrackProperly()
    {
        var lot = StockLot.Create("LOT-010", _partId, _warehouseId, _supplierId,
            _goodsReceiptLineId, 100, 50m, DateTime.UtcNow);

        lot.UpdateWarranty(true, 12, "MANUFACTURER", "Standard warranty terms");
        Assert.True(lot.HasWarranty);
        Assert.Equal(12, lot.WarrantyPeriodMonths);

        lot.UpdateWarranty(false, null, null, null);
        Assert.False(lot.HasWarranty);
        Assert.Null(lot.WarrantyPeriodMonths);
    }

    [Fact]
    public void StockLot_ActivateDeactivate_ShouldToggle()
    {
        var lot = StockLot.Create("LOT-011", _partId, _warehouseId, _supplierId,
            _goodsReceiptLineId, 100, 50m, DateTime.UtcNow);

        lot.Deactivate();
        Assert.False(lot.IsActive);
        lot.Activate();
        Assert.True(lot.IsActive);
    }

    // ========== STOCK LOT MOVEMENT ==========

    [Fact]
    public void StockLotMovement_Create_ShouldInitializeCorrectly()
    {
        var slm = StockLotMovement.Create(
            Guid.NewGuid(), 50, "SALE", Guid.NewGuid(), "SalesOrderLine",
            DateTime.UtcNow, 150m, "Customer purchase");

        Assert.Equal(50, slm.Quantity);
        Assert.Equal("SALE", slm.MovementType);
        Assert.Equal(150m, slm.CostAtMovement);
        Assert.Equal(7500m, slm.GetMovementCost());
    }

    [Fact]
    public void StockLotMovement_Create_WithInvalidInputs_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() =>
            StockLotMovement.Create(Guid.Empty, 10, "SALE"));
        Assert.Throws<ArgumentException>(() =>
            StockLotMovement.Create(Guid.NewGuid(), 0, "SALE"));
        Assert.Throws<ArgumentException>(() =>
            StockLotMovement.Create(Guid.NewGuid(), 10, ""));
        Assert.Throws<ArgumentException>(() =>
            StockLotMovement.Create(Guid.NewGuid(), 10, "INVALID"));
        Assert.Throws<ArgumentException>(() =>
            StockLotMovement.Create(Guid.NewGuid(), 10, "SALE", costAtMovement: -1m));
    }

    [Fact]
    public void StockLotMovement_AllMovementTypes_ShouldBeValid()
    {
        var types = new[] { "RECEIPT", "SALE", "ADJUSTMENT", "DAMAGE", "RETURN", "TRANSFER" };
        foreach (var type in types)
        {
            var slm = StockLotMovement.Create(Guid.NewGuid(), 10, type);
            Assert.Equal(type, slm.MovementType);
        }
    }

    [Fact]
    public void StockLotMovement_UpdateNotes_ShouldWork()
    {
        var slm = StockLotMovement.Create(Guid.NewGuid(), 10, "ADJUSTMENT");
        slm.UpdateNotes("Stock count adjustment");
        Assert.Equal("Stock count adjustment", slm.Notes);
    }
}
