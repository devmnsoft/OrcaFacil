using Microsoft.EntityFrameworkCore;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Domain.Entities;
using OrcaFacil.Domain.Enums;
using OrcaFacil.Persistence;

namespace OrcaFacil.Web.Services;

public interface IRecommendationService
{
    Task<IReadOnlyList<RecommendationCard>> GetOpenAsync(CancellationToken ct = default);
    Task GenerateAsync(CancellationToken ct = default);
}

/// <summary>Transparent account-scoped rules. Generation is idempotent through natural entity keys and a DB unique index.</summary>
public sealed class RecommendationService(ICurrentAccountService current, OrcaFacilDbContext db) : IRecommendationService
{
    private Guid AccountId => current.AccountId ?? throw new UnauthorizedAccessException("Selecione uma conta.");
    public async Task<IReadOnlyList<RecommendationCard>> GetOpenAsync(CancellationToken ct = default)
    {
        await GenerateAsync(ct);
        return await db.RecommendationCards.AsNoTracking().Where(x => x.AccountId == AccountId && !x.IsDeleted && x.Status == "Open")
            .OrderBy(x => x.Priority == "Critical" ? 0 : x.Priority == "High" ? 1 : x.Priority == "Medium" ? 2 : 3).ThenBy(x => x.CreatedAt).ToListAsync(ct);
    }

    public async Task GenerateAsync(CancellationToken ct = default)
    {
        var accountId = AccountId; var now = DateTime.UtcNow;
        var open = await db.RecommendationCards.Where(x => x.AccountId == accountId && !x.IsDeleted && x.Status == "Open").ToListAsync(ct);
        var docs = await db.Documents.AsNoTracking().Where(x => x.AccountId == accountId && !x.IsDeleted && x.Type == DocumentType.Budget).ToListAsync(ct);
        var accesses = await db.PublicDocumentAccesses.AsNoTracking().Where(x => x.AccountId == accountId && !x.IsDeleted).ToListAsync(ct);
        var orders = await db.WorkOrders.AsNoTracking().Where(x => x.AccountId == accountId && !x.IsDeleted).ToListAsync(ct);
        foreach (var doc in docs)
        {
            var access = accesses.Where(x => x.DocumentId == doc.Id).OrderByDescending(x => x.CreatedAt).FirstOrDefault();
            if (doc.ClientDecision == ClientDecision.Approved && !orders.Any(x => x.SourceDocumentId == doc.Id))
                Add("GenerateWorkOrder", "High", "Gere a ordem de serviço", "A proposta foi aprovada e ainda não possui OS.", "Gerar OS", $"/WorkOrders/Create?documentId={doc.Id}", doc);
            else if (doc.ClientDecision == ClientDecision.Pending && access?.LastViewedAt is { } viewedAt && viewedAt < now.AddDays(-1))
                Add("FollowUpProposal", "High", "Retome a proposta visualizada", "O cliente visualizou a proposta há mais de um dia e ainda não respondeu.", "Registrar contato", $"/Documents/Details?id={doc.Id}", doc);
            else if (doc.ClientDecision == ClientDecision.Pending && access is { LastViewedAt: null } && access.CreatedAt < now.AddDays(-2))
                Add("FollowUpProposal", "Medium", "Faça follow-up da proposta", "A proposta foi enviada há mais de dois dias e ainda não foi visualizada.", "Abrir proposta", $"/Documents/Details?id={doc.Id}", doc);
        }
        foreach (var order in orders.Where(x => x.Status == WorkOrderStatus.Completed && !x.PaymentReceived))
            Add("RegisterPayment", "Critical", "Registre o pagamento da OS", "A ordem de serviço foi concluída, mas o pagamento ainda não foi registrado.", "Registrar pagamento", $"/Payments/Create?workOrderId={order.Id}", null, order);

        foreach (var card in open)
        {
            var valid = card.Type switch { "GenerateWorkOrder" => !orders.Any(x => x.SourceDocumentId == card.DocumentId), "RegisterPayment" => orders.Any(x => x.Id == card.WorkOrderId && !x.PaymentReceived), _ => docs.Any(x => x.Id == card.DocumentId && x.ClientDecision == ClientDecision.Pending) };
            if (!valid) { card.Status = "Resolved"; card.ResolvedAt = now; card.Touch(); }
        }
        await db.SaveChangesAsync(ct);

        void Add(string type, string priority, string title, string reason, string label, string url, Document? doc = null, WorkOrder? order = null)
        {
            if (open.Any(x => x.Type == type && x.DocumentId == doc?.Id && x.WorkOrderId == order?.Id)) return;
            var card = new RecommendationCard { AccountId = accountId, ClientId = doc?.ClientId ?? order?.ClientId, DocumentId = doc?.Id, WorkOrderId = order?.Id, Type = type, Priority = priority, Title = title, Description = reason, Reason = reason, ActionLabel = label, ActionUrl = url };
            db.RecommendationCards.Add(card); open.Add(card);
        }
    }
}
