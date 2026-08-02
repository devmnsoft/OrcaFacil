using System.ComponentModel.DataAnnotations;
namespace OrcaFacil.Web.Pages.Services;
public sealed class ServiceFormModel
{
    public static IReadOnlyDictionary<string,string> Units { get; }=new Dictionary<string,string>{{"service","Serviço"},{"hour","Hora"},{"day","Dia"},{"unit","Unidade"},{"meter","Metro"},{"square_meter","Metro quadrado"},{"kilometer","Quilômetro"},{"month","Mês"},{"package","Pacote"},{"other","Outro"}};
    [Required,MaxLength(180)] public string Name{get;set;}=""; [MaxLength(40)] public string? Code{get;set;} [MaxLength(1200)] public string? Description{get;set;} [Required] public string UnitCode{get;set;}="service"; [Range(0,999999999)] public decimal StandardPrice{get;set;} [Range(0,999999999)] public decimal EstimatedCost{get;set;} [Range(1,525600)] public int? SuggestedDurationMinutes{get;set;} [MaxLength(2000)] public string? InternalNotes{get;set;} public bool IsFavorite{get;set;} public bool IsActive{get;set;}=true;
}
