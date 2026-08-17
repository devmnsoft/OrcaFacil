using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OrcaFacil.Persistence;

namespace OrcaFacil.Web.Areas.Admin.Pages.Webhooks;

[Authorize(Policy = "PlatformPaymentManagement")]
public sealed class IndexModel(OrcaFacilDbContext db) : PageModel
{
    public IReadOnlyList<Row> Items { get; private set; } = [];
    public async Task OnGetAsync(bool? processed, CancellationToken ct)
    {
        var query = db.MercadoPagoWebhookEvents.AsNoTracking();
        if (processed.HasValue) query = query.Where(x => x.Processed == processed);
        Items = await query.OrderByDescending(x => x.CreatedAt).Take(200)
            .Select(x => new Row(x.CreatedAt, x.EventKey, x.Topic, x.ExternalPaymentId, x.Processed, x.CorrelationId)).ToListAsync(ct);
    }
    public sealed record Row(DateTime CreatedAt, string EventKey, string? Topic, string? PaymentId, bool Processed, string? CorrelationId);
}
