using AutoPartShop.Domain.Entities;
using AutoPartsShop.Domain.Entities;
using Xunit;

namespace AutoPartShop.Api.Tests;

/// <summary>
/// Cross-entity integration tests simulating real business flows.
/// Domain entities are pure behavior — services orchestrate them.
/// These tests verify the data flow as a service would.
/// </summary>
public class IntegrationFlowSmokeTests
{
    private readonly Guid _partId = Guid.NewGuid();
    private readonly Guid _warehouseId = Guid.NewGuid();
    private readonly Guid _supplierId = Guid.NewGuid();
    private readonly Guid _customerId = Guid.NewGuid();
    private readonly Guid _paymentProviderId = Guid.NewGuid();
    private readonly DateTime _futureDate = DateTime.UtcNow.AddDays(7);

    // ── PURCHASE → RECEIVE → STOCK FLOW ──────────────────────────────────────

    [Fact]
    public void Integration_PurchaseOrder_To_StockLevel_FullFlow()
    {
        // 1. CREATE & SUBMIT PO
        var po = PurchaseOrder.Create("PO-INT-001", _supplierId, _warehouseId, _futureDate);
        var poLine = PurchaseOrderLine.Create(po.Id, _partId, 100, 50m, 1);
        po.LineItems.Add(poLine);
        po.Submit();
        Assert.Equal("SUBMITTED", po.Status);

        // 2. CONFIRM PO
        po.Confirm("manager");
        Assert.Equal("CONFIRMED", po.Status);

        // 3. CREATE GOODS RECEIPT
        var gr = GoodsReceipt.Create("GRN-INT-001", po.Id, _warehouseId);
        Assert.Equal("PENDING", gr.Status);

        var grLine = GoodsReceiptLine.Create(gr.Id, poLine.Id, _partId,
            orderedQuantity: 100, receivedQuantity: 100, unitCost: 50m,
            damagedQuantity: 0, wrongQuantity: 0, condition: "GOOD");
        gr.LineItems.Add(grLine);

        // 4. VERIFY GR
        gr.Verify("inspector");
        Assert.Equal("VERIFIED", gr.Status);
        Assert.Equal("inspector", gr.VerifiedBy);

        // 5. ACCEPT GR
        gr.Accept();
        Assert.Equal("ACCEPTED", gr.Status);

        // 6. SIMULATE SERVICE: Create StockLot from GR line data
        var lot = StockLot.Create("LOT-INT-001", _partId, _warehouseId, _supplierId,
            grLine.Id, grLine.AcceptedQuantity, grLine.UnitCost, DateTime.UtcNow,
            expiryDate: DateTime.UtcNow.AddMonths(12));
        Assert.Equal(100, lot.QuantityReceived);
        Assert.Equal(100, lot.QuantityAvailable);
        Assert.Equal(50m, lot.CostPrice);
        Assert.Equal("AVAILABLE", lot.Status);

        // 7. SIMULATE SERVICE: Add stock to StockLevel
        var stockLevel = StockLevel.Create(_partId, _warehouseId);
        stockLevel.AddStock(100);
        Assert.Equal(100, stockLevel.QuantityOnHand);
        Assert.Equal(100, stockLevel.QuantityAvailable);
    }

    [Fact]
    public void Integration_GoodsReceipt_Rejected_DoesNotCreateStock()
    {
        var po = PurchaseOrder.Create("PO-INT-002", _supplierId, _warehouseId, _futureDate);
        var poLine = PurchaseOrderLine.Create(po.Id, _partId, 50, 30m, 1);
        po.LineItems.Add(poLine);
        po.Submit();
        po.Confirm("manager");

        var gr = GoodsReceipt.Create("GRN-INT-002", po.Id, _warehouseId);
        var grLine = GoodsReceiptLine.Create(gr.Id, poLine.Id, _partId,
            orderedQuantity: 50, receivedQuantity: 50, unitCost: 30m,
            condition: "DAMAGED");
        gr.LineItems.Add(grLine);
        gr.Verify("inspector");
        gr.Reject("All items damaged");
        Assert.Equal("REJECTED", gr.Status);

        var stockLevel = StockLevel.Create(_partId, _warehouseId);
        Assert.Equal(0, stockLevel.QuantityOnHand);
    }

