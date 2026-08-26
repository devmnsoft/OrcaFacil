using Microsoft.AspNetCore.Mvc.RazorPages;
namespace OrcaFacil.Web.Pages.Segmentos;
public sealed class IndexModel:PageModel{
 private static readonly Dictionary<string,string> Names=new(StringComparer.OrdinalIgnoreCase){{"AssistenciaTecnica","Assistência técnica"},{"PrestadoresDeServico","Prestadores de serviço"},{"ManutencaoPreventiva","Manutenção preventiva"},{"Consultoria","Consultoria"},{"TI","Serviços de TI"},{"Agencias","Agências"},{"Reformas","Reformas"},{"ServicosRecorrentes","Serviços recorrentes"}};
 public string Title{get;private set;}="OrçaFácil para empresas de serviços"; public string Description{get;private set;}="Organize o processo comercial e operacional sem perder o contexto do cliente."; public string[] Modules{get;}=["CRM e clientes","Orçamentos e propostas","Ordens de serviço","Financeiro e contratos"];
 public void OnGet(string? slug){if(slug is not null&&Names.TryGetValue(slug,out var name)){Title=$"OrçaFácil para {name}";Description=$"Transforme pedidos de {name.ToLowerInvariant()} em propostas, execução e acompanhamento organizados.";}}
}
