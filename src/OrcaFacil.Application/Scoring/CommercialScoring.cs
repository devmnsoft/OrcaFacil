namespace OrcaFacil.Application.Scoring;

public sealed record ScoreResult(int Value, string Classification, IReadOnlyList<string> Reasons)
{
    public string Explanation => string.Join(" ", Reasons);
}

public sealed record QuoteScoreInput(decimal Value, bool CustomerApprovedBefore, bool Viewed,
    int DaysSinceSent, int FollowUps, int? DaysUntilExpiry, bool HasOverdueReceivable);

public interface IQuoteScoreService { ScoreResult Calculate(QuoteScoreInput input); }

/// <summary>Deterministic, explainable commercial prioritisation. It never predicts or uses random data.</summary>
public sealed class QuoteScoreService : IQuoteScoreService
{
    public ScoreResult Calculate(QuoteScoreInput x)
    {
        var score = 20;
        var reasons = new List<string> { "Base comercial: 20 pontos." };
        Add(x.Value >= 10_000, 15, "Proposta de alto valor: +15 pontos.");
        Add(x.CustomerApprovedBefore, 20, "Cliente já aprovou proposta: +20 pontos.");
        Add(x.Viewed, 20, "Proposta visualizada: +20 pontos.");
        Add(x.DaysSinceSent is >= 1 and <= 7, 10, "Envio recente: +10 pontos.");
        Add(x.FollowUps is >= 1 and <= 3, 5, "Follow-up registrado: +5 pontos.");
        Add(x.DaysUntilExpiry is >= 0 and <= 3, 10, "Vencimento próximo: +10 pontos.");
        Add(x.DaysUntilExpiry < 0, -25, "Proposta vencida: -25 pontos.");
        Add(x.HasOverdueReceivable, -25, "Cliente possui recebível vencido: -25 pontos.");
        score = Math.Clamp(score, 0, 100);
        return new(score, score >= 80 ? "Alta prioridade" : score >= 50 ? "Média prioridade" : score >= 20 ? "Baixa prioridade" : "Atenção ou baixa chance", reasons);

        void Add(bool condition, int points, string reason) { if (!condition) return; score += points; reasons.Add(reason); }
    }
}

public sealed record ClientScoreInput(decimal Sold, decimal Received, int Quotes, int ApprovedQuotes,
    decimal Overdue, int DaysSinceInteraction, int ActiveContracts);
public interface IClientScoreService { ScoreResult Calculate(ClientScoreInput input); }

public sealed class ClientScoreService : IClientScoreService
{
    public ScoreResult Calculate(ClientScoreInput x)
    {
        if (x.Overdue > 0) return new(10, "Cliente inadimplente", ["Há recebíveis vencidos em aberto."]);
        if (x.Quotes == 0) return new(30, "Cliente novo", ["Ainda não há propostas para este cliente."]);
        if (x.DaysSinceInteraction >= 30) return new(20, "Cliente inativo", [$"Última interação há {x.DaysSinceInteraction} dias."]);
        var rate = x.ApprovedQuotes * 100m / x.Quotes;
        var score = Math.Clamp(25 + (x.ActiveContracts > 0 ? 25 : 0) + (rate >= 60 ? 25 : 0) + (x.Received >= 10_000 ? 25 : 0), 0, 100);
        var classification = score >= 80 ? "Cliente estratégico" : x.ActiveContracts > 0 || x.ApprovedQuotes > 1 ? "Cliente recorrente" : "Cliente em atenção";
        return new(score, classification, [$"Taxa de aprovação: {rate:0.#}%.", $"{x.ActiveContracts} contrato(s) ativo(s).", $"Recebido: {x.Received:C}."]);
    }
}
