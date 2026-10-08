using OrcaFacil.Domain.Enums;

namespace OrcaFacil.Application.Documents;

public interface IDocumentNumberService
{
    Task<string> NextAsync(Guid userId, DocumentType type, Guid? accountId = null, CancellationToken ct = default);
}
