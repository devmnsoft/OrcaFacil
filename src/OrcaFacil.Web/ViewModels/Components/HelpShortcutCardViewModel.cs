namespace OrcaFacil.Web.ViewModels.Components;

public sealed class HelpShortcutCardViewModel
{
    public string Title { get; init; } = "Ajuda";
    public string Text { get; init; } = "Abrir manual";
    public string Icon { get; init; } = "bi-life-preserver";
    public string Page { get; init; } = "/Support/Index";
}
