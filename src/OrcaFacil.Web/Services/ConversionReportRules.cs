namespace OrcaFacil.Web.Services;

/// <summary>Um orçamento aprovado projetado em forma leve para o cálculo puro da conversão (sem DbContext).</summary>
public sealed record ApprovedBudgetRow(
    Guid Id,
    string ClientName,
    decimal Total,
    bool ConvertedToWorkOrder,
    bool ConvertedToReceipt,
    decimal Received,
    DateTime CreatedAt,
    DateTime? ClientDecisionAt,
    DateTime? LastStageAt);

/// <summary>
/// Regras puras do relatório "Conversão comercial". A camada Web apenas consulta os dados; toda a regra de
/// conversão (taxa, tempo até converter e linhas por cliente) vive aqui para ser unit-testável como as demais.
/// </summary>
public static class ConversionReportRules
{
    /// <summary>O orçamento converteu quando virou ordem de serviço OU recibo (qualquer um dos dois).</summary>
    public static bool IsConverted(ApprovedBudgetRow row) => row.ConvertedToWorkOrder || row.ConvertedToReceipt;

    /// <summary>Reconhece o status histórico "Converted" e o atual "ConvertedToWorkOrder" (sem distinguir maiúsculas).</summary>
    public static bool IsConvertedStatus(string? status) =>
        string.Equals(status, "Converted", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(status, "ConvertedToWorkOrder", StringComparison.OrdinalIgnoreCase);

    /// <summary>Taxa de conversão em percentual; -1 indica dados insuficientes (nenhum orçamento aprovado).</summary>
    public static decimal ConversionRate(IReadOnlyList<ApprovedBudgetRow> rows) =>
        rows.Count == 0 ? -1m : rows.Count(IsConverted) * 100m / rows.Count;

    /// <summary>Média de dias entre a decisão do cliente e a etapa de conversão; -1 quando nada converteu.</summary>
    public static decimal AverageConversionDays(IReadOnlyList<ApprovedBudgetRow> rows)
    {
        var spans = rows
            .Where(IsConverted)
            .Select(row =>
            {
                var baseline = row.ClientDecisionAt ?? row.CreatedAt;
                var atStage = row.LastStageAt ?? row.CreatedAt;
                return Math.Max(0d, (atStage - baseline).TotalDays);
            })
            .ToList();

        return spans.Count == 0 ? -1m : (decimal)spans.Average();
    }

    /// <summary>Métricas do cabeçalho do relatório no estilo da casa (-1 vira "Dados insuficientes" na view).</summary>
    public static List<Metric> BuildMetrics(IReadOnlyList<ApprovedBudgetRow> rows)
    {
        var converted = rows.Where(IsConverted).ToList();

        return
        [
            new("Propostas aprovadas", rows.Count),
            new("Valor aprovado", rows.Sum(x => x.Total), true),
            new("Convertidas em OS", rows.Count(x => x.ConvertedToWorkOrder)),
            new("Convertidas em recibo", rows.Count(x => x.ConvertedToReceipt)),
            new("Valor convertido", converted.Sum(x => x.Total), true),
            new("Taxa de conversão", ConversionRate(rows), false, "%"),
            new("Valor recebido das aprovadas", rows.Sum(x => x.Received), true),
            new("Tempo médio até converter", AverageConversionDays(rows), false, " dias")
        ];
    }

    /// <summary>Linhas por cliente: aprovações, valor aprovado, valor convertido, recebido e taxa individual.</summary>
    public static ReportRow[] BuildClientRows(IReadOnlyList<ApprovedBudgetRow> rows) =>
        rows
            .GroupBy(x => x.ClientName)
            .Select(group =>
            {
                var list = group.ToList();
                return new ReportRow(group.Key, list.Count, list.Sum(x => x.Total), list.Where(IsConverted).Sum(x => x.Total), list.Sum(x => x.Received), ConversionRate(list));
            })
            .OrderByDescending(x => x.Approved)
            .ThenByDescending(x => x.Received)
            .ToArray();
}
