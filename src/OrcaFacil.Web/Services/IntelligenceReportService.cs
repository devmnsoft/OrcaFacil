using System;
using Microsoft.EntityFrameworkCore;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Domain.Enums;
using OrcaFacil.Persistence;

namespace OrcaFacil.Web.Services;

public sealed record ReportFilter(DateTime? From, DateTime? To, Guid? ClientId = null, string? Status = null, string? PaymentMethod = null);
public sealed record Metric(string Label, decimal Value, bool IsMoney = false, string? Suffix = null);
public sealed record ReportRow(string Label, int Count, decimal Proposed, decimal Approved, decimal Received, decimal? Extra = null);
public sealed record IntelligenceReport(string Title, IReadOnlyList<Metric> Metrics, IReadOnlyList<ReportRow> Rows);

public interface IIntelligenceReportService
{
    Task<IntelligenceReport> CommercialFunnelAsync(ReportFilter filter, CancellationToken ct);
    Task<IntelligenceReport> FinancialAsync(ReportFilter filter, CancellationToken ct);
    Task<IntelligenceReport> ClientsAsync(ReportFilter filter, CancellationToken ct);
    Task<IntelligenceReport> ServicesAsync(ReportFilter filter, CancellationToken ct);
}

/// <summary>Account-scoped reporting queries. The account boundary always comes from the authenticated principal.</summary>
public sealed class IntelligenceReportService(ICurrentAccountService account, OrcaFacilDbContext db) : IIntelligenceReportService
{
    private Guid AccountId => account.AccountId ?? throw new UnauthorizedAccessException("Selecione uma conta para consultar relatórios.");
    // The upper bound is exclusive so every instant of the selected final day is included,
    // independently of the precision used by the database provider.
    private static (DateTime From, DateTime To) Period(ReportFilter filter)
    {
        var from = DateTime.SpecifyKind(filter.From?.Date ?? DateTime.UtcNow.Date.AddMonths(-1), DateTimeKind.Utc);
        var to = DateTime.SpecifyKind(filter.To?.Date.AddDays(1) ?? DateTime.UtcNow.Date.AddDays(1), DateTimeKind.Utc);

        if (from >= to)
        {
            throw new ArgumentException("A data inicial deve ser anterior ou igual à data final.", nameof(filter));
        }

        return (from, to);
    }

    public async Task<IntelligenceReport> CommercialFunnelAsync(ReportFilter f, CancellationToken ct)
    {
        var (from, to) = Period(f);
        var accountId = AccountId;
        var query = db.Documents.AsNoTracking().Where(x => x.AccountId == accountId && !x.IsDeleted && x.Type == DocumentType.Budget && x.CreatedAt >= from && x.CreatedAt < to);
        if (f.ClientId is { } clientId) query = query.Where(x => x.ClientId == clientId);
        if (!string.IsNullOrWhiteSpace(f.Status)) query = query.Where(x => x.Status == f.Status);
        var documents = await query.Select(x => new { x.Status, x.Total, x.ClientDecision, x.CreatedAt, x.UpdatedAt }).ToListAsync(ct);
        var now = DateTime.UtcNow;
        var rows = documents.GroupBy(x => NormalizeStage(x.Status, x.ClientDecision)).Select(g => new ReportRow(g.Key, g.Count(), g.Sum(x => x.Total), g.Where(x => x.ClientDecision == ClientDecision.Approved).Sum(x => x.Total), 0, (decimal)g.Average(x => Math.Max(0, (now - (x.UpdatedAt ?? x.CreatedAt)).TotalDays)))).OrderBy(x => StageOrder(x.Label)).ToArray();
        var decided = documents.Count(x => x.ClientDecision is ClientDecision.Approved or ClientDecision.Rejected);
        var approved = documents.Count(x => x.ClientDecision == ClientDecision.Approved);
        return new("Funil comercial", [new("Propostas", documents.Count), new("Valor proposto", documents.Sum(x => x.Total), true), new("Valor aprovado", documents.Where(x => x.ClientDecision == ClientDecision.Approved).Sum(x => x.Total), true), new("Taxa de aprovação", decided == 0 ? -1 : approved * 100m / decided, false, "%")], rows);
    }

