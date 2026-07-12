using AutoPartShop.Domain.Entities;
using Xunit;

namespace AutoPartShop.Api.Tests;

public class SaleFlowSmokeTests
{
    private readonly Guid _customerId = Guid.NewGuid();
    private readonly Guid _warehouseId = Guid.NewGuid();
    private readonly Guid _partId = Guid.NewGuid();

    [Fact]
    public void SalesOrder_FullLifecycle_ShouldTransitionThroughAllStates()
    {
        var so = SalesOrder.Create("SO-001", _customerId, "John Doe",
            "john@test.com", "555-0100", _warehouseId, channel: "POS");
        Assert.Equal("PENDING", so.Status);
        Assert.Equal("POS", so.Channel);

        var line = SalesOrderLine.Create(so.Id, _partId, 5, 200m, 1);
        so.LineItems.Add(line);
        so.CalculateTotal();
        Assert.Equal(1000m, so.SubTotal);
        Assert.Equal(1000m, so.GrandTotal);

        so.Confirm();
        Assert.Equal("CONFIRMED", so.Status);
        Assert.NotNull(so.ConfirmedDate);

        so.MarkAsReadyForDelivery();
        Assert.Equal("READY_FOR_DELIVERY", so.Status);

        so.MarkAsDelivered();
        Assert.Equal("DELIVERED", so.Status);
        Assert.NotNull(so.DeliveryDate);
    }

    [Fact]
    public void SalesOrder_ConfirmWithoutLines_ShouldThrow()
    {
        var so = SalesOrder.Create("SO-002", _customerId, "Jane Doe",
            "jane@test.com", "555-0200", _warehouseId);
        Assert.Throws<InvalidOperationException>(() => so.Confirm());
    }

    [Fact]
    public void SalesOrder_DirectDelivery_ShouldSkipReadyForDelivery()
    {
        var so = SalesOrder.Create("SO-003", _customerId, "Bob",
            "bob@test.com", "555-0300", _warehouseId);
        var line = SalesOrderLine.Create(so.Id, _partId, 3, 150m, 1);
        so.LineItems.Add(line);
        so.CalculateTotal();
        so.Confirm();

        so.MarkAsDelivered();
        Assert.Equal("DELIVERED", so.Status);
    }

    [Fact]
    public void SalesOrder_CalculateTotal_WithTaxAndDiscount_ShouldComputeCorrectly()
    {
        var so = SalesOrder.Create("SO-004", _customerId, "Alice",
            "alice@test.com", "555-0400", _warehouseId);
        so.LineItems.Add(SalesOrderLine.Create(so.Id, _partId, 10, 100m, 1));
        so.CalculateTotal();
        Assert.Equal(1000m, so.SubTotal);
        Assert.Equal(1000m, so.TotalAmount);
        Assert.Equal(0m, so.TaxAmount);

        so.SetDiscountPercentage(10);
        so.CalculateTotal();
        Assert.Equal(100m, so.DiscountAmount);
        Assert.Equal(900m, so.TotalAmount);
        Assert.Equal(900m, so.GrandTotal);
    }

    [Fact]
    public void SalesOrder_RecordPayment_ShouldUpdatePaymentStatus()
    {
        var so = SalesOrder.Create("SO-005", _customerId, "Charlie",
            "charlie@test.com", "555-0500", _warehouseId);
        so.LineItems.Add(SalesOrderLine.Create(so.Id, _partId, 5, 100m, 1));
        so.CalculateTotal();

        so.RecordPayment(300m);
        Assert.Equal("PARTIAL", so.PaymentStatus);
        Assert.Equal(300m, so.PaidAmount);

        so.RecordPayment(200m);
        Assert.Equal("PAID", so.PaymentStatus);
        Assert.Equal(500m, so.PaidAmount);
    }

    [Fact]
    public void SalesOrder_ProcessRefund_ShouldReducePaidAmount()
    {
        var so = SalesOrder.Create("SO-006", _customerId, "Diana",
            "diana@test.com", "555-0600", _warehouseId);
        so.LineItems.Add(SalesOrderLine.Create(so.Id, _partId, 5, 100m, 1));
        so.CalculateTotal();
        so.RecordPayment(500m);

        so.ProcessRefund(200m);
        Assert.Equal(300m, so.PaidAmount);
        Assert.Equal("PARTIAL", so.PaymentStatus);
    }

