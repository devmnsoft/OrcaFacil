using OrcaFacil.Application.Inventory;
using OrcaFacil.Domain.Entities;

namespace OrcaFacil.UnitTests;

public sealed class SupplyChainRulesTests
{
    [Fact]
    public void Movement_rejects_cross_account_and_negative_stock()
    {
        var account = Guid.NewGuid();
        var item = new InventoryItem { AccountId = account, QuantityOnHand = 2 };
        var foreign = new InventoryStockMovement { AccountId = Guid.NewGuid(), Quantity = 1, MovementType = StockMovementType.Exit };
        Assert.Throws<UnauthorizedAccessException>(() => SupplyChainRules.ApplyMovement(item, foreign));
        var excessive = new InventoryStockMovement { AccountId = account, Quantity = 3, MovementType = StockMovementType.Exit };
        Assert.Throws<InvalidOperationException>(() => SupplyChainRules.ApplyMovement(item, excessive));
    }

    [Fact]
    public void Adjustment_requires_reason_and_reservation_uses_available_balance()
    {
        var account = Guid.NewGuid();
        var item = new InventoryItem { AccountId = account, QuantityOnHand = 10, QuantityReserved = 4 };
        var adjustment = new InventoryStockMovement { AccountId = account, Quantity = 1, MovementType = StockMovementType.PositiveAdjustment };
        Assert.Throws<InvalidOperationException>(() => SupplyChainRules.ApplyMovement(item, adjustment));
        Assert.Throws<InvalidOperationException>(() => SupplyChainRules.Reserve(item, new InventoryReservation { AccountId = account, Quantity = 7 }));
    }

    [Fact]
    public void Margin_policy_requires_approval_below_minimum()
    {
        var policy = new MarginPolicy { AccountId = Guid.NewGuid(), MinimumMarginPercent = 30, RequiresApprovalBelowMinimum = true, IsActive = true };
        Assert.True(SupplyChainRules.RequiresMarginApproval(policy, 100, 80));
        Assert.False(SupplyChainRules.RequiresMarginApproval(policy, 100, 60));
    }

    [Fact]
    public void Receipt_cannot_exceed_pending_quantity()
    {
        var item = new PurchaseOrderItem { Quantity = 5, ReceivedQuantity = 4 };
        Assert.Throws<InvalidOperationException>(() => SupplyChainRules.ValidatePurchaseReceipt(item, 2));
        SupplyChainRules.ValidatePurchaseReceipt(item, 1);
    }
}
