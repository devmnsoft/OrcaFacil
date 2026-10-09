using System.Text.Json;
using OrcaFacil.Application.Common;
using OrcaFacil.Application.Documents;
using OrcaFacil.Application.Localization;
using OrcaFacil.Domain.Entities;
using OrcaFacil.Domain.Enums;

namespace OrcaFacil.Application.Commercial;

public sealed record ResolvedDocumentRevision(
    Document Document,
    IssuerProfile Issuer,
    string LanguageCode,
    string CurrencyCode,
    PlanType EffectivePlan,
    bool IsSnapshotSource,
    string? SnapshotHash
);

public interface ICommercialRevisionResolver
{
    OperationResult<ResolvedDocumentRevision> Resolve(
        Document document,
        DocumentRevision? currentRevision,
        IssuerProfile? defaultIssuer,
        PlanType fallbackPlan,
        string? defaultLanguageCode = null);
}

public sealed class CommercialRevisionResolver : ICommercialRevisionResolver
{
    private const decimal Tolerance = 0.01m;
    private static readonly DocumentSnapshotSerializer SnapshotSerializer = new();

    public OperationResult<ResolvedDocumentRevision> Resolve(
        Document document,
        DocumentRevision? currentRevision,
        IssuerProfile? defaultIssuer,
        PlanType fallbackPlan,
        string? defaultLanguageCode = null)
    {
        ArgumentNullException.ThrowIfNull(document);

        // Se há revisão emitida para este documento, ela É a fonte da verdade imutável
        if (currentRevision is not null)
        {
            if (currentRevision.DocumentId != Guid.Empty && document.Id != Guid.Empty && currentRevision.DocumentId != document.Id)
            {
                return OperationResult<ResolvedDocumentRevision>.Failure(
                    "InvalidSnapshot",
                    $"A revisão {currentRevision.VersionNumber} não pertence ao documento informado.");
            }

            if (currentRevision.AccountId != Guid.Empty && document.AccountId.HasValue && currentRevision.AccountId != document.AccountId.Value)
            {
                return OperationResult<ResolvedDocumentRevision>.Failure(
                    "InvalidSnapshot",
                    $"A revisão {currentRevision.VersionNumber} não pertence à conta do documento informado.");
            }

            if (string.IsNullOrWhiteSpace(currentRevision.ProtectedSnapshot))
            {
                return OperationResult<ResolvedDocumentRevision>.Failure(
                    "InvalidSnapshot",
                    $"O snapshot protegido da revisão {currentRevision.VersionNumber} está corrompido ou ausente. Não é seguro emitir o documento com dados divergentes.");
            }

            DocumentSnapshot? snapshot;
            try
            {
                snapshot = JsonSerializer.Deserialize<DocumentSnapshot>(
                    currentRevision.ProtectedSnapshot,
                    new JsonSerializerOptions(JsonSerializerDefaults.Web));
            }
            catch (JsonException ex)
            {
                return OperationResult<ResolvedDocumentRevision>.Failure(
                    "InvalidSnapshot",
                    $"Falha ao desserializar o snapshot da revisão {currentRevision.VersionNumber}: {ex.Message}. Recuperação administrativa necessária.");
            }

            if (snapshot is null)
            {
                return OperationResult<ResolvedDocumentRevision>.Failure(
                    "InvalidSnapshot",
                    $"O snapshot da revisão {currentRevision.VersionNumber} resultou em conteúdo nulo.");
            }

            var validation = ValidateSnapshot(snapshot, currentRevision);
            if (!validation.Succeeded)
                return OperationResult<ResolvedDocumentRevision>.Failure(validation.Code!, validation.Message ?? "Snapshot comercial inválido.");

            var reconstructedDoc = ReconstructDocument(snapshot, document.AccountId);
            var reconstructedIssuer = ReconstructIssuer(snapshot.Issuer);
            var planType = snapshot.Quote.ShowPlatformBrand ? PlanType.Free : PlanType.Professional;
            var languageCode = string.IsNullOrWhiteSpace(snapshot.Quote.LanguageCode) ? "pt-BR" : SupportedLocales.Normalize(snapshot.Quote.LanguageCode);
            var currencyCode = string.IsNullOrWhiteSpace(snapshot.Quote.CurrencyCode) ? "BRL" : snapshot.Quote.CurrencyCode.Trim().ToUpperInvariant();

            return OperationResult<ResolvedDocumentRevision>.Success(
                new ResolvedDocumentRevision(reconstructedDoc, reconstructedIssuer, languageCode, currencyCode, planType, true, currentRevision.SnapshotHash));
        }

        // Caso seja rascunho sem revisão congelada:
        var lang = string.IsNullOrWhiteSpace(defaultLanguageCode) ? "pt-BR" : SupportedLocales.Normalize(defaultLanguageCode);
        var activeIssuer = defaultIssuer ?? new IssuerProfile { BusinessName = "OrçaFácil" };
        return OperationResult<ResolvedDocumentRevision>.Success(
            new ResolvedDocumentRevision(document, activeIssuer, lang, "BRL", fallbackPlan, false, null));
    }