    [Fact]
    public void Integration_PartialReceipt_OnlyAddsAcceptedStock()
    {
        var po = PurchaseOrder.Create("PO-INT-003", _supplierId, _warehouseId, _futureDate);
        var poLine = PurchaseOrderLine.Create(po.Id, _partId, 100, 20m, 1);
        po.LineItems.Add(poLine);
        po.Submit();
        po.Confirm("manager");

        var gr = GoodsReceipt.Create("GRN-INT-003", po.Id, _warehouseId);
        var grLine = GoodsReceiptLine.Create(gr.Id, poLine.Id, _partId,
            orderedQuantity: 100, receivedQuantity: 70, unitCost: 20m,
            damagedQuantity: 5, wrongQuantity: 3, condition: "GOOD");
        gr.LineItems.Add(grLine);
        gr.Verify("inspector");
        gr.Accept();

        Assert.Equal(62, grLine.AcceptedQuantity);
        Assert.True(grLine.HasDiscrepancy);
        Assert.Equal(8, grLine.RejectedQuantity);

        var lot = StockLot.Create("LOT-INT-003", _partId, _warehouseId, _supplierId,
            grLine.Id, grLine.AcceptedQuantity, grLine.UnitCost, DateTime.UtcNow);
        Assert.Equal(62, lot.QuantityReceived);

        var stockLevel = StockLevel.Create(_partId, _warehouseId);
        stockLevel.AddStock(62);
        Assert.Equal(62, stockLevel.QuantityOnHand);

        poLine.UpdateReceivedQuantity(70);
        Assert.Equal(70, poLine.ReceivedQuantity);
    }

    // ── SALES ORDER → STOCK RESERVATION → DELIVERY → DEDUCTION ────────────────

    [Fact]
    public void Integration_SalesOrder_ReserveAndDeductStock()
    {
        var stockLevel = StockLevel.Create(_partId, _warehouseId);
        stockLevel.AddStock(100);
        Assert.Equal(100, stockLevel.QuantityAvailable);

        var so = SalesOrder.Create("SO-INT-001", _customerId, "Test Customer",
            "cust@test.com", "12345", _warehouseId);
        var line = SalesOrderLine.Create(so.Id, _partId, 30, 100m, 1);
        so.LineItems.Add(line);
        so.Confirm();

        stockLevel.ReserveStock(30);
        Assert.Equal(30, stockLevel.QuantityReserved);
        Assert.Equal(70, stockLevel.QuantityAvailable);

        so.MarkAsDelivered();
        stockLevel.ReleaseReservedStock(30);
        stockLevel.RemoveStock(30);
        Assert.Equal(0, stockLevel.QuantityReserved);
        Assert.Equal(70, stockLevel.QuantityOnHand);
        Assert.Equal(70, stockLevel.QuantityAvailable);
    }

    [Fact]
    public void Integration_SalesOrder_Cancel_ReleasesReservedStock()
    {
        var stockLevel = StockLevel.Create(_partId, _warehouseId);
        stockLevel.AddStock(50);

        var so = SalesOrder.Create("SO-INT-002", _customerId, "Test Customer",
            "cust@test.com", "12345", _warehouseId);
        var line = SalesOrderLine.Create(so.Id, _partId, 20, 100m, 1);
        so.LineItems.Add(line);
        so.Confirm();
        stockLevel.ReserveStock(20);
        Assert.Equal(20, stockLevel.QuantityReserved);
        Assert.Equal(30, stockLevel.QuantityAvailable);

        so.Cancel();
        stockLevel.ReleaseReservedStock(20);
        Assert.Equal(0, stockLevel.QuantityReserved);
        Assert.Equal(50, stockLevel.QuantityAvailable);
    }

    [Fact]
    public void Integration_SalesOrder_CannotOverReserve()
    {
        var stockLevel = StockLevel.Create(_partId, _warehouseId);
        stockLevel.AddStock(10);

        var so = SalesOrder.Create("SO-INT-003", _customerId, "Test Customer",
            "cust@test.com", "12345", _warehouseId);
        var line = SalesOrderLine.Create(so.Id, _partId, 20, 100m, 1);
        so.LineItems.Add(line);
        so.Confirm();

        Assert.Throws<InvalidOperationException>(() => stockLevel.ReserveStock(20));
    }

    // ── SALES RETURN → STOCK ADD-BACK ────────────────────────────────────────

