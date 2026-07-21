using OrcaFacil.Domain.Entities;
using OrcaFacil.Domain.Enums;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Document = OrcaFacil.Domain.Entities.Document;

namespace OrcaFacil.Infrastructure.Pdf;

public abstract class DocumentPdfTemplate
{
    private const string Primary = "#1E3A5F";
    private const string Accent = "#2D7DD2";
    private const string Success = "#1F9D6B";

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
            page.Margin(34);
            page.DefaultTextStyle(x => x.FontSize(10).FontColor("#1C2430"));
            page.Header().Background(Primary).Padding(18).Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("OrçaFácil").FontColor(Colors.White).Bold().FontSize(20);
                    col.Item().Text($"{Title} {Document.Number}").FontColor("#E9F3FF").FontSize(11);
                });
                row.ConstantItem(150).AlignRight().Text(Document.IssueDate.ToString("dd/MM/yyyy")).FontColor(Colors.White).SemiBold();
            });
            page.Content().PaddingVertical(18).Column(column =>
            {
                column.Spacing(12);
                column.Item().Row(row =>
                {
                    row.RelativeItem().Border(1).BorderColor("#E2E8F0").Padding(12).Column(col =>
                    {
                        col.Item().Text("Emitente").FontColor(Accent).Bold();
                        col.Item().Text(Issuer?.BusinessName ?? "Não informado").Bold();
                        col.Item().Text(Issuer?.DocumentNumber ?? string.Empty);
                        col.Item().Text(Issuer?.Email ?? string.Empty);
                    });
                    row.ConstantItem(16);
                    row.RelativeItem().Border(1).BorderColor("#E2E8F0").Padding(12).Column(col =>
                    {
                        col.Item().Text("Cliente").FontColor(Accent).Bold();
                        col.Item().Text(Document.ClientName).Bold();
                        col.Item().Text(Document.ClientDocument ?? string.Empty);
                        col.Item().Text(Document.ClientEmail ?? string.Empty);
                    });
                });
                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(c => { c.RelativeColumn(4); c.RelativeColumn(); c.RelativeColumn(); c.RelativeColumn(); });
                    table.Header(h => { h.Cell().Text("Descrição").Bold(); h.Cell().AlignRight().Text("Qtd.").Bold(); h.Cell().AlignRight().Text("Unitário").Bold(); h.Cell().AlignRight().Text("Total").Bold(); });
                    foreach (var item in Document.Items)
                    {
                        table.Cell().PaddingVertical(4).Text(item.Description);
                        table.Cell().PaddingVertical(4).AlignRight().Text(item.Quantity.ToString("N2"));
                        table.Cell().PaddingVertical(4).AlignRight().Text(item.UnitPrice.ToString("C"));
                        table.Cell().PaddingVertical(4).AlignRight().Text(item.CalculateTotal().ToString("C"));
                    }
                });
                column.Item().AlignRight().Background("#E9F7F1").Padding(12).Text($"Total: {Document.Total:C}").FontColor(Success).Bold().FontSize(18);
                if (!string.IsNullOrWhiteSpace(Document.Notes)) column.Item().Text(Document.Notes);
                AddSpecificContent(column);
                if (Plan == PlanType.Free) column.Item().AlignCenter().Text("Gerado com OrçaFácil — MNSOFT").FontColor(Colors.Grey.Medium).FontSize(9);
            });
            page.Footer().AlignCenter().Text("MNSOFT • OrçaFácil • comercial@mnsoft.com.br").FontColor(Primary);
        });
    }).GeneratePdf();

    protected virtual void AddSpecificContent(ColumnDescriptor column) { }
}
