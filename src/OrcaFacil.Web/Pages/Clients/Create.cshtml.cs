using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Application.Clients;
using OrcaFacil.Application.Plans;
using OrcaFacil.Domain.Plans;
using OrcaFacil.Web.ViewModels.Clients;

namespace OrcaFacil.Web.Pages.Clients;

[Authorize]
public sealed class CreateModel(IClientWorkspaceService workspace, ICurrentAccountService current, IPlanAccessService plans) : PageModel
{
    [BindProperty] public ClientEditorInput Input { get; set; } = new();
    public void OnGet() { }
    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        if (!ModelState.IsValid) return Page();
        var account = current.AccountId;
        if (account.HasValue)
        {
            // Gate de plano: clientes ativos (Free: 25) — CanUseAsync também barra
            // criação com trial expirado, conforme a política de operações comerciais.
            var decision = await plans.CanUseAsync(account.Value, PlanFeatureCodes.ClientsActiveLimit, ct);
            if (!decision.IsAllowed)
            {
                ModelState.AddModelError(string.Empty, decision.InternalReason == "PlanLimitReached" && decision.Limit is int limit
                    ? $"Seu plano permite até {limit} cliente(s) ativo(s). Faça upgrade para continuar cadastrando."
                    : decision.UserMessage);
                return Page();
            }
        }
        var i=Input; var result = await workspace.CreateAsync(new(i.PersonType,i.DocumentType,i.DocumentNumber,i.Name,i.LegalName,i.TradeName,i.Email,i.Phone,i.City,i.Address,i.InternalNotes,i.PreferredContactChannel,i.NextFollowUpAt,i.IsFavorite,i.IsActive,i.AllowPossibleDuplicate), ct);
        if (result.Code == ClientResultCode.Success) { TempData["Success"] = "Cliente salvo com sucesso."; return RedirectToPage("Details", new { id = result.ClientId }); }
        ModelState.AddModelError(string.Empty, result.Message ?? "Não foi possível salvar o cliente.");
        return Page();
    }
}
