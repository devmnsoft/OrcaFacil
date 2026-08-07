using OrcaFacil.Domain.Enums;
using OrcaFacil.Application.Common;

namespace OrcaFacil.Application.Documents;

public sealed record PublicQuoteView(
    Guid AccessId,
    Guid DocumentId,
    int Version,
    DateTime ExpiresAt,
    DocumentSnapshot Snapshot,
    bool DecisionRegistered);

public interface IPublicDocumentAccessService
{
    Task<OperationResult<PublicQuoteView>> OpenAsync(string token, string remoteAddress, string userAgent, CancellationToken ct = default);
}
