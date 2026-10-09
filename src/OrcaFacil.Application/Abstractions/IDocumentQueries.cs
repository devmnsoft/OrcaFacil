using OrcaFacil.Application.DTOs;

namespace OrcaFacil.Application.Abstractions;

public interface IDocumentQueries
{
    Task<IReadOnlyList<DocumentSummaryDto>> ListDocumentsAsync(Guid userId, Guid? accountId = null, CancellationToken ct = default);
}
