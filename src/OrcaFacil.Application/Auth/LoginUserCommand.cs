namespace OrcaFacil.Application.Auth;

public record LoginUserCommand(string Email, string Password, string? CorrelationId = null);
