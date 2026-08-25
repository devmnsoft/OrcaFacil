using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Application.Marketplace;
using OrcaFacil.Domain.Entities;
using OrcaFacil.Persistence;

namespace OrcaFacil.Web.Pages.Marketplace;
[Authorize]
public sealed class IndexModel(OrcaFacilDbContext db, ICurrentAccountService current) : PageModel
{
    public IReadOnlyList<PackageCard> Packages { get; private set; }=[];
    public async Task<IActionResult> OnGetAsync(CancellationToken ct)
    {
        if(current.AccountId is not Guid accountId || !await current.HasPermissionAsync(MarketplacePermissions.View,ct)) return Forbid();
        Packages=await(from p in db.MarketplacePackages.AsNoTracking() join v in db.MarketplacePackageVersions.AsNoTracking() on p.CurrentVersionId equals v.Id where p.IsActive&&p.IsPublished&&!p.IsDeleted&&v.IsPublished&&!v.IsDeleted select new PackageCard(p.Id,p.Name,p.Description,p.Category,p.TargetSegment,p.Author,v.Version,db.MarketplacePackageInstallations.Any(i=>i.AccountId==accountId&&i.PackageId==p.Id&&!i.IsDeleted&&i.Status==PackageInstallationStatus.Installed))).OrderBy(x=>x.Name).ToListAsync(ct);
        return Page();
    }
    public sealed record PackageCard(Guid Id,string Name,string Description,string Category,string Segment,string Author,string Version,bool Installed);
}
