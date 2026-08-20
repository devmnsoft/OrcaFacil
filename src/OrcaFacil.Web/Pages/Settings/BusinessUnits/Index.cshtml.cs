using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Domain.Entities;
using OrcaFacil.Persistence;

namespace OrcaFacil.Web.Pages.Settings.BusinessUnits;
[Authorize(Policy="Permission:BusinessUnits.View")]
public sealed class IndexModel(OrcaFacilDbContext db, ICurrentAccountService current, IAuditService audit) : PageModel
{
    public IReadOnlyList<BusinessUnit> Units { get; private set; } = [];
    [BindProperty] public string Name { get; set; } = string.Empty;
    public async Task<IActionResult> OnGetAsync(CancellationToken ct) { if (current.AccountId is not Guid accountId) return Forbid(); Units=await db.BusinessUnits.AsNoTracking().Where(x=>x.AccountId==accountId&&!x.IsDeleted).OrderByDescending(x=>x.IsMain).ThenBy(x=>x.Name).ToListAsync(ct); return Page(); }
    public async Task<IActionResult> OnPostCreateAsync(CancellationToken ct) { if (!await CanManage(ct)||current.AccountId is not Guid accountId) return Forbid(); if(string.IsNullOrWhiteSpace(Name)){ModelState.AddModelError(nameof(Name),"Informe o nome da unidade.");return await OnGetAsync(ct);} var first=!await db.BusinessUnits.AnyAsync(x=>x.AccountId==accountId&&x.IsMain&&x.IsActive&&!x.IsDeleted,ct); var unit=new BusinessUnit{AccountId=accountId,Name=Name.Trim(),IsMain=first};db.BusinessUnits.Add(unit);await audit.RegisterAsync(current.UserId,"business-unit.created",nameof(BusinessUnit),unit.Id.ToString(),null,new{unit.Name,unit.IsMain},null,ct,accountId);await db.SaveChangesAsync(ct);return RedirectToPage(); }
    public async Task<IActionResult> OnPostSetMainAsync(Guid id,CancellationToken ct) { if(!await CanManage(ct)||current.AccountId is not Guid accountId)return Forbid();var units=await db.BusinessUnits.Where(x=>x.AccountId==accountId&&!x.IsDeleted).ToListAsync(ct);var selected=units.SingleOrDefault(x=>x.Id==id&&x.IsActive);if(selected is null)return NotFound();foreach(var unit in units){unit.IsMain=unit.Id==id;unit.Touch();}await audit.RegisterAsync(current.UserId,"business-unit.main-changed",nameof(BusinessUnit),id.ToString(),null,new{MainUnitId=id},null,ct,accountId);await db.SaveChangesAsync(ct);return RedirectToPage(); }
    private Task<bool> CanManage(CancellationToken ct)=>current.HasPermissionAsync("BusinessUnits.Manage",ct);
}
