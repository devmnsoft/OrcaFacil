using OrcaFacil.Application.DTOs;
using OrcaFacil.Domain.Enums;

namespace OrcaFacil.Application.Documents;

public record CreateDocumentCommand(Guid UserId, DocumentType Type, string Number, string ClientName, IReadOnlyList<DocumentItemDto> Items, decimal Discount, string? Notes);
