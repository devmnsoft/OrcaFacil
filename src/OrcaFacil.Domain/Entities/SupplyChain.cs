using OrcaFacil.Domain.Common;

namespace OrcaFacil.Domain.Entities;

public enum SupplierStatus { Active, Inactive, Blocked, Preferred, UnderReview }
public enum StockMovementType { Entry, Exit, Reservation, ReservationRelease, WorkOrderConsumption, PositiveAdjustment, NegativeAdjustment, Return, Transfer, PurchaseReceipt, Cancellation }
public enum PurchaseRequestStatus { Draft, Requested, Approved, Rejected, ConvertedToPurchaseOrder, Canceled }
public enum PurchaseOrderStatus { Draft, PendingApproval, Approved, Sent, PartiallyReceived, Received, Canceled, Rejected }

public sealed class Supplier : Entity
{
    public Guid AccountId { get; set; }
    public string Name { get; set; } = "";
    public string? LegalName { get; set; }
    public string? DocumentNumber { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? WhatsApp { get; set; }
    public string? Website { get; set; }
    public string? AddressLine { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? ZipCode { get; set; }
    public string? Category { get; set; }
    public SupplierStatus Status { get; set; } = SupplierStatus.Active;
    public string? Notes { get; set; }
}

public sealed class MaterialUnit : Entity { public Guid? AccountId { get; set; } public string Name { get; set; } = ""; public string Symbol { get; set; } = ""; public bool IsGlobal { get; set; } public bool IsActive { get; set; } = true; }
public sealed class MaterialCategory : Entity { public Guid AccountId { get; set; } public string Name { get; set; } = ""; public bool IsActive { get; set; } = true; }
public sealed class Material : Entity
{
    public Guid AccountId { get; set; } public string Code { get; set; } = ""; public string Name { get; set; } = ""; public string? Description { get; set; }
    public Guid CategoryId { get; set; } public Guid UnitId { get; set; } public decimal DefaultCost { get; set; } public decimal DefaultSalePrice { get; set; }
    public decimal MinimumStock { get; set; } public bool IsStockControlled { get; set; } public bool IsActive { get; set; } = true;
}
public sealed class MaterialSupplierPrice : Entity { public Guid AccountId { get; set; } public Guid SupplierId { get; set; } public Guid MaterialId { get; set; } public decimal UnitCost { get; set; } public decimal MinimumQuantity { get; set; } = 1; public int LeadTimeDays { get; set; } public DateTime ValidFrom { get; set; } public DateTime? ValidUntil { get; set; } public bool IsPreferred { get; set; } }
public sealed class InventoryLocation : Entity { public Guid AccountId { get; set; } public Guid? BusinessUnitId { get; set; } public string Name { get; set; } = ""; public string? Description { get; set; } public bool IsDefault { get; set; } public bool IsActive { get; set; } = true; }
public sealed class InventoryItem : Entity { public Guid AccountId { get; set; } public Guid MaterialId { get; set; } public Guid InventoryLocationId { get; set; } public decimal QuantityOnHand { get; set; } public decimal QuantityReserved { get; set; } public decimal AverageCost { get; set; } public decimal QuantityAvailable => QuantityOnHand - QuantityReserved; }
public sealed class InventoryStockMovement : Entity { public Guid AccountId { get; set; } public Guid MaterialId { get; set; } public Guid InventoryLocationId { get; set; } public Guid? WorkOrderId { get; set; } public Guid? PurchaseOrderId { get; set; } public StockMovementType MovementType { get; set; } public decimal Quantity { get; set; } public decimal? UnitCost { get; set; } public string? Reason { get; set; } public Guid CreatedByUserId { get; set; } public Guid? ReversesMovementId { get; set; } public string? IdempotencyKey { get; set; } }
public sealed class InventoryReservation : Entity { public Guid AccountId { get; set; } public Guid WorkOrderId { get; set; } public Guid MaterialId { get; set; } public Guid InventoryLocationId { get; set; } public decimal Quantity { get; set; } public decimal ConsumedQuantity { get; set; } public bool IsReleased { get; set; } }
public sealed class PurchaseRequest : Entity { public Guid AccountId { get; set; } public Guid? BusinessUnitId { get; set; } public Guid RequestedByUserId { get; set; } public Guid? WorkOrderId { get; set; } public Guid? DocumentId { get; set; } public PurchaseRequestStatus Status { get; set; } public string Reason { get; set; } = ""; public DateTime? NeededByDate { get; set; } }
public sealed class PurchaseRequestItem : Entity { public Guid AccountId { get; set; } public Guid PurchaseRequestId { get; set; } public Guid? MaterialId { get; set; } public string Description { get; set; } = ""; public decimal Quantity { get; set; } public decimal? EstimatedUnitCost { get; set; } public Guid? PreferredSupplierId { get; set; } }
public sealed class PurchaseOrder : Entity { public Guid AccountId { get; set; } public Guid SupplierId { get; set; } public Guid? BusinessUnitId { get; set; } public Guid? PurchaseRequestId { get; set; } public string PurchaseOrderNumber { get; set; } = ""; public PurchaseOrderStatus Status { get; set; } public DateTime IssueDate { get; set; } public DateTime? ExpectedDeliveryDate { get; set; } public decimal Subtotal { get; set; } public decimal DiscountAmount { get; set; } public decimal TotalAmount { get; set; } public string? Notes { get; set; } public Guid CreatedByUserId { get; set; } public Guid? ApprovedByUserId { get; set; } }
public sealed class PurchaseOrderItem : Entity { public Guid AccountId { get; set; } public Guid PurchaseOrderId { get; set; } public Guid? MaterialId { get; set; } public string Description { get; set; } = ""; public decimal Quantity { get; set; } public decimal UnitCost { get; set; } public decimal TotalCost { get; set; } public decimal ReceivedQuantity { get; set; } }
public sealed class CostComposition : Entity { public Guid AccountId { get; set; } public string Name { get; set; } = ""; public string? Description { get; set; } public string TargetType { get; set; } = "Service"; public Guid? TargetId { get; set; } public bool IsActive { get; set; } = true; }
public sealed class CostCompositionItem : Entity { public Guid AccountId { get; set; } public Guid CompositionId { get; set; } public Guid? MaterialId { get; set; } public string Description { get; set; } = ""; public string CostType { get; set; } = "Other"; public decimal Quantity { get; set; } public decimal UnitCost { get; set; } public decimal TotalCost { get; set; } public decimal MarkupPercent { get; set; } public decimal SalePrice { get; set; } }
public sealed class DocumentCostSnapshot : Entity { public Guid AccountId { get; set; } public Guid DocumentId { get; set; } public decimal EstimatedCost { get; set; } public decimal SalePrice { get; set; } public string ItemsJson { get; set; } = "[]"; public DateTime CalculatedAt { get; set; } public Guid CalculatedByUserId { get; set; } }
public sealed class DocumentMarginSnapshot : Entity { public Guid AccountId { get; set; } public Guid DocumentId { get; set; } public Guid CostSnapshotId { get; set; } public decimal EstimatedMarginPercent { get; set; } public bool RequiresApproval { get; set; } }
public sealed class MarginPolicy : Entity { public Guid AccountId { get; set; } public Guid? BusinessUnitId { get; set; } public string Name { get; set; } = ""; public decimal MinimumMarginPercent { get; set; } public decimal WarningMarginPercent { get; set; } public bool RequiresApprovalBelowMinimum { get; set; } public bool IsActive { get; set; } = true; }
