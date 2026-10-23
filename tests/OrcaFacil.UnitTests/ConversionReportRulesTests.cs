using OrcaFacil.Web.Services;
using Xunit;

namespace OrcaFacil.UnitTests;

public sealed class ConversionReportRulesTests
{
    private static readonly DateTime Created = new(2026, 1, 10, 9, 0, 0, DateTimeKind.Utc);

    private static ApprovedBudgetRow Row(string client, decimal total, bool workOrder = false, bool receipt = false, decimal received = 0m, DateTime? decision = null, DateTime? stage = null) =>
        new(Guid.NewGuid(), client, total, workOrder, receipt, received, Created, decision, stage);

    private static Metric MetricOf(IReadOnlyList<Metric> metrics, string label) => Assert.Single(metrics, x => x.Label == label);

    [Theory]
    [InlineData(false, false)] // nenhum caminho de conversão
    [InlineData(true, false)]  // virou OS
    [InlineData(false, true)]  // virou recibo
    [InlineData(true, true)]   // ambos os caminhos
    public void IsConverted_counts_workorder_or_receipt(bool workOrder, bool receipt)
    {
        var row = Row("Cliente", 100m, workOrder, receipt);

        Assert.Equal(workOrder || receipt, ConversionReportRules.IsConverted(row));
    }

    [Theory]
    [InlineData("Converted", true)]
    [InlineData("ConvertedToWorkOrder", true)]
    [InlineData("convertedtoworkorder", true)]
    [InlineData("Approved", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsConvertedStatus_recognizes_legacy_and_current_os_statuses(string? status, bool expected)
    {
        Assert.Equal(expected, ConversionReportRules.IsConvertedStatus(status));
    }

    [Fact]
    public void ConversionRate_returns_minus_one_without_approved_budgets()
    {
        Assert.Equal(-1m, ConversionReportRules.ConversionRate([]));
    }

    [Fact]
    public void ConversionRate_counts_each_converted_budget_only_once()
    {
        IReadOnlyList<ApprovedBudgetRow> rows =
        [
            Row("A", 10m, workOrder: true),
            Row("B", 20m, receipt: true),
            Row("C", 30m, workOrder: true, receipt: true),
            Row("D", 40m)
        ];

        Assert.Equal(75m, ConversionReportRules.ConversionRate(rows));
    }

    [Fact]
    public void AverageConversionDays_returns_minus_one_when_nothing_converted()
    {
        IReadOnlyList<ApprovedBudgetRow> rows = [Row("A", 10m), Row("B", 20m, workOrder: false)];

        Assert.Equal(-1m, ConversionReportRules.AverageConversionDays(rows));
    }

    [Fact]
    public void AverageConversionDays_uses_decision_to_stage_span_with_fallbacks()
    {
        IReadOnlyList<ApprovedBudgetRow> rows =
        [
            // sem decisão nem etapa registradas cai no baseline de criação (0 dia)
            Row("A", 10m, workOrder: true),
            // decisão 1 dia após criação e conversão 4 dias após: 3 dias
            Row("B", 20m, receipt: true, decision: Created.AddDays(1), stage: Created.AddDays(4))
        ];

        Assert.Equal(1.5m, ConversionReportRules.AverageConversionDays(rows));
    }

    [Fact]
    public void AverageConversionDays_never_goes_negative_when_stage_precedes_decision()
    {
        IReadOnlyList<ApprovedBudgetRow> rows = [Row("A", 10m, workOrder: true, decision: Created.AddDays(3), stage: Created.AddDays(1))];

        Assert.Equal(0m, ConversionReportRules.AverageConversionDays(rows));
    }

    [Fact]
    public void BuildMetrics_reports_the_full_conversion_state()
    {
        IReadOnlyList<ApprovedBudgetRow> rows =
        [
            Row("Alfa", 100m, workOrder: true, received: 30m, decision: Created.AddHours(2), stage: Created.AddDays(3).AddHours(2)),
            Row("Beta", 50m)
        ];

        var metrics = ConversionReportRules.BuildMetrics(rows);

        Assert.Equal(2, MetricOf(metrics, "Propostas aprovadas").Value);
        Assert.True(MetricOf(metrics, "Valor aprovado").IsMoney);
        Assert.Equal(150m, MetricOf(metrics, "Valor aprovado").Value);
        Assert.Equal(1, MetricOf(metrics, "Convertidas em OS").Value);
        Assert.Equal(0, MetricOf(metrics, "Convertidas em recibo").Value);
        Assert.Equal(100m, MetricOf(metrics, "Valor convertido").Value);
        Assert.Equal("%", MetricOf(metrics, "Taxa de conversão").Suffix);
        Assert.Equal(50m, MetricOf(metrics, "Taxa de conversão").Value);
        Assert.Equal(30m, MetricOf(metrics, "Valor recebido das aprovadas").Value);
        Assert.Equal(3m, MetricOf(metrics, "Tempo médio até converter").Value);
    }

    [Fact]
    public void BuildClientRows_groups_by_client_orders_by_converted_value()
    {
        IReadOnlyList<ApprovedBudgetRow> rows =
        [
            Row("Beta", 60m, workOrder: true, received: 40m),
            Row("Beta", 10m),
            Row("Alfa", 200m, receipt: true, received: 100m),
            Row("Alfa", 80m)
        ];

        var reportRows = ConversionReportRules.BuildClientRows(rows);

        Assert.Equal(["Alfa", "Beta"], reportRows.Select(x => x.Label).ToArray());
        var alfa = reportRows[0];
        Assert.Equal(2, alfa.Count);
        Assert.Equal(280m, alfa.Proposed);
        Assert.Equal(200m, alfa.Approved);
        Assert.Equal(100m, alfa.Received);
        Assert.Equal(50m, alfa.Extra);
        var beta = reportRows[1];
        Assert.Equal(70m, beta.Proposed);
        Assert.Equal(60m, beta.Approved);
        Assert.Equal(50m, beta.Extra);
    }
}