    [Fact]
    public void SalesReturn_FullLifecycle_ShouldTransitionThroughAllStates()
    {
        var so = SalesOrder.Create("SO-RET-001", _customerId, "Eve",
            "eve@test.com", "555-0700", _warehouseId);
        so.LineItems.Add(SalesOrderLine.Create(so.Id, _partId, 5, 200m, 1));
        so.CalculateTotal();
        so.Confirm();
        so.MarkAsDelivered();

        var soLine = so.LineItems.First();

        var salesReturn = SalesReturn.Create("SR-001", so.Id, null,
            "DEFECTIVE", _warehouseId, DateTime.UtcNow, "Defective part");
        Assert.Equal("PENDING", salesReturn.Status);

        var returnLine = SalesReturnLine.Create(
            salesReturn.Id, soLine.Id, _partId, 2, 200m, "DAMAGED");
        salesReturn.LineItems.Add(returnLine);
        salesReturn.CalculateRefund();
        Assert.Equal(400m, salesReturn.RefundAmount);

        salesReturn.Approve("manager");
        Assert.Equal("APPROVED", salesReturn.Status);

        salesReturn.MarkAsReceived();
        Assert.Equal("RECEIVED", salesReturn.Status);

        salesReturn.Process();
        Assert.Equal("PROCESSED", salesReturn.Status);
    }

    [Fact]
    public void SalesReturn_InvalidTransitions_ShouldThrow()
    {
        var salesReturn = SalesReturn.Create("SR-002", Guid.NewGuid(), null,
            "DAMAGED", _warehouseId, DateTime.UtcNow);

        Assert.Throws<InvalidOperationException>(() => salesReturn.MarkAsReceived());
        Assert.Throws<InvalidOperationException>(() => salesReturn.Process());
    }

    [Fact]
    public void SalesReturn_Reject_ShouldSetStatus()
    {
        var salesReturn = SalesReturn.Create("SR-003", Guid.NewGuid(), null,
            "WRONG_ITEM", _warehouseId, DateTime.UtcNow);
        salesReturn.Reject("Customer changed mind");
        Assert.Equal("REJECTED", salesReturn.Status);
    }

    [Fact]
    public void SalesReturn_SetRefundType_ShouldValidate()
    {
        var salesReturn = SalesReturn.Create("SR-004", Guid.NewGuid(), null,
            "EXCESS_STOCK", _warehouseId, DateTime.UtcNow);

        salesReturn.SetRefundType("STORE_CREDIT");
        Assert.Equal("STORE_CREDIT", salesReturn.RefundType);

        salesReturn.SetRefundType("CASH_REFUND");
        Assert.Equal("CASH_REFUND", salesReturn.RefundType);

        Assert.Throws<ArgumentException>(() => salesReturn.SetRefundType("INVALID"));
    }

    [Fact]
    public void SalesOrderLine_ShippedQuantity_ShouldTrackCorrectly()
    {
        var so = SalesOrder.Create("SO-SL-001", _customerId, "Frank",
            "frank@test.com", "555-0800", _warehouseId);
        var line = SalesOrderLine.Create(so.Id, _partId, 10, 50m, 1);

        Assert.False(line.IsFullyShipped);
        Assert.Equal(10, line.PendingQuantity);

        line.UpdateShippedQuantity(6);
        Assert.Equal(6, line.ShippedQuantity);
        Assert.Equal(4, line.PendingQuantity);

        line.UpdateShippedQuantity(10);
        Assert.True(line.IsFullyShipped);
        Assert.Equal(0, line.PendingQuantity);
    }

    [Fact]
    public void SalesOrder_Cancel_ShouldWorkForPendingOrConfirmed()
    {
        var so = SalesOrder.Create("SO-CAN-001", _customerId, "Grace",
            "grace@test.com", "555-0900", _warehouseId);
        so.Cancel();
        Assert.Equal("CANCELLED", so.Status);
    }

    // ========== SALES ORDER ADDITIONAL EDGE CASES ==========

