using System.Text.Json;
using OrcaFacil.Domain.Entities;

namespace OrcaFacil.Application.Contracts;

public static class ContractSlaCalculator
{
    public static DateTime CalculateDueAt(DateTime startedAt, int minutes, bool businessHoursOnly, IReadOnlySet<DayOfWeek>? businessDays = null, TimeOnly? start = null, TimeOnly? end = null)
    {
        if (minutes <= 0) throw new ArgumentOutOfRangeException(nameof(minutes));
        if (!businessHoursOnly) return startedAt.AddMinutes(minutes);
        businessDays ??= new HashSet<DayOfWeek> { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday };
        var opens = start ?? new TimeOnly(8, 0); var closes = end ?? new TimeOnly(18, 0);
        if (closes <= opens) throw new ArgumentException("O fim do expediente deve ser posterior ao início.");
        var cursor = startedAt; var remaining = minutes;
        while (remaining > 0)
        {
            if (!businessDays.Contains(cursor.DayOfWeek) || TimeOnly.FromDateTime(cursor) >= closes)
            { cursor = NextOpening(cursor.Date.AddDays(1), businessDays, opens); continue; }
            if (TimeOnly.FromDateTime(cursor) < opens) cursor = cursor.Date.Add(opens.ToTimeSpan());
            var available = (int)Math.Floor((cursor.Date.Add(closes.ToTimeSpan()) - cursor).TotalMinutes);
            if (remaining <= available) return cursor.AddMinutes(remaining);
            remaining -= available; cursor = NextOpening(cursor.Date.AddDays(1), businessDays, opens);
        }
        return cursor;
    }
    private static DateTime NextOpening(DateTime date, IReadOnlySet<DayOfWeek> days, TimeOnly opens) { while (!days.Contains(date.DayOfWeek)) date = date.AddDays(1); return date.Date.Add(opens.ToTimeSpan()); }
}

public static class ContractRecurrenceRules
{
    public static bool CanGenerate(ContractStatus status) => status == ContractStatus.Active;
    public static string Key(Guid accountId, Guid contractId, string kind, DateOnly start, Guid? scheduleId = null) => $"{accountId:N}:{contractId:N}:{scheduleId?.ToString("N") ?? "contract"}:{kind}:{start:yyyyMMdd}";
    public static DateOnly Next(DateOnly date, PreventiveFrequency frequency, int interval = 1) => frequency switch
    { PreventiveFrequency.Weekly => date.AddDays(7 * interval), PreventiveFrequency.Monthly => date.AddMonths(interval), PreventiveFrequency.Bimonthly => date.AddMonths(2 * interval), PreventiveFrequency.Quarterly => date.AddMonths(3 * interval), PreventiveFrequency.Semiannual => date.AddMonths(6 * interval), PreventiveFrequency.Annual => date.AddYears(interval), PreventiveFrequency.Custom => date.AddMonths(interval), _ => throw new ArgumentOutOfRangeException(nameof(frequency)) };
}

public sealed record ContractHealthInput(bool Active, bool PaymentsCurrent, bool SlaMet, bool WorkOrdersCurrent, bool NoCriticalTickets, bool UsageWithinAllowance, bool RenewalCurrent, bool NpsAcceptable, bool HasOwner, bool BillingCurrent);
public sealed record ContractHealthResult(int Score, string Classification, IReadOnlyList<string> PositiveFactors, IReadOnlyList<string> RiskFactors, string NextAction)
{
    public string PositiveFactorsJson => JsonSerializer.Serialize(PositiveFactors);
    public string RiskFactorsJson => JsonSerializer.Serialize(RiskFactors);
}
public static class ContractHealthCalculator
{
    public static ContractHealthResult Calculate(ContractHealthInput input)
    {
        var checks = new (bool ok,string positive,string risk,string action)[] { (input.Active,"Contrato ativo","Contrato não está ativo","Revisar status contratual"), (input.PaymentsCurrent,"Pagamentos em dia","Há pagamentos vencidos","Tratar inadimplência"), (input.SlaMet,"SLA cumprido","Há violação de SLA","Criar plano de recuperação do SLA"), (input.WorkOrdersCurrent,"OS em dia","Há OS atrasadas","Reprogramar OS atrasadas"), (input.NoCriticalTickets,"Sem chamados críticos","Há chamado crítico","Priorizar chamado crítico"), (input.UsageWithinAllowance,"Uso dentro da franquia","Franquia excedida","Revisar franquia com o cliente"), (input.RenewalCurrent,"Renovação em dia","Renovação vencida ou próxima","Iniciar renovação"), (input.NpsAcceptable,"NPS aceitável","NPS abaixo do aceitável","Contatar cliente"), (input.HasOwner,"Responsável definido","Contrato sem responsável","Definir responsável"), (input.BillingCurrent,"Cobranças geradas","Cobrança recorrente pendente","Gerar cobrança pendente") };
        var positives = checks.Where(x=>x.ok).Select(x=>x.positive).ToList(); var risks = checks.Where(x=>!x.ok).Select(x=>x.risk).ToList(); var score = positives.Count * 10;
        var classification = score >= 80 ? "Saudável" : score >= 60 ? "Atenção leve" : score >= 40 ? "Em atenção" : "Crítico";
        return new(score, classification, positives, risks, checks.FirstOrDefault(x=>!x.ok).action ?? "Manter acompanhamento periódico");
    }
}

public static class ContractAdjustmentRules
{
    public static decimal Calculate(decimal currentValue, string type, decimal? percent, decimal? amount)
    {
        var next = type.ToLowerInvariant() switch { "percentual" => currentValue * (1 + (percent ?? 0) / 100m), "valor fixo" => currentValue + (amount ?? 0), "manual" or "índice informado manualmente" => amount ?? currentValue, _ => throw new ArgumentException("Tipo de reajuste inválido.", nameof(type)) };
        if (next < 0) throw new InvalidOperationException("O reajuste não pode resultar em valor negativo."); return decimal.Round(next, 2);
    }
}
