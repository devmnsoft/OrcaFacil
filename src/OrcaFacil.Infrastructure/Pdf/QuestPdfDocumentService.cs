using OrcaFacil.Application.Abstractions;
using OrcaFacil.Domain.Entities;
using OrcaFacil.Domain.Enums;
using QuestPDF.Infrastructure;

namespace OrcaFacil.Infrastructure.Pdf;

public class QuestPdfDocumentService : IPdfService
{
    public Task<byte[]> GenerateDocumentPdfAsync(Document document, IssuerProfile? issuer, PlanType plan, CancellationToken ct = default)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        DocumentPdfTemplate template;
        if (document.Type == DocumentType.Receipt)
            template = new ReceiptPdfTemplate(document, issuer, plan);
        else
            template = new BudgetPdfTemplate(document, issuer, plan);
        return Task.FromResult(template.Generate());
    }
}
