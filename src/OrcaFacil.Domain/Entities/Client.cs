using OrcaFacil.Domain.Common;
using OrcaFacil.Domain.Enums;

namespace OrcaFacil.Domain.Entities;

public class Client : Entity
{
    public Guid UserId { get; set; }
    public PersonType PersonType { get; set; } = PersonType.Individual;
    public DocumentType? DocumentType { get; set; } = Enums.DocumentType.CPF;
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
        DocumentType = PersonType == PersonType.Company ? Enums.DocumentType.CNPJ : Enums.DocumentType.CPF;
        DocumentNumber = BrazilianDocument.Normalize(DocumentNumber);
        if (!BrazilianDocument.HasBasicValidLength(DocumentType, DocumentNumber))
        {
            throw new InvalidOperationException(DocumentType == Enums.DocumentType.CNPJ ? "CNPJ inválido. Informe 14 números." : "CPF inválido. Informe 11 números.");
        }
        Touch();
    }
}
