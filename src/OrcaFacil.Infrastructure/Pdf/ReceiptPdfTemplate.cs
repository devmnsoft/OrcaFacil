using OrcaFacil.Domain.Entities;
using OrcaFacil.Domain.Enums;
using QuestPDF.Fluent;

namespace OrcaFacil.Infrastructure.Pdf;

public class ReceiptPdfTemplate : DocumentPdfTemplate
{
    public ReceiptPdfTemplate(Domain.Entities.Document document, IssuerProfile? issuer, PlanType plan) : base(document, issuer, plan) { }

    protected override string Title => "Recibo";

    protected override void AddSpecificContent(ColumnDescriptor column)
    {
        column.Item().Text($"Recebemos o valor de {Document.Total:C} referente aos itens descritos.");
    }
}
