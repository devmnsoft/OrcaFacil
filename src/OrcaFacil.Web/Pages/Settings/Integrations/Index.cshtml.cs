using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Application.Security;
using OrcaFacil.Domain.Entities;
using OrcaFacil.Persistence;

namespace OrcaFacil.Web.Pages.Settings.Integrations;

[Authorize(Policy = "Permission:" + PermissionCodes.IntegrationsView)]
public sealed class IndexModel(OrcaFacilDbContext db, ICurrentAccountService current, IAuthorizationService authorization, IDataProtectionProvider protection, IAuditService audit) : PageModel
{
    [BindProperty] public InputModel Input { get; set; } = new();
    public bool SmtpPasswordConfigured { get; private set; }
    public sealed class InputModel { [Url] public string? PublicBaseUrl { get; set; } public string? WhatsAppNumber { get; set; } public string? EmailSignature { get; set; } [EmailAddress] public string? SupportEmail { get; set; } public string? SmtpHost { get; set; } [Range(1,65535)] public int? SmtpPort { get; set; } public string? SmtpUser { get; set; } [DataType(DataType.Password)] public string? SmtpPassword { get; set; } public bool SmtpUseSsl { get; set; } = true; public bool EmailSendingEnabled { get; set; } }
    public async Task<IActionResult> OnGetAsync(CancellationToken ct) { if (!current.AccountId.HasValue) return Forbid(); var x=await db.IntegrationSettings.AsNoTracking().SingleOrDefaultAsync(x=>x.AccountId==current.AccountId&&!x.IsDeleted,ct); if(x is not null){ Input=new(){PublicBaseUrl=x.PublicBaseUrl,WhatsAppNumber=x.WhatsAppNumber,EmailSignature=x.EmailSignature,SupportEmail=x.SupportEmail,SmtpHost=x.SmtpHost,SmtpPort=x.SmtpPort,SmtpUser=x.SmtpUser,SmtpUseSsl=x.SmtpUseSsl,EmailSendingEnabled=x.EmailSendingEnabled}; SmtpPasswordConfigured=!string.IsNullOrEmpty(x.ProtectedSmtpPassword); } return Page(); }
    public async Task<IActionResult> OnPostAsync(CancellationToken ct) { if(!current.AccountId.HasValue || !(await authorization.AuthorizeAsync(User,"Permission:"+PermissionCodes.IntegrationsManage)).Succeeded) return Forbid(); if(Input.EmailSendingEnabled && (string.IsNullOrWhiteSpace(Input.SmtpHost)||!Input.SmtpPort.HasValue)){ModelState.AddModelError(string.Empty,"Informe host e porta SMTP antes de habilitar envios.");} if(!ModelState.IsValid)return Page(); var id=current.AccountId.Value; var x=await db.IntegrationSettings.SingleOrDefaultAsync(x=>x.AccountId==id&&!x.IsDeleted,ct); if(x is null){x=new(){AccountId=id};db.Add(x);} x.PublicBaseUrl=Input.PublicBaseUrl?.TrimEnd('/');x.WhatsAppNumber=Digits(Input.WhatsAppNumber);x.EmailSignature=Input.EmailSignature?.Trim();x.SupportEmail=Input.SupportEmail?.Trim();x.SmtpHost=Input.SmtpHost?.Trim();x.SmtpPort=Input.SmtpPort;x.SmtpUser=Input.SmtpUser?.Trim();x.SmtpUseSsl=Input.SmtpUseSsl;x.EmailSendingEnabled=Input.EmailSendingEnabled;if(!string.IsNullOrWhiteSpace(Input.SmtpPassword))x.ProtectedSmtpPassword=protection.CreateProtector("OrcaFacil.Integrations.Smtp.v1").Protect(Input.SmtpPassword);await db.SaveChangesAsync(ct);await audit.RegisterAsync(current.UserId,"integrations.updated",nameof(IntegrationSetting),x.Id.ToString(),null,new{x.PublicBaseUrl,x.SmtpHost,x.SmtpPort,x.SmtpUseSsl,x.EmailSendingEnabled,PasswordChanged=!string.IsNullOrWhiteSpace(Input.SmtpPassword)},null,ct,id);TempData["Success"]="Integrações atualizadas. Nenhum envio de teste foi simulado.";return RedirectToPage(); }
    private static string? Digits(string? value)=>string.IsNullOrWhiteSpace(value)?null:new(value.Where(char.IsDigit).ToArray());
}
