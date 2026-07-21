using OrcaFacil.Application.DTOs;

namespace OrcaFacil.Application.Documents;

public record CreateReceiptCommand(Guid UserId, string ClientName, IReadOnlyList<DocumentItemDto> Items, decimal Discount, string? Notes);
