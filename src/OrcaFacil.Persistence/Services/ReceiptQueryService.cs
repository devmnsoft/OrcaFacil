using Microsoft.EntityFrameworkCore;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Application.Receipts;
using OrcaFacil.Domain.Enums;

namespace OrcaFacil.Persistence.Services;

public sealed class ReceiptQueryService(OrcaFacilDbContext db, ICurrentAccountService account) : IReceiptQueryService
{
    public async Task<ReceiptListResult?> ListAsync(ReceiptListQuery request, CancellationToken ct = default)
    {
        if (account.AccountId is not Guid accountId) return null;
        var pageSize = Math.Clamp(request.PageSize, 10, 100);
        var query = db.Receipts.AsNoTracking().Where(x => x.AccountId == accountId && !x.IsDeleted);
        if (request.From.HasValue) query = query.Where(x => x.IssuedAt >= request.From.Value);
        if (request.To.HasValue) query = query.Where(x => x.IssuedAt < request.To.Value.Date.AddDays(1));
        if (request.ClientId.HasValue) query = query.Where(x => x.ClientId == request.ClientId.Value);
        if (request.OriginType.HasValue) query = query.Where(x => x.OriginType == request.OriginType.Value);
        if (request.MinimumAmount.HasValue) query = query.Where(x => x.Amount >= request.MinimumAmount.Value);
        if (request.MaximumAmount.HasValue) query = query.Where(x => x.Amount <= request.MaximumAmount.Value);
        if (!string.IsNullOrWhiteSpace(request.PaymentMethod) && PaymentMethodCodes.TryParse(request.PaymentMethod, out var method))
        {
            var code = method.ToCode(); var label = method.ToLabel();
            query = query.Where(x => x.PaymentMethod == code || x.PaymentMethod == label);
        }
        if (request.Status == "cancelled") query = query.Where(x => x.CancelledAt != null);
        else if (request.Status == "active") query = query.Where(x => x.CancelledAt == null);

        var total = await query.CountAsync(ct);
        var totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)pageSize));
        var page = Math.Clamp(request.Page, 1, totalPages);
        var ordered = request.Sort switch
        {
            "oldest" => query.OrderBy(x => x.IssuedAt).ThenBy(x => x.Id),
            "amount_desc" => query.OrderByDescending(x => x.Amount).ThenByDescending(x => x.IssuedAt).ThenBy(x => x.Id),
            "amount_asc" => query.OrderBy(x => x.Amount).ThenByDescending(x => x.IssuedAt).ThenBy(x => x.Id),
            "client" => query.OrderBy(x => db.Clients.Where(c => c.Id == x.ClientId && c.AccountId == accountId).Select(c => c.Name).FirstOrDefault()).ThenByDescending(x => x.IssuedAt).ThenBy(x => x.Id),
            _ => query.OrderByDescending(x => x.IssuedAt).ThenByDescending(x => x.Id)
        };
        var rows = await ordered.Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new { x.Id, x.Number, x.ClientId, ClientName = db.Clients.Where(c => c.Id == x.ClientId && c.AccountId == accountId).Select(c => c.Name).FirstOrDefault() ?? "Cliente", x.OriginType, x.Amount, x.PaymentMethod, x.IssuedAt, x.LastSharedAt, x.CancelledAt }).ToListAsync(ct);
        var summary = await query.GroupBy(_ => 1).Select(g => new { Active = g.Where(x => x.CancelledAt == null).Sum(x => x.Amount), Issued = g.Count(), Shared = g.Count(x => x.LastSharedAt != null), Cancelled = g.Count(x => x.CancelledAt != null) }).FirstOrDefaultAsync(ct);
        var items = rows.Select(x => new ReceiptListItem(x.Id, x.Number, x.ClientId, x.ClientName, x.OriginType,
            x.OriginType switch { ReceiptOriginType.Budget => "Orçamento", ReceiptOriginType.WorkOrder => "Ordem de serviço", _ => "Pagamento manual" }, x.Amount,
            PaymentMethodCodes.TryParse(x.PaymentMethod, out var payment) ? payment.ToCode() : "other", PaymentMethodCodes.ToDisplayLabel(x.PaymentMethod), x.IssuedAt, x.LastSharedAt, x.CancelledAt,
            x.CancelledAt.HasValue ? "cancelled" : x.LastSharedAt.HasValue ? "shared" : "issued", x.CancelledAt.HasValue ? "Cancelado" : x.LastSharedAt.HasValue ? "Compartilhado" : "Emitido",
            x.CancelledAt.HasValue ? "danger" : x.LastSharedAt.HasValue ? "success" : "info", x.CancelledAt.HasValue ? "view" : x.LastSharedAt.HasValue ? "download" : "share", x.CancelledAt.HasValue ? "Consultar" : x.LastSharedAt.HasValue ? "Baixar PDF" : "Compartilhar")).ToList();
        return new(items, total, page, pageSize, totalPages, summary?.Active ?? 0, summary?.Issued ?? 0, summary?.Shared ?? 0, summary?.Cancelled ?? 0);
    }
}
