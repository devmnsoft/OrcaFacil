using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Application.Receipts;
using OrcaFacil.Domain.Entities;
using OrcaFacil.Persistence;

namespace OrcaFacil.Web.Pages.Receipts;

[Authorize]
public sealed class DetailsModel(OrcaFacilDbContext db, ICurrentAccountService account, IReceiptApplicationService service) : PageModel
{
    public Receipt? Receipt { get; private set; }
    public Client? Client { get; private set; }
    [BindProperty] public string CancellationReason { get; set; } = string.Empty;

    public async Task<IActionResult> OnGetAsync(Guid id, CancellationToken ct)
    {
        Receipt = await db.Receipts.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id && x.AccountId == account.AccountId && !x.IsDeleted, ct);
        if (Receipt is null) return NotFound();
        Client = await db.Clients.AsNoTracking().SingleOrDefaultAsync(x => x.Id == Receipt.ClientId && x.AccountId == account.AccountId && !x.IsDeleted, ct);
        return Page();
    }

    public async Task<IActionResult> OnPostCancelAsync(Guid id, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(CancellationReason)) { TempData["Error"] = "Informe o motivo do cancelamento."; return RedirectToPage(new { id }); }
        TempData[await service.CancelAsync(id, CancellationReason, ct) ? "Success" : "Error"] = "Cancelamento processado.";
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostShareAsync(Guid id, CancellationToken ct)
    {
        TempData[await service.MarkSharedAsync(id, ct) ? "Success" : "Error"] = "Compartilhamento registrado.";
        return RedirectToPage(new { id });
    }
}
