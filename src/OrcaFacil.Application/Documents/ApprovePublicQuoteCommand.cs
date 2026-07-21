namespace OrcaFacil.Application.Documents;

public record ApprovePublicQuoteCommand(string Token, string Name, string? Document, string? Email, string? Note, bool AcceptedTerms, string UserAgent);
