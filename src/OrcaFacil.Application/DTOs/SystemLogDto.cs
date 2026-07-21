namespace OrcaFacil.Application.DTOs;

public record SystemLogDto(Guid Id, string Level, string Type, string Message, DateTime CreatedAt);
