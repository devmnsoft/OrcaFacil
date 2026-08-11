using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OrcaFacil.Domain.Entities;
using OrcaFacil.Domain.Enums;
using OrcaFacil.Persistence;

namespace OrcaFacil.Web.Areas.Admin.Pages.Leads;

[Authorize(Policy = "SuperAdminOnly")]
public sealed class IndexModel(OrcaFacilDbContext db) : PageModel
{
    public IReadOnlyList<CommercialLead> Items { get; private set; } = [];
    public async Task OnGetAsync(CommercialLeadStatus? status, CancellationToken ct)
    {
        var query = db.CommercialLeads.AsNoTracking().Where(x => !x.IsDeleted);
        if (status.HasValue) query = query.Where(x => x.Status == status);
        Items = await query.OrderByDescending(x => x.CreatedAt).Take(250).ToListAsync(ct);
    }
}
