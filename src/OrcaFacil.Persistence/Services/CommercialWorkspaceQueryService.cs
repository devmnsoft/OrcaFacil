using Microsoft.EntityFrameworkCore;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Application.Commercial;
using OrcaFacil.Domain.Entities;
using OrcaFacil.Domain.Enums;

namespace OrcaFacil.Persistence.Services;

public sealed class CommercialWorkspaceQueryService(OrcaFacilDbContext db, ICurrentAccountService currentAccount)
    : ICommercialWorkspaceQueryService
{
    private Guid AccountId => currentAccount.AccountId ?? throw new InvalidOperationException("Conta ativa não selecionada.");

    public async Task<CommercialDocumentWorkspaceView?> GetAsync(Guid documentId, CancellationToken ct = default)
    {
        await currentAccount.EnsureAccountAccessAsync(ct);
        var document = await db.Documents.Include(x => x.Items).AsNoTracking().SingleOrDefaultAsync(
            x => x.Id == documentId && x.AccountId == AccountId && !x.IsDeleted, ct);
        if (document is null) return null;

        var revisions = await db.DocumentRevisions.AsNoTracking().Where(x => x.AccountId == AccountId && x.DocumentId == documentId)
            .OrderByDescending(x => x.VersionNumber).ToListAsync(ct);
        var accesses = await db.PublicDocumentAccesses.AsNoTracking().Where(x => x.AccountId == AccountId && x.DocumentId == documentId)
            .OrderByDescending(x => x.CreatedAt).ToListAsync(ct);
        var decisions = await db.PublicDocumentDecisions.AsNoTracking().Where(x => x.AccountId == AccountId && x.DocumentId == documentId)
            .OrderByDescending(x => x.CreatedAt).ToListAsync(ct);
        var order = await db.WorkOrders.AsNoTracking().SingleOrDefaultAsync(x => x.AccountId == AccountId && x.SourceDocumentId == documentId && !x.IsDeleted, ct);
        List<ManualPayment> payments = order is null ? [] : await db.ManualPayments.AsNoTracking().Where(x => x.AccountId == AccountId && x.WorkOrderId == order.Id && !x.IsDeleted)
            .OrderByDescending(x => x.PaidAt).ToListAsync(ct);
        List<Receipt> receipts = order is null ? [] : await db.Receipts.AsNoTracking().Where(x => x.AccountId == AccountId && x.WorkOrderId == order.Id && !x.IsDeleted)
            .OrderByDescending(x => x.IssuedAt).ToListAsync(ct);
        var entityIds = new List<Guid> { documentId };
        if (order is not null) entityIds.Add(order.Id);
        entityIds.AddRange(payments.Select(x => x.Id)); entityIds.AddRange(receipts.Select(x => x.Id));
        var events = await db.ActivityEvents.AsNoTracking().Where(x => x.AccountId == AccountId && x.EntityId != null && entityIds.Contains(x.EntityId.Value))
            .OrderByDescending(x => x.CreatedAt).ToListAsync(ct);

        var access = accesses.FirstOrDefault(); var decision = decisions.FirstOrDefault(); var now = DateTime.UtcNow;
        var engagement = access is null ? null : new ClientEngagementView(
            access.RevokedAt is not null || access.Status == PublicAccessStatus.Revoked ? "Revogado" : access.ExpiresAt <= now ? "Expirado" : "Ativo",
            access.CreatedAt, access.ExpiresAt, access.ViewCount, access.LastViewedAt, decision?.Decision.ToString(), decision?.CreatedAt,
            decision?.CustomerName, decision?.Comment ?? decision?.ReasonCode);
        var paid = payments.Where(x => x.Status == FinancialRecordStatus.Active).Sum(x => x.Amount);
        var orderView = order is null ? null : new CommercialWorkOrderView(order.Id, order.Number, order.Status.ToString(),
            order.ScheduledStart, order.ScheduledEnd, paid, Math.Max(0, order.TotalSnapshot - paid), payments.FirstOrDefault()?.Id, receipts.FirstOrDefault()?.Id);
        var hasChange = decisions.Any(x => x.Decision == PublicDocumentDecisionType.ChangeRequested);
        var next = ResolveNext(document, engagement, orderView, hasChange);
        return new(document.Id, document.Type.ToString(), document.Number, document.Status, document.ClientName, document.CreatedAt,
            document.IssueDate, document.ValidUntil, document.Subtotal, document.Discount, document.Total, document.Notes,
            document.ConditionsText, document.OriginBudgetNumber is null ? "Criação direta" : $"Documento {document.OriginBudgetNumber}",
            revisions.FirstOrDefault(x => x.IsCurrent)?.VersionNumber ?? 0, document.ValidUntil < now, hasChange,
            document.Items.Select(x => new CommercialWorkspaceItem(x.Id, x.Description, x.Quantity, x.UnitPrice, x.Discount,
                x.Quantity * x.UnitPrice, x.CalculateTotal())).ToArray(),
            revisions.Select(x => new CommercialRevisionView(x.Id, x.VersionNumber, x.Status.ToString(), x.CreatedAt, x.IsCurrent)).ToArray(),
            engagement, events.Select(MapEvent).ToArray(), orderView, next, document.NextFollowUpAt,
            document.LastFollowUpAt, document.FollowUpStatus, document.FollowUpNote);
    }

    public async Task<CommercialDashboardView> GetDashboardAsync(CancellationToken ct = default)
    {
        await currentAccount.EnsureAccountAccessAsync(ct);
        var docs = await db.Documents.AsNoTracking().Where(x => x.AccountId == AccountId && !x.IsDeleted && x.Type == DocumentType.Budget)
            .OrderByDescending(x => x.CreatedAt).ToListAsync(ct);
        var ids = docs.Select(x => x.Id).ToArray();
        var access = await db.PublicDocumentAccesses.AsNoTracking().Where(x => x.AccountId == AccountId && ids.Contains(x.DocumentId)).ToListAsync(ct);
        var decisions = await db.PublicDocumentDecisions.AsNoTracking().Where(x => x.AccountId == AccountId && ids.Contains(x.DocumentId)).ToListAsync(ct);
        var orderDocIds = await db.WorkOrders.AsNoTracking().Where(x => x.AccountId == AccountId && x.SourceDocumentId != null && ids.Contains(x.SourceDocumentId.Value)).Select(x => x.SourceDocumentId!.Value).ToListAsync(ct);
        var paidDocumentIds = await (from payment in db.ManualPayments.AsNoTracking()
            join order in db.WorkOrders.AsNoTracking() on payment.WorkOrderId equals (Guid?)order.Id
            where payment.AccountId == AccountId && !payment.IsDeleted && payment.Status == FinancialRecordStatus.Active
                && order.SourceDocumentId != null && ids.Contains(order.SourceDocumentId.Value)
            group payment by order.SourceDocumentId!.Value into payments
            where payments.Sum(x => x.Amount) > 0
            select payments.Key).ToListAsync(ct);
        var groups = new[] {
            ("lead", "Lead", Array.Empty<string>()),
            ("client", "Cliente cadastrado", Array.Empty<string>()),
            ("draft", "Orçamento em rascunho", new[] { "Draft" }),
            ("ready", "Proposta pronta", new[] { "Ready", "Issued" }),
            ("sent", "Proposta enviada", new[] { "Sent" }),
            ("viewed", "Visualizada", new[] { "Viewed" }),
            ("change", "Em negociação", new[] { "InNegotiation", "ChangeRequested" }),
            ("approved", "Aprovada", new[] { "Approved" }),
            ("converted", "Convertida em OS", new[] { "Converted", "ConvertedToWorkOrder" }),
            ("paid", "Paga", Array.Empty<string>()),
            ("lost", "Perdida", new[] { "Rejected", "Expired" }) };
        var columns = groups.Select(group => {
            var rows = group.Item1 == "paid"
                ? docs.Where(x => paidDocumentIds.Contains(x.Id)).ToArray()
                : docs.Where(x => !paidDocumentIds.Contains(x.Id) && group.Item3.Contains(x.Status, StringComparer.OrdinalIgnoreCase)).ToArray();
            return new CommercialPipelineColumn(group.Item1, group.Item2, rows.Length, rows.Sum(x => x.Total), rows.Select(x => {
                var a = access.Where(y => y.DocumentId == x.Id).OrderByDescending(y => y.LastViewedAt ?? y.CreatedAt).FirstOrDefault();
                var last = a?.LastViewedAt ?? a?.CreatedAt ?? x.CreatedAt;
                return new CommercialPipelineCard(x.Id, x.Number, x.ClientName, x.Total, x.CreatedAt, last, x.ValidUntil, Context(group.Item1, last, x.ValidUntil));
            }).ToArray());
        }).ToArray();
        var attention = new List<CommercialAttentionItem>(); var now = DateTime.UtcNow;
        foreach (var d in docs) {
            var decision = decisions.Where(x => x.DocumentId == d.Id).OrderByDescending(x => x.CreatedAt).FirstOrDefault();
            if (d.NextFollowUpAt is { } followUp && followUp < now)
                attention.Add(new(d.Id, "critical", "Retorno atrasado", $"Orçamento {d.Number} · {d.ClientName}", followUp));
            else if (d.NextFollowUpAt is { } today && today.Date == now.Date)
                attention.Add(new(d.Id, "important", "Retorno hoje", $"Falar com {d.ClientName}", today));
            else if (decision?.Decision == PublicDocumentDecisionType.ChangeRequested)
                attention.Add(new(d.Id, "critical", "Cliente pediu alteração", decision.Comment ?? decision.ReasonCode ?? "Revise a proposta e crie uma nova versão.", decision.CreatedAt));
            else if (d.Status.Equals("Approved", StringComparison.OrdinalIgnoreCase) && !orderDocIds.Contains(d.Id))
                attention.Add(new(d.Id, "important", "Orçamento aprovado sem ordem de serviço", "Converta a aprovação em execução sem duplicar o trabalho.", decision?.CreatedAt ?? d.CreatedAt));
            else if (d.ValidUntil is { } valid && valid >= now && valid <= now.AddDays(2))
                attention.Add(new(d.Id, "warning", "Proposta perto do vencimento", $"A validade termina em {valid.ToLocalTime():dd/MM}.", valid));
        }
        var approved = docs.Where(x => x.Status.Equals("Approved", StringComparison.OrdinalIgnoreCase) || x.Status.Equals("ConvertedToWorkOrder", StringComparison.OrdinalIgnoreCase)).ToArray();
        var responded = decisions.Select(x => x.DocumentId).Distinct().Count();
        return new(columns, attention.OrderBy(x => x.Severity).ThenByDescending(x => x.OccurredAt).Take(5).ToArray(),
            docs.Count(x => x.Status.Equals("Sent", StringComparison.OrdinalIgnoreCase)), docs.Count(x => x.Status.Equals("Viewed", StringComparison.OrdinalIgnoreCase)),
            approved.Length, approved.Sum(x => x.Total), responded == 0 ? null : decimal.Round(approved.Length * 100m / responded, 1),
            approved.Length == 0 ? null : approved.Average(x => x.Total));
    }

    private static CommercialNextAction ResolveNext(Document d, ClientEngagementView? engagement, CommercialWorkOrderView? order, bool change)
    {
        if (change) return new("revision", "Transforme o retorno em uma nova versão", "Preserve a resposta do cliente e prepare os ajustes solicitados.", "Criar nova versão", "Revision", null, null, "version");
        if (order is not null) return order.Status switch {
            "Planned" => new("schedule", "Agende a execução", "Defina data e responsável para tirar o serviço do planejamento.", "Abrir ordem de serviço", null, "/WorkOrders/Details", order.Id, "calendar"),
            "Scheduled" => new("start", "Prepare o início do serviço", "A ordem está agendada e pronta para execução.", "Abrir ordem de serviço", null, "/WorkOrders/Details", order.Id, "start"),
            "InProgress" => new("complete", "Conclua a execução", "Registre a conclusão na ordem de serviço.", "Abrir ordem de serviço", null, "/WorkOrders/Details", order.Id, "success"),
            "Completed" when order.Balance > 0 => new("payment", order.Paid > 0 ? "Registre o saldo restante" : "Registre o pagamento", $"Saldo atual de {order.Balance:C}.", "Abrir ordem de serviço", null, "/WorkOrders/Details", order.Id, "payment"),
            _ when order.LatestReceiptId is not null => new("receipt", "Recibo disponível", "O pagamento e o recibo estão registrados.", "Visualizar recibo", null, "/Receipts/Details", order.LatestReceiptId, "receipt"),
            _ => new("receipt", "Emita o recibo", "O pagamento foi concluído e pode ser formalizado.", "Abrir ordem de serviço", null, "/WorkOrders/Details", order.Id, "receipt") };
        if (d.Status.Equals("Approved", StringComparison.OrdinalIgnoreCase)) return new("work-order", "Converta a aprovação em operação", "Crie uma ordem rastreável a partir da versão aprovada.", "Criar ordem de serviço", "WorkOrder", null, null, "work-order");
        if (d.Status.Equals("Rejected", StringComparison.OrdinalIgnoreCase)) return new("duplicate", "Reorganize a proposta", "Preserve o histórico e use o conteúdo em uma nova negociação.", "Duplicar proposta", "Duplicate", null, null, "copy-link");
        if (d.Status.Equals("Sent", StringComparison.OrdinalIgnoreCase) || d.Status.Equals("Viewed", StringComparison.OrdinalIgnoreCase))
            return new("follow-up", d.Status.Equals("Viewed", StringComparison.OrdinalIgnoreCase) ? "Acompanhe a resposta do cliente" : "Acompanhe o envio", engagement?.LastViewedAt is null ? "O link está disponível para o cliente." : "O cliente já abriu a proposta.", "Ver interação", null, null, null, "share");
        return new("prepare", "Edite e prepare para envio", "Confira itens, condições e validade antes de compartilhar.", "Editar orçamento", null, "/Documents/Edit", d.Id, "document");
    }

    private static CommercialTimelineEvent MapEvent(ActivityEvent e) => e.Action switch {
        "QuoteViewed" => new(e.Action, "Cliente abriu a proposta", e.Summary, e.CreatedAt, "Cliente", "info", "quote-viewed"),
        "QuoteApproved" => new(e.Action, "Cliente aprovou", e.Summary, e.CreatedAt, "Cliente", "success", "success"),
        "QuoteChangeRequested" => new(e.Action, "Cliente pediu alteração", e.Summary, e.CreatedAt, "Cliente", "warning", "document"),
        "QuoteRejected" => new(e.Action, "Cliente recusou", e.Summary, e.CreatedAt, "Cliente", "danger", "close"),
        "QuoteSent" => new(e.Action, "Link de aprovação criado", e.Summary, e.CreatedAt, "Equipe", "info", "share"),
        "QuoteRevisionCreated" => new(e.Action, "Nova revisão criada", e.Summary, e.CreatedAt, "Equipe", "neutral", "version"),
        "WorkOrderCreated" => new(e.Action, "Ordem de serviço criada", e.Summary, e.CreatedAt, "Equipe", "success", "work-order"),
        "PaymentRegistered" => new(e.Action, "Pagamento registrado", e.Summary, e.CreatedAt, "Equipe", "success", "payment"),
        "ReceiptGenerated" => new(e.Action, "Recibo emitido", e.Summary, e.CreatedAt, "Equipe", "success", "receipt"),
        "FOLLOW_UP_SCHEDULED" => new(e.Action, "Retorno agendado", e.Summary, e.CreatedAt, "Equipe", "info", "calendar"),
        "FOLLOW_UP_SNOOZED" => new(e.Action, "Retorno adiado", e.Summary, e.CreatedAt, "Equipe", "warning", "clock"),
        "FOLLOW_UP_COMPLETED" => new(e.Action, "Acompanhamento concluído", e.Summary, e.CreatedAt, "Equipe", "success", "success"),
        "MESSAGE_PREPARED" => new(e.Action, "Mensagem preparada", e.Summary, e.CreatedAt, "Equipe", "info", "share"),
        _ => new(e.Action, e.Summary ?? e.Action, null, e.CreatedAt, e.ActorUserId is null ? "Sistema" : "Equipe", "neutral", "audit") };
    private static string Context(string code, DateTime last, DateTime? valid) {
        if (valid is { } date && date > DateTime.UtcNow && date <= DateTime.UtcNow.AddDays(1)) return "Validade vence amanhã";
        var age = DateTime.UtcNow - last;
        if (code == "viewed") return age.TotalHours < 24 ? $"Visto há {Math.Max(1, (int)age.TotalHours)}h" : $"Visto há {(int)age.TotalDays} dias";
        return age.TotalDays < 1 ? "Atualizado hoje" : $"Aguardando há {(int)age.TotalDays} dias";
    }
}
