namespace OrcaFacil.Application.DTOs;

public record UserSummaryDto(Guid Id, string Name, string Email, string Role, string Plan, int SessionVersion);
