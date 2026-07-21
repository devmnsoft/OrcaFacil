namespace OrcaFacil.Application.Auth;

public record RegisterUserCommand(string Name, string Email, string Password, bool AcceptTerms, bool AcceptPrivacy);
