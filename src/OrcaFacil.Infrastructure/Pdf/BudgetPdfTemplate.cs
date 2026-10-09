using OrcaFacil.Domain.Entities;
using OrcaFacil.Domain.Enums;
using QuestPDF.Fluent;

namespace OrcaFacil.Infrastructure.Pdf;

public class BudgetPdfTemplate : DocumentPdfTemplate
{
    public BudgetPdfTemplate(Domain.Entities.Document document, IssuerProfile? issuer, PlanType plan, string? languageCode = null, string? currencyCode = "BRL")
        : base(document, issuer, plan, languageCode, currencyCode) { }

    protected override string Title => GetLabel("QuoteTitle");

    protected override void AddSpecificContent(ColumnDescriptor column)
    {
        column.Item().Text(GetLabel("PublicApprovalNotice"));
    }
}
