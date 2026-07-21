using OrcaFacil.Domain.Entities;
using OrcaFacil.Domain.Enums;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace OrcaFacil.Infrastructure.Pdf;

public abstract class DocumentPdfTemplate
{
    protected DocumentPdfTemplate(Document document, IssuerProfile? issuer, PlanType plan)
    {
        Document = document;
        Issuer = issuer;
        Plan = plan;
    }

    protected Document Document { get; }
    protected IssuerProfile? Issuer { get; }
    protected PlanType Plan { get; }
    protected abstract string Title { get; }

    public byte[] Generate() => QuestPDF.Fluent.Document.Create(container =>
    {
        container.Page(page =>
        {
            page.Margin(32);
            page.Header().Text($"OrçaFácil - {Title} {Document.Number}").Bold().FontSize(18);
            page.Content().Column(column =>
            {
                column.Item().Text($"Emitente: {Issuer?.BusinessName ?? "Não informado"}");
                column.Item().Text($"Cliente: {Document.ClientName}");
                foreach (var item in Document.Items)
                {
                    column.Item().Text($"{item.Description} - {item.Quantity:N2} x {item.UnitPrice:C} = {item.CalculateTotal():C}");
                }

                column.Item().Text($"Total: {Document.Total:C}").Bold();
                AddSpecificContent(column);
                if (Plan == PlanType.Free)
                {
                    column.Item().Text("Gerado com OrçaFácil — MNSOFT").FontColor(Colors.Grey.Medium);
                }
            });
            page.Footer().AlignCenter().Text("MNSOFT - comercial@mnsoft.com.br");
        });
    }).GeneratePdf();

    protected virtual void AddSpecificContent(ColumnDescriptor column)
    {
    }
}
