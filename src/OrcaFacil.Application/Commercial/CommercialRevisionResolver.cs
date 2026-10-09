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
}
