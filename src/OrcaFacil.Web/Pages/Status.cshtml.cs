using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Diagnostics.HealthChecks;
namespace OrcaFacil.Web.Pages;
public sealed class StatusModel(HealthCheckService health):PageModel{
 public DateTime CheckedAt{get;private set;} public string Headline{get;private set;}="Verificando serviços"; public List<Component> Components{get;}=[];
 public async Task OnGetAsync(CancellationToken ct){var report=await health.CheckHealthAsync(r=>r.Tags.Contains("ready"),ct);CheckedAt=DateTime.UtcNow;Headline=report.Status==HealthStatus.Healthy?"Serviços monitorados operacionais":"Há uma indisponibilidade em investigação";foreach(var entry in report.Entries.OrderBy(x=>x.Key)){var ok=entry.Value.Status==HealthStatus.Healthy;Components.Add(new(entry.Key switch{"postgresql"=>"Banco de dados","local-settings"=>"Configuração da aplicação",_=>"Aplicação Web"},ok?"Operacional":"Indisponível",ok?"is-ok":"is-down"));}}
 public sealed record Component(string Name,string Label,string Css);
}
