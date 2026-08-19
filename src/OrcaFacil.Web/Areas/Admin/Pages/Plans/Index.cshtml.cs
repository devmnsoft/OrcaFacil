using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OrcaFacil.Domain.Enums;
using OrcaFacil.Persistence;

namespace OrcaFacil.Web.Areas.Admin.Pages.Plans;

[Authorize(Policy = "PlatformPlanManagement")]
public sealed class IndexModel(OrcaFacilDbContext db) : PageModel
{
    public IReadOnlyList<Row> Items { get; private set; } = [];
    public async Task OnGetAsync(CancellationToken ct)
    {
        Items = await db.Plans.AsNoTracking().Where(x => !x.IsDeleted).OrderBy(x => x.DisplayOrder).Select(x => new Row(x.Code, x.DisplayName, x.ShortDescription, x.IsActive, db.PlanVersions.Count(v => v.PlanId == x.Id && !v.IsDeleted), db.PlanVersions.Where(v => v.PlanId == x.Id && !v.IsDeleted && v.Status == PlanVersionStatus.Published).Max(v => (int?)v.VersionNumber), db.Subscriptions.Count(s => !s.IsDeleted && s.AccountId != null && s.SelectedPlanVersionId != null && db.PlanVersions.Any(v => v.Id == s.SelectedPlanVersionId && v.PlanId == x.Id)))).ToListAsync(ct);
    }
    public sealed record Row(string Code, string Name, string Description, bool IsActive, int Versions, int? PublishedVersion, int Subscriptions);
}
