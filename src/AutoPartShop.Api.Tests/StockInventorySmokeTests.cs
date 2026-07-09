using AutoPartShop.Domain.Entities;
using Xunit;

namespace AutoPartShop.Api.Tests;

public class StockInventorySmokeTests
{
    private readonly Guid _partId = Guid.NewGuid();
    private readonly Guid _warehouseId = Guid.NewGuid();
    private readonly Guid _unitId = Guid.NewGuid();

    [Fact]
    public void StockLevel_Create_ShouldInitializeWithZeroStock()
    {
        var stock = StockLevel.Create(_partId, _warehouseId, reorderLevel: 5, reorderQuantity: 20);

        Assert.Equal(_partId, stock.PartId);
        Assert.Equal(_warehouseId, stock.WarehouseId);
        Assert.Equal(0, stock.QuantityOnHand);
        Assert.Equal(0, stock.QuantityReserved);
        Assert.Equal(0, stock.QuantityAvailable);
        Assert.Equal(5, stock.ReorderLevel);
        Assert.Equal(20, stock.ReorderQuantity);
        Assert.True(stock.IsActive);
        Assert.True(stock.NeedsReorder);
    }

    [Fact]
    public void StockLevel_AddStock_ShouldIncreaseQuantity()
    {
        var stock = StockLevel.Create(_partId, _warehouseId);

        stock.AddStock(100);
        Assert.Equal(100, stock.QuantityOnHand);
        Assert.Equal(100, stock.QuantityAvailable);

        stock.AddStock(50);
        Assert.Equal(150, stock.QuantityOnHand);
    }

    [Fact]
    public void StockLevel_RemoveStock_ShouldDecreaseAvailableQuantity()
    {
        var stock = StockLevel.Create(_partId, _warehouseId);
        stock.AddStock(100);

        stock.RemoveStock(30);
        Assert.Equal(70, stock.QuantityOnHand);
        Assert.Equal(70, stock.QuantityAvailable);
    }

    [Fact]
    public void StockLevel_RemoveStock_InsufficientStock_ShouldThrow()
    {
        var stock = StockLevel.Create(_partId, _warehouseId);
        stock.AddStock(10);

        Assert.Throws<InvalidOperationException>(() => stock.RemoveStock(20));
    }

    [Fact]
    public void StockLevel_ReserveAndRelease_ShouldTrackReservedQuantity()
    {
        var stock = StockLevel.Create(_partId, _warehouseId);
        stock.AddStock(100);

        stock.ReserveStock(30);
        Assert.Equal(30, stock.QuantityReserved);
        Assert.Equal(70, stock.QuantityAvailable);

        stock.ReleaseReservedStock(10);
        Assert.Equal(20, stock.QuantityReserved);
        Assert.Equal(80, stock.QuantityAvailable);
    }

    [Fact]
    public void StockLevel_ReserveMoreThanAvailable_ShouldThrow()
    {
        var stock = StockLevel.Create(_partId, _warehouseId);
        stock.AddStock(10);

        Assert.Throws<InvalidOperationException>(() => stock.ReserveStock(20));
    }

    [Fact]
    public void StockLevel_DamagedStock_ShouldTrackNonSellableInventory()
    {
        var stock = StockLevel.Create(_partId, _warehouseId);
        stock.AddStock(100);

        stock.AddDamagedStock(5);
        Assert.Equal(5, stock.QuantityDamaged);
        Assert.Equal(100, stock.QuantityAvailable);

        stock.RemoveDamagedStock(3);
        Assert.Equal(2, stock.QuantityDamaged);
    }

    [Fact]
    public void StockLevel_QuarantineStock_ShouldTrackNonSellableInventory()
    {
        var stock = StockLevel.Create(_partId, _warehouseId);
        stock.AddStock(100);

        stock.AddQuarantineStock(8);
        Assert.Equal(8, stock.QuantityQuarantine);
        Assert.Equal(100, stock.QuantityAvailable);

        stock.RemoveQuarantineStock(3);
        Assert.Equal(5, stock.QuantityQuarantine);
    }

    [Fact]
    public void StockLevel_UpdateReorderLevel_ShouldChangeThreshold()
    {
        var stock = StockLevel.Create(_partId, _warehouseId, reorderLevel: 5, reorderQuantity: 20);

        stock.UpdateReorderLevel(10, 50);
        Assert.Equal(10, stock.ReorderLevel);
        Assert.Equal(50, stock.ReorderQuantity);
        Assert.True(stock.NeedsReorder);
    }

    [Fact]
    public void StockLevel_ActivateDeactivate_ShouldToggleIsActive()
    {
        var stock = StockLevel.Create(_partId, _warehouseId);
        Assert.True(stock.IsActive);

        stock.Deactivate();
        Assert.False(stock.IsActive);

        stock.Activate();
        Assert.True(stock.IsActive);
    }

    [Fact]
    public void StockMovement_Create_ShouldRecordMovement()
    {
        var stockId = Guid.NewGuid();
        var movement = StockMovement.Create(
            stockId, "IN", 50, "GRN-001", "PO-001", DateTime.UtcNow, _unitId, 50);

        Assert.Equal("IN", movement.MovementType);
        Assert.Equal(50, movement.Quantity);
        Assert.Equal("GRN-001", movement.Reason);
        Assert.Equal("PO-001", movement.ReferenceNumber);
        Assert.Equal(stockId, movement.StockLevelId);
    }

    [Fact]
    public void StockMovement_InvalidTypes_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() =>
            StockMovement.Create(Guid.NewGuid(), "INVALID", 10));
    }

    [Fact]
    public void StockMovement_ZeroQuantity_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() =>
            StockMovement.Create(Guid.NewGuid(), "IN", 0));
    }

    [Fact]
    public void StockMovement_AllMovementTypes_ShouldBeValid()
    {
        var stockId = Guid.NewGuid();
        var validTypes = new[] { "IN", "OUT", "RETURN", "ADJUST", "TRANSFER" };

        foreach (var type in validTypes)
        {
            var movement = StockMovement.Create(stockId, type, 10, "Test", "REF-001");
            Assert.Equal(type, movement.MovementType);
        }
    }

    [Fact]
    public void StockMovement_Approve_ShouldSetApprovedBy()
    {
        var movement = StockMovement.Create(Guid.NewGuid(), "ADJUST", 5, "Count variance");
        movement.Approve("admin");

        Assert.Equal("admin", movement.ApprovedBy);
    }

    [Fact]
    public void StockLevel_AddStockInBaseUnit_ShouldTrackBoth()
    {
        var stock = StockLevel.Create(_partId, _warehouseId);

        stock.AddStock(10, 20);
        Assert.Equal(10, stock.QuantityOnHand);
        Assert.Equal(20, stock.QuantityOnHandInBaseUnit);
    }

    [Fact]
    public void StockLevel_FullAdjustmentCycle_ShouldMaintainConsistency()
    {
        var stock = StockLevel.Create(_partId, _warehouseId);

        stock.AddStock(200);
        stock.ReserveStock(50);
        Assert.Equal(150, stock.QuantityAvailable);

        stock.RemoveStock(30);
        Assert.Equal(120, stock.QuantityAvailable);
        Assert.Equal(120, stock.QuantityOnHand - stock.QuantityReserved);
    }
}
