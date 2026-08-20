using OrcaFacil.Domain.Common;

namespace OrcaFacil.Domain.Entities;

/// <summary>Tenant-owned defaults used when new commercial documents are created.</summary>
public sealed class AccountSettings : Entity
{
    public Guid AccountId { get; set; }
    public string? StateRegistration { get; set; }
    public string? MunicipalRegistration { get; set; }
    public string? WhatsApp { get; set; }
    public string? Website { get; set; }
    public string? PostalCode { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? InstitutionalNotes { get; set; }
    public string? PrimaryColor { get; set; } = "#155eef";
    public string? SecondaryColor { get; set; } = "#172b4d";
    public string? AccentColor { get; set; } = "#12b76a";
    public string? LogoPath { get; set; }
    public string? CompactLogoPath { get; set; }
    public string? VisualSignature { get; set; }
    public string? DocumentFooter { get; set; }
    public string? ShortInstitutionalText { get; set; }
    public int DefaultQuoteValidityDays { get; set; } = 15;
    public string? DefaultNotes { get; set; }
    public string? DefaultCommercialTerms { get; set; }
    public string? DefaultDeliveryTerm { get; set; }
    public string? DefaultSendMessage { get; set; }
    public string QuotePrefix { get; set; } = "ORC";
    public string WorkOrderPrefix { get; set; } = "OS";
    public string ReceiptPrefix { get; set; } = "REC";
    public bool ShowSignature { get; set; } = true;
    public bool ShowBankDetails { get; set; }
    public string? ReceiptNotice { get; set; }
    public int FollowUpAfterSentDays { get; set; } = 2;
    public int FollowUpAfterViewedDays { get; set; } = 1;
    public int ExpirationAlertDays { get; set; } = 2;
    public decimal MaximumDiscountPercent { get; set; }
    public decimal? DesiredMinimumMarginPercent { get; set; }
    public string? DiscountPolicy { get; set; }
    public string? WhatsAppMessage { get; set; }
    public string? EmailMessage { get; set; }
    public string? DefaultLossReason { get; set; }
    public string? AcceptedPaymentMethods { get; set; }
    public string? BankName { get; set; }
    public string? BankBranch { get; set; }
    public string? BankAccount { get; set; }
    public string? Beneficiary { get; set; }
    public string? PixKey { get; set; }
    public string? PaymentInstructions { get; set; }
    public string? ReceiptText { get; set; }
    public string? CollectionMessage { get; set; }
    public string NotificationPreferencesJson { get; set; } = "{}";
    public string CommunicationPreferencesJson { get; set; } = "{}";
}