    [Fact]
    public void Integration_SalesReturn_AddsStockBack()
    {
        var stockLevel = StockLevel.Create(_partId, _warehouseId);
        stockLevel.AddStock(20);
        stockLevel.ReserveStock(10);
        stockLevel.ReleaseReservedStock(10);
        stockLevel.RemoveStock(10);
        Assert.Equal(10, stockLevel.QuantityOnHand);

        var so = SalesOrder.Create("SO-INT-004", _customerId, "Test", "", "", _warehouseId);
        var line = SalesOrderLine.Create(so.Id, _partId, 10, 100m, 1);
        so.LineItems.Add(line);
        so.Confirm();
        so.MarkAsDelivered();
        so.MarkAsCompleted();

        var sr = SalesReturn.Create("SR-INT-001", so.Id, null, "Customer changed mind",
            _warehouseId);
        sr.Approve("manager");
        sr.MarkAsReceived();
        sr.Process();

        stockLevel.AddStock(3);
        Assert.Equal(13, stockLevel.QuantityOnHand);
    }

    // ── PURCHASE RETURN → STOCK DEDUCTION ────────────────────────────────────

    [Fact]
    public void Integration_PurchaseReturn_DeductsStock()
    {
        var stockLevel = StockLevel.Create(_partId, _warehouseId);
        stockLevel.AddStock(50);

        var po = PurchaseOrder.Create("PO-INT-004", _supplierId, _warehouseId, _futureDate);
        var poLine = PurchaseOrderLine.Create(po.Id, _partId, 50, 10m, 1);
        po.LineItems.Add(poLine);
        po.Submit();
        po.Confirm("manager");

        var pr = PurchaseReturn.Create("PR-INT-001", po.Id, _supplierId, "Defective");
        var prLine = PurchaseReturnLine.Create(pr.Id, poLine.Id, _partId, 5, 10m, "DEFECTIVE");
        pr.LineItems.Add(prLine);
        pr.Approve("manager");
        pr.MarkAsReturned();
        pr.MarkAsReceived("storekeeper");

        stockLevel.RemoveStock(5);
        Assert.Equal(45, stockLevel.QuantityOnHand);
    }

    // ── INVOICE → PAYMENT → CREDIT NOTE FLOW ────────────────────────────────

    [Fact]
    public void Integration_Invoice_CustomerPayment_CreditFlow()
    {
        var so = SalesOrder.Create("SO-INT-005", _customerId, "Test", "", "", _warehouseId);
        so.LineItems.Add(SalesOrderLine.Create(so.Id, _partId, 5, 200m, 1));
        so.Confirm();

        var inv = Invoice.Create("INV-INT-001", so.Id, 1000m, 0);
        inv.Issue();

        var payment1 = CustomerPayment.Create(_customerId, _paymentProviderId, 600m, "CASH");
        payment1.MarkAsCompleted();
        inv.CustomerPayments.Add(payment1);
        inv.UpdatePaymentStatus();
        Assert.Equal("PARTIALLY_PAID", inv.Status);
        Assert.Equal(600m, inv.AmountPaid);
        Assert.Equal(400m, inv.OutstandingAmount);

        var payment2 = CustomerPayment.Create(_customerId, _paymentProviderId, 400m, "BANK_TRANSFER");
        payment2.MarkAsCompleted();
        inv.CustomerPayments.Add(payment2);
        inv.UpdatePaymentStatus();
        Assert.Equal("PAID", inv.Status);
        Assert.Equal(1000m, inv.AmountPaid);
        Assert.Equal(0, inv.OutstandingAmount);
    }

    [Fact]
    public void Integration_Invoice_Overpayment_CreatesCreditBalance()
    {
        var so = SalesOrder.Create("SO-INT-006", _customerId, "Test", "", "", _warehouseId);
        so.LineItems.Add(SalesOrderLine.Create(so.Id, _partId, 2, 500m, 1));
        so.Confirm();

        var inv = Invoice.Create("INV-INT-002", so.Id, 1000m, 0);
        inv.Issue();

        var payment = CustomerPayment.Create(_customerId, _paymentProviderId, 1200m, "CASH");
        payment.MarkAsCompleted();
        inv.CustomerPayments.Add(payment);
        inv.UpdatePaymentStatus();
        Assert.Equal("PAID", inv.Status);
        Assert.Equal(200m, inv.CreditBalance);
        Assert.True(inv.HasCredit);
    }

    // ── CREDIT NOTE → PURCHASE ORDER ─────────────────────────────────────────

    [Fact]
    public void Integration_CreditNote_AppliedToPurchaseOrder()
    {
        var po = PurchaseOrder.Create("PO-INT-005", _supplierId, _warehouseId, _futureDate);
        po.LineItems.Add(PurchaseOrderLine.Create(po.Id, _partId, 10, 100m, 1));
        po.Submit();
        po.Confirm("manager");

        var cn = CreditNote.Create("CN-INT-001", _supplierId, null, 500m);
        Assert.Equal("AVAILABLE", cn.Status);
        Assert.Equal(500m, cn.AvailableAmount);

        cn.ApplyToPurchaseOrder(po.Id, 300m);
        Assert.Equal("PARTIALLY_USED", cn.Status);
        Assert.Equal(200m, cn.AvailableAmount);
    }

