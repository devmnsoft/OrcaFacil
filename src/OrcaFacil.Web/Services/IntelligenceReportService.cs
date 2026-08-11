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
    private DateTime From(ReportFilter f) => (f.From ?? DateTime.UtcNow.Date.AddMonths(-1)).ToUniversalTime();
    private DateTime To(ReportFilter f) => (f.To?.Date.AddDays(1) ?? DateTime.UtcNow.Date.AddDays(1)).ToUniversalTime();

    public async Task<IntelligenceReport> CommercialFunnelAsync(ReportFilter f, CancellationToken ct)
    {
        var from = From(f); var to = To(f); var accountId = AccountId;
        var query = db.Documents.AsNoTracking().Where(x => x.AccountId == accountId && !x.IsDeleted && x.Type == DocumentType.Budget && x.CreatedAt >= from && x.CreatedAt < to);
        if (f.ClientId is { } clientId) query = query.Where(x => x.ClientId == clientId);
        if (!string.IsNullOrWhiteSpace(f.Status)) query = query.Where(x => x.Status == f.Status);
        var documents = await query.Select(x => new { x.Status, x.Total, x.ClientDecision, x.CreatedAt, x.UpdatedAt }).ToListAsync(ct);
        var rows = documents.GroupBy(x => NormalizeStage(x.Status, x.ClientDecision)).Select(g => new ReportRow(g.Key, g.Count(), g.Sum(x => x.Total), g.Where(x => x.ClientDecision == ClientDecision.Approved).Sum(x => x.Total), 0, (decimal)g.Average(x => ((x.UpdatedAt ?? DateTime.UtcNow) - x.CreatedAt).TotalDays))).OrderBy(x => StageOrder(x.Label)).ToArray();
        var decided = documents.Count(x => x.ClientDecision is ClientDecision.Approved or ClientDecision.Rejected);
        var approved = documents.Count(x => x.ClientDecision == ClientDecision.Approved);
        return new("Funil comercial", [new("Propostas", documents.Count), new("Valor proposto", documents.Sum(x => x.Total), true), new("Valor aprovado", documents.Where(x => x.ClientDecision == ClientDecision.Approved).Sum(x => x.Total), true), new("Taxa de aprovação", decided == 0 ? -1 : approved * 100m / decided, false, "%")], rows);
    }

    public async Task<IntelligenceReport> FinancialAsync(ReportFilter f, CancellationToken ct)
    {
        var from = From(f); var to = To(f); var accountId = AccountId;
        var proposed = await db.Documents.AsNoTracking().Where(x => x.AccountId == accountId && !x.IsDeleted && x.Type == DocumentType.Budget && x.CreatedAt >= from && x.CreatedAt < to).SumAsync(x => x.Total, ct);
        var approved = await db.Documents.AsNoTracking().Where(x => x.AccountId == accountId && !x.IsDeleted && x.Type == DocumentType.Budget && x.ClientDecision == ClientDecision.Approved && x.CreatedAt >= from && x.CreatedAt < to).SumAsync(x => x.Total, ct);
        var payments = db.ManualPayments.AsNoTracking().Where(x => x.AccountId == accountId && !x.IsDeleted && x.PaidAt >= from && x.PaidAt < to);
        if (f.ClientId is { } clientId) payments = payments.Where(x => x.ClientId == clientId);
        if (!string.IsNullOrWhiteSpace(f.PaymentMethod)) payments = payments.Where(x => x.PaymentMethod == f.PaymentMethod);
        var paymentRows = await payments.GroupBy(x => x.PaymentMethod).Select(g => new ReportRow(g.Key, g.Count(), 0, 0, g.Where(x => x.Status == FinancialRecordStatus.Active).Sum(x => x.Amount), g.Where(x => x.Status == FinancialRecordStatus.Reversed).Sum(x => x.Amount))).ToListAsync(ct);
        var received = paymentRows.Sum(x => x.Received); var reversed = paymentRows.Sum(x => x.Extra ?? 0);
        var receipts = await db.Receipts.CountAsync(x => x.AccountId == accountId && !x.IsDeleted && x.IssuedAt >= from && x.IssuedAt < to, ct);
        return new("Financeiro", [new("Valor proposto", proposed, true), new("Valor aprovado", approved, true), new("Valor recebido", received, true), new("Saldo pendente", Math.Max(0, approved - received), true), new("Pagamentos revertidos", reversed, true), new("Recibos emitidos", receipts)], paymentRows);
    }

    public async Task<IntelligenceReport> ClientsAsync(ReportFilter f, CancellationToken ct)
    {
        var from = From(f); var to = To(f); var accountId = AccountId;
        var clients = await db.Clients.AsNoTracking().Where(x => x.AccountId == accountId && !x.IsDeleted).Select(x => new { x.Id, x.Name, x.IsActive, x.LastInteractionAt }).ToListAsync(ct);
        var docs = await db.Documents.AsNoTracking().Where(x => x.AccountId == accountId && !x.IsDeleted && x.ClientId != null && x.CreatedAt >= from && x.CreatedAt < to).Select(x => new { x.ClientId, x.Total, x.ClientDecision }).ToListAsync(ct);
        var paid = await db.ManualPayments.AsNoTracking().Where(x => x.AccountId == accountId && !x.IsDeleted && x.Status == FinancialRecordStatus.Active && x.PaidAt >= from && x.PaidAt < to).GroupBy(x => x.ClientId).Select(g => new { ClientId = g.Key, Total = g.Sum(x => x.Amount) }).ToDictionaryAsync(x => x.ClientId, x => x.Total, ct);
        var rows = clients.Select(c => new ReportRow(c.Name, docs.Count(x => x.ClientId == c.Id), docs.Where(x => x.ClientId == c.Id).Sum(x => x.Total), docs.Where(x => x.ClientId == c.Id && x.ClientDecision == ClientDecision.Approved).Sum(x => x.Total), paid.GetValueOrDefault(c.Id), c.LastInteractionAt is null ? null : (decimal)(DateTime.UtcNow - c.LastInteractionAt.Value).TotalDays)).OrderByDescending(x => x.Proposed).ToArray();
        return new("Clientes", [new("Cadastrados", clients.Count), new("Ativos", clients.Count(x => x.IsActive)), new("Sem movimentação (30 dias)", clients.Count(x => x.LastInteractionAt == null || x.LastInteractionAt < DateTime.UtcNow.AddDays(-30))), new("Com propostas aprovadas", rows.Count(x => x.Approved > 0))], rows);
    }

    public async Task<IntelligenceReport> ServicesAsync(ReportFilter f, CancellationToken ct)
    {
        var from = From(f); var to = To(f); var accountId = AccountId;
        var services = await db.ServiceCatalogItems.AsNoTracking().Where(x => x.AccountId == accountId && !x.IsDeleted).Select(x => new { x.Id, x.Name, x.EstimatedCost }).ToListAsync(ct);
        var items = await (from item in db.DocumentItems.AsNoTracking() join doc in db.Documents.AsNoTracking() on item.DocumentId equals doc.Id where doc.AccountId == accountId && !doc.IsDeleted && !item.IsDeleted && doc.Type == DocumentType.Budget && doc.CreatedAt >= from && doc.CreatedAt < to select new { item.ServiceCatalogItemId, item.Quantity, item.UnitPrice, item.Discount, item.EstimatedCostSnapshot, doc.ClientDecision }).ToListAsync(ct);
        var rows = services.Select(s => { var used = items.Where(x => x.ServiceCatalogItemId == s.Id).ToArray(); var total = used.Sum(x => Math.Max(0, x.Quantity * x.UnitPrice - x.Discount)); var approved = used.Where(x => x.ClientDecision == ClientDecision.Approved).Sum(x => Math.Max(0, x.Quantity * x.UnitPrice - x.Discount)); decimal? margin = used.Any(x => x.EstimatedCostSnapshot > 0) ? approved - used.Where(x => x.ClientDecision == ClientDecision.Approved).Sum(x => x.Quantity * x.EstimatedCostSnapshot) : null; return new ReportRow(s.Name, used.Length, total, approved, 0, margin); }).OrderByDescending(x => x.Approved).ToArray();
        return new("Serviços", [new("Serviços cadastrados", services.Count), new("Nunca usados", rows.Count(x => x.Count == 0)), new("Valor vendido", rows.Sum(x => x.Approved), true), new("Itens em propostas", rows.Sum(x => x.Count))], rows);
    }

    private static string NormalizeStage(string status, ClientDecision decision) => decision == ClientDecision.Approved ? "Aprovado" : decision == ClientDecision.Rejected ? "Recusado" : status switch { "Draft" => "Rascunho", "Issued" => "Pronto", "Sent" => "Enviado", "Viewed" => "Visualizado", "Converted" => "Convertido em OS", "Expired" => "Expirado", _ => status };
    private static int StageOrder(string stage) => Array.IndexOf(["Rascunho", "Pronto", "Enviado", "Visualizado", "Em negociação", "Aprovado", "Recusado", "Expirado", "Convertido em OS"], stage) is var value && value >= 0 ? value : 99;
}
