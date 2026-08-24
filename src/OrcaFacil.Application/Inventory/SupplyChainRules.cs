using OrcaFacil.Domain.Entities;

namespace OrcaFacil.Application.Inventory;

public static class SupplyChainRules
{
    public static decimal CalculateMargin(decimal salePrice, decimal cost)
    {
        if (salePrice < 0 || cost < 0) throw new ArgumentOutOfRangeException(nameof(salePrice), "Preço e custo não podem ser negativos.");
        return salePrice == 0 ? 0 : decimal.Round((salePrice - cost) / salePrice * 100m, 4);
    }

    public static bool RequiresMarginApproval(MarginPolicy? policy, decimal salePrice, decimal cost) =>
        policy is { IsActive: true, RequiresApprovalBelowMinimum: true } && CalculateMargin(salePrice, cost) < policy.MinimumMarginPercent;

    public static void ApplyMovement(InventoryItem item, InventoryStockMovement movement, bool allowNegative = false)
    {
        EnsureSameAccount(item.AccountId, movement.AccountId);
        if (movement.Quantity <= 0) throw new ArgumentOutOfRangeException(nameof(movement.Quantity), "A quantidade deve ser maior que zero.");
        if (movement.MovementType is StockMovementType.PositiveAdjustment or StockMovementType.NegativeAdjustment && string.IsNullOrWhiteSpace(movement.Reason))
            throw new InvalidOperationException("Ajuste de estoque exige motivo.");
        var delta = movement.MovementType is StockMovementType.Entry or StockMovementType.PositiveAdjustment or StockMovementType.Return or StockMovementType.PurchaseReceipt ? movement.Quantity : -movement.Quantity;
        if (item.QuantityOnHand + delta < 0 && !allowNegative) throw new InvalidOperationException("Saldo insuficiente para esta movimentação.");
        item.QuantityOnHand += delta;
        item.Touch();
    }

    public static void Reserve(InventoryItem item, InventoryReservation reservation)
    {
        EnsureSameAccount(item.AccountId, reservation.AccountId);
        if (reservation.Quantity <= 0) throw new ArgumentOutOfRangeException(nameof(reservation.Quantity));
        if (reservation.Quantity > item.QuantityAvailable) throw new InvalidOperationException("Estoque disponível insuficiente para reserva.");
        item.QuantityReserved += reservation.Quantity;
        item.Touch();
    }

    public static void ValidatePurchaseReceipt(PurchaseOrderItem item, decimal quantity)
    {
        if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity));
        if (item.ReceivedQuantity + quantity > item.Quantity) throw new InvalidOperationException("Quantidade recebida excede a quantidade pendente.");
    }

    public static void EnsureSameAccount(Guid expected, Guid actual)
    {
        if (expected == Guid.Empty || expected != actual) throw new UnauthorizedAccessException("O recurso não pertence à conta ativa.");
    }
}
