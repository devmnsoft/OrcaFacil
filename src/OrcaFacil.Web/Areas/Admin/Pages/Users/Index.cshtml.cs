using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OrcaFacil.Persistence;

namespace OrcaFacil.Web.Areas.Admin.Pages.Users;

[Authorize(Policy = "SuperAdminOnly")]
public sealed class IndexModel(OrcaFacilDbContext db) : PageModel
{
    public string? Query { get; private set; }
    public int Active { get; private set; }
    public int Blocked { get; private set; }
    public int Inactive { get; private set; }
    public IReadOnlyList<Row> Items { get; private set; } = [];
    public async Task OnGetAsync(string? query, string? status, CancellationToken ct)
    {
        Query = query?.Trim(); var source = db.Users.AsNoTracking().Where(x => !x.IsDeleted);
        Active = await source.CountAsync(x => x.IsActive && !x.IsBlocked, ct); Blocked = await source.CountAsync(x => x.IsBlocked, ct); Inactive = await source.CountAsync(x => !x.IsActive, ct);
        if (!string.IsNullOrWhiteSpace(Query)) source = source.Where(x => x.Name.Contains(Query) || x.Email.Contains(Query));
        source = status switch { "active" => source.Where(x => x.IsActive && !x.IsBlocked), "blocked" => source.Where(x => x.IsBlocked), "inactive" => source.Where(x => !x.IsActive), _ => source };
        Items = await source.OrderByDescending(x => x.LastLoginAt).Take(200).Select(x => new Row(x.Name, x.Email, x.Role.ToString(), x.IsBlocked ? "Bloqueado" : x.IsActive ? "Ativo" : "Inativo", x.LastLoginAt, x.LastFailedLoginAt, x.FailedLoginAttempts)).ToListAsync(ct);
    }
    public sealed record Row(string Name, string Email, string Role, string Status, DateTime? LastLoginAt, DateTime? LastFailedLoginAt, int FailedAttempts);
}