    private static Document ReconstructDocument(DocumentSnapshot snapshot, Guid? accountId)
    {
        var doc = new Document
        {
            AccountId = accountId,
            Type = DocumentType.Budget,
            Status = "Sent",
            ClientName = snapshot.Customer.Name,
            ClientDocument = snapshot.Customer.Document,
            ClientPhone = snapshot.Customer.Phone,
            ClientEmail = snapshot.Customer.Email,
            ClientCity = snapshot.Customer.City,
            IssueDate = snapshot.Quote.IssueDate,
            ValidUntil = snapshot.Quote.ValidUntil,
            EstimatedDuration = snapshot.Quote.DeliveryTime,
            PaymentMethod = snapshot.Quote.Payment,
            ConditionsText = snapshot.Quote.Conditions,
            WarrantyText = snapshot.Quote.Footer,
            Notes = snapshot.Quote.Notes,
            Discount = snapshot.Quote.Discount,
            TemplateCode = string.IsNullOrWhiteSpace(snapshot.Quote.Template) ? "essential" : snapshot.Quote.Template
        };

        doc.IssueNumber(snapshot.Quote.Number);

        doc.Items = snapshot.Items.Select((item, index) => new DocumentItem
        {
            Description = item.Description,
            Unit = string.IsNullOrWhiteSpace(item.Unit) ? "un" : item.Unit,
            Quantity = item.Quantity,
            UnitPrice = item.UnitPrice,
            Discount = item.Discount,
            SortOrder = index
        }).ToList();

        doc.CalculateTotals();
        return doc;
    }

    private static IssuerProfile ReconstructIssuer(IssuerSnapshot issuer) => new()
    {
        BusinessName = issuer.Name,
        DocumentNumber = issuer.Document,
        Email = issuer.Email,
        Phone = issuer.Phone,
        Address = issuer.Address,
        City = issuer.City,
        PixKey = issuer.Pix,
        LogoPath = issuer.Logo
    };

