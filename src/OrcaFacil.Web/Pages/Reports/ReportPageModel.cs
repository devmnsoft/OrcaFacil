using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OrcaFacil.Web.Services;

namespace OrcaFacil.Web.Pages.Reports;

public abstract class ReportPageModel(IIntelligenceReportService reports) : PageModel
{
    [BindProperty(SupportsGet = true)] public DateTime? From { get; set; }
    [BindProperty(SupportsGet = true)] public DateTime? To { get; set; }
    [BindProperty(SupportsGet = true)] public Guid? ClientId { get; set; }
    [BindProperty(SupportsGet = true)] public string? Status { get; set; }
    [BindProperty(SupportsGet = true)] public string? PaymentMethod { get; set; }
    public IntelligenceReport Report { get; protected set; } = new("Relatório", [], []);
    protected ReportFilter Filter => new(From, To, ClientId, Status, PaymentMethod);
    protected abstract Task<IntelligenceReport> LoadAsync(ReportFilter filter, CancellationToken ct);
    protected abstract string FilePrefix { get; }

    public async Task OnGetAsync(CancellationToken ct) => Report = await LoadAsync(Filter, ct);

    public async Task<IActionResult> OnGetExportAsync(CancellationToken ct)
    {
        var report = await LoadAsync(Filter, ct);
        var csv = new StringBuilder("\uFEFFEtapa/Item;Quantidade;Valor proposto;Valor aprovado;Valor recebido;Indicador\r\n");
        foreach (var row in report.Rows)
            csv.AppendLine(string.Join(';', Csv(row.Label), row.Count, Decimal(row.Proposed), Decimal(row.Approved), Decimal(row.Received), row.Extra is null ? "" : Decimal(row.Extra.Value)));
        TempData["Success"] = "CSV gerado com os filtros aplicados.";
        return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv; charset=utf-8", $"{FilePrefix}-{DateTime.UtcNow:yyyy-MM-dd}.csv");
    }

    private static string Csv(string value) => $"\"{value.Replace("\"", "\"\"")}\"";
    private static string Decimal(decimal value) => value.ToString("0.00", CultureInfo.InvariantCulture);
}
