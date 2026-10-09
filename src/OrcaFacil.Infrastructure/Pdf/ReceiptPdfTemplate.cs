using OrcaFacil.Domain.Entities;
using OrcaFacil.Domain.Enums;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace OrcaFacil.Infrastructure.Pdf;

public class ReceiptPdfTemplate : DocumentPdfTemplate
{
    private static readonly NumberToWordsPtBrService NumberService = new();

    public ReceiptPdfTemplate(Domain.Entities.Document document, IssuerProfile? issuer, PlanType plan, string? languageCode = null, string? currencyCode = "BRL")
        : base(document, issuer, plan, languageCode, currencyCode) { }

    protected override string Title => GetLabel("ReceiptTitle");

    protected override void AddSpecificContent(ColumnDescriptor column)
    {
        var extenso = NumberService.ToCurrencyWords(Document.Total, LanguageCode, CurrencyCode);
        var formattedTotal = FormatCurrency(Document.Total);
        var receiptText = string.Format(GetLabel("ReceiptNotice"), formattedTotal, extenso);

        column.Item().Border(1).BorderColor("#CBD5E1").Background("#F8FAFC").Padding(12).Column(c =>
        {
            c.Item().Text(receiptText).SemiBold();
            if (!string.IsNullOrWhiteSpace(Document.PaymentMethod))
            {
                var methodText = string.Format(GetLabel("PaymentMethod"), Document.PaymentMethod);
                c.Item().PaddingTop(4).Text(methodText).FontSize(9);
            }
        });
    }
}
