using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using OrcaFacil.Domain.Entities;
using OrcaFacil.Persistence;
namespace OrcaFacil.Web.Services;
public interface IAdminShellViewModelFactory { Task<AdminShellViewModel> CreateAsync(CancellationToken cancellationToken=default); }
public sealed class AdminShellViewModelFactory(IHttpContextAccessor context,OrcaFacilDbContext db):IAdminShellViewModelFactory
{
 public async Task<AdminShellViewModel> CreateAsync(CancellationToken ct=default)
 {
  var dead=await db.EmailOutboxMessages.AsNoTracking().CountAsync(x=>x.Status==EmailOutboxStatus.DeadLetter,ct);
  var errors=await db.SystemErrors.AsNoTracking().CountAsync(ct);
  var name=context.HttpContext?.User.FindFirstValue(ClaimTypes.Name)??"SuperAdministrador";
  return new(name,errors+dead,0,dead,"Monitorado","Monitorado",0,AdminMenu.Items,[new("Abrir usuários","/Admin/Users"),new("Investigar erros","/Admin/Errors")],context.HttpContext?.TraceIdentifier??"indisponível");
 }
}
public sealed record AdminShellViewModel(string SuperAdminName,int CriticalAlertCount,int PendingSupportCount,int DeadLetterCount,string DatabaseHealth,string EmailHealth,int PendingMigrationCount,IReadOnlyList<ShellMenuGroup> Menus,IReadOnlyList<AdminQuickAction> QuickActions,string CurrentCorrelationId);
public sealed record AdminQuickAction(string Label,string Url);
internal static class AdminMenu { internal static readonly IReadOnlyList<ShellMenuGroup> Items=[new("Visão geral",[new("Central de operação","/Dashboard","dashboard")]),new("Clientes",[new("Usuários","/Users","person")]),new("Operação",[new("Erros","/Errors","error"),new("Auditoria e logs","/Logs","audit")]),new("Sistema",[new("Configurações","/Settings","settings")])]; }
