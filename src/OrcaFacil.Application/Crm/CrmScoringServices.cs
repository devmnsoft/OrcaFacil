namespace OrcaFacil.Application.Crm;

public sealed record HealthScoreInput(bool ActiveRecently, int ApprovedProposals, bool PaymentsCurrent,
    bool ActiveContract, int RepeatPurchases, int? LatestNps, int OpenCriticalTickets,
    int DaysSinceLastPurchase, int DaysSinceLastInteraction);
public sealed record ScoreFactor(string Code, int Points, string Explanation);
public sealed record HealthScoreResult(int Score, string Classification, IReadOnlyList<ScoreFactor> Factors, string RecommendedAction);

public static class ClientHealthScoreService
{
    public static HealthScoreResult Calculate(HealthScoreInput input)
    {
        var factors = new List<ScoreFactor>();
        Add("recent_activity", input.ActiveRecently ? 15 : 0, input.ActiveRecently ? "Interação recente registrada." : "Sem interação recente.");
        Add("approved_proposals", Math.Min(input.ApprovedProposals, 2) * 10, $"{input.ApprovedProposals} proposta(s) aprovada(s).");
        Add("payments", input.PaymentsCurrent ? 15 : -20, input.PaymentsCurrent ? "Pagamentos em dia." : "Há pagamento vencido.");
        Add("active_contract", input.ActiveContract ? 15 : 0, input.ActiveContract ? "Contrato ativo." : "Sem contrato ativo.");
        Add("recurrence", Math.Min(input.RepeatPurchases, 3) * 5, $"{input.RepeatPurchases} recompra(s) registrada(s).");
        Add("nps", input.LatestNps is >= 9 ? 10 : input.LatestNps is <= 6 ? -10 : 0, input.LatestNps is null ? "Sem resposta NPS." : $"NPS mais recente: {input.LatestNps}.");
        Add("critical_support", input.OpenCriticalTickets > 0 ? -15 : 5, input.OpenCriticalTickets > 0 ? "Há chamado crítico aberto." : "Sem chamado crítico aberto.");
        Add("purchase_recency", input.DaysSinceLastPurchase <= 90 ? 10 : -10, $"Última compra há {input.DaysSinceLastPurchase} dia(s).");
        Add("interaction_recency", input.DaysSinceLastInteraction <= 30 ? 10 : -10, $"Última interação há {input.DaysSinceLastInteraction} dia(s).");
        var score = Math.Clamp(35 + factors.Sum(x => x.Points), 0, 100);
        var classification = score >= 80 ? "Saudável" : score >= 60 ? "Atenção leve" : score >= 40 ? "Em atenção" : "Em risco";
        var action = score < 40 ? "Priorizar contato de retenção e resolver pendências abertas." : score < 60 ? "Agendar acompanhamento com o responsável." : "Manter a cadência de relacionamento.";
        return new(score, classification, factors, action);
        void Add(string code, int points, string explanation) => factors.Add(new(code, points, explanation));
    }
}

public sealed record RetentionRiskInput(int DaysSinceInteraction, bool ExpiredProposal, bool OverduePayment,
    int? LatestNps, bool CriticalTicket, int? ContractDaysRemaining, bool DelayedWorkOrder, bool MissedRepurchaseWindow);
public sealed record RetentionRiskResult(Domain.Entities.RetentionRiskLevel Level, IReadOnlyList<string> Reasons, string RecommendedAction);

public static class RetentionRiskService
{
    public static RetentionRiskResult Evaluate(RetentionRiskInput input)
    {
        var reasons = new List<string>(); var weight = 0;
        Add(input.DaysSinceInteraction > 60, 2, $"Sem interação há {input.DaysSinceInteraction} dias.");
        Add(input.ExpiredProposal, 2, "Proposta vencida sem retorno.");
        Add(input.OverduePayment, 3, "Pagamento vencido.");
        Add(input.LatestNps is <= 6, 3, $"NPS detrator ({input.LatestNps}).");
        Add(input.CriticalTicket, 4, "Chamado crítico aberto.");
        Add(input.ContractDaysRemaining is >= 0 and <= 30, 2, $"Contrato vence em {input.ContractDaysRemaining} dias.");
        Add(input.DelayedWorkOrder, 2, "Ordem de serviço atrasada.");
        Add(input.MissedRepurchaseWindow, 2, "Janela esperada de recompra ultrapassada.");
        var level = weight >= 7 ? Domain.Entities.RetentionRiskLevel.Critical : weight >= 4 ? Domain.Entities.RetentionRiskLevel.High : weight >= 2 ? Domain.Entities.RetentionRiskLevel.Medium : Domain.Entities.RetentionRiskLevel.Low;
        return new(level, reasons, level is Domain.Entities.RetentionRiskLevel.Critical ? "Criar tarefa prioritária de retenção para o responsável." : "Acompanhar na próxima rotina de relacionamento.");
        void Add(bool condition, int points, string reason) { if (condition) { weight += points; reasons.Add(reason); } }
    }
}

public static class NpsService
{
    public static decimal? Calculate(IEnumerable<int> responses)
    {
        var values = responses.ToArray();
        if (values.Length == 0) return null;
        if (values.Any(x => x is < 0 or > 10)) throw new ArgumentOutOfRangeException(nameof(responses));
        return Math.Round(100m * (values.Count(x => x >= 9) - values.Count(x => x <= 6)) / values.Length, 1);
    }
}

public static class CampaignConsentService
{
    public static bool CanSendCommercial(bool hasConsent, bool optedOutForCommercial, bool optedOutForChannel) =>
        hasConsent && !optedOutForCommercial && !optedOutForChannel;

    public static bool CanDispatchAutomatically(Domain.Entities.CampaignChannel channel, bool smtpConfigured) =>
        channel switch
        {
            Domain.Entities.CampaignChannel.Email => smtpConfigured,
            Domain.Entities.CampaignChannel.InternalNotification => true,
            Domain.Entities.CampaignChannel.WhatsApp => false,
            _ => false
        };
}
