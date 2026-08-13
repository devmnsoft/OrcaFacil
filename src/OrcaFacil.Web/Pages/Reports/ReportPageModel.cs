using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Persistence;
using OrcaFacil.Web.Services;

namespace OrcaFacil.Web.Pages.Reports;

public abstract class ReportPageModel(IIntelligenceReportService reports, ICurrentAccountService account, OrcaFacilDbContext db) : PageModel
{
    [BindProperty(SupportsGet = true)] public DateTime? From { get; set; }
    [BindProperty(SupportsGet = true)] public DateTime? To { get; set; }
    [BindProperty(SupportsGet = true)] public Guid? ClientId { get; set; }
    [BindProperty(SupportsGet = true)] public string? Status { get; set; }
    [BindProperty(SupportsGet = true)] public string? PaymentMethod { get; set; }
    public IntelligenceReport Report { get; protected set; } = new("Relatório", [], []);
    public IReadOnlyList<ReportClientOption> Clients { get; private set; } = [];
    protected ReportFilter Filter => new(From, To, ClientId, Status, PaymentMethod);
    protected abstract Task<IntelligenceReport> LoadAsync(ReportFilter filter, CancellationToken ct);
    protected abstract string FilePrefix { get; }

    public async Task OnGetAsync(CancellationToken ct)
    {
        var accountId = account.AccountId ?? throw new UnauthorizedAccessException("Selecione uma conta para consultar relatórios.");
        Clients = await db.Clients.AsNoTracking()
            .Where(x => x.AccountId == accountId && !x.IsDeleted && x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new ReportClientOption(x.Id, x.Name))
            .ToListAsync(ct);

        if (!ValidatePeriod()) return;
        Report = await LoadAsync(Filter, ct);
    }

    public async Task<IActionResult> OnGetExportAsync(CancellationToken ct)
    {
        if (!ValidatePeriod())
        {
            TempData["Error"] = "Revise o período informado antes de exportar.";
            return RedirectToPage(new { From, To, ClientId, Status, PaymentMethod });
        }

        var report = await LoadAsync(Filter, ct);
        if (report.Rows.Count == 0)
        {
            TempData["Warning"] = "Não há dados para exportar com os filtros aplicados.";
            return RedirectToPage(new { From, To, ClientId, Status, PaymentMethod });
        }
        var csv = new StringBuilder("\uFEFFEtapa/Item;Quantidade;Valor proposto;Valor aprovado;Valor recebido;Indicador\r\n");
        foreach (var row in report.Rows)
            csv.AppendLine(string.Join(';', Csv(row.Label), row.Count, Decimal(row.Proposed), Decimal(row.Approved), Decimal(row.Received), row.Extra is null ? "" : Decimal(row.Extra.Value)));
        TempData["Success"] = "CSV gerado com os filtros aplicados.";
        return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv; charset=utf-8", $"{FilePrefix}-{DateTime.UtcNow:yyyy-MM-dd}.csv");
    }

    private static string Csv(string value) => $"\"{value.Replace("\"", "\"\"")}\"";
    private static string Decimal(decimal value) => value.ToString("0.00", CultureInfo.InvariantCulture);

    private bool ValidatePeriod()
    {
        if (From.HasValue && To.HasValue && From.Value.Date > To.Value.Date)
        {
            ModelState.AddModelError(string.Empty, "A data inicial não pode ser posterior à data final.");
            return false;
        }

        if (From.HasValue && To.HasValue && (To.Value.Date - From.Value.Date).TotalDays > 366)
        {
            ModelState.AddModelError(string.Empty, "Selecione um período de no máximo 12 meses.");
            return false;
        }

        return true;
    }
}

public sealed record ReportClientOption(Guid Id, string Name);
