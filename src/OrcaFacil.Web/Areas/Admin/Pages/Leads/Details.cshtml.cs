using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OrcaFacil.Domain.Entities;
using OrcaFacil.Domain.Enums;
using OrcaFacil.Persistence;
using OrcaFacil.Web.Extensions;

namespace OrcaFacil.Web.Areas.Admin.Pages.Leads;

[Authorize(Policy = "SuperAdminOnly")]
public sealed class DetailsModel(OrcaFacilDbContext db) : PageModel
{
    public CommercialLead Lead { get; private set; } = null!;
    [BindProperty, StringLength(3000)] public string? Notes { get; set; }
    [BindProperty, StringLength(500)] public string? DiscardReason { get; set; }

    public async Task<IActionResult> OnGetAsync(Guid id, CancellationToken ct)
    {
        var lead = await Find(id, ct); if (lead is null) return NotFound();
        Lead = lead; Notes = lead.InternalNotes; return Page();
    }
    public Task<IActionResult> OnPostContactedAsync(Guid id, CancellationToken ct) => Change(id, CommercialLeadStatus.Contacted, ct);
    public Task<IActionResult> OnPostQualifiedAsync(Guid id, CancellationToken ct) => Change(id, CommercialLeadStatus.Qualified, ct);
    public async Task<IActionResult> OnPostDiscardAsync(Guid id, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(DiscardReason)) ModelState.AddModelError(nameof(DiscardReason), "Informe o motivo do descarte.");
        if (!ModelState.IsValid) { var lead = await Find(id, ct); if (lead is null) return NotFound(); Lead = lead; return Page(); }
        var item = await Find(id, ct); if (item is null) return NotFound(); item.Status = CommercialLeadStatus.Discarded; item.DiscardReason = DiscardReason!.Trim(); item.Touch(); await db.SaveChangesAsync(ct); TempData.Success("Lead descartado."); return RedirectToPage(new { id });
    }
    public async Task<IActionResult> OnPostNotesAsync(Guid id, CancellationToken ct)
    {
        var item = await Find(id, ct); if (item is null) return NotFound(); item.InternalNotes = string.IsNullOrWhiteSpace(Notes) ? null : Notes.Trim(); item.Touch(); await db.SaveChangesAsync(ct); TempData.Success("Observações salvas."); return RedirectToPage(new { id });
    }
    private async Task<IActionResult> Change(Guid id, CommercialLeadStatus status, CancellationToken ct) { var item = await Find(id, ct); if (item is null) return NotFound(); item.Status = status; item.Touch(); await db.SaveChangesAsync(ct); TempData.Success("Status atualizado."); return RedirectToPage(new { id }); }
    private Task<CommercialLead?> Find(Guid id, CancellationToken ct) => db.CommercialLeads.SingleOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);
}
