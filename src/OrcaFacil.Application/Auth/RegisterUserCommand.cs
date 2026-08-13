using OrcaFacil.Domain.Enums;

namespace OrcaFacil.Application.Auth;

public sealed record RegisterUserCommand(
    PersonType AccountType,
    string DocumentNumber,
    string Name,
    string? ProfessionalName,
    string? LegalName,
    string? TradeName,
    string? ResponsibleName,
    string Phone,
    string Email,
    string? PostalCode,
    string? Street,
    string? StreetNumber,
    string? Complement,
    string? District,
    string City,
    string State,
    string Password,
    bool AcceptTerms,
    bool AcceptPrivacy,
    string? CorrelationId = null,
    string SelectedPlanCode = "FREE");
