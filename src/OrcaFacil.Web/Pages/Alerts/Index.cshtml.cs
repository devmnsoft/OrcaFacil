using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Domain.Entities;
using OrcaFacil.Domain.Enums;
using OrcaFacil.Persistence;
using OrcaFacil.Web.Services;

namespace OrcaFacil.Web.Pages.Alerts;

[Authorize]
public sealed class IndexModel(IOperationalAlertService alerts, ICurrentAccountService account, OrcaFacilDbContext db) : PageModel
{
    [BindProperty(SupportsGet = true)] public string? Type { get; set; }
    [BindProperty(SupportsGet = true)] public string? Status { get; set; }

    public IReadOnlyList<Notification> Items { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken ct)
    {
        await alerts.GenerateAsync(ct);
        Items = await FilteredQuery(asTracking: false)
            .OrderBy(x => x.IsRead)
            .ThenByDescending(x => x.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<IActionResult> OnGetExportAsync(CancellationToken ct)
    {
        await alerts.GenerateAsync(ct);
        var items = await FilteredQuery(asTracking: false)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(ct);
        if (items.Count == 0)
        {
            TempData["Warning"] = "Não há alertas para exportar com os filtros aplicados.";
            return RedirectToPage(new { Type, Status });
        }

        var csv = new StringBuilder("\uFEFFTipo;Severidade;Título;Descrição;Status;Criado em;Encerrado em;Ação\r\n");
        foreach (var item in items)
        {
            csv.AppendLine(string.Join(';',
                Csv(item.Category.ToString()),
                Csv(item.Type.ToString()),
                Csv(item.Title),
                Csv(Description(item)),
                Csv(item.IsRead ? "Encerrado" : "Aberto"),
                Csv(item.CreatedAt.ToString("O", CultureInfo.InvariantCulture)),
                Csv(item.ReadAt?.ToString("O", CultureInfo.InvariantCulture) ?? string.Empty),
                Csv(item.ActionUrl ?? string.Empty)));
        }

        TempData["Success"] = "CSV de alertas gerado com os filtros aplicados.";
        return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv; charset=utf-8", $"alertas-{DateTime.UtcNow:yyyy-MM-dd}.csv");
    }

    public async Task<IActionResult> OnPostResolveAsync(Guid id, CancellationToken ct) =>
        await Close(id, "Alerta resolvido.", ct);

    public async Task<IActionResult> OnPostIgnoreAsync(Guid id, CancellationToken ct) =>
        await Close(id, "Alerta ignorado.", ct);

    public static string Description(Notification item) => item.Message.Split(" [alert:", 2)[0];

    private IQueryable<Notification> FilteredQuery(bool asTracking)
    {
        var accountId = account.AccountId ?? Guid.Empty;
        IQueryable<Notification> query = db.Notifications.Where(x =>
            x.AccountId == accountId && !x.IsDeleted && x.Message.Contains("[alert:"));
        if (!asTracking) query = query.AsNoTracking();
        if (Enum.TryParse<NotificationCategory>(Type, true, out var category))
            query = query.Where(x => x.Category == category);
        if (string.Equals(Status, "open", StringComparison.OrdinalIgnoreCase)) query = query.Where(x => !x.IsRead);
        if (string.Equals(Status, "closed", StringComparison.OrdinalIgnoreCase)) query = query.Where(x => x.IsRead);
        return query;
    }

    private async Task<IActionResult> Close(Guid id, string message, CancellationToken ct)
    {
        var item = await FilteredQuery(asTracking: true).SingleOrDefaultAsync(x => x.Id == id, ct);
        if (item is null) return NotFound();
        item.MarkAsRead();
        await db.SaveChangesAsync(ct);
        TempData["Success"] = message;
        return RedirectToPage(new { Type, Status });
    }

    private static string Csv(string value) => $"\"{value.Replace("\"", "\"\"")}\"";
}