    public async Task<IntelligenceReport> FinancialAsync(ReportFilter f, CancellationToken ct)
    {
        var (from, to) = Period(f);
        var accountId = AccountId;
        var hasPaymentStatus = Enum.TryParse<FinancialRecordStatus>(f.Status, true, out var paymentStatus);
        var documents = db.Documents.AsNoTracking().Where(x => x.AccountId == accountId && !x.IsDeleted && x.Type == DocumentType.Budget && x.CreatedAt >= from && x.CreatedAt < to);
        if (f.ClientId is { } documentClientId) documents = documents.Where(x => x.ClientId == documentClientId);
        if (!string.IsNullOrWhiteSpace(f.Status) && !hasPaymentStatus) documents = documents.Where(x => x.Status == f.Status);
        var proposed = await documents.SumAsync(x => x.Total, ct);
        var approved = await documents.Where(x => x.ClientDecision == ClientDecision.Approved).SumAsync(x => x.Total, ct);
        var payments = db.ManualPayments.AsNoTracking().Where(x => x.AccountId == accountId && !x.IsDeleted && x.PaidAt >= from && x.PaidAt < to);
        if (f.ClientId is { } clientId) payments = payments.Where(x => x.ClientId == clientId);
        if (!string.IsNullOrWhiteSpace(f.PaymentMethod)) payments = payments.Where(x => x.PaymentMethod == f.PaymentMethod);
        if (hasPaymentStatus) payments = payments.Where(x => x.Status == paymentStatus);
        var paymentRows = await payments.GroupBy(x => x.PaymentMethod).Select(g => new ReportRow(g.Key, g.Count(), 0, 0, g.Where(x => x.Status == FinancialRecordStatus.Active).Sum(x => x.Amount), g.Where(x => x.Status == FinancialRecordStatus.Reversed).Sum(x => x.Amount))).ToListAsync(ct);
        var received = paymentRows.Sum(x => x.Received); var reversed = paymentRows.Sum(x => x.Extra ?? 0);
        var receipts = await db.Receipts.CountAsync(x => x.AccountId == accountId && !x.IsDeleted && x.IssuedAt >= from && x.IssuedAt < to, ct);
        return new("Financeiro", [new("Valor proposto", proposed, true), new("Valor aprovado", approved, true), new("Valor recebido", received, true), new("Saldo pendente", Math.Max(0, approved - received), true), new("Pagamentos revertidos", reversed, true), new("Recibos emitidos", receipts)], paymentRows);
    }

    public async Task<IntelligenceReport> ClientsAsync(ReportFilter f, CancellationToken ct)
    {
        var (from, to) = Period(f);
        var accountId = AccountId;
        var clientQuery = db.Clients.AsNoTracking().Where(x => x.AccountId == accountId && !x.IsDeleted);
        if (f.ClientId is { } clientId) clientQuery = clientQuery.Where(x => x.Id == clientId);
        var clients = await clientQuery.Select(x => new { x.Id, x.Name, x.IsActive, x.LastInteractionAt }).ToListAsync(ct);
        var documentQuery = db.Documents.AsNoTracking().Where(x => x.AccountId == accountId && !x.IsDeleted && x.ClientId != null && x.CreatedAt >= from && x.CreatedAt < to);
        if (f.ClientId is { } documentClientId) documentQuery = documentQuery.Where(x => x.ClientId == documentClientId);
        if (!string.IsNullOrWhiteSpace(f.Status)) documentQuery = documentQuery.Where(x => x.Status == f.Status);
        var documentsByClient = await documentQuery.GroupBy(x => x.ClientId!.Value).Select(g => new
        {
            ClientId = g.Key,
            Count = g.Count(),
            Proposed = g.Sum(x => x.Total),
            Approved = g.Where(x => x.ClientDecision == ClientDecision.Approved).Sum(x => x.Total)
        }).ToDictionaryAsync(x => x.ClientId, ct);
        var paymentQuery = db.ManualPayments.AsNoTracking().Where(x => x.AccountId == accountId && !x.IsDeleted && x.Status == FinancialRecordStatus.Active && x.PaidAt >= from && x.PaidAt < to);
        if (f.ClientId is { } paymentClientId) paymentQuery = paymentQuery.Where(x => x.ClientId == paymentClientId);
        if (!string.IsNullOrWhiteSpace(f.PaymentMethod)) paymentQuery = paymentQuery.Where(x => x.PaymentMethod == f.PaymentMethod);
        var paid = await paymentQuery.GroupBy(x => x.ClientId).Select(g => new { ClientId = g.Key, Total = g.Sum(x => x.Amount) }).ToDictionaryAsync(x => x.ClientId, x => x.Total, ct);
        var rows = clients.Select(c =>
        {
            documentsByClient.TryGetValue(c.Id, out var activity);
            return new ReportRow(c.Name, activity?.Count ?? 0, activity?.Proposed ?? 0, activity?.Approved ?? 0, paid.GetValueOrDefault(c.Id), c.LastInteractionAt is null ? null : (decimal)Math.Max(0, (DateTime.UtcNow - c.LastInteractionAt.Value).TotalDays));
        }).OrderByDescending(x => x.Proposed).ToArray();
        return new("Clientes", [new("Cadastrados", clients.Count), new("Ativos", clients.Count(x => x.IsActive)), new("Sem movimentação (30 dias)", clients.Count(x => x.LastInteractionAt == null || x.LastInteractionAt < DateTime.UtcNow.AddDays(-30))), new("Com propostas aprovadas", rows.Count(x => x.Approved > 0))], rows);
    }

