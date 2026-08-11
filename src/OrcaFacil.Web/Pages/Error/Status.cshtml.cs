using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
namespace OrcaFacil.Web.Pages.Error;
public sealed class StatusModel : PageModel
{
 [BindProperty(SupportsGet=true)] public int Code {get;set;}
 public string CorrelationId => HttpContext.Response.Headers["X-Correlation-Id"].FirstOrDefault() ?? HttpContext.TraceIdentifier;
 public string Message => Code switch {400=>"Não conseguimos entender a solicitação.",401=>"Entre na sua conta para continuar.",403=>"Você não tem permissão para acessar este conteúdo.",404=>"A página que você procura não foi encontrada.",503=>"O OrçaFácil está temporariamente indisponível.",_=>"Não foi possível concluir esta operação."};
 public void OnGet(int code){Code=code; Response.StatusCode=code;}
}
