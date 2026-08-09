using System.Security.Cryptography;
using System.Text;
using OrcaFacil.Domain.Common;
using OrcaFacil.Domain.Enums;

namespace OrcaFacil.Domain.Entities;

public class Document : Entity
{
    public Guid? AccountId { get; set; }
    public Guid? ClientId { get; set; }
    public Guid UserId { get; set; }
    public DocumentType Type { get; set; }
    public string Number { get; private set; } = string.Empty;
    public string Status { get; set; } = "Draft";
    public string ClientName { get; set; } = string.Empty;
    public string? ClientDocument { get; set; }
    public string? ClientPhone { get; set; }
    public string? ClientEmail { get; set; }
    public string? ClientCity { get; set; }
    public string? ClientSnapshot { get; set; }
    public int CurrentWizardStep { get; set; }
    public DateTime IssueDate { get; set; } = DateTime.UtcNow;
    public DateTime? ValidUntil { get; set; }
    public DateTime? ExpectedStartAt { get; set; }
    public string? EstimatedDuration { get; set; }
    public string? PaymentMethod { get; set; }
    public int? InstallmentCount { get; set; }
    public decimal? DepositAmount { get; set; }
    public string? PixInformation { get; set; }
    public string? WarrantyText { get; set; }
    public string? ConditionsText { get; set; }
    public string TemplateCode { get; set; } = "essential";
    public string? TemplateSnapshot { get; set; }
    public byte[] RowVersion { get; set; } = Guid.NewGuid().ToByteArray();
    public DateTime? LastAutosavedAt { get; set; }
    public string? LastAutosaveKey { get; set; }
    public string? Notes { get; set; }
    public DateTime? NextFollowUpAt { get; set; }
    public DateTime? LastFollowUpAt { get; set; }
    public FollowUpStatus FollowUpStatus { get; set; } = FollowUpStatus.None;
    public string? FollowUpNote { get; set; }
    public decimal Subtotal { get; private set; }
    public decimal Discount { get; set; }
    public decimal Total { get; private set; }
    public string? PublicToken { get; set; }
    public bool PublicEnabled { get; set; }
    public ClientDecision ClientDecision { get; set; } = ClientDecision.Pending;
    public DateTime? ClientDecisionAt { get; set; }
    public string? ClientDecisionNote { get; set; }
    public string? EvidenceHash { get; set; }
    public Guid? OriginBudgetId { get; set; }
    public string? OriginBudgetNumber { get; set; }
    public Guid? ConvertedReceiptId { get; set; }
    public string? ConvertedReceiptNumber { get; set; }
    public DateTime? DeletedAt { get; private set; }
    public Guid? DeletedBy { get; private set; }
    public List<DocumentItem> Items { get; set; } = [];

    public void IssueNumber(string number)
    {
        if (!string.IsNullOrWhiteSpace(Number)) throw new InvalidOperationException("Número emitido é imutável.");
        Number = number;
    }

    public void CalculateTotals()
    {
        Subtotal = Items.Sum(item => item.CalculateTotal());
        Total = Math.Max(0, Subtotal - Discount);
    }

    public Document ConvertToReceipt(string receiptNumber)
    {
        if (Type != DocumentType.Budget || ClientDecision != ClientDecision.Approved)
        {
            throw new InvalidOperationException("Somente orçamento aprovado pode virar recibo.");
        }

        var receipt = new Document
        {
            AccountId = AccountId,
            ClientId = ClientId,
            UserId = UserId,
            Type = DocumentType.Receipt,
            ClientName = ClientName,
            ClientDocument = ClientDocument,
            ClientPhone = ClientPhone,
            ClientEmail = ClientEmail,
            ClientCity = ClientCity,
            IssueDate = DateTime.UtcNow,
            Notes = Notes,
            OriginBudgetId = Id,
            OriginBudgetNumber = Number,
        };
        receipt.IssueNumber(receiptNumber);
        receipt.Items = Items.Select(item => new DocumentItem { Description = item.Description, Quantity = item.Quantity, UnitPrice = item.UnitPrice, Discount = item.Discount }).ToList();
        receipt.CalculateTotals();
        ConvertedReceiptId = receipt.Id;
        ConvertedReceiptNumber = receipt.Number;
        Status = BudgetStatus.Converted.ToString();
        return receipt;
    }

    public void Delete(Guid deletedBy)
    {
        MarkAsDeleted();
        DeletedAt = DateTime.UtcNow;
        DeletedBy = deletedBy;
        Touch();
    }

    public string GenerateEvidenceHash(string actor, string userAgent)
    {
        var payload = $"{Id}|{PublicToken}|{ClientDecision}|{ClientDecisionAt:o}|{actor}|{userAgent}|{Total}";
        EvidenceHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload)));
        return EvidenceHash;
    }
}
