using OrcaFacil.Application.DTOs;

namespace OrcaFacil.Application.Documents;

public record UpdateDocumentCommand(Guid UserId, Guid DocumentId, string ClientName, IReadOnlyList<DocumentItemDto> Items, decimal Discount, string? Notes);
