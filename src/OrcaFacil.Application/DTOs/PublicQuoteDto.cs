namespace OrcaFacil.Application.DTOs;

public record PublicQuoteDto(string Number, string ClientName, decimal Total, IReadOnlyList<DocumentItemDto> Items, string? Notes);