    private static OperationResult ValidateSnapshot(DocumentSnapshot snapshot, DocumentRevision revision)
    {
        if (snapshot.Issuer is null)
            return OperationResult.Failure("InvalidSnapshot", $"A revisão {revision.VersionNumber} não possui emitente no snapshot.");
        if (snapshot.Customer is null)
            return OperationResult.Failure("InvalidSnapshot", $"A revisão {revision.VersionNumber} não possui cliente no snapshot.");
        if (snapshot.Quote is null)
            return OperationResult.Failure("InvalidSnapshot", $"A revisão {revision.VersionNumber} não possui dados comerciais no snapshot.");
        if (snapshot.Items is null || snapshot.Items.Count == 0)
            return OperationResult.Failure("InvalidSnapshot", $"A revisão {revision.VersionNumber} não possui itens comerciais no snapshot.");

        if (string.IsNullOrWhiteSpace(snapshot.Issuer.Name))
            return OperationResult.Failure("InvalidSnapshot", $"A revisão {revision.VersionNumber} possui emitente sem nome.");
        if (string.IsNullOrWhiteSpace(snapshot.Customer.Name))
            return OperationResult.Failure("InvalidSnapshot", $"A revisão {revision.VersionNumber} possui cliente sem nome.");
        if (string.IsNullOrWhiteSpace(snapshot.Quote.Number))
            return OperationResult.Failure("InvalidSnapshot", $"A revisão {revision.VersionNumber} possui número do documento vazio.");
        if (string.IsNullOrWhiteSpace(snapshot.Quote.Template))
            return OperationResult.Failure("InvalidSnapshot", $"A revisão {revision.VersionNumber} possui template vazio.");

        var normalizedLanguage = SupportedLocales.Normalize(snapshot.Quote.LanguageCode);
        if (!string.Equals(normalizedLanguage, snapshot.Quote.LanguageCode, StringComparison.Ordinal))
            return OperationResult.Failure("InvalidSnapshot", $"A revisão {revision.VersionNumber} possui idioma inválido.");
        if (!string.Equals(snapshot.Quote.CurrencyCode?.Trim(), "BRL", StringComparison.OrdinalIgnoreCase))
            return OperationResult.Failure("InvalidSnapshot", $"A revisão {revision.VersionNumber} possui moeda inválida para a operação atual.");

        var subtotal = 0m;
        var itemsTotal = 0m;
        foreach (var item in snapshot.Items)
        {
            if (item is null)
                return OperationResult.Failure("InvalidSnapshot", $"A revisão {revision.VersionNumber} possui item nulo.");
            if (string.IsNullOrWhiteSpace(item.Description))
                return OperationResult.Failure("InvalidSnapshot", $"A revisão {revision.VersionNumber} possui item sem descrição.");
            if (item.Quantity <= 0m)
                return OperationResult.Failure("InvalidSnapshot", $"A revisão {revision.VersionNumber} possui item com quantidade inválida.");
            if (item.UnitPrice < 0m || item.Discount < 0m)
                return OperationResult.Failure("InvalidSnapshot", $"A revisão {revision.VersionNumber} possui valor negativo em item.");

            var expectedSubtotal = CommercialCalculator.Round(item.Quantity * item.UnitPrice);
            var expectedItemTotal = CommercialCalculator.Round(expectedSubtotal - item.Discount);
            if (expectedItemTotal < 0m)
                return OperationResult.Failure("InvalidSnapshot", $"A revisão {revision.VersionNumber} possui desconto de item maior que o subtotal.");
            if (!SameMoney(item.Subtotal, expectedSubtotal) || !SameMoney(item.Total, expectedItemTotal))
                return OperationResult.Failure("InvalidSnapshot", $"A revisão {revision.VersionNumber} possui totais inconsistentes em item.");

            subtotal += expectedSubtotal;
            itemsTotal += expectedItemTotal;
        }

        subtotal = CommercialCalculator.Round(subtotal);
        itemsTotal = CommercialCalculator.Round(itemsTotal);
        if (snapshot.Quote.Discount < 0m)
            return OperationResult.Failure("InvalidSnapshot", $"A revisão {revision.VersionNumber} possui desconto global negativo.");
        if (!SameMoney(snapshot.Quote.Subtotal, itemsTotal))
            return OperationResult.Failure("InvalidSnapshot", $"A revisão {revision.VersionNumber} possui subtotal inconsistente.");

        var expectedTotal = CommercialCalculator.Round(itemsTotal - snapshot.Quote.Discount);
        if (expectedTotal < 0m)
            return OperationResult.Failure("InvalidSnapshot", $"A revisão {revision.VersionNumber} possui desconto global maior que o total dos itens.");
        if (!SameMoney(snapshot.Quote.Total, expectedTotal) || (revision.Total > 0m && !SameMoney(revision.Total, snapshot.Quote.Total)))
            return OperationResult.Failure("InvalidSnapshot", $"A revisão {revision.VersionNumber} possui total comercial inconsistente.");

        if (!string.IsNullOrWhiteSpace(revision.SnapshotHash))
        {
            var canonical = SnapshotSerializer.Serialize(snapshot);
            if (!string.Equals(canonical.Hash, revision.SnapshotHash, StringComparison.OrdinalIgnoreCase))
                return OperationResult.Failure("InvalidSnapshot", $"A revisão {revision.VersionNumber} não confere com o hash protegido.");
        }

        return OperationResult.Success();
    }

    private static bool SameMoney(decimal left, decimal right) =>
        Math.Abs(CommercialCalculator.Round(left) - CommercialCalculator.Round(right)) <= Tolerance;
}
