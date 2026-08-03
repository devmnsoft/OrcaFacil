using OrcaFacil.Domain.Enums;

namespace OrcaFacil.Application.Receipts;

public sealed record ReceiptListQuery(DateTime? From, DateTime? To, Guid? ClientId, string? PaymentMethod,
    ReceiptOriginType? OriginType, string? Status, decimal? MinimumAmount, decimal? MaximumAmount,
    string Sort = "recent", int Page = 1, int PageSize = 25);

public sealed record ReceiptListItem(Guid Id, string Number, Guid ClientId, string ClientName,
    ReceiptOriginType OriginType, string OriginLabel, decimal Amount, string PaymentMethodCode,
    string PaymentMethodLabel, DateTime IssuedAt, DateTime? LastSharedAt, DateTime? CancelledAt,
    string StatusCode, string StatusLabel, string StatusTone, string NextActionCode, string NextActionLabel);

public sealed record ReceiptListResult(IReadOnlyList<ReceiptListItem> Items, int Total, int Page,
    int PageSize, int TotalPages, decimal ActiveAmount, int IssuedCount, int SharedCount, int CancelledCount);

public interface IReceiptQueryService
{
    Task<ReceiptListResult?> ListAsync(ReceiptListQuery query, CancellationToken ct = default);
}
