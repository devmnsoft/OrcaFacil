namespace OrcaFacil.Application.DTOs;

public record DocumentSummaryDto(Guid Id, string Type, string Number, string Status, string ClientName, decimal Total, DateTime CreatedAt);
