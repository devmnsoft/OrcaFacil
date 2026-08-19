using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OrcaFacil.Domain.Enums;
using OrcaFacil.Persistence;

namespace OrcaFacil.Web.Areas.Admin.Pages.Accounts;

[Authorize(Policy = "SuperAdminOnly")]
public sealed class IndexModel(OrcaFacilDbContext db) : PageModel
{
    public string? Query { get; private set; }
    public string? Status { get; private set; }
    public string? Plan { get; private set; }
    public int Total { get; private set; }
    public int Active { get; private set; }
    public int Blocked { get; private set; }
    public IReadOnlyList<Row> Items { get; private set; } = [];

    public async Task OnGetAsync(string? query, string? status, string? plan, CancellationToken ct)
    {
        Query = query?.Trim(); Status = status; Plan = plan?.Trim();
        var source = db.BusinessAccounts.AsNoTracking().Where(x => !x.IsDeleted);
        Total = await source.CountAsync(ct);
        Active = await source.CountAsync(x => x.Status == AccountStatus.Active, ct);
        Blocked = await source.CountAsync(x => x.Status == AccountStatus.Blocked, ct);
        if (!string.IsNullOrWhiteSpace(Query)) source = source.Where(x => x.DisplayName.Contains(Query) || x.Email.Contains(Query) || (x.DocumentNumber != null && x.DocumentNumber.Contains(Query)));
        if (Enum.TryParse<AccountStatus>(status, out var accountStatus)) source = source.Where(x => x.Status == accountStatus);
        if (!string.IsNullOrWhiteSpace(Plan)) source = source.Where(x => x.CurrentPlanCode == Plan);
        Items = await source.OrderBy(x => x.DisplayName).Take(200).Select(x => new Row(x.Id, x.DisplayName, x.Email, x.DocumentNumber == null ? "—" : "***" + x.DocumentNumber.Substring(Math.Max(0, x.DocumentNumber.Length - 4)), x.Status.ToString(), x.CurrentPlanCode, db.AccountMembers.Count(m => m.AccountId == x.Id && !m.IsDeleted))).ToListAsync(ct);
    }
    public sealed record Row(Guid Id, string Name, string Email, string Document, string Status, string Plan, int Members);
}