    // ── WARRANTY REGISTRATION → WARRANTY CLAIM FLOW ──────────────────────────

    [Fact]
    public void Integration_WarrantyRegistration_To_Claim()
    {
        var part = Product.Create("Battery", PartNumber.Create("BAT-001"), "BAT-001-SKU",
            Guid.NewGuid(), hasWarranty: true, warrantyPeriodMonths: 24, warrantyType: "MANUFACTURER");
        var so = SalesOrder.Create("SO-INT-007", _customerId, "Test", "", "", _warehouseId);
        var soLine = SalesOrderLine.Create(so.Id, part.Id, 1, 5000m, 1);
        so.LineItems.Add(soLine);
        so.Confirm();
        so.MarkAsDelivered();

        var wr = WarrantyRegistration.Create("WR-001", part.Id, so.Id, soLine.Id,
            _customerId, DateTime.UtcNow.AddMonths(-6), DateTime.UtcNow.AddMonths(-6),
            24, "MANUFACTURER", "Standard manufacturer warranty", "CERT-001");
        Assert.Equal("ACTIVE", wr.Status);
        Assert.True(wr.IsValid());

        var claim = WarrantyClaim.Create("WC-001", wr.Id, _customerId,
            DateTime.UtcNow, "Battery not holding charge", "REPLACEMENT");
        Assert.Equal("PENDING", claim.Status);

        claim.SubmitForReview();
        Assert.Equal("UNDER_REVIEW", claim.Status);

        claim.Approve("manager");
        Assert.Equal("APPROVED", claim.Status);

        claim.StartServiceWithoutTechnician();
        Assert.Equal("IN_PROGRESS", claim.Status);

        claim.Complete("Replaced under warranty");
        Assert.Equal("COMPLETED", claim.Status);

        claim.Close("Claim resolved");
        Assert.Equal("CLOSED", claim.Status);

        wr.MarkAsClaimed();
        Assert.Equal("CLAIMED", wr.Status);
    }

    // ── CUSTOMER CREDIT NOTE → INVOICE APPLICATION ───────────────────────────

    [Fact]
    public void Integration_CustomerCreditNote_AppliedToInvoice()
    {
        var so = SalesOrder.Create("SO-INT-008", _customerId, "Test", "", "", _warehouseId);
        so.LineItems.Add(SalesOrderLine.Create(so.Id, _partId, 3, 100m, 1));
        so.Confirm();
        so.MarkAsDelivered();

        var sr = SalesReturn.Create("SR-INT-002", so.Id, null, "Defective", _warehouseId);
        sr.Approve("manager");
        sr.MarkAsReceived();
        sr.Process();

        var ccn = CustomerCreditNote.Create("CCN-INT-001", _customerId, sr.Id, 300m);
        Assert.Equal("AVAILABLE", ccn.Status);
        Assert.Equal(300m, ccn.AvailableAmount);

        var inv2 = Invoice.Create("INV-INT-003", so.Id, 500m, 0);
        inv2.Issue();
        ccn.ApplyToInvoice(inv2.Id, so.Id, 200m);
        Assert.Equal("PARTIALLY_USED", ccn.Status);
        Assert.Equal(100m, ccn.AvailableAmount);
    }

    // ── DISCOUNT APPLICATION ─────────────────────────────────────────────────

    [Fact]
    public void Integration_Discount_Calculation()
    {
        var discount = Discount.Create("Summer Sale", "PERCENTAGE", 10m,
            DateTime.UtcNow.AddDays(-1),
            partId: _partId, promoCode: "SUMMER10",
            endDate: DateTime.UtcNow.AddDays(30));
        Assert.True(discount.IsValidOn(DateTime.UtcNow));
        Assert.True(discount.IsActive);

        var discounted = discount.CalculateDiscountAmount(500m);
        Assert.Equal(50m, discounted);
    }

    [Fact]
    public void Integration_Discount_Expired_IsNotValid()
    {
        var discount = Discount.Create("Old Sale", "PERCENTAGE", 20m,
            DateTime.UtcNow.AddDays(-60),
            partId: _partId,
            endDate: DateTime.UtcNow.AddDays(-1));
        Assert.False(discount.IsValidOn(DateTime.UtcNow));
    }
}
