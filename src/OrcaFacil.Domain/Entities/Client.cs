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
    public bool IsActive { get; set; } = true;
    public bool IsFavorite { get; set; }
    public string? InternalNotes { get; set; }
    public DateTime? LastInteractionAt { get; set; }
    public DateTime? NextFollowUpAt { get; set; }
    public string? PreferredContactChannel { get; set; }
    public uint Version { get; set; }

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

public sealed class ClientContact : Entity
{
    public Guid AccountId { get; set; }
    public Guid ClientId { get; set; }
    public string Name { get; set; } = string.Empty;
    public ClientContactType ContactType { get; set; }
    public string Value { get; set; } = string.Empty;
    public string? Label { get; set; }
    public bool IsPrimary { get; set; }
    public bool ReceivesQuotes { get; set; }
    public bool ReceivesReceipts { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
}

public sealed class ClientTag : Entity
{
    public Guid AccountId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string NormalizedName { get; set; } = string.Empty;
    public string ColorToken { get; set; } = "accent";
    public bool IsActive { get; set; } = true;
}

public sealed class ClientTagAssignment
{
    public Guid AccountId { get; set; }
    public Guid ClientId { get; set; }
    public Guid ClientTagId { get; set; }
}

public sealed class ClientNote : Entity
{
    public Guid AccountId { get; set; }
    public Guid ClientId { get; set; }
    public string Content { get; set; } = string.Empty;
    public bool IsPinned { get; set; }
    public Guid CreatedByUserId { get; set; }
}
