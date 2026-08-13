using Microsoft.AspNetCore.Authorization;using Microsoft.AspNetCore.Mvc;using Microsoft.AspNetCore.Mvc.RazorPages;using OrcaFacil.Application.Services;using OrcaFacil.Web.ViewModels.Services;
namespace OrcaFacil.Web.Pages.Services;
[Authorize] public sealed class CreateModel(IServiceCatalogApplicationService catalog,IServiceUnitCatalog units):PageModel
{
    [BindProperty] public ServiceEditorInput Input{get;set;}=new(); public ServiceEditorViewModel Editor{get;private set;}=new();
    public void OnGet()=>Build();
    public async Task<IActionResult> OnPostAsync(CancellationToken ct){if(!units.Contains(Input.UnitCode))ModelState.AddModelError("Input.UnitCode","Unidade inválida.");if(!ModelState.IsValid){Build();return Page();}var result=await catalog.CreateAsync(Map(),ct);if(result.Code!=ServiceCatalogResultCode.Success){ModelState.AddModelError("",result.Message??"Não foi possível salvar.");Build();return Page();}TempData["Success"]="Serviço adicionado ao catálogo.";return RedirectToPage("Index");}
    private ServiceCatalogInput Map()=>new(Input.Name,Input.Code,Input.Description,Input.CategoryId,Input.UnitCode,Input.StandardPrice,Input.EstimatedCost,Input.DesiredMarginPercentage,Input.SuggestedDurationMinutes,Input.DefaultDeliveryTerm,Input.DefaultNotes,Input.Tags,Input.InternalNotes,Input.IsFavorite,Input.IsActive,Input.IsRecurring,Input.IsRecommended,Input.DefaultPeriodicity,Input.SuggestedMonthlyPrice,Input.EstimatedMonthlyCost,Input.DefaultResponseSlaHours,Input.DefaultExecutionSlaHours,Input.DefaultChecklist);
    private void Build()=>Editor=new(){Input=Input,UnitOptions=units.GetAll()};
}
