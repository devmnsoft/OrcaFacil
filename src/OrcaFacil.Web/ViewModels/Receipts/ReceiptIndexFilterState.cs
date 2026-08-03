using OrcaFacil.Domain.Enums;
namespace OrcaFacil.Web.ViewModels.Receipts;
public sealed class ReceiptIndexFilterState
{
    public DateTime? From { get; init; } public DateTime? To { get; init; } public Guid? ClientId { get; init; }
    public string? PaymentMethod { get; init; } public ReceiptOriginType? OriginType { get; init; } public string? Status { get; init; }
    public decimal? MinimumAmount { get; init; } public decimal? MaximumAmount { get; init; }
    public string Sort { get; init; } = "recent"; public int PageNumber { get; init; } = 1; public int PageSize { get; init; } = 25;
}
