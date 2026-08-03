using System.ComponentModel.DataAnnotations;
using OrcaFacil.Application.Services;
namespace OrcaFacil.Web.ViewModels.Services;
public sealed class ServiceEditorInput
{
    [Required,MaxLength(180)] public string Name { get; set; } = ""; [MaxLength(40)] public string? Code { get; set; }
    public Guid? CategoryId { get; set; } [MaxLength(1200)] public string? Description { get; set; } [Required] public string UnitCode { get; set; } = "service";
    [Range(0,999999999)] public decimal StandardPrice { get; set; } [Range(0,999999999)] public decimal EstimatedCost { get; set; }
    [Range(1,525600)] public int? SuggestedDurationMinutes { get; set; } [MaxLength(2000)] public string? InternalNotes { get; set; }
    public bool IsFavorite { get; set; } public bool IsActive { get; set; } = true; public uint Version { get; set; }
}
public sealed class ServiceEditorViewModel
{
    public ServiceEditorInput Input { get; init; } = new(); public IReadOnlyList<ServiceUnitOption> UnitOptions { get; init; }=[];
    public IReadOnlyList<ServiceCategoryOption> CategoryOptions { get; init; }=[]; public bool IsEditing { get; init; }
    public bool RequiresPriceChangeReason { get; init; } public string? PriceChangeReason { get; init; }
    public decimal CalculatedMargin => Input.StandardPrice-Input.EstimatedCost;
    public decimal CalculatedMarginPercentage => Input.StandardPrice == 0 ? 0 : CalculatedMargin/Input.StandardPrice*100;
    public string PreviewTitle { get; init; }="Como aparecerá no orçamento"; public string SubmitLabel { get; init; }="Salvar serviço";
}
