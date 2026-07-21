using OrcaFacil.Application.DTOs;

namespace OrcaFacil.Application.Abstractions;

public interface IDocumentQueries
{
    Task<IReadOnlyList<DocumentSummaryDto>> ListDocumentsAsync(Guid userId, CancellationToken ct = default);
}
