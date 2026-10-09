using OrcaFacil.Domain.Entities;
using OrcaFacil.Domain.Enums;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace OrcaFacil.Infrastructure.Pdf;

public class ReceiptPdfTemplate : DocumentPdfTemplate
{
    private static readonly NumberToWordsPtBrService NumberService = new();

    public ReceiptPdfTemplate(Domain.Entities.Document document, IssuerProfile? issuer, PlanType plan) : base(document, issuer, plan) { }

    protected override string Title => "Recibo";

    protected override void AddSpecificContent(ColumnDescriptor column)
    {
        var extenso = NumberService.ToCurrencyWords(Document.Total);
        column.Item().Border(1).BorderColor("#CBD5E1").Background("#F8FAFC").Padding(12).Column(c =>
        {
            c.Item().Text($"Recebemos a quantia de {Document.Total:C} ({extenso}) referente aos serviços e itens discriminados neste documento.").SemiBold();
            if (!string.IsNullOrWhiteSpace(Document.PaymentMethod))
            {
                c.Item().PaddingTop(4).Text($"Forma de quitação / recebimento: {Document.PaymentMethod}").FontSize(9);
            }
        });
    }
}
