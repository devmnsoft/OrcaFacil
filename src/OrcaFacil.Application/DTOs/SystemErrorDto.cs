namespace OrcaFacil.Application.DTOs;

public record SystemErrorDto(Guid Id, string Message, string Severity, bool Resolved, DateTime CreatedAt);
