namespace OrcaFacil.Application.Profile;

public record SaveIssuerProfileCommand(Guid UserId, string BusinessName, string? DocumentNumber, string? Phone, string? Email, string? Address, string? City, string? PixKey, string? LogoPath);
