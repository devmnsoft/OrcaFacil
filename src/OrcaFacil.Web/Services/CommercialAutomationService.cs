using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Domain.Entities;
using OrcaFacil.Domain.Enums;
using OrcaFacil.Persistence;

namespace OrcaFacil.Web.Services;

public sealed record RoutineItem(Guid Id, string Kind, string Title, string Client, string Origin, DateTime Date,
    string Priority, string Status, string NextAction, string Page, string? Phone, string? Email);
public sealed record MessageTemplateView(Guid Id, string Code, string Name, string Channel, string? Subject,
    string Body, bool IsActive, bool IsSystem, DateTime CreatedAt, DateTime? UpdatedAt);

public interface ICommercialAutomationService
{
    Task<IReadOnlyList<RoutineItem>> GetRoutineAsync(bool pendingOnly, CancellationToken ct = default);
    Task<IReadOnlyList<MessageTemplateView>> GetTemplatesAsync(CancellationToken ct = default);
    Task<(bool Ok, string Message)> SaveTemplateAsync(Guid? id, string name, string channel, string? subject, string body, bool active, CancellationToken ct = default);
    Task<(bool Ok, string Message)> RecordPreparedMessageAsync(Guid documentId, string channel, CancellationToken ct = default);
}

public sealed partial class CommercialAutomationService(OrcaFacilDbContext db, ICurrentAccountService account,
    ICurrentUserService user) : ICommercialAutomationService
{
    public static readonly string[] Variables = ["ClienteNome", "EmpresaNome", "NumeroOrcamento", "ValorTotal", "Validade", "LinkPublico", "NomeUsuario", "TelefoneEmpresa"];
    private Guid AccountId => account.AccountId ?? throw new InvalidOperationException("Conta ativa não selecionada.");

    public async Task<IReadOnlyList<RoutineItem>> GetRoutineAsync(bool pendingOnly, CancellationToken ct = default)
    {
        await account.EnsureAccountAccessAsync(ct);
        var now = DateTime.UtcNow;
        var documents = await db.Documents.AsNoTracking()
            .Where(x => x.AccountId == AccountId && !x.IsDeleted && x.Type == DocumentType.Budget)
            .OrderBy(x => x.NextFollowUpAt ?? x.ValidUntil ?? x.CreatedAt).ToListAsync(ct);
        var ids = documents.Select(x => x.Id).ToArray();
        var accesses = await db.PublicDocumentAccesses.AsNoTracking()
            .Where(x => x.AccountId == AccountId && ids.Contains(x.DocumentId)).ToListAsync(ct);
        var decisions = await db.PublicDocumentDecisions.AsNoTracking()
            .Where(x => x.AccountId == AccountId && ids.Contains(x.DocumentId)).Select(x => x.DocumentId).Distinct().ToListAsync(ct);
        var orderIds = await db.WorkOrders.AsNoTracking().Where(x => x.AccountId == AccountId && x.SourceDocumentId != null)
            .Select(x => x.SourceDocumentId!.Value).ToListAsync(ct);
        var result = new List<RoutineItem>();
        foreach (var d in documents)
        {
            var access = accesses.Where(x => x.DocumentId == d.Id).OrderByDescending(x => x.LastViewedAt ?? x.CreatedAt).FirstOrDefault();
            var unanswered = access is not null && !decisions.Contains(d.Id) && d.ClientDecision == ClientDecision.Pending;
            if (unanswered)
                result.Add(Item(d, access.LastViewedAt is null ? "sent" : "viewed", access.LastViewedAt is null ? "Proposta enviada sem resposta" : "Proposta visualizada sem resposta", access.LastViewedAt ?? access.CreatedAt, access.LastViewedAt is null ? "Acompanhar envio" : "Retornar ao cliente", access.LastViewedAt is null ? "important" : "critical"));
            if (!pendingOnly && d.NextFollowUpAt is { } follow && d.FollowUpStatus != FollowUpStatus.Completed)
                result.Add(Item(d, follow < now ? "overdue-followup" : "followup", follow < now ? "Follow-up atrasado" : "Follow-up de hoje", follow, follow < now ? "Remarcar ou concluir" : "Registrar retorno", follow < now ? "critical" : "important"));
            if (!pendingOnly && d.Status.Equals("Draft", StringComparison.OrdinalIgnoreCase))
                result.Add(Item(d, "draft", "Orçamento em rascunho", d.CreatedAt, "Concluir orçamento", "normal"));
            if (!pendingOnly && d.Status.Equals("Approved", StringComparison.OrdinalIgnoreCase) && !orderIds.Contains(d.Id))
                result.Add(Item(d, "approved", "Aprovado sem ordem de serviço", d.ClientDecisionAt ?? d.CreatedAt, "Gerar ordem de serviço", "important"));
            if (!pendingOnly && d.ValidUntil is { } valid && d.ClientDecision == ClientDecision.Pending && valid < now)
                result.Add(Item(d, "expired", "Proposta vencida", valid, "Revisar proposta", "critical"));
            else if (!pendingOnly && d.ValidUntil is { } soon && d.ClientDecision == ClientDecision.Pending && soon <= now.AddDays(2))
                result.Add(Item(d, "expiring", "Proposta próxima do vencimento", soon, "Preparar lembrete", "important"));
        }
        return result.OrderBy(x => x.Priority == "critical" ? 0 : x.Priority == "important" ? 1 : 2).ThenBy(x => x.Date).ToArray();
    }

    private static RoutineItem Item(Document d, string kind, string title, DateTime date, string action, string priority) =>
        new(d.Id, kind, title, d.ClientName, $"Orçamento {d.Number}", date, priority, d.Status, action, "/Documents/Details", d.ClientPhone, d.ClientEmail);

    public async Task<IReadOnlyList<MessageTemplateView>> GetTemplatesAsync(CancellationToken ct = default)
    {
        await account.EnsureAccountAccessAsync(ct);
        return await db.CommercialMessageTemplates.AsNoTracking()
            .Where(x => x.AccountId == AccountId || (x.AccountId == null && x.IsSystem))
            .OrderByDescending(x => x.AccountId == AccountId).ThenBy(x => x.Name)
            .Select(x => new MessageTemplateView(x.Id, x.Code, x.Name, x.Channel, x.Subject, x.Body, x.IsActive, x.IsSystem, x.CreatedAt, x.UpdatedAt))
            .ToListAsync(ct);
    }

    public async Task<(bool Ok, string Message)> SaveTemplateAsync(Guid? id, string name, string channel, string? subject, string body, bool active, CancellationToken ct = default)
    {
        await account.EnsureAccountAccessAsync(ct);
        name = name?.Trim() ?? string.Empty; channel = channel?.Trim() ?? string.Empty; body = body?.Trim() ?? string.Empty; subject = subject?.Trim();
        if (name.Length is < 3 or > 140 || body.Length is < 5 or > 4000) return (false, "Informe nome e corpo válidos.");
        if (channel is not ("WhatsApp" or "Email" or "General")) return (false, "Canal inválido.");
        if (subject?.Length > 180) return (false, "O assunto deve ter no máximo 180 caracteres.");
        if (channel == "Email" && string.IsNullOrWhiteSpace(subject)) return (false, "O assunto é obrigatório para e-mail.");
        var content = $"{subject}\n{body}";
        var invalid = VariableRegex().Matches(content).Select(x => x.Groups[1].Value).FirstOrDefault(x => !Variables.Contains(x, StringComparer.Ordinal));
        if (invalid is not null) return (false, $"A variável {{{invalid}}} não é reconhecida. Use uma das variáveis disponíveis.");
        if (content.Count(x => x == '{') != content.Count(x => x == '}')) return (false, "Existe uma variável incompleta. Confira as chaves de abertura e fechamento.");
        CommercialMessageTemplate? template = id is null ? null : await db.CommercialMessageTemplates.SingleOrDefaultAsync(x => x.Id == id && x.AccountId == AccountId && !x.IsSystem, ct);
        if (id is not null && template is null) return (false, "Template não encontrado ou protegido.");
        var created = template is null;
        if (template is null) { template = new() { AccountId = AccountId, Code = $"custom-{Guid.NewGuid():N}", CreatedByUserId = user.UserId }; db.Add(template); }
        template.Name = name; template.Channel = channel; template.Subject = channel == "Email" ? subject : null; template.Body = body; template.IsActive = active;
        if (!created) template.Touch();
        db.ActivityEvents.Add(new ActivityEvent { AccountId = AccountId, ActorUserId = user.UserId, EntityType = nameof(CommercialMessageTemplate), EntityId = template.Id, Action = created ? "MESSAGE_TEMPLATE_CREATED" : "MESSAGE_TEMPLATE_UPDATED", Summary = created ? $"Template '{name}' criado." : $"Template '{name}' atualizado." });
        await db.SaveChangesAsync(ct); return (true, "Template salvo com sucesso.");
    }

    public async Task<(bool Ok, string Message)> RecordPreparedMessageAsync(Guid documentId, string channel, CancellationToken ct = default)
    {
        await account.EnsureAccountAccessAsync(ct);
        var document = await db.Documents.AsNoTracking().SingleOrDefaultAsync(x => x.Id == documentId && x.AccountId == AccountId && !x.IsDeleted, ct);
        if (document is null) return (false, "Orçamento não encontrado.");
        db.ActivityEvents.Add(new ActivityEvent { AccountId = AccountId, EntityId = documentId, ActorUserId = user.UserId, Action = "MESSAGE_PREPARED", Summary = $"Mensagem de {channel} preparada manualmente." });
        await db.SaveChangesAsync(ct); return (true, "Mensagem preparada. Confirme o envio no aplicativo escolhido.");
    }

    [GeneratedRegex(@"\{([^{}]*)\}")]
    private static partial Regex VariableRegex();
}
