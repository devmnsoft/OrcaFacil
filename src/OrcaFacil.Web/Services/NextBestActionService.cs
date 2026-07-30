using OrcaFacil.Application.Abstractions;
using OrcaFacil.Application.Profile;

namespace OrcaFacil.Web.Services;

public interface INextBestActionService
{
    Task<NextBestAction> GetAsync(CancellationToken cancellationToken = default);
}

public sealed class NextBestActionService(
    ICurrentUserService current,
    IDashboardQueries dashboardQueries,
    ProfileService profiles) : INextBestActionService
{
    public async Task<NextBestAction> GetAsync(CancellationToken cancellationToken = default)
    {
        var profile = await profiles.GetAsync(new(current.UserId), cancellationToken);
        if (profile is null)
            return new("Complete os dados do emitente", "Nome, documento e contato serão usados nos seus documentos.", "/Profile/Index", "Conferir dados", "high");

        var dashboard = await dashboardQueries.GetDashboardAsync(current.UserId, cancellationToken);
        if (dashboard.TotalDocuments == 0)
            return new("Prepare seu primeiro orçamento", "Cadastre o cliente e apresente serviços, valores, condições e prazo.", "/Documents/CreateBudget", "Criar orçamento", "high");
        if (dashboard.PdfsThisMonth == 0)
            return new("Gere e confira seu primeiro PDF", "Abra um documento para gerar o PDF e compartilhar com o cliente.", "/Documents/Index", "Abrir documentos", "normal");
        return new("Acompanhe suas propostas recentes", "Confira documentos que precisam de revisão ou resposta.", "/Documents/Index", "Ver documentos", "normal");
    }
}

public sealed record NextBestAction(string Title, string Explanation, string Page, string ActionLabel, string Priority);