    public async Task<IntelligenceReport> ServicesAsync(ReportFilter f, CancellationToken ct)
    {
        var (from, to) = Period(f);
        var accountId = AccountId;
        var services = await db.ServiceCatalogItems.AsNoTracking().Where(x => x.AccountId == accountId && !x.IsDeleted).Select(x => new { x.Id, x.Name }).ToListAsync(ct);
        var canViewMargin = account.AccountRoleCode is "Owner" or "Administrator";
        var itemQuery = db.DocumentItems
            .AsNoTracking()
            .Join(
                db.Documents.AsNoTracking(),
                item => item.DocumentId,
                document => document.Id,
                (item, document) => new { Item = item, Document = document })
            .Where(x => x.Document.AccountId == accountId)
            .Where(x => !x.Document.IsDeleted)
            .Where(x => !x.Item.IsDeleted)
            .Where(x => x.Document.Type == DocumentType.Budget)
            .Where(x => x.Document.CreatedAt >= from)
            .Where(x => x.Document.CreatedAt < to)
            .Select(x => new
            {
                x.Item.ServiceCatalogItemId,
                x.Item.Quantity,
                x.Item.UnitPrice,
                x.Item.Discount,
                x.Item.EstimatedCostSnapshot,
                x.Document.ClientId,
                x.Document.Status,
                x.Document.ClientDecision
            });
        if (f.ClientId is { } clientId) itemQuery = itemQuery.Where(x => x.ClientId == clientId);
        if (!string.IsNullOrWhiteSpace(f.Status)) itemQuery = itemQuery.Where(x => x.Status == f.Status);
        var items = await itemQuery.ToListAsync(ct);
        var itemsByService = items.Where(x => x.ServiceCatalogItemId.HasValue).ToLookup(x => x.ServiceCatalogItemId!.Value);
        var rows = services.Select(s =>
        {
            var used = itemsByService[s.Id].ToArray();
            var total = used.Sum(x => Math.Max(0, x.Quantity * x.UnitPrice - x.Discount));
            var approvedItems = used.Where(x => x.ClientDecision == ClientDecision.Approved).ToArray();
            var approved = approvedItems.Sum(x => Math.Max(0, x.Quantity * x.UnitPrice - x.Discount));
            var hasCompleteCostData = approvedItems.Length > 0 && approvedItems.All(x => x.EstimatedCostSnapshot > 0);
            decimal? margin = null;

            if (canViewMargin && hasCompleteCostData)
            {
                margin = approved - approvedItems.Sum(x => x.Quantity * x.EstimatedCostSnapshot);
            }

            return new ReportRow(s.Name, used.Length, total, approved, 0, margin);
        }).OrderByDescending(x => x.Approved).ToArray();
        return new("Serviços", [new("Serviços cadastrados", services.Count), new("Nunca usados", rows.Count(x => x.Count == 0)), new("Valor vendido", rows.Sum(x => x.Approved), true), new("Itens em propostas", rows.Sum(x => x.Count))], rows);
    }

    private static string NormalizeStage(string status, ClientDecision decision)
    {
        if (decision == ClientDecision.Approved)
        {
            return "Aprovado";
        }

        if (decision == ClientDecision.Rejected)
        {
            return "Recusado";
        }

        if (string.IsNullOrWhiteSpace(status))
        {
            return "Rascunho";
        }

        switch (status.Trim())
        {
            case "Draft":
                return "Rascunho";

            case "Ready":
            case "Issued":
                return "Pronto";

            case "Sent":
                return "Enviado";

            case "Viewed":
                return "Visualizado";

            case "InNegotiation":
            case "ChangeRequested":
                return "Em negociação";

            case "Approved":
                return "Aprovado";

            case "Rejected":
                return "Recusado";

            case "Expired":
                return "Expirado";

            case "Converted":
            case "ConvertedToWorkOrder":
                return "Convertido em OS";

            default:
                return status;
        }
    }

    private static int StageOrder(string stage)
    {
        var stages = new[]
        {
            "Rascunho",
            "Pronto",
            "Enviado",
            "Visualizado",
            "Em negociação",
            "Aprovado",
            "Recusado",
            "Expirado",
            "Convertido em OS"
        };

        var index = Array.IndexOf(stages, stage);

        return index >= 0 ? index : 99;
    }
}
