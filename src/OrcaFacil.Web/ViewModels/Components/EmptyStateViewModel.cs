namespace OrcaFacil.Web.ViewModels.Components;

public sealed class EmptyStateViewModel
{
    public string Title { get; init; } = "Nenhum conteúdo ainda.";
    public string Text { get; init; } = "Quando houver informações, elas aparecerão aqui.";
    public string Icon { get; init; } = "bi-stars";
    public string? ActionText { get; init; }
    public string? ActionPage { get; init; }
}