    [Fact]
    public void SalesOrder_Create_WithInvalidInputs_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() =>
            SalesOrder.Create("", _customerId, "Name", "", "", _warehouseId));
        Assert.Throws<ArgumentException>(() =>
            SalesOrder.Create("SO-X", Guid.Empty, "Name", "", "", _warehouseId));
        Assert.Throws<ArgumentException>(() =>
            SalesOrder.Create("SO-X", _customerId, "", "", "", _warehouseId));
    }

    [Fact]
    public void SalesOrder_Create_WithInvalidChannel_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() =>
            SalesOrder.Create("SO-EDGE-001", _customerId, "Test", "", "", channel: "INVALID"));
    }

    [Fact]
    public void SalesOrder_Confirm_WithZeroQuantityLines_ShouldThrow()
    {
        var so = SalesOrder.Create("SO-EDGE-002", _customerId, "Test", "", "", _warehouseId);
        var line = SalesOrderLine.Create(so.Id, _partId, 1, 100m, 1);
        var field = typeof(SalesOrderLine).GetField("<Quantity>k__BackingField", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field!.SetValue(line, 0);
        so.LineItems.Add(line);
        Assert.Throws<InvalidOperationException>(() => so.Confirm());
    }

    [Fact]
    public void SalesOrder_MarkAsPaid_FromWrongStatus_ShouldThrow()
    {
        var so = SalesOrder.Create("SO-EDGE-003", _customerId, "Test", "", "", _warehouseId);
        Assert.Throws<InvalidOperationException>(() => so.MarkAsPaid());
    }

    [Fact]
    public void SalesOrder_MarkAsPacked_FromWrongStatus_ShouldThrow()
    {
        var so = SalesOrder.Create("SO-EDGE-004", _customerId, "Test", "", "", _warehouseId);
        Assert.Throws<InvalidOperationException>(() => so.MarkAsPacked());
    }

    [Fact]
    public void SalesOrder_MarkAsDelivered_FromWrongStatus_ShouldThrow()
    {
        var so = SalesOrder.Create("SO-EDGE-005", _customerId, "Test", "", "", _warehouseId);
        so.LineItems.Add(SalesOrderLine.Create(so.Id, _partId, 5, 100m, 1));
        so.CalculateTotal();
        so.Confirm();
        so.Cancel();
        Assert.Throws<InvalidOperationException>(() => so.MarkAsDelivered());
    }

    [Fact]
    public void SalesOrder_MarkAsCompleted_FromWrongStatus_ShouldThrow()
    {
        var so = SalesOrder.Create("SO-EDGE-006", _customerId, "Test", "", "", _warehouseId);
        Assert.Throws<InvalidOperationException>(() => so.MarkAsCompleted());
    }

    [Fact]
    public void SalesOrder_Cancel_AfterDelivered_ShouldThrow()
    {
        var so = SalesOrder.Create("SO-EDGE-007", _customerId, "Test", "", "", _warehouseId);
        so.LineItems.Add(SalesOrderLine.Create(so.Id, _partId, 5, 100m, 1));
        so.CalculateTotal();
        so.Confirm();
        so.MarkAsDelivered();
        Assert.Throws<InvalidOperationException>(() => so.Cancel());
    }

    [Fact]
    public void SalesOrder_MarkAsReturned_FromNonDelivered_ShouldThrow()
    {
        var so = SalesOrder.Create("SO-EDGE-008", _customerId, "Test", "", "", _warehouseId);
        Assert.Throws<InvalidOperationException>(() => so.MarkAsReturned());
    }

    [Fact]
    public void SalesOrder_MarkAsReturned_FromDelivered_ShouldSucceed()
    {
        var so = SalesOrder.Create("SO-EDGE-009", _customerId, "Test", "", "", _warehouseId);
        so.LineItems.Add(SalesOrderLine.Create(so.Id, _partId, 5, 100m, 1));
        so.CalculateTotal();
        so.Confirm();
        so.MarkAsDelivered();
        so.MarkAsReturned();
        Assert.Equal("RETURNED", so.Status);
    }

    [Fact]
    public void SalesOrder_RecordPayment_ExceedingGrandTotal_ShouldThrow()
    {
        var so = SalesOrder.Create("SO-EDGE-010", _customerId, "Test", "", "", _warehouseId);
        so.LineItems.Add(SalesOrderLine.Create(so.Id, _partId, 5, 100m, 1));
        so.CalculateTotal();
        Assert.Throws<InvalidOperationException>(() => so.RecordPayment(1000m));
    }

    [Fact]
    public void SalesOrder_ProcessRefund_ExceedingPaid_ShouldThrow()
    {
        var so = SalesOrder.Create("SO-EDGE-011", _customerId, "Test", "", "", _warehouseId);
        so.LineItems.Add(SalesOrderLine.Create(so.Id, _partId, 5, 100m, 1));
        so.CalculateTotal();
        so.RecordPayment(300m);
        Assert.Throws<InvalidOperationException>(() => so.ProcessRefund(500m));
    }

    [Fact]
    public void SalesOrder_ApplyAdditionalDiscount_ExceedingTotal_ShouldThrow()
    {
        var so = SalesOrder.Create("SO-EDGE-012", _customerId, "Test", "", "", _warehouseId);
        so.LineItems.Add(SalesOrderLine.Create(so.Id, _partId, 5, 100m, 1));
        so.CalculateTotal();
        Assert.Throws<ArgumentException>(() => so.ApplyAdditionalDiscount(2000m));
    }

    [Fact]
    public void SalesOrder_ApplyAdditionalDiscount_ShouldReduceTotal()
    {
        var so = SalesOrder.Create("SO-EDGE-013", _customerId, "Test", "", "", _warehouseId);
        so.LineItems.Add(SalesOrderLine.Create(so.Id, _partId, 5, 100m, 1));
        so.CalculateTotal();
        so.ApplyAdditionalDiscount(50m);
        Assert.Equal(450m, so.TotalAmount);
    }

    [Fact]
    public void SalesOrder_SetDiscountPercentage_Exceeding100_ShouldThrow()
    {
        var so = SalesOrder.Create("SO-EDGE-014", _customerId, "Test", "", "", _warehouseId);
        Assert.Throws<ArgumentException>(() => so.SetDiscountPercentage(110));
    }

    [Fact]
    public void SalesOrder_SetTax_Negative_ShouldThrow()
    {
        var so = SalesOrder.Create("SO-EDGE-015", _customerId, "Test", "", "", _warehouseId);
        Assert.Throws<ArgumentException>(() => so.SetTax(-10m));
    }

    [Fact]
    public void SalesOrder_ClearLineItems_ShouldRemoveAll()
    {
        var so = SalesOrder.Create("SO-EDGE-016", _customerId, "Test", "", "", _warehouseId);
        so.LineItems.Add(SalesOrderLine.Create(so.Id, _partId, 5, 100m, 1));
        so.LineItems.Add(SalesOrderLine.Create(so.Id, Guid.NewGuid(), 3, 200m, 2));
        so.ClearLineItems();
        Assert.Empty(so.LineItems);
    }

    [Fact]
    public void SalesOrder_UpdateCustomer_WithInvalidInputs_ShouldThrow()
    {
        var so = SalesOrder.Create("SO-EDGE-017", _customerId, "Test", "", "", _warehouseId);
        Assert.Throws<ArgumentException>(() =>
            so.UpdateCustomer(Guid.Empty, "Name", "", "", ""));
        Assert.Throws<ArgumentException>(() =>
            so.UpdateCustomer(_customerId, "", "", "", ""));
    }

    [Fact]
    public void SalesOrderLine_Create_WithInvalidInputs_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() =>
            SalesOrderLine.Create(Guid.Empty, _partId, 5, 100m, 1));
        Assert.Throws<ArgumentException>(() =>
            SalesOrderLine.Create(Guid.NewGuid(), Guid.Empty, 5, 100m, 1));
        Assert.Throws<ArgumentException>(() =>
            SalesOrderLine.Create(Guid.NewGuid(), _partId, 0, 100m, 1));
        Assert.Throws<ArgumentException>(() =>
            SalesOrderLine.Create(Guid.NewGuid(), _partId, 5, 0m, 1));
        Assert.Throws<ArgumentException>(() =>
            SalesOrderLine.Create(Guid.NewGuid(), _partId, 5, 100m, 0));
        Assert.Throws<ArgumentException>(() =>
            SalesOrderLine.Create(Guid.NewGuid(), _partId, 5, 100m, 1, discount: 100m));
    }

    [Fact]
    public void SalesOrderLine_UpdateShippedQuantity_ExceedingOrdered_ShouldThrow()
    {
        var line = SalesOrderLine.Create(Guid.NewGuid(), _partId, 10, 100m, 1);
        Assert.Throws<InvalidOperationException>(() => line.UpdateShippedQuantity(15));
    }

    // ========== SALES RETURN ADDITIONAL EDGE CASES ==========

    [Fact]
    public void SalesReturn_Create_WithInvalidInputs_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() =>
            SalesReturn.Create("", Guid.NewGuid(), null, "DAMAGED", _warehouseId));
        Assert.Throws<ArgumentException>(() =>
            SalesReturn.Create("SR-X", Guid.Empty, null, "DAMAGED", _warehouseId));
        Assert.Throws<ArgumentException>(() =>
            SalesReturn.Create("SR-X", Guid.NewGuid(), null, "", _warehouseId));
        Assert.Throws<ArgumentException>(() =>
            SalesReturn.Create("SR-X", Guid.NewGuid(), null, "DAMAGED", Guid.Empty));
    }

    [Fact]
    public void SalesReturn_Approve_WithEmptyName_ShouldThrow()
    {
        var sr = SalesReturn.Create("SR-EDGE-001", Guid.NewGuid(), null, "DAMAGED", _warehouseId);
        Assert.Throws<ArgumentException>(() => sr.Approve(""));
    }

    [Fact]
    public void SalesReturn_Process_FromWrongStatus_ShouldThrow()
    {
        var sr = SalesReturn.Create("SR-EDGE-002", Guid.NewGuid(), null, "DAMAGED", _warehouseId);
        Assert.Throws<InvalidOperationException>(() => sr.Process());
    }

    [Fact]
    public void SalesReturnLine_Create_WithInvalidInputs_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() =>
            SalesReturnLine.Create(Guid.Empty, Guid.NewGuid(), _partId, 5, 100m));
        Assert.Throws<ArgumentException>(() =>
            SalesReturnLine.Create(Guid.NewGuid(), Guid.Empty, _partId, 5, 100m));
        Assert.Throws<ArgumentException>(() =>
            SalesReturnLine.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.Empty, 5, 100m));
        Assert.Throws<ArgumentException>(() =>
            SalesReturnLine.Create(Guid.NewGuid(), Guid.NewGuid(), _partId, 0, 100m));
        Assert.Throws<ArgumentException>(() =>
            SalesReturnLine.Create(Guid.NewGuid(), Guid.NewGuid(), _partId, 5, 0m));
        Assert.Throws<ArgumentException>(() =>
            SalesReturnLine.Create(Guid.NewGuid(), Guid.NewGuid(), _partId, 5, 100m, condition: "INVALID"));
    }

    // ========== CHALLAN ==========

    [Fact]
    public void Challan_Create_ShouldInitializeCorrectly()
    {
        var challan = Challan.Create("CH-001", Guid.NewGuid(), "123 Main St", "John", "555-0100");
        Assert.Equal("CH-001", challan.ChallanNumber);
        Assert.Equal("DRAFT", challan.Status);
    }

    [Fact]
    public void Challan_FullLifecycle_ShouldTransitionCorrectly()
    {
        var challan = Challan.Create("CH-002", Guid.NewGuid());

        challan.Issue();
        Assert.Equal("ISSUED", challan.Status);
        Assert.NotNull(challan.IssuedAt);

        challan.MarkDelivered("Receiver", "555-0200");
        Assert.Equal("DELIVERED", challan.Status);
        Assert.NotNull(challan.DeliveredAt);
        Assert.Equal("Receiver", challan.ReceiverName);
    }

    [Fact]
    public void Challan_Issue_FromWrongStatus_ShouldThrow()
    {
        var challan = Challan.Create("CH-003", Guid.NewGuid());
        challan.Issue();
        Assert.Throws<InvalidOperationException>(() => challan.Issue());
    }

    [Fact]
    public void Challan_MarkDelivered_FromDraft_ShouldThrow()
    {
        var challan = Challan.Create("CH-004", Guid.NewGuid());
        Assert.Throws<InvalidOperationException>(() => challan.MarkDelivered());
    }

    [Fact]
    public void Challan_UpdateTransport_ShouldWork()
    {
        var challan = Challan.Create("CH-005", Guid.NewGuid());
        challan.UpdateTransport("UPS", "TRK-123", "Driver", "555-0300");
        Assert.Equal("UPS", challan.TransportCompany);
        Assert.Equal("TRK-123", challan.VehicleNumber);
    }

    [Fact]
    public void Challan_UpdateDeliveryDetails_ShouldWork()
    {
        var challan = Challan.Create("CH-006", Guid.NewGuid());
        challan.UpdateDeliveryDetails("456 Oak Ave", "Jane", "555-0400");
    }

    [Fact]
    public void Challan_LinkToInvoice_ShouldSetReference()
    {
        var challan = Challan.Create("CH-007", Guid.NewGuid());
        challan.LinkToInvoice(Guid.NewGuid());
    }

    [Fact]
    public void ChallanLine_Create_ShouldInitialize()
    {
        var line = ChallanLine.Create(Guid.NewGuid(), _partId, 5,
            "Oil Filter", "OIL-001", "Filter", "PCS", 1);
        Assert.Equal(5, line.Quantity);
    }

    [Fact]
    public void ChallanLine_Create_WithZeroQuantity_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() =>
            ChallanLine.Create(Guid.NewGuid(), _partId, 0, "Part", "SKU", "Display", "PCS", 1));
    }
}
