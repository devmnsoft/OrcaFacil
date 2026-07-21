using OrcaFacil.Domain.Entities;
using OrcaFacil.Domain.Enums;
using QuestPDF.Fluent;

namespace OrcaFacil.Infrastructure.Pdf;

public class BudgetPdfTemplate : DocumentPdfTemplate
{
    public BudgetPdfTemplate(Document document, IssuerProfile? issuer, PlanType plan) : base(document, issuer, plan) { }

    protected override string Title => "Orçamento";

    protected override void AddSpecificContent(ColumnDescriptor column)
    {
        column.Item().Text("Bloco de aprovação do cliente disponível pelo link público.");
    }
}
