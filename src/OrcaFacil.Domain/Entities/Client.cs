using OrcaFacil.Domain.Common;
using OrcaFacil.Domain.Enums;

namespace OrcaFacil.Domain.Entities;

public class Client : Entity
{
    public Guid? AccountId { get; set; }
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
    public string? Notes { get; set; }

    public void NormalizeAndValidate()
    {
        DocumentType = PersonType == PersonType.Company ? BrazilianDocumentType.CNPJ : BrazilianDocumentType.CPF;
        DocumentNumber = BrazilianDocument.Normalize(DocumentNumber);
        if (!BrazilianDocument.HasValidCheckDigits(DocumentType, DocumentNumber))
        {
            throw new InvalidOperationException(DocumentType == BrazilianDocumentType.CNPJ ? "CNPJ inválido. Informe 14 números." : "CPF inválido. Informe 11 números.");
        }
        Touch();
    }
}
