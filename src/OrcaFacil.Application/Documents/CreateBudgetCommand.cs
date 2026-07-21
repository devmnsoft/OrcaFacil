using OrcaFacil.Application.DTOs;

namespace OrcaFacil.Application.Documents;

public record CreateBudgetCommand(Guid UserId, string ClientName, IReadOnlyList<DocumentItemDto> Items, decimal Discount, string? Notes);
