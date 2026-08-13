using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Persistence;
namespace OrcaFacil.Web.Pages.Settings;
[Authorize]
public sealed class CompanyModel(OrcaFacilDbContext db, ICurrentAccountService current, IAuditService audit) : PageModel
{
 [BindProperty] public Form Input {get;set;}=new();
 public sealed class Form { [Required,MaxLength(180)] public string TradeName{get;set;}=""; [MaxLength(180)] public string? LegalName{get;set;} [MaxLength(18)] public string? DocumentNumber{get;set;} [EmailAddress] public string Email{get;set;}=""; [Phone] public string? Phone{get;set;} public string? WhatsApp{get;set;} public string? Website{get;set;} public string? PostalCode{get;set;} public string? State{get;set;} public string? Address{get;set;} public string? City{get;set;} public string? StateRegistration{get;set;} public string? MunicipalRegistration{get;set;} public string? Notes{get;set;} }
 public async Task<IActionResult> OnGetAsync(CancellationToken ct){if(!current.AccountId.HasValue)return Forbid(); var a=await db.BusinessAccounts.AsNoTracking().SingleAsync(x=>x.Id==current.AccountId&&!x.IsDeleted,ct);var s=await db.AccountSettings.AsNoTracking().SingleOrDefaultAsync(x=>x.AccountId==a.Id&&!x.IsDeleted,ct);Input=new(){TradeName=a.TradeName??a.DisplayName,LegalName=a.LegalName,DocumentNumber=a.DocumentNumber,Email=a.Email,Phone=a.Phone,WhatsApp=s?.WhatsApp,Website=s?.Website,PostalCode=s?.PostalCode,Address=s?.Address,City=s?.City,State=s?.State,StateRegistration=s?.StateRegistration,MunicipalRegistration=s?.MunicipalRegistration,Notes=s?.InstitutionalNotes};return Page();}
 public async Task<IActionResult> OnPostAsync(CancellationToken ct){if(!current.AccountId.HasValue||!await current.HasPermissionAsync("account.edit",ct))return Forbid();if(!ModelState.IsValid)return Page();var a=await db.BusinessAccounts.SingleAsync(x=>x.Id==current.AccountId&&!x.IsDeleted,ct);var before=new{a.DisplayName,a.LegalName,a.DocumentNumber,a.Email,a.Phone};a.DisplayName=Input.TradeName.Trim();a.TradeName=Input.TradeName.Trim();a.LegalName=Input.LegalName?.Trim();a.DocumentNumber=Input.DocumentNumber?.Trim();a.Email=Input.Email.Trim();a.Phone=Input.Phone?.Trim();var s=await db.AccountSettings.SingleOrDefaultAsync(x=>x.AccountId==a.Id&&!x.IsDeleted,ct);if(s is null){s=new(){AccountId=a.Id};db.AccountSettings.Add(s);}s.WhatsApp=Input.WhatsApp?.Trim();s.Website=Input.Website?.Trim();s.PostalCode=Input.PostalCode?.Trim();s.Address=Input.Address?.Trim();s.City=Input.City?.Trim();s.State=Input.State?.Trim();s.StateRegistration=Input.StateRegistration?.Trim();s.MunicipalRegistration=Input.MunicipalRegistration?.Trim();s.InstitutionalNotes=Input.Notes?.Trim();await audit.RegisterAsync(current.UserId,"company.updated","BusinessAccount",a.Id.ToString(),before,new{a.DisplayName,a.LegalName,a.DocumentNumber,a.Email,a.Phone},null,ct,a.Id);await db.SaveChangesAsync(ct);TempData["Success"]="Dados da empresa atualizados.";return RedirectToPage();}
}
