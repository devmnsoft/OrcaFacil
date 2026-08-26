using Microsoft.AspNetCore.Mvc.RazorPages;
namespace OrcaFacil.Web.Pages.Recursos;
public sealed class IndexModel : PageModel
{
 private static readonly Dictionary<string,(string Title,string Description,string How)> Catalog=new(StringComparer.OrdinalIgnoreCase){
 ["Orcamentos"]=("Orçamentos profissionais","Crie, revise e acompanhe propostas com informações claras.","Cadastre cliente e serviços, componha valores, revise condições e gere o documento."),
 ["Propostas"]=("Propostas e aprovação","Compartilhe propostas por PDF ou link seguro e registre a decisão.","A proposta pública apresenta somente dados destinados ao cliente e preserva custos internos."),
 ["OS"]=("Ordens de serviço","Conecte a venda aprovada à execução e ao acompanhamento operacional.","Planeje responsáveis, agenda e evidências dentro do fluxo disponível."),
 ["Financeiro"]=("Gestão financeira","Acompanhe recebíveis, pagamentos registrados e recibos.","O financeiro usa lançamentos reais; recibos não substituem documentos fiscais."),
 ["Contratos"]=("Contratos e recorrência","Organize vigências, entregas e renovações de serviços.","Registre o contrato e acompanhe obrigações, recorrência e calendário."),
 ["CRM"]=("CRM de serviços","Preserve histórico, interações e próximas ações comerciais.","Centralize o contexto do cliente para dar continuidade ao relacionamento."),
 ["BI"]=("Indicadores operacionais","Analise dados registrados na operação, sem métricas inventadas.","Painéis consolidam informações persistidas e respeitam o acesso da conta."),
 ["Portais"]=("Portais seguros","Ofereça jornadas específicas para clientes e parceiros autorizados.","Acesso e conteúdo dependem de autenticação ou token válido, conforme o portal."),
 ["API"]=("API e webhooks","Integre fluxos por credenciais, escopos e endpoints configurados.","Chaves, limites e webhooks são administrados nas configurações autenticadas."),
 ["IA"]=("IA governada","Use recursos de IA somente quando um provedor estiver configurado.","Sem provedor e consentimento operacional, o recurso permanece indisponível e não simula respostas.")};
 public string Title{get;private set;}="Recursos para uma operação conectada"; public string Description{get;private set;}="Conheça módulos existentes do OrçaFácil e escolha os que atendem seu processo."; public string HowItWorks{get;private set;}="Clientes, serviços, documentos e execução compartilham o mesmo contexto operacional.";
 public IReadOnlyDictionary<string,string> Benefits{get;}=new Dictionary<string,string>{{"Menos retrabalho","Reutilize dados já cadastrados."},{"Mais contexto","Acompanhe histórico e próximas ações."},{"Acesso controlado","Permissões e plano governam a disponibilidade."}};
 public void OnGet(string? slug){if(slug is not null&&Catalog.TryGetValue(slug,out var item)){Title=item.Title;Description=item.Description;HowItWorks=item.How;}}
}
