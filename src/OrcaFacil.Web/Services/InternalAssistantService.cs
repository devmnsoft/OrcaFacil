using Microsoft.EntityFrameworkCore;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Application.Security;
using OrcaFacil.Persistence;

namespace OrcaFacil.Web.Services;

public interface IInternalAssistantService
{
    Task<AssistantAnswer> AskAsync(string question, CancellationToken ct = default);
}
public sealed record AssistantAnswer(string Answer, string SourceLabel, IReadOnlyList<AssistantLink> Links);
public sealed record AssistantLink(string Label, string Url);

/// <summary>A read-only, deterministic assistant. It never invokes mutations or claims to be an AI provider.</summary>
public sealed class InternalAssistantService(ICurrentAccountService account, OrcaFacilDbContext db) : IInternalAssistantService
{
    public async Task<AssistantAnswer> AskAsync(string question, CancellationToken ct = default)
    {
        if (account.AccountId is not Guid accountId || !await account.HasPermissionAsync(PermissionCodes.AssistantUse, ct))
            throw new UnauthorizedAccessException("Você não tem acesso ao assistente interno.");
        var normalized = question.Trim().ToLowerInvariant();
        if (normalized.Length < 3) return Rule("Descreva em poucas palavras o que você precisa encontrar ou aprender.", "/Help", "Abrir ajuda");

        if (normalized.Contains("como ") || normalized.Contains("configur"))
        {
            var terms = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries).Where(x => x.Length > 3).Take(5).ToArray();
            var term = terms.FirstOrDefault() ?? normalized;
            var articles = await db.KnowledgeBaseArticles.AsNoTracking().Where(x => x.IsPublished && !x.IsDeleted && (x.Title.ToLower().Contains(term) || x.Summary.ToLower().Contains(term))).OrderBy(x => x.DisplayOrder).Take(3).Select(x => new AssistantLink(x.Title, "/Help/Article/" + x.Slug)).ToListAsync(ct);
            return articles.Count > 0
                ? new("Encontrei orientações publicadas na base de conhecimento. Abra o artigo mais próximo da sua dúvida; nenhuma ação será executada automaticamente.", Source, articles)
                : Rule("Não encontrei um artigo publicado para essa dúvida. Consulte a central de ajuda ou abra um chamado; não vou inventar uma instrução.", "/Help", "Abrir central de ajuda");
        }
        if (normalized.Contains("proposta") && (normalized.Contains("sem resposta") || normalized.Contains("aguard")) && await account.HasPermissionAsync(PermissionCodes.DocumentsView, ct))
        {
            var count = await db.Documents.AsNoTracking().CountAsync(x => x.AccountId == accountId && !x.IsDeleted && (x.Status == "Sent" || x.Status == "Viewed"), ct);
            return Rule($"Há {count} proposta(s) enviada(s) ou visualizada(s) que merecem acompanhamento nesta conta.", "/CommercialPipeline/Index", "Ver pipeline");
        }
        if (normalized.Contains("os") && (normalized.Contains("atras") || normalized.Contains("hoje")) && await account.HasPermissionAsync(PermissionCodes.WorkOrdersView, ct))
        {
            var today = DateTime.UtcNow.Date;
            var count = await db.WorkOrders.AsNoTracking().CountAsync(x => x.AccountId == accountId && !x.IsDeleted && x.ScheduledStart.HasValue && x.ScheduledStart.Value < today && x.CompletedAt == null && x.CancelledAt == null, ct);
            return Rule($"Há {count} ordem(ns) agendada(s) antes de hoje nesta conta. Confirme o status na agenda antes de agir.", "/Schedule/Index", "Abrir agenda");
        }
        if (normalized.Contains("recibo") && normalized.Contains("pendent") && await account.HasPermissionAsync(PermissionCodes.ReceiptsView, ct))
        {
            var count = await db.ManualPayments.AsNoTracking().CountAsync(x => x.AccountId == accountId && !x.IsDeleted && !db.Receipts.Any(r => r.AccountId == accountId && !r.IsDeleted && r.PaymentId == x.Id), ct);
            return Rule($"Há {count} pagamento(s) manual(is) sem recibo localizado nesta conta.", "/Receipts/Index", "Revisar recibos");
        }
        return Rule("Posso orientar sobre o uso do OrçaFácil e consultar pendências permitidas, mas não encontrei uma regra segura para essa pergunta. Use a busca global ou a central de ajuda.", "/Search", "Abrir busca global", new("Central de ajuda", "/Help"));
    }
    private const string Source = "Resposta baseada nas regras do OrçaFácil";
    private static AssistantAnswer Rule(string answer, string url, string label, params AssistantLink[] extra) => new(answer, Source, [new(label,url), ..extra]);
}
