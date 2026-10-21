using Microsoft.AspNetCore.Authorization;using Microsoft.AspNetCore.Mvc;using Microsoft.AspNetCore.Mvc.RazorPages;using OrcaFacil.Application.Abstractions;using OrcaFacil.Application.Plans;using OrcaFacil.Application.Services;using OrcaFacil.Domain.Plans;using OrcaFacil.Web.ViewModels.Services;
namespace OrcaFacil.Web.Pages.Services;
[Authorize] public sealed class CreateModel(IServiceCatalogApplicationService catalog,IServiceUnitCatalog units,ICurrentAccountService current,IPlanAccessService plans):PageModel
{
    [BindProperty] public ServiceEditorInput Input{get;set;}=new(); public ServiceEditorViewModel Editor{get;private set;}=new();
    public void OnGet()=>Build();
    public async Task<IActionResult> OnPostAsync(CancellationToken ct){if(!units.Contains(Input.UnitCode))ModelState.AddModelError("Input.UnitCode","Unidade inválida.");if(!ModelState.IsValid){Build();return Page();}
        var account=current.AccountId;
        if(account.HasValue){ // Gate de plano: serviços ativos (Free: 20); CanUseAsync barra trial expirado junto.
            var decision=await plans.CanUseAsync(account.Value,PlanFeatureCodes.ServicesActiveLimit,ct);
            if(!decision.IsAllowed){ModelState.AddModelError(string.Empty,decision.InternalReason=="PlanLimitReached"&&decision.Limit is int limit?$"Seu plano permite até {limit} serviço(s) ativo(s). Faça upgrade para continuar adicionando.":decision.UserMessage);Build();return Page();}}
        var result=await catalog.CreateAsync(Map(),ct);if(result.Code!=ServiceCatalogResultCode.Success){ModelState.AddModelError("",result.Message??"Não foi possível salvar.");Build();return Page();}TempData["Success"]="Serviço adicionado ao catálogo.";return RedirectToPage("Index");}
    private ServiceCatalogInput Map()=>new(Input.Name,Input.Code,Input.Description,Input.CategoryId,Input.UnitCode,Input.StandardPrice,Input.EstimatedCost,Input.DesiredMarginPercentage,Input.SuggestedDurationMinutes,Input.DefaultDeliveryTerm,Input.DefaultNotes,Input.Tags,Input.InternalNotes,Input.IsFavorite,Input.IsActive,Input.IsRecurring,Input.IsRecommended,Input.DefaultPeriodicity,Input.SuggestedMonthlyPrice,Input.EstimatedMonthlyCost,Input.DefaultResponseSlaHours,Input.DefaultExecutionSlaHours,Input.DefaultChecklist);
    private void Build()=>Editor=new(){Input=Input,UnitOptions=units.GetAll()};
}
