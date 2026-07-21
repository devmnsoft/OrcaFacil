namespace OrcaFacil.Web.ViewModels.Components;

public sealed class StepCardViewModel
{
    public string Number { get; init; } = "1";
    public string Title { get; init; } = "Passo";
    public string Text { get; init; } = "Siga este passo para avançar.";
    public string? Icon { get; init; }
    public string? ActionText { get; init; }
    public string? ActionPage { get; init; }
    public bool IsActive { get; init; }
    public bool IsDone { get; init; }
}
