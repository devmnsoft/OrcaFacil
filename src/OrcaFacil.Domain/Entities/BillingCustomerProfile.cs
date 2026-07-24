using OrcaFacil.Domain.Common;
using OrcaFacil.Domain.Enums;

namespace OrcaFacil.Domain.Entities;

public class BillingCustomerProfile : Entity
{
    public Guid UserId { get; set; }
    public PersonType PersonType { get; set; } = PersonType.Individual;
    public BrazilianDocumentType? DocumentType { get; set; } = BrazilianDocumentType.CPF;
    public string? DocumentNumber { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? TradeName { get; set; }
    public string? LegalName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? City { get; set; }
    public string? Address { get; set; }
    public string? MercadoPagoCustomerId { get; set; }
}
