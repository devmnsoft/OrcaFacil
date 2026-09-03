using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OrcaFacil.Application.Quality;

namespace OrcaFacil.Web.Pages.Admin;

[Authorize(Policy = "SuperAdminOnly")]
public sealed class QualityGateModel(QualityGateService qualityGate, IWebHostEnvironment environment) : PageModel
{
    public QualityGateSnapshot? Snapshot { get; private set; }
    public string? LoadMessage { get; private set; }

    public async Task OnGetAsync(CancellationToken ct)
    {
        try
        {
            Snapshot = await qualityGate.EvaluateAsync((Directory.GetParent(environment.ContentRootPath)?.Parent?.FullName ?? environment.ContentRootPath), User.Identity?.Name ?? "Execução automatizada", ct);
        }
        catch
        {
            LoadMessage = "Não foi possível concluir todos os diagnósticos agora. Verifique a conexão do banco e tente novamente.";
        }
    }
}
