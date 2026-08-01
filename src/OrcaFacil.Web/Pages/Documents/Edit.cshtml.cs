using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Domain.Enums;
using OrcaFacil.Persistence;

namespace OrcaFacil.Web.Pages.Documents;

[Authorize]
public sealed class EditModel : PageModel
{
    private readonly OrcaFacilDbContext _db;
    private readonly ICurrentAccountService _currentAccount;

    public EditModel(OrcaFacilDbContext db, ICurrentAccountService currentAccount)
    {
        _db = db;
        _currentAccount = currentAccount;
    }

    public Guid DocumentId { get; private set; }
    public bool IsLegacyReceipt { get; private set; }
    public string Title { get; private set; } = "Documento protegido";
    public string Message { get; private set; } = "Este documento não pode ser alterado diretamente.";

    public async Task<IActionResult> OnGetAsync(Guid id, CancellationToken ct)
    {
        await _currentAccount.EnsureAccountAccessAsync(ct);
        if (_currentAccount.AccountId is not Guid accountId) return Forbid();

        var document = await _db.Documents.AsNoTracking().SingleOrDefaultAsync(
            x => x.Id == id && x.AccountId == accountId && !x.IsDeleted, ct);
        if (document is null) return NotFound();

        DocumentId = document.Id;
        if (document.Type == DocumentType.Budget && document.Status.Equals("Draft", StringComparison.OrdinalIgnoreCase))
            return RedirectToPage("/Documents/CreateBudget", new { documentId = id });

        if (document.Type == DocumentType.Receipt)
        {
            IsLegacyReceipt = true;
            Title = "Recibo histórico";
            Message = "O registro legado é imutável. Migre ou duplique conscientemente no formato operacional.";
            return Page();
        }

        TempData["Info"] = "Esta revisão é imutável. Crie uma nova versão ou duplique como rascunho.";
        return RedirectToPage("/Documents/Details", new { id });
    }
}
