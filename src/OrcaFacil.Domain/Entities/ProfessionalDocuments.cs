using OrcaFacil.Domain.Common;

namespace OrcaFacil.Domain.Entities;

public enum FileAssetCategory { Logo, ProposalAttachment, ReceiptAttachment, WorkOrderAttachment, ContractAttachment, PaymentProof, SupportAttachment, General }
public enum FileAssetVisibility { Private, Account, PublicLinked }
public enum DocumentTemplateType { Proposal, Receipt, WorkOrder, Contract, Collection, Email }

public sealed class FileAsset : Entity
{
    public Guid AccountId { get; set; }
    public Guid UploadedByUserId { get; set; }
    public string OriginalFileName { get; set; } = "";
    public string StoredFileName { get; set; } = "";
    public string StoragePath { get; set; } = "";
    public string ContentType { get; set; } = "application/octet-stream";
    public string Extension { get; set; } = "";
    public long SizeInBytes { get; set; }
    public string Sha256Hash { get; set; } = "";
    public FileAssetCategory Category { get; set; }
    public FileAssetVisibility Visibility { get; set; }
}

public sealed class FileAssetLink : Entity
{
    public Guid AccountId { get; set; }
    public Guid FileAssetId { get; set; }
    public string EntityType { get; set; } = "";
    public Guid EntityId { get; set; }
    public FileAssetVisibility Visibility { get; set; }
    public Guid CreatedByUserId { get; set; }
}

public sealed class CompanyBrandingProfile : Entity
{
    public Guid AccountId { get; set; }
    public Guid? LogoFileAssetId { get; set; }
    public string TradeName { get; set; } = "";
    public string? LegalName { get; set; }
    public string? DocumentNumber { get; set; }
    public string? Phone { get; set; }
    public string? WhatsApp { get; set; }
    public string? CommercialEmail { get; set; }
    public string? Website { get; set; }
    public string? Address { get; set; }
    public string PrimaryColor { get; set; } = "#155eef";
    public string SecondaryColor { get; set; } = "#172b4d";
    public string? DefaultFooter { get; set; }
    public string? DefaultCommercialNotes { get; set; }
    public string? VisualSignature { get; set; }
}

public sealed class DocumentTemplate : Entity
{
    public Guid? AccountId { get; set; }
    public string Name { get; set; } = "";
    public DocumentTemplateType Type { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class DocumentTemplateVersion : Entity
{
    public Guid TemplateId { get; set; }
    public int VersionNumber { get; set; }
    public string Content { get; set; } = "";
    public string? CssContent { get; set; }
    public string? HeaderContent { get; set; }
    public string? FooterContent { get; set; }
    public string VariablesJson { get; set; } = "[]";
    public DateTime? PublishedAt { get; set; }
}

public sealed class DocumentAuditEvent : Entity
{
    public Guid AccountId { get; set; }
    public Guid? UserId { get; set; }
    public string EventType { get; set; } = "";
    public string EntityType { get; set; } = "";
    public Guid EntityId { get; set; }
    public string? MetadataJson { get; set; }
}
