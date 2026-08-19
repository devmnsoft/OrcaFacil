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
  var leads=await db.CommercialLeads.AsNoTracking().CountAsync(x=>!x.IsDeleted&&x.Status==OrcaFacil.Domain.Enums.CommercialLeadStatus.New,ct);
  var name=context.HttpContext?.User.FindFirstValue(ClaimTypes.Name)??"SuperAdministrador";
  return new(name,errors+dead,leads,dead,"Monitorado","Monitorado",0,AdminMenu.Items,[new("Ver novos leads","/Admin/Leads"),new("Investigar erros","/Admin/Errors")],context.HttpContext?.TraceIdentifier??"indisponível");
 }
}
public sealed record AdminShellViewModel(string SuperAdminName,int CriticalAlertCount,int PendingSupportCount,int DeadLetterCount,string DatabaseHealth,string EmailHealth,int PendingMigrationCount,IReadOnlyList<ShellMenuGroup> Menus,IReadOnlyList<AdminQuickAction> QuickActions,string CurrentCorrelationId);
public sealed record AdminQuickAction(string Label,string Url);
internal static class AdminMenu { internal static readonly IReadOnlyList<ShellMenuGroup> Items=[new("Visão geral",[new("Central de operação","/Dashboard","dashboard")]),new("Contas e acesso",[new("Contas","/Accounts/Index","business"),new("Usuários","/Users/Index","person")]),new("Comercial",[new("Leads","/Leads/Index","client")]),new("Financeiro",[new("Planos","/Plans/Index","plan"),new("Assinaturas","/Subscriptions/Index","subscription"),new("Pagamentos","/Payments/Index","payment"),new("Webhooks","/Webhooks/Index","activity")]),new("Atendimento",[new("Suporte","/Support/Index","support"),new("Feedbacks","/Feedback/Index","activity"),new("Base de conhecimento","/KnowledgeBase/Index","help"),new("Release Notes","/ReleaseNotes/Index","document")]),new("Operação",[new("Erros","/Errors","error"),new("Logs do sistema","/SystemLogs/Index","logs"),new("Auditoria","/Audit/Index","audit"),new("EmailOutbox","/EmailOutbox/Index","mail")]),new("Sistema",[new("Diagnóstico do banco","/Settings/Database","health"),new("Configurações","/Settings","settings")])]; }
