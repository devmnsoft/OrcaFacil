namespace OrcaFacil.Web.ViewModels.Components;

public sealed class FeatureCardViewModel
{
    public string Title { get; init; } = "Recurso";
    public string Text { get; init; } = "Informação útil para usar o OrçaFácil com mais segurança.";
    public string Icon { get; init; } = "bi-check-circle";
    public string? ActionText { get; init; }
    public string? ActionPage { get; init; }
}
