using System.Reflection;
using OrcaFacil.Domain.Entities;
using OrcaFacil.Web.Pages.Documents;
using Xunit;

namespace OrcaFacil.UnitTests;

public sealed class CreateBudgetJourneyContractTests
{
    [Fact]
    public void Budget_catalog_choice_exposes_only_client_safe_fields()
    {
        var item = new ServiceCatalogItem
        {
            Name = "Instalação",
            Description = "Instalação completa",
            UnitCode = "serviço",
            StandardPrice = 350,
            EstimatedCost = 120,
            InternalNotes = "Fornecedor interno",
            DefaultDeliveryTerm = "5 dias",
            DefaultNotes = "Inclui configuração",
            IsFavorite = true,
            IsRecurring = false
        };

        var choice = CreateBudgetModel.ToBudgetCatalogChoice(item);
        var exposedNames = typeof(BudgetCatalogChoice).GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Select(x => x.Name)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Equal(item.Id, choice.Id);
        Assert.Equal("Instalação", choice.Name);
        Assert.Equal(350, choice.Price);
        Assert.Equal("5 dias", choice.DeliveryTerm);
        Assert.Equal("Inclui configuração", choice.DefaultNotes);
        Assert.DoesNotContain(nameof(ServiceCatalogItem.EstimatedCost), exposedNames);
        Assert.DoesNotContain(nameof(ServiceCatalogItem.InternalNotes), exposedNames);
        Assert.DoesNotContain("Margin", exposedNames);
    }

    [Fact]
    public void Budget_wizard_uses_page_scoped_catalog_and_quick_client_handlers()
    {
        var root = GetRepositoryRoot();
        var page = File.ReadAllText(Path.Combine(root, "src", "OrcaFacil.Web", "Pages", "Documents", "CreateBudget.cshtml"));
        var script = File.ReadAllText(Path.Combine(root, "src", "OrcaFacil.Web", "wwwroot", "js", "budget-wizard.js"));

        Assert.Contains("data-quick-client", page, StringComparison.Ordinal);
        Assert.Contains("handler=QuickClient", script, StringComparison.Ordinal);
        Assert.Contains("handler=BudgetCatalog", script, StringComparison.Ordinal);
        Assert.DoesNotContain("/Internal/Services/Search", script, StringComparison.Ordinal);
    }

    [Fact]
    public void Client_search_preserves_selected_client_and_distinguishes_failures()
    {
        var root = GetRepositoryRoot();
        var script = File.ReadAllText(Path.Combine(root, "src", "OrcaFacil.Web", "wwwroot", "js", "budget-wizard.js"));

        Assert.Contains("Cliente selecionado:", script, StringComparison.Ordinal);
        Assert.Contains("x.classList.toggle('is-selected', x.dataset.clientId === selectedId)", script, StringComparison.Ordinal);
        Assert.DoesNotContain("wizard.querySelector('[data-client-id]').value = ''", script, StringComparison.Ordinal);
        Assert.Contains("renderClientSearchFailure", script, StringComparison.Ordinal);
        Assert.Contains("response.status === 401 || response.status === 403", script, StringComparison.Ordinal);
        Assert.Contains("!Array.isArray(data.results)", script, StringComparison.Ordinal);
        Assert.Contains("term !== clientSearch.value.trim()", script, StringComparison.Ordinal);
    }

    [Fact]
    public void Quick_client_duplicate_confirmation_is_bound_to_reviewed_fields()
    {
        var root = GetRepositoryRoot();
        var script = File.ReadAllText(Path.Combine(root, "src", "OrcaFacil.Web", "wwwroot", "js", "budget-wizard.js"));

        Assert.Contains("quickClientDuplicateSignature", script, StringComparison.Ordinal);
        Assert.Contains("const duplicateSignature = quickClientSignature()", script, StringComparison.Ordinal);
        Assert.Contains("quickClientDuplicateSignature !== duplicateSignature", script, StringComparison.Ordinal);
        Assert.Contains("input:not([data-quick-client-allow-duplicate])", script, StringComparison.Ordinal);
    }

    private static string GetRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "OrcaFacil.sln")))
            directory = directory.Parent;

        return directory?.FullName ?? throw new DirectoryNotFoundException("Repository root not found.");
    }
}
