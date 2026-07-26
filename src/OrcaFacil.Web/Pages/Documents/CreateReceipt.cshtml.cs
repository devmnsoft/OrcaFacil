using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Application.Documents;
using OrcaFacil.Domain.Enums;
using OrcaFacil.Persistence;

namespace OrcaFacil.Web.Pages.Documents;

[Authorize]
public sealed class CreateReceiptModel : PageModel
{
    private readonly ICurrentUserService _current;
    private readonly OrcaFacilDbContext _db;
    private readonly DocumentService _service;

    public CreateReceiptModel(ICurrentUserService current, DocumentService service, OrcaFacilDbContext db)
    {
        _current = current;
        _service = service;
        _db = db;
    }

    [BindProperty]
    public DocumentForm Input { get; set; } = DocumentForm.Default();

    public async Task OnGetAsync(Guid? clientId, CancellationToken ct)
    {
        if (!clientId.HasValue) return;
        var client = await _db.Clients.AsNoTracking().SingleOrDefaultAsync(x => x.Id == clientId && x.UserId == _current.UserId && !x.IsDeleted, ct);
        if (client is not null) Input.ClientName = client.Name;
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        var cmd = new CreateDocumentCommand(_current.UserId, DocumentType.Receipt, "", Input.ClientName, Input.ToItems(), Input.Discount, Input.Notes);
        var r = await _service.CreateReceiptAsync(cmd, ct);
        if (!r.Succeeded)
        {
            ModelState.AddModelError("", r.Error ?? "Erro ao criar recibo.");
            return Page();
        }

        return RedirectToPage("/Documents/Details", new { id = r.Value });
    }
}
