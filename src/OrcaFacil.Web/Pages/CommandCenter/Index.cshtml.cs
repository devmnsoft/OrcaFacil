using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Application.Security;

namespace OrcaFacil.Web.Pages.CommandCenter;

[Authorize]
public sealed class IndexModel(ICurrentAccountService account) : PageModel
{
    public IReadOnlyList<CommandAction> Actions { get; private set; } = [];
    public async Task<IActionResult> OnGetAsync(CancellationToken ct)
    {
        if (!await account.HasPermissionAsync(PermissionCodes.CommandCenterUse, ct)) return Forbid();
        var candidates = new[] {
            new Candidate("Novo cliente","Comercial","Cadastre um contato para começar a vender.","/Clients/Create",PermissionCodes.ClientsManage),
            new Candidate("Novo orçamento","Comercial","Monte uma proposta com valores calculados no servidor.","/Documents/New",PermissionCodes.DocumentsCreate),
            new Candidate("Nova OS","Operação","Abra a lista e crie uma ordem autorizada.","/WorkOrders/Index",PermissionCodes.WorkOrdersCreate),
            new Candidate("Registrar pagamento","Financeiro","Abra o fluxo com revisão e confirmação.","/Payments/Register",PermissionCodes.PaymentsManage),
            new Candidate("Emitir recibo","Financeiro","Emita um recibo para um pagamento real.","/Receipts/Create",PermissionCodes.ReceiptsManage),
            new Candidate("Agenda de hoje","Operação","Veja os serviços programados.","/Schedule/Index",PermissionCodes.ScheduleView),
            new Candidate("Aprovações pendentes","Gestão","Revise solicitações que aguardam decisão.","/Approvals/Index",PermissionCodes.ApprovalsView),
            new Candidate("Abrir chamados","Suporte","Acompanhe solicitações da conta.","/Support/Tickets",PermissionCodes.SupportView),
            new Candidate("Meu plano","Conta","Consulte uso, limites e assinatura.","/Subscription/Index",PermissionCodes.BillingViewOwn),
            new Candidate("Configurações","Conta","Configure sua empresa com segurança.","/Settings/Index",PermissionCodes.SettingsView)
        };
        var visible = new List<CommandAction>();
        foreach (var item in candidates)
            if (await account.HasPermissionAsync(item.Permission, ct)) visible.Add(new(item.Title,item.Category,item.Description,item.Url));
        Actions = visible;
        return Page();
    }
    private sealed record Candidate(string Title,string Category,string Description,string Url,string Permission);
    public sealed record CommandAction(string Title,string Category,string Description,string Url);
}
